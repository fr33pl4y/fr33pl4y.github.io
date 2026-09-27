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

namespace ArkanoidClone
{
    // ------------------------------------------------------------------------------
    // Entry point
    // ------------------------------------------------------------------------------
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using var game = new ArkanoidGame();
            game.Run();
        }
    }

    // ------------------------------------------------------------------------------
    // Small data types
    // ------------------------------------------------------------------------------
    public enum GameState { Menu, Playing, GameOver }

    public enum PowerUpType { None, Slow, Laser, Catch, Break, Expand, Player, Disrupt }

    public class Brick
    {
        public Rectangle Bounds;
        public Color Color;
        public int HitsRemaining;
        public int Points;
        public bool Indestructible;
        public bool Alive = true;
    }

    public class PowerUpCapsule
    {
        public Vector2 Position; // top-left
        public PowerUpType Type;
        public Rectangle Bounds => new Rectangle((int)Position.X, (int)Position.Y, 24, 12);
    }

    public class Ball
    {
        public Vector2 Position;
        public Vector2 Direction = new Vector2(0f, -1f); // unit vector
        public float Speed;
        public float Radius = 5f;
        public bool Stuck = true;
        public bool Alive = true;
        public float StickOffset;
    }

    // ------------------------------------------------------------------------------
    // Hand-rolled 5x7 bitmap font (no MonoGame SpriteFont used anywhere)
    // ------------------------------------------------------------------------------
    public static class BitmapFont
    {
        private const int CharWidth = 5;
        private const int CharHeight = 7;

        private static readonly Dictionary<char, string[]> Glyphs = new Dictionary<char, string[]>
        {
            ['A'] = new[] { ".XXX.", "X...X", "X...X", "XXXXX", "X...X", "X...X", "X...X" },
            ['B'] = new[] { "XXXX.", "X...X", "X...X", "XXXX.", "X...X", "X...X", "XXXX." },
            ['C'] = new[] { ".XXXX", "X....", "X....", "X....", "X....", "X....", ".XXXX" },
            ['D'] = new[] { "XXXX.", "X...X", "X...X", "X...X", "X...X", "X...X", "XXXX." },
            ['E'] = new[] { "XXXXX", "X....", "X....", "XXXX.", "X....", "X....", "XXXXX" },
            ['F'] = new[] { "XXXXX", "X....", "X....", "XXXX.", "X....", "X....", "X...." },
            ['G'] = new[] { ".XXXX", "X....", "X....", "X.XXX", "X...X", "X...X", ".XXXX" },
            ['H'] = new[] { "X...X", "X...X", "X...X", "XXXXX", "X...X", "X...X", "X...X" },
            ['I'] = new[] { "XXXXX", "..X..", "..X..", "..X..", "..X..", "..X..", "XXXXX" },
            ['J'] = new[] { "..XXX", "...X.", "...X.", "...X.", "...X.", "X..X.", ".XX.." },
            ['K'] = new[] { "X...X", "X..X.", "X.X..", "XX...", "X.X..", "X..X.", "X...X" },
            ['L'] = new[] { "X....", "X....", "X....", "X....", "X....", "X....", "XXXXX" },
            ['M'] = new[] { "X...X", "XX.XX", "X.X.X", "X...X", "X...X", "X...X", "X...X" },
            ['N'] = new[] { "X...X", "XX..X", "X.X.X", "X..XX", "X...X", "X...X", "X...X" },
            ['O'] = new[] { ".XXX.", "X...X", "X...X", "X...X", "X...X", "X...X", ".XXX." },
            ['P'] = new[] { "XXXX.", "X...X", "X...X", "XXXX.", "X....", "X....", "X...." },
            ['Q'] = new[] { ".XXX.", "X...X", "X...X", "X...X", "X.X.X", "X..X.", ".XX.X" },
            ['R'] = new[] { "XXXX.", "X...X", "X...X", "XXXX.", "X.X..", "X..X.", "X...X" },
            ['S'] = new[] { ".XXXX", "X....", "X....", ".XXX.", "....X", "....X", "XXXX." },
            ['T'] = new[] { "XXXXX", "..X..", "..X..", "..X..", "..X..", "..X..", "..X.." },
            ['U'] = new[] { "X...X", "X...X", "X...X", "X...X", "X...X", "X...X", ".XXX." },
            ['V'] = new[] { "X...X", "X...X", "X...X", "X...X", "X...X", ".X.X.", "..X.." },
            ['W'] = new[] { "X...X", "X...X", "X...X", "X.X.X", "X.X.X", "X.X.X", ".X.X." },
            ['X'] = new[] { "X...X", "X...X", ".X.X.", "..X..", ".X.X.", "X...X", "X...X" },
            ['Y'] = new[] { "X...X", "X...X", ".X.X.", "..X..", "..X..", "..X..", "..X.." },
            ['Z'] = new[] { "XXXXX", "....X", "...X.", "..X..", ".X...", "X....", "XXXXX" },
            ['0'] = new[] { ".XXX.", "X...X", "X..XX", "X.X.X", "XX..X", "X...X", ".XXX." },
            ['1'] = new[] { "..X..", ".XX..", "..X..", "..X..", "..X..", "..X..", "XXXXX" },
            ['2'] = new[] { ".XXX.", "X...X", "....X", "...X.", "..X..", ".X...", "XXXXX" },
            ['3'] = new[] { ".XXX.", "X...X", "....X", "..XX.", "....X", "X...X", ".XXX." },
            ['4'] = new[] { "...X.", "..XX.", ".X.X.", "X..X.", "XXXXX", "...X.", "...X." },
            ['5'] = new[] { "XXXXX", "X....", "XXXX.", "....X", "....X", "X...X", ".XXX." },
            ['6'] = new[] { "..XX.", ".X...", "X....", "XXXX.", "X...X", "X...X", ".XXX." },
            ['7'] = new[] { "XXXXX", "....X", "...X.", "..X..", ".X...", ".X...", ".X..." },
            ['8'] = new[] { ".XXX.", "X...X", "X...X", ".XXX.", "X...X", "X...X", ".XXX." },
            ['9'] = new[] { ".XXX.", "X...X", "X...X", ".XXXX", "....X", "...X.", ".XX.." },
            [' '] = new[] { ".....", ".....", ".....", ".....", ".....", ".....", "....." },
            ['-'] = new[] { ".....", ".....", ".....", "XXXXX", ".....", ".....", "....." },
            [':'] = new[] { ".....", "..X..", ".....", ".....", "..X..", ".....", "....." },
            ['!'] = new[] { "..X..", "..X..", "..X..", "..X..", "..X..", ".....", "..X.." },
            ['.'] = new[] { ".....", ".....", ".....", ".....", ".....", ".....", "..X.." },
        };

        public static void DrawText(SpriteBatch sb, Texture2D pixel, string text, Vector2 position, float pixelSize, Color color, float spacingPixels = 1f)
        {
            float cursorX = position.X;
            foreach (char raw in text)
            {
                char c = char.ToUpperInvariant(raw);
                if (!Glyphs.TryGetValue(c, out var glyph))
                    glyph = Glyphs[' '];

                for (int row = 0; row < CharHeight; row++)
                {
                    string line = glyph[row];
                    for (int col = 0; col < CharWidth; col++)
                    {
                        if (line[col] == 'X')
                        {
                            var rect = new Rectangle(
                                (int)(cursorX + col * pixelSize),
                                (int)(position.Y + row * pixelSize),
                                (int)Math.Ceiling(pixelSize),
                                (int)Math.Ceiling(pixelSize));
                            sb.Draw(pixel, rect, color);
                        }
                    }
                }
                cursorX += CharWidth * pixelSize + spacingPixels * pixelSize;
            }
        }

        public static Vector2 MeasureText(string text, float pixelSize, float spacingPixels = 1f)
        {
            float width = text.Length * (CharWidth * pixelSize + spacingPixels * pixelSize);
            float height = CharHeight * pixelSize;
            return new Vector2(width, height);
        }
    }

    // ------------------------------------------------------------------------------
    // Main game class
    // ------------------------------------------------------------------------------
    public class ArkanoidGame : Game
    {
        private readonly GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        // Procedurally generated textures (no external assets)
        private Texture2D pixel;
        private Texture2D circleTex;
        private Texture2D topWallTex;
        private Texture2D sideWallTex;
        private RenderTarget2D sceneTarget;

        // Virtual canvas (game is always drawn at this fixed resolution, then scaled)
        private const int ScreenWidth = 448;
        private const int ScreenHeight = 640;

        private const int HudHeight = 64;
        private const int WallThick = 32;
        private const int FieldTop = HudHeight + WallThick;      // 96
        private const int FieldLeft = WallThick;                 // 32
        private const int FieldRight = ScreenWidth - WallThick;   // 416
        private const int FieldBottom = 608;

        private const int Cols = 12;
        private const int Rows = 8;
        private const int BrickW = 32;
        private const int BrickH = 16;
        private const int BrickAreaTop = FieldTop + 8;

        private const int PaddleY = 560;
        private const int PaddleHeight = 12;
        private const float PaddleBaseWidth = 56f;
        private const float PaddleExpandedWidth = 100f;
        private const float PaddleSpeed = 300f;

        // Game state
        private GameState state = GameState.Menu;
        private int score;
        private int highScore = 50000;
        private int lives;
        private int level;
        private bool inTransition;
        private float levelTransitionTimer;
        private float blinkTimer;

        private float paddleX;
        private float paddleWidth = PaddleBaseWidth;

        private bool expandActive; private float expandTimer;
        private bool laserActive; private float laserTimer; private float laserCooldown;
        private bool catchActive; private float catchTimer;
        private bool slowActive; private float slowTimer;
        private float speedMultiplier = 1f;
        private bool lifePillUsedThisLife;
        private PowerUpType lastPowerUpType = PowerUpType.None;

        private const float BallBaseSpeed = 190f;

        private readonly List<Ball> balls = new List<Ball>();
        private readonly List<Brick> bricks = new List<Brick>();
        private readonly List<PowerUpCapsule> capsules = new List<PowerUpCapsule>();
        private readonly List<Vector2> laserBolts = new List<Vector2>();

        private readonly Random rng = new Random();
        private KeyboardState prevKb;

        private static readonly Dictionary<char, (Color Color, int Points)> BrickInfo = new Dictionary<char, (Color, int)>
        {
            ['W'] = (Color.White, 50),
            ['O'] = (Color.Orange, 60),
            ['C'] = (Color.Cyan, 70),
            ['G'] = (Color.LimeGreen, 90),
            ['R'] = (Color.Red, 100),
            ['B'] = (Color.DodgerBlue, 110),
            ['V'] = (Color.Violet, 120),
            ['Y'] = (Color.Yellow, 50),
        };

        // 12 columns x 8 rows level layouts. '.' empty, 'S' silver (dynamic), 'X' gold
        // (indestructible), any other letter looked up in BrickInfo.
        private static readonly string[][] LevelPatterns =
        {
            new[]
            {
                "....YY......",
                "...YYYY.....",
                "..SSSSSSSS..",
                "..SRRSSRRS..",
                "..SRRSSRRS..",
                "..SSSSSSSS..",
                "...SSSSSS...",
                "............",
            },
            new[]
            {
                "YYYYYYYYYYYY",
                "OOOOOOOOOOOO",
                "CCCCCCCCCCCC",
                "GGGGGGGGGGGG",
                "RRRRRRRRRRRR",
                "BBBBBBBBBBBB",
                "............",
                "............",
            },
            new[]
            {
                "RRRRRRRRRRRR",
                "R.R.R.R.R.R.",
                "GGGGGGGGGGGG",
                "G.G.G.G.G.G.",
                "..XXXXXXXX..",
                "BBBBBBBBBBBB",
                "............",
                "............",
            },
            new[]
            {
                "......Y.....",
                ".....YYY....",
                "....YYYYY...",
                "...YYYYYYY..",
                "...YYYYYYY..",
                "....YYYYY...",
                ".....YYY....",
                "......Y.....",
            },
        };

        public ArkanoidGame()
        {
            graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = ScreenWidth,
                PreferredBackBufferHeight = ScreenHeight,
                SynchronizeWithVerticalRetrace = true
            };
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.AllowUserResizing = true;
            Window.Title = "Arkanoid";
        }

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            pixel = new Texture2D(GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });

            circleTex = CreateCircleTexture(16);
            topWallTex = CreatePatternTexture(ScreenWidth, WallThick);
            sideWallTex = CreatePatternTexture(WallThick, FieldBottom - FieldTop);

            sceneTarget = new RenderTarget2D(GraphicsDevice, ScreenWidth, ScreenHeight);

            ResetBallAndPaddle();
        }

        // -------------------------------------------------------------------------
        // Procedural texture generation (hand-drawn graphics only, no image files)
        // -------------------------------------------------------------------------
        private Texture2D CreateCircleTexture(int diameter)
        {
            var tex = new Texture2D(GraphicsDevice, diameter, diameter);
            var data = new Color[diameter * diameter];
            float r = diameter / 2f;
            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    float dx = x - r + 0.5f;
                    float dy = y - r + 0.5f;
                    data[y * diameter + x] = (dx * dx + dy * dy <= r * r) ? Color.White : Color.Transparent;
                }
            }
            tex.SetData(data);
            return tex;
        }

        private Texture2D CreatePatternTexture(int w, int h)
        {
            var tex = new Texture2D(GraphicsDevice, w, h);
            var data = new Color[w * h];
            Color c1 = new Color(18, 18, 90);
            Color c2 = new Color(42, 42, 140);
            const int tile = 8;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int tx = (x / tile) % 2;
                    int ty = (y / tile) % 2;
                    data[y * w + x] = ((tx + ty) % 2 == 0) ? c1 : c2;
                }
            }
            tex.SetData(data);
            return tex;
        }

        private void ToggleFullscreen()
        {
            graphics.IsFullScreen = !graphics.IsFullScreen;
            if (graphics.IsFullScreen)
            {
                graphics.PreferredBackBufferWidth = GraphicsDevice.Adapter.CurrentDisplayMode.Width;
                graphics.PreferredBackBufferHeight = GraphicsDevice.Adapter.CurrentDisplayMode.Height;
            }
            else
            {
                graphics.PreferredBackBufferWidth = ScreenWidth;
                graphics.PreferredBackBufferHeight = ScreenHeight;
            }
            graphics.ApplyChanges();
        }

        // -------------------------------------------------------------------------
        // Game flow helpers
        // -------------------------------------------------------------------------
        private void StartNewGame()
        {
            score = 0;
            lives = 3;
            level = 1;
            lifePillUsedThisLife = false;
            lastPowerUpType = PowerUpType.None;
            inTransition = false;
            BuildLevel(level);
            ResetBallAndPaddle();
            state = GameState.Playing;
        }

        private void ResetBallAndPaddle()
        {
            paddleWidth = PaddleBaseWidth;
            expandActive = false; expandTimer = 0;
            laserActive = false; laserTimer = 0; laserCooldown = 0;
            catchActive = false; catchTimer = 0;
            slowActive = false; slowTimer = 0;
            speedMultiplier = 1f;

            capsules.Clear();
            laserBolts.Clear();

            paddleX = FieldLeft + (FieldRight - FieldLeft - paddleWidth) / 2f;

            balls.Clear();
            var ball = new Ball { Speed = BallBaseSpeed, Stuck = true, StickOffset = 0f };
            ball.Position = new Vector2(paddleX + paddleWidth / 2f, PaddleY - ball.Radius - 1);
            balls.Add(ball);
        }

        private void LoseLife()
        {
            lives--;
            lifePillUsedThisLife = false;
            if (lives < 0)
            {
                if (score > highScore) highScore = score;
                state = GameState.GameOver;
            }
            else
            {
                ResetBallAndPaddle();
            }
        }

        private void CompleteLevel()
        {
            if (inTransition) return;
            inTransition = true;
            levelTransitionTimer = 2.5f;

            // The win check only requires destructible bricks to be cleared - gold
            // ("X") bricks are intentionally indestructible and don't count toward
            // it, matching the "Gold brick: Cannot be destroyed" rule. But leaving
            // those permanent blocks fully visible through the "LEVEL CLEAR"
            // transition makes it look like the stage ended with bricks still
            // standing. Sweep the board clean the moment the stage is won so the
            // transition screen matches what it's telling the player.
            foreach (var b in bricks)
                b.Alive = false;
        }

        private void BuildLevel(int lvl)
        {
            bricks.Clear();
            string[] basePattern = LevelPatterns[(lvl - 1) % LevelPatterns.Length];
            var chars = basePattern.Select(r => r.ToCharArray()).ToArray();

            if (lvl > 4)
            {
                double chance = Math.Min(0.25, lvl * 0.015);
                for (int r = 0; r < Rows; r++)
                    for (int c = 0; c < Cols; c++)
                        if (chars[r][c] == '.' && rng.NextDouble() < chance)
                            chars[r][c] = 'X';
            }

            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Cols; col++)
                {
                    char ch = chars[row][col];
                    if (ch == '.') continue;

                    var bounds = new Rectangle(FieldLeft + col * BrickW, BrickAreaTop + row * BrickH, BrickW - 2, BrickH - 2);
                    var brick = new Brick { Bounds = bounds, Alive = true };

                    if (ch == 'S')
                    {
                        brick.Color = Color.Silver;
                        brick.Points = 50 * lvl;
                        brick.HitsRemaining = 1 + lvl / 8;
                        brick.Indestructible = false;
                    }
                    else if (ch == 'X')
                    {
                        brick.Color = Color.Goldenrod;
                        brick.Points = 0;
                        brick.HitsRemaining = int.MaxValue;
                        brick.Indestructible = true;
                    }
                    else if (BrickInfo.TryGetValue(ch, out var info))
                    {
                        brick.Color = info.Color;
                        brick.Points = info.Points;
                        brick.HitsRemaining = 1;
                        brick.Indestructible = false;
                    }
                    else
                    {
                        continue;
                    }

                    bricks.Add(brick);
                }
            }
        }

        private PowerUpType ChoosePowerUpType()
        {
            var options = new List<(PowerUpType type, int weight)>
            {
                (PowerUpType.Slow, 2),
                (PowerUpType.Laser, 2),
                (PowerUpType.Catch, 2),
                (PowerUpType.Expand, 2),
                (PowerUpType.Disrupt, 2),
                (PowerUpType.Break, 1),
            };
            if (!lifePillUsedThisLife) options.Add((PowerUpType.Player, 1));

            int total = options.Sum(o => o.weight);
            int roll = rng.Next(total);
            PowerUpType picked = PowerUpType.Disrupt;
            int acc = 0;
            foreach (var o in options)
            {
                acc += o.weight;
                if (roll < acc) { picked = o.type; break; }
            }

            if (picked == lastPowerUpType) picked = PowerUpType.Disrupt;
            lastPowerUpType = picked;
            if (picked == PowerUpType.Player) lifePillUsedThisLife = true;
            return picked;
        }

        private void SpawnCapsule(Vector2 center)
        {
            var type = ChoosePowerUpType();
            capsules.Add(new PowerUpCapsule { Position = new Vector2(center.X - 12, center.Y - 6), Type = type });
        }

        private void ApplyPowerUp(PowerUpType type)
        {
            switch (type)
            {
                case PowerUpType.Slow:
                    speedMultiplier = 0.65f;
                    slowActive = true; slowTimer = 10f;
                    break;
                case PowerUpType.Laser:
                    laserActive = true; laserTimer = 15f;
                    break;
                case PowerUpType.Catch:
                    catchActive = true; catchTimer = 15f;
                    break;
                case PowerUpType.Expand:
                    paddleWidth = PaddleExpandedWidth;
                    expandActive = true; expandTimer = 15f;
                    break;
                case PowerUpType.Player:
                    lives++;
                    break;
                case PowerUpType.Break:
                    CompleteLevel();
                    break;
                case PowerUpType.Disrupt:
                    var toAdd = new List<Ball>();
                    foreach (var b in balls.Where(b => b.Alive && !b.Stuck).ToList())
                    {
                        toAdd.Add(CloneBallWithAngle(b, 25f));
                        toAdd.Add(CloneBallWithAngle(b, -25f));
                    }
                    balls.AddRange(toAdd);
                    break;
            }
        }

        private static Ball CloneBallWithAngle(Ball src, float degrees)
        {
            float rad = MathHelper.ToRadians(degrees);
            float cos = (float)Math.Cos(rad), sin = (float)Math.Sin(rad);
            Vector2 d = src.Direction;
            Vector2 newDir = new Vector2(d.X * cos - d.Y * sin, d.X * sin + d.Y * cos);
            newDir.Normalize();
            return new Ball { Position = src.Position, Direction = newDir, Speed = src.Speed, Stuck = false, Alive = true };
        }

        private static Color DarkenColor(Color c, float factor)
        {
            return new Color((int)(c.R * factor), (int)(c.G * factor), (int)(c.B * factor));
        }

        private static char LetterForType(PowerUpType t)
        {
            return t switch
            {
                PowerUpType.Slow => 'S',
                PowerUpType.Laser => 'L',
                PowerUpType.Catch => 'C',
                PowerUpType.Break => 'B',
                PowerUpType.Expand => 'E',
                PowerUpType.Player => 'P',
                PowerUpType.Disrupt => 'D',
                _ => '?'
            };
        }

        private static Color ColorForType(PowerUpType t)
        {
            return t switch
            {
                PowerUpType.Slow => Color.LightBlue,
                PowerUpType.Laser => Color.OrangeRed,
                PowerUpType.Catch => Color.LimeGreen,
                PowerUpType.Break => Color.Orange,
                PowerUpType.Expand => Color.Cyan,
                PowerUpType.Player => Color.HotPink,
                PowerUpType.Disrupt => Color.MediumPurple,
                _ => Color.White
            };
        }

        private Rectangle BallRect(Ball b) => new Rectangle((int)(b.Position.X - b.Radius), (int)(b.Position.Y - b.Radius), (int)(b.Radius * 2), (int)(b.Radius * 2));

        private void HandleBallPaddle(Ball ball)
        {
            if (ball.Direction.Y <= 0) return;
            var paddleRect = new Rectangle((int)paddleX, PaddleY, (int)paddleWidth, PaddleHeight);
            var ballRect = BallRect(ball);
            if (!ballRect.Intersects(paddleRect)) return;

            float hitPos = (ball.Position.X - (paddleX + paddleWidth / 2f)) / (paddleWidth / 2f);
            hitPos = MathHelper.Clamp(hitPos, -1f, 1f);

            if (catchActive)
            {
                ball.Stuck = true;
                ball.StickOffset = hitPos * (paddleWidth / 2f);
            }
            else
            {
                float maxAngle = MathHelper.ToRadians(60f);
                float angle = hitPos * maxAngle;
                ball.Direction = new Vector2((float)Math.Sin(angle), -(float)Math.Cos(angle));
            }
            ball.Position.Y = PaddleY - ball.Radius - 1;
        }

        private void HandleBallBricks(Ball ball)
        {
            var ballRect = BallRect(ball);
            foreach (var brick in bricks)
            {
                if (!brick.Alive) continue;
                if (!ballRect.Intersects(brick.Bounds)) continue;

                float overlapLeft = ballRect.Right - brick.Bounds.Left;
                float overlapRight = brick.Bounds.Right - ballRect.Left;
                float overlapTop = ballRect.Bottom - brick.Bounds.Top;
                float overlapBottom = brick.Bounds.Bottom - ballRect.Top;
                float minX = Math.Min(overlapLeft, overlapRight);
                float minY = Math.Min(overlapTop, overlapBottom);

                if (minX < minY)
                {
                    ball.Direction.X = -ball.Direction.X;
                    ball.Position.X += (overlapLeft < overlapRight) ? -minX : minX;
                }
                else
                {
                    ball.Direction.Y = -ball.Direction.Y;
                    ball.Position.Y += (overlapTop < overlapBottom) ? -minY : minY;
                }

                if (!brick.Indestructible)
                {
                    brick.HitsRemaining--;
                    if (brick.HitsRemaining <= 0)
                    {
                        brick.Alive = false;
                        score += brick.Points;
                        if (rng.NextDouble() < 0.30)
                            SpawnCapsule(new Vector2(brick.Bounds.Center.X, brick.Bounds.Center.Y));
                    }
                }
                break;
            }
        }

        // -------------------------------------------------------------------------
        // Update
        // -------------------------------------------------------------------------
        protected override void Update(GameTime gameTime)
        {
            var kb = Keyboard.GetState();

            if (kb.IsKeyDown(Keys.F11) && !prevKb.IsKeyDown(Keys.F11))
                ToggleFullscreen();
            if (kb.IsKeyDown(Keys.Escape))
                Exit();

            blinkTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

            switch (state)
            {
                case GameState.Menu: UpdateMenu(kb); break;
                case GameState.Playing: UpdatePlaying(gameTime, kb); break;
                case GameState.GameOver: UpdateGameOver(kb); break;
            }

            prevKb = kb;
            base.Update(gameTime);
        }

        private void UpdateMenu(KeyboardState kb)
        {
            if ((kb.IsKeyDown(Keys.Enter) && !prevKb.IsKeyDown(Keys.Enter)) ||
                (kb.IsKeyDown(Keys.Space) && !prevKb.IsKeyDown(Keys.Space)))
            {
                StartNewGame();
            }
        }

        private void UpdateGameOver(KeyboardState kb)
        {
            if (kb.IsKeyDown(Keys.Enter) && !prevKb.IsKeyDown(Keys.Enter))
                state = GameState.Menu;
        }

        private void UpdatePlaying(GameTime gameTime, KeyboardState kb)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (inTransition)
            {
                levelTransitionTimer -= dt;
                if (levelTransitionTimer <= 0)
                {
                    inTransition = false;
                    level++;
                    BuildLevel(level);
                    ResetBallAndPaddle();
                }
                return;
            }

            // Paddle movement
            if (kb.IsKeyDown(Keys.Left) || kb.IsKeyDown(Keys.A)) paddleX -= PaddleSpeed * dt;
            if (kb.IsKeyDown(Keys.Right) || kb.IsKeyDown(Keys.D)) paddleX += PaddleSpeed * dt;
            paddleX = MathHelper.Clamp(paddleX, FieldLeft, FieldRight - paddleWidth);

            // Power-up timers
            if (expandActive) { expandTimer -= dt; if (expandTimer <= 0) { expandActive = false; paddleWidth = PaddleBaseWidth; } }
            if (laserActive) { laserTimer -= dt; if (laserTimer <= 0) laserActive = false; }
            if (catchActive) { catchTimer -= dt; if (catchTimer <= 0) catchActive = false; }
            if (slowActive) { slowTimer -= dt; if (slowTimer <= 0) { slowActive = false; speedMultiplier = 1f; } }
            if (laserCooldown > 0) laserCooldown -= dt;

            // Launch stuck ball(s) / fire laser
            if (kb.IsKeyDown(Keys.Space) && !prevKb.IsKeyDown(Keys.Space))
            {
                bool launchedAny = false;
                foreach (var b in balls.Where(b => b.Stuck))
                {
                    b.Stuck = false;
                    float ratio = paddleWidth > 0 ? b.StickOffset / (paddleWidth / 2f) : 0f;
                    Vector2 dir = new Vector2(ratio * 0.6f, -1f);
                    dir.Normalize();
                    b.Direction = dir;
                    launchedAny = true;
                }
                if (!launchedAny && laserActive && laserCooldown <= 0)
                {
                    laserBolts.Add(new Vector2(paddleX + 6, PaddleY - 10));
                    laserBolts.Add(new Vector2(paddleX + paddleWidth - 9, PaddleY - 10));
                    laserCooldown = 0.35f;
                }
            }

            // Capsules
            for (int i = capsules.Count - 1; i >= 0; i--)
            {
                var cap = capsules[i];
                cap.Position.Y += 90f * dt;
                var paddleRect = new Rectangle((int)paddleX, PaddleY, (int)paddleWidth, PaddleHeight);
                if (cap.Bounds.Intersects(paddleRect))
                {
                    score += 1000;
                    ApplyPowerUp(cap.Type);
                    capsules.RemoveAt(i);
                    continue;
                }
                if (cap.Position.Y > ScreenHeight)
                    capsules.RemoveAt(i);
            }

            // Laser bolts
            for (int i = laserBolts.Count - 1; i >= 0; i--)
            {
                var pos = laserBolts[i] - new Vector2(0, 420f * dt);
                laserBolts[i] = pos;
                if (pos.Y < FieldTop) { laserBolts.RemoveAt(i); continue; }

                var boltRect = new Rectangle((int)pos.X, (int)pos.Y, 3, 10);
                bool hit = false;
                foreach (var brick in bricks)
                {
                    if (!brick.Alive) continue;
                    if (!boltRect.Intersects(brick.Bounds)) continue;
                    if (!brick.Indestructible)
                    {
                        brick.HitsRemaining--;
                        if (brick.HitsRemaining <= 0)
                        {
                            brick.Alive = false;
                            score += brick.Points;
                            if (rng.NextDouble() < 0.30)
                                SpawnCapsule(new Vector2(brick.Bounds.Center.X, brick.Bounds.Center.Y));
                        }
                    }
                    hit = true;
                    break;
                }
                if (hit) laserBolts.RemoveAt(i);
            }

            // Balls
            foreach (var ball in balls)
            {
                if (!ball.Alive) continue;

                if (ball.Stuck)
                {
                    ball.Position = new Vector2(paddleX + paddleWidth / 2f + ball.StickOffset, PaddleY - ball.Radius - 1);
                    continue;
                }

                ball.Position += ball.Direction * ball.Speed * speedMultiplier * dt;

                if (ball.Position.X - ball.Radius < FieldLeft)
                {
                    ball.Position.X = FieldLeft + ball.Radius;
                    ball.Direction.X = Math.Abs(ball.Direction.X);
                }
                if (ball.Position.X + ball.Radius > FieldRight)
                {
                    ball.Position.X = FieldRight - ball.Radius;
                    ball.Direction.X = -Math.Abs(ball.Direction.X);
                }
                if (ball.Position.Y - ball.Radius < FieldTop)
                {
                    ball.Position.Y = FieldTop + ball.Radius;
                    ball.Direction.Y = Math.Abs(ball.Direction.Y);
                }

                HandleBallPaddle(ball);
                HandleBallBricks(ball);

                if (ball.Position.Y - ball.Radius > FieldBottom)
                    ball.Alive = false;
            }
            balls.RemoveAll(b => !b.Alive);
            if (balls.Count == 0)
            {
                LoseLife();
                return;
            }

            if (bricks.Count(b => !b.Indestructible && b.Alive) == 0)
                CompleteLevel();
        }

        // -------------------------------------------------------------------------
        // Draw
        // -------------------------------------------------------------------------
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.SetRenderTarget(sceneTarget);
            GraphicsDevice.Clear(new Color(8, 8, 24));
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            DrawPlayfieldBackground();
            DrawWalls();
            DrawBricks();
            DrawCapsules();
            DrawPaddle();
            DrawBalls();
            DrawLasers();
            DrawHUD();

            if (state == GameState.Menu) DrawMenuOverlay();
            else if (state == GameState.GameOver) DrawGameOverOverlay();
            else if (state == GameState.Playing && inTransition) DrawLevelTransitionOverlay();

            spriteBatch.End();

            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.Black);

            var pp = GraphicsDevice.PresentationParameters;
            float scale = Math.Min((float)pp.BackBufferWidth / ScreenWidth, (float)pp.BackBufferHeight / ScreenHeight);
            int destW = (int)(ScreenWidth * scale);
            int destH = (int)(ScreenHeight * scale);
            var destRect = new Rectangle((pp.BackBufferWidth - destW) / 2, (pp.BackBufferHeight - destH) / 2, destW, destH);

            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            spriteBatch.Draw(sceneTarget, destRect, Color.White);
            spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawPlayfieldBackground()
        {
            spriteBatch.Draw(pixel, new Rectangle(FieldLeft, FieldTop, FieldRight - FieldLeft, FieldBottom - FieldTop), new Color(6, 6, 40));
        }

        private void DrawWalls()
        {
            spriteBatch.Draw(topWallTex, new Vector2(0, HudHeight), Color.White);
            spriteBatch.Draw(sideWallTex, new Vector2(0, FieldTop), Color.White);
            spriteBatch.Draw(sideWallTex, new Vector2(FieldRight, FieldTop), Color.White);
        }

        private void DrawBricks()
        {
            foreach (var brick in bricks)
            {
                if (!brick.Alive) continue;
                spriteBatch.Draw(pixel, brick.Bounds, DarkenColor(brick.Color, 0.55f));
                var inner = new Rectangle(brick.Bounds.X + 2, brick.Bounds.Y + 2, Math.Max(0, brick.Bounds.Width - 4), Math.Max(0, brick.Bounds.Height - 4));
                spriteBatch.Draw(pixel, inner, brick.Color);
            }
        }

        private void DrawCapsules()
        {
            foreach (var cap in capsules)
            {
                spriteBatch.Draw(pixel, cap.Bounds, ColorForType(cap.Type));
                string letter = LetterForType(cap.Type).ToString();
                var size = BitmapFont.MeasureText(letter, 1f);
                var pos = new Vector2(cap.Bounds.Center.X - size.X / 2f, cap.Bounds.Center.Y - size.Y / 2f);
                BitmapFont.DrawText(spriteBatch, pixel, letter, pos, 1f, Color.Black);
            }
        }

        private void DrawPaddle()
        {
            var paddleRect = new Rectangle((int)paddleX, PaddleY, (int)paddleWidth, PaddleHeight);
            spriteBatch.Draw(pixel, paddleRect, Color.LightGray);
            spriteBatch.Draw(pixel, new Rectangle(paddleRect.X, paddleRect.Y, paddleRect.Width, 4), Color.Red);
            if (laserActive)
            {
                spriteBatch.Draw(pixel, new Rectangle(paddleRect.X, paddleRect.Y - 4, 4, 4), Color.Yellow);
                spriteBatch.Draw(pixel, new Rectangle(paddleRect.Right - 8, paddleRect.Y - 4, 4, 4), Color.Yellow);
            }
        }

        private void DrawBalls()
        {
            foreach (var ball in balls)
            {
                if (!ball.Alive) continue;
                var rect = new Rectangle((int)(ball.Position.X - ball.Radius), (int)(ball.Position.Y - ball.Radius), (int)(ball.Radius * 2), (int)(ball.Radius * 2));
                spriteBatch.Draw(circleTex, rect, Color.White);
            }
        }

        private void DrawLasers()
        {
            foreach (var pos in laserBolts)
                spriteBatch.Draw(pixel, new Rectangle((int)pos.X, (int)pos.Y, 3, 10), Color.Red);
        }

        private void DrawTextCentered(string text, float centerX, float y, float pixelSize, Color color)
        {
            var size = BitmapFont.MeasureText(text, pixelSize);
            BitmapFont.DrawText(spriteBatch, pixel, text, new Vector2(centerX - size.X / 2f, y), pixelSize, color);
        }

        private void DrawHUD()
        {
            BitmapFont.DrawText(spriteBatch, pixel, "1UP", new Vector2(16, 8), 2f, Color.Red);
            BitmapFont.DrawText(spriteBatch, pixel, score.ToString(), new Vector2(16, 26), 2f, Color.White);

            string hs = "HIGH SCORE";
            var hsSize = BitmapFont.MeasureText(hs, 2f);
            BitmapFont.DrawText(spriteBatch, pixel, hs, new Vector2(ScreenWidth / 2f - hsSize.X / 2f, 8), 2f, Color.Red);
            string hsVal = Math.Max(highScore, score).ToString();
            var hsValSize = BitmapFont.MeasureText(hsVal, 2f);
            BitmapFont.DrawText(spriteBatch, pixel, hsVal, new Vector2(ScreenWidth / 2f - hsValSize.X / 2f, 26), 2f, Color.White);

            int extraLives = Math.Max(0, lives);
            for (int i = 0; i < extraLives; i++)
            {
                var r = new Rectangle(16 + i * 18, 616, 14, 6);
                spriteBatch.Draw(pixel, r, Color.Red);
            }

            string lvlText = "LEVEL " + level;
            var lvlSize = BitmapFont.MeasureText(lvlText, 1.5f);
            BitmapFont.DrawText(spriteBatch, pixel, lvlText, new Vector2(ScreenWidth - lvlSize.X - 16, 616), 1.5f, Color.White);
        }

        private void DrawMenuOverlay()
        {
            spriteBatch.Draw(pixel, new Rectangle(0, 0, ScreenWidth, ScreenHeight), new Color(0, 0, 0, 180));
            DrawTextCentered("ARKANOID", ScreenWidth / 2f, 220, 4f, Color.Yellow);
            if (blinkTimer % 1.0f < 0.6f)
                DrawTextCentered("PRESS ENTER TO START", ScreenWidth / 2f, 320, 2f, Color.Cyan);
            DrawTextCentered("ARROWS MOVE   SPACE LAUNCH-FIRE", ScreenWidth / 2f, 380, 1.5f, Color.LightGray);
            DrawTextCentered("F11 FULLSCREEN   ESC QUIT", ScreenWidth / 2f, 400, 1.5f, Color.LightGray);
        }

        private void DrawGameOverOverlay()
        {
            spriteBatch.Draw(pixel, new Rectangle(0, 0, ScreenWidth, ScreenHeight), new Color(0, 0, 0, 180));
            DrawTextCentered("GAME OVER", ScreenWidth / 2f, 260, 4f, Color.Red);
            DrawTextCentered("SCORE " + score, ScreenWidth / 2f, 320, 2f, Color.White);
            if (blinkTimer % 1.0f < 0.6f)
                DrawTextCentered("PRESS ENTER", ScreenWidth / 2f, 360, 2f, Color.Cyan);
        }

        private void DrawLevelTransitionOverlay()
        {
            DrawTextCentered("LEVEL " + level + " CLEAR", ScreenWidth / 2f, 300, 3f, Color.Yellow);
        }
    }
}
