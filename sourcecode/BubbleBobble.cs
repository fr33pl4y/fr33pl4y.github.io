// +====================================================================+
// |                                                                    |
// |  ######  #####   #####   #####     #####   ##      ##  ##  ##  ##  |
// |  ##      ##  ##      ##      ##    ##  ##  ##      ##  ##  ##  ##  |
// |  #####   ##  ##   ####    ####     ##  ##  ##      ##  ##   ####   |
// |  ##      #####       ##      ##    #####   ##      ######    ##    |
// |  ##      ## ##   ##  ##  ##  ##    ##      ##          ##    ##    |
// |  ##      ##  ##   ####    ####     ##      ######      ##    ##    |
// |                                                                    |
// +====================================================================+
// Website: https://fr33pl4y.github.io/
// License: GNU General Public License v3 
// https://www.gnu.org/licenses/gpl-3.0.en.html

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BubbleBobble
{
    public static class Program
    {
        [STAThread]
        public static void Main() { using (var g = new BubbleBobbleGame()) g.Run(); }
    }

    class Actor
    {
        public float x, y, vx, vy;
        public bool ground, wrapped, nowrap, dead;
        public int dir = 1;
    }

    class Player : Actor
    {
        public int id, lives, score, deadT, inv, shootCd, shootAnim, jumps, shots;
        public bool active, outGame, dying, rapid, fast, shoes, gY, gB, gS;
        public float walked, anim;
        public bool[] letters = new bool[6];
    }

    class Enemy : Actor
    {
        public int variant, jumpCd, turnCd, anim;
        public bool angry;
        public float rot;
    }

    class Item : Actor { public int kind, sub, value, life; }
    class Hazard : Actor { public int kind, owner, life, anim; }

    class Bubble
    {
        public float x, y, vx;
        public int owner, phase, life, age, seed, tv, special = -1, letter, popDelay, chain;
        public bool trapped, tangry, dead, fast;
        public Player popBy;
    }

    class Popup { public string t; public float x, y; public int life; public Color c; }
    class Spark { public float x, y, vx, vy; public int life; public Color c; }
    struct Inp { public bool L, R, J, Jp, F, Fp; }

    public class BubbleBobbleGame : Game
    {
        const int W = 256, H = 224;
        const string EXT = "EXTEND";
        enum St { Title, Intro, Play, GameOver, Ending }

        GraphicsDeviceManager gfx;
        SpriteBatch sb;
        RenderTarget2D rt;
        Texture2D px, fontTex, tileTex, bubbleTex, ghostTex, fireTex, dropTex, boltTex;
        Texture2D[,] plTex = new Texture2D[2, 3];
        Texture2D[,,] enTex = new Texture2D[3, 2, 2];
        Texture2D[] fruitTex = new Texture2D[4];
        Texture2D[] candyTex = new Texture2D[3];

        KeyboardState kb, pkb;
        GamePadState[] pads = new GamePadState[2];
        Inp[] ins = new Inp[2];
        bool[] pJ = new bool[2], pF = new bool[2];
        int winW = 768, winH = 672;
        Random rng = new Random();

        St state = St.Title;
        int stT, level, hi = 20000, levelT, clearT, hurryT, specTimer;
        bool paused;
        int[,] tiles = new int[32, 28];
        List<int[]> plats = new List<int[]>();
        Player[] players = new Player[2];
        List<Enemy> enemies = new List<Enemy>(), deads = new List<Enemy>();
        List<Bubble> bubbles = new List<Bubble>();
        List<Item> items = new List<Item>();
        List<Hazard> hazards = new List<Hazard>();
        List<Popup> popups = new List<Popup>();
        List<Spark> sparks = new List<Spark>();
        Actor ghost;

        static readonly Color[] WallCols = {
            new Color(255,110,210), new Color(110,170,255), new Color(255,170,80),
            new Color(110,220,120), new Color(200,120,255), new Color(240,220,90) };

        // platform triples: row, firstCol, lastCol
        static readonly int[][] Layouts = {
            new[]{22,2,11, 22,20,29, 18,6,25, 14,2,11, 14,20,29, 10,6,25, 6,2,11, 6,20,29},
            new[]{22,8,23, 18,2,13, 18,18,29, 14,8,23, 10,2,13, 10,18,29, 6,8,23},
            new[]{22,2,9, 22,22,29, 18,6,13, 18,18,25, 14,2,9, 14,22,29, 10,6,13, 10,18,25, 6,10,21},
            new[]{22,2,14, 22,17,29, 18,2,11, 18,20,29, 14,2,14, 14,17,29, 10,2,11, 10,20,29, 6,2,14, 6,17,29},
            new[]{22,2,20, 18,11,29, 14,2,20, 10,11,29, 6,2,20},
            new[]{22,4,12, 22,19,27, 18,2,8, 18,23,29, 14,10,21, 10,2,8, 10,23,29, 6,4,12, 6,19,27},
            new[]{22,2,13, 22,18,29, 18,8,23, 14,2,13, 14,18,29, 10,8,23, 6,2,13, 6,18,29},
            new[]{22,6,25, 18,2,9, 18,22,29, 14,6,25, 10,2,9, 10,22,29, 6,6,25},
        };

        // 5x7 glyphs: one entry per character "<char>:<7 rows separated by commas>", rows use X (pixel) and - (empty)
        const string FontData =
            "A:-XXX-,X---X,X---X,XXXXX,X---X,X---X,X---X;B:XXXX-,X---X,X---X,XXXX-,X---X,X---X,XXXX-;C:-XXX-,X---X,X----,X----,X----,X---X,-XXX-;" +
            "D:XXXX-,X---X,X---X,X---X,X---X,X---X,XXXX-;E:XXXXX,X----,X----,XXXX-,X----,X----,XXXXX;F:XXXXX,X----,X----,XXXX-,X----,X----,X----;" +
            "G:-XXX-,X---X,X----,X-XXX,X---X,X---X,-XXXX;H:X---X,X---X,X---X,XXXXX,X---X,X---X,X---X;I:-XXX-,--X--,--X--,--X--,--X--,--X--,-XXX-;" +
            "J:--XXX,---X-,---X-,---X-,---X-,X--X-,-XX--;K:X---X,X--X-,X-X--,XX---,X-X--,X--X-,X---X;L:X----,X----,X----,X----,X----,X----,XXXXX;" +
            "M:X---X,XX-XX,X-X-X,X-X-X,X---X,X---X,X---X;N:X---X,XX--X,X-X-X,X--XX,X---X,X---X,X---X;O:-XXX-,X---X,X---X,X---X,X---X,X---X,-XXX-;" +
            "P:XXXX-,X---X,X---X,XXXX-,X----,X----,X----;Q:-XXX-,X---X,X---X,X---X,X-X-X,X--X-,-XX-X;R:XXXX-,X---X,X---X,XXXX-,X-X--,X--X-,X---X;" +
            "S:-XXXX,X----,X----,-XXX-,----X,----X,XXXX-;T:XXXXX,--X--,--X--,--X--,--X--,--X--,--X--;U:X---X,X---X,X---X,X---X,X---X,X---X,-XXX-;" +
            "V:X---X,X---X,X---X,X---X,X---X,-X-X-,--X--;W:X---X,X---X,X---X,X-X-X,X-X-X,XX-XX,X---X;X:X---X,X---X,-X-X-,--X--,-X-X-,X---X,X---X;" +
            "Y:X---X,X---X,-X-X-,--X--,--X--,--X--,--X--;Z:XXXXX,----X,---X-,--X--,-X---,X----,XXXXX;0:-XXX-,X---X,X--XX,X-X-X,XX--X,X---X,-XXX-;" +
            "1:--X--,-XX--,--X--,--X--,--X--,--X--,-XXX-;2:-XXX-,X---X,----X,---X-,--X--,-X---,XXXXX;3:XXXX-,----X,----X,-XXX-,----X,----X,XXXX-;" +
            "4:---X-,--XX-,-X-X-,X--X-,XXXXX,---X-,---X-;5:XXXXX,X----,XXXX-,----X,----X,X---X,-XXX-;6:--XX-,-X---,X----,XXXX-,X---X,X---X,-XXX-;" +
            "7:XXXXX,----X,---X-,--X--,-X---,-X---,-X---;8:-XXX-,X---X,X---X,-XXX-,X---X,X---X,-XXX-;9:-XXX-,X---X,X---X,-XXXX,----X,---X-,-XX--;" +
            ".:-----,-----,-----,-----,-----,-XX--,-XX--;!:--X--,--X--,--X--,--X--,--X--,-----,--X--;-:-----,-----,-----,XXXXX,-----,-----,-----;" +
            "::-----,-XX--,-XX--,-----,-XX--,-XX--,-----;?:-XXX-,X---X,----X,---X-,--X--,-----,--X--;':--X--,--X--,-X---,-----,-----,-----,-----;" +
            "=:-----,-----,XXXXX,-----,XXXXX,-----,-----;,:-----,-----,-----,-----,-XX--,--X--,-X---;+:-----,--X--,--X--,XXXXX,--X--,--X--,-----";

        public BubbleBobbleGame()
        {
            gfx = new GraphicsDeviceManager(this) { PreferredBackBufferWidth = winW, PreferredBackBufferHeight = winH };
            IsMouseVisible = false;
            Window.AllowUserResizing = true;
            Window.Title = "Bubble Bobble";
            for (int i = 0; i < 2; i++) players[i] = new Player { id = i };
        }

        // ------------------------------------------------------------------ textures
        Texture2D Tex(int w, int h, Func<int, int, Color> f)
        {
            var t = new Texture2D(GraphicsDevice, w, h);
            var d = new Color[w * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) d[y * w + x] = f(x, y);
            t.SetData(d);
            return t;
        }

        Texture2D Rows(string[] rows, Func<char, Color> pal)
        {
            return Tex(16, 16, (x, y) => (y < rows.Length && x < rows[y].Length) ? pal(rows[y][x]) : Color.Transparent);
        }

        static string[] PlayerRows(int frame)
        {
            var r = new[] {
                "................",
                "......kkkkk.....",
                ".....kgggggkk...",
                "..pp.kggggggggk.",
                ".pppkgggwwgwwgk.",
                ".ppkggggwbgwbggk",
                "..kgggggggggllgk",
                "..kkggggggggggk.",
                "..kglllllgggkk..",
                ".kgglllllllggk..",
                ".kgglllllllggk..",
                ".kkgglllllggkk..",
                "..kkgggggggkk...",
                "..kgggkkkgggk...",
                "...kggk.kggk....",
                "...kkkk.kkkk...." };
            if (frame == 1) { r[14] = "..kggk...kggk..."; r[15] = "..kkkk...kkkk..."; }
            if (frame == 2) r[6] = "..kgggggggggrrrk";
            return r;
        }

        static string[] EnemyRows(int frame)
        {
            var r = new[] {
                "......p..p......",
                "......kkkk......",
                "....kkggggkk....",
                "...kggggggggk...",
                "..kggwwggwwggk..",
                "..kggwbggwbggk..",
                "..kggggggggggk..",
                "..kggkkkkkkggk..",
                "..kggkwkwkkggk..",
                "...kggggggggk...",
                "...kggggggggk...",
                "....kggggggk....",
                ".....kkkkkk.....",
                "....yy....yy....",
                "...yyy....yyy...",
                "................" };
            if (frame == 1) { r[13] = "...yy......yy..."; r[14] = "..yyy......yyy.."; }
            return r;
        }

        Texture2D Fruit(Color c)
        {
            return Tex(16, 16, (x, y) =>
            {
                float dx = x - 7.5f, dy = y - 9.5f, d = dx * dx + dy * dy;
                if (d <= 34)
                {
                    if (d >= 26) return Color.Lerp(c, Color.Black, .4f);
                    if (x >= 4 && x <= 6 && y >= 6 && y <= 7) return Color.Lerp(c, Color.White, .6f);
                    return c;
                }
                if (x >= 7 && x <= 8 && y >= 2 && y <= 4) return new Color(110, 70, 30);
                if (y == 3 && x >= 9 && x <= 11) return new Color(60, 200, 60);
                return Color.Transparent;
            });
        }

        Texture2D Candy(Color c)
        {
            return Tex(16, 16, (x, y) =>
            {
                float dx = x - 7.5f, dy = y - 7.5f;
                if (dx * dx / 25f + dy * dy / 16f <= 1f) return (x + y) % 5 == 0 ? Color.White : c;
                int ady = (int)Math.Abs(dy + .5f);
                if (x >= 1 && x <= 3 && ady <= 4 - x) return Color.Lerp(c, Color.White, .3f);
                if (x >= 12 && x <= 14 && ady <= x - 11) return Color.Lerp(c, Color.White, .3f);
                return Color.Transparent;
            });
        }

        Color Pal(char ch, Color body, Color belly)
        {
            switch (ch)
            {
                case 'k': return new Color(28, 22, 50);
                case 'g': return body;
                case 'l': return belly;
                case 'w': return Color.White;
                case 'b': return new Color(10, 10, 30);
                case 'p': return new Color(250, 120, 40);
                case 'r': return new Color(230, 60, 90);
                case 'y': return new Color(255, 215, 60);
                default: return Color.Transparent;
            }
        }

        protected override void LoadContent()
        {
            sb = new SpriteBatch(GraphicsDevice);
            rt = new RenderTarget2D(GraphicsDevice, W, H);
            px = Tex(1, 1, (x, y) => Color.White);

            // bitmap font atlas (16 x 6 cells of 8x8, ASCII 32..127)
            var fd = new Color[128 * 48];
            foreach (var ent in FontData.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                char ch = ent[0];
                var v = ent.Substring(2).Split(',');
                int idx = ch - 32, cx = (idx % 16) * 8, cy = (idx / 16) * 8;
                for (int r = 0; r < 7; r++)
                    for (int c = 0; c < 5; c++)
                        if (v[r][c] == 'X') fd[(cy + r) * 128 + cx + c] = Color.White;
            }
            fontTex = new Texture2D(GraphicsDevice, 128, 48);
            fontTex.SetData(fd);

            tileTex = Tex(8, 8, (x, y) => ((x / 2 + y / 2) % 2 == 0) ? Color.White : new Color(205, 205, 205));

            bubbleTex = Tex(16, 16, (x, y) =>
            {
                float dx = x - 7.5f, dy = y - 7.5f, d = (float)Math.Sqrt(dx * dx + dy * dy);
                if (d > 7.6f) return Color.Transparent;
                if (d > 6.2f) return Color.White;
                if ((x == 4 && y == 4) || (x == 5 && y == 4) || (x == 4 && y == 5)) return Color.White;
                return new Color(70, 70, 70, 70);
            });

            Color[] body = { new Color(70, 205, 90), new Color(80, 140, 245) };
            Color[] belly = { new Color(240, 245, 150), new Color(205, 225, 255) };
            for (int p = 0; p < 2; p++)
                for (int f = 0; f < 3; f++)
                {
                    int pp = p; var rows = PlayerRows(f);
                    plTex[p, f] = Rows(rows, ch => Pal(ch, body[pp], belly[pp]));
                }

            Color[] ebody = { new Color(170, 90, 230), new Color(60, 190, 190), new Color(240, 160, 50) };
            for (int v = 0; v < 3; v++)
                for (int a = 0; a < 2; a++)
                    for (int f = 0; f < 2; f++)
                    {
                        Color c = a == 1 ? new Color(235, 60, 50) : ebody[v];
                        enTex[v, a, f] = Rows(EnemyRows(f), ch => Pal(ch, c, c));
                    }

            ghostTex = Rows(new[] {
                "................", "....wwwwwwww....", "..wwwwwwwwwwww..", ".wwwwwwwwwwwwww.",
                ".wwbbwwwwbbwwww.", ".wwbbwwwwbbwwww.", ".wwwwwwwwwwwwww.", ".wwwwwwkkwwwwww.",
                ".wwwwwwwwwwwwww.", ".wwwwwwwwwwwwww.", ".wwwwwwwwwwwwww.", ".wwwwwwwwwwwwww.",
                ".wwwwwwwwwwwwww.", ".ww.www.www.www." },
                ch => ch == 'w' ? new Color(200, 225, 255) : ch == 'b' ? new Color(20, 20, 60) : ch == 'k' ? new Color(120, 60, 100) : Color.Transparent);

            fireTex = Rows(new[] {
                "................", "................", "................", ".......o........", "......oo........",
                "......ooo.o.....", ".....oooyoo.....", ".....ooyyyo.....", ".....oyyyyoo....", ".....ooyyyo.....",
                "......ooooo....." },
                ch => ch == 'o' ? new Color(240, 90, 20) : ch == 'y' ? new Color(255, 230, 80) : Color.Transparent);
            dropTex = Rows(new[] {
                "................", "................", "................", ".......c........", "......ccc.......",
                "......cwc.......", ".....ccwcc......", ".....cwccc......", ".....ccccc......", "......ccc......." },
                ch => ch == 'c' ? new Color(40, 120, 255) : ch == 'w' ? Color.White : Color.Transparent);
            boltTex = Rows(new[] {
                "................", "................", "................", "........yy......", ".......yy.......",
                "......yy........", ".....yyyyy......", ".......yy.......", "......yy........", ".....y.........." },
                ch => ch == 'y' ? new Color(255, 235, 40) : Color.Transparent);

            fruitTex[0] = Fruit(new Color(230, 50, 50));
            fruitTex[1] = Fruit(new Color(255, 150, 30));
            fruitTex[2] = Fruit(new Color(150, 70, 200));
            fruitTex[3] = Fruit(new Color(250, 225, 60));

            candyTex[0] = Candy(new Color(255, 220, 40));
            candyTex[1] = Candy(new Color(60, 140, 255));
            candyTex[2] = Rows(new[] {
                "", "", "", "....kkkk", "....krrk", "....krrk", "....krrkk", "....krrrrkk", "...krrrrrrrkk",
                "..krrrrrrrrrrk", "..kwwwwwwwwwwk", "..kkkkkkkkkkkk" },
                ch => ch == 'k' ? new Color(40, 20, 30) : ch == 'r' ? new Color(230, 40, 50) : ch == 'w' ? Color.White : Color.Transparent);
        }

        // ------------------------------------------------------------------ helpers
        int Tile(int tx, int ty)
        {
            if (tx < 0 || tx >= 32) return 1;
            if (ty < 0 || ty >= 28) return 0;
            return tiles[tx, ty];
        }
        bool WallAt(float px_, float py_) { return Tile((int)Math.Floor(px_ / 8), (int)Math.Floor(py_ / 8)) == 1; }
        static bool Overlap(Actor a, Actor b) { return a.x + 2 < b.x + 14 && a.x + 14 > b.x + 2 && a.y + 2 < b.y + 16 && a.y + 16 > b.y + 2; }
        static Color Dim(Color c, float f) { return new Color((int)(c.R * f), (int)(c.G * f), (int)(c.B * f)); }
        bool Hit(Keys k) { return kb.IsKeyDown(k) && !pkb.IsKeyDown(k); }
        void AddScore(Player p, int v) { if (p == null) return; p.score += v; if (p.score > hi) hi = p.score; }
        void AddPopup(string t, float x, float y, Color c) { popups.Add(new Popup { t = t, x = x, y = y, life = 60, c = c }); }
        void AddSparks(float x, float y, Color c)
        {
            for (int i = 0; i < 6; i++)
            {
                double a = rng.NextDouble() * Math.PI * 2;
                sparks.Add(new Spark { x = x, y = y, vx = (float)Math.Cos(a) * 1.3f, vy = (float)Math.Sin(a) * 1.3f, life = 14, c = c });
            }
        }

        bool MoveX(Actor a, float dx)
        {
            bool hit = false;
            a.x += dx;
            float top = a.y + 2, mid = a.y + 9, bot = a.y + 15.9f;
            if (dx > 0)
            {
                float rx = a.x + 13.99f;
                if (WallAt(rx, top) || WallAt(rx, mid) || WallAt(rx, bot)) { a.x = (float)Math.Floor(rx / 8) * 8 - 14; hit = true; }
            }
            else if (dx < 0)
            {
                float lx = a.x + 2;
                if (WallAt(lx, top) || WallAt(lx, mid) || WallAt(lx, bot)) { a.x = (float)(Math.Floor(lx / 8) + 1) * 8 - 2; hit = true; }
            }
            float cx = MathHelper.Clamp(a.x, 14, 226);
            if (a.wrapped) cx = MathHelper.Clamp(cx, 96, 144);
            if (cx != a.x) { a.x = cx; hit = true; }
            return hit;
        }

        bool MoveY(Actor a)
        {
            float oldFeet = a.y + 16;
            a.y += a.vy;
            a.ground = false;
            int tx1 = (int)Math.Floor((a.x + 2) / 8), tx2 = (int)Math.Floor((a.x + 13.99f) / 8);
            if (a.vy > 0)
            {
                int ty = (int)Math.Floor((a.y + 16) / 8);
                if (ty >= 0 && ty < 28)
                    for (int tx = tx1; tx <= tx2; tx++)
                    {
                        int t = Tile(tx, ty);
                        if ((t == 1 || t == 2) && ty * 8 >= oldFeet - 0.01f) { a.y = ty * 8 - 16; a.vy = 0; a.ground = true; break; }
                    }
            }
            else if (a.vy < 0)
            {
                int ty = (int)Math.Floor((a.y + 2) / 8);
                if (ty >= 0 && ty < 28)
                    for (int tx = tx1; tx <= tx2; tx++)
                        if (Tile(tx, ty) == 1) { a.y = (ty + 1) * 8 - 2; a.vy = 0; break; }
            }
            if (a.wrapped && a.y >= 32) a.wrapped = false;
            if (a.y > H + 8)
            {
                if (a.nowrap) a.dead = true;
                else { a.y = -16; a.wrapped = true; a.x = MathHelper.Clamp(a.x, 96, 144); }
            }
            return a.ground;
        }

        // ------------------------------------------------------------------ game flow
        void StartGame(int np)
        {
            for (int i = 0; i < 2; i++)
            {
                var p = players[i];
                p.active = i < np; p.outGame = false; p.dying = false;
                p.lives = 2; p.score = 0; p.jumps = p.shots = 0; p.walked = 0;
                p.rapid = p.fast = p.shoes = p.gY = p.gB = p.gS = false;
                Array.Clear(p.letters, 0, 6);
            }
            level = 1;
            StartLevel();
        }

        void StartLevel()
        {
            Array.Clear(tiles, 0, tiles.Length);
            for (int y = 2; y < 28; y++)
                for (int x = 0; x < 32; x++)
                {
                    bool gap = x >= 12 && x <= 19;
                    if (x < 2 || x > 29) tiles[x, y] = 1;
                    else if (y <= 3 && !gap) tiles[x, y] = 1;
                    else if (y >= 26 && !gap) tiles[x, y] = 1;
                }
            plats.Clear();
            var L = Layouts[(level - 1) % Layouts.Length];
            bool mirror = ((level - 1) / Layouts.Length) % 2 == 1;
            for (int i = 0; i < L.Length; i += 3)
            {
                int r = L[i], a = L[i + 1], b = L[i + 2];
                if (mirror) { int na = 31 - b, nb = 31 - a; a = na; b = nb; }
                for (int x = a; x <= b; x++) { tiles[x, r] = 2; tiles[x, r + 1] = 3; }
                plats.Add(new[] { r, a, b });
            }
            enemies.Clear(); deads.Clear(); bubbles.Clear(); items.Clear(); hazards.Clear(); popups.Clear(); sparks.Clear();
            ghost = null; levelT = 0; clearT = 0; hurryT = 0; specTimer = 420;
            int cnt = Math.Min(3 + (level + 1) / 4, 9);
            for (int k = 0; k < cnt; k++)
            {
                var pl = plats[rng.Next(plats.Count)];
                int x0 = pl[1] * 8, x1 = (pl[2] + 1) * 8 - 16;
                enemies.Add(new Enemy
                {
                    x = rng.Next(x0, Math.Max(x0 + 1, x1 + 1)), y = pl[0] * 8 - 16, dir = rng.Next(2) == 0 ? -1 : 1,
                    variant = (level / 2 + k % 2) % 3, ground = true, jumpCd = 40 + rng.Next(60)
                });
            }
            foreach (var p in players) if (p.active && !p.outGame) Respawn(p, 90);
            state = St.Intro; stT = 0;
        }

        void Respawn(Player p, int inv)
        {
            p.x = p.id == 0 ? 24 : 216; p.y = 192; p.vx = p.vy = 0;
            p.dir = p.id == 0 ? 1 : -1; p.dying = false; p.inv = inv; p.ground = true;
            p.wrapped = false; p.dead = false;
        }

        void KillPlayer(Player p)
        {
            p.dying = true; p.deadT = 0; p.vy = -3.5f;
        }

        // ------------------------------------------------------------------ input
        Inp ReadIn(int i)
        {
            var r = new Inp(); var gp = pads[i];
            if (i == 0)
            {
                r.L = kb.IsKeyDown(Keys.Left); r.R = kb.IsKeyDown(Keys.Right);
                r.J = kb.IsKeyDown(Keys.Z) || kb.IsKeyDown(Keys.Up);
                r.F = kb.IsKeyDown(Keys.X) || kb.IsKeyDown(Keys.Space);
            }
            else
            {
                r.L = kb.IsKeyDown(Keys.A); r.R = kb.IsKeyDown(Keys.D);
                r.J = kb.IsKeyDown(Keys.W); r.F = kb.IsKeyDown(Keys.S);
            }
            if (gp.IsConnected)
            {
                r.L |= gp.DPad.Left == ButtonState.Pressed || gp.ThumbSticks.Left.X < -0.4f;
                r.R |= gp.DPad.Right == ButtonState.Pressed || gp.ThumbSticks.Left.X > 0.4f;
                r.J |= gp.Buttons.A == ButtonState.Pressed;
                r.F |= gp.Buttons.X == ButtonState.Pressed || gp.Buttons.B == ButtonState.Pressed;
            }
            r.Jp = r.J && !pJ[i]; r.Fp = r.F && !pF[i];
            pJ[i] = r.J; pF[i] = r.F;
            return r;
        }

        void ToggleFullscreen()
        {
            if (!gfx.IsFullScreen)
            {
                winW = gfx.PreferredBackBufferWidth; winH = gfx.PreferredBackBufferHeight;
                var dm = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
                gfx.PreferredBackBufferWidth = dm.Width; gfx.PreferredBackBufferHeight = dm.Height;
                gfx.HardwareModeSwitch = false;
                gfx.IsFullScreen = true;
            }
            else
            {
                gfx.IsFullScreen = false;
                gfx.PreferredBackBufferWidth = winW; gfx.PreferredBackBufferHeight = winH;
            }
            gfx.ApplyChanges();
        }

        // ------------------------------------------------------------------ update
        protected override void Update(GameTime gt)
        {
            kb = Keyboard.GetState();
            pads[0] = GamePad.GetState(PlayerIndex.One); pads[1] = GamePad.GetState(PlayerIndex.Two);
            if (Hit(Keys.F11)) ToggleFullscreen();
            ins[0] = ReadIn(0); ins[1] = ReadIn(1);

            switch (state)
            {
                case St.Title:
                    stT++;
                    if (Hit(Keys.Escape)) Exit();
                    if (Hit(Keys.D1) || Hit(Keys.NumPad1) || Hit(Keys.Enter)) StartGame(1);
                    else if (Hit(Keys.D2) || Hit(Keys.NumPad2)) StartGame(2);
                    break;
                case St.Intro:
                    stT++;
                    if (Hit(Keys.Escape)) state = St.Title;
                    if (stT > 110) state = St.Play;
                    break;
                case St.Play:
                    if (Hit(Keys.Escape)) { state = St.Title; break; }
                    if (Hit(Keys.P)) paused = !paused;
                    if (!paused) UpdatePlay();
                    break;
                case St.GameOver:
                    stT++;
                    if (stT > 260 || Hit(Keys.Escape)) state = St.Title;
                    break;
                case St.Ending:
                    stT++;
                    if ((stT > 120 && Hit(Keys.Enter)) || Hit(Keys.Escape)) state = St.Title;
                    break;
            }
            pkb = kb;
            base.Update(gt);
        }

        Player Nearest(float x, float y)
        {
            Player best = null; float bd = 1e9f;
            foreach (var p in players)
            {
                if (!p.active || p.outGame || p.dying) continue;
                float d = Math.Abs(p.x - x) + Math.Abs(p.y - y);
                if (d < bd) { bd = d; best = p; }
            }
            return best;
        }

        void UpdatePlay()
        {
            levelT++;
            for (int i = 0; i < 2; i++)
                if (players[i].active && !players[i].outGame) UpdatePlayer(players[i], ins[i]);
            UpdateEnemies();
            UpdateBubbles();
            UpdateHazards();
            UpdateDeads();
            UpdateItems();

            // player / enemy contact
            foreach (var p in players)
            {
                if (!p.active || p.outGame || p.dying || p.inv > 0) continue;
                foreach (var e in enemies) if (Overlap(p, e)) { KillPlayer(p); break; }
                if (!p.dying && ghost != null && Overlap(p, ghost)) KillPlayer(p);
            }

            // hurry up / ghost
            if (levelT == 1500 && enemies.Count > 0) { foreach (var e in enemies) e.angry = true; hurryT = 130; }
            if (hurryT > 0) hurryT--;
            if (levelT == 2300 && enemies.Count > 0) ghost = new Actor { x = 112, y = -16 };
            if (ghost != null)
            {
                var t = Nearest(ghost.x, ghost.y);
                if (t != null)
                {
                    float sp = Math.Min(0.55f + (levelT - 2300) * 0.0003f, 1.3f);
                    float dx = t.x - ghost.x, dy = t.y - ghost.y, d = (float)Math.Sqrt(dx * dx + dy * dy);
                    if (d > 0.5f) { ghost.x += dx / d * sp; ghost.y += dy / d * sp; }
                }
            }

            // special bubbles
            if (enemies.Count > 0 && --specTimer <= 0) { SpawnSpecial(); specTimer = 500 + rng.Next(300); }

            // popups / sparks
            foreach (var p in popups) { p.y -= 0.35f; p.life--; }
            popups.RemoveAll(p => p.life <= 0);
            foreach (var s in sparks) { s.x += s.vx; s.y += s.vy; s.life--; }
            sparks.RemoveAll(s => s.life <= 0);

            // level complete?
            if (enemies.Count == 0 && deads.Count == 0 && !bubbles.Exists(b => b.trapped))
            {
                ghost = null;
                if (++clearT >= 200)
                {
                    if (level >= 100) { state = St.Ending; stT = 0; }
                    else { level++; StartLevel(); }
                }
            }
            else clearT = 0;

            // game over?
            bool anyIn = false;
            foreach (var p in players) if (p.active && !p.outGame) anyIn = true;
            if (!anyIn) { state = St.GameOver; stT = 0; }
        }

        void UpdatePlayer(Player p, Inp inp)
        {
            if (p.dying)
            {
                p.deadT++; p.vy = Math.Min(p.vy + 0.18f, 4f); p.y += p.vy;
                if (p.deadT > 110)
                {
                    if (p.lives > 0) { p.lives--; Respawn(p, 150); }
                    else p.outGame = true;
                }
                return;
            }
            if (p.inv > 0) p.inv--;
            if (p.shootCd > 0) p.shootCd--;
            if (p.shootAnim > 0) p.shootAnim--;
            float spd = p.shoes ? 1.75f : 1.25f, mv = 0;
            if (inp.L && !inp.R) { mv = -spd; p.dir = -1; }
            else if (inp.R && !inp.L) { mv = spd; p.dir = 1; }
            p.vx = mv;
            if (mv != 0) { MoveX(p, mv); p.walked += spd; p.anim += 0.2f; }
            if (inp.Jp && p.ground) { p.vy = -4.6f; p.ground = false; p.jumps++; }
            p.vy = Math.Min(p.vy + 0.2f, 4f);
            MoveY(p);

            bool want = p.rapid ? inp.F : inp.Fp;
            if (want && p.shootCd <= 0)
            {
                Shoot(p);
                p.shootCd = p.rapid ? 7 : 12; p.shootAnim = 10; p.shots++;
            }

            // power-ups appear after enough activity (internal counters)
            if (!p.gY && p.jumps >= 45) { p.gY = true; SpawnItem(1); }
            if (!p.gB && p.shots >= 35) { p.gB = true; SpawnItem(2); }
            if (!p.gS && p.walked >= 1500) { p.gS = true; SpawnItem(3); }

            // touch bubbles
            foreach (var b in bubbles)
            {
                if (b.dead || b.popDelay > 0) continue;
                if (p.x + 2 < b.x + 14 && p.x + 14 > b.x + 2 && p.y + 2 < b.y + 14 && p.y + 16 > b.y + 2)
                {
                    if (p.vy > 0 && inp.J && p.y + 16 - b.y < 11)
                    { p.y = b.y - 14; p.vy = -4.1f; p.ground = false; }   // ride the bubble
                    else Pop(b, p, 0);
                }
            }

            // collect items
            foreach (var it in items)
            {
                if (it.dead || !Overlap(p, it)) continue;
                it.dead = true;
                if (it.kind == 0) { AddScore(p, it.value); AddPopup(it.value.ToString(), it.x + 8, it.y, Color.White); }
                else
                {
                    AddScore(p, 500);
                    if (it.kind == 1) { p.rapid = true; AddPopup("RAPID FIRE", it.x + 8, it.y, Color.Yellow); }
                    if (it.kind == 2) { p.fast = true; AddPopup("BUBBLE UP", it.x + 8, it.y, new Color(120, 180, 255)); }
                    if (it.kind == 3) { p.shoes = true; AddPopup("SPEED UP", it.x + 8, it.y, new Color(255, 100, 100)); }
                }
            }
            items.RemoveAll(i => i.dead);
        }

        void Shoot(Player p)
        {
            float bx = p.x + p.dir * 14, by = p.y;
            if (WallAt(bx + 8, by + 8)) { AddScore(p, 10); AddSparks(bx + 8, by + 8, Color.White); return; }
            bubbles.Add(new Bubble
            {
                x = bx, y = by, vx = p.dir * (p.fast ? 3.2f : 2.4f), phase = p.fast ? 18 : 15,
                life = 600, owner = p.id, fast = p.fast, seed = rng.Next(100)
            });
        }

        void SpawnItem(int kind)
        {
            items.Add(new Item { x = rng.Next(40, 200), y = 36, kind = kind, life = 900, nowrap = true });
        }

        void SpawnSpecial()
        {
            var b = new Bubble { x = rng.Next(100, 140), y = 206, life = 1300, seed = rng.Next(100) };
            if (rng.NextDouble() < 0.5)
            {
                b.special = 3;
                var need = new List<int>();
                var p = players[rng.Next(2)];
                if (!p.active) p = players[0];
                for (int i = 0; i < 6; i++) if (!p.letters[i]) need.Add(i);
                b.letter = need.Count > 0 ? need[rng.Next(need.Count)] : rng.Next(6);
            }
            else b.special = rng.Next(3);
            bubbles.Add(b);
        }

        float EnemySpeed(Enemy e)
        {
            float b = 0.55f + Math.Min(level, 80) * 0.007f;
            return e.angry ? b * 1.9f + 0.25f : b;
        }

        void UpdateEnemies()
        {
            foreach (var e in enemies)
            {
                e.anim++;
                if (e.jumpCd > 0) e.jumpCd--;
                if (e.turnCd > 0) e.turnCd--;
                if (e.ground)
                {
                    var t = Nearest(e.x, e.y);
                    if (t != null && e.angry && rng.NextDouble() < 0.04) e.dir = t.x > e.x ? 1 : -1;
                    bool up = t != null && t.y < e.y - 8;
                    double chance = up ? (e.angry ? 0.03 : 0.015) : 0.003;
                    if (e.jumpCd == 0 && rng.NextDouble() < chance) { e.vy = -4.6f; e.jumpCd = 50; e.ground = false; }
                    if (e.turnCd == 0)
                    {
                        int tx = (int)Math.Floor((e.x + 8 + e.dir * 9) / 8), ty = (int)Math.Floor((e.y + 17) / 8);
                        int tl = Tile(tx, ty);
                        if (tl != 1 && tl != 2) { if (rng.NextDouble() < 0.5) e.dir = -e.dir; e.turnCd = 30; }
                    }
                }
                if (MoveX(e, e.dir * EnemySpeed(e))) e.dir = -e.dir;
                e.vy = Math.Min(e.vy + 0.2f, 4f);
                MoveY(e);
            }
        }

        void KillEnemy(Enemy e, Player p, int score, int dirHint)
        {
            AddScore(p, score);
            AddPopup(score.ToString(), e.x + 8, e.y, new Color(255, 240, 120));
            e.vy = -3.5f; e.vx = dirHint * 1.2f; e.ground = false; e.rot = 0;
            deads.Add(e);
        }

        void Pop(Bubble b, Player p, int chain)
        {
            if (b.dead) return;
            b.dead = true;
            AddSparks(b.x + 8, b.y + 8, Color.White);
            if (b.special >= 0) { Special(b, p); return; }
            int chain2 = chain;
            if (b.trapped)
            {
                int sc = 1000 << Math.Min(chain, 6);
                var e = new Enemy { x = b.x, y = b.y, variant = b.tv, angry = b.tangry };
                KillEnemy(e, p, sc, rng.Next(2) == 0 ? -1 : 1);
                chain2 = chain + 1;
            }
            else AddScore(p, 10);
            foreach (var o in bubbles)
                if (!o.dead && o != b && o.special < 0 && o.popDelay == 0 && o.phase == 0)
                {
                    float dx = o.x - b.x, dy = o.y - b.y;
                    if (dx * dx + dy * dy < 22 * 22) { o.popDelay = 5; o.chain = chain2; o.popBy = p; }
                }
        }

        void Special(Bubble b, Player p)
        {
            int owner = p != null ? p.id : 0;
            switch (b.special)
            {
                case 0:
                    for (int d = -1; d <= 1; d += 2)
                        hazards.Add(new Hazard { kind = 0, x = b.x, y = b.y, dir = d, owner = owner, life = 600, nowrap = true });
                    break;
                case 1:
                    hazards.Add(new Hazard { kind = 1, x = b.x, y = b.y, dir = p != null ? p.dir : 1, owner = owner, life = 700, nowrap = true });
                    break;
                case 2:
                    {
                        int d = (p != null && p.x + 8 < b.x + 8) ? 1 : -1;
                        hazards.Add(new Hazard { kind = 2, x = b.x, y = b.y, dir = d, owner = owner, life = 200, nowrap = true });
                    }
                    break;
                case 3:
                    if (p == null) break;
                    char c = EXT[b.letter];
                    for (int i = 0; i < 6; i++) if (!p.letters[i] && EXT[i] == c) { p.letters[i] = true; break; }
                    AddScore(p, 500);
                    AddPopup(c.ToString(), b.x + 8, b.y, new Color(255, 220, 80));
                    bool all = true;
                    foreach (bool l in p.letters) if (!l) all = false;
                    if (all)
                    {
                        p.lives++; Array.Clear(p.letters, 0, 6);
                        AddPopup("EXTEND! 1UP", b.x + 8, b.y - 10, new Color(255, 120, 255));
                    }
                    break;
            }
        }

        void UpdateBubbles()
        {
            foreach (var b in bubbles)
            {
                if (b.dead) continue;
                b.age++;
                if (b.popDelay > 0) { if (--b.popDelay == 0) { b.dead = false; Pop(b, b.popBy, b.chain); } continue; }
                if (b.phase > 0)
                {
                    b.phase--;
                    float nx = b.x + b.vx;
                    if (WallAt(nx + 8, b.y + 8)) b.phase = 0; else b.x = nx;
                    if (b.phase == 0) b.vx = 0;
                    if (b.special < 0 && !b.trapped)
                        for (int i = 0; i < enemies.Count; i++)
                        {
                            var e = enemies[i];
                            if (e.x + 2 < b.x + 14 && e.x + 14 > b.x + 2 && e.y + 2 < b.y + 14 && e.y + 16 > b.y + 2)
                            {
                                b.trapped = true; b.tv = e.variant; b.tangry = e.angry;
                                b.phase = 0; b.vx = 0; b.life = 430;
                                enemies.RemoveAt(i);
                                break;
                            }
                        }
                }
                else
                {
                    b.y += b.fast && b.special < 0 ? -0.75f : -0.5f;
                    b.x += (float)Math.Sin((b.age + b.seed) * 0.06f) * 0.35f;
                    if (b.y < 34) b.y = 34;
                    b.x = MathHelper.Clamp(b.x, 16, 224);
                }
                if (--b.life <= 0)
                {
                    b.dead = true;
                    AddSparks(b.x + 8, b.y + 8, Color.White);
                    if (b.trapped)
                        enemies.Add(new Enemy { x = b.x, y = b.y, variant = b.tv, angry = true, dir = rng.Next(2) == 0 ? -1 : 1, jumpCd = 30 });
                }
            }
            bubbles.RemoveAll(b => b.dead);
        }

        void UpdateHazards()
        {
            foreach (var h in hazards)
            {
                h.anim++;
                if (--h.life <= 0) h.dead = true;
                if (h.kind == 2) { h.x += h.dir * 4.5f; if (h.x < 8 || h.x > 232) h.dead = true; }
                else
                {
                    float sp = h.kind == 0 ? 1.5f : 1.1f;
                    if (MoveX(h, h.dir * sp)) { if (h.kind == 0) h.dead = true; else h.dir = -h.dir; }
                    h.vy = Math.Min(h.vy + 0.25f, 4.5f);
                    MoveY(h);
                }
                if (h.dead) continue;
                for (int i = enemies.Count - 1; i >= 0; i--)
                {
                    var e = enemies[i];
                    if (Overlap(h, e)) { enemies.RemoveAt(i); KillEnemy(e, players[h.owner], 1000, h.dir); }
                }
            }
            hazards.RemoveAll(h => h.dead);
        }

        void UpdateDeads()
        {
            foreach (var d in deads)
            {
                d.rot += 0.35f;
                if (MoveX(d, d.vx)) d.vx = -d.vx;
                d.vy = Math.Min(d.vy + 0.2f, 4f);
                MoveY(d);
                if (d.ground)
                {
                    d.dead = true;
                    items.Add(new Item { x = d.x, y = d.y, kind = 0, sub = rng.Next(4), value = 700, life = 720, nowrap = true });
                }
            }
            deads.RemoveAll(d => d.dead);
        }

        void UpdateItems()
        {
            foreach (var it in items)
            {
                it.vy = Math.Min(it.vy + 0.2f, 4f);
                MoveY(it);
                if (--it.life <= 0) it.dead = true;
            }
            items.RemoveAll(i => i.dead);
        }

        // ------------------------------------------------------------------ drawing
        void DrawText(string s, float x, float y, Color c, int scale = 1)
        {
            s = s.ToUpperInvariant();
            int ix = (int)Math.Round(x), iy = (int)Math.Round(y);
            for (int i = 0; i < s.Length; i++)
            {
                int idx = s[i] - 32;
                if (idx <= 0 || idx >= 96) continue;
                var src = new Rectangle((idx % 16) * 8, (idx / 16) * 8, 8, 8);
                sb.Draw(fontTex, new Rectangle(ix + i * 8 * scale, iy, 8 * scale, 8 * scale), src, c);
            }
        }
        void DrawCentered(string s, float y, Color c, int scale = 1) { DrawText(s, (W - s.Length * 8 * scale) / 2f, y, c, scale); }

        void Spr(Texture2D t, float x, float y, bool flip, Color c, float rot = 0, float scale = 1)
        {
            sb.Draw(t, new Vector2((float)Math.Round(x) + 8, (float)Math.Round(y) + 8), null, c, rot, new Vector2(8, 8), scale,
                flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
        }

        protected override void Draw(GameTime gt)
        {
            GraphicsDevice.SetRenderTarget(rt);
            GraphicsDevice.Clear(Color.Black);
            sb.Begin(samplerState: SamplerState.PointClamp);
            switch (state)
            {
                case St.Title: DrawTitle(); break;
                case St.Ending: DrawEnding(); break;
                default:
                    DrawScene(); DrawHud();
                    if (state == St.Intro)
                    {
                        DrawCentered("ROUND " + level, 92, Color.White, 2);
                        DrawCentered("READY!", 116, new Color(255, 230, 80), 2);
                    }
                    if (state == St.GameOver) DrawCentered("GAME OVER", 100, new Color(255, 80, 80), 2);
                    if (paused) DrawCentered("PAUSED", 100, Color.White, 2);
                    if (hurryT > 0 && (hurryT / 8) % 2 == 0) DrawCentered("HURRY UP!", 100, new Color(255, 60, 60), 2);
                    break;
            }
            sb.End();

            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.Black);
            int bw = GraphicsDevice.PresentationParameters.BackBufferWidth, bh = GraphicsDevice.PresentationParameters.BackBufferHeight;
            float s = Math.Min(bw / (float)W, bh / (float)H);
            if (s >= 1) s = (float)Math.Floor(s);
            int dw = (int)(W * s), dh = (int)(H * s);
            sb.Begin(samplerState: SamplerState.PointClamp);
            sb.Draw(rt, new Rectangle((bw - dw) / 2, (bh - dh) / 2, dw, dh), Color.White);
            sb.End();
            base.Draw(gt);
        }

        void DrawScene()
        {
            Color wc = WallCols[((level - 1) / 3) % WallCols.Length], wd = Dim(wc, .8f);
            for (int x = 0; x < 32; x++)
                for (int y = 0; y < 28; y++)
                {
                    int t = tiles[x, y];
                    if (t != 0) sb.Draw(tileTex, new Vector2(x * 8, y * 8), t == 3 ? wd : wc);
                }

            foreach (var b in bubbles) DrawBubble(b);

            foreach (var it in items)
            {
                if (it.life < 120 && (it.life / 6) % 2 == 0) continue;
                Texture2D t = it.kind == 0 ? fruitTex[it.sub] : candyTex[it.kind - 1];
                Spr(t, it.x, it.y + (float)Math.Sin(levelT * 0.15f) * (it.kind == 0 ? 0 : 1.5f), false, Color.White);
            }

            foreach (var e in enemies)
                Spr(enTex[e.variant, e.angry ? 1 : 0, (e.anim / 10) % 2], e.x, e.y, false, Color.White);

            foreach (var h in hazards) DrawHazard(h);

            foreach (var d in deads)
                Spr(enTex[d.variant, d.angry ? 1 : 0, 0], d.x, d.y, false, Color.White, d.rot);

            if (ghost != null)
                Spr(ghostTex, ghost.x, ghost.y + (float)Math.Sin(levelT * 0.1f) * 2, ghost.x > 0 && Nearest(ghost.x, ghost.y) is Player gp && gp.x < ghost.x, new Color(230, 240, 255) * 0.85f);

            foreach (var p in players)
            {
                if (!p.active || p.outGame) continue;
                if (p.dying)
                {
                    Spr(plTex[p.id, 1], p.x, p.y, p.dir < 0, Color.White, p.deadT * 0.35f);
                    continue;
                }
                if (p.inv > 0 && (p.inv / 4) % 2 == 0 && state != St.Intro) continue;
                int fr = p.shootAnim > 0 ? 2 : (!p.ground ? 1 : (p.vx != 0 ? ((int)p.anim) % 2 : 0));
                Spr(plTex[p.id, fr], p.x, p.y, p.dir < 0, Color.White);
            }

            foreach (var pu in popups) DrawText(pu.t, pu.x - pu.t.Length * 4, pu.y, pu.c);
            foreach (var s in sparks) sb.Draw(px, new Rectangle((int)s.x, (int)s.y, 2, 2), s.c);
        }

        void DrawBubble(Bubble b)
        {
            Color tint;
            float bx = b.x, by = b.y;
            if (b.special >= 0)
            {
                Color[] sc = { new Color(255, 150, 120), new Color(120, 180, 255), new Color(255, 240, 120), new Color(230, 150, 255) };
                tint = sc[b.special];
                Spr(bubbleTex, bx, by, false, tint);
                if (b.special == 0) Spr(fireTex, bx, by, false, Color.White);
                else if (b.special == 1) Spr(dropTex, bx, by, false, Color.White);
                else if (b.special == 2) Spr(boltTex, bx, by, false, Color.White);
                else DrawText(EXT[b.letter].ToString(), (int)bx + 5, (int)by + 5, Color.White);
                return;
            }
            if (b.trapped)
            {
                Spr(enTex[b.tv, b.tangry ? 1 : 0, (b.age / 12) % 2], bx, by, false, Color.White, 0, 0.72f);
                tint = (b.life < 110 && (b.life / 5) % 2 == 0) ? new Color(255, 100, 100) : new Color(190, 255, 200);
            }
            else tint = b.owner == 0 ? new Color(150, 255, 170) : new Color(150, 190, 255);
            Spr(bubbleTex, bx, by, false, tint);
        }

        void DrawHazard(Hazard h)
        {
            if (h.kind == 0) Spr(fireTex, h.x, h.y + 2, (h.anim / 4) % 2 == 0, Color.White);
            else if (h.kind == 1)
            {
                sb.Draw(px, new Rectangle((int)h.x + 1, (int)h.y + 5, 14, 11), new Color(50, 130, 255));
                sb.Draw(px, new Rectangle((int)h.x + 1 + ((h.anim / 5) % 2) * 2, (int)h.y + 3, 10, 3), new Color(170, 220, 255));
                sb.Draw(px, new Rectangle((int)h.x + 3, (int)h.y + 9, 4, 2), Color.White);
            }
            else
            {
                for (int k = 0; k < 7; k++)
                {
                    int yy = (int)h.y + 7 + (((k + h.anim / 2) % 2 == 0) ? -3 : 3);
                    sb.Draw(px, new Rectangle((int)(h.x - h.dir * k * 4), yy, 4, 3), new Color(255, 240, 60));
                    sb.Draw(px, new Rectangle((int)(h.x - h.dir * k * 4), yy + 1, 4, 1), Color.White);
                }
            }
        }

        void DrawHud()
        {
            DrawText("1UP", 24, 0, new Color(120, 255, 120));
            string s0 = players[0].score.ToString();
            DrawText(s0, 80 - s0.Length * 8, 8, Color.White);
            DrawText("HIGH SCORE", 88, 0, new Color(255, 80, 80));
            string h = hi.ToString();
            DrawText(h, 128 - h.Length * 4, 8, Color.White);
            if (players[1].active)
            {
                DrawText("2UP", 200, 0, new Color(120, 180, 255));
                string s1 = players[1].score.ToString();
                DrawText(s1, 232 - s1.Length * 8, 8, Color.White);
            }
            string lv = level.ToString();
            DrawText(lv, 0, 20, Color.White);

            for (int i = 0; i < 2; i++)
            {
                var p = players[i];
                if (!p.active) continue;
                int lx0 = i == 0 ? 18 : 224, step = i == 0 ? 9 : -9, ex0 = i == 0 ? 48 : 160;
                for (int k = 0; k < Math.Min(p.lives, 4); k++)
                    sb.Draw(plTex[i, 0], new Rectangle(lx0 + k * step, 211, 9, 9), Color.White);
                for (int k = 0; k < 6; k++)
                    DrawText(EXT[k].ToString(), ex0 + k * 8, 212, p.letters[k] ? new Color(255, 230, 60) : new Color(70, 40, 90));
            }
        }

        void DrawTitle()
        {
            Color[] rb = { new Color(255, 80, 80), new Color(255, 170, 40), new Color(255, 240, 60), new Color(90, 230, 90), new Color(80, 170, 255), new Color(200, 120, 255) };
            for (int i = 0; i < 14; i++)
            {
                float bx = 16 + (i * 37) % 216, by = H - ((stT * 0.5f + i * 29) % 260) + 20;
                sb.Draw(bubbleTex, new Vector2((int)bx, (int)by), rb[i % 6] * 0.9f);
            }
            string a = "BUBBLE", b = "BOBBLE";
            for (int i = 0; i < a.Length; i++)
                DrawText(a[i].ToString(), 32 + i * 32, 30 + (int)(Math.Sin(stT * 0.08 + i) * 3), rb[i % 6], 4);
            for (int i = 0; i < b.Length; i++)
                DrawText(b[i].ToString(), 32 + i * 32, 66 + (int)(Math.Sin(stT * 0.08 + i + 2) * 3), rb[(i + 3) % 6], 4);

            DrawCentered("HIGH SCORE " + hi, 112, Color.White);
            if ((stT / 30) % 2 == 0) DrawCentered("PRESS 1 OR 2", 128, new Color(255, 240, 60), 2);
            DrawCentered("1 = ONE PLAYER", 148, new Color(120, 255, 120));
            DrawCentered("2 = TWO PLAYERS", 158, new Color(120, 180, 255));
            DrawText("P1: ARROWS  Z JUMP  X BUBBLE", 8, 176, new Color(200, 200, 200));
            DrawText("P2: A D  W JUMP  S BUBBLE", 8, 186, new Color(200, 200, 200));
            DrawText("F11 FULLSCREEN  P PAUSE  ESC QUIT", 8, 200, new Color(150, 150, 150));

            int f = (stT / 12) % 2;
            Spr(plTex[0, f], 24, 100, false, Color.White, 0, 2f);
            Spr(plTex[1, f], 216, 100, true, Color.White, 0, 2f);
            Spr(enTex[0, 0, f], 216, 140, false, Color.White, 0, 1f);
            Spr(enTex[1, 0, f], 24, 140, false, Color.White, 0, 1f);
        }

        void DrawEnding()
        {
            DrawCentered("CONGRATULATIONS!", 50, new Color(255, 240, 60), 2);
            DrawCentered("YOU CLEARED ALL 100 ROUNDS", 90, Color.White);
            DrawCentered("AND POPPED EVERY LAST BUBBLE!", 104, Color.White);
            DrawCentered("SCORE " + Math.Max(players[0].score, players[1].score), 130, new Color(120, 255, 120));
            Spr(plTex[0, (stT / 12) % 2], 88, 160, false, Color.White, 0, 2f);
            Spr(plTex[1, (stT / 12) % 2], 136, 160, true, Color.White, 0, 2f);
            if (stT > 120 && (stT / 30) % 2 == 0) DrawCentered("PRESS ENTER", 206, Color.White);
        }
    }
}
