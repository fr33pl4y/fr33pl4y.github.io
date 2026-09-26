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

namespace DonkeyKong
{
	public static class Program
	{
		public static void Main()
		{
			using var game = new Game1();
			game.Run();
		}
	}

    public enum GameState { Title, Playing, LevelWon, GameOver }

    // A single straight girder. Rendered as a thick line from A to B.
    public struct Platform
    {
        public Vector2 A;
        public Vector2 B;

        public Platform(Vector2 a, Vector2 b) { A = a; B = b; }

        public float MinX => Math.Min(A.X, B.X);
        public float MaxX => Math.Max(A.X, B.X);

        // Height of the girder's surface at a given x (clamped to the segment).
        public float GetYAtX(float x)
        {
            float minX = MinX, maxX = MaxX;
            float cx = MathHelper.Clamp(x, minX, maxX);
            if (Math.Abs(B.X - A.X) < 0.0001f) return A.Y;
            float t = (cx - A.X) / (B.X - A.X);
            return A.Y + (B.Y - A.Y) * t;
        }

        public bool ContainsX(float x) => x >= MinX - 2 && x <= MaxX + 2;
    }

    public struct Ladder
    {
        public float X;
        public float Top;
        public float Bottom;
        public int LowerPlatformIndex;
        public int UpperPlatformIndex;
        public bool Broken; // doesn't span the full gap - player must jump the rest
    }

    public class Player
    {
        public Vector2 Position;    // feet position (bottom-center)
        public Vector2 Velocity;
        public bool OnGround;
        public bool Climbing;
        public int Facing = 1;
        public bool Jumping;
        public bool HasHammer;
        public float HammerTimer;

        public Rectangle Bounds =>
            new Rectangle((int)Position.X - 7, (int)Position.Y - 26, 14, 26);
    }

    public class Hammer
    {
        public Vector2 Position;    // ground position where the hammer sits
        public bool Collected;

        public Rectangle Bounds =>
            new Rectangle((int)Position.X - 8, (int)Position.Y - 16, 16, 16);
    }

    public class Barrel
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public int PlatformIndex;
        public bool Falling;
        public bool Scored;
        public bool Dead;

