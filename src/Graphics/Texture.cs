#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

#region Using Statements
using System;
using System.Diagnostics;
#endregion

namespace Microsoft.Xna.Framework.Graphics
{
	public abstract class Texture : GraphicsResource
	{
		#region Public Properties

		public SurfaceFormat Format
		{
			get;
			protected set;
		}

		public int LevelCount
		{
			get;
			protected set;
		}

		#endregion

		#region Internal OpenGL Variables

		internal OpenGLDevice.OpenGLTexture texture;

		internal OpenGLDevice.GLenum glInternalFormat;
		internal OpenGLDevice.GLenum glFormat;
		internal OpenGLDevice.GLenum glType;

		#endregion

		#region Protected Dispose Method

		protected override void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				GraphicsDevice.AddDisposeAction(() =>
				{
					if (texture != null)
					{
						Game.Instance.GraphicsDevice.GLDevice.DeleteTexture(texture);
					}
				});
			}
			base.Dispose(disposing);
		}

		#endregion

		#region Internal Surface Pitch Calculator

		internal int GetPitch(int width)
		{
			Debug.Assert(width > 0, "The width is negative!");

			if (	Format == SurfaceFormat.Dxt1 ||
				Format == SurfaceFormat.Dxt3 ||
				Format == SurfaceFormat.Dxt5	)
			{
				return ((width + 3) / 4) * GetFormatSize();
			}
			return width * GetFormatSize();
		}

		#endregion

		#region Internal Context Reset Method

		internal protected override void GraphicsDeviceResetting()
		{
			// FIXME: Do we even want to bother with DeviceResetting for GL? -flibit
		}

		#endregion

		#region Protected XNA->GL SurfaceFormat Conversion Method

		protected void GetGLSurfaceFormat()
		{
			switch (Format)
			{
				case SurfaceFormat.Color:
					glInternalFormat = OpenGLDevice.GLenum.GL_RGBA;
					glFormat = OpenGLDevice.GLenum.GL_RGBA;
					glType = OpenGLDevice.GLenum.GL_UNSIGNED_BYTE;
					break;
				case SurfaceFormat.Bgr565:
					glInternalFormat = OpenGLDevice.GLenum.GL_RGB;
					glFormat = OpenGLDevice.GLenum.GL_RGB;
					glType = OpenGLDevice.GLenum.GL_UNSIGNED_SHORT_5_6_5;
					break;
				case SurfaceFormat.Bgra4444:
					glInternalFormat = OpenGLDevice.GLenum.GL_RGBA4;
					glFormat = OpenGLDevice.GLenum.GL_BGRA;
					glType = OpenGLDevice.GLenum.GL_UNSIGNED_SHORT_4_4_4_4;
					break;
				case SurfaceFormat.Bgra5551:
					glInternalFormat = OpenGLDevice.GLenum.GL_RGBA;
					glFormat = OpenGLDevice.GLenum.GL_BGRA;
					glType = OpenGLDevice.GLenum.GL_UNSIGNED_SHORT_5_5_5_1;
					break;
				case SurfaceFormat.Alpha8:
					glInternalFormat = OpenGLDevice.GLenum.GL_LUMINANCE;
					glFormat = OpenGLDevice.GLenum.GL_LUMINANCE;
					glType = OpenGLDevice.GLenum.GL_UNSIGNED_BYTE;
					break;
				case SurfaceFormat.Dxt1:
					glInternalFormat = OpenGLDevice.GLenum.GL_COMPRESSED_RGBA_S3TC_DXT1_EXT;
					glFormat = OpenGLDevice.GLenum.GL_COMPRESSED_TEXTURE_FORMATS;
					break;
				case SurfaceFormat.Dxt3:
					glInternalFormat = OpenGLDevice.GLenum.GL_COMPRESSED_RGBA_S3TC_DXT3_EXT;
					glFormat = OpenGLDevice.GLenum.GL_COMPRESSED_TEXTURE_FORMATS;
					break;
				case SurfaceFormat.Dxt5:
					glInternalFormat = OpenGLDevice.GLenum.GL_COMPRESSED_RGBA_S3TC_DXT5_EXT;
					glFormat = OpenGLDevice.GLenum.GL_COMPRESSED_TEXTURE_FORMATS;
					break;
				case SurfaceFormat.Single:
					glInternalFormat = OpenGLDevice.GLenum.GL_R32F;
					glFormat = OpenGLDevice.GLenum.GL_RED;
					glType = OpenGLDevice.GLenum.GL_FLOAT;
					break;
				case SurfaceFormat.HalfVector2:
					glInternalFormat = OpenGLDevice.GLenum.GL_RG16F;
					glFormat = OpenGLDevice.GLenum.GL_RG;
					glType = OpenGLDevice.GLenum.GL_HALF_FLOAT;
					break;
				case SurfaceFormat.HdrBlendable:
				case SurfaceFormat.HalfVector4:
					glInternalFormat = OpenGLDevice.GLenum.GL_RGBA16F;
					glFormat = OpenGLDevice.GLenum.GL_RGBA;
					glType = OpenGLDevice.GLenum.GL_HALF_FLOAT;
					break;
				case SurfaceFormat.HalfSingle:
					glInternalFormat = OpenGLDevice.GLenum.GL_R16F;
					glFormat = OpenGLDevice.GLenum.GL_RED;
					glType = OpenGLDevice.GLenum.GL_HALF_FLOAT;
					break;
				case SurfaceFormat.Vector2:
					glInternalFormat = OpenGLDevice.GLenum.GL_RG32F;
					glFormat = OpenGLDevice.GLenum.GL_RG;
					glType = OpenGLDevice.GLenum.GL_FLOAT;
					break;
				case SurfaceFormat.Vector4:
					glInternalFormat = OpenGLDevice.GLenum.GL_RGBA32F;
					glFormat = OpenGLDevice.GLenum.GL_RGBA;
					glType = OpenGLDevice.GLenum.GL_FLOAT;
					break;
				case SurfaceFormat.NormalizedByte2: // Unconfirmed!
					glInternalFormat = OpenGLDevice.GLenum.GL_RG8I;
					glFormat = OpenGLDevice.GLenum.GL_RG;
					glType = OpenGLDevice.GLenum.GL_BYTE;
					break;
				case SurfaceFormat.NormalizedByte4: // Unconfirmed!
					glInternalFormat = OpenGLDevice.GLenum.GL_RGBA8I;
					glFormat = OpenGLDevice.GLenum.GL_RGBA;
					glType = OpenGLDevice.GLenum.GL_BYTE;
					break;
				case SurfaceFormat.Rg32:
					glInternalFormat = OpenGLDevice.GLenum.GL_RG16;
					glFormat = OpenGLDevice.GLenum.GL_RG;
					glType = OpenGLDevice.GLenum.GL_UNSIGNED_SHORT;
					break;
				case SurfaceFormat.Rgba64:
					glInternalFormat = OpenGLDevice.GLenum.GL_RGBA16;
					glFormat = OpenGLDevice.GLenum.GL_RGBA;
					glType = OpenGLDevice.GLenum.GL_UNSIGNED_SHORT;
					break;
				case SurfaceFormat.Rgba1010102:
					glInternalFormat = OpenGLDevice.GLenum.GL_RGB10_A2_EXT;
					glFormat = OpenGLDevice.GLenum.GL_RGBA;
					glType = OpenGLDevice.GLenum.GL_UNSIGNED_INT_10_10_10_2;
					break;
				default:
					throw new NotSupportedException();
			}
		}

		#endregion

		#region Protected SurfaceFormat Size Method

		protected int GetFormatSize()
		{
			switch (Format)
			{
				case SurfaceFormat.Dxt1:
					return 8;
				case SurfaceFormat.Dxt3:
				case SurfaceFormat.Dxt5:
					return 16;
				case SurfaceFormat.Alpha8:
					return 1;
				case SurfaceFormat.Bgr565:
				case SurfaceFormat.Bgra4444:
				case SurfaceFormat.Bgra5551:
				case SurfaceFormat.HalfSingle:
				case SurfaceFormat.NormalizedByte2:
					return 2;
				case SurfaceFormat.Color:
				case SurfaceFormat.Single:
				case SurfaceFormat.Rg32:
				case SurfaceFormat.HalfVector2:
				case SurfaceFormat.NormalizedByte4:
				case SurfaceFormat.Rgba1010102:
					return 4;
				case SurfaceFormat.HalfVector4:
				case SurfaceFormat.Rgba64:
				case SurfaceFormat.Vector2:
					return 8;
				case SurfaceFormat.Vector4:
					return 16;
				default:
					throw new ArgumentException("Should be a value defined in SurfaceFormat", "Format");
			}
		}

		#endregion

		#region Static Mipmap Level Calculator

		internal static int CalculateMipLevels(
			int width,
			int height = 0,
			int depth = 0
		) {
			int levels = 1;
			for (
				int size = Math.Max(Math.Max(width, height), depth);
				size > 1;
				levels += 1
			) {
				size /= 2;
			}
			return levels;
		}

		#endregion
	}
}
