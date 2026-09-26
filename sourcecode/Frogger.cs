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
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Frogger
{
    // ------------------------------------------------------------------
    // Entry point
    // ------------------------------------------------------------------
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using var game = new Game1();
            game.Run();
        }
    }

    public enum GameState { Start, Playing, Dead, LevelComplete, GameOver }

    // Simple mutable frog state
    public class Frog
    {
        public float X;
        public int Row;
    }

    // A "lane" of moving objects (cars on the road, logs/turtles on the river)
    public class Lane
    {
        public int Row;
        public float Speed;      // pixels/sec, sign = direction
        public float ObjWidth;
        public float Gap;
        public bool IsRiver;
        public bool IsTurtle;
        public Color Color;
        public List<float> Positions = new List<float>();
    }

    public class Game1 : Game
    {
        // ---------------- Grid / window layout ----------------
        const int CellSize = 40;
        const int Cols = 15;
        const int Rows = 14;              // grid rows 0 (top/home) .. 13 (bottom/start)
        const int GridWidth = Cols * CellSize;   // 600
        const int GridHeight = Rows * CellSize;  // 560
        const int HUD_TOP = 50;
        const int HUD_BOTTOM = 50;
        const int StartRow = Rows - 1;    // 13

        static readonly HashSet<int> RiverRows = new HashSet<int> { 2, 3, 4, 5, 6 };
        static readonly HashSet<int> RoadRows = new HashSet<int> { 8, 9, 10, 11 };

        // ---------------- MonoGame plumbing ----------------
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Texture2D pixel;
        RasterizerState scissorRs;

        // ---------------- Game state ----------------
        GameState state;
        int score, lives, level;
        bool[] slotFilled = new bool[5];
        List<Lane> lanes = new List<Lane>();
        Frog frog = new Frog();
        int bestRow;                 // furthest (lowest) row reached this life, for scoring
        float timeLeft, maxTime = 25f;
        float deathTimer, levelCompleteTimer;
        KeyboardState prevKs;

        float StartX => (Cols / 2) * (float)CellSize;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            graphics.PreferredBackBufferWidth = GridWidth;
            graphics.PreferredBackBufferHeight = GridHeight + HUD_TOP + HUD_BOTTOM;
            Window.Title = "Frogger";
        }

        protected override void Initialize()
        {
            graphics.ApplyChanges();
            level = 1;
            lives = 3;
            score = 0;
            slotFilled = new bool[5];
            SetupLanes();
            frog.X = StartX;
            frog.Row = StartRow;
            bestRow = StartRow;
            state = GameState.Start;
            prevKs = Keyboard.GetState();
            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            pixel = new Texture2D(GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });
            scissorRs = new RasterizerState { ScissorTestEnable = true };
        }

        // ================================================================
        // LANES
        // ================================================================
        void SetupLanes()
        {
            lanes.Clear();
            var rand = new Random(12345 + level);

            int[] riverRows = { 2, 3, 4, 5, 6 };
            float baseRiverSpeed = 55 + level * 7;
            for (int i = 0; i < riverRows.Length; i++)
            {
                float speed = baseRiverSpeed + i * 12;
                if (i % 2 == 0) speed = -speed;
                bool turtle = (i == 1 || i == 3);
                var lane = new Lane
                {
                    Row = riverRows[i],
                    Speed = speed,
                    ObjWidth = turtle ? 110f : 130f,
                    Gap = turtle ? 70f : 90f,
                    IsRiver = true,
                    IsTurtle = turtle,
                    Color = turtle ? new Color(40, 150, 70) : new Color(120, 72, 20)
                };
                float spacing = lane.ObjWidth + lane.Gap;
                int count = (int)(GridWidth / spacing) + 2;
                float offset = (float)rand.NextDouble() * spacing;
                for (int j = 0; j < count; j++)
                    lane.Positions.Add(-lane.ObjWidth + offset + j * spacing);
                lanes.Add(lane);
            }

            int[] roadRows = { 8, 9, 10, 11 };
            float baseRoadSpeed = 42 + level * 5;
            Color[] carColors =
            {
                new Color(220,50,50), new Color(230,200,40),
                new Color(60,120,220), new Color(230,230,230)
            };
            for (int i = 0; i < roadRows.Length; i++)
            {
                float speed = baseRoadSpeed + i * 9;
                if (i % 2 == 0) speed = -speed;
                var lane = new Lane
                {
                    Row = roadRows[i],
                    Speed = speed,
                    ObjWidth = 60f,
                    Gap = 100f + (float)rand.NextDouble() * 60f,
                    IsRiver = false,
                    Color = carColors[i % carColors.Length]
                };
                float spacing = lane.ObjWidth + lane.Gap;
                int count = (int)(GridWidth / spacing) + 2;
                float offset = (float)rand.NextDouble() * spacing;
                for (int j = 0; j < count; j++)
                    lane.Positions.Add(-lane.ObjWidth + offset + j * spacing);
                lanes.Add(lane);
            }
        }

        void UpdateLanes(float dt)
        {
            foreach (var lane in lanes)
            {
                float spacing = lane.ObjWidth + lane.Gap;
                int count = lane.Positions.Count;
                for (int i = 0; i < count; i++)
                {
                    lane.Positions[i] += lane.Speed * dt;
                    if (lane.Speed > 0 && lane.Positions[i] > GridWidth)
                        lane.Positions[i] -= spacing * count;
                    else if (lane.Speed < 0 && lane.Positions[i] < -lane.ObjWidth)
                        lane.Positions[i] += spacing * count;
                }
            }
        }

        // ================================================================
        // GAME FLOW HELPERS
        // ================================================================
        void NewGame()
        {
            score = 0;
            lives = 3;
            level = 1;
            slotFilled = new bool[5];
            SetupLanes();
            ResetFrog();
            state = GameState.Playing;
        }

        void ResetFrog()
        {
            frog.X = StartX;
            frog.Row = StartRow;
            bestRow = StartRow;
            timeLeft = maxTime;
        }

        void KillFrog()
        {
            lives--;
            deathTimer = 1.1f;
            state = GameState.Dead;
        }

        Rectangle GetFrogRect()
        {
            return new Rectangle((int)frog.X + 4, RowToY(frog.Row) + 4, CellSize - 8, CellSize - 8);
        }

        static int RowToY(int row) => HUD_TOP + row * CellSize;

        bool KeyPressed(KeyboardState cur, Keys k) => cur.IsKeyDown(k) && !prevKs.IsKeyDown(k);

        void ToggleFullScreen()
        {
            if (!graphics.IsFullScreen)
            {
                graphics.PreferredBackBufferWidth = GraphicsDevice.DisplayMode.Width;
                graphics.PreferredBackBufferHeight = GraphicsDevice.DisplayMode.Height;
            }
            else
            {
                graphics.PreferredBackBufferWidth = GridWidth;
                graphics.PreferredBackBufferHeight = GridHeight + HUD_TOP + HUD_BOTTOM;
            }
            graphics.IsFullScreen = !graphics.IsFullScreen;
            graphics.ApplyChanges();
        }

        // ================================================================
        // UPDATE
        // ================================================================
        protected override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            var ks = Keyboard.GetState();

            if (ks.IsKeyDown(Keys.Escape)) Exit();
            if (KeyPressed(ks, Keys.F11)) ToggleFullScreen();

            UpdateLanes(dt);

            switch (state)
            {
                case GameState.Start:
                    if (KeyPressed(ks, Keys.Enter) || KeyPressed(ks, Keys.Space))
                        NewGame();
                    break;

                case GameState.Playing:
                    UpdatePlaying(ks, dt);
                    break;

                case GameState.Dead:
                    deathTimer -= dt;
                    if (deathTimer <= 0)
                    {
                        if (lives <= 0) state = GameState.GameOver;
                        else { ResetFrog(); state = GameState.Playing; }
                    }
                    break;

                case GameState.LevelComplete:
                    levelCompleteTimer -= dt;
                    if (levelCompleteTimer <= 0)
                    {
                        level++;
                        slotFilled = new bool[5];
                        SetupLanes();
                        ResetFrog();
                        state = GameState.Playing;
                    }
                    break;

                case GameState.GameOver:
                    if (KeyPressed(ks, Keys.Enter) || KeyPressed(ks, Keys.Space))
                        NewGame();
                    break;
            }

            prevKs = ks;
            base.Update(gameTime);
        }

        void UpdatePlaying(KeyboardState ks, float dt)
        {
            // --- discrete hop input ---
            if (KeyPressed(ks, Keys.Up))
            {
                if (frog.Row > 0)
                {
                    frog.Row--;
                    if (frog.Row < bestRow) { score += 10; bestRow = frog.Row; }
                }
            }
            if (KeyPressed(ks, Keys.Down))
            {
                if (frog.Row < StartRow) frog.Row++;
            }
            if (KeyPressed(ks, Keys.Left))
            {
                frog.X -= CellSize;
                if (frog.X < 0) frog.X = 0;
            }
            if (KeyPressed(ks, Keys.Right))
            {
                frog.X += CellSize;
                if (frog.X > GridWidth - CellSize) frog.X = GridWidth - CellSize;
            }

            // --- timer ---
            timeLeft -= dt;
            if (timeLeft <= 0) { KillFrog(); return; }

            var frogRect = GetFrogRect();

            if (RoadRows.Contains(frog.Row))
            {
                if (CheckRoadCollision(frogRect)) { KillFrog(); return; }
            }
            else if (RiverRows.Contains(frog.Row))
            {
                float? carry = GetRiverCarry(frog.Row, frogRect);
                if (carry == null) { KillFrog(); return; }
                frog.X += carry.Value * dt;
                if (frog.X < -20 || frog.X > GridWidth - CellSize + 20) { KillFrog(); return; }
                frog.X = MathHelper.Clamp(frog.X, -10, GridWidth - CellSize + 10);
            }
            else if (frog.Row == 0)
            {
                HandleHomeRow();
            }
        }

        bool CheckRoadCollision(Rectangle frogRect)
        {
            foreach (var lane in lanes)
            {
                if (lane.IsRiver || lane.Row != frog.Row) continue;
                foreach (var pos in lane.Positions)
                {
                    var rect = new Rectangle((int)pos, RowToY(lane.Row) + 6, (int)lane.ObjWidth, CellSize - 12);
                    if (rect.Intersects(frogRect)) return true;
                }
            }
            return false;
        }

        float? GetRiverCarry(int row, Rectangle frogRect)
        {
            foreach (var lane in lanes)
            {
                if (!lane.IsRiver || lane.Row != row) continue;
                foreach (var pos in lane.Positions)
                {
                    var rect = new Rectangle((int)pos, RowToY(lane.Row) + 4, (int)lane.ObjWidth, CellSize - 8);
                    if (rect.Intersects(frogRect)) return lane.Speed;
                }
            }
            return null;
        }

        void HandleHomeRow()
        {
            float centerX = frog.X + CellSize / 2f;
            int slotIndex = (int)(centerX / 120f);
            if (slotIndex < 0) slotIndex = 0;
            if (slotIndex > 4) slotIndex = 4;
            float gapStart = slotIndex * 120 + 40;
            float gapEnd = slotIndex * 120 + 80;

            if (centerX >= gapStart && centerX <= gapEnd && !slotFilled[slotIndex])
            {
                slotFilled[slotIndex] = true;
                score += 50;
                if (slotFilled.All(s => s))
                {
                    state = GameState.LevelComplete;
                    levelCompleteTimer = 3f;
                    score += 200;
                }
                else
                {
                    ResetFrog();
                }
            }
            else
            {
                KillFrog();
            }
        }

        // ================================================================
        // DRAW
        // ================================================================
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(10, 10, 15));

            int screenW = GridWidth;
            int screenH = GridHeight + HUD_TOP + HUD_BOTTOM;
            int pw = GraphicsDevice.PresentationParameters.BackBufferWidth;
            int ph = GraphicsDevice.PresentationParameters.BackBufferHeight;
            float scale = Math.Min(pw / (float)screenW, ph / (float)screenH);
            float ox = (pw - screenW * scale) * 0.5f;
            float oy = (ph - screenH * scale) * 0.5f;
            Matrix m = Matrix.CreateScale(scale, scale, 1f) * Matrix.CreateTranslation(ox, oy, 0f);

            GraphicsDevice.ScissorRectangle = Rectangle.Intersect(
                new Rectangle((int)Math.Round(ox), (int)Math.Round(oy),
                              (int)Math.Round(screenW * scale), (int)Math.Round(screenH * scale)),
                new Rectangle(0, 0, pw, ph));

            spriteBatch.Begin(samplerState: SamplerState.PointClamp, rasterizerState: scissorRs, transformMatrix: m);

            DrawBackground();
            DrawLaneObjects();
            DrawHome();

            if (state == GameState.Playing || state == GameState.Dead)
                DrawFrogSprite(new Vector2(frog.X, RowToY(frog.Row)), state == GameState.Dead);

            DrawHud();

            if (state == GameState.Start) DrawStartOverlay();
            else if (state == GameState.GameOver) DrawGameOverOverlay();
            else if (state == GameState.LevelComplete) DrawLevelCompleteOverlay();

            spriteBatch.End();

            base.Draw(gameTime);
        }

        void DrawBackground()
        {
            Color waterColor = new Color(25, 60, 190);
            Color roadColor = new Color(40, 40, 40);
            Color grassColor = new Color(20, 120, 40);

            for (int row = 0; row < Rows; row++)
            {
                Color c;
                if (row == 0) c = waterColor;
                else if (RiverRows.Contains(row)) c = waterColor;
                else if (RoadRows.Contains(row)) c = roadColor;
                else c = grassColor;

                DrawFilledRect(new Rectangle(0, RowToY(row), GridWidth, CellSize), c);
            }

            // dashed lane markers on the road
            foreach (int row in RoadRows)
            {
                int y = RowToY(row) + CellSize - 2;
                for (int x = 0; x < GridWidth; x += 40)
                    DrawFilledRect(new Rectangle(x + 8, y, 20, 2), Color.Yellow * 0.55f);
            }
        }

        void DrawLaneObjects()
        {
            foreach (var lane in lanes)
            {
                foreach (var pos in lane.Positions)
                {
                    if (lane.IsRiver)
                    {
                        if (lane.IsTurtle)
                            DrawTurtles(new Rectangle((int)pos, RowToY(lane.Row) + 4, (int)lane.ObjWidth, CellSize - 8), lane.Color);
                        else
                            DrawLog(new Rectangle((int)pos, RowToY(lane.Row) + 6, (int)lane.ObjWidth, CellSize - 12), lane.Color);
                    }
                    else
                    {
                        DrawCar(new Rectangle((int)pos, RowToY(lane.Row) + 6, (int)lane.ObjWidth, CellSize - 12), lane.Color);
                    }
                }
            }
        }

        void DrawHome()
        {
            Color hedgeColor = new Color(10, 90, 30);
            for (int slot = 0; slot < 5; slot++)
            {
                int baseX = slot * 120;
                DrawFilledRect(new Rectangle(baseX, RowToY(0), 40, CellSize), hedgeColor);
                DrawFilledRect(new Rectangle(baseX + 80, RowToY(0), 40, CellSize), hedgeColor);

                if (slotFilled[slot])
                {
                    DrawFilledRect(new Rectangle(baseX + 40, RowToY(0), 40, CellSize), new Color(20, 150, 60));
                    DrawFrogSprite(new Vector2(baseX + 40, RowToY(0)), false, 0.8f);
                }
            }
        }

        void DrawHud()
        {
            DrawFilledRect(new Rectangle(0, 0, GridWidth, HUD_TOP), Color.Black);
            DrawFilledRect(new Rectangle(0, GridHeight + HUD_TOP, GridWidth, HUD_BOTTOM), Color.Black);

            DrawText("SCORE " + score, new Vector2(10, 15), 2, Color.White);

            string levelText = "LEVEL " + level;
            DrawText(levelText, new Vector2(GridWidth / 2f - GetTextWidth(levelText, 2) / 2f, 15), 2, Color.Yellow);

            if (state == GameState.Playing)
            {
                float ratio = MathHelper.Clamp(timeLeft / maxTime, 0, 1);
                DrawFilledRect(new Rectangle(GridWidth - 160, 15, 150, 20), Color.DarkGray);
                Color barColor = ratio < 0.3f ? Color.Red : Color.LimeGreen;
                DrawFilledRect(new Rectangle(GridWidth - 160, 15, (int)(150 * ratio), 20), barColor);
            }

            DrawText("LIVES", new Vector2(10, GridHeight + HUD_TOP + 15), 2, Color.White);
            for (int i = 0; i < Math.Max(lives, 0); i++)
                DrawFrogSprite(new Vector2(110 + i * 35, GridHeight + HUD_TOP + 6), false, 0.65f);
        }

        void DrawStartOverlay()
        {
            int centerY = HUD_TOP + GridHeight / 2;
            string t1 = "FROGGER";
            DrawText(t1, new Vector2(GridWidth / 2f - GetTextWidth(t1, 6) / 2f, centerY - 70), 6, Color.LimeGreen);
            string t2 = "PRESS ENTER TO START";
            DrawText(t2, new Vector2(GridWidth / 2f - GetTextWidth(t2, 2) / 2f, centerY + 40), 2, Color.White);
            string t3 = "F11 FULLSCREEN   ESC QUIT";
            DrawText(t3, new Vector2(GridWidth / 2f - GetTextWidth(t3, 1) / 2f, centerY + 80), 1, Color.Gray);
        }

        void DrawGameOverOverlay()
        {
            int centerY = HUD_TOP + GridHeight / 2;
            string t1 = "GAME OVER";
            DrawText(t1, new Vector2(GridWidth / 2f - GetTextWidth(t1, 4) / 2f, centerY - 50), 4, Color.Red);
            string t2 = "PRESS ENTER TO RESTART";
            DrawText(t2, new Vector2(GridWidth / 2f - GetTextWidth(t2, 2) / 2f, centerY + 30), 2, Color.White);
        }

        void DrawLevelCompleteOverlay()
        {
            int centerY = HUD_TOP + GridHeight / 2;
            string t1 = "LEVEL COMPLETE";
            DrawText(t1, new Vector2(GridWidth / 2f - GetTextWidth(t1, 3) / 2f, centerY - 15), 3, Color.Yellow);
        }

        // ================================================================
        // PRIMITIVE DRAWING HELPERS (no external art assets)
        // ================================================================
        void DrawFilledRect(Rectangle rect, Color color)
        {
            spriteBatch.Draw(pixel, rect, color);
        }

        void DrawCircle(Vector2 center, float radius, Color color)
        {
            int r = (int)radius;
            if (r <= 0) return;
            for (int y = -r; y <= r; y++)
            {
                int dx = (int)Math.Sqrt(Math.Max(0, r * r - y * y));
                var rect = new Rectangle((int)(center.X - dx), (int)(center.Y + y), dx * 2, 1);
                spriteBatch.Draw(pixel, rect, color);
            }
        }

        void DrawFrogSprite(Vector2 topLeft, bool dead, float scale = 1f)
        {
            int size = (int)(CellSize * scale);
            Color body = dead ? new Color(150, 150, 150) : new Color(50, 190, 60);
            Vector2 center = topLeft + new Vector2(size / 2f, size / 2f);

            float legSize = size * 0.22f;
            Color legColor = body * 0.9f;
            DrawFilledRect(new Rectangle((int)(topLeft.X - legSize * 0.3f), (int)(topLeft.Y + size * 0.6f), (int)legSize, (int)legSize), legColor);
            DrawFilledRect(new Rectangle((int)(topLeft.X + size - legSize * 0.7f), (int)(topLeft.Y + size * 0.6f), (int)legSize, (int)legSize), legColor);

            DrawCircle(center, size * 0.42f, body);

            float eyeR = size * 0.12f;
            Vector2 eyeOffset = new Vector2(size * 0.18f, size * 0.22f);
            DrawCircle(center - new Vector2(eyeOffset.X, eyeOffset.Y), eyeR, Color.White);
            DrawCircle(center + new Vector2(eyeOffset.X, -eyeOffset.Y), eyeR, Color.White);
            DrawCircle(center - new Vector2(eyeOffset.X, eyeOffset.Y), eyeR * 0.45f, Color.Black);
            DrawCircle(center + new Vector2(eyeOffset.X, -eyeOffset.Y), eyeR * 0.45f, Color.Black);
        }

        void DrawCar(Rectangle rect, Color color)
        {
            DrawFilledRect(rect, color);
            DrawFilledRect(new Rectangle(rect.X + rect.Width / 4, rect.Y + 3, rect.Width / 2, Math.Max(1, rect.Height - 6)), Color.White * 0.5f);
            DrawFilledRect(new Rectangle(rect.X + 4, rect.Bottom - 5, 8, 6), Color.Black);
            DrawFilledRect(new Rectangle(rect.Right - 12, rect.Bottom - 5, 8, 6), Color.Black);
        }

        void DrawLog(Rectangle rect, Color color)
        {
            DrawFilledRect(rect, color);
            Color dark = color * 0.7f;
            for (int x = rect.X + 10; x < rect.Right - 5; x += 18)
                DrawFilledRect(new Rectangle(x, rect.Y + 2, 4, Math.Max(1, rect.Height - 4)), dark);
        }

        void DrawTurtles(Rectangle rect, Color shellColor)
        {
            int count = Math.Max(1, rect.Width / 40);
            float r = rect.Height / 2f;
            for (int i = 0; i < count; i++)
            {
                Vector2 c = new Vector2(rect.X + r + i * (rect.Width / (float)count), rect.Y + rect.Height / 2f);
                DrawCircle(c, r * 0.9f, shellColor);
                DrawCircle(c, r * 0.5f, shellColor * 0.7f);
            }
        }

        // ================================================================
        // BITMAP FONT (5x7, no SpriteFont / content files)
        // ================================================================
        void DrawText(string text, Vector2 pos, int scale, Color color)
        {
            float x = pos.X;
            foreach (char raw in text)
            {
                char ch = char.ToUpperInvariant(raw);
                if (Font.TryGetValue(ch, out var glyph))
                {
                    for (int row = 0; row < 7; row++)
                        for (int col = 0; col < 5; col++)
                            if (glyph[row][col] == '1')
                                DrawFilledRect(new Rectangle((int)(x + col * scale), (int)(pos.Y + row * scale), scale, scale), color);
                }
                x += 6 * scale;
            }
        }

        static int GetTextWidth(string text, int scale)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            return text.Length * 6 * scale - scale;
        }

        static readonly Dictionary<char, string[]> Font = new Dictionary<char, string[]>
        {
            ['0'] = new[] { "01110", "10001", "10011", "10101", "11001", "10001", "01110" },
            ['1'] = new[] { "00100", "01100", "00100", "00100", "00100", "00100", "01110" },
            ['2'] = new[] { "01110", "10001", "00001", "00010", "00100", "01000", "11111" },
            ['3'] = new[] { "11111", "00010", "00100", "00010", "00001", "10001", "01110" },
            ['4'] = new[] { "00010", "00110", "01010", "10010", "11111", "00010", "00010" },
            ['5'] = new[] { "11111", "10000", "11110", "00001", "00001", "10001", "01110" },
            ['6'] = new[] { "00110", "01000", "10000", "11110", "10001", "10001", "01110" },
            ['7'] = new[] { "11111", "00001", "00010", "00100", "01000", "01000", "01000" },
            ['8'] = new[] { "01110", "10001", "10001", "01110", "10001", "10001", "01110" },
            ['9'] = new[] { "01110", "10001", "10001", "01111", "00001", "00010", "01100" },

            ['A'] = new[] { "01110", "10001", "10001", "11111", "10001", "10001", "10001" },
            ['B'] = new[] { "11110", "10001", "10001", "11110", "10001", "10001", "11110" },
            ['C'] = new[] { "01111", "10000", "10000", "10000", "10000", "10000", "01111" },
            ['D'] = new[] { "11110", "10001", "10001", "10001", "10001", "10001", "11110" },
            ['E'] = new[] { "11111", "10000", "10000", "11110", "10000", "10000", "11111" },
            ['F'] = new[] { "11111", "10000", "10000", "11110", "10000", "10000", "10000" },
            ['G'] = new[] { "01111", "10000", "10000", "10111", "10001", "10001", "01111" },
            ['H'] = new[] { "10001", "10001", "10001", "11111", "10001", "10001", "10001" },
            ['I'] = new[] { "01110", "00100", "00100", "00100", "00100", "00100", "01110" },
            ['J'] = new[] { "00111", "00010", "00010", "00010", "00010", "10010", "01100" },
            ['K'] = new[] { "10001", "10010", "10100", "11000", "10100", "10010", "10001" },
            ['L'] = new[] { "10000", "10000", "10000", "10000", "10000", "10000", "11111" },
            ['M'] = new[] { "10001", "11011", "10101", "10101", "10001", "10001", "10001" },
            ['N'] = new[] { "10001", "11001", "10101", "10101", "10011", "10001", "10001" },
            ['O'] = new[] { "01110", "10001", "10001", "10001", "10001", "10001", "01110" },
            ['P'] = new[] { "11110", "10001", "10001", "11110", "10000", "10000", "10000" },
            ['Q'] = new[] { "01110", "10001", "10001", "10001", "10101", "10010", "01101" },
            ['R'] = new[] { "11110", "10001", "10001", "11110", "10100", "10010", "10001" },
            ['S'] = new[] { "01111", "10000", "10000", "01110", "00001", "00001", "11110" },
            ['T'] = new[] { "11111", "00100", "00100", "00100", "00100", "00100", "00100" },
            ['U'] = new[] { "10001", "10001", "10001", "10001", "10001", "10001", "01110" },
            ['V'] = new[] { "10001", "10001", "10001", "10001", "10001", "01010", "00100" },
            ['W'] = new[] { "10001", "10001", "10001", "10101", "10101", "10101", "01010" },
            ['X'] = new[] { "10001", "10001", "01010", "00100", "01010", "10001", "10001" },
            ['Y'] = new[] { "10001", "10001", "01010", "00100", "00100", "00100", "00100" },
            ['Z'] = new[] { "11111", "00001", "00010", "00100", "01000", "10000", "11111" },

            [' '] = new[] { "00000", "00000", "00000", "00000", "00000", "00000", "00000" },
            [':'] = new[] { "00000", "00100", "00000", "00000", "00100", "00000", "00000" },
            ['-'] = new[] { "00000", "00000", "00000", "11111", "00000", "00000", "00000" },
            ['!'] = new[] { "00100", "00100", "00100", "00100", "00100", "00000", "00100" },
            ['\''] = new[] { "00100", "00100", "00000", "00000", "00000", "00000", "00000" },
        };
    }
}