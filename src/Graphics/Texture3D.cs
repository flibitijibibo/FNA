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
	public class Texture3D : Texture
	{
		#region Public Properties

		public int Width
		{
			get;
			private set;
		}

		public int Height
		{
			get;
			private set;
		}

		public int Depth
		{
			get;
			private set;
		}

		#endregion

		#region Public Constructor

		public Texture3D(
			GraphicsDevice graphicsDevice,
			int width,
			int height,
			int depth,
			bool mipMap,
			SurfaceFormat format
		) {
			if (graphicsDevice == null)
			{
				throw new ArgumentNullException("graphicsDevice");
			}

			GraphicsDevice = graphicsDevice;
			Width = width;
			Height = height;
			Depth = depth;
			LevelCount = mipMap ? CalculateMipLevels(width, height) : 1;

			Format = format;
			GetGLSurfaceFormat();

			Threading.ForceToMainThread(() =>
			{
				texture = GraphicsDevice.GLDevice.CreateTexture(
					typeof(Texture3D),
					Format,
					mipMap
				);

				for (int i = 0; i < LevelCount; i += 1)
				{
					GraphicsDevice.GLDevice.glTexImage3D(
						OpenGLDevice.GLenum.GL_TEXTURE_3D,
						i,
						(int) glInternalFormat,
						Math.Max(width >> i, 1),
						Math.Max(height >> i, 1),
						depth,
						0,
						glFormat,
						glType,
						IntPtr.Zero
					);
				}
			});
		}

		#endregion

		#region Public SetData Methods

		public void SetData<T>(T[] data) where T : struct
		{
			SetData<T>(
				data,
				0,
				data.Length
			);
		}

		public void SetData<T>(
			T[] data,
			int startIndex,
			int elementCount
		) where T : struct {
			SetData<T>(
				0,
				0,
				0,
				Width,
				Height,
				0,
				Depth,
				data,
				startIndex,
				elementCount
			);
		}

		public void SetData<T>(
			int level,
			int left,
			int top,
			int right,
			int bottom,
			int front,
			int back,
			T[] data,
			int startIndex,
			int elementCount
		) where T : struct {
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}

			Threading.ForceToMainThread(() =>
			{
				GCHandle dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
				try
				{
					GraphicsDevice.GLDevice.BindTexture(texture);
					GraphicsDevice.GLDevice.glTexSubImage3D(
						OpenGLDevice.GLenum.GL_TEXTURE_3D,
						level,
						left,
						top,
						front,
						right - left,
						bottom - top,
						back - front,
						glFormat,
						glType,
						(IntPtr) (dataHandle.AddrOfPinnedObject().ToInt64() + startIndex * Marshal.SizeOf(typeof(T)))
					);
				}
				finally
				{
					dataHandle.Free();
				}
			});
		}

		#endregion

		#region Public GetData Methods

		/// <summary>
		/// Gets a copy of 3D texture data.
		/// </summary>
		/// <typeparam name="T">The type of the elements in the array.</typeparam>
		/// <param name="data">Array of data.</param>
		public void GetData<T>(T[] data) where T : struct
		{
			GetData(
				data,
				0,
				data.Length
			);
		}

		/// <summary>
		/// Gets a copy of 3D texture data, specifying a start index and number of elements.
		/// </summary>
		/// <typeparam name="T">The type of the elements in the array.</typeparam>
		/// <param name="data">Array of data.</param>
		/// <param name="startIndex">Index of the first element to get.</param>
		/// <param name="elementCount">Number of elements to get.</param>
		public void GetData<T>(
			T[] data,
			int startIndex,
			int elementCount
		) where T : struct {
			GetData(
				0,
				0,
				0,
				Width,
				Height,
				0,
				Depth,
				data,
				startIndex,
				elementCount
			);
		}

		/// <summary>
		/// Gets a copy of 3D texture data, specifying a mipmap level, source box, start index, and number of elements.
		/// </summary>
		/// <typeparam name="T">The type of the elements in the array.</typeparam>
		/// <param name="level">Mipmap level.</param>
		/// <param name="left">Position of the left side of the box on the x-axis.</param>
		/// <param name="top">Position of the top of the box on the y-axis.</param>
		/// <param name="right">Position of the right side of the box on the x-axis.</param>
		/// <param name="bottom">Position of the bottom of the box on the y-axis.</param>
		/// <param name="front">Position of the front of the box on the z-axis.</param>
		/// <param name="back">Position of the back of the box on the z-axis.</param>
		/// <param name="data">Array of data.</param>
		/// <param name="startIndex">Index of the first element to get.</param>
		/// <param name="elementCount">Number of elements to get.</param>
		public void GetData<T>(
			int level,
			int left,
			int top,
			int right,
			int bottom,
			int front,
			int back,
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
			if (	(left < 0 || left >= right) ||
				(top < 0 || top >= bottom) ||
				(front < 0 || front >= back)	)
			{
				throw new ArgumentException("Neither box size nor box position can be negative");
			}

			throw new NotImplementedException();
		}

		#endregion
	}
}
