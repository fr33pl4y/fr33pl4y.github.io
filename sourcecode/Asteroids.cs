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
// https://fr33pl4y.github.io/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Asteroids;

public static class Program
{
    public static void Main()
    {
		using var game = new Game1();
        game.Run();
    }
}

public class Game1 : Game
{
    private readonly GraphicsDeviceManager graphics;
    private SpriteBatch spriteBatch;
    private Texture2D pixel;

    private Player player;

    private readonly List<Asteroid> asteroids = new();
    private readonly List<Bullet> bullets = new();

    private readonly Random random = new();

    private int score;
    private int lives = 3;
    private int wave = 1;

    private bool gameOver;

    private float respawnTimer;
    private float waveTimer;

    private KeyboardState previousKeyboard;

    private int ScreenWidth => GraphicsDevice.Viewport.Width;
    private int ScreenHeight => GraphicsDevice.Viewport.Height;

    public Game1()
    {
        graphics = new GraphicsDeviceManager(this);

        Content.RootDirectory = "Content";

        IsMouseVisible = false;

        graphics.PreferredBackBufferWidth = 1280;
        graphics.PreferredBackBufferHeight = 720;

        graphics.SynchronizeWithVerticalRetrace = true;
    }

    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override void LoadContent()
    {
        spriteBatch = new SpriteBatch(GraphicsDevice);

        // The entire game uses one 1x1 white texture.
        // No SpriteFont or Content assets are required.
        pixel = new Texture2D(
            GraphicsDevice,
            1,
            1);

        pixel.SetData(new[]
        {
            Color.White
        });

        player = new Player(
            pixel,
            new Vector2(
                ScreenWidth / 2f,
                ScreenHeight / 2f));

        StartWave();
    }

    // ============================================================
    // WAVE MANAGEMENT
    // ============================================================

    private void StartWave()
    {
        asteroids.Clear();
        bullets.Clear();

        int asteroidCount = 3 + wave * 2;

        for (int i = 0; i < asteroidCount; i++)
        {
            SpawnLargeAsteroid();
        }

        waveTimer = 0f;
    }

    private void SpawnLargeAsteroid()
    {
        Vector2 position;

        do
        {
            position = new Vector2(
                random.Next(0, ScreenWidth),
                random.Next(0, ScreenHeight));

        } while (
            Vector2.Distance(
                position,
                player.Position) < 180f);

        float angle =
            (float)random.NextDouble() *
            MathHelper.TwoPi;

        float speed =
            50f +
            (float)random.NextDouble() * 70f;

        Vector2 velocity =
            new Vector2(
                MathF.Cos(angle),
                MathF.Sin(angle)) *
            speed;

        asteroids.Add(
            new Asteroid(
                pixel,
                position,
                velocity,
                AsteroidSize.Large,
                random));
    }

    private void RestartGame()
    {
        score = 0;
        lives = 3;
        wave = 1;

        gameOver = false;
        respawnTimer = 0f;

        player.Reset(
            new Vector2(
                ScreenWidth / 2f,
                ScreenHeight / 2f));

        StartWave();
    }

    // ============================================================
    // UPDATE
    // ============================================================

