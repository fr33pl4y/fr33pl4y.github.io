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

// ArcadeBitmapFont.cs
//
// A tiny, dependency-free, hand-drawn pixel/bitmap font library for MonoGame.
// No .spritefont / Content Pipeline font is used at all: every glyph is a small
// hand-authored grid of on/off pixels, drawn at runtime as scaled 1x1 rectangles.
// That's the classic "arcade" look — chunky, blocky, monospaced letters.
//
// USAGE
// -----
// 1. Drop this file into your MonoGame project (no Content Pipeline step needed).
// 2. In Game.LoadContent(): ArcadeFonts.LoadContent(GraphicsDevice);
// 3. In Game.Draw(), inside a SpriteBatch.Begin/End block:
//
//      ArcadeFonts.Small.DrawString(spriteBatch, "HELLO WORLD", new Vector2(10, 10), Color.White);
//      ArcadeFonts.Medium.DrawString(spriteBatch, "SCORE: 001200", new Vector2(10, 40), Color.Yellow, scale: 2);
//      ArcadeFonts.Large.DrawStringShadow(spriteBatch, "GAME OVER", new Vector2(10, 90), Color.Red, Color.Black, new Vector2(2, 2), scale: 3);
//
// Only uppercase A-Z, digits 0-9, space and a handful of punctuation
// ( . , ! ? : ' - ) are defined. Lowercase input is automatically
// upper-cased before lookup, since real arcade cabinets never had lowercase.
// Unknown characters are skipped (but still advance the cursor).

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ArcadeBitmapFont
{
    /// <summary>The three built-in hand-drawn font sizes.</summary>
    public enum ArcadeFontSize
    {
        Small,
        Medium,
        Large
    }

    /// <summary>
    /// A single hand-drawn bitmap font. Fixed-width (monospaced) glyphs, drawn as
    /// scaled solid rectangles using a shared 1x1 white pixel texture.
    /// </summary>
    public sealed class ArcadeFont
    {
        private readonly Dictionary<char, string[]> _glyphs;

        /// <summary>Width of a single glyph, in font "pixels" (before scaling).</summary>
        public int GlyphWidth { get; }

        /// <summary>Height of a single glyph, in font "pixels" (before scaling).</summary>
        public int GlyphHeight { get; }

        internal ArcadeFont(Dictionary<char, string[]> glyphs, int glyphWidth, int glyphHeight)
        {
            _glyphs = glyphs;
            GlyphWidth = glyphWidth;
            GlyphHeight = glyphHeight;
        }

        /// <summary>
        /// Measures the size, in screen pixels, that <paramref name="text"/> would occupy
        /// when drawn with the given scale/spacing. Supports '\n' for multiple lines.
        /// </summary>
        public Vector2 MeasureString(string text, int scale = 1, int spacing = 1)
        {
            if (string.IsNullOrEmpty(text)) return Vector2.Zero;

            int cellW = (GlyphWidth + spacing) * scale;
            int lineH = (GlyphHeight + spacing) * scale;

            string[] lines = text.Split('\n');
            int maxWidth = 0;
            foreach (string line in lines)
            {
                int w = Math.Max(0, line.Length * cellW - spacing * scale);
                if (w > maxWidth) maxWidth = w;
            }

            int height = Math.Max(0, lines.Length * lineH - spacing * scale);
            return new Vector2(maxWidth, height);
        }

        /// <summary>Draws hand-drawn bitmap text at the given position.</summary>
        /// <param name="scale">How many screen pixels each font "pixel" occupies. 1 = native size.</param>
        /// <param name="spacing">Gap, in font pixels, between glyphs (horizontally) and lines (vertically).</param>
        public void DrawString(SpriteBatch spriteBatch, string text, Vector2 position, Color color, int scale = 1, int spacing = 1)
        {
            DrawInternal(spriteBatch, text, position, color, scale, spacing);
        }

        /// <summary>Convenience helper that draws a drop-shadow copy behind the main text.</summary>
        public void DrawStringShadow(SpriteBatch spriteBatch, string text, Vector2 position, Color color,
            Color shadowColor, Vector2 shadowOffset, int scale = 1, int spacing = 1)
        {
            DrawInternal(spriteBatch, text, position + shadowOffset, shadowColor, scale, spacing);
            DrawInternal(spriteBatch, text, position, color, scale, spacing);
        }

        private void DrawInternal(SpriteBatch spriteBatch, string text, Vector2 position, Color color, int scale, int spacing)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (scale < 1) scale = 1;

            Texture2D pixel = ArcadeFonts.Pixel;
            int cellW = (GlyphWidth + spacing) * scale;
            int lineH = (GlyphHeight + spacing) * scale;

            float cursorX = position.X;
            float cursorY = position.Y;

            foreach (char raw in text)
            {
                if (raw == '\n')
                {
                    cursorX = position.X;
                    cursorY += lineH;
                    continue;
                }

                char c = char.ToUpperInvariant(raw);
                if (_glyphs.TryGetValue(c, out string[] rows))
                {
                    for (int y = 0; y < rows.Length; y++)
                    {
                        string row = rows[y];
                        for (int x = 0; x < row.Length; x++)
                        {
                            if (row[x] == '#')
                            {
                                var rect = new Rectangle(
                                    (int)Math.Round(cursorX) + x * scale,
                                    (int)Math.Round(cursorY) + y * scale,
                                    scale,
                                    scale);
                                spriteBatch.Draw(pixel, rect, color);
                            }
                        }
                    }
                }

                cursorX += cellW;
            }
        }
    }

    /// <summary>
    /// Static access point for the three built-in hand-drawn arcade fonts.
    /// Call <see cref="LoadContent"/> once, from Game.LoadContent(), before drawing.
    /// </summary>
    public static class ArcadeFonts
    {
        private static Texture2D _pixel;

        /// <summary>3x5 pixel font. Best for dense HUD text, tiny labels.</summary>
        public static ArcadeFont Small { get; }

        /// <summary>5x7 pixel font. General purpose arcade text, scores, menus.</summary>
        public static ArcadeFont Medium { get; }

        /// <summary>7x9 pixel font. Titles, "GAME OVER", big banners.</summary>
        public static ArcadeFont Large { get; }

        static ArcadeFonts()
        {
            Small = new ArcadeFont(ParseFont(SmallFontData, 5), 3, 5);
            Medium = new ArcadeFont(ParseFont(MediumFontData, 7), 5, 7);
            Large = new ArcadeFont(ParseFont(LargeFontData, 9), 7, 9);
        }

        /// <summary>Creates the shared 1x1 pixel texture used to draw every glyph. Call once at startup.</summary>
        public static void LoadContent(GraphicsDevice graphicsDevice)
        {
            if (_pixel == null || _pixel.IsDisposed)
            {
                _pixel = new Texture2D(graphicsDevice, 1, 1);
                _pixel.SetData(new[] { Color.White });
            }
        }

        /// <summary>Returns the font matching the given size enum.</summary>
        public static ArcadeFont Get(ArcadeFontSize size)
        {
            switch (size)
            {
                case ArcadeFontSize.Small: return Small;
                case ArcadeFontSize.Medium: return Medium;
                case ArcadeFontSize.Large: return Large;
                default: throw new ArgumentOutOfRangeException(nameof(size));
            }
        }

        internal static Texture2D Pixel =>
            _pixel ?? throw new InvalidOperationException(
                "ArcadeFonts.LoadContent(GraphicsDevice) must be called before drawing (e.g. in Game.LoadContent()).");

        // ------------------------------------------------------------------
        // Glyph data parsing
        // ------------------------------------------------------------------
        // Each font is authored as plain text: a line with the character,
        // followed by exactly `height` lines of '#' (pixel on) and '.' (pixel
        // off), with a blank line between glyphs. The literal word SPACE is
        // used as the label for the space character, since a blank label line
        // would otherwise be mistaken for a separator.
        private static Dictionary<char, string[]> ParseFont(string raw, int height)
        {
            var dict = new Dictionary<char, string[]>();
            string[] lines = raw.Replace("\r\n", "\n").Split('\n');

            int i = 0;
            while (i < lines.Length)
            {
                string label = lines[i].Trim();
                i++;

                if (label.Length == 0) continue;

                char symbol = label == "SPACE" ? ' ' : label[0];
                var rows = new string[height];
                for (int r = 0; r < height; r++)
                {
                    rows[r] = i < lines.Length ? lines[i] : string.Empty;
                    i++;
                }

                dict[symbol] = rows;
            }

            return dict;
        }

        // ------------------------------------------------------------------
        // SMALL FONT — 3 wide x 5 tall
        // ------------------------------------------------------------------
        private const string SmallFontData = @"
A
.#.
#.#
###
#.#
#.#

B
##.
#.#
##.
#.#
##.

C
.##
#..
#..
#..
.##

D
##.
#.#
#.#
#.#
##.

E
###
#..
##.
#..
###

F
###
#..
##.
#..
#..

G
.##
#..
#.#
#.#
.##

H
#.#
#.#
###
#.#
#.#

I
###
.#.
.#.
.#.
###

J
..#
..#
..#
#.#
.#.

K
#.#
#.#
##.
#.#
#.#

L
#..
#..
#..
#..
###

M
#.#
###
###
#.#
#.#

N
#.#
###
###
###
#.#

O
.#.
#.#
#.#
#.#
.#.

P
##.
#.#
##.
#..
#..

Q
.#.
#.#
#.#
.#.
..#

R
##.
#.#
##.
#.#
#.#

S
.##
#..
.#.
..#
##.

T
###
.#.
.#.
.#.
.#.

U
#.#
#.#
#.#
#.#
.#.

V
#.#
#.#
#.#
.#.
.#.

W
#.#
#.#
#.#
###
#.#

X
#.#
#.#
.#.
#.#
#.#

Y
#.#
#.#
.#.
.#.
.#.

Z
###
..#
.#.
#..
###

0
.#.
#.#
#.#
#.#
.#.

1
.#.
##.
.#.
.#.
###

2
##.
..#
.#.
#..
###

3
##.
..#
.#.
..#
##.

4
#.#
#.#
###
..#
..#

5
###
#..
##.
..#
##.

6
.##
#..
##.
#.#
.#.

7
###
..#
.#.
.#.
.#.

8
.#.
#.#
.#.
#.#
.#.

9
.#.
#.#
.##
..#
.#.

SPACE
...
...
...
...
...

.
...
...
...
...
.#.

,
...
...
...
.#.
#..

!
.#.
.#.
.#.
...
.#.

?
##.
..#
.#.
...
.#.

:
...
.#.
...
.#.
...

'
.#.
.#.
...
...
...

-
...
...
###
...
...
";

        // ------------------------------------------------------------------
        // MEDIUM FONT — 5 wide x 7 tall
        // ------------------------------------------------------------------
        private const string MediumFontData = @"
A
.###.
#...#
#...#
#####
#...#
#...#
#...#

B
####.
#...#
#...#
####.
#...#
#...#
####.

C
.####
#....
#....
#....
#....
#....
.####

D
####.
#...#
#...#
#...#
#...#
#...#
####.

E
#####
#....
#....
####.
#....
#....
#####

F
#####
#....
#....
####.
#....
#....
#....

G
.####
#....
#....
#.###
#...#
#...#
.####

H
#...#
#...#
#...#
#####
#...#
#...#
#...#

I
#####
..#..
..#..
..#..
..#..
..#..
#####

J
..###
...#.
...#.
...#.
...#.
#..#.
.##..

K
#...#
#..#.
#.#..
##...
#.#..
#..#.
#...#

L
#....
#....
#....
#....
#....
#....
#####

M
#...#
##.##
#.#.#
#...#
#...#
#...#
#...#

N
#...#
##..#
#.#.#
#..##
#...#
#...#
#...#

O
.###.
#...#
#...#
#...#
#...#
#...#
.###.

P
####.
#...#
#...#
####.
#....
#....
#....

Q
.###.
#...#
#...#
#...#
#.#.#
#..#.
.##.#

R
####.
#...#
#...#
####.
#.#..
#..#.
#...#

S
.####
#....
#....
.###.
....#
....#
####.

T
#####
..#..
..#..
..#..
..#..
..#..
..#..

U
#...#
#...#
#...#
#...#
#...#
#...#
.###.

V
#...#
#...#
#...#
#...#
#...#
.#.#.
..#..

W
#...#
#...#
#...#
#.#.#
#.#.#
##.##
#...#

X
#...#
#...#
.#.#.
..#..
.#.#.
#...#
#...#

Y
#...#
#...#
.#.#.
..#..
..#..
..#..
..#..

Z
#####
....#
...#.
..#..
.#...
#....
#####

0
.###.
#...#
#..##
#.#.#
##..#
#...#
.###.

1
..#..
.##..
..#..
..#..
..#..
..#..
.###.

2
.###.
#...#
....#
...#.
..#..
.#...
#####

3
.###.
#...#
....#
..##.
....#
#...#
.###.

4
...#.
..##.
.#.#.
#..#.
#####
...#.
...#.

5
#####
#....
####.
....#
....#
#...#
.###.

6
..##.
.#...
#....
####.
#...#
#...#
.###.

7
#####
....#
...#.
..#..
.#...
.#...
.#...

8
.###.
#...#
#...#
.###.
#...#
#...#
.###.

9
.###.
#...#
#...#
.####
....#
...#.
.##..

SPACE
.....
.....
.....
.....
.....
.....
.....

.
.....
.....
.....
.....
.....
..##.
..##.

,
.....
.....
.....
.....
..##.
..##.
.#...

!
..#..
..#..
..#..
..#..
..#..
.....
..#..

?
.###.
#...#
....#
...#.
..#..
.....
..#..

:
.....
..##.
..##.
.....
..##.
..##.
.....

'
..#..
..#..
.....
.....
.....
.....
.....

-
.....
.....
.....
#####
.....
.....
.....
";

        // ------------------------------------------------------------------
        // LARGE FONT — 7 wide x 9 tall
        // ------------------------------------------------------------------
        private const string LargeFontData = @"
A
..###..
.#...#.
#.....#
#.....#
#######
#.....#
#.....#
#.....#
#.....#

B
######.
#.....#
#.....#
######.
#.....#
#.....#
#.....#
#.....#
######.

C
.#####.
#.....#
#......
#......
#......
#......
#......
#.....#
.#####.

D
######.
#.....#
#.....#
#.....#
#.....#
#.....#
#.....#
#.....#
######.

E
#######
#......
#......
#......
#####..
#......
#......
#......
#######

F
#######
#......
#......
#......
#####..
#......
#......
#......
#......

G
.#####.
#.....#
#......
#......
#..####
#.....#
#.....#
#.....#
.#####.

H
#.....#
#.....#
#.....#
#.....#
#######
#.....#
#.....#
#.....#
#.....#

I
#######
...#...
...#...
...#...
...#...
...#...
...#...
...#...
#######

J
....###
.....#.
.....#.
.....#.
.....#.
.....#.
#....#.
#....#.
.####..

K
#.....#
#....#.
#...#..
#..#...
###....
#..#...
#...#..
#....#.
#.....#

L
#......
#......
#......
#......
#......
#......
#......
#......
#######

M
#.....#
##...##
#.#.#.#
#..#..#
#.....#
#.....#
#.....#
#.....#
#.....#

N
#.....#
##....#
#.#...#
#..#..#
#...#.#
#....##
#.....#
#.....#
#.....#

O
.#####.
#.....#
#.....#
#.....#
#.....#
#.....#
#.....#
#.....#
.#####.

P
######.
#.....#
#.....#
#.....#
######.
#......
#......
#......
#......

Q
.#####.
#.....#
#.....#
#.....#
#.....#
#...#.#
#....#.
#.....#
.#####.

R
######.
#.....#
#.....#
#.....#
######.
#...#..
#....#.
#.....#
#.....#

S
.#####.
#.....#
#......
#......
.#####.
......#
......#
#.....#
.#####.

T
#######
...#...
...#...
...#...
...#...
...#...
...#...
...#...
...#...

U
#.....#
#.....#
#.....#
#.....#
#.....#
#.....#
#.....#
#.....#
.#####.

V
#.....#
#.....#
#.....#
#.....#
.#...#.
.#...#.
..#.#..
..#.#..
...#...

W
#.....#
#.....#
#.....#
#.....#
#..#..#
#.#.#.#
#.#.#.#
##...##
#.....#

X
#.....#
.#...#.
..#.#..
...#...
...#...
...#...
..#.#..
.#...#.
#.....#

Y
#.....#
.#...#.
..#.#..
...#...
...#...
...#...
...#...
...#...
...#...

Z
#######
......#
.....#.
....#..
...#...
..#....
.#.....
#......
#######

0
.#####.
#.....#
#....##
#...#.#
#..#..#
#.#...#
##....#
#.....#
.#####.

1
...#...
..##...
...#...
...#...
...#...
...#...
...#...
...#...
.#####.

2
.#####.
#.....#
......#
.....#.
....#..
...#...
..#....
.#.....
#######

3
.#####.
#.....#
......#
...###.
......#
......#
#.....#
#.....#
.#####.

4
....#..
...##..
..#.#..
.#..#..
#...#..
#######
....#..
....#..
....#..

5
#######
#......
#......
######.
......#
......#
......#
#.....#
.#####.

6
..####.
.#.....
#......
######.
#.....#
#.....#
#.....#
#.....#
.#####.

7
#######
......#
.....#.
....#..
...#...
..#....
..#....
..#....
..#....

8
.#####.
#.....#
#.....#
.#####.
#.....#
#.....#
#.....#
#.....#
.#####.

9
.#####.
#.....#
#.....#
#.....#
.######
......#
......#
.....#.
.####..

SPACE
.......
.......
.......
.......
.......
.......
.......
.......
.......

.
.......
.......
.......
.......
.......
.......
.......
..###..
..###..

,
.......
.......
.......
.......
.......
.......
..###..
..###..
...#...

!
...#...
...#...
...#...
...#...
...#...
...#...
...#...
.......
...#...

?
.#####.
#.....#
......#
.....#.
....#..
...#...
.......
...#...
...#...

:
.......
..###..
..###..
.......
.......
..###..
..###..
.......
.......

'
...#...
...#...
...#...
.......
.......
.......
.......
.......
.......

-
.......
.......
.......
.......
#######
.......
.......
.......
.......
";
    }
}
