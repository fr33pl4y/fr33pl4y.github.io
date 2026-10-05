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

namespace TetrisClone
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            using (var game = new TetrisGame())
                game.Run();
        }
    }

    // ------------------------------------------------------------------
    // Bitmap font: 5x7 glyphs baked into a texture atlas at startup
    // ------------------------------------------------------------------
    public class BitmapFont
    {
        const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789:-.!/";
        static readonly string[][] Glyphs =
        {
            new[]{ "-XXX-", "X---X", "X---X", "XXXXX", "X---X", "X---X", "X---X" }, // A
            new[]{ "XXXX-", "X---X", "X---X", "XXXX-", "X---X", "X---X", "XXXX-" }, // B
            new[]{ "-XXX-", "X---X", "X----", "X----", "X----", "X---X", "-XXX-" }, // C
            new[]{ "XXXX-", "X---X", "X---X", "X---X", "X---X", "X---X", "XXXX-" }, // D
            new[]{ "XXXXX", "X----", "X----", "XXXX-", "X----", "X----", "XXXXX" }, // E
            new[]{ "XXXXX", "X----", "X----", "XXXX-", "X----", "X----", "X----" }, // F
            new[]{ "-XXX-", "X---X", "X----", "X-XXX", "X---X", "X---X", "-XXXX" }, // G
            new[]{ "X---X", "X---X", "X---X", "XXXXX", "X---X", "X---X", "X---X" }, // H
            new[]{ "-XXX-", "--X--", "--X--", "--X--", "--X--", "--X--", "-XXX-" }, // I
            new[]{ "--XXX", "---X-", "---X-", "---X-", "---X-", "X--X-", "-XX--" }, // J
            new[]{ "X---X", "X--X-", "X-X--", "XX---", "X-X--", "X--X-", "X---X" }, // K
            new[]{ "X----", "X----", "X----", "X----", "X----", "X----", "XXXXX" }, // L
            new[]{ "X---X", "XX-XX", "X-X-X", "X-X-X", "X---X", "X---X", "X---X" }, // M
            new[]{ "X---X", "X---X", "XX--X", "X-X-X", "X--XX", "X---X", "X---X" }, // N
            new[]{ "-XXX-", "X---X", "X---X", "X---X", "X---X", "X---X", "-XXX-" }, // O
            new[]{ "XXXX-", "X---X", "X---X", "XXXX-", "X----", "X----", "X----" }, // P
            new[]{ "-XXX-", "X---X", "X---X", "X---X", "X-X-X", "X--X-", "-XX-X" }, // Q
            new[]{ "XXXX-", "X---X", "X---X", "XXXX-", "X-X--", "X--X-", "X---X" }, // R
            new[]{ "-XXXX", "X----", "X----", "-XXX-", "----X", "----X", "XXXX-" }, // S
            new[]{ "XXXXX", "--X--", "--X--", "--X--", "--X--", "--X--", "--X--" }, // T
            new[]{ "X---X", "X---X", "X---X", "X---X", "X---X", "X---X", "-XXX-" }, // U
            new[]{ "X---X", "X---X", "X---X", "X---X", "X---X", "-X-X-", "--X--" }, // V
            new[]{ "X---X", "X---X", "X---X", "X-X-X", "X-X-X", "X-X-X", "-X-X-" }, // W
            new[]{ "X---X", "X---X", "-X-X-", "--X--", "-X-X-", "X---X", "X---X" }, // X
            new[]{ "X---X", "X---X", "-X-X-", "--X--", "--X--", "--X--", "--X--" }, // Y
            new[]{ "XXXXX", "----X", "---X-", "--X--", "-X---", "X----", "XXXXX" }, // Z
            new[]{ "-XXX-", "X---X", "X--XX", "X-X-X", "XX--X", "X---X", "-XXX-" }, // 0
            new[]{ "--X--", "-XX--", "--X--", "--X--", "--X--", "--X--", "-XXX-" }, // 1
            new[]{ "-XXX-", "X---X", "----X", "---X-", "--X--", "-X---", "XXXXX" }, // 2
            new[]{ "XXXXX", "---X-", "--X--", "---X-", "----X", "X---X", "-XXX-" }, // 3
            new[]{ "---X-", "--XX-", "-X-X-", "X--X-", "XXXXX", "---X-", "---X-" }, // 4
            new[]{ "XXXXX", "X----", "XXXX-", "----X", "----X", "X---X", "-XXX-" }, // 5
            new[]{ "--XX-", "-X---", "X----", "XXXX-", "X---X", "X---X", "-XXX-" }, // 6
            new[]{ "XXXXX", "----X", "---X-", "--X--", "-X---", "-X---", "-X---" }, // 7
            new[]{ "-XXX-", "X---X", "X---X", "-XXX-", "X---X", "X---X", "-XXX-" }, // 8
            new[]{ "-XXX-", "X---X", "X---X", "-XXXX", "----X", "---X-", "-XX--" }, // 9
            new[]{ "-----", "--X--", "-----", "-----", "-----", "--X--", "-----" }, // :
            new[]{ "-----", "-----", "-----", "XXXXX", "-----", "-----", "-----" }, // -
            new[]{ "-----", "-----", "-----", "-----", "-----", "-XX--", "-XX--" }, // .
            new[]{ "--X--", "--X--", "--X--", "--X--", "--X--", "-----", "--X--" }, // !
            new[]{ "----X", "----X", "---X-", "--X--", "-X---", "X----", "X----" }, // /
        };

        Texture2D atlas;

        public BitmapFont(GraphicsDevice gd)
        {
            int n = Glyphs.Length;
            var data = new Color[n * 6 * 7];
            int w = n * 6;
            for (int g = 0; g < n; g++)
                for (int y = 0; y < 7; y++)
                    for (int x = 0; x < 5; x++)
                        if (Glyphs[g][y][x] == 'X')
                            data[y * w + g * 6 + x] = Color.White;
            atlas = new Texture2D(gd, w, 7);
            atlas.SetData(data);
        }

        public int Measure(string s, int scale = 1) => s.Length * 6 * scale - scale;

        public void Draw(SpriteBatch sb, string text, int x, int y, Color color, int scale = 1)
        {
            foreach (char ch in text.ToUpperInvariant())
            {
                int idx = Chars.IndexOf(ch);
                if (idx >= 0)
                    sb.Draw(atlas, new Rectangle(x, y, 5 * scale, 7 * scale),
                            new Rectangle(idx * 6, 0, 5, 7), color);
                x += 6 * scale;
            }
        }
    }

    // ------------------------------------------------------------------
    // Game
    // ------------------------------------------------------------------
    public class TetrisGame : Game
    {
        // Internal arcade resolution
        const int W = 336, H = 240;
        const int COLS = 10, ROWS = 20, CELL = 9;
        const int FX = 36, FY = 22;              // playfield top-left
        const int LINES_PER_ROUND = 20;

        enum State { Title, Playing, Clearing, RoundEnd, GameOver, Paused }

        // ---- Tetromino data ----
        static readonly Point[][] BaseShapes =
        {
            new[]{ new Point(0,1), new Point(1,1), new Point(2,1), new Point(3,1) }, // I
            new[]{ new Point(0,0), new Point(1,0), new Point(0,1), new Point(1,1) }, // O
            new[]{ new Point(0,0), new Point(1,0), new Point(2,0), new Point(1,1) }, // T
            new[]{ new Point(0,0), new Point(1,0), new Point(2,0), new Point(2,1) }, // J
            new[]{ new Point(0,0), new Point(1,0), new Point(2,0), new Point(0,1) }, // L
            new[]{ new Point(1,0), new Point(2,0), new Point(0,1), new Point(1,1) }, // S
            new[]{ new Point(0,0), new Point(1,0), new Point(1,1), new Point(2,1) }, // Z
        };
        static readonly int[] BoxSize = { 4, 2, 3, 3, 3, 3, 3 };
        static readonly Color[] PieceColors =
        {
            new Color(0, 220, 230),   // I
            new Color(245, 225, 0),   // O
            new Color(205, 0, 205),   // T
            new Color(40, 70, 245),   // J
            new Color(255, 140, 0),   // L
            new Color(0, 205, 50),    // S
            new Color(235, 25, 25),   // Z
        };
        static readonly Point[][][] Rots = BuildRotations();

        static Point[][][] BuildRotations()
        {
            var res = new Point[7][][];
            for (int t = 0; t < 7; t++)
            {
                res[t] = new Point[4][];
                var cur = (Point[])BaseShapes[t].Clone();
                int n = BoxSize[t];
                for (int r = 0; r < 4; r++)
                {
                    res[t][r] = (Point[])cur.Clone();
                    var next = new Point[4];
                    for (int i = 0; i < 4; i++)
                        next[i] = new Point(n - 1 - cur[i].Y, cur[i].X);
                    cur = next;
                }
            }
            return res;
        }

        // Bonus for incomplete lines left at the bottom when a round ends (0..19 lines).
        // Index 1 is not listed in the source text; 1900 is an interpolated value.
        static readonly int[] RoundBonus =
        {
            2100, 1900, 1710, 1530, 1360, 1200, 1050, 910, 780, 660,
            550, 450, 360, 280, 210, 150, 100, 60, 30, 10
        };
        static readonly int[] LineScore = { 0, 50, 150, 400, 900 };

        // ---- Graphics ----
        GraphicsDeviceManager gfx;
        SpriteBatch sb;
        Texture2D px;
        RenderTarget2D rt;
        BitmapFont font;
        bool isFull;
        int winW = W * 3, winH = H * 3;

        // ---- Input ----
        KeyboardState ks, prev;
        int dasDir; float dasT;

        // ---- Game state ----
        State state = State.Title;
        int[,] grid = new int[ROWS, COLS];
        int curType, curRot, curX, curY, nextType;
        int[] stats = new int[7];
        Random rng = new Random();
        float fallT, clearT, roundT;
        int manualRows;
        long score;
        long highScore = 10000;
        int lines, linesLeft = LINES_PER_ROUND, round = 1;
        long lastBonus, lastRoundBonus;
        List<int> clearRows = new List<int>();
        double time;

        public TetrisGame()
        {
            gfx = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = winW,
                PreferredBackBufferHeight = winH,
                HardwareModeSwitch = false
            };
            Content.RootDirectory = "Content";
            Window.AllowUserResizing = true;
            Window.Title = "Tetris";
            IsMouseVisible = true;
        }

        protected override void LoadContent()
        {
            sb = new SpriteBatch(GraphicsDevice);
            px = new Texture2D(GraphicsDevice, 1, 1);
            px.SetData(new[] { Color.White });
            rt = new RenderTarget2D(GraphicsDevice, W, H);
            font = new BitmapFont(GraphicsDevice);
            nextType = rng.Next(7);
        }

        // ------------------------------------------------------------------
        // Full screen
        // ------------------------------------------------------------------
        void ToggleFullScreen()
        {
            if (!isFull)
            {
                winW = Window.ClientBounds.Width;
                winH = Window.ClientBounds.Height;
                var dm = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
                gfx.PreferredBackBufferWidth = dm.Width;
                gfx.PreferredBackBufferHeight = dm.Height;
                gfx.HardwareModeSwitch = false;
                gfx.IsFullScreen = true;
                IsMouseVisible = false;
            }
            else
            {
                gfx.IsFullScreen = false;
                gfx.PreferredBackBufferWidth = winW;
                gfx.PreferredBackBufferHeight = winH;
                IsMouseVisible = true;
            }
            isFull = !isFull;
            gfx.ApplyChanges();
        }

        // ------------------------------------------------------------------
        // Game logic
        // ------------------------------------------------------------------
        bool Pressed(Keys k) => ks.IsKeyDown(k) && !prev.IsKeyDown(k);

        float Gravity => (float)Math.Max(0.05, 0.8 * Math.Pow(0.82, round - 1));

        void NewGame()
        {
            Array.Clear(grid, 0, grid.Length);
            Array.Clear(stats, 0, stats.Length);
            score = 0; lines = 0; round = 1; linesLeft = LINES_PER_ROUND;
            lastBonus = 0;
            nextType = rng.Next(7);
            state = State.Playing;
            Spawn();
        }

        int RandomPiece(int previous)
        {
            int t = rng.Next(7);
            if (t == previous) t = rng.Next(7); // slight anti-repeat
            return t;
        }

        void Spawn()
        {
            curType = nextType;
            nextType = RandomPiece(curType);
            curRot = 0;
            curX = (COLS - BoxSize[curType]) / 2;
            int minY = 99;
            foreach (var p in Rots[curType][0]) minY = Math.Min(minY, p.Y);
            curY = -minY;
            fallT = 0; manualRows = 0;
            stats[curType]++;
            state = State.Playing;
            if (!Fits(curType, curRot, curX, curY))
            {
                state = State.GameOver;
                if (score > highScore) highScore = score;
            }
        }

        bool Fits(int type, int rot, int x, int y)
        {
            foreach (var p in Rots[type][rot])
            {
                int cx = x + p.X, cy = y + p.Y;
                if (cx < 0 || cx >= COLS || cy >= ROWS) return false;
                if (cy >= 0 && grid[cy, cx] != 0) return false;
            }
            return true;
        }

        bool Move(int dx, int dy)
        {
            if (Fits(curType, curRot, curX + dx, curY + dy))
            {
                curX += dx; curY += dy;
                return true;
            }
            return false;
        }

        void TryRotate(int dir)
        {
            if (curType == 1) return; // O piece
            int nr = (curRot + dir + 4) % 4;
            int[] kicks = { 0, -1, 1, -2, 2 };
            foreach (int k in kicks)
            {
                if (Fits(curType, nr, curX + k, curY))
                {
                    curRot = nr; curX += k;
                    return;
                }
            }
        }

        void LockPiece()
        {
            foreach (var p in Rots[curType][curRot])
            {
                int cx = curX + p.X, cy = curY + p.Y;
                if (cy >= 0) grid[cy, cx] = curType + 1;
            }
            score += Math.Min(500, manualRows * 10 + (round - 1) * 5);
            manualRows = 0;

            clearRows.Clear();
            for (int r = 0; r < ROWS; r++)
            {
                bool full = true;
                for (int c = 0; c < COLS; c++)
                    if (grid[r, c] == 0) { full = false; break; }
                if (full) clearRows.Add(r);
            }
            if (clearRows.Count > 0)
            {
                state = State.Clearing;
                clearT = 0.45f;
            }
            else
            {
                if (score > highScore) highScore = score;
                Spawn();
            }
        }

        void FinishClear()
        {
            int n = clearRows.Count;
            var ng = new int[ROWS, COLS];
            int dst = ROWS - 1;
            for (int r = ROWS - 1; r >= 0; r--)
            {
                if (clearRows.Contains(r)) continue;
                for (int c = 0; c < COLS; c++) ng[dst, c] = grid[r, c];
                dst--;
            }
            grid = ng;
            clearRows.Clear();

            score += LineScore[Math.Min(4, n)];
            lines += n;
            linesLeft -= n;
            if (score > highScore) highScore = score;

            if (linesLeft <= 0)
            {
                int incomplete = 0;
                for (int r = 0; r < ROWS; r++)
                    for (int c = 0; c < COLS; c++)
                        if (grid[r, c] != 0) { incomplete++; break; }
                lastRoundBonus = incomplete < RoundBonus.Length ? RoundBonus[incomplete] : 0;
                score += lastRoundBonus;
                if (score > highScore) highScore = score;
                state = State.RoundEnd;
                roundT = 3.0f;
            }
            else
            {
                Spawn();
            }
        }

        void StartNextRound()
        {
            round++;
            linesLeft = LINES_PER_ROUND;
            lastBonus = 0;
            if (round == 4) lastBonus = 20000;
            else if (round == 7) lastBonus = 40000;
            score += lastBonus;
            if (score > highScore) highScore = score;
            Spawn();
        }

        protected override void Update(GameTime gameTime)
        {
            prev = ks;
            ks = Keyboard.GetState();
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            time += dt;

            if (Pressed(Keys.F11) ||
                ((ks.IsKeyDown(Keys.LeftAlt) || ks.IsKeyDown(Keys.RightAlt)) && Pressed(Keys.Enter)))
                ToggleFullScreen();
            if (Pressed(Keys.Escape)) Exit();

            switch (state)
            {
                case State.Title:
                case State.GameOver:
                    if (Pressed(Keys.Enter) && !(ks.IsKeyDown(Keys.LeftAlt) || ks.IsKeyDown(Keys.RightAlt)))
                        NewGame();
                    break;

                case State.Paused:
                    if (Pressed(Keys.P)) state = State.Playing;
                    break;

                case State.Clearing:
                    clearT -= dt;
                    if (clearT <= 0) FinishClear();
                    break;

                case State.RoundEnd:
                    roundT -= dt;
                    if (roundT <= 0) StartNextRound();
                    break;

                case State.Playing:
                    UpdatePlaying(dt);
                    break;
            }
            base.Update(gameTime);
        }

        void UpdatePlaying(float dt)
        {
            if (Pressed(Keys.P)) { state = State.Paused; return; }

            if (Pressed(Keys.Up) || Pressed(Keys.X)) TryRotate(1);
            if (Pressed(Keys.Z) || Pressed(Keys.LeftControl)) TryRotate(-1);

            bool l = ks.IsKeyDown(Keys.Left), r = ks.IsKeyDown(Keys.Right);
            int dir = (l && !r) ? -1 : (r && !l) ? 1 : 0;
            if (dir != 0)
            {
                if (dir != dasDir) { dasDir = dir; Move(dir, 0); dasT = 0.17f; }
                else
                {
                    dasT -= dt;
                    if (dasT <= 0) { Move(dir, 0); dasT = 0.05f; }
                }
            }
            else dasDir = 0;

            if (Pressed(Keys.Space))
            {
                while (Move(0, 1)) manualRows++;
                LockPiece();
                return;
            }

            bool down = ks.IsKeyDown(Keys.Down);
            float delay = down ? Math.Min(Gravity, 0.035f) : Gravity;
            fallT += dt;
            while (fallT >= delay && state == State.Playing)
            {
                fallT -= delay;
                if (Move(0, 1))
                {
                    if (down) manualRows++;
                }
                else
                {
                    LockPiece();
                    break;
                }
            }
        }

        // ------------------------------------------------------------------
        // Drawing helpers
        // ------------------------------------------------------------------
        void Rect(int x, int y, int w, int h, Color c) => sb.Draw(px, new Rectangle(x, y, w, h), c);

        void DrawBlock(int x, int y, int s, Color c)
        {
            Rect(x, y, s, s, Color.Lerp(c, Color.Black, 0.55f));
            Rect(x, y, s - 1, s - 1, Color.Lerp(c, Color.White, 0.55f));
            Rect(x + 1, y + 1, s - 2, s - 2, c);
            Rect(x + 2, y + 2, 2, 1, Color.White * 0.8f);
        }

        void Text(string s, int x, int y, Color c, int scale = 1) => font.Draw(sb, s, x, y, c, scale);

        void TextCentered(string s, int cx, int y, Color c, int scale = 1)
            => font.Draw(sb, s, cx - font.Measure(s, scale) / 2, y, c, scale);

        void DrawPillar(int x, int y, int w, int h)
        {
            Color baseC = new Color(104, 96, 78), dark = new Color(60, 54, 44), light = new Color(150, 140, 115);
            Rect(x, y, w, h, baseC);
            for (int j = 0, row = 0; j < h; j += 10, row++)
            {
                Rect(x, y + j, w, 1, dark);
                int off = (row % 2 == 0) ? 0 : w / 2;
                if (off > 0) Rect(x + off, y + j, 1, Math.Min(10, h - j), dark);
                else if (w > 8) Rect(x + w / 4, y + j, 1, Math.Min(10, h - j), dark);
                Rect(x, y + j + 1, w, 1, light);
            }
            Rect(x, y, 2, h, light);
            Rect(x + w - 2, y, 2, h, dark);
        }

        void DrawDome(int cx, int baseY, Color c)
        {
            int[] widths = { 10, 12, 12, 10, 8, 6, 4, 2 };
            Color hi = Color.Lerp(c, Color.White, 0.5f);
            for (int i = 0; i < widths.Length; i++)
            {
                int w = widths[i];
                Rect(cx - w / 2, baseY - i * 2 - 2, w, 2, c);
                Rect(cx - w / 2, baseY - i * 2 - 2, 1, 2, hi);
            }
            Rect(cx, baseY - widths.Length * 2 - 4, 1, 3, hi);
        }

        void DrawCastle()
        {
            Color gold = new Color(235, 170, 30);
            // towers
            Rect(132, 20, 10, 28, new Color(200, 150, 40));
            Rect(194, 20, 10, 28, new Color(200, 150, 40));
            Rect(132, 20, 2, 28, gold); Rect(194, 20, 2, 28, gold);
            DrawDome(137, 22, gold);
            DrawDome(199, 22, gold);
            // central building
            Rect(144, 14, 48, 42, new Color(120, 112, 92));
            Rect(144, 14, 48, 2, new Color(170, 160, 130));
            // top emblem
            Rect(164, 2, 8, 8, new Color(235, 150, 0));
            Rect(165, 3, 6, 6, new Color(255, 200, 60));
            Text("F", 166, 2, new Color(120, 40, 0));
            // sign
            Rect(148, 18, 40, 11, new Color(230, 215, 150));
            Rect(148, 18, 40, 1, Color.White);
            TextCentered("TETRIS", 168, 20, new Color(215, 20, 20));
            // arched windows
            for (int i = 0; i < 2; i++)
            {
                int wx = 150 + i * 19;
                Rect(wx, 36, 15, 20, new Color(40, 70, 190));
                Rect(wx + 2, 34, 11, 2, new Color(40, 70, 190));
                Rect(wx + 4, 33, 7, 1, new Color(40, 70, 190));
                Rect(wx + 7, 33, 1, 23, new Color(20, 30, 100));
                Rect(wx, 44, 15, 1, new Color(20, 30, 100));
                Rect(wx - 1, 36, 1, 20, new Color(60, 56, 46));
                Rect(wx + 15, 36, 1, 20, new Color(60, 56, 46));
            }
            // red awning
            for (int x = 144; x < 192; x += 6)
            {
                Rect(x, 57, 6, 3, new Color(210, 20, 20));
                Rect(x + 1, 60, 4, 2, new Color(210, 20, 20));
                Rect(x + 2, 62, 2, 1, new Color(210, 20, 20));
            }

            // lines-left panel
            Rect(134, 70, 68, 56, new Color(10, 40, 24));
            Rect(134, 70, 68, 1, new Color(90, 120, 90));
            Rect(134, 125, 68, 1, new Color(90, 120, 90));
            TextCentered("LINES", 168, 88, Color.White);
            TextCentered("LEFT", 168, 98, Color.White);
            TextCentered(Math.Max(0, linesLeft).ToString(), 168, 110, new Color(250, 230, 60), 1);
            Rect(140, 74, 56, 2, new Color(210, 20, 20));
            Rect(140, 120, 56, 2, new Color(210, 20, 20));

            // scores
            TextCentered("HIGH SCORE", 168, 140, Color.White);
            TextCentered(highScore.ToString(), 168, 150, new Color(250, 230, 60));
            TextCentered("ROUND " + round, 168, 170, Color.White);
            TextCentered("FREE PLAY", 168, 184, new Color(150, 200, 255));
        }

        void DrawStats()
        {
            Text("STATS", 224, 58, Color.White);
            int max = 1;
            for (int i = 0; i < 7; i++) max = Math.Max(max, stats[i]);
            float scale = Math.Min(3f, 78f / max);
            int baseY = 196;
            for (int i = 0; i < 7; i++)
            {
                int h = (int)(stats[i] * scale);
                int x = 216 + i * 11;
                Rect(x, baseY - h, 9, h, PieceColors[i]);
                if (h > 0)
                {
                    Rect(x, baseY - h, 9, 1, Color.Lerp(PieceColors[i], Color.White, 0.6f));
                    Rect(x + 8, baseY - h, 1, h, Color.Lerp(PieceColors[i], Color.Black, 0.5f));
                }
            }
            Rect(214, baseY, 80, 1, new Color(90, 90, 90));
        }

        void DrawNext()
        {
            Text("NEXT", 4, 4, new Color(235, 30, 30));
            var cells = Rots[nextType][0];
            int minX = 99, maxX = -1, minY = 99, maxY = -1;
            foreach (var p in cells)
            {
                minX = Math.Min(minX, p.X); maxX = Math.Max(maxX, p.X);
                minY = Math.Min(minY, p.Y); maxY = Math.Max(maxY, p.Y);
            }
            int s = 7;
            int w = (maxX - minX + 1) * s, h = (maxY - minY + 1) * s;
            int ox = 17 - w / 2, oy = 22 - h / 2 + 4;
            foreach (var p in cells)
                DrawBlock(ox + (p.X - minX) * s, oy + (p.Y - minY) * s, s, PieceColors[nextType]);
        }

        void DrawHud()
        {
            Color panel = new Color(30, 8, 8), edge = new Color(180, 120, 40);
            Rect(0, 212, 128, 26, edge);
            Rect(1, 213, 126, 24, panel);
            Text("SCORE " + score.ToString("D6"), 6, 218, new Color(255, 140, 40));
            Text("LINES " + lines, 6, 228, new Color(255, 140, 40));

            Rect(212, 212, 90, 26, new Color(60, 90, 200));
            Rect(213, 213, 88, 24, new Color(10, 14, 60));
            if (((int)(time * 2)) % 2 == 0)
                TextCentered("INSERT COIN", 257, 221, new Color(120, 160, 255));

            // little gauge bars at the corners like the cabinet
            Rect(2, 206, 4, 4, new Color(0, 200, 60));
            Rect(330, 206, 4, 4, new Color(0, 200, 60));
        }

        void DrawField()
        {
            // frame
            Rect(FX - 3, FY - 3, COLS * CELL + 6, ROWS * CELL + 6, new Color(150, 140, 115));
            Rect(FX - 1, FY - 1, COLS * CELL + 2, ROWS * CELL + 2, Color.Black);

            bool flash = state == State.Clearing && ((int)(clearT * 20) % 2 == 0);
            for (int r = 0; r < ROWS; r++)
                for (int c = 0; c < COLS; c++)
                {
                    int v = grid[r, c];
                    if (v == 0) continue;
                    Color col = PieceColors[v - 1];
                    if (flash && clearRows.Contains(r)) col = Color.White;
                    DrawBlock(FX + c * CELL, FY + r * CELL, CELL, col);
                }

            if (state == State.Playing || state == State.Paused)
            {
                if (state == State.Playing)
                    foreach (var p in Rots[curType][curRot])
                    {
                        int cy = curY + p.Y;
                        if (cy < 0) continue;
                        DrawBlock(FX + (curX + p.X) * CELL, FY + cy * CELL, CELL, PieceColors[curType]);
                    }
            }
        }

        void DrawOverlay()
        {
            int cx = FX + COLS * CELL / 2;
            switch (state)
            {
                case State.Title:
                {
                    string t = "TETRIS";
                    int x = cx - font.Measure(t, 2) / 2;
                    for (int i = 0; i < t.Length; i++)
                        Text(t[i].ToString(), x + i * 12, FY + 50, PieceColors[i % 7], 2);
                    if (((int)(time * 2)) % 2 == 0)
                        TextCentered("PRESS ENTER", cx, FY + 80, Color.White);
                    TextCentered("1989 ATARI", cx, FY + 110, new Color(150, 150, 150));
                    break;
                }
                case State.Paused:
                    Rect(FX + 8, FY + 70, 74, 20, Color.Black);
                    TextCentered("PAUSED", cx, FY + 77, Color.White);
                    break;
                case State.GameOver:
                    Rect(FX + 4, FY + 62, 82, 36, Color.Black);
                    TextCentered("GAME OVER", cx, FY + 68, new Color(235, 30, 30));
                    if (((int)(time * 2)) % 2 == 0)
                        TextCentered("PRESS ENTER", cx, FY + 84, Color.White);
                    break;
                case State.RoundEnd:
                    Rect(FX + 2, FY + 56, 86, 48, Color.Black);
                    TextCentered("ROUND CLEAR", cx, FY + 62, new Color(250, 230, 60));
                    TextCentered("BONUS", cx, FY + 76, Color.White);
                    TextCentered(lastRoundBonus.ToString(), cx, FY + 88, new Color(250, 230, 60));
                    break;
                case State.Playing:
                    if (lastBonus > 0 && fallT < 99 && time % 1000 >= 0 && BonusVisible())
                    {
                        Rect(FX + 2, FY + 4, 86, 20, Color.Black);
                        TextCentered("ROUND " + round, cx, FY + 8, Color.White);
                        TextCentered("BONUS " + lastBonus, cx, FY + 16, new Color(250, 230, 60));
                    }
                    break;
            }
        }

        // Show the round-start bonus for the first couple of pieces' worth of time
        float bonusShown;
        bool BonusVisible()
        {
            bonusShown += 1f / 60f;
            if (bonusShown > 150f) { lastBonus = 0; bonusShown = 0; return false; }
            return true;
        }

        protected override void Draw(GameTime gameTime)
        {
            // ---- render to low-res target ----
            GraphicsDevice.SetRenderTarget(rt);
            GraphicsDevice.Clear(Color.Black);
            sb.Begin(samplerState: SamplerState.PointClamp);

            DrawPillar(0, 38, 34, 170);
            DrawPillar(302, 38, 34, 170);
            DrawField();
            DrawCastle();
            if (state == State.Title) DrawTitleHelp(); else DrawStats();
            DrawNext();
            DrawHud();
            DrawOverlay();

            sb.End();

            // ---- scale to window (letterboxed) ----
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.Black);
            int vw = GraphicsDevice.PresentationParameters.BackBufferWidth;
            int vh = GraphicsDevice.PresentationParameters.BackBufferHeight;
            float s = Math.Min(vw / (float)W, vh / (float)H);
            int dw = (int)(W * s), dh = (int)(H * s);
            var dest = new Rectangle((vw - dw) / 2, (vh - dh) / 2, dw, dh);
            sb.Begin(samplerState: SamplerState.PointClamp);
            sb.Draw(rt, dest, Color.White);
            sb.End();

            base.Draw(gameTime);
        }

        void DrawTitleHelp()
        {
            Color c = new Color(190, 190, 190);
            Text("CONTROLS", 224, 58, Color.White);
            Text("ARROWS:MOVE", 218, 76, c);
            Text("DOWN:SOFT DROP", 218, 88, c);
            Text("SPACE:HARD DROP", 218, 100, c);
            Text("UP/X:ROTATE", 218, 112, c);
            Text("Z:ROTATE BACK", 218, 124, c);
            Text("P:PAUSE", 218, 136, c);
            Text("F11:FULLSCREEN", 218, 148, c);
            Text("ESC:QUIT", 218, 160, c);
        }
    }
}
