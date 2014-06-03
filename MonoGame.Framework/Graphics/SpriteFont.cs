#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */

// Original code from SilverSprite Project
#endregion

#region Using Statements
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
#endregion

namespace Microsoft.Xna.Framework.Graphics
{
	// TODO: Rewrite from scratch. Just. No. -flibit
	public sealed class SpriteFont
	{
		#region Public Properties

		/// <summary>
		/// Gets a collection of the characters in the font.
		/// </summary>
		public ReadOnlyCollection<char> Characters
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets or sets the character that will be substituted when a
		/// given character is not included in the font.
		/// </summary>
		public char? DefaultCharacter
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the line spacing (the distance from baseline
		/// to baseline) of the font.
		/// </summary>
		public int LineSpacing
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the spacing (tracking) between characters in
		/// the font.
		/// </summary>
		public float Spacing
		{
			get;
			set;
		}

		/// <summary>
		/// Gets the texture that this SpriteFont draws from.
		/// THIS IS A MONOGAME EXTENSION!
		/// </summary>
		/// <remarks>Can be used to implement custom rendering of a SpriteFont</remarks>
		public Texture2D Texture
		{
			get
			{
				return _texture;
			}
		}

		#endregion

		#region Internal Readonly Variables

		private readonly Texture2D _texture;

		#endregion

		#region Private Readonly Variables

		private readonly Dictionary<char, Glyph> _glyphs;
#pragma warning disable 0414
		/* These are needed for Reflection purposes.
		 * Don't worry, SpriteFont's going to get rewritten like everything else
		 * in this damned library.
		 * -flibit
		 */
		private readonly List<Rectangle> _glyphBounds;
		private readonly List<Rectangle> _cropping;
		private readonly List<Vector3> _kerning;
		private readonly List<char> _characterMap;
#pragma warning restore 0414

		#endregion

		#region Internal Constructor

		internal SpriteFont(
			Texture2D texture,
			List<Rectangle> glyphBounds,
			List<Rectangle> cropping,
			List<char> characters,
			int lineSpacing,
			float spacing,
			List<Vector3> kerning,
			char? defaultCharacter
		) {
			Characters = new ReadOnlyCollection<char>(characters.ToArray());
			_texture = texture;
			_glyphBounds = glyphBounds;
			_cropping = cropping;
			_kerning = kerning;
			_characterMap = characters;
			LineSpacing = lineSpacing;
			Spacing = spacing;
			DefaultCharacter = defaultCharacter;

			_glyphs = new Dictionary<char, Glyph>(characters.Count, CharComparer.Default);

			for (int i = 0; i < characters.Count; i += 1)
			{
				Glyph glyph = new Glyph
				{
					BoundsInTexture = glyphBounds[i],
					Cropping = cropping[i],
					Character = characters[i],

					LeftSideBearing = kerning[i].X,
					Width = kerning[i].Y,
					RightSideBearing = kerning[i].Z,

					WidthIncludingBearings = kerning[i].X + kerning[i].Y + kerning[i].Z
				};
				_glyphs.Add(glyph.Character, glyph);
			}
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Returns the size of a string when rendered in this font.
		/// </summary>
		/// <param name="text">The text to measure.</param>
		/// <returns>The size, in pixels, of 'text' when rendered in
		/// this font.</returns>
		public Vector2 MeasureString(string text)
		{
			CharacterSource source = new CharacterSource(text);
			Vector2 size;
			MeasureString(ref source, out size);
			return size;
		}

		/// <summary>
		/// Returns the size of the contents of a StringBuilder when
		/// rendered in this font.
		/// </summary>
		/// <param name="text">The text to measure.</param>
		/// <returns>The size, in pixels, of 'text' when rendered in
		/// this font.</returns>
		public Vector2 MeasureString(StringBuilder text)
		{
			CharacterSource source = new CharacterSource(text);
			Vector2 size;
			MeasureString(ref source, out size);
			return size;
		}

		/// <summary>
		/// Returns a copy of the dictionary containing the glyphs in this SpriteFont.
		/// THIS IS A MONOGAME EXTENSION!
		/// </summary>
		/// <returns>A new Dictionary containing all of the glyphs inthis SpriteFont</returns>
		/// <remarks>Can be used to calculate character bounds when implementing custom SpriteFont rendering.</remarks>
		public Dictionary<char, Glyph> GetGlyphs()
		{
			return new Dictionary<char, Glyph>(_glyphs, _glyphs.Comparer);
		}

		#endregion

		#region Internal Methods

		internal void DrawInto(
			SpriteBatch spriteBatch,
			ref CharacterSource text,
			Vector2 position,
			Color color,
			float rotation,
			Vector2 origin,
			Vector2 scale,
			SpriteEffects effect,
			float depth
		) {
            this.DrawIntoInternal(spriteBatch, ref text, position, color, rotation, origin, scale, effect, depth);
		}

		#endregion

		#region Private Methods

		private void MeasureString(ref CharacterSource text, out Vector2 size)
		{
			if (text.Length == 0)
			{
				size = Vector2.Zero;
				return;
			}

			// Get the default glyph here once.
			Glyph? defaultGlyph = null;
			if (DefaultCharacter.HasValue)
			{
				defaultGlyph = _glyphs[DefaultCharacter.Value];
			}

			float width = 0.0f;
			float finalLineHeight = (float)LineSpacing;
			int fullLineCount = 0;
			Glyph currentGlyph = Glyph.Empty;
			Vector2 offset = Vector2.Zero;
			bool hasCurrentGlyph = false;
			bool firstGlyphOfLine = true;

			for (int i = 0; i < text.Length; i += 1)
			{
				char c = text[i];
				if (c == '\r')
				{
					hasCurrentGlyph = false;
					continue;
				}

				if (c == '\n')
				{
					fullLineCount += 1;
					finalLineHeight = LineSpacing;

					offset.X = 0;
					offset.Y = LineSpacing * fullLineCount;
					hasCurrentGlyph = false;
					firstGlyphOfLine = true;
					continue;
				}

				if (hasCurrentGlyph)
				{
					offset.X += Spacing;

					/* The first character on a line might have a negative left
					 * side bearing. In this scenario, SpriteBatch/SpriteFont
					 * normally offset the text to the right, so that text does
					 * not hang off the left side of its rectangle.
					 */
					if (firstGlyphOfLine)
					{
						offset.X = Math.Max(offset.X + Math.Abs(currentGlyph.LeftSideBearing), 0);
						firstGlyphOfLine = false;
					}
					else
					{
						offset.X += currentGlyph.LeftSideBearing;
					}

					offset.X += currentGlyph.Width + currentGlyph.RightSideBearing;
				}

				hasCurrentGlyph = _glyphs.TryGetValue(c, out currentGlyph);
				if (!hasCurrentGlyph)
				{
					if (!defaultGlyph.HasValue)
					{
						throw new ArgumentException(Errors.TextContainsUnresolvableCharacters, "text");
					}

					currentGlyph = defaultGlyph.Value;
					hasCurrentGlyph = true;
				}

				float proposedWidth = offset.X + currentGlyph.WidthIncludingBearings + Spacing;
				if (proposedWidth > width)
				{
					width = proposedWidth;
				}

				if (currentGlyph.Cropping.Height > finalLineHeight)
				{
					finalLineHeight = currentGlyph.Cropping.Height;
				}
			}

			size.X = width;
			size.Y = fullLineCount * LineSpacing + finalLineHeight;
		}

		#endregion

		#region Internal CharacterSource struct

		internal struct CharacterSource
		{
			private readonly string _string;
			private readonly StringBuilder _builder;

			public CharacterSource(string s)
			{
				_string = s;
				_builder = null;
				Length = s.Length;
			}

			public CharacterSource(StringBuilder builder)
			{
				_builder = builder;
				_string = null;
				Length = _builder.Length;
			}

			public readonly int Length;
			public char this[int index]
			{
				get
				{
					if (_string != null)
					{
						return _string[index];
					}
					return _builder[index];
				}
			}
		}

		#endregion

		#region Private Glyph struct

		/// <summary>
		/// Struct that defines the spacing, Kerning, and bounds of a character.
		/// THIS IS A MONOGAME EXTENSION!
		/// </summary>
		/// <remarks>Provides the data necessary to implement custom SpriteFont rendering.</remarks>
		public struct Glyph
		{
			/// <summary>
			/// The char associated with this glyph.
			/// </summary>
			public char Character;
			/// <summary>
			/// Rectangle in the font texture where this letter exists.
			/// </summary>
 			public Rectangle BoundsInTexture;
			/// <summary>
			/// Cropping applied to the BoundsInTexture to calculate the bounds of the actual character.
			/// </summary>
			public Rectangle Cropping;
			/// <summary>
			/// The amount of space between the left side of the character and its first pixel in the X dimension.
			/// </summary>
			public float LeftSideBearing;
			/// <summary>
			/// The amount of space between the right side of the character and its last pixel in the X dimension.
			/// </summary>
			public float RightSideBearing;
			/// <summary>
			/// Width of the character before kerning is applied. 
			/// </summary>
			public float Width;
			/// <summary>
			/// Width of the character before kerning is applied. 
			/// </summary>
			public float WidthIncludingBearings;

			public static readonly Glyph Empty = new Glyph();

			public override string ToString()
			{
				return string.Format(
					"CharacterIndex={0}, Glyph={1}, Cropping={2}, Kerning={3},{4},{5}",
					Character, BoundsInTexture, Cropping, LeftSideBearing, Width, RightSideBearing);
			}
		}

		#endregion

		#region Character Comparison Class

		class CharComparer: IEqualityComparer<char>
		{
			public bool Equals(char x, char y)
			{
				return x == y;
			}

			public int GetHashCode(char b)
			{
				return (b | (b << 16));
			}

			static public readonly CharComparer Default = new CharComparer();
		}

		#endregion

		#region Private Static Errors Class

		static class Errors
		{
			public const string TextContainsUnresolvableCharacters =
				"Text contains characters that cannot be resolved by this SpriteFont.";
		}

		#endregion

        #region ISpriteBatch extensions
        public void DrawInto(ISpriteBatch spriteBatch, ref String text, Vector2 position, Color color,
                                float rotation, Vector2 origin, Vector2 scale, SpriteEffects effect, float depth)
        {
            CharacterSource src = new CharacterSource(text);
            this.DrawIntoInternal(spriteBatch, ref src, position, color, rotation, origin, scale, effect, depth);
        }

        public void DrawInto(ISpriteBatch spriteBatch, ref StringBuilder text, Vector2 position, Color color,
                                float rotation, Vector2 origin, Vector2 scale, SpriteEffects effect, float depth)
        {
            CharacterSource src = new CharacterSource(text);
            this.DrawIntoInternal(spriteBatch, ref src, position, color, rotation, origin, scale, effect, depth);
        }

        private void DrawIntoInternal(ISpriteBatch spriteBatch, ref CharacterSource text, Vector2 position, Color color,
                                float rotation, Vector2 origin, Vector2 scale, SpriteEffects effect, float depth)
        {
            Vector2 flipAdjustment = Vector2.Zero;

            bool flippedVert = (effect & SpriteEffects.FlipVertically) == SpriteEffects.FlipVertically;
            bool flippedHorz = (effect & SpriteEffects.FlipHorizontally) == SpriteEffects.FlipHorizontally;

            if (flippedVert || flippedHorz)
            {
                Vector2 size;
                MeasureString(ref text, out size);

                if (flippedHorz)
                {
                    origin.X *= -1;
                    flipAdjustment.X = -size.X;
                }

                if (flippedVert)
                {
                    origin.Y *= -1;
                    flipAdjustment.Y = LineSpacing - size.Y;
                }
            }

            /* TODO: This looks excessive... i suspect we could do most
             * of this with simple vector math and avoid this much matrix work.
             */

            Matrix transformation, temp;
            Matrix.CreateTranslation(-origin.X, -origin.Y, 0f, out transformation);
            Matrix.CreateScale((flippedHorz ? -scale.X : scale.X), (flippedVert ? -scale.Y : scale.Y), 1f, out temp);
            Matrix.Multiply(ref transformation, ref temp, out transformation);
            Matrix.CreateTranslation(flipAdjustment.X, flipAdjustment.Y, 0, out temp);
            Matrix.Multiply(ref temp, ref transformation, out transformation);
            Matrix.CreateRotationZ(rotation, out temp);
            Matrix.Multiply(ref transformation, ref temp, out transformation);
            Matrix.CreateTranslation(position.X, position.Y, 0f, out temp);
            Matrix.Multiply(ref transformation, ref temp, out transformation);

            // Get the default glyph here once.
            Glyph? defaultGlyph = null;
            if (DefaultCharacter.HasValue)
            {
                defaultGlyph = _glyphs[DefaultCharacter.Value];
            }

            Glyph currentGlyph = Glyph.Empty;
            Vector2 offset = Vector2.Zero;
            bool hasCurrentGlyph = false;
            bool firstGlyphOfLine = true;

            for (int i = 0; i < text.Length; i += 1)
            {
                char c = text[i];
                if (c == '\r')
                {
                    hasCurrentGlyph = false;
                    continue;
                }

                if (c == '\n')
                {
                    offset.X = 0;
                    offset.Y += LineSpacing;
                    hasCurrentGlyph = false;
                    firstGlyphOfLine = true;
                    continue;
                }

                if (hasCurrentGlyph)
                {
                    offset.X += Spacing + currentGlyph.Width + currentGlyph.RightSideBearing;
                }

                if (!_glyphs.TryGetValue(c, out currentGlyph))
                {
                    if (!defaultGlyph.HasValue)
                    {
                        throw new ArgumentException(Errors.TextContainsUnresolvableCharacters, "text");
                    }

                    currentGlyph = defaultGlyph.Value;
                }
                hasCurrentGlyph = true;

                /* The first character on a line might have a negative left
                 * side bearing. In this scenario, SpriteBatch/SpriteFont
                 * normally offset the text to the right, so that text does
                 * not hang off the left side of its rectangle.
                 */
                if (firstGlyphOfLine)
                {
                    offset.X = Math.Max(currentGlyph.LeftSideBearing, 0);
                    firstGlyphOfLine = false;
                }
                else
                {
                    offset.X += currentGlyph.LeftSideBearing;
                }

                Vector2 p = offset;

                if (flippedHorz)
                {
                    p.X += currentGlyph.BoundsInTexture.Width;
                }

                p.X += currentGlyph.Cropping.X;

                if (flippedVert)
                {
                    p.Y += currentGlyph.BoundsInTexture.Height - LineSpacing;
                }

                p.Y += currentGlyph.Cropping.Y;

                Vector2.Transform(ref p, ref transformation, out p);

                Vector4 destRect = new Vector4(
                    p.X,
                    p.Y,
                    currentGlyph.BoundsInTexture.Width * scale.X,
                    currentGlyph.BoundsInTexture.Height * scale.Y
                );

                spriteBatch.Draw(_texture,
                    destRect,
                    currentGlyph.BoundsInTexture,
                    color, rotation,
                    Vector2.Zero,
                    effect,
                    depth,
                    false);
            }

            // We need to flush if we're using Immediate sort mode.
            spriteBatch.FlushIfNeeded();
        }
        #endregion
    }
}
