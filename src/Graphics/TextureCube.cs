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
using System.Runtime.InteropServices;
#endregion

namespace Microsoft.Xna.Framework.Graphics
{
	public class TextureCube : Texture
	{
		#region Public Properties

		/// <summary>
		/// Gets the width and height of the cube map face in pixels.
		/// </summary>
		/// <value>The width and height of a cube map face in pixels.</value>
		public int Size
		{
			get;
			private set;
		}

		#endregion

		#region Public Constructor

		public TextureCube(
			GraphicsDevice graphicsDevice,
			int size,
			bool mipMap,
			SurfaceFormat format
		) {
			if (graphicsDevice == null)
			{
				throw new ArgumentNullException("graphicsDevice");
			}

			GraphicsDevice = graphicsDevice;
			Size = size;
			LevelCount = mipMap ? CalculateMipLevels(size) : 1;
			Format = format;
			GetGLSurfaceFormat();

			Threading.ForceToMainThread(() =>
			{
				texture = GraphicsDevice.GLDevice.CreateTexture(
					typeof(TextureCube),
					Format,
					mipMap
				);

				if (glFormat == OpenGLDevice.GLenum.GL_COMPRESSED_TEXTURE_FORMATS)
				{
					for (int i = 0; i < 6; i += 1)
					{
						for (int l = 0; l < LevelCount; l += 1)
						{
							int levelSize = Math.Max(size >> l, 1);
							graphicsDevice.GLDevice.glCompressedTexImage2D(
								OpenGLDevice.GLenum.GL_TEXTURE_CUBE_MAP_POSITIVE_X + i,
								l,
								(int) glInternalFormat,
								levelSize,
								levelSize,
								0,
								((levelSize + 3) / 4) * ((levelSize + 3) / 4) * GetFormatSize(),
								IntPtr.Zero
							);
						}
					}
				}
				else
				{
					for (int i = 0; i < 6; i += 1)
					{
						for (int l = 0; l < LevelCount; l += 1)
						{
							int levelSize = Math.Max(size >> l, 1);
							graphicsDevice.GLDevice.glTexImage2D(
								OpenGLDevice.GLenum.GL_TEXTURE_CUBE_MAP_POSITIVE_X + i,
								l,
								(int) glInternalFormat,
								levelSize,
								levelSize,
								0,
								glFormat,
								glType,
								IntPtr.Zero
							);
						}
					}
				}
			});
		}

		#endregion

		#region Public SetData Methods

		public void SetData<T>(
			CubeMapFace cubeMapFace,
			T[] data
		) where T : struct {
			SetData(
				cubeMapFace,
				0,
				null,
				data,
				0,
				data.Length
			);
		}

		public void SetData<T>(
			CubeMapFace cubeMapFace,
			T[] data,
			int startIndex,
			int elementCount
		) where T : struct {
			SetData(
				cubeMapFace,
				0,
				null,
				data,
				startIndex,
				elementCount
			);
		}

		public void SetData<T>(
			CubeMapFace cubeMapFace,
			int level,
			Rectangle? rect,
			T[] data,
			int startIndex,
			int elementCount
		) where T : struct {
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}

			int xOffset, yOffset, width, height;
			if (rect.HasValue)
			{
				xOffset = rect.Value.X;
				yOffset = rect.Value.Y;
				width = rect.Value.Width;
				height = rect.Value.Height;
			}
			else
			{
				xOffset = 0;
				yOffset = 0;
				width = Math.Max(1, Size >> level);
				height = Math.Max(1, Size >> level);
			}

