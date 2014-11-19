#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

#region Using Statements
using System.IO;
#endregion

namespace Microsoft.Xna.Framework.Graphics
{
	internal class Resources
	{
		#region Public Static Properties

		public static byte[] AlphaTestEffect
		{
			get
			{
				if (alphaTestEffect == null)
				{
					alphaTestEffect = GetResource("SpriteEffect");
				}
				return alphaTestEffect;
			}
		}

		public static byte[] BasicEffect
		{
			get
			{
				if (basicEffect == null)
				{
					basicEffect = GetResource("SpriteEffect");
				}
				return basicEffect;
			}
		}

		public static byte[] DualTextureEffect
		{
			get
			{
				if (dualTextureEffect == null)
				{
					dualTextureEffect = GetResource("SpriteEffect");
				}
				return dualTextureEffect;
			}
		}

		public static byte[] EnvironmentMapEffect
		{
			get
			{
				if (environmentMapEffect == null)
				{
					environmentMapEffect = GetResource("SpriteEffect");
				}
				return environmentMapEffect;
			}
		}

		public static byte[] SkinnedEffect
		{
			get
			{
				if (skinnedEffect == null)
				{
					skinnedEffect = GetResource("SpriteEffect");
				}
				return skinnedEffect;
			}
		}

		public static byte[] SpriteEffect
		{
			get
			{
				if (spriteEffect == null)
				{
					spriteEffect = GetResource("SpriteEffect");
				}
				return spriteEffect;
			}
		}

		#endregion

		#region Private Static Variables

		private static byte[] alphaTestEffect;
		private static byte[] basicEffect;
		private static byte[] dualTextureEffect;
		private static byte[] environmentMapEffect;
		private static byte[] skinnedEffect;
		private static byte[] spriteEffect;

		#endregion

		#region Private Static Methods

		private static byte[] GetResource(string name)
		{
			Stream stream = typeof(Resources).Assembly.GetManifestResourceStream(
				"Microsoft.Xna.Framework.Graphics.Effect.Resources." + name + ".fxb"
			);
			using (MemoryStream ms = new MemoryStream())
			{
				stream.CopyTo(ms);
				return ms.ToArray();
			}
		}

		#endregion
	}
}
