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
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace AnteaterClone
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            using (var game = new AnteaterGame()) game.Run();
        }
    }

    enum GameState { Title, Playing, Dying, LevelClear, GameOver }

    // ---------------------------------------------------------------- entities
    class Walker
    {
        public Point From, Cell, Dir = new Point(1, 0);
        public float T, Step = 0.12f;
        public static Vector2 Center(Point p) { return new Vector2(p.X * 8 + 4, p.Y * 8 + 4); }
        public Vector2 Pos { get { return Vector2.Lerp(Center(From), Center(Cell), MathHelper.Clamp(T, 0f, 1f)); } }
    }
    class Ant : Walker { public float Anim; }
    class Worm : Walker { public List<Point> Trail = new List<Point>(); }
    class Queen { public Point Cell; public bool Alive = true; public float Respawn; }
    class Popup { public Vector2 Pos; public string Text; public float Life = 0.9f; }
    class Spider { public float X; public bool Descending; public float Idx; }

    class Palette
    {
        public Color Base, Spk1, Spk2, Edge, Larva1, Larva2, Sun1, Sun2;
        public Palette(Color b, Color s1, Color s2, Color e, Color l1, Color l2, Color su1, Color su2)
        { Base = b; Spk1 = s1; Spk2 = s2; Edge = e; Larva1 = l1; Larva2 = l2; Sun1 = su1; Sun2 = su2; }
    }

    // -------------------------------------------------------------------- game
    public class AnteaterGame : Game
    {
        const int W = 224, H = 288, COLS = 28, ROWS = 36;
        const float TongueSpeed = 120f;
        static readonly int[] TunnelRows = { 9, 12, 15, 18, 21, 24, 27, 30 };
        static readonly Point[] Dirs = { new Point(1, 0), new Point(-1, 0), new Point(0, 1), new Point(0, -1) };

        static readonly Palette[] Palettes =
        {
            new Palette(new Color(222,120,30), new Color(255,200,70), new Color(170,70,20), new Color(255,220,90),
                        new Color(255,110,190), new Color(255,255,120), new Color(255,90,255), new Color(170,60,220)),
            new Palette(new Color(200,40,40), new Color(255,110,70), new Color(130,20,30), new Color(250,230,80),
                        new Color(120,255,120), new Color(255,255,255), new Color(90,255,90), new Color(30,160,40)),
            new Palette(new Color(150,60,200), new Color(210,130,255), new Color(90,30,140), new Color(120,255,230),
                        new Color(255,220,80), new Color(255,255,255), new Color(80,240,255), new Color(30,140,200)),
            new Palette(new Color(40,90,200), new Color(100,170,255), new Color(20,50,130), new Color(255,200,60),
                        new Color(255,120,120), new Color(200,255,120), new Color(255,255,80), new Color(255,160,30)),
        };

        // ---- 5x7 bitmap font -------------------------------------------------
        const string FontChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-.:!/";
        static readonly string[] FontData =
        {
            "01110/10001/10011/10101/11001/10001/01110", // 0
            "00100/01100/00100/00100/00100/00100/01110", // 1
            "01110/10001/00001/00010/00100/01000/11111", // 2
            "11110/00001/00001/01110/00001/00001/11110", // 3
            "00010/00110/01010/10010/11111/00010/00010", // 4
            "11111/10000/11110/00001/00001/10001/01110", // 5
            "00110/01000/10000/11110/10001/10001/01110", // 6
            "11111/00001/00010/00100/01000/01000/01000", // 7
            "01110/10001/10001/01110/10001/10001/01110", // 8
            "01110/10001/10001/01111/00001/00010/01100", // 9
            "01110/10001/10001/11111/10001/10001/10001", // A
            "11110/10001/10001/11110/10001/10001/11110", // B
            "01110/10001/10000/10000/10000/10001/01110", // C
            "11110/10001/10001/10001/10001/10001/11110", // D
            "11111/10000/10000/11110/10000/10000/11111", // E
            "11111/10000/10000/11110/10000/10000/10000", // F
            "01110/10001/10000/10111/10001/10001/01111", // G
            "10001/10001/10001/11111/10001/10001/10001", // H
            "01110/00100/00100/00100/00100/00100/01110", // I
            "00111/00010/00010/00010/00010/10010/01100", // J
            "10001/10010/10100/11000/10100/10010/10001", // K
            "10000/10000/10000/10000/10000/10000/11111", // L
            "10001/11011/10101/10101/10001/10001/10001", // M
            "10001/11001/10101/10011/10001/10001/10001", // N
            "01110/10001/10001/10001/10001/10001/01110", // O
            "11110/10001/10001/11110/10000/10000/10000", // P
            "01110/10001/10001/10001/10101/10010/01101", // Q
            "11110/10001/10001/11110/10100/10010/10001", // R
            "01111/10000/10000/01110/00001/00001/11110", // S
            "11111/00100/00100/00100/00100/00100/00100", // T
            "10001/10001/10001/10001/10001/10001/01110", // U
            "10001/10001/10001/10001/10001/01010/00100", // V
            "10001/10001/10001/10101/10101/11011/10001", // W
            "10001/10001/01010/00100/01010/10001/10001", // X
            "10001/10001/01010/00100/00100/00100/00100", // Y
            "11111/00001/00010/00100/01000/10000/11111", // Z
            "00000/00000/00000/11111/00000/00000/00000", // -
            "00000/00000/00000/00000/00000/01100/01100", // .
            "00000/01100/01100/00000/01100/01100/00000", // :
            "00100/00100/00100/00100/00100/00000/00100", // !
            "00001/00010/00010/00100/01000/01000/10000", // /
        };

        // ---- sprites (hand drawn, '.' = transparent) -------------------------
        static readonly string[] AntRows0 =
        {
            "........",
            "......w.",
            ".ww.www.",
            "wwwwwwww",
            ".ww.www.",
            "w.w.w.w.",
            "........",
            "........",
        };
        static readonly string[] AntRows1 =
        {
            "........",
            "......w.",
            ".ww.www.",
            "wwwwwwww",
            ".ww.www.",
            ".w.w.w.w",
            "........",
            "........",
        };
        static readonly string[] QueenRows0 =
        {
            "......y.",
            "..ggggg.",
            ".gGGGggg",
            "ggGGGggg",
            ".gGGGggg",
            "..ggggg.",
            ".g.g.g.g",
            "........",
        };
        static readonly string[] QueenRows1 =
        {
            "......y.",
            "..ggggg.",
            ".gGGGggg",
            "ggGGGggg",
            ".gGGGggg",
            "..ggggg.",
            "g.g.g.g.",
            "........",
        };
        static readonly string[] SpiderRows =
        {
            "s..ss..s",
            ".s.ss.s.",
            "..ssss..",
            "ssrssrss",
            "..ssss..",
            ".s.ss.s.",
            "s..ss..s",
            "........",
        };
        static readonly string[] SunRows =
        {
            ".....a.....",
            ".a...a...a.",
            "..a.bbb.a..",
            "...bbbbb...",
            "..bbbbbbb..",
            "aabbbbbbbaa",
            "..bbbbbbb..",
            "...bbbbb...",
            "..a.bbb.a..",
            ".a...a...a.",
            ".....a.....",
        };

        // ---- state -----------------------------------------------------------
        GraphicsDeviceManager gfx;
        SpriteBatch sb;
        RenderTarget2D rt;
        Texture2D pix, fontTex, antTex0, antTex1, queenTex0, queenTex1, spiderTex, anteaterTex, sunTex, bgTex;
        Random rng = new Random();
        KeyboardState kb, prevKb;
        Point lastDir = Point.Zero;

        GameState state = GameState.Title;
        float stateTimer, anim, dayTime, dayLen = 55f, antSpawn, wormSpawn, flash;
        bool paused;
        int score, hi, lives, level = 1, nextExtra = 10000;

        bool[,] tunnel = new bool[COLS, ROWS];
        bool[,] larva = new bool[COLS, ROWS];
        int larvaLeft;
        Palette pal = Palettes[0];

        float antX = 116f;
        bool facingRight = true;
        List<Point> path = new List<Point>();
        Vector2 tipVis;
        Point tipPrev = new Point(-9, -9);

        List<Ant> ants = new List<Ant>();
        List<Worm> worms = new List<Worm>();
        List<Queen> queens = new List<Queen>();
        List<Popup> popups = new List<Popup>();
        Spider spider;
        int shaftX = 14;   // column of the single hole in the ground the tongue can enter through

        Vector2 Start { get { return new Vector2(antX, 46f); } }
        bool TongueBusy { get { return path.Count > 0 || Vector2.Distance(tipVis, Start) > 0.5f; } }

        // ================================================================ setup
        public AnteaterGame()
        {
            gfx = new GraphicsDeviceManager(this);
            gfx.HardwareModeSwitch = false;   // borderless fullscreen at desktop resolution
            IsMouseVisible = false;
            Window.AllowUserResizing = true;
            Window.Title = "Anteater - MonoGame clone";
            int dh = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            int sc = Math.Max(1, Math.Min(4, (dh - 120) / H));
            gfx.PreferredBackBufferWidth = W * sc;
            gfx.PreferredBackBufferHeight = H * sc;
        }

        protected override void LoadContent()
        {
            sb = new SpriteBatch(GraphicsDevice);
            rt = new RenderTarget2D(GraphicsDevice, W, H);
            pix = new Texture2D(GraphicsDevice, 1, 1);
            pix.SetData(new[] { Color.White });
            BuildFont();
            BuildSprites();
            tipVis = Start;
        }

        void BuildFont()
        {
            var data = new Color[128 * 32];
            for (int i = 0; i < FontData.Length; i++)
            {
                string[] rows = FontData[i].Split('/');
                int ox = (i % 16) * 8, oy = (i / 16) * 8;
                for (int y = 0; y < 7; y++)
                    for (int x = 0; x < 5; x++)
                        if (rows[y][x] == '1') data[(oy + y) * 128 + ox + x] = Color.White;
            }
            fontTex = new Texture2D(GraphicsDevice, 128, 32);
            fontTex.SetData(data);
        }

        static string R(params (char c, int n)[] segs)
        {
            var s = new StringBuilder();
            foreach (var seg in segs) s.Append(seg.c, seg.n);
            return s.ToString();
        }

        Texture2D MakeTex(string[] rows, Dictionary<char, Color> pal2)
        {
            int h = rows.Length, w = 0;
            foreach (var r in rows) w = Math.Max(w, r.Length);
            var data = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    char c = x < rows[y].Length ? rows[y][x] : '.';
                    Color col;
                    if (pal2.TryGetValue(c, out col)) data[y * w + x] = col;
                }
            var t = new Texture2D(GraphicsDevice, w, h);
            t.SetData(data);
            return t;
        }

        void BuildSprites()
        {
            var antPal = new Dictionary<char, Color> { { 'w', new Color(235, 235, 235) } };
            antTex0 = MakeTex(AntRows0, antPal);
            antTex1 = MakeTex(AntRows1, antPal);

            var qPal = new Dictionary<char, Color>
            { { 'g', new Color(70, 225, 70) }, { 'G', new Color(20, 140, 40) }, { 'y', new Color(255, 240, 80) } };
            queenTex0 = MakeTex(QueenRows0, qPal);
            queenTex1 = MakeTex(QueenRows1, qPal);

            spiderTex = MakeTex(SpiderRows, new Dictionary<char, Color>
            { { 's', new Color(215, 175, 255) }, { 'r', new Color(255, 50, 50) } });

            // Anteater, 40x12, snout points right. o=body d=dark k=black l=belly
            string[] a =
            {
                R(('.',6),('d',18)),
                R(('.',2),('d',4),('o',24)),
                R(('.',1),('d',5),('o',26)),
                R(('d',6),('o',25),('k',1),('o',3)),
                R(('.',1),('d',4),('o',34),('k',1)),
                R(('.',2),('d',3),('o',35)),
                R(('.',3),('d',2),('o',28)),
                R(('.',5),('l',22)),
                R(('.',6),('d',22)),
                R(('.',7),('d',3),('.',13),('d',3),('.',3),('d',3)),
                R(('.',7),('d',3),('.',13),('d',3),('.',3),('d',3)),
                R(('.',6),('d',5),('.',11),('d',5),('.',1),('d',5)),
            };
            anteaterTex = MakeTex(a, new Dictionary<char, Color>
            {
                { 'o', new Color(225, 120, 40) }, { 'd', new Color(140, 60, 20) },
                { 'k', new Color(15, 10, 10) }, { 'l', new Color(255, 190, 90) }
            });
        }

        void BuildSun()
        {
            if (sunTex != null) sunTex.Dispose();
            sunTex = MakeTex(SunRows, new Dictionary<char, Color> { { 'a', pal.Sun1 }, { 'b', pal.Sun2 } });
        }

        static int Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return h & 0x7fffffff;
            }
        }

        // ============================================================ level setup
        void StartGame()
        {
            score = 0; lives = 3; level = 1; nextExtra = 10000;
            NewLevel();
            state = GameState.Playing;
        }

        void NewLevel()
        {
            pal = Palettes[(level - 1) % Palettes.Length];
            dayLen = Math.Max(35f, 60f - 3f * (level - 1));
            GenerateMaze();
            var shaftCols = new List<int>();
            for (int x = 3; x < COLS - 3; x++) if (tunnel[x, 9]) shaftCols.Add(x);
            shaftX = shaftCols.Count > 0 ? shaftCols[rng.Next(shaftCols.Count)] : 14;
            larvaLeft = 0;
            Array.Clear(larva, 0, larva.Length);
            foreach (int r in TunnelRows)
                for (int x = 1; x < COLS - 1; x++)
                    if (tunnel[x, r]) { larva[x, r] = true; larvaLeft++; }
            BuildBackground();
            BuildSun();
            queens.Clear();
            for (int k = 0; k < 2; k++) { var q = new Queen(); queens.Add(q); PlaceQueen(q); }
            ResetRound();
        }

        void ResetRound()
        {
            path.Clear();
            ants.Clear();
            worms.Clear();
            popups.Clear();
            spider = null;
            dayTime = 0f;
            antX = 116f;
            tipVis = Start;
            tipPrev = new Point(-9, -9);
            antSpawn = 1.0f;
            wormSpawn = 6f;
        }

        void GenerateMaze()
        {
            for (int attempt = 0; attempt < 200; attempt++)
            {
                tunnel = new bool[COLS, ROWS];
                foreach (int r in TunnelRows)
                    for (int x = 0; x < COLS; x++) tunnel[x, r] = true;

                int wallsPer = 2 + Math.Min(level / 3, 2);
                foreach (int r in TunnelRows)
                {
                    int n = rng.Next(wallsPer, wallsPer + 2);
                    for (int k = 0; k < n; k++)
                    {
                        int len = rng.Next(2, 5);
                        int x0 = rng.Next(2, COLS - 2 - len);
                        for (int x = x0; x < x0 + len; x++) tunnel[x, r] = false;
                    }
                }
                for (int i = 0; i < TunnelRows.Length - 1; i++)
                {
                    int r = TunnelRows[i];
                    int want = rng.Next(3, 6);
                    var cols = new List<int>();
                    for (int tries = 0; tries < 80 && cols.Count < want; tries++)
                    {
                        int x = rng.Next(1, COLS - 1);
                        if (!tunnel[x, r] || !tunnel[x, r + 3]) continue;
                        bool ok = true;
                        foreach (int c in cols) if (Math.Abs(c - x) < 3) ok = false;
                        if (ok) cols.Add(x);
                    }
                    foreach (int x in cols) { tunnel[x, r + 1] = true; tunnel[x, r + 2] = true; }
                }
                if (AllReachable()) return;
            }
            // fallback: plain open rows with a few connectors
            tunnel = new bool[COLS, ROWS];
            foreach (int r in TunnelRows)
                for (int x = 0; x < COLS; x++) tunnel[x, r] = true;
            for (int i = 0; i < TunnelRows.Length - 1; i++)
            {
                int r = TunnelRows[i];
                int x1 = (i % 2 == 0) ? 4 : 22;
                int x2 = (i % 2 == 0) ? 20 : 8;
                tunnel[x1, r + 1] = tunnel[x1, r + 2] = true;
                tunnel[x2, r + 1] = tunnel[x2, r + 2] = true;
            }
        }

        bool AllReachable()
        {
            var seen = new bool[COLS, ROWS];
            var q = new Queue<Point>();
            int total = 0, cnt = 0;
            for (int x = 0; x < COLS; x++)
                for (int y = 0; y < ROWS; y++) if (tunnel[x, y]) total++;
            for (int x = 0; x < COLS; x++)
                if (tunnel[x, 9]) { seen[x, 9] = true; q.Enqueue(new Point(x, 9)); }
            while (q.Count > 0)
            {
                var p = q.Dequeue(); cnt++;
                foreach (var d in Dirs)
                {
                    int nx = p.X + d.X, ny = p.Y + d.Y;
                    if (nx >= 0 && nx < COLS && ny >= 0 && ny < ROWS && tunnel[nx, ny] && !seen[nx, ny])
                    { seen[nx, ny] = true; q.Enqueue(new Point(nx, ny)); }
                }
            }
            return cnt == total;
        }

        bool TunPx(int x, int y)
        {
            return x >= 0 && x < W && y >= 56 && y < 256 && tunnel[x >> 3, y >> 3];
        }

        void BuildBackground()
        {
            var px = new Color[W * H];
            int seed = level * 7919 + 17;
            Color gDark = new Color(0, 110, 20), gLite = new Color(60, 200, 40);
            for (int x = 0; x < W; x++)
            {
                for (int y = 53; y < 56; y++) px[y * W + x] = (y == 53) ? gLite : gDark;
                if ((x & 1) == 0)
                {
                    int h = Hash(x, 1, seed) % 4;
                    Color bc = (Hash(x, 2, seed) % 3 == 0) ? Color.White : gLite;
                    for (int k = 1; k <= h; k++) px[(53 - k) * W + x] = bc;
                }
            }
            for (int y = 56; y < 256; y++)
                for (int x = 0; x < W; x++)
                {
                    if (TunPx(x, y)) continue;
                    Color c;
                    if (TunPx(x - 1, y) || TunPx(x + 1, y) || TunPx(x, y - 1) || TunPx(x, y + 1))
                        c = ((x + y) & 1) == 0 ? pal.Edge : pal.Spk2;
                    else
                    {
                        int n = Hash(x >> 1, y, seed) % 100;
                        c = n < 10 ? pal.Spk1 : (n < 22 ? pal.Spk2 : pal.Base);
                    }
                    px[y * W + x] = c;
                }
            // the opening in the ground: a gap in the grass and a dug shaft through the dirt
            for (int y = 49; y < 72; y++)
                for (int x = shaftX * 8 + 1; x < shaftX * 8 + 7; x++)
                    px[y * W + x] = Color.Transparent;
            for (int y = 56; y < 72; y++)
            {
                px[y * W + shaftX * 8] = pal.Edge;
                px[y * W + shaftX * 8 + 7] = pal.Edge;
            }
            if (bgTex != null) bgTex.Dispose();
            bgTex = new Texture2D(GraphicsDevice, W, H);
            bgTex.SetData(px);
        }

        void PlaceQueen(Queen q)
        {
            for (int tries = 0; tries < 60; tries++)
            {
                int x = rng.Next(2, COLS - 2);
                if (!tunnel[x, 30]) continue;
                var c = new Point(x, 30);
                bool bad = path.Contains(c);
                foreach (var o in queens) if (o != q && o.Alive && Math.Abs(o.Cell.X - x) < 6) bad = true;
                if (bad) continue;
                q.Cell = c; q.Alive = true; return;
            }
            q.Cell = new Point(3, 30); q.Alive = true;
        }

        bool IsTunnel(Point p)
        {
            return p.X >= 0 && p.X < COLS && p.Y >= 0 && p.Y < ROWS && tunnel[p.X, p.Y];
        }

        // ================================================================= input
        bool Pressed(Keys k) { return kb.IsKeyDown(k) && prevKb.IsKeyUp(k); }

        bool HeldDir(Point d)
        {
            if (d.X > 0) return kb.IsKeyDown(Keys.Right) || kb.IsKeyDown(Keys.D);
            if (d.X < 0) return kb.IsKeyDown(Keys.Left) || kb.IsKeyDown(Keys.A);
            if (d.Y > 0) return kb.IsKeyDown(Keys.Down) || kb.IsKeyDown(Keys.S);
            if (d.Y < 0) return kb.IsKeyDown(Keys.Up) || kb.IsKeyDown(Keys.W);
            return false;
        }

        Point ReadDir()
        {
            if (Pressed(Keys.Left) || Pressed(Keys.A)) lastDir = new Point(-1, 0);
            else if (Pressed(Keys.Right) || Pressed(Keys.D)) lastDir = new Point(1, 0);
            else if (Pressed(Keys.Up) || Pressed(Keys.W)) lastDir = new Point(0, -1);
            else if (Pressed(Keys.Down) || Pressed(Keys.S)) lastDir = new Point(0, 1);
            if (HeldDir(lastDir)) return lastDir;
            foreach (var d in Dirs) if (HeldDir(d)) return d;
            return Point.Zero;
        }

        // ================================================================ update
        protected override void Update(GameTime gt)
        {
            float dt = Math.Min((float)gt.ElapsedGameTime.TotalSeconds, 0.05f);
            prevKb = kb;
            kb = Keyboard.GetState();
            if (Pressed(Keys.Escape)) Exit();
            if (Pressed(Keys.F11)) gfx.ToggleFullScreen();
            anim += dt;

            switch (state)
            {
                case GameState.Title:
                case GameState.GameOver:
                    if (Pressed(Keys.Enter) || Pressed(Keys.Space)) StartGame();
                    break;
                case GameState.Playing:
                    if (Pressed(Keys.P)) paused = !paused;
                    if (!paused) UpdatePlaying(dt);
                    break;
                case GameState.Dying:
                    stateTimer -= dt;
                    UpdatePopups(dt);
                    if (stateTimer <= 0f)
                    {
                        lives--;
                        if (lives <= 0) { hi = Math.Max(hi, score); ResetRound(); state = GameState.GameOver; }
                        else { ResetRound(); state = GameState.Playing; }
                    }
                    break;
                case GameState.LevelClear:
                    stateTimer -= dt;
                    UpdatePopups(dt);
                    if (stateTimer <= 0f) { level++; NewLevel(); state = GameState.Playing; }
                    break;
            }
            if (flash > 0f) flash -= dt;
            base.Update(gt);
        }

        void UpdatePopups(float dt)
        {
            for (int i = popups.Count - 1; i >= 0; i--)
            {
                popups[i].Life -= dt;
                popups[i].Pos.Y -= 12f * dt;
                if (popups[i].Life <= 0f) popups.RemoveAt(i);
            }
        }

        void UpdatePlaying(float dt)
        {
            dayTime += dt;
            Point dir = ReadDir();

            if (!TongueBusy)
            {
                if (dir.X != 0)
                {
                    facingRight = dir.X > 0;
                    antX = MathHelper.Clamp(antX + dir.X * 60f * dt, 20f, 204f);
                }
                tipVis = Start;
            }
            UpdateTongue(dt, dir);
            UpdateEnemies(dt);
            UpdateSpider(dt);
            UpdateSpawns(dt);
            foreach (var q in queens)
            {
                if (q.Alive) continue;
                q.Respawn -= dt;
                if (q.Respawn <= 0f) PlaceQueen(q);
            }
            UpdatePopups(dt);
            CheckCollisions();

            if (state == GameState.Playing && larvaLeft <= 0)
            {
                state = GameState.LevelClear;
                stateTimer = 2.2f;
            }
        }

        // ---- tongue ---------------------------------------------------------
        void UpdateTongue(float dt, Point dir)
        {
            float remaining = TongueSpeed * dt;
            for (int iter = 0; iter < 4; iter++)
            {
                Vector2 target = path.Count > 0 ? Walker.Center(path[path.Count - 1]) : Start;
                Vector2 d = target - tipVis;
                float len = d.Length();
                if (len > remaining) { tipVis += d / len * remaining; return; }
                tipVis = target;
                remaining -= len;
                if (dir != Point.Zero && TryStep(dir)) continue;
                return;
            }
        }

        bool CanEnter(Point nt, Point tip)
        {
            if (nt.X < 0 || nt.X >= COLS || nt.Y < 0 || nt.Y >= ROWS) return false;
            if (tunnel[nt.X, nt.Y]) return true;
            // the dug shaft from the surface down to the first tunnel row
            if (path.Count == 1 && tip.Y == 7 && nt.Y == 8 && nt.X == tip.X) return true;
            return false;
        }

        bool TryStep(Point dir)
        {
            if (path.Count == 0)
            {
                if (dir.Y > 0 && (int)(antX / 8f) == shaftX)
                {
                    int c = shaftX;
                    antX = c * 8 + 4;
                    tipVis = Start;
                    path.Add(new Point(c, 7));
                    tipPrev = new Point(c, 6);
                    return true;
                }
                return false;
            }
            Point tip = path[path.Count - 1];
            Point nt = new Point(tip.X + dir.X, tip.Y + dir.Y);

            if (path.Count >= 2 && nt == path[path.Count - 2])
            {   // retract one cell
                tipPrev = tip;
                path.RemoveAt(path.Count - 1);
                return true;
            }
            if (path.Count == 1 && dir.Y < 0)
            {   // fully retract
                tipPrev = tip;
                path.Clear();
                return true;
            }
            if (!CanEnter(nt, tip)) return false;
            if (path.Contains(nt)) return false;
            tipPrev = tip;
            path.Add(nt);
            if (larva[nt.X, nt.Y]) { larva[nt.X, nt.Y] = false; larvaLeft--; AddScore(10); }
            return true;
        }

        // ---- enemies --------------------------------------------------------
        List<Point> Options(Walker w)
        {
            var o = new List<Point>();
            foreach (var d in Dirs)
                if (IsTunnel(new Point(w.Cell.X + d.X, w.Cell.Y + d.Y))) o.Add(d);
            var rev = new Point(-w.Dir.X, -w.Dir.Y);
            if (o.Count > 1) o.Remove(rev);
            return o;
        }

        Point PickDir(Walker w, List<Point> opts, bool chase)
        {
            if (opts.Count == 1) return opts[0];
            float chaseProb = Math.Min(0.6f, 0.25f + 0.04f * level);
            if (chase && path.Count > 0 && rng.NextDouble() < chaseProb)
            {
                Point tip = path[path.Count - 1];
                Point best = opts[0]; int bd = int.MaxValue;
                foreach (var d in opts)
                {
                    int dist = Math.Abs(w.Cell.X + d.X - tip.X) + Math.Abs(w.Cell.Y + d.Y - tip.Y);
                    if (dist < bd) { bd = dist; best = d; }
                }
                return best;
            }
            int total = 0;
            foreach (var d in opts) total += (d == w.Dir) ? 4 : 1;
            int roll = rng.Next(total);
            foreach (var d in opts)
            {
                roll -= (d == w.Dir) ? 4 : 1;
                if (roll < 0) return d;
            }
            return opts[0];
        }

        void UpdateEnemies(float dt)
        {
            float antStep = Math.Max(0.12f, 0.20f - level * 0.005f);
            foreach (var a in ants)
            {
                a.Step = antStep;
                a.Anim += dt * 12f;
                a.T += dt / a.Step;
                if (a.T >= 1f)
                {
                    a.T -= 1f;
                    var opts = Options(a);
                    if (opts.Count == 0) continue;
                    a.From = a.Cell;
                    Point d = PickDir(a, opts, true);
                    a.Dir = d;
                    a.Cell = new Point(a.Cell.X + d.X, a.Cell.Y + d.Y);
                }
            }
            float wormStep = Math.Max(0.12f, 0.2f - level * 0.005f);
            foreach (var w in worms)
            {
                w.Step = wormStep;
                w.T += dt / w.Step;
                if (w.T >= 1f)
                {
                    w.T -= 1f;
                    var opts = Options(w);
                    if (opts.Count == 0) continue;
                    w.From = w.Cell;
                    Point d = PickDir(w, opts, false);
                    w.Dir = d;
                    w.Cell = new Point(w.Cell.X + d.X, w.Cell.Y + d.Y);
                    w.Trail.Insert(0, w.Cell);
                    if (w.Trail.Count > 4) w.Trail.RemoveAt(w.Trail.Count - 1);
                }
            }
        }

        bool SpawnCellFree(Point c)
        {
            if (path.Contains(c)) return false;
            foreach (var a in ants) if (a.Cell == c) return false;
            foreach (var w in worms) if (w.Cell == c) return false;
            return true;
        }

        void UpdateSpawns(float dt)
        {
            int maxAnts = Math.Min(2 + level, 7);
            antSpawn -= dt;
            if (antSpawn <= 0f)
            {
                antSpawn = Math.Max(1.2f, 4f - 0.25f * level) * (0.7f + (float)rng.NextDouble() * 0.6f);
                if (ants.Count < maxAnts)
                {
                    for (int tries = 0; tries < 12; tries++)
                    {
                        int r = TunnelRows[rng.Next(TunnelRows.Length)];
                        bool left = rng.Next(2) == 0;
                        var c = new Point(left ? 0 : COLS - 1, r);
                        if (!tunnel[c.X, c.Y] || !SpawnCellFree(c)) continue;
                        var a = new Ant { From = c, Cell = c, T = 1f, Dir = new Point(left ? 1 : -1, 0) };
                        ants.Add(a);
                        break;
                    }
                }
            }
            if (level >= 2)
            {
                int maxWorms = Math.Min(1 + (level - 2) / 2, 3);
                wormSpawn -= dt;
                if (wormSpawn <= 0f)
                {
                    wormSpawn = 7f + (float)rng.NextDouble() * 4f;
                    if (worms.Count < maxWorms)
                    {
                        for (int tries = 0; tries < 12; tries++)
                        {
                            int r = TunnelRows[rng.Next(TunnelRows.Length)];
                            bool left = rng.Next(2) == 0;
                            var c = new Point(left ? 0 : COLS - 1, r);
                            if (!tunnel[c.X, c.Y] || !SpawnCellFree(c)) continue;
                            var w = new Worm { From = c, Cell = c, T = 1f, Dir = new Point(left ? 1 : -1, 0) };
                            for (int k = 0; k < 4; k++) w.Trail.Add(c);
                            worms.Add(w);
                            break;
                        }
                    }
                }
            }
        }

        // ---- spider (night) -------------------------------------------------
        Vector2 V(int k) { return k == 0 ? Start : Walker.Center(path[k - 1]); }

        Vector2 SpiderPos()
        {
            if (!spider.Descending) return new Vector2(spider.X, 47f);
            int n = path.Count;
            float t = Math.Min(spider.Idx, n);
            int i = (int)t;
            float f = t - i;
            return Vector2.Lerp(V(i), V(Math.Min(i + 1, n)), f);
        }

        void UpdateSpider(float dt)
        {
            if (spider == null)
            {
                if (dayTime >= dayLen)
                    spider = new Spider { X = rng.Next(2) == 0 ? -6f : W + 6f };
                return;
            }
            if (!spider.Descending)
            {
                float tx = Start.X;
                float dx = tx - spider.X;
                float mv = 50f * dt;
                if (Math.Abs(dx) <= mv) { spider.X = tx; spider.Descending = true; spider.Idx = 0f; }
                else spider.X += Math.Sign(dx) * mv;
            }
            if (spider.Descending)
            {
                spider.Idx += (3.6f + 0.3f * level) * dt;
                if (spider.Idx >= path.Count) Die();
            }
        }

        // ---- collisions -----------------------------------------------------
        void CheckCollisions()
        {
            bool has = path.Count > 0;
            Point tip = has ? path[path.Count - 1] : new Point(-9, -9);

            if (has)
                foreach (var q in queens)
                    if (q.Alive && q.Cell == tip) EatQueen(q);

            for (int i = ants.Count - 1; i >= 0; i--)
            {
                var a = ants[i];
                bool hitTip = has && (a.Cell == tip || (a.From == tip && a.Cell == tipPrev));
                if (hitTip)
                {
                    AddScore(100, a.Pos);
                    ants.RemoveAt(i);
                    continue;
                }
                int idx = path.IndexOf(a.Cell);
                if (idx >= 0 && idx < path.Count - 1) { Die(); return; }   // bitten by the tongue body
            }

            for (int i = worms.Count - 1; i >= 0; i--)
            {
                var w = worms[i];
                if (!has) break;
                Point head = w.Trail[0];
                if ((tip == w.Trail[1] || tip == w.Trail[2]) && tip != head)
                {   // eaten from behind
                    AddScore(200, w.Pos);
                    worms.RemoveAt(i);
                }
                else if (tip == head)
                {   // head touches the tip
                    Die();
                    return;
                }
            }
        }

        void EatQueen(Queen q)
        {
            q.Alive = false;
            q.Respawn = 18f;
            ants.Clear();
            worms.Clear();
            spider = null;
            dayTime = 0f;
            flash = 0.35f;
            AddScore(1000, Walker.Center(q.Cell));
        }

        void Die()
        {
            if (state != GameState.Playing) return;
            state = GameState.Dying;
            stateTimer = 1.5f;
        }

        void AddScore(int pts) { AddScore(pts, null); }

        void AddScore(int pts, Vector2? at)
        {
            score += pts;
            if (score > hi) hi = score;
            if (score >= nextExtra) { lives++; nextExtra += 10000; }
            if (at.HasValue) popups.Add(new Popup { Pos = at.Value - new Vector2(pts.ToString().Length * 3, 4), Text = pts.ToString() });
        }

        // ================================================================== draw
        protected override void Draw(GameTime gt)
        {
            GraphicsDevice.SetRenderTarget(rt);
            GraphicsDevice.Clear(Color.Black);
            sb.Begin(samplerState: SamplerState.PointClamp);
            if (state == GameState.Title) DrawTitle(); else DrawPlay();
            sb.End();

            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.Black);
            int bw = GraphicsDevice.PresentationParameters.BackBufferWidth;
            int bh = GraphicsDevice.PresentationParameters.BackBufferHeight;
            float s = Math.Min(bw / (float)W, bh / (float)H);
            if (s >= 1f) s = (float)Math.Floor(s);
            int dw = (int)(W * s), dh = (int)(H * s);
            sb.Begin(samplerState: SamplerState.PointClamp);
            sb.Draw(rt, new Rectangle((bw - dw) / 2, (bh - dh) / 2, dw, dh), Color.White);
            sb.End();
            base.Draw(gt);
        }

        void Rect(int x, int y, int w, int h, Color c) { sb.Draw(pix, new Rectangle(x, y, w, h), c); }

        void DrawText(string s, int x, int y, Color c, int scale = 1)
        {
            s = s.ToUpperInvariant();
            foreach (char ch in s)
            {
                int i = FontChars.IndexOf(ch);
                if (i >= 0)
                    sb.Draw(fontTex, new Rectangle(x, y, 5 * scale, 7 * scale),
                            new Rectangle((i % 16) * 8, (i / 16) * 8, 5, 7), c);
                x += 6 * scale;
            }
        }

        void DrawTextC(string s, int y, Color c, int scale = 1)
        {
            int w = s.Length * 6 * scale - scale;
            DrawText(s, (W - w) / 2, y, c, scale);
        }

        void DrawTitle()
        {
            DrawTextC("ANTEATER", 24, new Color(255, 150, 40), 4);
            DrawTextC("1982 TAGO ELECTRONICS", 62, new Color(120, 255, 120));
            sb.Draw(anteaterTex, new Vector2(92, 84), Color.White);
            Rect(0, 100, W, 3, new Color(0, 110, 20));
            Color t = new Color(230, 230, 230);
            int y = 124;
            string[] lines =
            {
                "ARROWS/WASD: MOVE ANTEATER+TONGUE",
                "DOWN: DROP TONGUE INTO ANTHILL",
                "EAT ALL LARVAE BEFORE NIGHT FALLS",
                "ANTS: EAT WITH TONGUE TIP ONLY",
                "WORMS: EAT FROM BEHIND ONLY",
                "QUEEN: CLEARS SCREEN, KILLS SPIDER",
            };
            foreach (var l in lines) { DrawTextC(l, y, t); y += 12; }
            DrawTextC("LARVA 10  ANT 100  WORM 200", 206, new Color(255, 220, 90));
            DrawTextC("QUEEN ANT 1000", 218, new Color(255, 220, 90));
            if (((int)(anim * 2)) % 2 == 0) DrawTextC("PRESS ENTER TO START", 244, new Color(255, 80, 80));
            DrawTextC("HIGH SCORE " + hi, 266, Color.White);
        }

        void Line(Vector2 a, Vector2 b, Color c)
        {
            int ax = (int)Math.Round(a.X), ay = (int)Math.Round(a.Y);
            int bx = (int)Math.Round(b.X), by = (int)Math.Round(b.Y);
            if (Math.Abs(bx - ax) >= Math.Abs(by - ay))
            {
                int x0 = Math.Min(ax, bx), x1 = Math.Max(ax, bx);
                Rect(x0, ay - 1, x1 - x0 + 1, 2, c);
            }
            else
            {
                int y0 = Math.Min(ay, by), y1 = Math.Max(ay, by);
                Rect(ax - 1, y0, 2, y1 - y0 + 1, c);
            }
        }

        void DrawTongue()
        {
            if (path.Count == 0 && Vector2.Distance(tipVis, Start) < 0.5f) return;
            var pts = new List<Vector2>();
            pts.Add(Start);
            foreach (var p in path) pts.Add(Walker.Center(p));
            if (pts.Count >= 2)
            {
                Vector2 a = pts[pts.Count - 2], b = pts[pts.Count - 1];
                if (Vector2.Distance(a, tipVis) + Vector2.Distance(tipVis, b) < Vector2.Distance(a, b) + 0.01f)
                    pts.RemoveAt(pts.Count - 1);
            }
            pts.Add(tipVis);
            Color tc = new Color(175, 255, 110);
            for (int i = 0; i < pts.Count - 1; i++) Line(pts[i], pts[i + 1], tc);
            int tx = (int)Math.Round(tipVis.X), ty = (int)Math.Round(tipVis.Y);
            Rect(tx - 2, ty - 2, 4, 4, new Color(255, 110, 150));
            Rect(tx - 1, ty - 1, 2, 2, new Color(255, 200, 220));
        }

        void DrawWalker(Texture2D tex, Walker w)
        {
            Vector2 p = w.Pos;
            p = new Vector2((float)Math.Round(p.X), (float)Math.Round(p.Y));
            float rot = 0f;
            SpriteEffects fx = SpriteEffects.None;
            if (w.Dir.Y > 0) rot = MathHelper.PiOver2;
            else if (w.Dir.Y < 0) rot = -MathHelper.PiOver2;
            else if (w.Dir.X < 0) fx = SpriteEffects.FlipHorizontally;
            sb.Draw(tex, p, null, Color.White, rot, new Vector2(4, 4), 1f, fx, 0f);
        }

        void DrawWorm(Worm w)
        {
            Color body = new Color(235, 90, 150), head = new Color(255, 225, 120);
            for (int i = 2; i >= 0; i--)
            {
                Vector2 p = Vector2.Lerp(Walker.Center(w.Trail[i + 1]), Walker.Center(w.Trail[i]), MathHelper.Clamp(w.T, 0f, 1f));
                int cx = (int)Math.Round(p.X), cy = (int)Math.Round(p.Y);
                Color c = i == 0 ? head : body;
                Rect(cx - 3, cy - 2, 6, 4, c);
                Rect(cx - 2, cy - 3, 4, 6, c);
                if (i == 0)
                {
                    int ex = cx + w.Dir.X * 2, ey = cy + w.Dir.Y * 2;
                    if (w.Dir.X != 0) { Rect(ex, cy - 2, 1, 1, Color.Black); Rect(ex, cy + 1, 1, 1, Color.Black); }
                    else { Rect(cx - 2, ey, 1, 1, Color.Black); Rect(cx + 1, ey, 1, 1, Color.Black); }
                }
            }
        }

        void DrawPlay()
        {
            sb.Draw(bgTex, Vector2.Zero, Color.White);

            // sky: sun by day, stars by night
            if (dayTime < dayLen)
            {
                float p = MathHelper.Clamp(dayTime / dayLen, 0f, 1f);
                int sx = (int)(12 + p * (W - 24));
                int sy = (int)(30 - Math.Sin(p * Math.PI) * 8);
                sb.Draw(sunTex, new Vector2(sx - 5, sy - 5), Color.White);
            }
            else
            {
                for (int i = 0; i < 14; i++)
                {
                    int x = Hash(i, 3, 99) % W, y = 18 + Hash(i, 4, 99) % 22;
                    bool tw = ((int)(anim * 3) + i) % 4 != 0;
                    if (tw) Rect(x, y, 1, 1, Color.White);
                }
            }

            // larvae
            foreach (int r in TunnelRows)
                for (int x = 1; x < COLS - 1; x++)
                    if (larva[x, r])
                    {
                        int cx = x * 8 + 4, cy = r * 8 + 4;
                        Color c = (x & 1) == 0 ? pal.Larva1 : pal.Larva2;
                        Rect(cx - 1, cy, 3, 1, c);
                        Rect(cx, cy - 1, 1, 3, c);
                    }

            // queens
            foreach (var q in queens)
                if (q.Alive)
                    sb.Draw(((int)(anim * 4) & 1) == 0 ? queenTex0 : queenTex1, new Vector2(q.Cell.X * 8, q.Cell.Y * 8), Color.White);

            DrawTongue();

            foreach (var a in ants) DrawWalker(((int)a.Anim & 1) == 0 ? antTex0 : antTex1, a);
            foreach (var w in worms) DrawWorm(w);

            if (spider != null)
            {
                Vector2 sp = SpiderPos();
                sb.Draw(spiderTex, new Vector2((float)Math.Round(sp.X) - 4, (float)Math.Round(sp.Y) - 4), Color.White);
            }

            // anteater
            bool visible = state != GameState.Dying || ((int)(stateTimer * 10f) % 2 == 0);
            if (visible)
            {
                if (facingRight)
                    sb.Draw(anteaterTex, new Vector2((int)antX - 39, 40), Color.White);
                else
                    sb.Draw(anteaterTex, new Vector2((int)antX, 40), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.FlipHorizontally, 0f);
            }

            foreach (var p in popups) DrawText(p.Text, (int)p.Pos.X, (int)p.Pos.Y, Color.White);

            // HUD
            DrawText("1ST", 4, 2, new Color(255, 70, 70));
            string sc = score.ToString();
            DrawText(sc, 40 - sc.Length * 6, 11, Color.White);
            DrawText("HIGH SCORE", 72, 2, new Color(255, 230, 60));
            string hs = Math.Max(hi, score).ToString();
            DrawText(hs, 132 - hs.Length * 6, 11, Color.White);
            DrawText("ANTHILL " + level, 130, 276, new Color(80, 255, 80));
            for (int i = 0; i < Math.Min(lives, 5); i++)
                sb.Draw(anteaterTex, new Vector2(4 + i * 18, 277), null, Color.White, 0f, Vector2.Zero, 0.42f, SpriteEffects.None, 0f);

            if (paused) DrawTextC("PAUSED", 140, Color.White, 2);
            if (state == GameState.LevelClear) DrawTextC("ANTHILL CLEARED!", 140, new Color(255, 230, 60), 1);
            if (state == GameState.GameOver)
            {
                DrawTextC("GAME OVER", 130, new Color(255, 60, 60), 3);
                if (((int)(anim * 2)) % 2 == 0) DrawTextC("PRESS ENTER", 160, Color.White);
            }
            if (flash > 0f) Rect(0, 0, W, H, Color.White * (flash / 0.35f * 0.6f));
        }
    }
}