    protected override void Update(GameTime gameTime)
    {
        KeyboardState keyboard = Keyboard.GetState();

        if (keyboard.IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        // --------------------------------------------------------
        // GAME OVER
        // --------------------------------------------------------

        if (gameOver)
        {
            if (Pressed(keyboard, Keys.Enter) ||
                Pressed(keyboard, Keys.Space))
            {
                RestartGame();
            }

            previousKeyboard = keyboard;

            base.Update(gameTime);
            return;
        }

        float dt =
            (float)gameTime.ElapsedGameTime.TotalSeconds;

        // --------------------------------------------------------
        // PLAYER
        // --------------------------------------------------------

        if (player.Alive)
        {
            player.Update(
                gameTime,
                ScreenWidth,
                ScreenHeight);

            Bullet bullet = player.TryShoot();

            if (bullet != null)
            {
                bullets.Add(bullet);
            }
        }
        else
        {
            respawnTimer -= dt;

            if (respawnTimer <= 0 &&
                lives > 0)
            {
                player.Reset(
                    new Vector2(
                        ScreenWidth / 2f,
                        ScreenHeight / 2f));
            }
        }

        // --------------------------------------------------------
        // BULLETS
        // --------------------------------------------------------

        foreach (Bullet bullet in bullets)
        {
            bullet.Update(
                gameTime,
                ScreenWidth,
                ScreenHeight);
        }

        // --------------------------------------------------------
        // ASTEROIDS
        // --------------------------------------------------------

        foreach (Asteroid asteroid in asteroids)
        {
            asteroid.Update(
                gameTime,
                ScreenWidth,
                ScreenHeight);
        }

        // --------------------------------------------------------
        // COLLISIONS
        // --------------------------------------------------------

        HandleBulletCollisions();
        HandlePlayerCollisions();

        asteroids.RemoveAll(
            asteroid => !asteroid.Active);

        bullets.RemoveAll(
            bullet => !bullet.Active);

        // --------------------------------------------------------
        // NEXT WAVE
        // --------------------------------------------------------

        if (asteroids.Count == 0)
        {
            waveTimer += dt;

            if (waveTimer > 1f)
            {
                wave++;

                StartWave();
            }
        }

        previousKeyboard = keyboard;

        base.Update(gameTime);
    }

    // ============================================================
    // BULLET COLLISIONS
    // ============================================================

    private void HandleBulletCollisions()
    {
        foreach (Bullet bullet in bullets)
        {
            if (!bullet.Active)
                continue;

            foreach (Asteroid asteroid in asteroids)
            {
                if (!asteroid.Active)
                    continue;

                float distance =
                    Vector2.Distance(
                        bullet.Position,
                        asteroid.Position);

                if (distance <
                    bullet.Radius +
                    asteroid.Radius)
                {
                    bullet.Destroy();
                    asteroid.Destroy();

                    score += asteroid.Size switch
                    {
                        AsteroidSize.Large => 20,
                        AsteroidSize.Medium => 50,
                        AsteroidSize.Small => 100,
                        _ => 0
                    };

                    asteroids.AddRange(
                        asteroid.Split(random));

                    break;
                }
            }
        }
    }

    // ============================================================
    // PLAYER COLLISIONS
    // ============================================================

    private void HandlePlayerCollisions()
    {
        if (!player.Alive)
            return;

        foreach (Asteroid asteroid in asteroids)
        {
            if (!asteroid.Active)
                continue;

            float distance =
                Vector2.Distance(
                    player.Position,
                    asteroid.Position);

            if (distance <
                player.Radius +
                asteroid.Radius * 0.8f)
            {
                asteroid.Destroy();

                lives--;

                if (lives <= 0)
                {
                    player.Kill();

                    gameOver = true;
                }
                else
                {
                    player.Kill();

                    respawnTimer = 1.5f;
                }

                break;
            }
        }
    }

    // ============================================================
    // DRAW
    // ============================================================

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        spriteBatch.Begin();

        // Asteroids
        foreach (Asteroid asteroid in asteroids)
        {
            asteroid.Draw(spriteBatch);
        }

        // Bullets
        foreach (Bullet bullet in bullets)
        {
            bullet.Draw(spriteBatch);
        }

        // Player
        player.Draw(spriteBatch);

        // HUD
        DrawHud();

        // Game over
        if (gameOver)
        {
            DrawGameOver();
        }

        spriteBatch.End();

        base.Draw(gameTime);
    }

    // ============================================================
    // HUD
    // ============================================================

    private void DrawHud()
    {
        // SCORE
        DrawText(
            "SCORE",
            new Vector2(20, 18),
            2f,
            Color.Cyan);

        DrawNumber(
            score,
            new Vector2(20, 42),
            4f,
            Color.White);

        // WAVE
        DrawText(
            "WAVE",
            new Vector2(
                ScreenWidth - 145,
                18),
            2f,
            Color.Cyan);

        DrawNumber(
            wave,
            new Vector2(
                ScreenWidth - 145,
                42),
            4f,
            Color.White);

        // LIVES
        DrawText(
            "LIVES",
            new Vector2(
                ScreenWidth / 2f - 35,
                18),
            2f,
            Color.Cyan);

        DrawLives();
    }

