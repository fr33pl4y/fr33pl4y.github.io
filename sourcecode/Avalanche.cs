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

namespace AvalancheClone
{
    // =================================================================================
    //  Entry point
    // =================================================================================
    public static class Program
    {
        [STAThread]
        private static void Main()
        {
            using (var game = new AvalancheGame())
            {
                game.Run();
            }
        }
    }

    // =================================================================================
    //  The states the game can be in
    // =================================================================================
    public enum GameState
    {
        WaitingToServe,   // Platform is ready, waiting for SERVE (or the 5 second timer)
        Playing,          // Rocks are falling
        BoardSetup,       // Short pause while a fresh set of rocks is put on the board
        GameOver          // Out of misses (or everything cleared)
    }

    // =================================================================================
    //  A single rock
    // =================================================================================
    public class Rock
    {
        public int Row;           // 0 = lowest row (worth 1 point) ... 5 = highest row (worth 6 points)
        public int Column;        // 0..31
        public bool IsCaught;     // Caught rocks are gone for good (until the next refill)
        public bool IsFalling;    // True while the rock is on its way down
        public float X;           // Current top-left position in screen pixels
        public float Y;

        public int Points { get { return Row + 1; } }

        public int Size { get { return AvalancheGame.RockSizeForRow(Row); } }

        // Puts the rock back to its place in the grid at the top of the screen.
        public void ReturnToTop()
        {
            IsFalling = false;
            X = Column * AvalancheGame.ColumnWidth + (AvalancheGame.ColumnWidth - Size) / 2;
            Y = AvalancheGame.RowTopY[Row];
        }
    }

    // =================================================================================
    //  The game itself
    // =================================================================================
    public class AvalancheGame : Game
    {
        // ---------------------------------------------------------------------------
        //  Screen layout (all numbers are in "virtual pixels"; the picture is scaled up)
        // ---------------------------------------------------------------------------
        public const int ScreenWidth = 256;
        public const int ScreenHeight = 240;

        private const int GroundY = 236;            // Rocks that reach this height count as a miss
        private const int GroundHeight = 4;

        // ---------------------------------------------------------------------------
        //  Rock grid
        // ---------------------------------------------------------------------------
        public const int RowCount = 6;
        public const int RocksPerRow = 32;
        public const int ColumnWidth = ScreenWidth / RocksPerRow;   // 8 pixels per rock slot

        // Top edge of each row, index 0 = lowest row (the one that falls first).
        public static readonly int[] RowTopY = { 46, 40, 35, 30, 26, 22 };

        // Rows 1-2 are big cyan rocks, rows 3-4 medium orange, rows 5-6 small yellow.
        public static int RockSizeForRow(int row)
        {
            if (row < 2) return 6;
            if (row < 4) return 5;
            return 4;
        }

        // ---------------------------------------------------------------------------
        //  Platform
        // ---------------------------------------------------------------------------
        private const int PlatformWidth = 16;
        private const int PlatformThickness = 3;
        private const int PlatformBottomY = 228;    // Top edge of the lowest story
        private const int StorySpacing = 24;        // Vertical distance between stories
        private const float KeyboardSpeed = 140f;   // Pixels per second

        // How many stories you start with: first board, then after each refill.
        private static readonly int[] StartingStories = { 6, 3, 2, 1 };
        private const int MaxRefills = 3;

        // ---------------------------------------------------------------------------
        //  Rock behaviour
        // ---------------------------------------------------------------------------
        // Fall speed in pixels per second for row pairs (rows 1-2, rows 3-4, rows 5-6).
        private static readonly float[] BaseFallSpeed = { 55f, 80f, 110f };
        private const float SpeedBonusPerRefill = 10f;
        private const int MaxRocksFallingAtOnce = 4;
        private const float MinSpawnDelay = 0.4f;
        private const float MaxSpawnDelay = 0.9f;

        // ---------------------------------------------------------------------------
        //  Game rules
        // ---------------------------------------------------------------------------
        private const float ServeCountdownSeconds = 5f;
        private const float BoardSetupSeconds = 2f;
        private const int MissesPerGame = 3;
        private const int ExtendedPlayScore = 200;   // Reach this score and you get extra misses
        private const int ExtraMissesForExtendedPlay = 3;

