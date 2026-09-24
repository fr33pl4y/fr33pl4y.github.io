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

namespace SpaceInvadersClone
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new InvadersGame())
                game.Run();
        }
    }

    // ---------------------------------------------------------------------------------
    //  Sprite: a white-on-transparent texture that we tint at draw time
    // ---------------------------------------------------------------------------------
    sealed class Sprite
    {
        public readonly Texture2D Tex;
        public readonly int W, H;
        public Sprite(Texture2D tex) { Tex = tex; W = tex.Width; H = tex.Height; }
    }

    // ---------------------------------------------------------------------------------
    //  Art: every sprite and font glyph in the game, drawn by hand as text
    // ---------------------------------------------------------------------------------
    static class Art
    {
        public static Texture2D Pixel;
        public static Sprite[][] Aliens;        // [type][animation frame]  0=squid 1=crab 2=octopus
        public static Sprite[][] AlienBullets;  // [type][animation frame]
        public static Sprite Ufo, UfoBoom, Player, PlayerBoom1, PlayerBoom2, AlienBoom, ShotBoom, GroundBoom;
        public static readonly Dictionary<char, Sprite> Font = new Dictionary<char, Sprite>();

        // 5x7 bitmap font. Format:  "<char>:row|row|row|row|row|row|row"
        static readonly string[] FontData =
        {
            "0:.XXX.|X...X|X..XX|X.X.X|XX..X|X...X|.XXX.",
            "1:..X..|.XX..|..X..|..X..|..X..|..X..|.XXX.",
            "2:.XXX.|X...X|....X|...X.|..X..|.X...|XXXXX",
            "3:XXXXX|...X.|..X..|...X.|....X|X...X|.XXX.",
            "4:...X.|..XX.|.X.X.|X..X.|XXXXX|...X.|...X.",
            "5:XXXXX|X....|XXXX.|....X|....X|X...X|.XXX.",
            "6:..XX.|.X...|X....|XXXX.|X...X|X...X|.XXX.",
            "7:XXXXX|....X|...X.|..X..|.X...|.X...|.X...",
            "8:.XXX.|X...X|X...X|.XXX.|X...X|X...X|.XXX.",
            "9:.XXX.|X...X|X...X|.XXXX|....X|...X.|.XX..",
            "A:.XXX.|X...X|X...X|XXXXX|X...X|X...X|X...X",
            "B:XXXX.|X...X|X...X|XXXX.|X...X|X...X|XXXX.",
            "C:.XXX.|X...X|X....|X....|X....|X...X|.XXX.",
            "D:XXXX.|X...X|X...X|X...X|X...X|X...X|XXXX.",
            "E:XXXXX|X....|X....|XXXX.|X....|X....|XXXXX",
            "F:XXXXX|X....|X....|XXXX.|X....|X....|X....",
            "G:.XXX.|X...X|X....|X.XXX|X...X|X...X|.XXXX",
            "H:X...X|X...X|X...X|XXXXX|X...X|X...X|X...X",
            "I:.XXX.|..X..|..X..|..X..|..X..|..X..|.XXX.",
            "J:..XXX|...X.|...X.|...X.|...X.|X..X.|.XX..",
            "K:X...X|X..X.|X.X..|XX...|X.X..|X..X.|X...X",
            "L:X....|X....|X....|X....|X....|X....|XXXXX",
            "M:X...X|XX.XX|X.X.X|X.X.X|X...X|X...X|X...X",
            "N:X...X|XX..X|X.X.X|X..XX|X...X|X...X|X...X",
            "O:.XXX.|X...X|X...X|X...X|X...X|X...X|.XXX.",
            "P:XXXX.|X...X|X...X|XXXX.|X....|X....|X....",
            "Q:.XXX.|X...X|X...X|X...X|X.X.X|X..X.|.XX.X",
            "R:XXXX.|X...X|X...X|XXXX.|X.X..|X..X.|X...X",
            "S:.XXXX|X....|X....|.XXX.|....X|....X|XXXX.",
            "T:XXXXX|..X..|..X..|..X..|..X..|..X..|..X..",
            "U:X...X|X...X|X...X|X...X|X...X|X...X|.XXX.",
            "V:X...X|X...X|X...X|X...X|X...X|.X.X.|..X..",
            "W:X...X|X...X|X...X|X.X.X|X.X.X|XX.XX|X...X",
            "X:X...X|X...X|.X.X.|..X..|.X.X.|X...X|X...X",
            "Y:X...X|X...X|.X.X.|..X..|..X..|..X..|..X..",
            "Z:XXXXX|....X|...X.|..X..|.X...|X....|XXXXX",
            "<:...X.|..X..|.X...|X....|.X...|..X..|...X.",
            ">:.X...|..X..|...X.|....X|...X.|..X..|.X...",
            "-:.....|.....|.....|XXXXX|.....|.....|.....",
            "=:.....|.....|XXXXX|.....|XXXXX|.....|.....",
            "?:.XXX.|X...X|....X|...X.|..X..|.....|..X..",
            "*:.....|X.X.X|.XXX.|XXXXX|.XXX.|X.X.X|.....",
            "!:..X..|..X..|..X..|..X..|..X..|.....|..X..",
            "/:....X|...X.|...X.|..X..|.X...|.X...|X....",
            "::.....|.XX..|.XX..|.....|.XX..|.XX..|.....",
            "..:.....|.....|.....|.....|.....|.XX..|.XX..",
        };

        /// <summary>Builds a sprite from rows of text: 'X' = pixel, anything else = transparent.</summary>
        public static Sprite Make(GraphicsDevice gd, params string[] rows)
        {
            int h = rows.Length, w = 0;
            foreach (var r in rows) w = Math.Max(w, r.Length);
            var data = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < rows[y].Length; x++)
                    if (rows[y][x] == 'X') data[y * w + x] = Color.White;
            var tex = new Texture2D(gd, w, h);
            tex.SetData(data);
            return new Sprite(tex);
        }

        public static void Load(GraphicsDevice gd)
        {
            Pixel = new Texture2D(gd, 1, 1);
            Pixel.SetData(new[] { Color.White });

            // ---- the three invader types, two animation frames each ----
            Aliens = new[]
            {
                // squid (8x8) - 30 points
                new[]
                {
                    Make(gd,
                        "...XX...",
                        "..XXXX..",
                        ".XXXXXX.",
                        "XX.XX.XX",
                        "XXXXXXXX",
                        "..X..X..",
                        ".X.XX.X.",
                        "X.X..X.X"),
                    Make(gd,
                        "...XX...",
                        "..XXXX..",
                        ".XXXXXX.",
                        "XX.XX.XX",
                        "XXXXXXXX",
                        ".X.XX.X.",
                        "X......X",
                        ".X....X."),
                },
                // crab (11x8) - 20 points
                new[]
                {
                    Make(gd,
                        "..X.....X..",
                        "...X...X...",
                        "..XXXXXXX..",
                        ".XX.XXX.XX.",
                        "XXXXXXXXXXX",
                        "X.XXXXXXX.X",
                        "X.X.....X.X",
                        "...XX.XX..."),
                    Make(gd,
                        "..X.....X..",
                        "X..X...X..X",
                        "X.XXXXXXX.X",
                        "XXX.XXX.XXX",
                        "XXXXXXXXXXX",
                        ".XXXXXXXXX.",
                        "..X.....X..",
                        ".X.......X."),
                },
                // octopus (12x8) - 10 points
                new[]
                {
                    Make(gd,
                        "....XXXX....",
                        ".XXXXXXXXXX.",
                        "XXXXXXXXXXXX",
                        "XXX..XX..XXX",
                        "XXXXXXXXXXXX",
                        "...XX..XX...",
                        "..XX.XX.XX..",
                        "XX........XX"),
                    Make(gd,
                        "....XXXX....",
                        ".XXXXXXXXXX.",
                        "XXXXXXXXXXXX",
                        "XXX..XX..XXX",
                        "XXXXXXXXXXXX",
                        "..XXX..XXX..",
                        ".XX..XX..XX.",
                        "..XX....XX.."),
                },
            };

            Ufo = Make(gd,
                ".....XXXXXX.....",
                "...XXXXXXXXXX...",
                "..XXXXXXXXXXXX..",
                ".XX.XX.XX.XX.XX.",
                "XXXXXXXXXXXXXXXX",
                "..XXX..XX..XXX..",
                "...X........X...");

            UfoBoom = Make(gd,
                ".X..X.X..X.X..X.",
                "..X..XXXXX..X...",
                "X..XXXXXXXXX..X.",
                ".XXX..XXX..XXX..",
                "..XXXXXXXXXXXX..",
                "X..XX.XXX.XX..X.",
                "..X..X...X..X...");

            Player = Make(gd,
                "......X......",
                ".....XXX.....",
                ".....XXX.....",
                ".XXXXXXXXXXX.",
                "XXXXXXXXXXXXX",
                "XXXXXXXXXXXXX",
                "XXXXXXXXXXXXX",
                "XXXXXXXXXXXXX");

            PlayerBoom1 = Make(gd,
                "X...X..X....X",
                ".X...X..X..X.",
                "..X.XXXX.X...",
                ".X.XXXXXXX.X.",
                "X.XXXXXXXXX.X",
                "..XXXXXXXXX..",
                ".XXXXXXXXXXX.",
                "XXXXXXXXXXXXX");

            PlayerBoom2 = Make(gd,
                "..X.....X....",
                "X...X.X...X..",
                "...X.X.X.X..X",
                ".X..XXXXX..X.",
                "..XXX.XXXXX..",
                ".X.XXXXXXXX.X",
                "..XXXXXXXXXX.",
                ".XXXXXXXXXXXX");

            AlienBoom = Make(gd,
                "....X...X....",
                "X....X.X....X",
                ".X..XXXXX..X.",
                "..XX.....XX..",
                "XX.........XX",
                "..XX.....XX..",
                ".X..X...X..X.",
                "X..X.....X..X");

            ShotBoom = Make(gd,
                "X..X..X.",
                ".X.XX.X.",
                "..XXXX..",
                "XXX..XXX",
                "..XXXX..",
                ".X.XX.X.",
                "X..X..X.");

            GroundBoom = Make(gd,
                "X..XX..X",
                ".XXXXXX.",
                "XXXXXXXX");

            // two kinds of alien bullet, each with two animation frames (3x6 pixels)
            AlienBullets = new[]
            {
                new[]
                {
                    Make(gd, ".X.", "X..", ".X.", "..X", ".X.", "X.."),   // squiggle
                    Make(gd, ".X.", "..X", ".X.", "X..", ".X.", "..X"),
                },
                new[]
                {
                    Make(gd, ".X.", ".X.", "XXX", ".X.", ".X.", ".X."),   // cross
                    Make(gd, ".X.", ".X.", ".X.", "XXX", ".X.", ".X."),
                },
            };

            // bitmap font
            foreach (var g in FontData)
                Font[g[0]] = Make(gd, g.Substring(2).Split('|'));
        }
    }

    // ---------------------------------------------------------------------------------
    //  The game
    // ---------------------------------------------------------------------------------
    sealed class InvadersGame : Game
    {
        // ---- virtual arcade screen (scaled up with point sampling) ----
        const int W = 224, H = 256;
        const int GroundY = 228;
        const int PlayerY = 218;
        const int UfoY = 30;
        const int TopLimit = 28;
        const int ShieldW = 22, ShieldH = 16, ShieldY = 180;
        const int Cols = 11, Rows = 5;

        static readonly Color Green = new Color(80, 255, 80);
        static readonly Color Red = new Color(255, 70, 70);
        static readonly Color Magenta = new Color(255, 110, 220);
        static readonly Color[] AlienColors =
        {
            new Color(255, 110, 220),   // squid
            new Color(100, 230, 255),   // crab
            new Color(255, 235, 110),   // octopus
        };

        enum State { Title, Playing, PlayerDying, WaveClear, GameOver }

        sealed class Alien { public int Row, Col, Type; public bool Alive; }
        sealed class Bullet { public float X, Y, Anim; public int Type; }
        sealed class Shield { public int X, Y; public Color[] Px; public Texture2D Tex; public bool Dirty; }
        sealed class Fx { public Sprite Spr; public string Text; public float X, Y, Life, Delay; public Color Col; }

        readonly GraphicsDeviceManager gfx;
        SpriteBatch sb;
        RenderTarget2D target;
        readonly Random rng = new Random();

        KeyboardState kb, prevKb;
        GamePadState pad, prevPad;
        bool paused;

        State state = State.Title;
        float stateTimer, blink;
        int score, hiScore, lives, wave;
        bool bonusGiven;

        // player
        float playerX;
        bool pbActive;
        float pbX, pbY;

        // invaders
        readonly Alien[] aliens = new Alien[Cols * Rows];
        int formX, formY, dir = 1, frame, aliveCount;
        float stepTimer, fireTimer;
        readonly List<Bullet> abullets = new List<Bullet>();

        // mystery ship
        bool ufoActive;
        float ufoX, ufoTimer;
        int ufoDir;

        Shield[] shields = new Shield[0];
        readonly List<Fx> fxs = new List<Fx>();

        public InvadersGame()
        {
            gfx = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = W * 3,
                PreferredBackBufferHeight = H * 3,
            };
            Window.Title = "Space Invaders";
            Window.AllowUserResizing = true;
            IsMouseVisible = false;
        }

        protected override void LoadContent()
        {
            sb = new SpriteBatch(GraphicsDevice);
            target = new RenderTarget2D(GraphicsDevice, W, H);
            Art.Load(GraphicsDevice);
        }

        // ------------------------------------------------------------------ helpers
        static bool Overlap(float ax, float ay, float aw, float ah, float bx, float by, float bw, float bh)
        {
            return ax < bx + bw && ax + aw > bx && ay < by + bh && ay + ah > by;
        }

        float Rand(float a, float b) { return a + (float)rng.NextDouble() * (b - a); }
        bool Pressed(Keys k) { return kb.IsKeyDown(k) && prevKb.IsKeyUp(k); }

        bool StartPressed()
        {
            return Pressed(Keys.Enter) || Pressed(Keys.Space)
                || (pad.Buttons.Start == ButtonState.Pressed && prevPad.Buttons.Start == ButtonState.Released)
                || (pad.Buttons.A == ButtonState.Pressed && prevPad.Buttons.A == ButtonState.Released);
        }

        static int AlienW(Alien a) { return Art.Aliens[a.Type][0].W; }
        int AlienX(Alien a) { return formX + a.Col * 16 + (16 - AlienW(a)) / 2; }
        int AlienY(Alien a) { return formY + a.Row * 16; }

        void AddFx(Sprite spr, float x, float y, float life, Color c, float delay = 0f, string text = null)
        {
            fxs.Add(new Fx { Spr = spr, Text = text, X = x, Y = y, Life = life, Col = c, Delay = delay });
        }

        void AddScore(int pts)
        {
            score += pts;
            if (score > hiScore) hiScore = score;
            if (!bonusGiven && score >= 1500)
            {
                bonusGiven = true;
                lives++;
            }
        }

        // ------------------------------------------------------------------ game flow
        void NewGame()
        {
            score = 0; lives = 3; wave = 1; bonusGiven = false; paused = false;
            playerX = (W - Art.Player.W) / 2f;
            fxs.Clear();
            StartWave();
            state = State.Playing;
        }

        void StartWave()
        {
            formX = 24;
            formY = 60 + Math.Min(wave - 1, 5) * 8;   // later waves start lower
            dir = 1; frame = 0;
            stepTimer = 0.6f;
            fireTimer = 1.2f;
            for (int i = 0; i < aliens.Length; i++)
            {
                int row = i / Cols;
                aliens[i] = new Alien { Row = row, Col = i % Cols, Type = row == 0 ? 0 : (row < 3 ? 1 : 2), Alive = true };
            }
            aliveCount = aliens.Length;
            abullets.Clear();
            pbActive = false;
            ufoActive = false;
            ufoTimer = Rand(15f, 25f);
            BuildShields();
        }

        void BuildShields()
        {
            foreach (var old in shields) old.Tex.Dispose();
            shields = new Shield[4];
            for (int i = 0; i < 4; i++)
            {
                var s = new Shield { X = 33 + i * 45, Y = ShieldY, Px = new Color[ShieldW * ShieldH] };
                for (int y = 0; y < ShieldH; y++)
                    for (int x = 0; x < ShieldW; x++)
                    {
                        int inset = y < 4 ? 4 - y : 0;                       // chamfered top corners
                        bool solid = x >= inset && x < ShieldW - inset;
                        if (y >= 11)                                          // notch at the bottom
                        {
                            int hw = y == 11 ? 2 : (y == 12 ? 3 : 4);
                            if (x >= 11 - hw && x < 11 + hw) solid = false;
                        }
                        s.Px[y * ShieldW + x] = solid ? Color.White : Color.Transparent;
                    }
                s.Tex = new Texture2D(GraphicsDevice, ShieldW, ShieldH);
                s.Tex.SetData(s.Px);
                shields[i] = s;
            }
        }

        void KillPlayer(bool invaded)
        {
            lives = invaded ? 0 : lives - 1;
            state = State.PlayerDying;
            stateTimer = 1.6f;
            abullets.Clear();
            pbActive = false;
            ufoActive = false;
            ufoTimer = Rand(15f, 25f);
        }

        void Respawn()
        {
            playerX = (W - Art.Player.W) / 2f;
            fireTimer = 1.0f;
            state = State.Playing;
        }

        // ------------------------------------------------------------------ update
        protected override void Update(GameTime gameTime)
        {
            float dt = Math.Min((float)gameTime.ElapsedGameTime.TotalSeconds, 1f / 30f);
            prevKb = kb; kb = Keyboard.GetState();
            prevPad = pad; pad = GamePad.GetState(PlayerIndex.One);
            blink += dt;

            if (kb.IsKeyDown(Keys.Escape)) Exit();
            if (Pressed(Keys.F11)) gfx.ToggleFullScreen();

            switch (state)
            {
                case State.Title:
                    if (StartPressed()) NewGame();
                    break;

                case State.Playing:
                    if (Pressed(Keys.P) || (pad.Buttons.Start == ButtonState.Pressed && prevPad.Buttons.Start == ButtonState.Released))
                        paused = !paused;
                    if (!paused) UpdatePlaying(dt);
                    break;

                case State.PlayerDying:
                    stateTimer -= dt;
                    if (stateTimer <= 0)
                    {
                        if (lives <= 0) { state = State.GameOver; stateTimer = 1.5f; }
                        else Respawn();
                    }
                    break;

                case State.WaveClear:
                    stateTimer -= dt;
                    if (stateTimer <= 0) { wave++; StartWave(); state = State.Playing; }
                    break;

                case State.GameOver:
                    stateTimer -= dt;
                    if (stateTimer <= 0 && StartPressed()) state = State.Title;
                    break;
            }

            if (!(paused && state == State.Playing)) UpdateFx(dt);
            base.Update(gameTime);
        }

        void UpdateFx(float dt)
        {
            for (int i = fxs.Count - 1; i >= 0; i--)
            {
                var f = fxs[i];
                if (f.Delay > 0) { f.Delay -= dt; continue; }
                f.Life -= dt;
                if (f.Life <= 0) fxs.RemoveAt(i);
            }
        }

        void UpdatePlaying(float dt)
        {
            // ---- player movement & firing ----
            float move = 0;
            if (kb.IsKeyDown(Keys.Left) || kb.IsKeyDown(Keys.A) || pad.DPad.Left == ButtonState.Pressed || pad.ThumbSticks.Left.X < -0.3f) move -= 1;
            if (kb.IsKeyDown(Keys.Right) || kb.IsKeyDown(Keys.D) || pad.DPad.Right == ButtonState.Pressed || pad.ThumbSticks.Left.X > 0.3f) move += 1;
            playerX = MathHelper.Clamp(playerX + move * 95f * dt, 6, W - 6 - Art.Player.W);

            bool fire = kb.IsKeyDown(Keys.Space) || kb.IsKeyDown(Keys.Up) || kb.IsKeyDown(Keys.W) || pad.Buttons.A == ButtonState.Pressed;
            if (fire && !pbActive)
            {
                pbActive = true;
                pbX = (int)playerX + Art.Player.W / 2;
                pbY = PlayerY - 4;
            }

            if (pbActive) UpdatePlayerBullet(dt);

            // ---- invader march (speed depends on how many are left) ----
            stepTimer -= dt;
            if (stepTimer <= 0 && aliveCount > 0)
            {
                StepAliens();
                if (state != State.Playing) return;
                stepTimer = StepInterval();
            }

            UpdateUfo(dt);
            UpdateAlienFire(dt);
            UpdateAlienBullets(dt);
            if (state != State.Playing) return;

            if (aliveCount == 0)
            {
                state = State.WaveClear;
                stateTimer = 1.8f;
                abullets.Clear();
                ufoActive = false;
            }
        }

        float StepInterval()
        {
            float f = (aliveCount - 1) / (float)(Cols * Rows - 1);
            float t = 0.02f + 0.5f * f;
            return t * Math.Max(0.55f, 1f - 0.05f * (wave - 1));
        }

        void StepAliens()
        {
            int minL = int.MaxValue, maxR = int.MinValue;
            foreach (var a in aliens)
            {
                if (!a.Alive) continue;
                int x = AlienX(a);
                minL = Math.Min(minL, x);
                maxR = Math.Max(maxR, x + AlienW(a));
            }

            if ((dir > 0 && maxR + 2 > W - 6) || (dir < 0 && minL - 2 < 6)) { formY += 8; dir = -dir; }
            else formX += 2 * dir;

            frame ^= 1;

            AliensEatShields();

            foreach (var a in aliens)
                if (a.Alive && AlienY(a) + 8 >= PlayerY) { KillPlayer(true); return; }
        }

        void AliensEatShields()
        {
            foreach (var s in shields)
                foreach (var a in aliens)
                {
                    if (!a.Alive) continue;
                    int ax = AlienX(a), ay = AlienY(a), aw = AlienW(a);
                    int x0 = Math.Max(ax, s.X), x1 = Math.Min(ax + aw, s.X + ShieldW);
                    int y0 = Math.Max(ay, s.Y), y1 = Math.Min(ay + 8, s.Y + ShieldH);
                    if (x1 <= x0 || y1 <= y0) continue;
                    for (int y = y0; y < y1; y++)
                        for (int x = x0; x < x1; x++)
                            s.Px[(y - s.Y) * ShieldW + (x - s.X)] = Color.Transparent;
                    s.Dirty = true;
                }
        }

        // ---- player bullet ----
        void UpdatePlayerBullet(float dt)
        {
            pbY -= 230f * dt;

            if (pbY < TopLimit)
            {
                pbActive = false;
                AddFx(Art.ShotBoom, pbX - 3, TopLimit - 3, 0.25f, Red);
                return;
            }

            if (ufoActive && Overlap(pbX, pbY, 1, 4, ufoX, UfoY, Art.Ufo.W, Art.Ufo.H))
            {
                HitUfo();
                pbActive = false;
                return;
            }

            foreach (var a in aliens)
            {
                if (!a.Alive) continue;
                if (Overlap(pbX, pbY, 1, 4, AlienX(a), AlienY(a), AlienW(a), 8))
                {
                    KillAlien(a);
                    pbActive = false;
                    return;
                }
            }

            if (HitShield(pbX, pbY, 1, 4, true, 2)) pbActive = false;
        }

        void KillAlien(Alien a)
        {
            a.Alive = false;
            aliveCount--;
            AddScore(a.Type == 0 ? 30 : (a.Type == 1 ? 20 : 10));
            int aw = AlienW(a);
            AddFx(Art.AlienBoom, AlienX(a) + aw / 2 - Art.AlienBoom.W / 2, AlienY(a), 0.25f, AlienColors[a.Type]);
        }

        void HitUfo()
        {
            int[] table = { 50, 100, 100, 150, 150, 300 };
            int pts = table[rng.Next(table.Length)];
            ufoActive = false;
            ufoTimer = Rand(20f, 30f);
            AddScore(pts);
            AddFx(Art.UfoBoom, ufoX, UfoY, 0.35f, Red);
            AddFx(null, MathHelper.Clamp(ufoX, 0, W - 20), UfoY, 1.0f, Magenta, 0.35f, pts.ToString());
        }

        // ---- shields: pixel-perfect collision + ragged bite-shaped damage ----
        bool HitShield(float px, float py, int bw, int bh, bool goingUp, int radius)
        {
            int bx = (int)Math.Floor(px), by = (int)Math.Floor(py);
            foreach (var s in shields)
            {
                int x0 = Math.Max(bx, s.X), x1 = Math.Min(bx + bw, s.X + ShieldW);
                int y0 = Math.Max(by, s.Y), y1 = Math.Min(by + bh, s.Y + ShieldH);
                if (x1 <= x0 || y1 <= y0) continue;

                for (int i = 0; i < y1 - y0; i++)
                {
                    int y = goingUp ? y1 - 1 - i : y0 + i;   // scan from the leading edge of the bullet
                    for (int x = x0; x < x1; x++)
                        if (s.Px[(y - s.Y) * ShieldW + (x - s.X)].A != 0)
                        {
                            Damage(s, x - s.X, y - s.Y, radius);
                            return true;
                        }
                }
            }
            return false;
        }

        void Damage(Shield s, int cx, int cy, int r)
        {
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                {
                    int d2 = dx * dx + dy * dy;
                    if (d2 > r * r) continue;
                    if (d2 > (r - 1) * (r - 1) && rng.Next(3) == 0) continue;   // ragged rim
                    int x = cx + dx, y = cy + dy;
                    if (x < 0 || y < 0 || x >= ShieldW || y >= ShieldH) continue;
                    s.Px[y * ShieldW + x] = Color.Transparent;
                }
            s.Dirty = true;
        }

        // ---- mystery ship ----
        void UpdateUfo(float dt)
        {
            if (!ufoActive)
            {
                ufoTimer -= dt;
                if (ufoTimer <= 0 && aliveCount > 7)
                {
                    ufoActive = true;
                    ufoDir = rng.Next(2) == 0 ? 1 : -1;
                    ufoX = ufoDir > 0 ? -Art.Ufo.W : W;
                }
                return;
            }

            ufoX += ufoDir * 45f * dt;
            if ((ufoDir > 0 && ufoX > W) || (ufoDir < 0 && ufoX < -Art.Ufo.W))
            {
                ufoActive = false;
                ufoTimer = Rand(20f, 30f);
            }
        }

        // ---- invader shooting ----
        void UpdateAlienFire(float dt)
        {
            fireTimer -= dt;
            if (fireTimer > 0) return;

            float waveF = Math.Max(0.45f, 1f - 0.07f * (wave - 1));
            fireTimer = (0.3f + 0.9f * aliveCount / (Cols * Rows) + (float)rng.NextDouble() * 0.5f) * waveF;

            int maxBullets = wave >= 4 ? 4 : 3;
            if (abullets.Count >= maxBullets) return;

            // candidates: the lowest living invader of each column
            var cands = new List<Alien>();
            for (int c = 0; c < Cols; c++)
                for (int r = Rows - 1; r >= 0; r--)
                {
                    var a = aliens[r * Cols + c];
                    if (a.Alive) { cands.Add(a); break; }
                }
            if (cands.Count == 0) return;

            Alien shooter = cands[rng.Next(cands.Count)];
            if (rng.Next(100) < 35)   // sometimes aim at the player
            {
                int px = (int)playerX + Art.Player.W / 2, best = int.MaxValue;
                foreach (var a in cands)
                {
                    int d = Math.Abs(AlienX(a) + AlienW(a) / 2 - px);
                    if (d < best) { best = d; shooter = a; }
                }
            }

            abullets.Add(new Bullet
            {
                X = AlienX(shooter) + AlienW(shooter) / 2 - 1,
                Y = AlienY(shooter) + 8,
                Type = rng.Next(2),
            });
        }

        void UpdateAlienBullets(float dt)
        {
            float speed = 100f + 8f * Math.Min(wave - 1, 8);
            for (int i = abullets.Count - 1; i >= 0; i--)
            {
                var b = abullets[i];
                b.Y += speed * dt;
                b.Anim += dt;

                // bullets cancel each other out
                if (pbActive && Overlap(pbX, pbY, 1, 4, b.X, b.Y, 3, 6))
                {
                    pbActive = false;
                    abullets.RemoveAt(i);
                    AddFx(Art.ShotBoom, b.X - 3, b.Y, 0.2f, Color.White);
                    continue;
                }

                if (HitShield(b.X, b.Y, 3, 6, false, 3)) { abullets.RemoveAt(i); continue; }

                if (Overlap(b.X, b.Y, 3, 6, playerX, PlayerY, Art.Player.W, Art.Player.H))
                {
                    abullets.RemoveAt(i);
                    KillPlayer(false);
                    return;
                }

                if (b.Y + 6 >= GroundY)
                {
                    abullets.RemoveAt(i);
                    AddFx(Art.GroundBoom, b.X - 3, GroundY - Art.GroundBoom.H, 0.25f, Green);
                }
            }
        }

        // ------------------------------------------------------------------ drawing
        protected override void Draw(GameTime gameTime)
        {
            foreach (var s in shields)
                if (s.Dirty) { s.Tex.SetData(s.Px); s.Dirty = false; }

            // 1) render the 224x256 arcade screen
            GraphicsDevice.SetRenderTarget(target);
            GraphicsDevice.Clear(Color.Black);
            sb.Begin(samplerState: SamplerState.PointClamp);
            DrawHud();
            sb.Draw(Art.Pixel, new Rectangle(0, GroundY, W, 1), Green);
            if (state == State.Title) DrawTitle(); else DrawWorld();
            sb.End();

            // 2) scale it (integer scale when possible) into the window with letterboxing
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.Black);
            var vp = GraphicsDevice.Viewport;
            float scale = Math.Min(vp.Width / (float)W, vp.Height / (float)H);
            if (scale >= 1f) scale = (float)Math.Floor(scale);
            int dw = (int)(W * scale), dh = (int)(H * scale);
            var dest = new Rectangle((vp.Width - dw) / 2, (vp.Height - dh) / 2, dw, dh);
            sb.Begin(samplerState: SamplerState.PointClamp);
            sb.Draw(target, dest, Color.White);
            sb.End();

            base.Draw(gameTime);
        }

        void DrawSprite(Sprite s, float x, float y, Color c)
        {
            sb.Draw(s.Tex, new Vector2((int)Math.Round(x), (int)Math.Round(y)), c);
        }

        // ---- bitmap font text ----
        static int TextW(string s, int scale) { return s.Length * 6 * scale - scale; }

        void Text(string s, int x, int y, Color c, int scale = 1)
        {
            foreach (char ch in s)
            {
                Sprite g;
                if (Art.Font.TryGetValue(ch, out g))
                    sb.Draw(g.Tex, new Vector2(x, y), null, c, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                x += 6 * scale;
            }
        }

        void TextC(string s, int y, Color c, int scale = 1)
        {
            Text(s, (W - TextW(s, scale)) / 2, y, c, scale);
        }

        void DrawHud()
        {
            Text("SCORE<1>", 8, 6, Color.White);
            Text(score.ToString("D4"), 16, 17, Color.White);
            Text("HI-SCORE", 88, 6, Color.White);
            Text(hiScore.ToString("D4"), 100, 17, Color.White);
            Text("WAVE", 193, 6, Color.White);
            Text(Math.Max(wave, 1).ToString("D2"), 199, 17, Color.White);

            if (state != State.Title)
            {
                Text(Math.Max(lives, 0).ToString(), 8, 240, Color.White);
                for (int i = 0; i < lives - 1; i++)
                    DrawSprite(Art.Player, 20 + i * 16, 239, Green);
            }
        }

        void DrawTitle()
        {
            TextC("PLAY", 56, Color.White);
            TextC("SPACE INVADERS", 72, Green, 2);
            TextC("*SCORE ADVANCE TABLE*", 108, Color.White);

            int tf = (int)(blink * 2) & 1;
            DrawSprite(Art.Ufo, 80 - Art.Ufo.W / 2, 124, Red);
            Text("= ? MYSTERY", 96, 124, Color.White);

            int[] pts = { 30, 20, 10 };
            for (int t = 0; t < 3; t++)
            {
                var spr = Art.Aliens[t][tf];
                int y = 140 + t * 14;
                DrawSprite(spr, 80 - spr.W / 2, y, AlienColors[t]);
                Text("= " + pts[t] + " POINTS", 96, y, Color.White);
            }

            if (((int)(blink * 2) & 1) == 0) TextC("PRESS ENTER TO START", 190, Color.White);
            TextC("<  > MOVE   SPACE FIRE", 208, Green);
            TextC("P PAUSE", 218, Green);
        }

        void DrawWorld()
        {
            // shields
            foreach (var s in shields)
                sb.Draw(s.Tex, new Vector2(s.X, s.Y), Green);

            // invaders
            foreach (var a in aliens)
                if (a.Alive)
                    DrawSprite(Art.Aliens[a.Type][frame], AlienX(a), AlienY(a), AlienColors[a.Type]);

            // mystery ship
            if (ufoActive) DrawSprite(Art.Ufo, ufoX, UfoY, Red);

            // bullets
            if (pbActive) sb.Draw(Art.Pixel, new Rectangle((int)pbX, (int)pbY, 1, 4), Color.White);
            foreach (var b in abullets)
                DrawSprite(Art.AlienBullets[b.Type][(int)(b.Anim * 12) & 1], b.X, b.Y, Color.White);

            // player
            if (state == State.Playing || state == State.WaveClear)
            {
                DrawSprite(Art.Player, playerX, PlayerY, Green);
            }
            else if (state == State.PlayerDying)
            {
                var boom = ((int)(stateTimer * 8) & 1) == 0 ? Art.PlayerBoom1 : Art.PlayerBoom2;
                DrawSprite(boom, playerX + Art.Player.W / 2f - boom.W / 2f, PlayerY, Green);
            }

            // explosions and score popups
            foreach (var f in fxs)
            {
                if (f.Delay > 0) continue;
                if (f.Spr != null) DrawSprite(f.Spr, f.X, f.Y, f.Col);
                if (f.Text != null) Text(f.Text, (int)f.X, (int)f.Y, f.Col);
            }

            // overlays
            if (state == State.WaveClear)
                TextC("WAVE " + (wave + 1), 120, Color.White);

            if (paused && state == State.Playing)
                TextC("PAUSED", 120, Color.White, 2);

            if (state == State.GameOver)
            {
                sb.Draw(Art.Pixel, new Rectangle(30, 100, 164, 44), Color.Black);
                TextC("GAME OVER", 108, Red, 2);
                if (stateTimer <= 0 && ((int)(blink * 2) & 1) == 0)
                    TextC("PRESS ENTER", 130, Color.White);
            }
        }
    }
}