    private void DrawLives()
    {
        float startX =
            ScreenWidth / 2f - 35;

        float y = 54;

        for (int i = 0; i < lives; i++)
        {
            DrawMiniShip(
                new Vector2(
                    startX + i * 30,
                    y),
                Color.White);
        }
    }

    // ============================================================
    // CUSTOM BITMAP FONT
    //
    // No SpriteFont is used.
    //
    // Every letter is a 5 x 7 bitmap.
    //
    // Letters currently included:
    //
    // A C E G I L M O R S V W
    //
    // These cover:
    // SCORE
    // WAVE
    // LIVES
    // GAME OVER
    // ============================================================

    private void DrawText(
        string text,
        Vector2 position,
        float scale,
        Color color)
    {
        float x = position.X;

        foreach (char character in text)
        {
            // Space
            if (character == ' ')
            {
                x += 4f * scale;
                continue;
            }

            int[] glyph =
                GetGlyph(character);

            if (glyph != null)
            {
                DrawGlyph(
                    glyph,
                    new Vector2(
                        x,
                        position.Y),
                    scale,
                    color);
            }

            x += 6f * scale;
        }
    }

    private void DrawGlyph(
        int[] glyph,
        Vector2 position,
        float scale,
        Color color)
    {
        for (int row = 0; row < 7; row++)
        {
            int bits = glyph[row];

            for (int column = 0; column < 5; column++)
            {
                int mask =
                    1 << (4 - column);

                if ((bits & mask) != 0)
                {
                    spriteBatch.Draw(
                        pixel,
                        new Rectangle(
                            (int)(
                                position.X +
                                column * scale),

                            (int)(
                                position.Y +
                                row * scale),

                            Math.Max(
                                1,
                                (int)scale),

                            Math.Max(
                                1,
                                (int)scale)),
                        color);
                }
            }
        }
    }

    // ============================================================
    // GLYPH TABLE
    // ============================================================

    private int[] GetGlyph(char character)
    {
        return character switch
        {
            // ----------------------------------------------------
            // A
            // ----------------------------------------------------
            'A' => new[]
            {
                0b01110,
                0b10001,
                0b10001,
                0b11111,
                0b10001,
                0b10001,
                0b10001
            },

            // ----------------------------------------------------
            // C
            //
            // FIXED: This was missing before.
            // ----------------------------------------------------
            'C' => new[]
            {
                0b01110,
                0b10001,
                0b10000,
                0b10000,
                0b10000,
                0b10001,
                0b01110
            },

            // ----------------------------------------------------
            // E
            // ----------------------------------------------------
            'E' => new[]
            {
                0b11111,
                0b10000,
                0b10000,
                0b11110,
                0b10000,
                0b10000,
                0b11111
            },

            // ----------------------------------------------------
            // G
            // ----------------------------------------------------
            'G' => new[]
            {
                0b01110,
                0b10001,
                0b10000,
                0b10111,
                0b10001,
                0b10001,
                0b01110
            },

            // ----------------------------------------------------
            // I
            // ----------------------------------------------------
            'I' => new[]
            {
                0b11111,
                0b00100,
                0b00100,
                0b00100,
                0b00100,
                0b00100,
                0b11111
            },

            // ----------------------------------------------------
            // L
            // ----------------------------------------------------
            'L' => new[]
            {
                0b10000,
                0b10000,
                0b10000,
                0b10000,
                0b10000,
                0b10000,
                0b11111
            },

            // ----------------------------------------------------
            // M
            // ----------------------------------------------------
            'M' => new[]
            {
                0b10001,
                0b11011,
                0b10101,
                0b10101,
                0b10001,
                0b10001,
                0b10001
            },

            // ----------------------------------------------------
            // O
            // ----------------------------------------------------
            'O' => new[]
            {
                0b01110,
                0b10001,
                0b10001,
                0b10001,
                0b10001,
                0b10001,
                0b01110
            },

            // ----------------------------------------------------
            // R
            // ----------------------------------------------------
            'R' => new[]
            {
                0b11110,
                0b10001,
                0b10001,
                0b11110,
                0b10100,
                0b10010,
                0b10001
            },

            // ----------------------------------------------------
            // S
            // ----------------------------------------------------
            'S' => new[]
            {
                0b01111,
                0b10000,
                0b10000,
                0b01110,
                0b00001,
                0b00001,
                0b11110
            },

            // ----------------------------------------------------
            // V
            // ----------------------------------------------------
            'V' => new[]
            {
                0b10001,
                0b10001,
                0b10001,
                0b10001,
                0b10001,
                0b01010,
                0b00100
            },

            // ----------------------------------------------------
            // W
            // ----------------------------------------------------
            'W' => new[]
            {
                0b10001,
                0b10001,
                0b10001,
                0b10101,
                0b10101,
                0b11011,
                0b10001
            },

            // Unknown character
            _ => null
        };
    }

