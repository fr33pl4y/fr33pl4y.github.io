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

namespace WizardOfWor
{
    // =================================================================================
    //  1. PROGRAM ENTRY POINT
    // =================================================================================
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            using (var game = new WizardOfWorGame())
            {
                game.Run();
            }
        }
    }

    // =================================================================================
    //  2. ENUMS, HELPERS AND CONSTANTS
    // =================================================================================
    public enum Direction { Up, Right, Down, Left, None }

    public enum GameState { Title, Playing, GameOver }

    /// <summary>The phases of one dungeon, in the order they happen.</summary>
    public enum Stage
    {
        Monsters,   // kill the Burwors / Garwors / Thorwors
        Worluk,     // the Worluk tries to escape (dungeon 2 and up)
        Wizard,     // the Wizard of Wor may show up
        Finished    // short pause, then the next dungeon starts
    }

    public enum MonsterKind { Burwor, Garwor, Thorwor, Worluk }

    public enum WorriorState
    {
        InBox,   // waiting in the little ready box (10 second countdown)
        Active,  // walking around in the dungeon
        Dying,   // just got hit, explosion is playing
        Out      // no lives left
    }

    public static class DirectionExtensions
    {
        public static readonly Direction[] AllDirections =
            { Direction.Up, Direction.Right, Direction.Down, Direction.Left };

        /// <summary>The (dx, dy) of one step in this direction.</summary>
        public static Point ToStep(this Direction d)
        {
            switch (d)
            {
                case Direction.Up: return new Point(0, -1);
                case Direction.Right: return new Point(1, 0);
                case Direction.Down: return new Point(0, 1);
                case Direction.Left: return new Point(-1, 0);
                default: return Point.Zero;
            }
        }

        public static Direction Opposite(this Direction d)
        {
            switch (d)
            {
                case Direction.Up: return Direction.Down;
                case Direction.Down: return Direction.Up;
                case Direction.Left: return Direction.Right;
                case Direction.Right: return Direction.Left;
                default: return Direction.None;
            }
        }

        public static bool IsHorizontal(this Direction d)
        {
            return d == Direction.Left || d == Direction.Right;
        }
    }

    /// <summary>All the tweakable numbers of the game in one place.</summary>
    public static class Config
    {
        // --- Virtual screen (everything is drawn at this tiny size, then scaled up) ---
        public const int ScreenWidth = 320;
        public const int ScreenHeight = 216;

        // --- Maze geometry: an 11 x 6 grid of square cells ---
        public const int Cols = 11;
        public const int Rows = 6;
        public const int CellSize = 22;
        public const int HalfCell = CellSize / 2;          // pixel offset of a cell's centre
        public const int MazeWidth = Cols * CellSize;
        public const int MazeHeight = Rows * CellSize;
        public const int MazeX = (ScreenWidth - MazeWidth) / 2;   // screen position of the maze
        public const int MazeY = 8;
        public const int DoorRow = 2;                      // row of the side "warp" doors
        public const int DoorStubLength = 14;              // little corridor outside each door

        // --- Speeds are in pixels per second ---
        public const float WorriorSpeed = 70f;
        public const float WorriorShotSpeed = 230f;
        public const float EnemyBoltSpeed = 130f;
        public const float MonsterTopSpeed = 95f;
        public const float MonsterSpeedUpInterval = 7f;    // seconds between speed-ups
        public const float MonsterSpeedUpAmount = 5f;
        public const float WorlukSpeed = 85f;

        // --- Misc ---
        public const float TapDelay = 0.07f;               // shorter than this = just turn, don't walk
        public const float BoxCountdown = 10f;             // seconds to leave the ready box
        public const float DoorClosedTime = 3.5f;          // a used door is shut for this long
        public const int HitDistance = 8;                  // pixel distance that counts as a hit
        public const int StartLives = 3;
        public const int MaxLives = 16;
    }

    // =================================================================================
    //  3. RULES  (straight from the game description)
    // =================================================================================
    public static class Rules
    {
        /// <summary>The Pit is dungeon 13 and every 6th dungeon after that (19, 25, ...).</summary>
        public static bool IsPit(int dungeon) { return dungeon >= 13 && (dungeon - 13) % 6 == 0; }

        /// <summary>The Arena comes with the first bonus player (we use dungeon 4).</summary>
        public static bool IsArena(int dungeon) { return dungeon == 4; }

        /// <summary>Dungeon 8 and up are the tougher "Worlord" dungeons.</summary>
        public static bool IsWorlord(int dungeon) { return dungeon >= 8; }

        /// <summary>How many Burwors turn into Garwors: 1 in dungeon 1, 2 in dungeon 2 ... max 6.</summary>
        public static int GarworsInDungeon(int dungeon) { return Math.Min(dungeon, 6); }

        /// <summary>Bonus worrior in the Arena and in the first Pit.</summary>
        public static bool AwardsBonusWorrior(int dungeon) { return dungeon == 4 || dungeon == 13; }

        public static int PointsFor(MonsterKind kind)
        {
            switch (kind)
            {
                case MonsterKind.Burwor: return 100;
                case MonsterKind.Garwor: return 200;
                case MonsterKind.Thorwor: return 500;
                default: return 1000; // Worluk
            }
        }

        public const int WorriorKillPoints = 1000;
        public const int WizardPoints = 2500;
    }

    // =================================================================================
    //  4. MAZE
    // =================================================================================
    /// <summary>
    /// The dungeon layout. Every cell has a wall (or opening) on each side.
    /// We store only the wall on the RIGHT and the wall BELOW each cell - the other
    /// two sides are the neighbours' walls.
    /// </summary>
    public class Maze
    {
        private readonly bool[,] wallRight = new bool[Config.Cols, Config.Rows];
        private readonly bool[,] wallBelow = new bool[Config.Cols, Config.Rows];

        private float doorClosedTimer;

        /// <summary>While the Worluk is out, the doors stay open all the time.</summary>
        public bool ForceDoorsOpen;

        public bool DoorsOpen { get { return ForceDoorsOpen || doorClosedTimer <= 0f; } }

        public void CloseDoorsForAWhile() { doorClosedTimer = Config.DoorClosedTime; }

        public void Update(float dt) { if (doorClosedTimer > 0f) doorClosedTimer -= dt; }

        // ---------------------------------------------------------------------------
        // Wall queries
        // ---------------------------------------------------------------------------

        /// <summary>Is there a wall on this side of the given cell?</summary>
        public bool HasWall(int col, int row, Direction side)
        {
            switch (side)
            {
                case Direction.Up:
                    return row == 0 || wallBelow[col, row - 1];
                case Direction.Down:
                    return row == Config.Rows - 1 || wallBelow[col, row];
                case Direction.Right:
                    if (col == Config.Cols - 1) return !(row == Config.DoorRow && DoorsOpen);
                    return wallRight[col, row];
                case Direction.Left:
                    if (col == 0) return !(row == Config.DoorRow && DoorsOpen);
                    return wallRight[col - 1, row];
            }
            return true;
        }

        /// <summary>
        /// True when two cells are in the same row or column with no wall between them.
        /// This is what "being in the same corridor" means (visibility, shooting).
        /// </summary>
        public bool LineOfSight(int col1, int row1, int col2, int row2)
        {
            if (row1 == row2)
            {
                int from = Math.Min(col1, col2), to = Math.Max(col1, col2);
                for (int c = from; c < to; c++)
                    if (wallRight[c, row1]) return false;
                return true;
            }
            if (col1 == col2)
            {
                int from = Math.Min(row1, row2), to = Math.Max(row1, row2);
                for (int r = from; r < to; r++)
                    if (wallBelow[col1, r]) return false;
                return true;
            }
            return false;
        }

        // ---------------------------------------------------------------------------
        // Maze generation
        // ---------------------------------------------------------------------------

        /// <summary>
        /// Builds a mirror-symmetric maze for the given dungeon. The same dungeon number
        /// always gives the same maze. Basic dungeons have many walls, Worlord dungeons
        /// few, the Arena has a walled central room and the Pit is completely open.
        /// </summary>
        public void Generate(int dungeon)
        {
            bool isPit = Rules.IsPit(dungeon);
            bool isArena = Rules.IsArena(dungeon);
            float density = Rules.IsWorlord(dungeon) ? 0.08f : 0.22f;
            var rng = new Random(dungeon * 7919 + 13);

            for (int attempt = 0; attempt < 500; attempt++)
            {
                ClearWalls();
                if (!isPit) AddRandomSymmetricWalls(rng, density);
                if (isArena) BuildArenaRoom();
                if (IsFullyConnected()) return;   // every cell can be reached: good maze
            }
            ClearWalls(); // extremely unlikely fallback: an empty maze
        }

        private void ClearWalls()
        {
            for (int c = 0; c < Config.Cols; c++)
                for (int r = 0; r < Config.Rows; r++)
                {
                    wallRight[c, r] = false;
                    wallBelow[c, r] = false;
                }
        }

        private void AddRandomSymmetricWalls(Random rng, float density)
        {
            // Horizontal walls (the wall below a cell). Left half plus the centre column,
            // each one mirrored to the right half. Sometimes the wall is made longer.
            for (int r = 0; r < Config.Rows - 1; r++)
                for (int c = 0; c <= Config.Cols / 2; c++)
                {
                    if (rng.NextDouble() >= density) continue;
                    SetWallBelow(c, r);
                    if (c + 1 <= Config.Cols / 2 && rng.NextDouble() < 0.5) SetWallBelow(c + 1, r);
                }

            // Vertical walls (the wall right of a cell). Left half, mirrored to the right.
            for (int r = 0; r < Config.Rows; r++)
                for (int c = 0; c <= Config.Cols / 2 - 1; c++)
                {
                    if (r == Config.DoorRow && c == 0) continue; // keep the door entrance clear
                    if (rng.NextDouble() >= density) continue;
                    SetWallRight(c, r);
                    if (r + 1 < Config.Rows && rng.NextDouble() < 0.5) SetWallRight(c, r + 1);
                }
        }

        private void SetWallBelow(int col, int row)
        {
            wallBelow[col, row] = true;
            wallBelow[Config.Cols - 1 - col, row] = true;     // mirror image
        }

        private void SetWallRight(int col, int row)
        {
            wallRight[col, row] = true;
            wallRight[Config.Cols - 2 - col, row] = true;     // mirror image
        }

        /// <summary>The Arena: a big room in the middle that is open at door level.</summary>
        private void BuildArenaRoom()
        {
            const int left = 3, right = 7, top = 1, bottom = 4;

            // Remove everything inside the room.
            for (int c = left; c <= right; c++)
                for (int r = top; r <= bottom; r++)
                {
                    if (c < right) wallRight[c, r] = false;
                    if (r < bottom) wallBelow[c, r] = false;
                }

            // Build the room's outer walls, leaving openings at the door row.
            for (int c = left; c <= right; c++)
            {
                wallBelow[c, top - 1] = true;
                wallBelow[c, bottom] = true;
            }
            for (int r = top; r <= bottom; r++)
            {
                if (r == Config.DoorRow) continue;
                wallRight[left - 1, r] = true;
                wallRight[right, r] = true;
            }
        }

        /// <summary>Flood fill from the top-left cell to check that nothing is walled off.</summary>
        private bool IsFullyConnected()
        {
            var visited = new bool[Config.Cols, Config.Rows];
            var stack = new Stack<Point>();
            stack.Push(new Point(0, 0));
            visited[0, 0] = true;
            int count = 0;

            while (stack.Count > 0)
            {
                Point p = stack.Pop();
                count++;
                foreach (Direction d in DirectionExtensions.AllDirections)
                {
                    if (p.X == 0 && d == Direction.Left) continue;
                    if (p.X == Config.Cols - 1 && d == Direction.Right) continue;
                    if (HasWall(p.X, p.Y, d)) continue;

                    Point step = d.ToStep();
                    int nx = p.X + step.X, ny = p.Y + step.Y;
                    if (nx < 0 || ny < 0 || nx >= Config.Cols || ny >= Config.Rows) continue;
                    if (visited[nx, ny]) continue;
                    visited[nx, ny] = true;
                    stack.Push(new Point(nx, ny));
                }
            }
            return count == Config.Cols * Config.Rows;
        }
    }

    // =================================================================================
    //  5. SHOT  (worrior bullets and enemy lightning bolts)
    // =================================================================================
    public class Shot
    {
        public int PixelX, PixelY;
        public Direction Direction;
        public bool IsAlive = true;

        /// <summary>Who fired it. null means it is an enemy bolt (monster or Wizard).</summary>
        public readonly Worrior Owner;
        public bool IsEnemyBolt { get { return Owner == null; } }

        private readonly float speed;
        private float stepBudget;

        public Shot(int x, int y, Direction direction, float speed, Worrior owner)
        {
            PixelX = x; PixelY = y;
            Direction = direction;
            this.speed = speed;
            Owner = owner;
        }

        public void Update(float dt, Maze maze)
        {
            stepBudget += speed * dt;
            while (IsAlive && stepBudget >= 1f)
            {
                stepBudget -= 1f;
                StepOnePixel(maze);
            }
        }

        /// <summary>Moves one pixel; the shot dies when it hits a wall or leaves the maze.</summary>
        private void StepOnePixel(Maze maze)
        {
            Point step = Direction.ToStep();
            int newX = PixelX + step.X, newY = PixelY + step.Y;

            if (newX < 0 || newX >= Config.MazeWidth || newY < 0 || newY >= Config.MazeHeight)
            {
                IsAlive = false;
                return;
            }

            int col = PixelX / Config.CellSize, row = PixelY / Config.CellSize;
            int newCol = newX / Config.CellSize, newRow = newY / Config.CellSize;
            bool enteringNewCell = (col != newCol) || (row != newRow);

            if (enteringNewCell && maze.HasWall(col, row, Direction))
            {
                IsAlive = false;
                return;
            }

            PixelX = newX;
            PixelY = newY;
        }
    }

    // =================================================================================
    //  6. MOVER  (corridor movement shared by worriors and monsters)
    // =================================================================================
    /// <summary>
    /// Creatures move one pixel at a time along the centre lines of the corridors.
    /// They may only turn (or be blocked by a wall) when exactly in the middle of a cell.
    /// Between two cell centres they can only keep going or turn around.
    /// </summary>
    public abstract class Mover
    {
        protected readonly Maze maze;

        public int PixelX, PixelY;                 // position inside the maze, in pixels
        public Direction MoveDir = Direction.None; // the direction we last moved in
        public Direction Facing = Direction.Right;

        public bool UsedDoor;                      // set when we went through a side door
        public bool CanWrapThroughDoor = true;     // false for the Worluk (it escapes instead)
        public bool EscapedThroughDoor;

        protected float stepBudget;                // fractional pixels carried between frames
        private bool hasChosenDirectionHere;
        private Direction desiredDirection = Direction.None;

        protected Mover(Maze maze) { this.maze = maze; }

        public int Col { get { return Math.Max(0, Math.Min(Config.Cols - 1, PixelX / Config.CellSize)); } }
        public int Row { get { return Math.Max(0, Math.Min(Config.Rows - 1, PixelY / Config.CellSize)); } }

        /// <summary>True while in the little corridor outside a side door.</summary>
        public bool InDoorStub { get { return PixelX < 0 || PixelX >= Config.MazeWidth; } }

        public bool AtCellCenter
        {
            get
            {
                return !InDoorStub
                    && PixelX % Config.CellSize == Config.HalfCell
                    && PixelY % Config.CellSize == Config.HalfCell;
            }
        }

        public void PlaceAtCell(int col, int row)
        {
            PixelX = col * Config.CellSize + Config.HalfCell;
            PixelY = row * Config.CellSize + Config.HalfCell;
            MoveDir = Direction.None;
            stepBudget = 0f;
            hasChosenDirectionHere = false;
        }

        /// <summary>Try to move exactly one pixel. Returns false if something is in the way.</summary>
        public bool TryStep(Direction d)
        {
            if (d == Direction.None) return false;
            Point step = d.ToStep();

            // --- Outside the maze, in a door corridor: only left/right movement ---
            if (InDoorStub)
            {
                if (!d.IsHorizontal()) return false;
                PixelX += step.X;
                MoveDir = d;
                HandleLeavingThroughDoor();
                return true;
            }

            // --- Inside the maze ---
            bool alignedWithRowCentre = PixelY % Config.CellSize == Config.HalfCell;
            bool alignedWithColCentre = PixelX % Config.CellSize == Config.HalfCell;

            if (d.IsHorizontal())
            {
                if (!alignedWithRowCentre) return false;                  // not in a horizontal corridor
                if (alignedWithColCentre && maze.HasWall(Col, Row, d)) return false;
            }
            else
            {
                if (!alignedWithColCentre) return false;                  // not in a vertical corridor
                if (alignedWithRowCentre && maze.HasWall(Col, Row, d)) return false;
            }

            PixelX += step.X;
            PixelY += step.Y;
            MoveDir = d;
            return true;
        }

        /// <summary>The two side doors are connected: leave on one side, appear on the other.</summary>
        private void HandleLeavingThroughDoor()
        {
            bool offLeft = PixelX < -Config.DoorStubLength;
            bool offRight = PixelX >= Config.MazeWidth + Config.DoorStubLength;
            if (!offLeft && !offRight) return;

            if (!CanWrapThroughDoor)
            {
                EscapedThroughDoor = true;
                return;
            }

            PixelX = offLeft ? Config.MazeWidth + Config.DoorStubLength - 1 : -Config.DoorStubLength;
            UsedDoor = true;
        }

        /// <summary>
        /// Auto-moves for 'dt' seconds at 'speed'. Every time we arrive in the middle of a
        /// cell, 'chooseDirection' is asked where to go next. Used by monsters.
        /// </summary>
        public void Advance(float dt, float speed, Func<Direction> chooseDirection)
        {
            stepBudget += speed * dt;
            while (stepBudget >= 1f)
            {
                stepBudget -= 1f;

                if (AtCellCenter)
                {
                    if (!hasChosenDirectionHere)
                    {
                        desiredDirection = chooseDirection();
                        hasChosenDirectionHere = true;
                    }
                }
                else
                {
                    hasChosenDirectionHere = false;
                }

                if (!TryStep(desiredDirection)) hasChosenDirectionHere = false;
            }
        }
    }

    // =================================================================================
    //  7. ACTORS
    // =================================================================================
    public class Worrior : Mover
    {
        public readonly int PlayerNumber;       // 1 = yellow (right box), 2 = blue (left box)
        public readonly Color BodyColor;
        public readonly int HomeCol;            // column of the ready box: 10 (right) or 0 (left)
        public bool IsComputer;

        public Keys[] MoveKeys;                 // Up, Right, Down, Left
        public Keys[] FireKeys;

        public WorriorState State;
        public int Lives;
        public int Score;
        public float BoxTimer;
        public float DeathTimer;
        public Shot Shot;                       // only one shot may be in the air at a time
        public bool IsMoving;

        // Keyboard handling
        public Direction LastPressed = Direction.None;
        private Direction previousWanted = Direction.None;
        private float heldTime;

        // Computer-controlled worrior brain
        public Direction AiDirection = Direction.None;
        public Point AiLastDecisionCell = new Point(-1, -1);
        public float AiEnterDelay;

        public Worrior(Maze maze, int playerNumber, Color color, int homeCol) : base(maze)
        {
            PlayerNumber = playerNumber;
            BodyColor = color;
            HomeCol = homeCol;
            Lives = Config.StartLives;
            State = WorriorState.InBox;
        }

        public bool IsActive { get { return State == WorriorState.Active; } }

        /// <summary>Steps out of the ready box into the bottom corner of the dungeon.</summary>
        public void EnterMaze()
        {
            PlaceAtCell(HomeCol, Config.Rows - 1);
            Facing = Direction.Up;
            State = WorriorState.Active;
            AiDirection = Direction.None;
            AiLastDecisionCell = new Point(-1, -1);
            previousWanted = Direction.None;
            heldTime = 0f;
            IsMoving = false;
        }

        /// <summary>You can only turn in the middle of a cell, or around within a corridor.</summary>
        public bool CanTurnTo(Direction d)
        {
            return AtCellCenter || MoveDir == Direction.None || d.IsHorizontal() == MoveDir.IsHorizontal();
        }

        public void FaceDirection(Direction d)
        {
            if (d != Direction.None && CanTurnTo(d)) Facing = d;
        }

        /// <summary>
        /// Moves according to the direction the player (or the computer) is holding.
        /// A very short press only turns the worrior ("tap to turn"); a longer press walks.
        /// </summary>
        public void UpdateMovement(float dt, Direction wanted)
        {
            if (wanted != previousWanted)
            {
                heldTime = 0f;
                previousWanted = wanted;
            }

            if (wanted == Direction.None)
            {
                IsMoving = false;
                stepBudget = 0f;
                return;
            }

            heldTime += dt;
            FaceDirection(wanted);

            bool wasStandingStill = !IsMoving;
            if (wasStandingStill && heldTime < Config.TapDelay) return;   // just a tap: turn only

            stepBudget += Config.WorriorSpeed * dt;
            bool moved = false;
            while (stepBudget >= 1f)
            {
                stepBudget -= 1f;
                if (StepTowards(wanted)) moved = true;
                else { stepBudget = 0f; break; }
            }
            IsMoving = moved;
        }

        /// <summary>
        /// Moves one pixel towards the wanted direction. If the player already pressed a
        /// corner direction slightly too early, keep walking until the corner is reached.
        /// </summary>
        private bool StepTowards(Direction wanted)
        {
            if (TryStep(wanted)) return true;

            bool wantedIsPerpendicular = MoveDir != Direction.None
                && MoveDir.IsHorizontal() != wanted.IsHorizontal();
            if (wantedIsPerpendicular) return TryStep(MoveDir);
            return false;
        }
    }

    public class Monster : Mover
    {
        public readonly MonsterKind Kind;
        public float Speed;
        public float SpawnFlashTimer;           // newly teleported-in monsters are visible for a moment
        public Shot Bolt;                       // each monster has at most one lightning bolt in the air
        public float FireCooldown;
        public int TargetDoorSide = 1;          // Worluk only: +1 flies to the right door, -1 to the left

        public Monster(Maze maze, MonsterKind kind, float speed) : base(maze)
        {
            Kind = kind;
            Speed = speed;
            if (kind == MonsterKind.Worluk) CanWrapThroughDoor = false;
        }
    }

    /// <summary>Visual-only particles for explosions.</summary>
    public class Effect
    {
        public int X, Y;
        public float Age;
        public const float Life = 0.55f;
        public Color Color;
    }

    // =================================================================================
    //  8. GRAPHICS BUILT IN CODE
    // =================================================================================

    /// <summary>
    /// A tiny 5x7 pixel font. Every glyph is described as 7 rows of 5 bits ('1' = pixel).
    /// Each glyph becomes a small white texture that is tinted when drawn.
    /// </summary>
    public class BitmapFont
    {
        private const int GlyphWidth = 5;
        private const int GlyphHeight = 7;
        private const int Spacing = 1;

        private readonly Dictionary<char, Texture2D> glyphs = new Dictionary<char, Texture2D>();

        public BitmapFont(GraphicsDevice device)
        {
            Define(device, ' ', "-----", "-----", "-----", "-----", "-----", "-----", "-----");
            Define(device, 'A', "-XXX-", "X---X", "X---X", "XXXXX", "X---X", "X---X", "X---X");
            Define(device, 'B', "XXXX-", "X---X", "X---X", "XXXX-", "X---X", "X---X", "XXXX-");
            Define(device, 'C', "-XXX-", "X---X", "X----", "X----", "X----", "X---X", "-XXX-");
            Define(device, 'D', "XXXX-", "X---X", "X---X", "X---X", "X---X", "X---X", "XXXX-");
            Define(device, 'E', "XXXXX", "X----", "X----", "XXXX-", "X----", "X----", "XXXXX");
            Define(device, 'F', "XXXXX", "X----", "X----", "XXXX-", "X----", "X----", "X----");
            Define(device, 'G', "-XXX-", "X---X", "X----", "X-XXX", "X---X", "X---X", "-XXXX");
            Define(device, 'H', "X---X", "X---X", "X---X", "XXXXX", "X---X", "X---X", "X---X");
            Define(device, 'I', "-XXX-", "--X--", "--X--", "--X--", "--X--", "--X--", "-XXX-");
            Define(device, 'J', "--XXX", "---X-", "---X-", "---X-", "---X-", "X--X-", "-XX--");
            Define(device, 'K', "X---X", "X--X-", "X-X--", "XX---", "X-X--", "X--X-", "X---X");
            Define(device, 'L', "X----", "X----", "X----", "X----", "X----", "X----", "XXXXX");
            Define(device, 'M', "X---X", "XX-XX", "X-X-X", "X-X-X", "X---X", "X---X", "X---X");
            Define(device, 'N', "X---X", "XX--X", "X-X-X", "X--XX", "X---X", "X---X", "X---X");
            Define(device, 'O', "-XXX-", "X---X", "X---X", "X---X", "X---X", "X---X", "-XXX-");
            Define(device, 'P', "XXXX-", "X---X", "X---X", "XXXX-", "X----", "X----", "X----");
            Define(device, 'Q', "-XXX-", "X---X", "X---X", "X---X", "X-X-X", "X--X-", "-XX-X");
            Define(device, 'R', "XXXX-", "X---X", "X---X", "XXXX-", "X-X--", "X--X-", "X---X");
            Define(device, 'S', "-XXXX", "X----", "X----", "-XXX-", "----X", "----X", "XXXX-");
            Define(device, 'T', "XXXXX", "--X--", "--X--", "--X--", "--X--", "--X--", "--X--");
            Define(device, 'U', "X---X", "X---X", "X---X", "X---X", "X---X", "X---X", "-XXX-");
            Define(device, 'V', "X---X", "X---X", "X---X", "X---X", "X---X", "-X-X-", "--X--");
            Define(device, 'W', "X---X", "X---X", "X---X", "X-X-X", "X-X-X", "X-X-X", "-X-X-");
            Define(device, 'X', "X---X", "X---X", "-X-X-", "--X--", "-X-X-", "X---X", "X---X");
            Define(device, 'Y', "X---X", "X---X", "-X-X-", "--X--", "--X--", "--X--", "--X--");
            Define(device, 'Z', "XXXXX", "----X", "---X-", "--X--", "-X---", "X----", "XXXXX");
            Define(device, '0', "-XXX-", "X---X", "X--XX", "X-X-X", "XX--X", "X---X", "-XXX-");
            Define(device, '1', "--X--", "-XX--", "--X--", "--X--", "--X--", "--X--", "-XXX-");
            Define(device, '2', "-XXX-", "X---X", "----X", "---X-", "--X--", "-X---", "XXXXX");
            Define(device, '3', "XXXX-", "----X", "----X", "-XXX-", "----X", "----X", "XXXX-");
            Define(device, '4', "---X-", "--XX-", "-X-X-", "X--X-", "XXXXX", "---X-", "---X-");
            Define(device, '5', "XXXXX", "X----", "XXXX-", "----X", "----X", "X---X", "-XXX-");
            Define(device, '6', "--XX-", "-X---", "X----", "XXXX-", "X---X", "X---X", "-XXX-");
            Define(device, '7', "XXXXX", "----X", "---X-", "--X--", "-X---", "-X---", "-X---");
            Define(device, '8', "-XXX-", "X---X", "X---X", "-XXX-", "X---X", "X---X", "-XXX-");
            Define(device, '9', "-XXX-", "X---X", "X---X", "-XXXX", "----X", "---X-", "-XX--");
            Define(device, '-', "-----", "-----", "-----", "XXXXX", "-----", "-----", "-----");
            Define(device, '.', "-----", "-----", "-----", "-----", "-----", "-XX--", "-XX--");
            Define(device, ',', "-----", "-----", "-----", "-----", "-XX--", "--X--", "-X---");
            Define(device, ':', "-----", "-XX--", "-XX--", "-----", "-XX--", "-XX--", "-----");
            Define(device, '!', "--X--", "--X--", "--X--", "--X--", "--X--", "-----", "--X--");
            Define(device, '?', "-XXX-", "X---X", "----X", "---X-", "--X--", "-----", "--X--");
            Define(device, '/', "----X", "----X", "---X-", "--X--", "-X---", "X----", "X----");
            Define(device, '+', "-----", "--X--", "--X--", "XXXXX", "--X--", "--X--", "-----");
            Define(device, '<', "---X-", "--X--", "-X---", "X----", "-X---", "--X--", "---X-");
            Define(device, '>', "-X---", "--X--", "---X-", "----X", "---X-", "--X--", "-X---");
        }

        private void Define(GraphicsDevice device, char character, params string[] rows)
        {
            var texture = new Texture2D(device, GlyphWidth, GlyphHeight);
            var pixels = new Color[GlyphWidth * GlyphHeight];
            for (int y = 0; y < GlyphHeight; y++)
                for (int x = 0; x < GlyphWidth; x++)
                    pixels[y * GlyphWidth + x] = rows[y][x] == 'X' ? Color.White : Color.Transparent;
            texture.SetData(pixels);
            glyphs[character] = texture;
        }

        public int Measure(string text, int scale = 1)
        {
            if (text.Length == 0) return 0;
            return (text.Length * (GlyphWidth + Spacing) - Spacing) * scale;
        }

        public void Draw(SpriteBatch batch, string text, int x, int y, Color color, int scale = 1)
        {
            foreach (char raw in text)
            {
                char c = char.ToUpperInvariant(raw);
                Texture2D glyph;
                if (glyphs.TryGetValue(c, out glyph))
                    batch.Draw(glyph, new Vector2(x, y), null, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                x += (GlyphWidth + Spacing) * scale;
            }
        }

        public void DrawCentered(SpriteBatch batch, string text, int centerX, int y, Color color, int scale = 1)
        {
            Draw(batch, text, centerX - Measure(text, scale) / 2, y, color, scale);
        }
    }

    /// <summary>
    /// All sprites, drawn by hand as text patterns.
    ///   '#' = the creature's main colour     'k' = black (eyes, visor)
    ///   'w' = white                          '.' = transparent
    /// Every creature has two animation frames. Sprites face RIGHT; the game flips or
    /// rotates them for other directions.
    /// </summary>
    public class SpriteLibrary
    {
        public static readonly Color Yellow = new Color(255, 220, 40);
        public static readonly Color Blue = new Color(70, 120, 255);
        public static readonly Color Red = new Color(245, 55, 35);
        public static readonly Color Orange = new Color(255, 140, 20);

        public Texture2D[] YellowWorrior, BlueWorrior;
        public Texture2D[] Burwor, Garwor, Thorwor, Worluk, Wizard;
        public Texture2D MiniYellow, MiniBlue;

        // ----- Worrior (a space soldier with a rifle pointing right) -----
        private static readonly string[] WorriorA =
        {
            "....####....",
            "...######...",
            "...###kkk...",
            "...######...",
            "..########..",
            "..#.####wwww",
            "....####....",
            "....####....",
            "...##..##...",
            "...##..##...",
            "..###..###..",
            "............",
        };
        private static readonly string[] WorriorB =
        {
            "....####....",
            "...######...",
            "...###kkk...",
            "...######...",
            "..########..",
            "..#.####wwww",
            "....####....",
            "....####....",
            "..##....##..",
            "..##....##..",
            ".###....###.",
            "............",
        };

        private static readonly string[] MiniWorrior =
        {
            "..####..",
            ".######.",
            ".##kkk#.",
            ".######.",
            "..####..",
            ".#.##.#.",
            "..#..#..",
            ".##..##.",
        };

        // ----- Burwor: the blue wolf-like creature -----
        private static readonly string[] BurworA =
        {
            "..#......#..",
            "..##....##..",
            "..########..",
            ".##########.",
            ".#kk####kk#.",
            ".##########.",
            ".##########.",
            "..########..",
            "..#.#..#.#..",
            "..#.#..#.#..",
            ".##.#..#.##.",
            "............",
        };
        private static readonly string[] BurworB =
        {
            "..#......#..",
            "..##....##..",
            "..########..",
            ".##########.",
            ".#kk####kk#.",
            ".##########.",
            ".##########.",
            "..########..",
            ".#..#..#..#.",
            ".#..#..#..#.",
            "##..#..#..##",
            "............",
        };

        // ----- Garwor: the yellow Tyrannosaurus-like creature -----
        private static readonly string[] GarworA =
        {
            "....######..",
            "...########.",
            "...##k#####.",
            "...#########",
            "...######...",
            "#..######...",
            "##.#######..",
            ".#########..",
            "..#######...",
            "...##..##...",
            "...##..##...",
            "..###.###...",
        };
        private static readonly string[] GarworB =
        {
            "....######..",
            "...########.",
            "...##k#####.",
            "...#########",
            "...######...",
            "#..######...",
            "##.#######..",
            ".#########..",
            "..#######...",
            "..##....##..",
            "..##....##..",
            ".###....###.",
        };

        // ----- Thorwor: the red scorpion-like creature -----
        private static readonly string[] ThorworA =
        {
            ".#........#.",
            ".##......##.",
            "..#..##..#..",
            "..#.####.#..",
            "...######...",
            "..#k####k#..",
            ".##########.",
            "###.####.###",
            "#..#.##.#..#",
            "...#....#...",
            "..#......#..",
            "............",
        };
        private static readonly string[] ThorworB =
        {
            ".#........#.",
            ".##......##.",
            "..#..##..#..",
            "..#.####.#..",
            "...######...",
            "..#k####k#..",
            ".##########.",
            "###.####.###",
            ".#.#.##.#.#.",
            "..#......#..",
            "...#....#...",
            "............",
        };

        // ----- Worluk: the flying insect (wings flap between frames) -----
        private static readonly string[] WorlukA =
        {
            "#..........#",
            "##...##...##",
            ".###.##.###.",
            "..##.##.##..",
            "...######...",
            "..###kk###..",
            "...######...",
            "....####....",
            ".....##.....",
            "....#..#....",
            "............",
            "............",
        };
        private static readonly string[] WorlukB =
        {
            "............",
            "#..........#",
            "##...##...##",
            ".###.##.###.",
            "...######...",
            "..###kk###..",
            "...######...",
            "....####....",
            ".....##.....",
            "....#..#....",
            "............",
            "............",
        };

        // ----- Wizard of Wor: pointy hat, face, long robe -----
        private static readonly string[] WizardA =
        {
            ".....##.....",
            "....####....",
            "...######...",
            ".##########.",
            "...#wwww#...",
            "...#wkwk#...",
            "...#wwww#...",
            "..########..",
            ".##########.",
            ".##########.",
            ".###.##.###.",
            "..##....##..",
        };
        private static readonly string[] WizardB =
        {
            ".....##.....",
            "....####....",
            "...######...",
            ".##########.",
            "...#wwww#...",
            "...#wkwk#...",
            "...#wwww#...",
            "..########..",
            ".##########.",
            ".##########.",
            ".##..##..##.",
            ".#...##...#.",
        };

        public SpriteLibrary(GraphicsDevice device)
        {
            YellowWorrior = new[] { Build(device, WorriorA, Yellow), Build(device, WorriorB, Yellow) };
            BlueWorrior = new[] { Build(device, WorriorA, Blue), Build(device, WorriorB, Blue) };
            MiniYellow = Build(device, MiniWorrior, Yellow);
            MiniBlue = Build(device, MiniWorrior, Blue);

            Burwor = new[] { Build(device, BurworA, Blue), Build(device, BurworB, Blue) };
            Garwor = new[] { Build(device, GarworA, Yellow), Build(device, GarworB, Yellow) };
            Thorwor = new[] { Build(device, ThorworA, Red), Build(device, ThorworB, Red) };
            Worluk = new[] { Build(device, WorlukA, Orange), Build(device, WorlukB, Orange) };
            Wizard = new[] { Build(device, WizardA, Blue), Build(device, WizardB, Blue) };
        }

        public Texture2D[] ForWorrior(Worrior w) { return w.PlayerNumber == 1 ? YellowWorrior : BlueWorrior; }

        public Texture2D[] ForMonster(MonsterKind kind)
        {
            switch (kind)
            {
                case MonsterKind.Burwor: return Burwor;
                case MonsterKind.Garwor: return Garwor;
                case MonsterKind.Thorwor: return Thorwor;
                default: return Worluk;
            }
        }

        /// <summary>Turns a text pattern into a texture.</summary>
        private static Texture2D Build(GraphicsDevice device, string[] rows, Color mainColor)
        {
            int height = rows.Length;
            int width = rows[0].Length;
            var pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    char c = x < rows[y].Length ? rows[y][x] : '.';
                    Color color = Color.Transparent;
                    if (c == '#') color = mainColor;
                    else if (c == 'k') color = Color.Black;
                    else if (c == 'w') color = Color.White;
                    pixels[y * width + x] = color;
                }

            var texture = new Texture2D(device, width, height);
            texture.SetData(pixels);
            return texture;
        }
    }

    // =================================================================================
    //  9. THE GAME
    // =================================================================================
    public class WizardOfWorGame : Game
    {
        // ------------------------------------------------------------------ framework
        private readonly GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private RenderTarget2D canvas;            // the small virtual screen we draw on
        private Texture2D pixel;                  // 1x1 white texture for rectangles
        private BitmapFont font;
        private SpriteLibrary sprites;
        private int windowedWidth = 960, windowedHeight = 648;

        private KeyboardState keys, previousKeys;
        private readonly Random rng = new Random();
        private float clock;                      // seconds since start, drives animations
        private readonly List<Point> stars = new List<Point>();

        // ------------------------------------------------------------------ game state
        private GameState state = GameState.Title;
        private bool twoPlayers;
        private float gameOverTimer;

        private readonly Maze maze = new Maze();
        private Worrior yellow, blue;             // player 1 and player 2 (or the computer)
        private Worrior[] worriors = new Worrior[0];

        private readonly List<Monster> monsters = new List<Monster>();
        private readonly List<Shot> shots = new List<Shot>();
        private readonly List<Effect> effects = new List<Effect>();

        private int dungeon;
        private Stage stage;
        private float stageTimer;
        private float speedUpTimer;
        private bool doubleScoreActive;           // points doubled in this dungeon
        private bool doubleScoreNext;             // ... and in the next one

        // Wizard of Wor
        private bool wizardActive;
        private int wizardCol, wizardRow;
        private float wizardTimer;
        private int wizardTeleports;
        private Shot wizardBolt;

        // Message shown between the maze and the radar
        private string overrideMessage;
        private float overrideMessageTimer;

        // =============================================================================
        //  Setup
        // =============================================================================
        public WizardOfWorGame()
        {
            graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = windowedWidth,
                PreferredBackBufferHeight = windowedHeight,
                HardwareModeSwitch = false          // "borderless" fullscreen, switches instantly
            };
            Window.AllowUserResizing = true;
            Window.Title = "Wizard of Wor";
            IsMouseVisible = false;
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            canvas = new RenderTarget2D(GraphicsDevice, Config.ScreenWidth, Config.ScreenHeight);

            pixel = new Texture2D(GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });

            font = new BitmapFont(GraphicsDevice);
            sprites = new SpriteLibrary(GraphicsDevice);

            // A fixed starfield for the background.
            var starRng = new Random(42);
            for (int i = 0; i < 70; i++)
                stars.Add(new Point(starRng.Next(Config.ScreenWidth), starRng.Next(Config.ScreenHeight)));
        }

        private void ToggleFullscreen()
        {
            if (!graphics.IsFullScreen)
            {
                windowedWidth = GraphicsDevice.PresentationParameters.BackBufferWidth;
                windowedHeight = GraphicsDevice.PresentationParameters.BackBufferHeight;
                DisplayMode mode = GraphicsDevice.Adapter.CurrentDisplayMode;
                graphics.PreferredBackBufferWidth = mode.Width;
                graphics.PreferredBackBufferHeight = mode.Height;
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

        private bool WasPressed(Keys key) { return keys.IsKeyDown(key) && previousKeys.IsKeyUp(key); }

        // =============================================================================
        //  UPDATE
        // =============================================================================
        protected override void Update(GameTime gameTime)
        {
            float dt = Math.Min((float)gameTime.ElapsedGameTime.TotalSeconds, 0.05f);
            clock += dt;
            previousKeys = keys;
            keys = Keyboard.GetState();

            if (WasPressed(Keys.F11)) ToggleFullscreen();
            if (keys.IsKeyDown(Keys.Escape)) Exit();

            switch (state)
            {
                case GameState.Title: UpdateTitle(); break;
                case GameState.Playing: UpdatePlaying(dt); break;
                case GameState.GameOver: UpdateGameOver(dt); break;
            }

            base.Update(gameTime);
        }

        private void UpdateTitle()
        {
            if (WasPressed(Keys.D1) || WasPressed(Keys.NumPad1)) StartNewGame(false);
            if (WasPressed(Keys.D2) || WasPressed(Keys.NumPad2)) StartNewGame(true);
        }

        private void UpdateGameOver(float dt)
        {
            gameOverTimer -= dt;
            UpdateEffects(dt);
            if (gameOverTimer <= 0f || WasPressed(Keys.Enter)) state = GameState.Title;
        }

        // -----------------------------------------------------------------------------
        //  Starting a game and a dungeon
        // -----------------------------------------------------------------------------
        private void StartNewGame(bool twoPlayerGame)
        {
            twoPlayers = twoPlayerGame;

            yellow = new Worrior(maze, 1, SpriteLibrary.Yellow, Config.Cols - 1)
            {
                MoveKeys = new[] { Keys.Up, Keys.Right, Keys.Down, Keys.Left },
                FireKeys = new[] { Keys.Space, Keys.RightControl },
            };
            blue = new Worrior(maze, 2, SpriteLibrary.Blue, 0)
            {
                MoveKeys = new[] { Keys.W, Keys.D, Keys.S, Keys.A },
                FireKeys = new[] { Keys.Q, Keys.LeftControl },
                IsComputer = !twoPlayerGame,        // in a 1-player game blue is our friend, the computer
            };
            worriors = new[] { yellow, blue };

            doubleScoreNext = false;
            state = GameState.Playing;
            StartDungeon(1);
        }

        private void StartDungeon(int number)
        {
            dungeon = number;
            maze.Generate(dungeon);
            maze.ForceDoorsOpen = false;

            monsters.Clear();
            shots.Clear();
            effects.Clear();
            wizardActive = false;
            wizardBolt = null;
            stage = Stage.Monsters;
            stageTimer = 0f;
            speedUpTimer = 0f;

            doubleScoreActive = doubleScoreNext;
            doubleScoreNext = false;

            // Put every worrior that still has a life into its ready box.
            foreach (Worrior w in worriors)
            {
                if (Rules.AwardsBonusWorrior(dungeon) && w.Lives > 0)
                    w.Lives = Math.Min(Config.MaxLives, w.Lives + 1);

                w.Shot = null;
                if (w.Lives > 0)
                {
                    w.State = WorriorState.InBox;
                    w.BoxTimer = Config.BoxCountdown;
                    w.AiEnterDelay = 0.8f + (float)rng.NextDouble() * 1.5f;
                }
                else
                {
                    w.State = WorriorState.Out;
                }
            }

            // Every dungeon starts with six Burwors.
            for (int i = 0; i < 6; i++) SpawnMonster(MonsterKind.Burwor, InitialMonsterSpeed());

            if (doubleScoreActive) ShowMessage("DOUBLE SCORE", 2.5f);
        }

        private float InitialMonsterSpeed()
        {
            if (dungeon >= 7) return Config.MonsterTopSpeed;      // Worlord dungeons: all at top speed
            return Math.Min(Config.MonsterTopSpeed, 28f + 4f * dungeon);
        }

        private void ShowMessage(string text, float seconds)
        {
            overrideMessage = text;
            overrideMessageTimer = seconds;
        }

        // -----------------------------------------------------------------------------
        //  The main playing update
        // -----------------------------------------------------------------------------
        private void UpdatePlaying(float dt)
        {
            if (overrideMessageTimer > 0f) overrideMessageTimer -= dt;

            maze.Update(dt);
            UpdateMonsterSpeedUp(dt);
            UpdateWorriors(dt);
            UpdateMonsters(dt);
            UpdateWizard(dt);
            foreach (Shot s in shots) s.Update(dt, maze);
            ResolveCollisions();
            UpdateEffects(dt);
            UpdateStage(dt);
            CheckForGameOver();
        }

        /// <summary>Every ~7 seconds all monsters get a bit faster.</summary>
        private void UpdateMonsterSpeedUp(float dt)
        {
            speedUpTimer += dt;
            if (speedUpTimer < Config.MonsterSpeedUpInterval) return;
            speedUpTimer = 0f;
            foreach (Monster m in monsters)
                if (m.Kind != MonsterKind.Worluk)
                    m.Speed = Math.Min(Config.MonsterTopSpeed, m.Speed + Config.MonsterSpeedUpAmount);
        }

        // =============================================================================
        //  WORRIORS
        // =============================================================================
        private void UpdateWorriors(float dt)
        {
            foreach (Worrior w in worriors)
            {
                switch (w.State)
                {
                    case WorriorState.InBox: UpdateWorriorInBox(w, dt); break;
                    case WorriorState.Active: UpdateActiveWorrior(w, dt); break;
                    case WorriorState.Dying: UpdateDyingWorrior(w, dt); break;
                }

                if (w.UsedDoor) { w.UsedDoor = false; OnDoorUsed(); }
            }
        }

        /// <summary>Doors shut for a short time after someone has used them.</summary>
        private void OnDoorUsed()
        {
            if (stage != Stage.Worluk) maze.CloseDoorsForAWhile();
        }

        private void UpdateWorriorInBox(Worrior w, float dt)
        {
            w.BoxTimer -= dt;

            bool wantsToLeave;
            if (w.IsComputer) wantsToLeave = (Config.BoxCountdown - w.BoxTimer) >= w.AiEnterDelay;
            else wantsToLeave = ReadHumanDirection(w) == Direction.Up;   // push the stick towards the maze

            // After 10 seconds the worrior is pushed into the dungeon automatically.
            if (wantsToLeave || w.BoxTimer <= 0f) w.EnterMaze();
        }

        private void UpdateDyingWorrior(Worrior w, float dt)
        {
            w.DeathTimer -= dt;
            if (w.DeathTimer > 0f) return;

            if (w.Lives > 0)
            {
                w.State = WorriorState.InBox;           // the backup worrior's box opens
                w.BoxTimer = Config.BoxCountdown;
                w.AiEnterDelay = 0.8f + (float)rng.NextDouble() * 1.5f;
            }
            else
            {
                w.State = WorriorState.Out;
            }
        }

        private void UpdateActiveWorrior(Worrior w, float dt)
        {
            if (w.IsComputer)
            {
                UpdateComputerWorrior(w, dt);
                return;
            }

            Direction wanted = ReadHumanDirection(w);
            w.UpdateMovement(dt, wanted);

            if (IsAnyKeyDown(w.FireKeys)) TryFire(w);
        }

        private bool IsAnyKeyDown(Keys[] list)
        {
            foreach (Keys k in list) if (keys.IsKeyDown(k)) return true;
            return false;
        }

        /// <summary>Reads the 4 direction keys; the most recently pressed key wins.</summary>
        private Direction ReadHumanDirection(Worrior w)
        {
            for (int i = 0; i < 4; i++)
                if (keys.IsKeyDown(w.MoveKeys[i]) && previousKeys.IsKeyUp(w.MoveKeys[i]))
                    w.LastPressed = (Direction)i;

            if (w.LastPressed != Direction.None && keys.IsKeyDown(w.MoveKeys[(int)w.LastPressed]))
                return w.LastPressed;

            for (int i = 0; i < 4; i++)
                if (keys.IsKeyDown(w.MoveKeys[i]))
                {
                    w.LastPressed = (Direction)i;
                    return w.LastPressed;
                }

            w.LastPressed = Direction.None;
            return Direction.None;
        }

        /// <summary>Only one shot may be in the air at a time.</summary>
        private void TryFire(Worrior w)
        {
            if (w.Shot != null && w.Shot.IsAlive) return;
            if (w.InDoorStub) return;
            w.Shot = new Shot(w.PixelX, w.PixelY, w.Facing, Config.WorriorShotSpeed, w);
            shots.Add(w.Shot);
        }

        // -----------------------------------------------------------------------------
        //  The computer-controlled blue worrior (1-player game).
        //  He only shoots monsters, never the player (but you can walk into his shots!).
        // -----------------------------------------------------------------------------
        private void UpdateComputerWorrior(Worrior w, float dt)
        {
            // 1. Is a monster standing in a clear corridor line from us? Then shoot it.
            Direction shootDir = FindMonsterInLineOfFire(w);
            if (shootDir != Direction.None && (w.Facing == shootDir || w.AtCellCenter))
            {
                w.FaceDirection(shootDir);
                if (w.Facing == shootDir) TryFire(w);
                w.UpdateMovement(dt, Direction.None);   // stand still while shooting
                return;
            }

            // 2. Otherwise pick a new direction every time we reach a cell centre.
            if (w.AtCellCenter && (w.AiLastDecisionCell.X != w.Col || w.AiLastDecisionCell.Y != w.Row))
            {
                w.AiLastDecisionCell = new Point(w.Col, w.Row);
                w.AiDirection = ChooseComputerWorriorDirection(w);
            }
            w.UpdateMovement(dt, w.AiDirection);
        }

        private Direction FindMonsterInLineOfFire(Worrior w)
        {
            foreach (Direction d in DirectionExtensions.AllDirections)
                foreach (Monster m in monsters)
                    if (IsInFront(w, m, d) && maze.LineOfSight(w.Col, w.Row, m.Col, m.Row))
                        return d;
            return Direction.None;
        }

        private Direction ChooseComputerWorriorDirection(Worrior w)
        {
            List<Direction> options = OpenDirections(w, true);
            if (options.Count == 0) return Direction.None;

            // Mostly hunt the nearest monster, sometimes just wander.
            Monster target = NearestMonster(w);
            if (target != null && rng.NextDouble() < 0.6)
                return DirectionTowards(w, options, target.Col, target.Row);

            return options[rng.Next(options.Count)];
        }

        // =============================================================================
        //  MONSTERS
        // =============================================================================
        private Monster SpawnMonster(MonsterKind kind, float speed)
        {
            var monster = new Monster(maze, kind, speed);

            // Pick a random cell that is not close to either worrior.
            int col = 0, row = 0;
            for (int attempt = 0; attempt < 100; attempt++)
            {
                col = rng.Next(Config.Cols);
                row = rng.Next(Config.Rows);
                if (IsFarFromWorriors(col, row, 5)) break;
            }

            monster.PlaceAtCell(col, row);
            monster.SpawnFlashTimer = 1.5f;       // teleported-in monsters are briefly visible
            monsters.Add(monster);
            return monster;
        }

        private bool IsFarFromWorriors(int col, int row, int minDistance)
        {
            foreach (Worrior w in worriors)
            {
                // Worriors in a box count as standing in their corner cell.
                int wc = w.IsActive ? w.Col : w.HomeCol;
                int wr = w.IsActive ? w.Row : Config.Rows - 1;
                if (Math.Abs(wc - col) + Math.Abs(wr - row) < minDistance) return false;
            }
            return true;
        }

        private void UpdateMonsters(float dt)
        {
            bool worlukEscaped = false;

            foreach (Monster m in monsters.ToArray())
            {
                m.SpawnFlashTimer -= dt;
                m.FireCooldown -= dt;

                if (m.Kind == MonsterKind.Worluk)
                {
                    m.Advance(dt, Config.WorlukSpeed, () => ChooseWorlukDirection(m));
                    if (m.EscapedThroughDoor)
                    {
                        monsters.Remove(m);
                        worlukEscaped = true;
                    }
                    continue;
                }

                m.Advance(dt, m.Speed, () => ChooseMonsterDirection(m));
                if (m.UsedDoor) { m.UsedDoor = false; OnDoorUsed(); }
                MaybeShoot(m, dt);
            }

            if (worlukEscaped) ShowMessage("ESCAPED", 2.5f);
        }

        /// <summary>
        /// Called when a monster arrives in the middle of a cell. Monsters don't turn back
        /// unless they must. The deeper the dungeon, the more often they chase a worrior.
        /// </summary>
        private Direction ChooseMonsterDirection(Monster m)
        {
            List<Direction> options = OpenDirections(m, true);

            Worrior target = NearestActiveWorrior(m);
            float chaseChance = Math.Min(0.85f, 0.2f + 0.06f * dungeon);
            if (target != null && rng.NextDouble() < chaseChance)
                return DirectionTowards(m, options, target.Col, target.Row);

            return options[rng.Next(options.Count)];
        }

        /// <summary>The Worluk flies erratically but wants to reach a side door.</summary>
        private Direction ChooseWorlukDirection(Monster m)
        {
            List<Direction> options = OpenDirections(m, true);
            if (rng.NextDouble() < 0.4) return options[rng.Next(options.Count)];

            int targetCol = m.TargetDoorSide > 0 ? Config.Cols : -1;    // just outside the door
            return DirectionTowards(m, options, targetCol, Config.DoorRow);
        }

        private void MaybeShoot(Monster m, float dt)
        {
            if (m.Speed >= Config.MonsterTopSpeed) return;           // too fast to shoot
            if (m.Bolt != null && m.Bolt.IsAlive) return;
            if (m.FireCooldown > 0f || m.MoveDir == Direction.None) return;

            foreach (Worrior w in worriors)
            {
                if (!w.IsActive) continue;
                bool inSights = IsInFront(m, w, m.MoveDir) && maze.LineOfSight(m.Col, m.Row, w.Col, w.Row);
                if (inSights && rng.NextDouble() < 2.0 * dt)
                {
                    m.Bolt = new Shot(m.PixelX, m.PixelY, m.MoveDir, Config.EnemyBoltSpeed, null);
                    shots.Add(m.Bolt);
                    m.FireCooldown = 1.0f;
                    return;
                }
            }
        }

        /// <summary>
        /// Garwors and Thorwors are invisible, except when they share a corridor with a
        /// worrior, were just teleported in, or move at top speed. Burwors are always seen.
        /// </summary>
        private bool IsMonsterVisible(Monster m)
        {
            if (m.Kind == MonsterKind.Burwor || m.Kind == MonsterKind.Worluk) return true;
            if (m.Speed >= Config.MonsterTopSpeed) return true;
            if (m.SpawnFlashTimer > 0f) return true;

            foreach (Worrior w in worriors)
                if (w.IsActive && maze.LineOfSight(m.Col, m.Row, w.Col, w.Row)) return true;
            return false;
        }

        // =============================================================================
        //  SHARED AI HELPERS
        // =============================================================================

        /// <summary>The directions we can walk from our current cell.</summary>
        private List<Direction> OpenDirections(Mover who, bool avoidTurningBack)
        {
            var list = new List<Direction>();
            foreach (Direction d in DirectionExtensions.AllDirections)
                if (!maze.HasWall(who.Col, who.Row, d)) list.Add(d);

            // Turning back is only allowed in a dead end.
            if (avoidTurningBack && who.MoveDir != Direction.None && list.Count > 1)
                list.Remove(who.MoveDir.Opposite());
            return list;
        }

        /// <summary>Of the given directions, the one that gets us closest to the target cell.</summary>
        private Direction DirectionTowards(Mover who, List<Direction> options, int targetCol, int targetRow)
        {
            Direction best = options[0];
            int bestDistance = int.MaxValue;
            foreach (Direction d in options)
            {
                Point step = d.ToStep();
                int distance = Math.Abs(who.Col + step.X - targetCol) + Math.Abs(who.Row + step.Y - targetRow);
                if (distance < bestDistance) { bestDistance = distance; best = d; }
            }
            return best;
        }

        private Worrior NearestActiveWorrior(Mover from)
        {
            Worrior best = null;
            int bestDistance = int.MaxValue;
            foreach (Worrior w in worriors)
            {
                if (!w.IsActive) continue;
                int distance = Math.Abs(w.Col - from.Col) + Math.Abs(w.Row - from.Row);
                if (distance < bestDistance) { bestDistance = distance; best = w; }
            }
            return best;
        }

        private Monster NearestMonster(Mover from)
        {
            Monster best = null;
            int bestDistance = int.MaxValue;
            foreach (Monster m in monsters)
            {
                int distance = Math.Abs(m.Col - from.Col) + Math.Abs(m.Row - from.Row);
                if (distance < bestDistance) { bestDistance = distance; best = m; }
            }
            return best;
        }

        /// <summary>Is 'target' in the row/column that 'shooter' is looking along, in front of him?</summary>
        private bool IsInFront(Mover shooter, Mover target, Direction direction)
        {
            switch (direction)
            {
                case Direction.Right: return shooter.Row == target.Row && target.PixelX > shooter.PixelX;
                case Direction.Left: return shooter.Row == target.Row && target.PixelX < shooter.PixelX;
                case Direction.Down: return shooter.Col == target.Col && target.PixelY > shooter.PixelY;
                case Direction.Up: return shooter.Col == target.Col && target.PixelY < shooter.PixelY;
            }
            return false;
        }

        // =============================================================================
        //  THE WIZARD OF WOR
        // =============================================================================
        private void StartWizard()
        {
            stage = Stage.Wizard;
            wizardActive = true;
            wizardTeleports = 0;
            ShowMessage("WIZARD OF WOR", 100f);
            WizardTeleport();
        }

        private void UpdateWizard(float dt)
        {
            if (!wizardActive) return;
            wizardTimer -= dt;
            if (wizardTimer <= 0f) WizardTeleport();
        }

        /// <summary>
        /// Each teleport lands closer to a chosen worrior and fires a lightning bolt.
        /// Deeper dungeons: he teleports faster and so gets close sooner.
        /// </summary>
        private void WizardTeleport()
        {
            var candidates = new List<Worrior>();
            foreach (Worrior w in worriors) if (w.IsActive) candidates.Add(w);
            if (candidates.Count == 0) { EndWizardStage(); return; }

            Worrior target = candidates[rng.Next(candidates.Count)];
            int wantedDistance = Math.Max(1, 7 - wizardTeleports);
            wizardTeleports++;

            // Collect all cells at (about) the wanted distance from the target.
            var cells = new List<Point>();
            for (int slack = 0; slack < 6 && cells.Count == 0; slack++)
                for (int c = 0; c < Config.Cols; c++)
                    for (int r = 0; r < Config.Rows; r++)
                    {
                        int distance = Math.Abs(c - target.Col) + Math.Abs(r - target.Row);
                        if (distance == wantedDistance - slack || distance == wantedDistance + slack)
                            if (distance >= 1) cells.Add(new Point(c, r));
                    }
            Point cell = cells.Count > 0 ? cells[rng.Next(cells.Count)] : new Point(5, 2);
            wizardCol = cell.X;
            wizardRow = cell.Y;

            wizardTimer = Math.Max(0.35f, 1.5f - 0.07f * dungeon);

            // Fire a bolt: towards the worrior if lined up, otherwise in a random direction.
            if (wizardBolt == null || !wizardBolt.IsAlive)
            {
                Direction dir = DirectionExtensions.AllDirections[rng.Next(4)];
                if (wizardRow == target.Row && maze.LineOfSight(wizardCol, wizardRow, target.Col, target.Row))
                    dir = target.PixelX > WizardPixelX ? Direction.Right : Direction.Left;
                else if (wizardCol == target.Col && maze.LineOfSight(wizardCol, wizardRow, target.Col, target.Row))
                    dir = target.PixelY > WizardPixelY ? Direction.Down : Direction.Up;

                wizardBolt = new Shot(WizardPixelX, WizardPixelY, dir, Config.EnemyBoltSpeed * 1.2f, null);
                shots.Add(wizardBolt);
            }
        }

        private int WizardPixelX { get { return wizardCol * Config.CellSize + Config.HalfCell; } }
        private int WizardPixelY { get { return wizardRow * Config.CellSize + Config.HalfCell; } }

        private void EndWizardStage()
        {
            wizardActive = false;
            stage = Stage.Finished;
            stageTimer = 0f;
            overrideMessageTimer = 0f;
        }

        // =============================================================================
        //  COLLISIONS AND KILLING
        // =============================================================================
        private static bool IsNear(int x1, int y1, int x2, int y2, int distance)
        {
            return Math.Abs(x1 - x2) < distance && Math.Abs(y1 - y2) < distance;
        }

        private void ResolveCollisions()
        {
            ShotsCancelEachOther();
            WorriorShotsHitThings();
            EnemyBoltsHitWorriors();
            CreaturesTouchWorriors();
            shots.RemoveAll(s => !s.IsAlive);
        }

        /// <summary>Shots can be negated by other shots, monster breath and Wizard lightning.</summary>
        private void ShotsCancelEachOther()
        {
            for (int i = 0; i < shots.Count; i++)
                for (int j = i + 1; j < shots.Count; j++)
                {
                    Shot a = shots[i], b = shots[j];
                    if (!a.IsAlive || !b.IsAlive) continue;
                    if (a.IsEnemyBolt && b.IsEnemyBolt) continue;      // enemy bolts pass each other
                    if (IsNear(a.PixelX, a.PixelY, b.PixelX, b.PixelY, 4))
                    {
                        a.IsAlive = false;
                        b.IsAlive = false;
                    }
                }
        }

        private void WorriorShotsHitThings()
        {
            foreach (Shot s in shots)
            {
                if (!s.IsAlive || s.IsEnemyBolt) continue;

                // Hit a monster?
                foreach (Monster m in monsters.ToArray())
                    if (IsNear(s.PixelX, s.PixelY, m.PixelX, m.PixelY, Config.HitDistance))
                    {
                        KillMonster(m, s.Owner);
                        s.IsAlive = false;
                        break;
                    }
                if (!s.IsAlive) continue;

                // Hit the Wizard? (he is only banished until the next dungeon)
                if (wizardActive && IsNear(s.PixelX, s.PixelY, WizardPixelX, WizardPixelY, Config.HitDistance))
                {
                    s.Owner.Score += Rules.WizardPoints * ScoreMultiplier();
                    AddExplosion(WizardPixelX, WizardPixelY, SpriteLibrary.Blue);
                    s.IsAlive = false;
                    EndWizardStage();
                    continue;
                }

                // Hit the other worrior? (bonus points, he loses a life)
                foreach (Worrior other in worriors)
                    if (other != s.Owner && other.IsActive
                        && IsNear(s.PixelX, s.PixelY, other.PixelX, other.PixelY, Config.HitDistance))
                    {
                        s.Owner.Score += Rules.WorriorKillPoints * ScoreMultiplier();
                        KillWorrior(other);
                        s.IsAlive = false;
                        break;
                    }
            }
        }

        private void EnemyBoltsHitWorriors()
        {
            foreach (Shot s in shots)
            {
                if (!s.IsAlive || !s.IsEnemyBolt) continue;
                foreach (Worrior w in worriors)
                    if (w.IsActive && IsNear(s.PixelX, s.PixelY, w.PixelX, w.PixelY, Config.HitDistance - 1))
                    {
                        KillWorrior(w);
                        s.IsAlive = false;
                        break;
                    }
            }
        }

        /// <summary>Touching a monster (or the Wizard) is deadly.</summary>
        private void CreaturesTouchWorriors()
        {
            foreach (Worrior w in worriors)
            {
                if (!w.IsActive) continue;

                foreach (Monster m in monsters)
                    if (IsNear(m.PixelX, m.PixelY, w.PixelX, w.PixelY, Config.HitDistance))
                    {
                        KillWorrior(w);
                        break;
                    }

                if (w.IsActive && wizardActive
                    && IsNear(WizardPixelX, WizardPixelY, w.PixelX, w.PixelY, Config.HitDistance))
                    KillWorrior(w);
            }
        }

        private int ScoreMultiplier() { return doubleScoreActive ? 2 : 1; }

        /// <summary>
        /// Removes a monster, awards points and applies the replacement rules:
        /// Burwor -> Garwor (depending on dungeon), Garwor -> Thorwor.
        /// </summary>
        private void KillMonster(Monster dead, Worrior killer)
        {
            monsters.Remove(dead);
            if (killer != null) killer.Score += Rules.PointsFor(dead.Kind) * ScoreMultiplier();
            AddExplosion(dead.PixelX, dead.PixelY, SpriteLibrary.Orange);

            switch (dead.Kind)
            {
                case MonsterKind.Burwor:
                    // The LAST few Burwors are replaced: 1 in dungeon 1, 2 in dungeon 2 ...
                    int burworsLeft = CountMonsters(MonsterKind.Burwor);
                    if (burworsLeft < Rules.GarworsInDungeon(dungeon))
                        SpawnMonster(MonsterKind.Garwor, dead.Speed);
                    break;

                case MonsterKind.Garwor:
                    SpawnMonster(MonsterKind.Thorwor, dead.Speed);
                    break;

                case MonsterKind.Worluk:
                    doubleScoreNext = true;
                    ShowMessage("DOUBLE SCORE", 2.5f);
                    break;
            }
        }

        private int CountMonsters(MonsterKind kind)
        {
            int count = 0;
            foreach (Monster m in monsters) if (m.Kind == kind) count++;
            return count;
        }

        private void KillWorrior(Worrior w)
        {
            if (!w.IsActive) return;
            w.State = WorriorState.Dying;
            w.DeathTimer = 1.2f;
            w.Lives--;
            AddExplosion(w.PixelX, w.PixelY, w.BodyColor);

            // The Wizard keeps teleporting until one worrior has been destroyed.
            if (stage == Stage.Wizard) EndWizardStage();
        }

        // =============================================================================
        //  DUNGEON FLOW
        // =============================================================================
        private void UpdateStage(float dt)
        {
            // While the Worluk is out, the side doors never close.
            maze.ForceDoorsOpen = (stage == Stage.Worluk);

            switch (stage)
            {
                case Stage.Monsters:
                    if (monsters.Count == 0)
                    {
                        if (dungeon >= 2) StartWorlukStage();   // no Worluk in dungeon 1
                        else DecideAboutWizard();
                    }
                    break;

                case Stage.Worluk:
                    // Over once the Worluk was shot or has escaped.
                    if (monsters.Count == 0) DecideAboutWizard();
                    break;

                case Stage.Wizard:
                    break;   // ends when the Wizard is shot or a worrior dies

                case Stage.Finished:
                    stageTimer += dt;
                    if (stageTimer > 2.5f) StartDungeon(dungeon + 1);
                    break;
            }
        }

        private void StartWorlukStage()
        {
            stage = Stage.Worluk;
            Monster worluk = SpawnMonster(MonsterKind.Worluk, Config.WorlukSpeed);

            // Put it near the middle and let it fly to the opposite side's door.
            worluk.PlaceAtCell(3 + rng.Next(5), 1 + rng.Next(4));
            worluk.TargetDoorSide = worluk.Col < Config.Cols / 2 ? 1 : (worluk.Col > Config.Cols / 2 ? -1 : (rng.Next(2) * 2 - 1));
            maze.ForceDoorsOpen = true;
        }

        /// <summary>After the monsters/Worluk, the Wizard shows up more often in deeper dungeons.</summary>
        private void DecideAboutWizard()
        {
            float chance = Math.Min(0.85f, 0.25f + 0.04f * dungeon);
            if (rng.NextDouble() < chance)
            {
                StartWizard();
            }
            else
            {
                stage = Stage.Finished;
                stageTimer = 0f;
            }
        }

        private void CheckForGameOver()
        {
            bool allPlayersOut = twoPlayers
                ? (yellow.State == WorriorState.Out && blue.State == WorriorState.Out)
                : (yellow.State == WorriorState.Out);

            if (!allPlayersOut) return;
            state = GameState.GameOver;
            gameOverTimer = 6f;
        }

        // =============================================================================
        //  EFFECTS
        // =============================================================================
        private void AddExplosion(int pixelX, int pixelY, Color color)
        {
            effects.Add(new Effect { X = pixelX, Y = pixelY, Color = color });
        }

        private void UpdateEffects(float dt)
        {
            foreach (Effect e in effects) e.Age += dt;
            effects.RemoveAll(e => e.Age >= Effect.Life);
        }

        // =============================================================================
        //  DRAWING
        // =============================================================================
        protected override void Draw(GameTime gameTime)
        {
            // 1. Draw the whole game at the small virtual resolution...
            GraphicsDevice.SetRenderTarget(canvas);
            GraphicsDevice.Clear(Color.Black);
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            DrawStars();
            if (state == GameState.Title) DrawTitleScreen();
            else DrawGameScreen();

            spriteBatch.End();

            // 2. ...then stretch it to the window, keeping the aspect ratio (black bars).
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.Black);

            int windowWidth = GraphicsDevice.PresentationParameters.BackBufferWidth;
            int windowHeight = GraphicsDevice.PresentationParameters.BackBufferHeight;
            float scale = Math.Min(windowWidth / (float)Config.ScreenWidth, windowHeight / (float)Config.ScreenHeight);
            int width = (int)(Config.ScreenWidth * scale);
            int height = (int)(Config.ScreenHeight * scale);
            var destination = new Rectangle((windowWidth - width) / 2, (windowHeight - height) / 2, width, height);

            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            spriteBatch.Draw(canvas, destination, Color.White);
            spriteBatch.End();

            base.Draw(gameTime);
        }

        // ----------------------------------------------------------------- small helpers
        private void FillRect(int x, int y, int width, int height, Color color)
        {
            spriteBatch.Draw(pixel, new Rectangle(x, y, width, height), color);
        }

        /// <summary>Draws a sprite centred on a screen position.</summary>
        private void DrawSprite(Texture2D texture, int centerX, int centerY, bool flipHorizontally = false,
                                float rotation = 0f, int scale = 1)
        {
            var origin = new Vector2(texture.Width / 2f, texture.Height / 2f);
            SpriteEffects effects = flipHorizontally ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            spriteBatch.Draw(texture, new Vector2(centerX, centerY), null, Color.White, rotation, origin, scale, effects, 0f);
        }

        private void DrawStars()
        {
            for (int i = 0; i < stars.Count; i++)
            {
                int brightness = 70 + (i * 37) % 120;
                FillRect(stars[i].X, stars[i].Y, 1, 1, new Color(brightness, brightness, brightness));
            }
        }

        // ----------------------------------------------------------------- title screen
        private void DrawTitleScreen()
        {
            font.DrawCentered(spriteBatch, "WIZARD OF WOR", Config.ScreenWidth / 2, 24, SpriteLibrary.Yellow, 3);
            font.DrawCentered(spriteBatch, "1980 MIDWAY", Config.ScreenWidth / 2, 54,
                              new Color(150, 150, 150));

            // Meet the monsters.
            DrawTitleMonster(sprites.Burwor, "BURWOR", "100", 84, SpriteLibrary.Blue);
            DrawTitleMonster(sprites.Garwor, "GARWOR", "200", 100, SpriteLibrary.Yellow);
            DrawTitleMonster(sprites.Thorwor, "THORWOR", "500", 116, SpriteLibrary.Red);
            DrawTitleMonster(sprites.Worluk, "WORLUK", "1000", 132, SpriteLibrary.Orange);
            DrawTitleMonster(sprites.Wizard, "WIZARD OF WOR", "2500", 148, SpriteLibrary.Blue);

            font.DrawCentered(spriteBatch, "PRESS 1 - ONE PLAYER", Config.ScreenWidth / 2, 172, Color.White);
            font.DrawCentered(spriteBatch, "PRESS 2 - TWO PLAYERS", Config.ScreenWidth / 2, 183, Color.White);
            font.DrawCentered(spriteBatch, "P1 YELLOW: ARROWS + SPACE   P2 BLUE: WASD + Q", Config.ScreenWidth / 2, 197,
                              new Color(150, 150, 150));
            font.DrawCentered(spriteBatch, "F11 FULLSCREEN   ESC QUIT", Config.ScreenWidth / 2, 206,
                              new Color(150, 150, 150));
        }

        private void DrawTitleMonster(Texture2D[] frames, string name, string points, int y, Color color)
        {
            int frame = (int)(clock * 4) % 2;
            DrawSprite(frames[frame], 100, y + 4, false, 0f, 1);
            font.Draw(spriteBatch, name, 116, y, color);
            font.Draw(spriteBatch, points, 216, y, Color.White);
        }

        // ----------------------------------------------------------------- game screen
        private void DrawGameScreen()
        {
            DrawMaze();
            DrawDoorStubs();
            DrawShots();
            DrawMonsters();
            DrawWizard();
            DrawWorriors();
            DrawEffects();
            DrawHud();

            if (state == GameState.GameOver)
            {
                FillRect(60, 70, 200, 40, Color.Black);
                font.DrawCentered(spriteBatch, "GAME OVER", Config.ScreenWidth / 2, 80, SpriteLibrary.Red, 3);
                font.DrawCentered(spriteBatch, "PRESS ENTER", Config.ScreenWidth / 2, 100, Color.White);
            }
        }

        private Color WallColor()
        {
            Color[] colors =
            {
                new Color(60, 100, 255),    // blue
                new Color(250, 70, 40),     // red
                new Color(40, 200, 90),     // green
                new Color(255, 160, 30),    // orange
            };
            return colors[(dungeon - 1) % colors.Length];
        }

        private void DrawMaze()
        {
            Color wall = WallColor();
            int cell = Config.CellSize;
            int left = Config.MazeX, top = Config.MazeY;

            // Inner walls
            for (int r = 0; r < Config.Rows; r++)
                for (int c = 0; c < Config.Cols; c++)
                {
                    int x = left + c * cell, y = top + r * cell;
                    if (c < Config.Cols - 1 && maze.HasWall(c, r, Direction.Right))
                        FillRect(x + cell - 1, y, 2, cell + 1, wall);
                    if (r < Config.Rows - 1 && maze.HasWall(c, r, Direction.Down))
                        FillRect(x, y + cell - 1, cell + 1, 2, wall);
                }

            // Outer border: top and bottom, then left and right with a gap for the doors.
            FillRect(left - 1, top - 1, Config.MazeWidth + 2, 2, wall);
            FillRect(left - 1, top + Config.MazeHeight - 1, Config.MazeWidth + 2, 2, wall);

            int doorTop = top + Config.DoorRow * cell;
            int doorBottom = doorTop + cell;
            int rightEdge = left + Config.MazeWidth - 1;

            FillRect(left - 1, top - 1, 2, doorTop - top + 2, wall);
            FillRect(left - 1, doorBottom, 2, top + Config.MazeHeight - doorBottom + 1, wall);
            FillRect(rightEdge, top - 1, 2, doorTop - top + 2, wall);
            FillRect(rightEdge, doorBottom, 2, top + Config.MazeHeight - doorBottom + 1, wall);

            // A closed door is drawn as a dim barrier.
            if (!maze.DoorsOpen)
            {
                var barrier = new Color(120, 30, 20);
                FillRect(left - 1, doorTop, 2, cell, barrier);
                FillRect(rightEdge, doorTop, 2, cell, barrier);
            }
        }

        private void DrawDoorStubs()
        {
            Color wall = WallColor();
            int doorTop = Config.MazeY + Config.DoorRow * Config.CellSize;
            int doorBottom = doorTop + Config.CellSize;
            int length = Config.DoorStubLength;

            int leftX = Config.MazeX - length;
            int rightX = Config.MazeX + Config.MazeWidth;

            foreach (int x in new[] { leftX, rightX })
            {
                FillRect(x, doorTop - 1, length, 2, wall);
                FillRect(x, doorBottom - 1, length, 2, wall);
            }

            // Arrows showing that the two doors are connected.
            Color arrow = maze.DoorsOpen ? SpriteLibrary.Red : new Color(90, 40, 30);
            int arrowY = doorTop + Config.CellSize / 2 - 3;
            font.Draw(spriteBatch, "<", leftX + 3, arrowY, arrow);
            font.Draw(spriteBatch, ">", rightX + length - 9, arrowY, arrow);
        }

        private void DrawShots()
        {
            foreach (Shot s in shots)
            {
                int x = Config.MazeX + s.PixelX, y = Config.MazeY + s.PixelY;
                if (s.IsEnemyBolt)
                {
                    // Enemy bolts flicker white / red.
                    Color c = ((int)(clock * 30) % 2 == 0) ? Color.White : SpriteLibrary.Red;
                    if (s.Direction.IsHorizontal()) FillRect(x - 3, y - 1, 6, 2, c);
                    else FillRect(x - 1, y - 3, 2, 6, c);
                }
                else
                {
                    if (s.Direction.IsHorizontal()) FillRect(x - 2, y - 1, 4, 2, Color.White);
                    else FillRect(x - 1, y - 2, 2, 4, Color.White);
                }
            }
        }

        private void DrawMonsters()
        {
            int frame = (int)(clock * 6) % 2;
            foreach (Monster m in monsters)
            {
                if (!IsMonsterVisible(m)) continue;
                Texture2D texture = sprites.ForMonster(m.Kind)[frame];
                bool facingLeft = m.MoveDir == Direction.Left;
                DrawSprite(texture, Config.MazeX + m.PixelX, Config.MazeY + m.PixelY, facingLeft);
            }
        }

        private void DrawWizard()
        {
            if (!wizardActive) return;
            int frame = (int)(clock * 8) % 2;
            DrawSprite(sprites.Wizard[frame], Config.MazeX + WizardPixelX, Config.MazeY + WizardPixelY);
        }

        private void DrawWorriors()
        {
            foreach (Worrior w in worriors)
            {
                if (!w.IsActive) continue;

                int frame = w.IsMoving ? (int)(clock * 8) % 2 : 0;
                Texture2D texture = sprites.ForWorrior(w)[frame];
                float rotation = 0f;
                bool flip = false;

                switch (w.Facing)
                {
                    case Direction.Up: rotation = -MathHelper.PiOver2; break;
                    case Direction.Down: rotation = MathHelper.PiOver2; break;
                    case Direction.Left: flip = true; break;
                }
                DrawSprite(texture, Config.MazeX + w.PixelX, Config.MazeY + w.PixelY, flip, rotation);
            }
        }

        private void DrawEffects()
        {
            foreach (Effect e in effects)
            {
                float progress = e.Age / Effect.Life;
                int radius = (int)(2 + progress * 12);
                int x = Config.MazeX + e.X, y = Config.MazeY + e.Y;
                Color color = progress < 0.5f ? Color.White : e.Color;

                for (int i = 0; i < 8; i++)
                {
                    double angle = i * Math.PI / 4;
                    int px = x + (int)(Math.Cos(angle) * radius);
                    int py = y + (int)(Math.Sin(angle) * radius);
                    FillRect(px - 1, py - 1, 2, 2, color);
                }
            }
        }

        // ----------------------------------------------------------------- HUD (boxes, radar, scores, message)
        private void DrawHud()
        {
            DrawMessage();
            DrawRadar();
            DrawWorriorHud(blue, true);
            DrawWorriorHud(yellow, false);
        }

        private void DrawMessage()
        {
            string text;
            Color color = SpriteLibrary.Red;

            if (overrideMessageTimer > 0f || (wizardActive && overrideMessage == "WIZARD OF WOR"))
            {
                text = overrideMessage;
                color = text == "WIZARD OF WOR" ? new Color(110, 150, 255) : SpriteLibrary.Yellow;
            }
            else if (stage == Stage.Worluk)
            {
                text = "WORLUK";
                color = SpriteLibrary.Yellow;
            }
            else if (dungeon == 1) text = "RADAR";
            else if (Rules.IsPit(dungeon)) text = "THE PIT";
            else if (Rules.IsArena(dungeon)) text = "THE ARENA";
            else text = "DUNGEON " + dungeon;

            int y = Config.MazeY + Config.MazeHeight + 6;
            font.DrawCentered(spriteBatch, text, Config.ScreenWidth / 2, y, color);
        }

        private void DrawRadar()
        {
            const int radarWidth = 88, radarHeight = 48;
            int x = (Config.ScreenWidth - radarWidth) / 2;
            int y = Config.ScreenHeight - radarHeight - 6;

            Color border = SpriteLibrary.Blue;
            FillRect(x - 1, y - 1, radarWidth + 2, radarHeight + 2, border);
            FillRect(x, y, radarWidth, radarHeight, Color.Black);

            // The radar shows every monster, visible or not (but not worriors or the Wizard).
            foreach (Monster m in monsters)
            {
                int dotX = x + m.PixelX * radarWidth / Config.MazeWidth;
                int dotY = y + m.PixelY * radarHeight / Config.MazeHeight;
                dotX = Math.Max(x + 1, Math.Min(x + radarWidth - 3, dotX));
                Color dot = m.Kind == MonsterKind.Worluk ? SpriteLibrary.Yellow : new Color(150, 180, 255);
                FillRect(dotX, dotY, 3, 3, dot);
            }
        }

        private void DrawWorriorHud(Worrior w, bool leftSide)
        {
            if (w == null) return;

            const int boxSize = 20;
            int boxX = leftSide ? Config.MazeX : Config.MazeX + Config.MazeWidth - boxSize;
            int boxY = Config.MazeY + Config.MazeHeight + 3;
            Color color = w.BodyColor;

            // The ready box. Its lid (top line) is open while the worrior may walk out.
            bool open = w.State == WorriorState.InBox;
            FillRect(boxX, boxY, 2, boxSize, color);
            FillRect(boxX + boxSize - 2, boxY, 2, boxSize, color);
            FillRect(boxX, boxY + boxSize - 2, boxSize, 2, color);
            if (!open) FillRect(boxX, boxY, boxSize, 2, color);

            // A backup worrior stands in the box.
            bool backupInBox = w.State == WorriorState.InBox
                || (w.State == WorriorState.Active && w.Lives >= 2)
                || (w.State == WorriorState.Dying && w.Lives >= 1);
            if (backupInBox)
                DrawSprite(sprites.ForWorrior(w)[0], boxX + boxSize / 2, boxY + boxSize / 2 + 1, false, -MathHelper.PiOver2);

            // Remaining reserve worriors, as little icons beside the box.
            int reserve = w.State == WorriorState.Active ? w.Lives - 2 : w.Lives - 1;
            reserve = Math.Max(0, Math.Min(9, reserve));
            Texture2D mini = w.PlayerNumber == 1 ? sprites.MiniYellow : sprites.MiniBlue;
            for (int i = 0; i < reserve; i++)
            {
                int iconX = leftSide ? 4 + (i % 3) * 10 : 285 + (i % 3) * 10;
                int iconY = boxY + 1 + (i / 3) * 10;
                spriteBatch.Draw(mini, new Vector2(iconX, iconY), Color.White);
            }

            // The 10 second countdown next to the open box.
            if (w.State == WorriorState.InBox)
            {
                string count = ((int)Math.Ceiling(w.BoxTimer)).ToString();
                int textX = leftSide ? boxX + boxSize + 5 : boxX - 5 - font.Measure(count);
                font.Draw(spriteBatch, count, textX, boxY + 6, color);
            }

            // Score box in the bottom corner.
            const int scoreWidth = 44, scoreHeight = 22;
            int scoreX = leftSide ? 8 : Config.ScreenWidth - 8 - scoreWidth;
            int scoreY = Config.ScreenHeight - scoreHeight - 6;
            FillRect(scoreX, scoreY, scoreWidth, scoreHeight, color);
            Color textColor = w.PlayerNumber == 1 ? Color.Black : Color.White;
            string scoreText = w.Score.ToString();
            font.Draw(spriteBatch, scoreText, scoreX + scoreWidth - 4 - font.Measure(scoreText), scoreY + 8, textColor);
        }
    }
}