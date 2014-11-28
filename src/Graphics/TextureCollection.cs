#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

namespace Microsoft.Xna.Framework.Graphics
{
	public sealed class TextureCollection
	{
		#region Public Array Access Property

		public Texture this[int index]
		{
			get
			{
				return textures[index];
			}
			set
			{
				if (textures[index] != value)
				{
					textures[index] = value;
					if (!graphicsDevice.ModifiedSamplers.Contains(index))
					{
						graphicsDevice.ModifiedSamplers.Enqueue(index);
					}
				}
			}
		}

		#endregion

		#region Private Variables

		private readonly Texture[] textures;
		private GraphicsDevice graphicsDevice;

		#endregion

		#region Internal Constructor

		internal TextureCollection(GraphicsDevice parentDevice)
		{
			textures = new Texture[parentDevice.GLDevice.MaxTextureSlots];
			graphicsDevice = parentDevice;
			for (int i = 0; i < textures.Length; i += 1)
			{
				textures[i] = null;
			}
		}

		#endregion
	}
}