    // ============================================================
    // NUMBER DISPLAY
    // ============================================================

    private void DrawNumber(
        int number,
        Vector2 position,
        float scale,
        Color color)
    {
        string text =
            Math.Max(0, number).ToString();

        float x = position.X;

        foreach (char character in text)
        {
            DrawDigit(
                character - '0',
                new Vector2(
                    x,
                    position.Y),
                scale,
                color);

            x += 6f * scale;
        }
    }

    private void DrawDigit(
        int digit,
        Vector2 position,
        float scale,
        Color color)
    {
        // Seven segment display:
        //
        //       AAA
        //      F   B
        //       GGG
        //      E   C
        //       DDD

        bool[][] segments =
        {
            // 0
            new[]
            {
                true, true, true,
                true, true, true, false
            },

            // 1
            new[]
            {
                false, true, true,
                false, false, false, false
            },

            // 2
            new[]
            {
                true, true, false,
                true, true, false, true
            },

            // 3
            new[]
            {
                true, true, true,
                true, false, false, true
            },

            // 4
            new[]
            {
                false, true, true,
                false, false, true, true
            },

            // 5
            new[]
            {
                true, false, true,
                true, false, true, true
            },

            // 6
            new[]
            {
                true, false, true,
                true, true, true, true
            },

            // 7
            new[]
            {
                true, true, true,
                false, false, false, false
            },

            // 8
            new[]
            {
                true, true, true,
                true, true, true, true
            },

            // 9
            new[]
            {
                true, true, true,
                true, false, true, true
            }
        };

        if (digit < 0 || digit > 9)
            return;

        bool[] active =
            segments[digit];

        float width = 4f * scale;
        float height = scale;

        // A
        if (active[0])
        {
            DrawRect(
                position +
                new Vector2(
                    scale,
                    0),
                new Vector2(
                    width,
                    height),
                color);
        }

        // B
        if (active[1])
        {
            DrawRect(
                position +
                new Vector2(
                    4f * scale,
                    scale),
                new Vector2(
                    scale,
                    3f * scale),
                color);
        }

        // C
        if (active[2])
        {
            DrawRect(
                position +
                new Vector2(
                    4f * scale,
                    4f * scale),
                new Vector2(
                    scale,
                    3f * scale),
                color);
        }

        // D
        if (active[3])
        {
            DrawRect(
                position +
                new Vector2(
                    scale,
                    7f * scale),
                new Vector2(
                    width,
                    height),
                color);
        }

        // E
        if (active[4])
        {
            DrawRect(
                position +
                new Vector2(
                    0,
                    4f * scale),
                new Vector2(
                    scale,
                    3f * scale),
                color);
        }

        // F
        if (active[5])
        {
            DrawRect(
                position +
                new Vector2(
                    0,
                    scale),
                new Vector2(
                    scale,
                    3f * scale),
                color);
        }

        // G
        if (active[6])
        {
            DrawRect(
                position +
                new Vector2(
                    scale,
                    3.5f * scale),
                new Vector2(
                    width - scale,
                    scale),
                color);
        }
    }