        // ---------------------------------------------------------------------------
        //  Colours
        // ---------------------------------------------------------------------------
        private static readonly Color BackgroundColor = new Color(0, 18, 22);
        private static readonly Color CyanColor = new Color(0, 230, 230);
        private static readonly Color OrangeColor = new Color(250, 110, 10);
        private static readonly Color YellowColor = new Color(240, 230, 20);
        private static readonly Color HudBarColor = new Color(0, 0, 150);
        private static readonly Color HudBarTextColor = new Color(90, 110, 255);
        private static readonly Color WhiteColor = new Color(235, 235, 235);

        // ---------------------------------------------------------------------------
        //  Hand-drawn rock sprites ('#' = pixel, '.' = empty)
        // ---------------------------------------------------------------------------
        private static readonly string[] RockSprite6 =
        {
            ".####.",
            "######",
            "######",
            "######",
            "######",
            ".####."
        };

        private static readonly string[] RockSprite5 =
        {
            ".###.",
            "#####",
            "#####",
            "#####",
            ".###."
        };

        private static readonly string[] RockSprite4 =
        {
            ".##.",
            "####",
            "####",
            ".##."
        };

        // ---------------------------------------------------------------------------
        //  MonoGame objects
        // ---------------------------------------------------------------------------
        private readonly GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private Texture2D pixel;                  // A single white pixel, used for everything we draw

        // Window / full screen handling
        private bool isFullScreen;
        private int windowedWidth = ScreenWidth * 4;
        private int windowedHeight = ScreenHeight * 4;
        private int drawScale = 4;                // Integer scale factor of the virtual screen
        private int drawOffsetX;                  // Black bars (letterbox) offsets
        private int drawOffsetY;

        // Input memory (to detect "key was just pressed")
        private KeyboardState previousKeyboard;
        private int previousMouseX = -1;

        // ---------------------------------------------------------------------------
        //  Game data
        // ---------------------------------------------------------------------------
        private readonly Random random = new Random();
        private readonly Rock[,] rocks = new Rock[RowCount, RocksPerRow];
        private readonly List<Rock> fallingRocks = new List<Rock>();

        private GameState state = GameState.WaitingToServe;
        private float platformX;                  // Left edge of the platform
        private int score;
        private int highScore;
        private int misses;
        private int allowedMisses = MissesPerGame;
        private bool extendedPlayGranted;
        private int refillsUsed;                  // 0 = original rocks
        private bool boardWasCleared;             // True if the game ended because all rocks were caught

        private float serveCountdown;
        private float boardSetupTimer;
        private float spawnTimer;
        private float bannerTimer;                // Shows the "EXTENDED PLAY" message for a moment

        // =============================================================================
        //  Construction & setup
        // =============================================================================
        public AvalancheGame()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = windowedWidth;
            graphics.PreferredBackBufferHeight = windowedHeight;
            Content.RootDirectory = "Content";   // Not used, but harmless
            IsMouseVisible = false;
            Window.Title = "Avalanche";
            Window.AllowUserResizing = true;
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // A 1x1 white texture. Every shape in the game is a scaled/tinted copy of it.
            pixel = new Texture2D(GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });

            BitmapFont.Build();
            StartNewGame();
        }

        // Resets everything for a brand new game.
        private void StartNewGame()
        {
            score = 0;
            misses = 0;
            allowedMisses = MissesPerGame;
            extendedPlayGranted = false;
            refillsUsed = 0;
            boardWasCleared = false;
            bannerTimer = 0f;

            FillBoardWithRocks();
            platformX = (ScreenWidth - PlatformWidth) / 2f;
            EnterWaitingToServe();
        }

        // Creates all 6 x 32 rocks and places them at the top.
        private void FillBoardWithRocks()
        {
            fallingRocks.Clear();
            for (int row = 0; row < RowCount; row++)
            {
                for (int column = 0; column < RocksPerRow; column++)
                {
                    var rock = new Rock { Row = row, Column = column, IsCaught = false };
                    rock.ReturnToTop();
                    rocks[row, column] = rock;
                }
            }
        }

        private void EnterWaitingToServe()
        {
            state = GameState.WaitingToServe;
            serveCountdown = ServeCountdownSeconds;
        }

        private void EnterPlaying()
        {
            state = GameState.Playing;
            spawnTimer = 0.3f;
        }

        // =============================================================================
        //  UPDATE
        // =============================================================================
        protected override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            KeyboardState keyboard = Keyboard.GetState();
            MouseState mouse = Mouse.GetState();

            if (keyboard.IsKeyDown(Keys.Escape))
            {
                Exit();
            }

            if (WasPressed(keyboard, Keys.F11))
            {
                ToggleFullScreen();
            }

            UpdateLayout();
            MovePlatform(dt, keyboard, mouse);

            switch (state)
            {
                case GameState.WaitingToServe: UpdateWaitingToServe(dt, keyboard); break;
                case GameState.Playing:        UpdatePlaying(dt);                  break;
                case GameState.BoardSetup:     UpdateBoardSetup(dt);               break;
                case GameState.GameOver:       UpdateGameOver(keyboard);           break;
            }

            if (bannerTimer > 0f)
            {
                bannerTimer -= dt;
            }

            previousKeyboard = keyboard;
            base.Update(gameTime);
        }

        private bool WasPressed(KeyboardState keyboard, Keys key)
        {
            return keyboard.IsKeyDown(key) && previousKeyboard.IsKeyUp(key);
        }

        // ---------------------------------------------------------------------------
        //  Platform movement (keyboard and mouse both work)
        // ---------------------------------------------------------------------------
        private void MovePlatform(float dt, KeyboardState keyboard, MouseState mouse)
        {
            // Keyboard
            if (keyboard.IsKeyDown(Keys.Left) || keyboard.IsKeyDown(Keys.A))
            {
                platformX -= KeyboardSpeed * dt;
            }
            if (keyboard.IsKeyDown(Keys.Right) || keyboard.IsKeyDown(Keys.D))
            {
                platformX += KeyboardSpeed * dt;
            }

            // Mouse: only follow when it actually moved, so keyboard and mouse don't fight.
            if (mouse.X != previousMouseX)
            {
                if (previousMouseX != -1)
                {
                    float virtualMouseX = (mouse.X - drawOffsetX) / (float)drawScale;
                    platformX = virtualMouseX - PlatformWidth / 2f;
                }
                previousMouseX = mouse.X;
            }

            // Keep the platform on screen.
            platformX = MathHelper.Clamp(platformX, 0f, ScreenWidth - PlatformWidth);
        }

        // ---------------------------------------------------------------------------
        //  State: WaitingToServe
        // ---------------------------------------------------------------------------
        private void UpdateWaitingToServe(float dt, KeyboardState keyboard)
        {
            serveCountdown -= dt;

            bool servePressed = WasPressed(keyboard, Keys.Space) || WasPressed(keyboard, Keys.Enter);
            if (servePressed || serveCountdown <= 0f)
            {
                EnterPlaying();
            }
        }

        // ---------------------------------------------------------------------------
        //  State: BoardSetup (a short pause after a refill)
        // ---------------------------------------------------------------------------
        private void UpdateBoardSetup(float dt)
        {
            boardSetupTimer -= dt;
            if (boardSetupTimer <= 0f)
            {
                EnterWaitingToServe();
            }
        }

        // ---------------------------------------------------------------------------
        //  State: GameOver
        // ---------------------------------------------------------------------------
        private void UpdateGameOver(KeyboardState keyboard)
        {
            if (WasPressed(keyboard, Keys.Enter) || WasPressed(keyboard, Keys.Space))
            {
                StartNewGame();
            }
        }

        // ---------------------------------------------------------------------------
        //  State: Playing  -  the heart of the game
        // ---------------------------------------------------------------------------
        private void UpdatePlaying(float dt)
        {
            SpawnRocksIfNeeded(dt);
            MoveFallingRocks(dt);

            // MoveFallingRocks may have ended the round (a miss), so check the state again.
            if (state != GameState.Playing)
            {
                return;
            }

            CheckForExtendedPlay();
            CheckForClearedBoard();
        }

        // Lets a new rock start falling from the lowest row that still has rocks.
        private void SpawnRocksIfNeeded(float dt)
        {
            spawnTimer -= dt;
            if (spawnTimer > 0f || fallingRocks.Count >= MaxRocksFallingAtOnce)
            {
                return;
            }

            int currentRow = GetLowestRowWithRocks();
            if (currentRow < 0)
            {
                return;
            }

            // Collect the rocks of that row that are still waiting at the top.
            var waitingRocks = new List<Rock>();
            for (int column = 0; column < RocksPerRow; column++)
            {
                Rock rock = rocks[currentRow, column];
                if (!rock.IsCaught && !rock.IsFalling)
                {
                    waitingRocks.Add(rock);
                }
            }

            if (waitingRocks.Count == 0)
            {
                return;   // The rest of this row is already on its way down
            }

            Rock chosen = waitingRocks[random.Next(waitingRocks.Count)];
            chosen.IsFalling = true;
            fallingRocks.Add(chosen);

            spawnTimer = MinSpawnDelay + (float)random.NextDouble() * (MaxSpawnDelay - MinSpawnDelay);
        }

        // Moves every falling rock down and checks for catches and misses.
        private void MoveFallingRocks(float dt)
        {
            // Walk backwards so we can safely remove rocks while looping.
            for (int i = fallingRocks.Count - 1; i >= 0; i--)
            {
                Rock rock = fallingRocks[i];

                float bottomBefore = rock.Y + rock.Size;
                rock.Y += GetFallSpeed(rock.Row) * dt;
                float bottomAfter = rock.Y + rock.Size;

                if (TryCatchWithPlatform(rock, bottomBefore, bottomAfter))
                {
                    fallingRocks.RemoveAt(i);
                    AddScore(rock.Points);
                    continue;
                }

                if (bottomAfter >= GroundY)
                {
                    HandleMiss();
                    return;   // The whole round is reset, nothing more to move
                }
            }
        }

        // A rock is caught when its bottom edge crosses a platform story
        // while the rock is horizontally above that platform.
        private bool TryCatchWithPlatform(Rock rock, float bottomBefore, float bottomAfter)
        {
            bool overlapsHorizontally = rock.X + rock.Size > platformX &&
                                        rock.X < platformX + PlatformWidth;
            if (!overlapsHorizontally)
            {
                return false;
            }

            int stories = GetCurrentStoryCount();
            for (int story = 0; story < stories; story++)
            {
                float storyY = GetStoryTopY(story);
                if (bottomBefore <= storyY && bottomAfter >= storyY)
                {
                    rock.IsCaught = true;
                    rock.IsFalling = false;
                    return true;
                }
            }
            return false;
        }

        // A rock hit the ground: every falling rock goes back to the top.
        private void HandleMiss()
        {
            misses++;

            foreach (Rock rock in fallingRocks)
            {
                rock.ReturnToTop();
            }
            fallingRocks.Clear();

            if (misses >= allowedMisses)
            {
                EndGame();
            }
            else
            {
                EnterWaitingToServe();
            }
        }

        private void EndGame()
        {
            state = GameState.GameOver;
            highScore = Math.Max(highScore, score);
        }

        private void AddScore(int points)
        {
            score += points;
            highScore = Math.Max(highScore, score);
        }

        // Reaching a certain score gives extra misses ("extended play").
        private void CheckForExtendedPlay()
        {
            if (!extendedPlayGranted && score >= ExtendedPlayScore)
            {
                extendedPlayGranted = true;
                allowedMisses += ExtraMissesForExtendedPlay;
                bannerTimer = 2f;
            }
        }

        // When no rocks are left, refill the board (up to 3 times).
        private void CheckForClearedBoard()
        {
            if (GetLowestRowWithRocks() >= 0)
            {
                return;
            }

            if (refillsUsed >= MaxRefills)
            {
                boardWasCleared = true;
                EndGame();
                return;
            }

            refillsUsed++;
            FillBoardWithRocks();
            state = GameState.BoardSetup;
            boardSetupTimer = BoardSetupSeconds;
        }

        // ---------------------------------------------------------------------------
        //  Helper calculations
        // ---------------------------------------------------------------------------

        // Returns the lowest row (0..5) that still has uncaught rocks, or -1 if none.
        private int GetLowestRowWithRocks()
        {
            for (int row = 0; row < RowCount; row++)
            {
                for (int column = 0; column < RocksPerRow; column++)
                {
                    if (!rocks[row, column].IsCaught)
                    {
                        return row;
                    }
                }
            }
            return -1;
        }

        // Each cleared row removes one story. There is always at least one story.
        private int GetCurrentStoryCount()
        {
            int lowestRow = GetLowestRowWithRocks();
            if (lowestRow < 0)
            {
                lowestRow = RowCount - 1;
            }
            int startStories = StartingStories[Math.Min(refillsUsed, StartingStories.Length - 1)];
            return Math.Max(1, startStories - lowestRow);
        }

        // story 0 is the bottom platform, story 1 sits above it, etc.
        private static int GetStoryTopY(int story)
        {
            return PlatformBottomY - story * StorySpacing;
        }

        // Rows 1-2 are slowest, rows 5-6 are fastest. Every refill adds a little speed.
        private float GetFallSpeed(int row)
        {
            int rowPair = row / 2;
            return BaseFallSpeed[rowPair] + refillsUsed * SpeedBonusPerRefill;
        }

        // =============================================================================
        //  Full screen and window layout
        // =============================================================================
        private void ToggleFullScreen()
        {
            if (!isFullScreen)
            {
                // Remember the window size so we can come back to it.
                windowedWidth = GraphicsDevice.PresentationParameters.BackBufferWidth;
                windowedHeight = GraphicsDevice.PresentationParameters.BackBufferHeight;

                DisplayMode desktop = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
                graphics.PreferredBackBufferWidth = desktop.Width;
                graphics.PreferredBackBufferHeight = desktop.Height;
                graphics.HardwareModeSwitch = false;   // Borderless full screen: fast and safe
                graphics.IsFullScreen = true;
            }
            else
            {
                graphics.IsFullScreen = false;
                graphics.PreferredBackBufferWidth = windowedWidth;
                graphics.PreferredBackBufferHeight = windowedHeight;
            }

            graphics.ApplyChanges();
            isFullScreen = !isFullScreen;
        }

        // Works out how big the 256x240 picture can be drawn (whole-number scale, centred).
        private void UpdateLayout()
        {
            int width = GraphicsDevice.PresentationParameters.BackBufferWidth;
            int height = GraphicsDevice.PresentationParameters.BackBufferHeight;

            drawScale = Math.Max(1, Math.Min(width / ScreenWidth, height / ScreenHeight));
            drawOffsetX = (width - ScreenWidth * drawScale) / 2;
            drawOffsetY = (height - ScreenHeight * drawScale) / 2;
        }

        // =============================================================================
        //  DRAW
        // =============================================================================
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);   // Black bars around the picture

            Matrix scaleAndPosition =
                Matrix.CreateScale(drawScale, drawScale, 1f) *
                Matrix.CreateTranslation(drawOffsetX, drawOffsetY, 0f);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                              SamplerState.PointClamp, null, null, null, scaleAndPosition);

            FillRect(0, 0, ScreenWidth, ScreenHeight, BackgroundColor);
            DrawRocks();
            DrawPlatform();
            DrawGround();
            DrawHud();
            DrawMessages();

            spriteBatch.End();
            base.Draw(gameTime);
        }

        // ---------------------------------------------------------------------------
        //  Rocks
        // ---------------------------------------------------------------------------
        private void DrawRocks()
        {
            for (int row = 0; row < RowCount; row++)
            {
                for (int column = 0; column < RocksPerRow; column++)
                {
                    Rock rock = rocks[row, column];
                    if (!rock.IsCaught)
                    {
                        DrawSprite(GetRockSprite(row), (int)rock.X, (int)rock.Y, GetRockColor(row));
                    }
                }
            }
        }

        private static string[] GetRockSprite(int row)
        {
            int size = RockSizeForRow(row);
            if (size == 6) return RockSprite6;
            if (size == 5) return RockSprite5;
            return RockSprite4;
        }

        private static Color GetRockColor(int row)
        {
            if (row < 2) return CyanColor;
            if (row < 4) return OrangeColor;
            return YellowColor;
        }

        // ---------------------------------------------------------------------------
        //  Platform and ground
        // ---------------------------------------------------------------------------
        private void DrawPlatform()
        {
            int stories = GetCurrentStoryCount();
            for (int story = 0; story < stories; story++)
            {
                FillRect((int)platformX, GetStoryTopY(story), PlatformWidth, PlatformThickness, CyanColor);
            }
        }

        private void DrawGround()
        {
            FillRect(0, GroundY, ScreenWidth, GroundHeight, CyanColor);
        }

        // ---------------------------------------------------------------------------
        //  Score display at the top
        // ---------------------------------------------------------------------------
        private void DrawHud()
        {
            // First line: misses on the left, high score on the right.
            BitmapFont.DrawText(this, "MISSES " + misses, 8, 2, CyanColor);
            BitmapFont.DrawText(this, "HIGH SCORE " + highScore, 136, 2, CyanColor);

            // Second line: blue bar with the player's score.
            FillRect(0, 10, ScreenWidth, 9, HudBarColor);
            BitmapFont.DrawText(this, "PLAYER-1 " + score, 8, 11, HudBarTextColor);
        }

        // ---------------------------------------------------------------------------
        //  Centre-screen messages
        // ---------------------------------------------------------------------------
        private void DrawMessages()
        {
            switch (state)
            {
                case GameState.WaitingToServe:
                    DrawCentredText("PRESS SPACE TO SERVE", 100, WhiteColor);
                    DrawCentredText("SERVING IN " + (int)Math.Ceiling(Math.Max(0f, serveCountdown)), 112, CyanColor);
                    break;

                case GameState.BoardSetup:
                    DrawCentredText("GET READY!", 110, WhiteColor);
                    break;

                case GameState.GameOver:
                    DrawCentredText(boardWasCleared ? "ALL ROCKS CLEARED!" : "GAME OVER", 100, WhiteColor);
                    DrawCentredText("PRESS ENTER TO PLAY AGAIN", 112, CyanColor);
                    break;
            }

            if (bannerTimer > 0f)
            {
                DrawCentredText("EXTENDED PLAY!", 80, YellowColor);
            }
        }

        private void DrawCentredText(string text, int y, Color color)
        {
            int x = (ScreenWidth - BitmapFont.MeasureWidth(text)) / 2;
            BitmapFont.DrawText(this, text, x, y, color);
        }

        // ---------------------------------------------------------------------------
        //  Low level drawing helpers (used by the font class too)
        // ---------------------------------------------------------------------------

        // Draws a solid rectangle in virtual pixels.
        public void FillRect(int x, int y, int width, int height, Color color)
        {
            spriteBatch.Draw(pixel, new Rectangle(x, y, width, height), color);
        }

        // Draws a hand-drawn sprite given as lines of text: '#' = pixel, anything else = empty.
        public void DrawSprite(string[] sprite, int x, int y, Color color)
        {
            for (int row = 0; row < sprite.Length; row++)
            {
                string line = sprite[row];
                for (int column = 0; column < line.Length; column++)
                {
                    if (line[column] == '#')
                    {
                        FillRect(x + column, y + row, 1, 1, color);
                    }
                }
            }
        }
    }

    // =================================================================================
    //  A hand-made 5x7 bitmap font (no SpriteFont needed)
    //  Each glyph is 7 lines of 5 characters, '#' = pixel.
    // =================================================================================
    public static class BitmapFont
    {
        private const int GlyphWidth = 5;
        private const int GlyphHeight = 7;
        private const int Advance = 6;     // 5 pixels for the letter + 1 pixel gap

        private static readonly Dictionary<char, string[]> glyphs = new Dictionary<char, string[]>();

        // Width in pixels of a piece of text.
        public static int MeasureWidth(string text)
        {
            return text.Length * Advance - 1;
        }

        // Draws text with its top-left corner at (x, y).
        public static void DrawText(AvalancheGame game, string text, int x, int y, Color color)
        {
            foreach (char letter in text)
            {
                string[] glyph;
                if (glyphs.TryGetValue(letter, out glyph))
                {
                    game.DrawSprite(glyph, x, y, color);
                }
                x += Advance;   // Unknown characters (and the space) simply leave a gap
            }
        }

        // Fills the glyph table. Called once at start-up.
        public static void Build()
        {
            if (glyphs.Count > 0) return;

            // ---- Letters ----
            Add('A', ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#");
            Add('B', "####.", "#...#", "#...#", "####.", "#...#", "#...#", "####.");
            Add('C', ".###.", "#...#", "#....", "#....", "#....", "#...#", ".###.");
            Add('D', "####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####.");
            Add('E', "#####", "#....", "#....", "####.", "#....", "#....", "#####");
            Add('F', "#####", "#....", "#....", "####.", "#....", "#....", "#....");
            Add('G', ".###.", "#...#", "#....", "#.###", "#...#", "#...#", ".###.");
            Add('H', "#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#");
            Add('I', ".###.", "..#..", "..#..", "..#..", "..#..", "..#..", ".###.");
            Add('J', "..###", "...#.", "...#.", "...#.", "...#.", "#..#.", ".##..");
            Add('K', "#...#", "#..#.", "#.#..", "##...", "#.#..", "#..#.", "#...#");
            Add('L', "#....", "#....", "#....", "#....", "#....", "#....", "#####");
            Add('M', "#...#", "##.##", "#.#.#", "#.#.#", "#...#", "#...#", "#...#");
            Add('N', "#...#", "##..#", "#.#.#", "#..##", "#...#", "#...#", "#...#");
            Add('O', ".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###.");
            Add('P', "####.", "#...#", "#...#", "####.", "#....", "#....", "#....");
            Add('Q', ".###.", "#...#", "#...#", "#...#", "#.#.#", "#..#.", ".##.#");
            Add('R', "####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#");
            Add('S', ".####", "#....", "#....", ".###.", "....#", "....#", "####.");
            Add('T', "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#..");
            Add('U', "#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###.");
            Add('V', "#...#", "#...#", "#...#", "#...#", "#...#", ".#.#.", "..#..");
            Add('W', "#...#", "#...#", "#...#", "#.#.#", "#.#.#", "##.##", "#...#");
            Add('X', "#...#", "#...#", ".#.#.", "..#..", ".#.#.", "#...#", "#...#");
            Add('Y', "#...#", "#...#", ".#.#.", "..#..", "..#..", "..#..", "..#..");
            Add('Z', "#####", "....#", "...#.", "..#..", ".#...", "#....", "#####");

            // ---- Digits ----
            Add('0', ".###.", "#...#", "#..##", "#.#.#", "##..#", "#...#", ".###.");
            Add('1', "..#..", ".##..", "..#..", "..#..", "..#..", "..#..", ".###.");
            Add('2', ".###.", "#...#", "....#", "...#.", "..#..", ".#...", "#####");
            Add('3', "####.", "....#", "....#", ".###.", "....#", "....#", "####.");
            Add('4', "...#.", "..##.", ".#.#.", "#..#.", "#####", "...#.", "...#.");
            Add('5', "#####", "#....", "####.", "....#", "....#", "#...#", ".###.");
            Add('6', ".###.", "#....", "#....", "####.", "#...#", "#...#", ".###.");
            Add('7', "#####", "....#", "...#.", "..#..", ".#...", ".#...", ".#...");
            Add('8', ".###.", "#...#", "#...#", ".###.", "#...#", "#...#", ".###.");
            Add('9', ".###.", "#...#", "#...#", ".####", "....#", "....#", ".###.");

            // ---- Punctuation ----
            Add('-', ".....", ".....", ".....", "#####", ".....", ".....", ".....");
            Add('!', "..#..", "..#..", "..#..", "..#..", "..#..", ".....", "..#..");
            Add(':', ".....", "..#..", ".....", ".....", ".....", "..#..", ".....");
        }

        // Stores one glyph (must be exactly 7 lines of 5 characters).
        private static void Add(char letter, params string[] lines)
        {
            glyphs[letter] = lines;
        }
    }
}