			Threading.ForceToMainThread(() =>
			{
				GCHandle dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
				int elementSizeInBytes = Marshal.SizeOf(typeof(T));
				int startByte = startIndex * elementSizeInBytes;
				IntPtr dataPtr = (IntPtr) (dataHandle.AddrOfPinnedObject().ToInt64() + startByte);

				try
				{
					GraphicsDevice.GLDevice.BindTexture(texture);
					if (glFormat == OpenGLDevice.GLenum.GL_COMPRESSED_TEXTURE_FORMATS)
					{
						int dataLength;
						if (elementCount > 0)
						{
							dataLength = elementCount * elementSizeInBytes;
						}
						else
						{
							dataLength = data.Length - startByte;
						}

						/* Note that we're using glInternalFormat, not glFormat.
						 * In this case, they should actually be the same thing,
						 * but we use glFormat somewhat differently for
						 * compressed textures.
						 * -flibit
						 */
						GraphicsDevice.GLDevice.glCompressedTexSubImage2D(
							OpenGLDevice.GLenum.GL_TEXTURE_CUBE_MAP_POSITIVE_X + (int) cubeMapFace,
							level,
							xOffset,
							yOffset,
							width,
							height,
							glInternalFormat,
							dataLength,
							dataPtr
						);
					}
					else
					{
						GraphicsDevice.GLDevice.glTexSubImage2D(
							OpenGLDevice.GLenum.GL_TEXTURE_CUBE_MAP_POSITIVE_X + (int) cubeMapFace,
							level,
							xOffset,
							yOffset,
							width,
							height,
							glFormat,
							glType,
							dataPtr
						);
					}
				}
				finally
				{
					dataHandle.Free();
				}
			});
		}

		#endregion

		#region Public GetData Method

		public void GetData<T>(
			CubeMapFace cubeMapFace,
			T[] data
		) where T : struct {
			GetData(
				cubeMapFace,
				0,
				null,
				data,
				0,
				data.Length
			);
		}

		public void GetData<T>(
			CubeMapFace cubeMapFace,
			T[] data,
			int startIndex,
			int elementCount
		) where T : struct {
			GetData(
				cubeMapFace,
				0,
				null,
				data,
				startIndex,
				elementCount
			);
		}

		public void GetData<T>(
			CubeMapFace cubeMapFace,
			int level,
			Rectangle? rect,
			T[] data,
			int startIndex,
			int elementCount
		) where T : struct {
			if (data == null || data.Length == 0)
			{
				throw new ArgumentException("data cannot be null");
			}
			if (data.Length < startIndex + elementCount)
			{
				throw new ArgumentException(
					"The data passed has a length of " + data.Length.ToString() +
					" but " + elementCount.ToString() + " pixels have been requested."
				);
			}

			GraphicsDevice.GLDevice.BindTexture(texture);

			if (glFormat == OpenGLDevice.GLenum.GL_COMPRESSED_TEXTURE_FORMATS)
			{
				throw new NotImplementedException("GetData, CompressedTexture");
			}
			else if (rect == null)
			{
				// Just throw the whole texture into the user array.
				GCHandle ptr = GCHandle.Alloc(data, GCHandleType.Pinned);
				try
				{
					GraphicsDevice.GLDevice.glGetTexImage(
						OpenGLDevice.GLenum.GL_TEXTURE_CUBE_MAP_POSITIVE_X + (int) cubeMapFace,
						0,
						glFormat,
						glType,
						ptr.AddrOfPinnedObject()
					);
				}
				finally
				{
					ptr.Free();
				}
			}
			else
			{
				// Get the whole texture...
				T[] texData = new T[Size * Size];
				GCHandle ptr = GCHandle.Alloc(texData, GCHandleType.Pinned);
				try
				{
					GraphicsDevice.GLDevice.glGetTexImage(
						OpenGLDevice.GLenum.GL_TEXTURE_CUBE_MAP_POSITIVE_X + (int) cubeMapFace,
						0,
						glFormat,
						glType,
						ptr.AddrOfPinnedObject()
					);
				}
				finally
				{
					ptr.Free();
				}

				// Now, blit the rect region into the user array.
				Rectangle region = rect.Value;
				int curPixel = -1;
				for (int row = region.Y; row < region.Y + region.Height; row += 1)
				{
					for (int col = region.X; col < region.X + region.Width; col += 1)
					{
						curPixel += 1;
						if (curPixel < startIndex)
						{
							// If we're not at the start yet, just keep going...
							continue;
						}
						if (curPixel > elementCount)
						{
							// If we're past the end, we're done!
							return;
						}
						data[curPixel - startIndex] = texData[(row * Size) + col];
					}
				}
			}
		}

		#endregion
	}
}