    private void DrawRect(
        Vector2 position,
        Vector2 size,
        Color color)
    {
        spriteBatch.Draw(
            pixel,
            new Rectangle(
                (int)position.X,
                (int)position.Y,
                Math.Max(1, (int)size.X),
                Math.Max(1, (int)size.Y)),
            color);
    }

    // ============================================================
    // GAME OVER
    // ============================================================

    private void DrawGameOver()
    {
        string title = "GAME OVER";

        float scale = 6f;

        float characterWidth =
            6f * scale;

        float spaceWidth =
            4f * scale;

        float totalWidth = 0f;

        foreach (char character in title)
        {
            if (character == ' ')
                totalWidth += spaceWidth;
            else
                totalWidth += characterWidth;
        }

        float x =
            (ScreenWidth - totalWidth) / 2f;

        float y =
            ScreenHeight / 2f - 55f;

        foreach (char character in title)
        {
            if (character == ' ')
            {
                x += spaceWidth;
                continue;
            }

            int[] glyph =
                GetGlyph(character);

            if (glyph != null)
            {
                DrawGlyph(
                    glyph,
                    new Vector2(
                        x,
                        y),
                    scale,
                    Color.Red);
            }

            x += characterWidth;
        }

        DrawRestartPrompt(
            new Vector2(
                ScreenWidth / 2f,
                ScreenHeight / 2f + 35f));
    }

    private void DrawRestartPrompt(
        Vector2 center)
    {
        float pulse =
            0.5f +
            0.5f *
            MathF.Sin(
                (float)
                    DateTime.Now.TimeOfDay.TotalSeconds *
                5f);

        Color color =
            Color.Lerp(
                Color.DarkGray,
                Color.White,
                pulse);

        float width = 180f;
        float height = 40f;

        Rectangle rectangle =
            new Rectangle(
                (int)(center.X - width / 2f),
                (int)center.Y,
                (int)width,
                (int)height);

        DrawOutline(
            rectangle,
            color,
            2);

        // Restart arrow
        Vector2 left =
            new Vector2(
                center.X - 55,
                center.Y + 20);

        Vector2 right =
            new Vector2(
                center.X + 55,
                center.Y + 20);

        DrawLine(
            left,
            right,
            color,
            3);

        DrawLine(
            right,
            right + new Vector2(-14, -9),
            color,
            3);

        DrawLine(
            right,
            right + new Vector2(-14, 9),
            color,
            3);

        // Small key symbol
        DrawLine(
            new Vector2(
                center.X - 65,
                center.Y + 8),
            new Vector2(
                center.X - 65,
                center.Y + 32),
            color,
            2);
    }

    // ============================================================
    // MINI SHIP
    // ============================================================

    private void DrawMiniShip(
        Vector2 position,
        Color color)
    {
        Vector2 nose =
            position +
            new Vector2(0, -10);

        Vector2 left =
            position +
            new Vector2(-7, 7);

        Vector2 right =
            position +
            new Vector2(7, 7);

        DrawLine(
            nose,
            left,
            color,
            2);

        DrawLine(
            left,
            right,
            color,
            2);

        DrawLine(
            right,
            nose,
            color,
            2);
    }

    // ============================================================
    // DRAWING HELPERS
    // ============================================================

    private void DrawLine(
        Vector2 start,
        Vector2 end,
        Color color,
        float thickness)
    {
        Vector2 difference =
            end - start;

        float length =
            difference.Length();

        if (length <= 0)
            return;

        float angle =
            MathF.Atan2(
                difference.Y,
                difference.X);

        spriteBatch.Draw(
            pixel,
            start,
            null,
            color,
            angle,
            Vector2.Zero,
            new Vector2(
                length,
                thickness),
            SpriteEffects.None,
            0f);
    }