        public Rectangle Bounds =>
            new Rectangle((int)Position.X - 8, (int)Position.Y - 7, 16, 14);
    }

    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private Texture2D _pixel;
        private readonly Random _rng = new Random();

        private const int ScreenWidth = 448;
        private const int ScreenHeight = 512;

        private const float Gravity = 900f;
        private const float MoveSpeed = 110f;
        private const float ClimbSpeed = 85f;
        private const float JumpVelocity = -320f;
        private const float BarrelSpeed = 70f;
        private const float LadderGrabRange = 9f;
        private const float HammerDuration = 8f;
        private const int HammerScore = 150;

        private GameState _state = GameState.Title;

        private List<Platform> _platforms;
        private List<Ladder> _ladders;
        private List<Barrel> _barrels;
        private List<Hammer> _hammers;

        private Player _player;
        private Vector2 _dkPos;
        private Vector2 _paulinePos;
        private Vector2 _playerStart;

        private int _score;
        private int _lives;
        private int _level;
        private float _barrelSpawnTimer;
        private float _barrelSpawnInterval;
        private float _invulnTimer;
        private float _messageTimer;
        private float _blinkTimer;
        private float _gameTimeSeconds;

        private KeyboardState _prevKb;

        // ------------------------------------------------------------------
        //  5x7 BITMAP FONT
        //  Each glyph is 7 rows of 5 characters ('X' = pixel on).
        // ------------------------------------------------------------------
        private static readonly Dictionary<char, string[]> Font = new Dictionary<char, string[]>
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
            ['W'] = new[] { "X...X", "X...X", "X...X", "X.X.X", "X.X.X", "XX.XX", "X...X" },
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
            [':'] = new[] { ".....", "..X..", ".....", ".....", ".....", "..X..", "....." },
            ['!'] = new[] { "..X..", "..X..", "..X..", "..X..", "..X..", ".....", "..X.." },
            ['-'] = new[] { ".....", ".....", ".....", "XXXXX", ".....", ".....", "....." },
            ['\''] = new[] { "..X..", "..X..", ".....", ".....", ".....", ".....", "....." },
            ['.'] = new[] { ".....", ".....", ".....", ".....", ".....", ".....", "..X.." },
            ['?'] = new[] { ".XXX.", "X...X", "....X", "...X.", "..X..", ".....", "..X.." },
        };

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = false;
        }

        protected override void Initialize()
        {
            _graphics.PreferredBackBufferWidth = ScreenWidth;
            _graphics.PreferredBackBufferHeight = ScreenHeight;
            _graphics.ApplyChanges();

            BuildLevel();
            ResetGame();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
        }

        // ------------------------------------------------------------------
        //  LEVEL CONSTRUCTION
        //  Classic "25m" zig-zag: starting at the top girder, each girder
        //  slopes down in the opposite direction from the one above it -
        //  top girder slopes down left-to-right, the next slopes down
        //  right-to-left, and so on, alternating all the way to the
        //  bottom. Barrels roll down the slope of whichever girder
        //  they're on and drop straight down to the next girder when
        //  they run off the end.
        // ------------------------------------------------------------------
        private const int TierCount = 6;
        private const float TopGirderY = 95f;
        private const float BottomGirderY = 475f;
        private const float GirderTilt = 26f;
        private const float GirderLeftX = 20f;
        private const float GirderRightX = 428f;

        private void BuildLevel()
        {
            _platforms = new List<Platform>();

            float spacing = (BottomGirderY - TopGirderY) / (TierCount - 1);
            for (int i = 0; i < TierCount; i++)
            {
                float centerY = BottomGirderY - i * spacing;

                // Count tiers down from the top girder so the alternation
                // always starts with "top girder slopes left-to-right".
                int distanceFromTop = (TierCount - 1) - i;
                bool slopesLeftToRight = (distanceFromTop % 2 == 0);

                float leftY = slopesLeftToRight ? centerY - GirderTilt : centerY + GirderTilt;
                float rightY = slopesLeftToRight ? centerY + GirderTilt : centerY - GirderTilt;

                _platforms.Add(new Platform(
                    new Vector2(GirderLeftX, leftY),
                    new Vector2(GirderRightX, rightY)));
            }

            _ladders = new List<Ladder>();
            for (int i = 0; i < _platforms.Count - 1; i++)
            {
                float[] xs = (i % 2 == 0) ? new[] { 110f, 360f } : new[] { 160f, 310f };
                var lower = _platforms[i];
                var upper = _platforms[i + 1];

                for (int j = 0; j < xs.Length; j++)
                {
                    float x = xs[j];
                    float top = upper.GetYAtX(x);
                    float bottom = lower.GetYAtX(x);

                    // The first ladder in each gap is always complete, so there's
                    // always a guaranteed way up. The second is broken - it's
                    // missing a chunk off one end, alternating top/bottom per
                    // gap for variety, matching the classic game's broken
                    // ladders that force a jump across the remaining gap.
                    bool broken = j == 1;
                    if (broken)
                    {
                        float span = bottom - top;
                        float trim = span * 0.42f;
                        bool missingBottom = (i % 2 == 0);
                        if (missingBottom) bottom -= trim;
                        else top += trim;
                    }

                    _ladders.Add(new Ladder
                    {
                        X = x,
                        Top = top,
                        Bottom = bottom,
                        LowerPlatformIndex = i,
                        UpperPlatformIndex = i + 1,
                        Broken = broken
                    });
                }
            }

            int topIndex = _platforms.Count - 1;
            _dkPos = new Vector2(60, _platforms[topIndex].GetYAtX(60));
            _paulinePos = new Vector2(390, _platforms[topIndex].GetYAtX(390));
            _playerStart = new Vector2(40, _platforms[0].GetYAtX(40));

            SetupHammers();
        }

        private void SetupHammers()
        {
            // Fixed pickup spots on a couple of the mid platforms.
            _hammers = new List<Hammer>
            {
                new Hammer { Position = new Vector2(250, _platforms[1].GetYAtX(250)) },
                new Hammer { Position = new Vector2(190, _platforms[4].GetYAtX(190)) },
            };
        }

        private void ResetHammers()
        {
            if (_hammers == null) return;
            foreach (var h in _hammers) h.Collected = false;
        }

        private void ResetGame()
        {
            _score = 0;
            _lives = 3;
            _level = 1;
            _barrelSpawnInterval = 2.5f;
            _barrels = new List<Barrel>();
            ResetHammers();
            ResetPlayer();
            _state = GameState.Title;
        }

        private void ResetPlayer()
        {
            _player = new Player { Position = _playerStart, OnGround = true };
            _invulnTimer = 1.0f;
        }

        // ------------------------------------------------------------------
        //  UPDATE
        // ------------------------------------------------------------------
        protected override void Update(GameTime gameTime)
        {
            var kb = Keyboard.GetState();
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (kb.IsKeyDown(Keys.Escape)) Exit();
            if (Pressed(kb, Keys.F11)) ToggleFullScreen();

            _blinkTimer += dt;

            switch (_state)
            {
                case GameState.Title:
                    if (Pressed(kb, Keys.Enter))
                    {
                        ResetGame();
                        _state = GameState.Playing;
                    }
                    break;

                case GameState.Playing:
                    UpdatePlaying(dt, kb);
                    break;

                case GameState.LevelWon:
                    _messageTimer -= dt;
                    if (_messageTimer <= 0)
                    {
                        _level++;
                        _barrelSpawnInterval = Math.Max(0.9f, _barrelSpawnInterval - 0.25f);
                        _barrels.Clear();
                        ResetHammers();
                        ResetPlayer();
                        _state = GameState.Playing;
                    }
                    break;

                case GameState.GameOver:
                    if (Pressed(kb, Keys.Enter))
                    {
                        ResetGame();
                        _state = GameState.Playing;
                    }
                    break;
            }

            _prevKb = kb;
            base.Update(gameTime);
        }

        private bool Pressed(KeyboardState kb, Keys k) => kb.IsKeyDown(k) && !_prevKb.IsKeyDown(k);

        private void ToggleFullScreen()
        {
            if (!_graphics.IsFullScreen)
            {
                _graphics.PreferredBackBufferWidth = GraphicsDevice.DisplayMode.Width;
                _graphics.PreferredBackBufferHeight = GraphicsDevice.DisplayMode.Height;
            }
            else
            {
                _graphics.PreferredBackBufferWidth = ScreenWidth;
                _graphics.PreferredBackBufferHeight = ScreenHeight;
            }
            _graphics.IsFullScreen = !_graphics.IsFullScreen;
            _graphics.ApplyChanges();
        }

        private void UpdatePlaying(float dt, KeyboardState kb)
        {
            _gameTimeSeconds += dt;
            if (_invulnTimer > 0) _invulnTimer -= dt;

            bool left = kb.IsKeyDown(Keys.Left);
            bool right = kb.IsKeyDown(Keys.Right);
            bool up = kb.IsKeyDown(Keys.Up);
            bool down = kb.IsKeyDown(Keys.Down);
            bool jumpPressed = Pressed(kb, Keys.Space);

            // find a ladder the player currently overlaps
            Ladder? currentLadder = null;
            foreach (var l in _ladders)
            {
                if (Math.Abs(_player.Position.X - l.X) < LadderGrabRange &&
                    _player.Position.Y >= l.Top - 4 && _player.Position.Y <= l.Bottom + 4)
                {
                    currentLadder = l;
                    break;
                }
            }

            if ((up || down) && currentLadder.HasValue && !_player.Jumping && !_player.HasHammer)
            {
                var l = currentLadder.Value;
                _player.Climbing = true;
                _player.OnGround = false;
                _player.Velocity = Vector2.Zero;
                _player.Position.X = l.X;
                _player.Position.Y += (down ? 1 : -1) * ClimbSpeed * dt;
                _player.Position.Y = MathHelper.Clamp(_player.Position.Y, l.Top, l.Bottom);

                if (_player.Position.Y <= l.Top + 0.5f)
                {
                    _player.Climbing = false;
                    _player.OnGround = true;
                }
                else if (_player.Position.Y >= l.Bottom - 0.5f && down)
                {
                    _player.Climbing = false;
                    _player.OnGround = true;
                }
            }
            else
            {
                _player.Climbing = false;

                if (left) { _player.Position.X -= MoveSpeed * dt; _player.Facing = -1; }
                if (right) { _player.Position.X += MoveSpeed * dt; _player.Facing = 1; }
                _player.Position.X = MathHelper.Clamp(_player.Position.X, 18, 430);

                if (jumpPressed && _player.OnGround && !_player.HasHammer)
                {
                    _player.Velocity.Y = JumpVelocity;
                    _player.OnGround = false;
                    _player.Jumping = true;
                }

                _player.Velocity.Y += Gravity * dt;
                _player.Position.Y += _player.Velocity.Y * dt;

                bool landed = false;
                if (_player.Velocity.Y >= 0)
                {
                    foreach (var p in _platforms)
                    {
                        if (!p.ContainsX(_player.Position.X)) continue;
                        float platY = p.GetYAtX(_player.Position.X);
                        if (_player.Position.Y >= platY && _player.Position.Y <= platY + 14)
                        {
                            _player.Position.Y = platY;
                            _player.Velocity.Y = 0;
                            _player.OnGround = true;
                            _player.Jumping = false;
                            landed = true;
                            break;
                        }
                    }
                }
                if (!landed) _player.OnGround = false;

                if (_player.Position.Y > ScreenHeight + 40)
                {
                    LoseLife();
                }
            }

            UpdateHammer(dt);
            UpdateBarrels(dt);

            // win condition - reached Pauline on the top platform
            if (Vector2.Distance(_player.Position, _paulinePos) < 28)
            {
                _state = GameState.LevelWon;
                _messageTimer = 3f;
                _score += 5000;
            }
        }

        private void UpdateHammer(float dt)
        {
            if (!_player.HasHammer)
            {
                foreach (var h in _hammers)
                {
                    if (!h.Collected && h.Bounds.Intersects(_player.Bounds))
                    {
                        h.Collected = true;
                        _player.HasHammer = true;
                        _player.HammerTimer = HammerDuration;
                        break;
                    }
                }
            }
            else
            {
                _player.HammerTimer -= dt;
                if (_player.HammerTimer <= 0)
                {
                    _player.HasHammer = false;
                    _player.HammerTimer = 0;
                }
            }
        }

        private void UpdateBarrels(float dt)
        {
            _barrelSpawnTimer -= dt;
            if (_barrelSpawnTimer <= 0)
            {
                _barrelSpawnTimer = _barrelSpawnInterval + (float)_rng.NextDouble();
                _barrels.Add(new Barrel
                {
                    Position = _dkPos + new Vector2(20, -4),
                    Velocity = new Vector2(BarrelSpeed, 0),
                    PlatformIndex = _platforms.Count - 1,
                    Falling = false
                });
            }

            foreach (var b in _barrels)
            {
                if (b.Dead) continue;

                if (!b.Falling)
                {
                    var plat = _platforms[b.PlatformIndex];
                    b.Position.X += b.Velocity.X * dt;
                    b.Position.Y = plat.GetYAtX(b.Position.X);

                    // small chance to drop straight through a ladder when passing near one
                    bool dropThroughLadder = false;
                    foreach (var l in _ladders)
                    {
                        if (!l.Broken && l.UpperPlatformIndex == b.PlatformIndex &&
                            Math.Abs(b.Position.X - l.X) < 4 && _rng.NextDouble() < 0.01)
                        {
                            dropThroughLadder = true;
                            break;
                        }
                    }

                    bool offEdge = b.Position.X < plat.MinX || b.Position.X > plat.MaxX;

                    // NOTE: only ever drop ONE platform index per frame. Previously the
                    // ladder-drop check and the edge check could both fire in the same
                    // frame and decrement PlatformIndex twice, causing the barrel to skip
                    // the girder directly below it and fall much further than intended.
                    if (dropThroughLadder || offEdge)
                    {
                        b.Position.X = MathHelper.Clamp(b.Position.X, plat.MinX, plat.MaxX);
                        b.Falling = true;
                        b.PlatformIndex -= 1;
                        if (b.PlatformIndex < 0)
                        {
                            b.Dead = true;
                        }
                    }
                }
                else
                {
                    // While falling, only move vertically. Continuing to apply the
                    // stored horizontal velocity here would drift the barrel past
                    // the edge of the platform below (since it starts falling right
                    // at the edge of the platform above), causing it to miss the
                    // landing check entirely and fall straight through every girder
                    // beneath it. It keeps its horizontal velocity so it resumes
                    // rolling in the same direction once it lands.
                    b.Velocity.Y += Gravity * dt;
                    b.Position.Y += b.Velocity.Y * dt;

                    if (b.PlatformIndex >= 0)
                    {
                        var plat = _platforms[b.PlatformIndex];
                        if (plat.ContainsX(b.Position.X))
                        {
                            float platY = plat.GetYAtX(b.Position.X);
                            if (b.Position.Y >= platY && b.Velocity.Y >= 0)
                            {
                                b.Position.Y = platY;
                                b.Falling = false;
                                // Reverse direction so it rolls away from the edge it just
                                // fell off, instead of immediately running off the same
                                // side of the girder it just landed on.
                                b.Velocity = new Vector2(-b.Velocity.X, 0);
                            }
                        }
                    }

                    if (b.Position.Y > ScreenHeight + 50) b.Dead = true;
                }

                // scoring: jumped over a barrel
                if (!b.Scored && _player.Jumping && Math.Abs(b.Position.X - _player.Position.X) < 16 &&
                    Math.Abs(b.Position.Y - _player.Position.Y) < 20)
                {
                    b.Scored = true;
                    _score += 100;
                }

                // collision with player
                if (b.Bounds.Intersects(_player.Bounds))
                {
                    if (_player.HasHammer)
                    {
                        if (!b.Dead)
                        {
                            b.Dead = true;
                            _score += HammerScore;
                        }
                    }
                    else if (_invulnTimer <= 0)
                    {
                        LoseLife();
                    }
                }
            }

            _barrels.RemoveAll(b => b.Dead);
        }

        private void LoseLife()
        {
            if (_invulnTimer > 0) return;
            _lives--;
            if (_lives <= 0)
            {
                _state = GameState.GameOver;
            }
            else
            {
                ResetPlayer();
            }
        }

        // ------------------------------------------------------------------
        //  DRAW
        // ------------------------------------------------------------------
        protected override void Draw(GameTime gameTime)
        {
            int pw = GraphicsDevice.PresentationParameters.BackBufferWidth;
            int ph = GraphicsDevice.PresentationParameters.BackBufferHeight;
            float scale = Math.Min(pw / (float)ScreenWidth, ph / (float)ScreenHeight);
            float ox = (pw - ScreenWidth * scale) * 0.5f;
            float oy = (ph - ScreenHeight * scale) * 0.5f;
            Matrix m = Matrix.CreateScale(scale, scale, 1f) * Matrix.CreateTranslation(ox, oy, 0f);

            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: m);

            switch (_state)
            {
                case GameState.Title:
                    DrawTitle();
                    break;
                case GameState.Playing:
                    DrawWorld();
                    DrawHud();
                    break;
                case GameState.LevelWon:
                    DrawWorld();
                    DrawHud();
                    DrawCenteredText("PAULINE SAVED!", 240, 3, Color.Yellow);
                    break;
                case GameState.GameOver:
                    DrawWorld();
                    DrawHud();
                    DrawCenteredText("GAME OVER", 220, 4, Color.Red);
                    if (((int)(_blinkTimer * 2)) % 2 == 0)
                        DrawCenteredText("PRESS ENTER TO RESTART", 270, 2, Color.White);
                    break;
            }

            _spriteBatch.End();
            base.Draw(gameTime);
        }

        private void DrawWorld()
        {
            // platforms
            // Drawn using each platform's real A/B endpoints, so the
            // girders shown on screen always line up exactly with the
            // collision line barrels and the player actually walk on.
            foreach (var p in _platforms)
                DrawGirder(p.A, p.B);

            // ladders
            foreach (var l in _ladders)
            {
                Color rungColor = l.Broken ? new Color(230, 140, 40) : Color.Yellow;
                DrawRect(new Rectangle((int)l.X - 6, (int)l.Top, 2, (int)(l.Bottom - l.Top)), rungColor);
                DrawRect(new Rectangle((int)l.X + 4, (int)l.Top, 2, (int)(l.Bottom - l.Top)), rungColor);
                for (float y = l.Top; y < l.Bottom; y += 10)
                    DrawRect(new Rectangle((int)l.X - 6, (int)y, 12, 2), rungColor);
            }

            DrawDK();
            DrawPauline();

            foreach (var h in _hammers)
                if (!h.Collected) DrawHammerPickup(h);

            foreach (var b in _barrels)
                if (!b.Dead) DrawBarrel(b);

            bool flicker = _invulnTimer > 0 && ((int)(_invulnTimer * 12)) % 2 == 0;
            if (!flicker) DrawPlayer();
        }

        private void DrawDK()
        {
            Vector2 p = _dkPos;
            Color brown = new Color(101, 67, 33);
            Color dark = new Color(60, 38, 18);
            DrawRect(new Rectangle((int)p.X - 20, (int)p.Y - 40, 40, 40), brown);
            DrawRect(new Rectangle((int)p.X - 14, (int)p.Y - 58, 28, 20), dark);
            DrawRect(new Rectangle((int)p.X - 32, (int)p.Y - 32, 12, 30), brown);
            DrawRect(new Rectangle((int)p.X + 20, (int)p.Y - 32, 12, 30), brown);
            DrawText("DK", new Vector2(p.X - 9, p.Y - 24), 2, Color.White);
        }

        private void DrawPauline()
        {
            Vector2 p = _paulinePos;
            DrawRect(new Rectangle((int)p.X - 6, (int)p.Y - 24, 12, 24), Color.DeepPink);
            DrawRect(new Rectangle((int)p.X - 5, (int)p.Y - 33, 10, 9), new Color(255, 220, 180));
            DrawText("HELP!", new Vector2(p.X - 15, p.Y - 50), 1, Color.White);
        }

        private void DrawPlayer()
        {
            Vector2 p = _player.Position;
            Color skin = new Color(255, 200, 150);
            Color overalls = Color.Blue;
            Color cap = Color.Red;

            DrawRect(new Rectangle((int)p.X - 6, (int)p.Y - 16, 12, 10), overalls);
            DrawRect(new Rectangle((int)p.X - 6, (int)p.Y - 6, 5, 6), skin);
            DrawRect(new Rectangle((int)p.X + 1, (int)p.Y - 6, 5, 6), skin);
            DrawRect(new Rectangle((int)p.X - 5, (int)p.Y - 24, 10, 9), skin);
            DrawRect(new Rectangle((int)p.X - 6, (int)p.Y - 27, 12, 4), cap);
            // little peak showing facing direction
            int peakX = _player.Facing > 0 ? (int)p.X + 5 : (int)p.X - 8;
            DrawRect(new Rectangle(peakX, (int)p.Y - 25, 3, 2), cap);

            if (_player.HasHammer)
            {
                Color handle = new Color(120, 80, 40);
                Color head = new Color(90, 90, 90);
                bool swing = ((int)(_gameTimeSeconds * 10)) % 2 == 0;
                int hx = _player.Facing > 0 ? (int)p.X + 7 : (int)p.X - 17;
                int hy = swing ? (int)p.Y - 26 : (int)p.Y - 20;
                DrawRect(new Rectangle(hx, hy, 3, 12), handle);
                DrawRect(new Rectangle(hx - 4, hy - 2, 11, 5), head);
            }
        }

        private void DrawHammerPickup(Hammer h)
        {
            Color handle = new Color(120, 80, 40);
            Color head = new Color(90, 90, 90);
            DrawRect(new Rectangle((int)h.Position.X - 1, (int)h.Position.Y - 14, 3, 14), handle);
            DrawRect(new Rectangle((int)h.Position.X - 6, (int)h.Position.Y - 16, 12, 6), head);
        }

        private void DrawBarrel(Barrel b)
        {
            Rectangle body = new Rectangle((int)b.Position.X - 8, (int)b.Position.Y - 7, 16, 14);
            DrawRect(body, new Color(139, 90, 43));
            DrawRect(new Rectangle(body.X, body.Y + 2, 16, 2), new Color(90, 60, 30));
            DrawRect(new Rectangle(body.X, body.Y + 9, 16, 2), new Color(90, 60, 30));
        }

        private void DrawHud()
        {
            DrawText("SCORE", new Vector2(10, 8), 2, Color.White);
            DrawText(_score.ToString(), new Vector2(10, 24), 2, Color.Yellow);
            DrawText("LIVES " + _lives, new Vector2(280, 8), 2, Color.White);
            DrawText("LEVEL " + _level, new Vector2(280, 24), 2, Color.White);

            if (_player.HasHammer)
            {
                int secondsLeft = (int)Math.Ceiling(_player.HammerTimer);
                DrawText("HAMMER " + secondsLeft, new Vector2(150, 8), 2, Color.Orange);
            }
        }

        private void DrawTitle()
        {
            DrawCenteredText("DONKEY KONG", 130, 4, Color.Red);
            DrawCenteredText("CLONE", 190, 3, Color.OrangeRed);

            DrawDK();
            DrawPauline();

            if (((int)(_blinkTimer * 2)) % 2 == 0)
                DrawCenteredText("PRESS ENTER TO START", 400, 2, Color.White);

            DrawCenteredText("ARROWS MOVE / CLIMB  -  SPACE JUMP", 440, 1, Color.Gray);
            DrawCenteredText("F11 FULLSCREEN  -  ESC QUIT", 460, 1, Color.Gray);
        }

        private void DrawCenteredText(string text, float y, int pixelSize, Color color)
        {
            float width = MeasureText(text, pixelSize);
            DrawText(text, new Vector2((ScreenWidth - width) / 2f, y), pixelSize, color);
        }

        // ------------------------------------------------------------------
        //  DRAWING PRIMITIVES
        // ------------------------------------------------------------------
        private void DrawRect(Rectangle rect, Color color)
        {
            _spriteBatch.Draw(_pixel, rect, color);
        }

        private void DrawLine(Vector2 a, Vector2 b, int thickness, Color color)
        {
            Vector2 edge = b - a;
            float length = edge.Length();
            float angle = (float)Math.Atan2(edge.Y, edge.X);

            _spriteBatch.Draw(
                _pixel,
                new Rectangle((int)a.X, (int)a.Y, (int)length, thickness),
                null,
                color,
                angle,
                Vector2.Zero,
                SpriteEffects.None,
                0f);
        }

        // Renders a girder as a solid beam plus a diagonal cross-hatch
        // truss pattern along its length, matching the classic Donkey
        // Kong look. Always follows the exact A/B line passed in, so it
        // stays lined up with whatever slope the platform actually has.
        private void DrawGirder(Vector2 a, Vector2 b)
        {
            Color beamColor = new Color(216, 40, 32);
            Color hatchColor = new Color(255, 148, 40);

            // Solid beam.
            DrawLine(a, b, 6, beamColor);

            Vector2 edge = b - a;
            float length = edge.Length();
            if (length < 1f)
                return;

            Vector2 dir = edge / length;
            Vector2 normal = new Vector2(-dir.Y, dir.X);

            const float hatchSpacing = 16f;
            const float hatchHeight = 9f;

            bool up = true;
            for (float d = 0; d < length; d += hatchSpacing)
            {
                Vector2 baseA = a + dir * d - normal * (hatchHeight / 2f);
                Vector2 baseB = a + dir * Math.Min(d + hatchSpacing, length) + normal * (hatchHeight / 2f);

                if (!up)
                {
                    baseA = a + dir * d + normal * (hatchHeight / 2f);
                    baseB = a + dir * Math.Min(d + hatchSpacing, length) - normal * (hatchHeight / 2f);
                }

                DrawLine(baseA, baseB, 2, hatchColor);
                up = !up;
            }
        }

        private float MeasureText(string text, int pixelSize)
        {
            return text.Length * 6 * pixelSize;
        }

        private void DrawText(string text, Vector2 pos, int pixelSize, Color color)
        {
            float x = pos.X;
            foreach (char raw in text)
            {
                char c = char.ToUpperInvariant(raw);
                if (Font.TryGetValue(c, out string[] rows))
                {
                    for (int row = 0; row < rows.Length; row++)
                    {
                        string line = rows[row];
                        for (int col = 0; col < line.Length; col++)
                        {
                            if (line[col] == 'X')
                            {
                                DrawRect(new Rectangle(
                                    (int)(x + col * pixelSize),
                                    (int)(pos.Y + row * pixelSize),
                                    pixelSize, pixelSize), color);
                            }
                        }
                    }
                }
                x += 6 * pixelSize;
            }
        }
    }
}
