using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace Pong
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        // Generated at runtime.
        // No image assets are required.
        private Texture2D pixel;

        // ---------------------------------------------------------
        // GAME SETTINGS
        // ---------------------------------------------------------

        private const int ScreenWidth = 960;
        private const int ScreenHeight = 540;

        private const int PaddleWidth = 12;
        private const int PaddleHeight = 90;
        private const int BallSize = 12;

        private const float PaddleSpeed = 500f;
        private const float BallSpeed = 400f;

        // Computer paddle settings.
        private const float ComputerSpeed = 350f;

        // ---------------------------------------------------------
        // GAME OBJECTS
        // ---------------------------------------------------------

        private Rectangle leftPaddle;
        private Rectangle rightPaddle;
        private Rectangle ball;

        private Vector2 ballVelocity;

        // ---------------------------------------------------------
        // SCORE
        // ---------------------------------------------------------

        private int leftScore;
        private int rightScore;

        private Random random = new Random();

        // ---------------------------------------------------------
        // 5x7 BITMAP FONT
        //
        // Each digit is seven rows high and five pixels wide.
        // 1 = pixel on
        // 0 = pixel off
        // ---------------------------------------------------------

        private static readonly string[][] Digits =
        {
            // 0
            new[]
            {
                "11111",
                "10001",
                "10001",
                "10001",
                "10001",
                "10001",
                "11111"
            },

            // 1
            new[]
            {
                "00100",
                "01100",
                "00100",
                "00100",
                "00100",
                "00100",
                "01110"
            },

            // 2
            new[]
            {
                "11111",
                "00001",
                "00001",
                "11111",
                "10000",
                "10000",
                "11111"
            },

            // 3
            new[]
            {
                "11111",
                "00001",
                "00001",
                "11111",
                "00001",
                "00001",
                "11111"
            },

            // 4
            new[]
            {
                "10001",
                "10001",
                "10001",
                "11111",
                "00001",
                "00001",
                "00001"
            },

            // 5
            new[]
            {
                "11111",
                "10000",
                "10000",
                "11111",
                "00001",
                "00001",
                "11111"
            },

            // 6
            new[]
            {
                "11111",
                "10000",
                "10000",
                "11111",
                "10001",
                "10001",
                "11111"
            },

            // 7
            new[]
            {
                "11111",
                "00001",
                "00001",
                "00010",
                "00100",
                "00100",
                "00100"
            },

            // 8
            new[]
            {
                "11111",
                "10001",
                "10001",
                "11111",
                "10001",
                "10001",
                "11111"
            },

            // 9
            new[]
            {
                "11111",
                "10001",
                "10001",
                "11111",
                "00001",
                "00001",
                "11111"
            }
        };

        // ---------------------------------------------------------
        // CONSTRUCTOR
        // ---------------------------------------------------------

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);

            Content.RootDirectory = "Content";

            IsMouseVisible = false;

            graphics.PreferredBackBufferWidth = ScreenWidth;
            graphics.PreferredBackBufferHeight = ScreenHeight;
        }

        // ---------------------------------------------------------
        // INITIALIZE
        // ---------------------------------------------------------

        protected override void Initialize()
        {
            leftPaddle = new Rectangle(
                40,
                ScreenHeight / 2 - PaddleHeight / 2,
                PaddleWidth,
                PaddleHeight
            );

            rightPaddle = new Rectangle(
                ScreenWidth - 40 - PaddleWidth,
                ScreenHeight / 2 - PaddleHeight / 2,
                PaddleWidth,
                PaddleHeight
            );

            ball = new Rectangle(
                ScreenWidth / 2 - BallSize / 2,
                ScreenHeight / 2 - BallSize / 2,
                BallSize,
                BallSize
            );

            ResetBall();

            base.Initialize();
        }

        // ---------------------------------------------------------
        // LOAD CONTENT
        // ---------------------------------------------------------

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // Create a 1x1 white texture in memory.
            //
            // All game graphics are rectangles made from this
            // texture, so no PNGs or other assets are required.
            pixel = new Texture2D(
                GraphicsDevice,
                1,
                1
            );

            pixel.SetData(new[] { Color.White });
        }

        // ---------------------------------------------------------
        // UPDATE
        // ---------------------------------------------------------

        protected override void Update(GameTime gameTime)
        {
            KeyboardState keyboard = Keyboard.GetState();

            // Escape quits the game.
            if (keyboard.IsKeyDown(Keys.Escape))
                Exit();

            float deltaTime =
                (float)gameTime.ElapsedGameTime.TotalSeconds;

            UpdatePlayerPaddle(keyboard, deltaTime);

            UpdateComputerPaddle(deltaTime);

            UpdateBall(deltaTime);

            base.Update(gameTime);
        }

        // ---------------------------------------------------------
        // PLAYER PADDLE
        //
        // LEFT PADDLE
        //
        // Up    = move up
        // Down  = move down
        // ---------------------------------------------------------

        private void UpdatePlayerPaddle(
            KeyboardState keyboard,
            float deltaTime)
        {
            if (keyboard.IsKeyDown(Keys.Up))
            {
                leftPaddle.Y -=
                    (int)(PaddleSpeed * deltaTime);
            }

            if (keyboard.IsKeyDown(Keys.Down))
            {
                leftPaddle.Y +=
                    (int)(PaddleSpeed * deltaTime);
            }

            // Keep paddle on screen.
            leftPaddle.Y = Math.Clamp(
                leftPaddle.Y,
                0,
                ScreenHeight - PaddleHeight
            );
        }

        // ---------------------------------------------------------
        // COMPUTER PADDLE
        //
        // The computer follows the vertical position of the ball.
        // It does not instantly teleport to the ball; it moves at
        // a limited speed so it can actually be beaten.
        // ---------------------------------------------------------

        private void UpdateComputerPaddle(float deltaTime)
        {
            float paddleCenter =
                rightPaddle.Y + rightPaddle.Height / 2f;

            float ballCenter =
                ball.Y + ball.Height / 2f;

            // Move toward the ball.
            if (paddleCenter < ballCenter - 5)
            {
                rightPaddle.Y +=
                    (int)(ComputerSpeed * deltaTime);
            }
            else if (paddleCenter > ballCenter + 5)
            {
                rightPaddle.Y -=
                    (int)(ComputerSpeed * deltaTime);
            }

            // Keep computer paddle on screen.
            rightPaddle.Y = Math.Clamp(
                rightPaddle.Y,
                0,
                ScreenHeight - PaddleHeight
            );
        }

        // ---------------------------------------------------------
        // BALL
        // ---------------------------------------------------------

        private void UpdateBall(float deltaTime)
        {
            ball.X +=
                (int)(ballVelocity.X * deltaTime);

            ball.Y +=
                (int)(ballVelocity.Y * deltaTime);

            // -----------------------------------------------------
            // TOP WALL
            // -----------------------------------------------------

            if (ball.Top <= 0)
            {
                ball.Y = 0;

                ballVelocity.Y =
                    Math.Abs(ballVelocity.Y);
            }

            // -----------------------------------------------------
            // BOTTOM WALL
            // -----------------------------------------------------

            if (ball.Bottom >= ScreenHeight)
            {
                ball.Y =
                    ScreenHeight - BallSize;

                ballVelocity.Y =
                    -Math.Abs(ballVelocity.Y);
            }

            // -----------------------------------------------------
            // LEFT PADDLE
            // -----------------------------------------------------

            if (ballVelocity.X < 0 &&
                ball.Intersects(leftPaddle))
            {
                ball.X =
                    leftPaddle.Right;

                BounceFromPaddle(leftPaddle);
            }

            // -----------------------------------------------------
            // RIGHT COMPUTER PADDLE
            // -----------------------------------------------------

            if (ballVelocity.X > 0 &&
                ball.Intersects(rightPaddle))
            {
                ball.X =
                    rightPaddle.Left - BallSize;

                BounceFromPaddle(rightPaddle);
            }

            // -----------------------------------------------------
            // RIGHT SIDE
            //
            // Player scores.
            // -----------------------------------------------------

            if (ball.Left > ScreenWidth)
            {
                leftScore++;

                ResetBall();
            }

            // -----------------------------------------------------
            // LEFT SIDE
            //
            // Computer scores.
            // -----------------------------------------------------

            if (ball.Right < 0)
            {
                rightScore++;

                ResetBall();
            }
        }

        // ---------------------------------------------------------
        // BALL / PADDLE BOUNCE
        // ---------------------------------------------------------

        private void BounceFromPaddle(Rectangle paddle)
        {
            float paddleCenter =
                paddle.Y + paddle.Height / 2f;

            float ballCenter =
                ball.Y + ball.Height / 2f;

            // Calculate where the ball hit the paddle.
            //
            // -1 = very top
            //  0 = center
            // +1 = very bottom
            float hitPosition =
                (ballCenter - paddleCenter) /
                (paddle.Height / 2f);

            hitPosition =
                Math.Clamp(hitPosition, -1f, 1f);

            // Reverse horizontal direction.
            ballVelocity.X =
                -ballVelocity.X;

            // Change vertical direction depending on
            // where the ball hit the paddle.
            ballVelocity.Y =
                hitPosition * BallSpeed;

            // Prevent the ball from traveling almost perfectly
            // horizontally.
            if (Math.Abs(ballVelocity.Y) < 80f)
            {
                if (ballVelocity.Y < 0)
                    ballVelocity.Y = -80f;
                else
                    ballVelocity.Y = 80f;
            }

            // Normalize so the ball always travels at the
            // same overall speed.
            Vector2 direction =
                Vector2.Normalize(ballVelocity);

            ballVelocity =
                direction * BallSpeed;
        }

        // ---------------------------------------------------------
        // RESET BALL
        // ---------------------------------------------------------

        private void ResetBall()
        {
            ball.X =
                ScreenWidth / 2 - BallSize / 2;

            ball.Y =
                ScreenHeight / 2 - BallSize / 2;

            // Random horizontal direction.
            float directionX =
                random.Next(0, 2) == 0
                    ? -1f
                    : 1f;

            // Give the ball a random vertical angle.
            float directionY =
                (float)(
                    random.NextDouble() * 1.4 - 0.7
                );

            Vector2 direction =
                Vector2.Normalize(
                    new Vector2(
                        directionX,
                        directionY
                    )
                );

            ballVelocity =
                direction * BallSpeed;
        }

        // ---------------------------------------------------------
        // DRAW
        // ---------------------------------------------------------

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            spriteBatch.Begin();

            // Left player paddle.
            DrawRect(
                leftPaddle,
                Color.White
            );

            // Right computer paddle.
            DrawRect(
                rightPaddle,
                Color.White
            );

            // Ball.
            DrawRect(
                ball,
                Color.White
            );

            // Center line.
            DrawCenterLine();

            // -----------------------------------------------------
            // PLAYER SCORE
            // -----------------------------------------------------

            DrawScore(
                leftScore,
                ScreenWidth / 2 - 100,
                45,
                8
            );

            // -----------------------------------------------------
            // COMPUTER SCORE
            // -----------------------------------------------------

            DrawScore(
                rightScore,
                ScreenWidth / 2 + 40,
                45,
                8
            );

            spriteBatch.End();

            base.Draw(gameTime);
        }

        // ---------------------------------------------------------
        // DRAW RECTANGLE
        // ---------------------------------------------------------

        private void DrawRect(
            Rectangle rectangle,
            Color color)
        {
            spriteBatch.Draw(
                pixel,
                rectangle,
                color
            );
        }

        // ---------------------------------------------------------
        // CENTER LINE
        // ---------------------------------------------------------

        private void DrawCenterLine()
        {
            const int dashHeight = 20;
            const int gap = 15;
            const int lineWidth = 4;

            for (
                int y = 0;
                y < ScreenHeight;
                y += dashHeight + gap)
            {
                DrawRect(
                    new Rectangle(
                        ScreenWidth / 2 -
                            lineWidth / 2,

                        y,

                        lineWidth,
                        dashHeight
                    ),
                    Color.White
                );
            }
        }

        // ---------------------------------------------------------
        // DRAW SCORE
        //
        // Uses the custom 5x7 bitmap font instead of SpriteFont.
        // ---------------------------------------------------------

        private void DrawScore(
            int score,
            int x,
            int y,
            int scale)
        {
            string text =
                score.ToString();

            int cursorX = x;

            foreach (char character in text)
            {
                if (character >= '0' &&
                    character <= '9')
                {
                    int digit =
                        character - '0';

                    DrawBitmapDigit(
                        digit,
                        cursorX,
                        y,
                        scale
                    );

                    // Digit is 5 pixels wide.
                    // Add 2 pixels of spacing.
                    cursorX +=
                        7 * scale;
                }
            }
        }

        // ---------------------------------------------------------
        // DRAW ONE BITMAP DIGIT
        // ---------------------------------------------------------

        private void DrawBitmapDigit(
            int digit,
            int x,
            int y,
            int scale)
        {
            string[] bitmap =
                Digits[digit];

            for (
                int row = 0;
                row < bitmap.Length;
                row++)
            {
                for (
                    int column = 0;
                    column < bitmap[row].Length;
                    column++)
                {
                    // Only draw pixels marked with 1.
                    if (bitmap[row][column] == '1')
                    {
                        DrawRect(
                            new Rectangle(
                                x +
                                    column * scale,

                                y +
                                    row * scale,

                                scale,
                                scale
                            ),
                            Color.White
                        );
                    }
                }
            }
        }

        // ---------------------------------------------------------
        // CLEANUP
        // ---------------------------------------------------------

        protected override void UnloadContent()
        {
            if (pixel != null)
                pixel.Dispose();

            base.UnloadContent();
        }
    }
}