    private void DrawOutline(
        Rectangle rectangle,
        Color color,
        int thickness)
    {
        spriteBatch.Draw(
            pixel,
            new Rectangle(
                rectangle.X,
                rectangle.Y,
                rectangle.Width,
                thickness),
            color);

        spriteBatch.Draw(
            pixel,
            new Rectangle(
                rectangle.X,
                rectangle.Bottom - thickness,
                rectangle.Width,
                thickness),
            color);

        spriteBatch.Draw(
            pixel,
            new Rectangle(
                rectangle.X,
                rectangle.Y,
                thickness,
                rectangle.Height),
            color);

        spriteBatch.Draw(
            pixel,
            new Rectangle(
                rectangle.Right - thickness,
                rectangle.Y,
                thickness,
                rectangle.Height),
            color);
    }

    private bool Pressed(
        KeyboardState keyboard,
        Keys key)
    {
        return keyboard.IsKeyDown(key) &&
               previousKeyboard.IsKeyUp(key);
    }

    // ============================================================
    // PLAYER
    // ============================================================

    private class Player
    {
        public Vector2 Position;
        public Vector2 Velocity;

        public float Rotation;

        public bool Alive { get; private set; }

        public float Radius => 14f;

        private readonly Texture2D pixel;

        private const float Acceleration = 260f;
        private const float MaxSpeed = 380f;
        private const float RotationSpeed = 3.8f;
        private const float Friction = 0.995f;

        private float shootTimer;

        public Player(
            Texture2D pixel,
            Vector2 startPosition)
        {
            this.pixel = pixel;

            Position = startPosition;

            Rotation =
                -MathF.PI / 2f;

            Alive = true;
        }

        public void Reset(Vector2 position)
        {
            Position = position;

            Velocity = Vector2.Zero;

            Rotation =
                -MathF.PI / 2f;

            Alive = true;

            shootTimer = 0f;
        }

        public void Kill()
        {
            Alive = false;
        }

        public void Update(
            GameTime gameTime,
            int width,
            int height)
        {
            if (!Alive)
                return;

            float dt =
                (float)
                    gameTime.ElapsedGameTime.TotalSeconds;

            KeyboardState keyboard =
                Keyboard.GetState();

            // Rotate left
            if (keyboard.IsKeyDown(Keys.Left) ||
                keyboard.IsKeyDown(Keys.A))
            {
                Rotation -=
                    RotationSpeed * dt;
            }

            // Rotate right
            if (keyboard.IsKeyDown(Keys.Right) ||
                keyboard.IsKeyDown(Keys.D))
            {
                Rotation +=
                    RotationSpeed * dt;
            }

            // Thrust
            if (keyboard.IsKeyDown(Keys.Up) ||
                keyboard.IsKeyDown(Keys.W))
            {
                Velocity +=
                    Forward() *
                    Acceleration *
                    dt;

                if (Velocity.Length() > MaxSpeed)
                {
                    Velocity =
                        Vector2.Normalize(Velocity) *
                        MaxSpeed;
                }
            }

            // Friction
            Velocity *=
                MathF.Pow(
                    Friction,
                    dt * 60f);

            Position +=
                Velocity * dt;

            Wrap(
                ref Position,
                width,
                height);

            if (shootTimer > 0)
            {
                shootTimer -= dt;
            }
        }

        public Bullet TryShoot()
        {
            if (!Alive ||
                shootTimer > 0)
            {
                return null;
            }

            KeyboardState keyboard =
                Keyboard.GetState();

            if (!keyboard.IsKeyDown(Keys.Space))
            {
                return null;
            }

            shootTimer = 0.18f;

            Vector2 direction =
                Forward();

            return new Bullet(
                pixel,
                Position +
                direction * 20f,
                direction * 600f);
        }

        public Vector2 Forward()
        {
            return new Vector2(
                MathF.Cos(Rotation),
                MathF.Sin(Rotation));
        }

