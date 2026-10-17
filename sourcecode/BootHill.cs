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

namespace BootHillClone
{
    // =================================================================================
    //  1. PROGRAM ENTRY POINT
    // =================================================================================
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            using (var game = new BootHillGame())
            {
                game.Run();
            }
        }
    }

    // =================================================================================
    //  2. SETTINGS - change these numbers to tune the game
    // =================================================================================
    public static class Settings
    {
        // The game is drawn on a tiny "arcade monitor" and then scaled up to the window.
        public const int ScreenWidth = 256;
        public const int ScreenHeight = 224;

        // The area where cowboys walk and bullets fly (the top strip is for the scores,
        // the bottom strip is for the bullet counters).
        public const int PlayfieldTop = 16;
        public const int PlayfieldBottom = 208;

        public const float RoundSeconds = 90f;      // the original default: ninety seconds
        public const int BulletsPerReload = 6;      // each cowboy gets six bullets
        public const float CowboySpeed = 70f;       // pixels per second
        public const float BulletSpeed = 150f;      // horizontal pixels per second
        public const float AimSlope = 0.4f;         // how steep an "aimed up/down" shot is
        public const int GunLength = 6;             // length of the pistol in pixels
        public const float HitPauseSeconds = 1.5f;  // how long the "UMF !" scene lasts
        public const float WagonSpeed = 22f;        // how fast the wagon rolls up and down
    }

    // =================================================================================
    //  3. PIXEL ART - sprites are lists of text rows. '#' is a lit pixel, '.' is empty.
    // =================================================================================
    public static class PixelArt
    {
        // A cowboy, 12 pixels wide and 20 pixels tall. (The pistol is added when drawing.)
        public static readonly string[] CowboyStanding =
        {
            "....####....",   // hat
            "....####....",
            ".##########.",   // hat brim
            "....####....",   // face
            "....####....",
            ".....##.....",   // neck
            "..########..",   // shoulders
            ".##########.",
            ".##.####.##.",   // arms
            ".##.####.##.",
            "...######...",   // belt and body
            "...######...",
            "...######...",
            "....#..#....",   // legs
            "....#..#....",
            "....#..#....",
            "...##..##...",
            "...##..##...",
            "..###..###..",   // boots
            "..###..###..",
        };

        // The same cowboy after being shot: arms thrown up in the air.
        public static readonly string[] CowboyShot =
        {
            "..#.####.#..",
            "..#.####.#..",
            ".##########.",
            "..#.####.#..",
            "..#.####.#..",
            "..#..##..#..",
            "..########..",
            "...######...",
            "...######...",
            "...######...",
            "...######...",
            "...######...",
            "....#..#....",
            "....#..#....",
            "....#..#....",
            "...#....#...",
            "...#....#...",
            "..##....##..",
            "..#......#..",
            "..##....##..",
        };

        // A cactus, 9 pixels wide and 18 tall.
        public static readonly string[] Cactus =
        {
            "....##...",
            "....##...",
            "....##..#",
            "#...##..#",
            "#...##..#",
            "#...##..#",
            "##..##.##",
            ".#..##.#.",
            ".######..",
            "..#####..",
            "....##...",
            "....##...",
            "....##...",
            "....##...",
            "....##...",
            "....##...",
            "...####..",
            "...####..",
        };

        // A covered wagon, 28 pixels wide and 16 tall: canopy, box, and two spoked wheels.
        public static readonly string[] Wagon =
        {
            "......############..........",
            "....##............##........",
            "...#................#.......",
            "..#..................#......",
            ".#....................#.....",
            ".######################.....",
            ".#....................#.....",
            ".#....................#.....",
            ".######################.....",
            ".....#####.........#####....",
            "....#..#..#.......#..#..#...",
            "...#...#...#.....#...#...#..",
            "...#########.....#########..",
            "...#...#...#.....#...#...#..",
            "....#..#..#.......#..#..#...",
            ".....#####.........#####....",
        };

        // A small bullet icon, used for the "bullets left" counters at the bottom.
        public static readonly string[] BulletIcon =
        {
            ".#.",
            "###",
            "###",
            "###",
            "###",
            "###",
        };

        // Draws a sprite: every '#' becomes a 1x1 white square.
        public static void Draw(SpriteBatch spriteBatch, Texture2D pixel, string[] sprite, int x, int y, Color color)
        {
            for (int row = 0; row < sprite.Length; row++)
            {
                for (int column = 0; column < sprite[row].Length; column++)
                {
                    if (sprite[row][column] == '#')
                    {
                        spriteBatch.Draw(pixel, new Rectangle(x + column, y + row, 1, 1), color);
                    }
                }
            }
        }
    }

    // =================================================================================
    //  4. BITMAP FONT - each letter is 5 pixels wide and 7 pixels tall
    // =================================================================================
    public static class BitmapFont
    {
        public const int GlyphWidth = 5;
        public const int GlyphHeight = 7;
        public const int Spacing = 1;   // empty pixels between two letters

        private static readonly Dictionary<char, string[]> glyphs = new Dictionary<char, string[]>();

        // Fill the table once, when the class is first used.
        static BitmapFont()
        {
            // Digits
            Add('0', "01110", "10001", "10011", "10101", "11001", "10001", "01110");
            Add('1', "00100", "01100", "00100", "00100", "00100", "00100", "01110");
            Add('2', "01110", "10001", "00001", "00010", "00100", "01000", "11111");
            Add('3', "11110", "00001", "00001", "01110", "00001", "00001", "11110");
            Add('4', "00010", "00110", "01010", "10010", "11111", "00010", "00010");
            Add('5', "11111", "10000", "11110", "00001", "00001", "10001", "01110");
            Add('6', "00110", "01000", "10000", "11110", "10001", "10001", "01110");
            Add('7', "11111", "00001", "00010", "00100", "01000", "01000", "01000");
            Add('8', "01110", "10001", "10001", "01110", "10001", "10001", "01110");
            Add('9', "01110", "10001", "10001", "01111", "00001", "00010", "01100");

            // Letters
            Add('A', "01110", "10001", "10001", "11111", "10001", "10001", "10001");
            Add('B', "11110", "10001", "10001", "11110", "10001", "10001", "11110");
            Add('C', "01110", "10001", "10000", "10000", "10000", "10001", "01110");
            Add('D', "11110", "10001", "10001", "10001", "10001", "10001", "11110");
            Add('E', "11111", "10000", "10000", "11110", "10000", "10000", "11111");
            Add('F', "11111", "10000", "10000", "11110", "10000", "10000", "10000");
            Add('G', "01110", "10001", "10000", "10111", "10001", "10001", "01111");
            Add('H', "10001", "10001", "10001", "11111", "10001", "10001", "10001");
            Add('I', "01110", "00100", "00100", "00100", "00100", "00100", "01110");
            Add('J', "00111", "00010", "00010", "00010", "00010", "10010", "01100");
            Add('K', "10001", "10010", "10100", "11000", "10100", "10010", "10001");
            Add('L', "10000", "10000", "10000", "10000", "10000", "10000", "11111");
            Add('M', "10001", "11011", "10101", "10101", "10001", "10001", "10001");
            Add('N', "10001", "11001", "10101", "10011", "10001", "10001", "10001");
            Add('O', "01110", "10001", "10001", "10001", "10001", "10001", "01110");
            Add('P', "11110", "10001", "10001", "11110", "10000", "10000", "10000");
            Add('Q', "01110", "10001", "10001", "10001", "10101", "10010", "01101");
            Add('R', "11110", "10001", "10001", "11110", "10100", "10010", "10001");
            Add('S', "01111", "10000", "10000", "01110", "00001", "00001", "11110");
            Add('T', "11111", "00100", "00100", "00100", "00100", "00100", "00100");
            Add('U', "10001", "10001", "10001", "10001", "10001", "10001", "01110");
            Add('V', "10001", "10001", "10001", "10001", "10001", "01010", "00100");
            Add('W', "10001", "10001", "10001", "10101", "10101", "11011", "10001");
            Add('X', "10001", "10001", "01010", "00100", "01010", "10001", "10001");
            Add('Y', "10001", "10001", "01010", "00100", "00100", "00100", "00100");
            Add('Z', "11111", "00001", "00010", "00100", "01000", "10000", "11111");

            // Punctuation
            Add('!', "00100", "00100", "00100", "00100", "00100", "00000", "00100");
            Add(':', "00000", "00100", "00000", "00000", "00000", "00100", "00000");
            Add('-', "00000", "00000", "00000", "11111", "00000", "00000", "00000");
            Add('.', "00000", "00000", "00000", "00000", "00000", "01100", "01100");
        }

        private static void Add(char character, params string[] rows)
        {
            glyphs[character] = rows;
        }

        // How many pixels wide a text will be (useful for centering).
        public static int MeasureWidth(string text, int scale)
        {
            if (text.Length == 0) return 0;
            return (text.Length * (GlyphWidth + Spacing) - Spacing) * scale;
        }

        // Draws text with its top-left corner at (x, y). 'scale' makes every pixel bigger.
        public static void Draw(SpriteBatch spriteBatch, Texture2D pixel, string text, int x, int y, int scale, Color color)
        {
            foreach (char character in text)
            {
                // Unknown characters (and the space) are simply left empty.
                if (glyphs.TryGetValue(char.ToUpperInvariant(character), out string[] rows))
                {
                    DrawGlyph(spriteBatch, pixel, rows, x, y, scale, color);
                }
                x += (GlyphWidth + Spacing) * scale;
            }
        }

        private static void DrawGlyph(SpriteBatch spriteBatch, Texture2D pixel, string[] rows, int x, int y, int scale, Color color)
        {
            for (int row = 0; row < GlyphHeight; row++)
            {
                for (int column = 0; column < GlyphWidth; column++)
                {
                    if (rows[row][column] == '1')
                    {
                        spriteBatch.Draw(pixel, new Rectangle(x + column * scale, y + row * scale, scale, scale), color);
                    }
                }
            }
        }
    }

    // =================================================================================
    //  5. BULLET AND COWBOY
    // =================================================================================
    public class Bullet
    {
        public bool IsFlying;
        public Vector2 Position;
        public Vector2 Velocity;   // pixels per second
    }

    // What a cowboy wants to do this frame. Humans fill this from the keyboard,
    // the computer opponent fills it from its "brain".
    public struct CowboyInput
    {
        public int Move;    // -1 = up, 0 = stand still, +1 = down
        public int Aim;     // -1 = aim up, 0 = straight ahead, +1 = aim down
        public bool Fire;   // true on the frame the trigger is pulled
    }

    public class Cowboy
    {
        public const int Width = 12;
        public const int Height = 20;

        public readonly float X;           // cowboys only move up and down, so X never changes
        public readonly int Direction;     // +1 = facing right (left player), -1 = facing left (right player)
        public float Y;                    // top edge of the cowboy
        public int Aim;                    // -1 up, 0 straight, +1 down
        public int BulletsLeft;
        public int Score;
        public bool IsHit;                 // true while the "UMF !" scene is playing
        public readonly Bullet Bullet = new Bullet();   // one bullet may be in the air at a time

        public Cowboy(float x, int direction)
        {
            X = x;
            Direction = direction;
            ResetForNewRound();
        }

        public float CenterY
        {
            get { return Y + Height / 2f; }
        }

        public Rectangle HitBox
        {
            get { return new Rectangle((int)Math.Round(X), (int)Math.Round(Y), Width, Height); }
        }

        // Where the pistol starts (next to the cowboy's hand).
        public Vector2 GunBase
        {
            get { return new Vector2(X + Width / 2f + Direction * 6, Y + 9); }
        }

        // Where the pistol ends - bullets leave from here.
        public Vector2 GunTip
        {
            get { return GunBase + new Vector2(Direction * Settings.GunLength, GunHeightOffset(Settings.GunLength)); }
        }

        // How many pixels the pistol has risen/fallen after 'distance' pixels of length.
        public float GunHeightOffset(int distance)
        {
            return (float)Math.Round(Aim * Settings.AimSlope * distance);
        }

        public void ResetForNewRound()
        {
            Y = (Settings.PlayfieldTop + Settings.PlayfieldBottom) / 2f - Height / 2f;
            Aim = 0;
            BulletsLeft = Settings.BulletsPerReload;
            IsHit = false;
            Bullet.IsFlying = false;
        }
    }

    // =================================================================================
    //  6. OBSTACLE - a cactus or the wagon. Bullets chew pixels out of it.
    // =================================================================================
    public class Obstacle
    {
        public Vector2 Position;           // top-left corner
        public float SpeedY;               // 0 for a cactus, non-zero for the rolling wagon

        private readonly string[] sprite;  // only used to draw the obstacle
        private readonly bool[,] pixels;   // true = this pixel is still there
        private readonly int width;
        private readonly int height;

        public Obstacle(string[] sprite, float x, float y, float speedY)
        {
            this.sprite = sprite;
            Position = new Vector2(x, y);
            SpeedY = speedY;

            // Rows may have different lengths, so use the longest one as the width.
            height = sprite.Length;
            width = 0;
            foreach (string row in sprite) width = Math.Max(width, row.Length);

            // Copy the text sprite into a grid of "pixel is present" flags.
            pixels = new bool[width, height];
            for (int row = 0; row < height; row++)
            {
                for (int column = 0; column < sprite[row].Length; column++)
                {
                    pixels[column, row] = sprite[row][column] == '#';
                }
            }
        }

        // Moving obstacles roll up and down, turning around at the edges of the playfield.
        public void Update(float deltaSeconds)
        {
            if (SpeedY == 0) return;

            Position.Y += SpeedY * deltaSeconds;

            if (Position.Y < Settings.PlayfieldTop)
            {
                Position.Y = Settings.PlayfieldTop;
                SpeedY = Math.Abs(SpeedY);
            }
            else if (Position.Y + height > Settings.PlayfieldBottom)
            {
                Position.Y = Settings.PlayfieldBottom - height;
                SpeedY = -Math.Abs(SpeedY);
            }
        }

        // Returns true if the bullet is touching a solid pixel. If so, a small chunk crumbles away.
        public bool TryHit(Vector2 bulletPosition)
        {
            int localX = (int)Math.Floor(bulletPosition.X - Position.X);
            int localY = (int)Math.Floor(bulletPosition.Y - (int)Math.Round(Position.Y));

            bool isInsideSprite = localX >= 0 && localX < width && localY >= 0 && localY < height;
            if (!isInsideSprite || !pixels[localX, localY]) return false;

            RemoveChunk(localX, localY);
            return true;
        }

        // Removes a 3x3 block of pixels around the impact point.
        private void RemoveChunk(int centerX, int centerY)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int x = centerX + dx;
                    int y = centerY + dy;
                    if (x >= 0 && x < width && y >= 0 && y < height)
                    {
                        pixels[x, y] = false;
                    }
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
        {
            int left = (int)Math.Round(Position.X);
            int top = (int)Math.Round(Position.Y);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (pixels[x, y])
                    {
                        spriteBatch.Draw(pixel, new Rectangle(left + x, top + y, 1, 1), Color.White);
                    }
                }
            }
        }
    }

    // =================================================================================
    //  7. COMPUTER BRAIN - plays the right-hand cowboy in 1-player mode.
    //     It "thinks" a few times per second (not every frame), so it is beatable.
    // =================================================================================
    public class ComputerBrain
    {
        private readonly Random random = new Random();
        private float secondsUntilNextThought;
        private int currentMove;
        private int currentAim;

        public CowboyInput Think(Cowboy me, Cowboy enemy, float deltaSeconds)
        {
            bool shouldFire = false;

            secondsUntilNextThought -= deltaSeconds;
            if (secondsUntilNextThought <= 0)
            {
                secondsUntilNextThought = 0.25f + (float)random.NextDouble() * 0.35f;
                shouldFire = MakeNewDecision(me, enemy);
            }

            // Between two thoughts the cowboy simply keeps doing what it decided last time.
            return new CowboyInput { Move = currentMove, Aim = currentAim, Fire = shouldFire };
        }

        // Picks a direction to walk and an angle to aim. Returns true if it wants to shoot now.
        private bool MakeNewDecision(Cowboy me, Cowboy enemy)
        {
            float distanceToEnemy = Math.Abs(enemy.X - me.X);

            // Try all three aim angles and keep the one whose bullet lands closest to the enemy.
            int bestAim = 0;
            float smallestMiss = float.MaxValue;
            for (int aim = -1; aim <= 1; aim++)
            {
                float landingY = me.GunBase.Y + aim * Settings.AimSlope * distanceToEnemy;
                float miss = Math.Abs(landingY - enemy.CenterY);
                if (miss < smallestMiss)
                {
                    smallestMiss = miss;
                    bestAim = aim;
                }
            }
            currentAim = bestAim;

            // Walk towards the height of the enemy...
            float heightDifference = enemy.CenterY - me.CenterY;
            if (heightDifference > 6) currentMove = 1;
            else if (heightDifference < -6) currentMove = -1;
            else currentMove = 0;

            // ...but sometimes wander randomly, which also makes the computer harder to hit.
            if (random.NextDouble() < 0.25) currentMove = random.Next(-1, 2);

            bool lookingGood = smallestMiss < 10;
            return lookingGood && random.NextDouble() < 0.7;
        }
    }

    // =================================================================================
    //  8. THE GAME
    // =================================================================================
    public enum GameState
    {
        Title,      // "press 1 or 2"
        Playing,    // the gunfight
        GameOver,   // time is up
    }

    public class BootHillGame : Game
    {
        // ---- Graphics plumbing ----
        private readonly GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private Texture2D pixel;               // a single white pixel; every drawing is made of these
        private RenderTarget2D screen;         // the small 256x224 "arcade screen"
        private int windowedWidth = Settings.ScreenWidth * 3;
        private int windowedHeight = Settings.ScreenHeight * 3;

        // ---- Input ----
        private KeyboardState keys;
        private KeyboardState previousKeys;

        // ---- Game data ----
        private GameState state = GameState.Title;
        private bool isTwoPlayerGame;
        private Cowboy leftCowboy;
        private Cowboy rightCowboy;
        private List<Obstacle> obstacles;
        private readonly ComputerBrain computerBrain = new ComputerBrain();
        private float secondsLeft;
        private float hitPauseSecondsLeft;     // > 0 while the "UMF !" scene is showing

        public BootHillGame()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = windowedWidth;
            graphics.PreferredBackBufferHeight = windowedHeight;
            Window.Title = "Boot Hill";
            Window.AllowUserResizing = true;
            IsMouseVisible = false;
        }

        // -----------------------------------------------------------------------------
        //  Setup
        // -----------------------------------------------------------------------------
        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            pixel = new Texture2D(GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });

            screen = new RenderTarget2D(GraphicsDevice, Settings.ScreenWidth, Settings.ScreenHeight);

            CreateCowboysAndObstacles();
        }

        private void CreateCowboysAndObstacles()
        {
            leftCowboy = new Cowboy(x: 20, direction: +1);
            rightCowboy = new Cowboy(x: Settings.ScreenWidth - 20 - Cowboy.Width, direction: -1);

            obstacles = new List<Obstacle>
            {
                // Cacti scattered around the playfield (never moving).
                new Obstacle(PixelArt.Cactus, 62, 36, 0),
                new Obstacle(PixelArt.Cactus, 84, 122, 0),
                new Obstacle(PixelArt.Cactus, 58, 172, 0),
                new Obstacle(PixelArt.Cactus, 188, 48, 0),
                new Obstacle(PixelArt.Cactus, 164, 112, 0),
                new Obstacle(PixelArt.Cactus, 192, 164, 0),

                // The wagon rolls up and down the middle of the playfield.
                new Obstacle(PixelArt.Wagon, 114, 100, Settings.WagonSpeed),
            };
        }

        private void StartNewGame(bool twoPlayers)
        {
            isTwoPlayerGame = twoPlayers;
            CreateCowboysAndObstacles();   // fresh, undamaged obstacles and zero scores
            secondsLeft = Settings.RoundSeconds;
            hitPauseSecondsLeft = 0;
            state = GameState.Playing;
        }

        // -----------------------------------------------------------------------------
        //  Update (runs 60 times per second)
        // -----------------------------------------------------------------------------
        protected override void Update(GameTime gameTime)
        {
            previousKeys = keys;
            keys = Keyboard.GetState();
            float deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (WasPressed(Keys.Escape)) Exit();
            if (WasPressed(Keys.F11)) ToggleFullScreen();

            switch (state)
            {
                case GameState.Title: UpdateTitle(); break;
                case GameState.Playing: UpdatePlaying(deltaSeconds); break;
                case GameState.GameOver: UpdateGameOver(); break;
            }

            base.Update(gameTime);
        }

        private void UpdateTitle()
        {
            if (WasPressed(Keys.D1) || WasPressed(Keys.NumPad1)) StartNewGame(twoPlayers: false);
            if (WasPressed(Keys.D2) || WasPressed(Keys.NumPad2)) StartNewGame(twoPlayers: true);
        }

        private void UpdateGameOver()
        {
            if (WasPressed(Keys.Enter)) state = GameState.Title;
        }

        private void UpdatePlaying(float deltaSeconds)
        {
            // While the "UMF !" scene is showing, the game is frozen.
            if (hitPauseSecondsLeft > 0)
            {
                hitPauseSecondsLeft -= deltaSeconds;
                if (hitPauseSecondsLeft <= 0) StartNextRound();
                return;
            }

            // The clock.
            secondsLeft -= deltaSeconds;
            if (secondsLeft <= 0)
            {
                secondsLeft = 0;
                state = GameState.GameOver;
                return;
            }

            // The wagon rolls.
            foreach (Obstacle obstacle in obstacles) obstacle.Update(deltaSeconds);

            // Cowboys: read what they want to do, then do it.
            CowboyInput leftInput = ReadKeyboard(Keys.W, Keys.S, Keys.E, Keys.D, Keys.Space);
            CowboyInput rightInput = isTwoPlayerGame
                ? ReadKeyboard(Keys.I, Keys.K, Keys.O, Keys.L, Keys.RightShift)
                : computerBrain.Think(rightCowboy, leftCowboy, deltaSeconds);

            ApplyInput(leftCowboy, leftInput, deltaSeconds);
            ApplyInput(rightCowboy, rightInput, deltaSeconds);

            // Bullets fly.
            MoveBullet(leftCowboy, rightCowboy, deltaSeconds);
            MoveBullet(rightCowboy, leftCowboy, deltaSeconds);

            ReloadIfEveryoneIsOutOfBullets();
        }

        // Turns the state of five keyboard keys into a CowboyInput.
        private CowboyInput ReadKeyboard(Keys moveUp, Keys moveDown, Keys aimUp, Keys aimDown, Keys fire)
        {
            return new CowboyInput
            {
                Move = AxisFromKeys(moveUp, moveDown),
                Aim = AxisFromKeys(aimUp, aimDown),
                Fire = WasPressed(fire),
            };
        }

        // Returns -1 when only 'negativeKey' is held, +1 when only 'positiveKey' is held, otherwise 0.
        private int AxisFromKeys(Keys negativeKey, Keys positiveKey)
        {
            int axis = 0;
            if (keys.IsKeyDown(negativeKey)) axis -= 1;
            if (keys.IsKeyDown(positiveKey)) axis += 1;
            return axis;
        }

        private void ApplyInput(Cowboy cowboy, CowboyInput input, float deltaSeconds)
        {
            cowboy.Aim = input.Aim;

            // Walk up or down, but stay inside the playfield.
            cowboy.Y += input.Move * Settings.CowboySpeed * deltaSeconds;
            cowboy.Y = MathHelper.Clamp(cowboy.Y, Settings.PlayfieldTop, Settings.PlayfieldBottom - Cowboy.Height);

            if (input.Fire) TryToFire(cowboy);
        }

        private void TryToFire(Cowboy cowboy)
        {
            bool canFire = !cowboy.Bullet.IsFlying && cowboy.BulletsLeft > 0;
            if (!canFire) return;

            cowboy.BulletsLeft -= 1;
            cowboy.Bullet.IsFlying = true;
            cowboy.Bullet.Position = cowboy.GunTip;
            cowboy.Bullet.Velocity = new Vector2(
                cowboy.Direction * Settings.BulletSpeed,
                cowboy.Aim * Settings.AimSlope * Settings.BulletSpeed);
        }

        // Moves one bullet. It travels in steps of at most one pixel so it can never
        // jump over a thin obstacle or over a cowboy.
        private void MoveBullet(Cowboy shooter, Cowboy target, float deltaSeconds)
        {
            Bullet bullet = shooter.Bullet;
            if (!bullet.IsFlying || hitPauseSecondsLeft > 0) return;

            Vector2 totalMovement = bullet.Velocity * deltaSeconds;
            int stepCount = Math.Max(1, (int)Math.Ceiling(Math.Max(Math.Abs(totalMovement.X), Math.Abs(totalMovement.Y))));
            Vector2 movementPerStep = totalMovement / stepCount;

            for (int step = 0; step < stepCount; step++)
            {
                bullet.Position += movementPerStep;

                if (IsOutsidePlayfield(bullet.Position))
                {
                    bullet.IsFlying = false;
                    return;
                }

                if (HitAnyObstacle(bullet.Position))
                {
                    bullet.IsFlying = false;
                    return;
                }

                if (target.HitBox.Contains(bullet.Position.ToPoint()))
                {
                    ScoreHit(shooter, target);
                    return;
                }
            }
        }

        private bool IsOutsidePlayfield(Vector2 position)
        {
            return position.X < 0 || position.X >= Settings.ScreenWidth
                || position.Y < Settings.PlayfieldTop || position.Y >= Settings.PlayfieldBottom;
        }

        private bool HitAnyObstacle(Vector2 position)
        {
            foreach (Obstacle obstacle in obstacles)
            {
                if (obstacle.TryHit(position)) return true;
            }
            return false;
        }

        // A cowboy was shot: give the shooter a point and start the "UMF !" scene.
        private void ScoreHit(Cowboy shooter, Cowboy victim)
        {
            shooter.Score += 1;
            victim.IsHit = true;
            shooter.Bullet.IsFlying = false;
            victim.Bullet.IsFlying = false;
            hitPauseSecondsLeft = Settings.HitPauseSeconds;
        }

        // After the "UMF !" scene both cowboys return to the middle with a full gun.
        // The obstacles stay damaged.
        private void StartNextRound()
        {
            leftCowboy.ResetForNewRound();
            rightCowboy.ResetForNewRound();
        }

        // Nobody can shoot any more? Then both cowboys reload so the game never gets stuck.
        private void ReloadIfEveryoneIsOutOfBullets()
        {
            bool bothEmpty = leftCowboy.BulletsLeft == 0 && rightCowboy.BulletsLeft == 0;
            bool nothingFlying = !leftCowboy.Bullet.IsFlying && !rightCowboy.Bullet.IsFlying;

            if (bothEmpty && nothingFlying)
            {
                leftCowboy.BulletsLeft = Settings.BulletsPerReload;
                rightCowboy.BulletsLeft = Settings.BulletsPerReload;
            }
        }

        // -----------------------------------------------------------------------------
        //  Keyboard helper and full screen
        // -----------------------------------------------------------------------------

        // True only on the frame in which the key goes from "up" to "down".
        private bool WasPressed(Keys key)
        {
            return keys.IsKeyDown(key) && previousKeys.IsKeyUp(key);
        }

        private void ToggleFullScreen()
        {
            if (!graphics.IsFullScreen)
            {
                // Remember the window size so we can come back to it.
                windowedWidth = Window.ClientBounds.Width;
                windowedHeight = Window.ClientBounds.Height;

                DisplayMode desktop = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
                graphics.PreferredBackBufferWidth = desktop.Width;
                graphics.PreferredBackBufferHeight = desktop.Height;
                graphics.HardwareModeSwitch = false;   // "borderless" full screen: fast and friendly
                graphics.IsFullScreen = true;
            }
            else
            {
                graphics.IsFullScreen = false;
                graphics.PreferredBackBufferWidth = windowedWidth;
                graphics.PreferredBackBufferHeight = windowedHeight;
            }
            graphics.ApplyChanges();
        }

        // -----------------------------------------------------------------------------
        //  Drawing
        // -----------------------------------------------------------------------------
        protected override void Draw(GameTime gameTime)
        {
            // Step 1: draw the whole game on the small 256x224 "arcade screen".
            GraphicsDevice.SetRenderTarget(screen);
            GraphicsDevice.Clear(Color.Black);
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            switch (state)
            {
                case GameState.Title:
                    DrawTitleScreen();
                    break;

                case GameState.Playing:
                    DrawPlayfield();
                    DrawHud();
                    break;

                case GameState.GameOver:
                    DrawPlayfield();
                    DrawHud();
                    DrawGameOverMessage();
                    break;
            }

            spriteBatch.End();

            // Step 2: stretch that small picture to fill the window (black bars if needed).
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.Black);
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            spriteBatch.Draw(screen, CalculateScreenRectangle(), Color.White);
            spriteBatch.End();

            base.Draw(gameTime);
        }

        // Finds the biggest centered rectangle with the arcade screen's proportions.
        // When there is room, the scale is a whole number so the pixels stay crisp.
        private Rectangle CalculateScreenRectangle()
        {
            int windowWidth = GraphicsDevice.PresentationParameters.BackBufferWidth;
            int windowHeight = GraphicsDevice.PresentationParameters.BackBufferHeight;

            float scale = Math.Min(windowWidth / (float)Settings.ScreenWidth, windowHeight / (float)Settings.ScreenHeight);
            if (scale >= 1f) scale = (float)Math.Floor(scale);

            int width = (int)(Settings.ScreenWidth * scale);
            int height = (int)(Settings.ScreenHeight * scale);
            return new Rectangle((windowWidth - width) / 2, (windowHeight - height) / 2, width, height);
        }

        // ---- Title screen ----
        private void DrawTitleScreen()
        {
            DrawCenteredText("BOOT HILL", 30, 4);

            DrawCenteredText("PRESS 1 FOR ONE PLAYER", 90, 1);
            DrawCenteredText("PRESS 2 FOR TWO PLAYERS", 105, 1);

            DrawCenteredText("P1  W S MOVE   E D AIM   SPACE FIRE", 140, 1);
            DrawCenteredText("P2  I K MOVE   O L AIM   R SHIFT FIRE", 152, 1);
            DrawCenteredText("F11 FULL SCREEN    ESC QUIT", 172, 1);

            // Two cowboys facing each other, just for decoration.
            PixelArt.Draw(spriteBatch, pixel, PixelArt.CowboyStanding, 40, 190, Color.White);
            PixelArt.Draw(spriteBatch, pixel, PixelArt.CowboyStanding, Settings.ScreenWidth - 40 - Cowboy.Width, 190, Color.White);
        }

        // ---- The gunfight ----
        private void DrawPlayfield()
        {
            foreach (Obstacle obstacle in obstacles) obstacle.Draw(spriteBatch, pixel);

            DrawCowboy(leftCowboy);
            DrawCowboy(rightCowboy);

            DrawBullet(leftCowboy.Bullet);
            DrawBullet(rightCowboy.Bullet);
        }

        private void DrawCowboy(Cowboy cowboy)
        {
            int x = (int)Math.Round(cowboy.X);
            int y = (int)Math.Round(cowboy.Y);

            if (cowboy.IsHit)
            {
                // A shot cowboy trembles left and right, and shouts.
                int shake = ((int)(hitPauseSecondsLeft * 12)) % 2;
                PixelArt.Draw(spriteBatch, pixel, PixelArt.CowboyShot, x + shake, y, Color.White);

                const string shout = "UMF !";
                int textX = cowboy.Direction == 1 ? x + Cowboy.Width + 4 : x - BitmapFont.MeasureWidth(shout, 1) - 4;
                BitmapFont.Draw(spriteBatch, pixel, shout, textX, y, 1, Color.White);
                return;
            }

            PixelArt.Draw(spriteBatch, pixel, PixelArt.CowboyStanding, x, y, Color.White);
            DrawPistol(cowboy);
        }

        // The pistol is a short line of pixels that points straight, up or down.
        private void DrawPistol(Cowboy cowboy)
        {
            Vector2 gunBase = cowboy.GunBase;

            for (int i = 0; i <= Settings.GunLength; i++)
            {
                int pixelX = (int)Math.Round(gunBase.X + cowboy.Direction * i);
                int pixelY = (int)Math.Round(gunBase.Y + cowboy.GunHeightOffset(i));
                spriteBatch.Draw(pixel, new Rectangle(pixelX, pixelY, 1, 1), Color.White);
            }
        }

        private void DrawBullet(Bullet bullet)
        {
            if (!bullet.IsFlying) return;

            int x = (int)Math.Round(bullet.Position.X);
            int y = (int)Math.Round(bullet.Position.Y);
            spriteBatch.Draw(pixel, new Rectangle(x - 1, y - 1, 2, 2), Color.White);
        }

        // ---- Scores, clock and remaining bullets ----
        private void DrawHud()
        {
            // Left score, clock in the middle, right score.
            BitmapFont.Draw(spriteBatch, pixel, leftCowboy.Score.ToString(), 8, 4, 1, Color.White);

            string clock = ((int)Math.Ceiling(secondsLeft)).ToString();
            DrawCenteredText(clock, 4, 1);

            string rightScore = rightCowboy.Score.ToString();
            int rightScoreX = Settings.ScreenWidth - 8 - BitmapFont.MeasureWidth(rightScore, 1);
            BitmapFont.Draw(spriteBatch, pixel, rightScore, rightScoreX, 4, 1, Color.White);

            // One little bullet icon for every bullet left, along the bottom edge.
            const int iconWidth = 3;
            const int iconSpacing = 6;
            const int iconY = Settings.ScreenHeight - 12;

            for (int i = 0; i < leftCowboy.BulletsLeft; i++)
            {
                PixelArt.Draw(spriteBatch, pixel, PixelArt.BulletIcon, 24 + i * iconSpacing, iconY, Color.White);
            }
            for (int i = 0; i < rightCowboy.BulletsLeft; i++)
            {
                int x = Settings.ScreenWidth - 24 - iconWidth - i * iconSpacing;
                PixelArt.Draw(spriteBatch, pixel, PixelArt.BulletIcon, x, iconY, Color.White);
            }
        }

        // ---- "Game over" message on top of the frozen playfield ----
        private void DrawGameOverMessage()
        {
            string result;
            if (leftCowboy.Score == rightCowboy.Score)
            {
                result = "DRAW";
            }
            else if (leftCowboy.Score > rightCowboy.Score)
            {
                result = "PLAYER 1 WINS";
            }
            else
            {
                result = isTwoPlayerGame ? "PLAYER 2 WINS" : "COMPUTER WINS";
            }

            // A black box so the text stays readable even in front of a cactus.
            spriteBatch.Draw(pixel, new Rectangle(40, 70, Settings.ScreenWidth - 80, 70), Color.Black);

            DrawCenteredText("GAME OVER", 78, 2);
            DrawCenteredText(result, 102, 1);
            DrawCenteredText("PRESS ENTER", 120, 1);
        }

        // Draws text horizontally centered on the screen.
        private void DrawCenteredText(string text, int y, int scale)
        {
            int x = (Settings.ScreenWidth - BitmapFont.MeasureWidth(text, scale)) / 2;
            BitmapFont.Draw(spriteBatch, pixel, text, x, y, scale, Color.White);
        }
    }
}
