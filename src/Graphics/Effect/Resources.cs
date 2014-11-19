#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

#region Using Statements
using System.Globalization;
using System.Resources;
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
				return Res.GetResource("AlphaTestEffect");
			}
		}

		public static byte[] BasicEffect
		{
			get
			{
				return Res.GetResource("BasicEffect");
			}
		}

		public static byte[] DualTextureEffect
		{
			get
			{
				return Res.GetResource("DualTextureEffect");
			}
		}

		public static byte[] EnvironmentMapEffect
		{
			get
			{
				return Res.GetResource("EnvironmentMapEffect");
			}
		}

		public static byte[] SkinnedEffect
		{
			get
			{
				return Res.GetResource("SkinnedEffect");
			}
		}

		public static byte[] SpriteEffect
		{
			get
			{
				return Res.GetResource("SpriteEffect");
			}
		}

		#endregion

		#region Private Static Properties

		private static Resources Res
		{
			get
			{
				if (resources != null)
				{
					resources = new Resources();
				}
				return resources;
			}
		}

		#endregion

		#region Private Static Variables

		private static Resources resources;

		#endregion

		#region Private Variables

		private ResourceManager resourceManager;

		#endregion

		#region Internal Constructor

		internal Resources()
		{
			resourceManager = new ResourceManager(
				"Microsoft.Xna.Framework.Graphics.Resources",
				typeof(Resources).Assembly
			);
		}

		#endregion

		#region Private Methods

		private byte[] GetResource(string name)
		{
			return (byte[]) resourceManager.GetObject(
				name + ".fxb",
				CultureInfo.InvariantCulture
			);
		}

		#endregion
	}
}