        public void Draw(
            SpriteBatch spriteBatch)
        {
            if (!Alive)
                return;

            Vector2 forward =
                Forward();

            Vector2 right =
                new Vector2(
                    -forward.Y,
                    forward.X);

            Vector2 nose =
                Position +
                forward * 19f;

            Vector2 leftWing =
                Position -
                forward * 12f +
                right * 12f;

            Vector2 rightWing =
                Position -
                forward * 12f -
                right * 12f;

            DrawLine(
                spriteBatch,
                nose,
                leftWing,
                Color.White,
                2f);

            DrawLine(
                spriteBatch,
                leftWing,
                rightWing,
                Color.White,
                2f);

            DrawLine(
                spriteBatch,
                rightWing,
                nose,
                Color.White,
                2f);

            KeyboardState keyboard =
                Keyboard.GetState();

            // Engine flame
            if (keyboard.IsKeyDown(Keys.Up) ||
                keyboard.IsKeyDown(Keys.W))
            {
                Vector2 flame =
                    Position -
                    forward * 19f;

                DrawLine(
                    spriteBatch,
                    leftWing,
                    flame,
                    Color.Orange,
                    2f);

                DrawLine(
                    spriteBatch,
                    flame,
                    rightWing,
                    Color.Yellow,
                    2f);
            }
        }

        private void DrawLine(
            SpriteBatch spriteBatch,
            Vector2 start,
            Vector2 end,
            Color color,
            float thickness)
        {
            Vector2 difference =
                end - start;

            float length =
                difference.Length();

            if (length <= 0)
                return;

            float angle =
                MathF.Atan2(
                    difference.Y,
                    difference.X);

            spriteBatch.Draw(
                pixel,
                start,
                null,
                color,
                angle,
                Vector2.Zero,
                new Vector2(
                    length,
                    thickness),
                SpriteEffects.None,
                0f);
        }

        private static void Wrap(
            ref Vector2 position,
            int width,
            int height)
        {
            if (position.X < 0)
                position.X += width;

            if (position.X >= width)
                position.X -= width;

            if (position.Y < 0)
                position.Y += height;

            if (position.Y >= height)
                position.Y -= height;
        }
    }

    // ============================================================
    // BULLET
    // ============================================================

    private class Bullet
    {
        public Vector2 Position;
        public Vector2 Velocity;

        public bool Active { get; private set; } = true;

        public float Radius => 3f;

        private readonly Texture2D pixel;

        private float lifetime = 1.2f;

        public Bullet(
            Texture2D pixel,
            Vector2 position,
            Vector2 velocity)
        {
            this.pixel = pixel;

            Position = position;
            Velocity = velocity;
        }

        public void Update(
            GameTime gameTime,
            int width,
            int height)
        {
            if (!Active)
                return;

            float dt =
                (float)
                    gameTime.ElapsedGameTime.TotalSeconds;

            Position +=
                Velocity * dt;

            lifetime -= dt;

            if (lifetime <= 0)
            {
                Active = false;
                return;
            }

            if (Position.X < 0 ||
                Position.X >= width ||
                Position.Y < 0 ||
                Position.Y >= height)
            {
                Active = false;
            }
        }

        public void Destroy()
        {
            Active = false;
        }

        public void Draw(
            SpriteBatch spriteBatch)
        {
            if (!Active)
                return;

            spriteBatch.Draw(
                pixel,
                new Rectangle(
                    (int)Position.X - 2,
                    (int)Position.Y - 2,
                    4,
                    4),
                Color.White);
        }
    }

    // ============================================================
    // ASTEROID
    // ============================================================

    private enum AsteroidSize
    {
        Large,
        Medium,
        Small
    }

    private class Asteroid
    {
        public Vector2 Position;
        public Vector2 Velocity;

        public AsteroidSize Size { get; }

        public bool Active { get; private set; } = true;

        public float Radius
        {
            get
            {
                return Size switch
                {
                    AsteroidSize.Large => 42f,
                    AsteroidSize.Medium => 25f,
                    _ => 14f
                };
            }
        }

        private readonly Texture2D pixel;

        private readonly Vector2[] vertices;

        private readonly float rotationSpeed;

        private float rotation;

        public Asteroid(
            Texture2D pixel,
            Vector2 position,
            Vector2 velocity,
            AsteroidSize size,
            Random random)
        {
            this.pixel = pixel;

            Position = position;
            Velocity = velocity;
            Size = size;

            rotation = 0f;

            rotationSpeed =
                ((float)random.NextDouble() - 0.5f) *
                2.5f;

            int vertexCount = 9;

            vertices =
                new Vector2[vertexCount];

            for (int i = 0;
                 i < vertexCount;
                 i++)
            {
                float angle =
                    MathHelper.TwoPi *
                    i /
                    vertexCount;

                float variation =
                    0.75f +
                    (float)random.NextDouble() *
                    0.35f;

                vertices[i] =
                    new Vector2(
                        MathF.Cos(angle),
                        MathF.Sin(angle)) *
                    Radius *
                    variation;
            }
        }

        public void Update(
            GameTime gameTime,
            int width,
            int height)
        {
            if (!Active)
                return;

            float dt =
                (float)
                    gameTime.ElapsedGameTime.TotalSeconds;

            Position +=
                Velocity * dt;

            rotation +=
                rotationSpeed * dt;

            Wrap(
                ref Position,
                width,
                height);
        }

        public void Destroy()
        {
            Active = false;
        }

        public List<Asteroid> Split(
            Random random)
        {
            var result =
                new List<Asteroid>();

            if (Size == AsteroidSize.Small)
            {
                return result;
            }

            AsteroidSize newSize =
                Size == AsteroidSize.Large
                    ? AsteroidSize.Medium
                    : AsteroidSize.Small;

            float speed =
                newSize == AsteroidSize.Medium
                    ? 130f
                    : 190f;

            float angle =
                MathF.Atan2(
                    Velocity.Y,
                    Velocity.X);

            float offset = 0.7f;

            Vector2 velocity1 =
                new Vector2(
                    MathF.Cos(angle - offset),
                    MathF.Sin(angle - offset)) *
                speed;

            Vector2 velocity2 =
                new Vector2(
                    MathF.Cos(angle + offset),
                    MathF.Sin(angle + offset)) *
                speed;

            result.Add(
                new Asteroid(
                    pixel,
                    Position,
                    velocity1,
                    newSize,
                    random));

            result.Add(
                new Asteroid(
                    pixel,
                    Position,
                    velocity2,
                    newSize,
                    random));

            return result;
        }

        public void Draw(
            SpriteBatch spriteBatch)
        {
            if (!Active)
                return;

            for (int i = 0;
                 i < vertices.Length;
                 i++)
            {
                Vector2 a =
                    Transform(vertices[i]);

                Vector2 b =
                    Transform(
                        vertices[
                            (i + 1) %
                            vertices.Length]);

                DrawLine(
                    spriteBatch,
                    a,
                    b,
                    Color.LightGray,
                    2f);
            }
        }

        private Vector2 Transform(
            Vector2 point)
        {
            float cos =
                MathF.Cos(rotation);

            float sin =
                MathF.Sin(rotation);

            return Position +
                   new Vector2(
                       point.X * cos -
                       point.Y * sin,

                       point.X * sin +
                       point.Y * cos);
        }

        private void DrawLine(
            SpriteBatch spriteBatch,
            Vector2 start,
            Vector2 end,
            Color color,
            float thickness)
        {
            Vector2 difference =
                end - start;

            float length =
                difference.Length();

            if (length <= 0)
                return;

            float angle =
                MathF.Atan2(
                    difference.Y,
                    difference.X);

            spriteBatch.Draw(
                pixel,
                start,
                null,
                color,
                angle,
                Vector2.Zero,
                new Vector2(
                    length,
                    thickness),
                SpriteEffects.None,
                0f);
        }

        private static void Wrap(
            ref Vector2 position,
            int width,
            int height)
        {
            if (position.X < 0)
                position.X += width;

            if (position.X >= width)
                position.X -= width;

            if (position.Y < 0)
                position.Y += height;

            if (position.Y >= height)
                position.Y -= height;
        }
    }
}
