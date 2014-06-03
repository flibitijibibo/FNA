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
using OpenTK.Graphics.OpenGL;
#endregion

namespace Microsoft.Xna.Framework.Graphics
{
	public class RenderTarget2D : Texture2D, IRenderTarget
	{
		#region Public Properties

		public DepthFormat DepthStencilFormat
		{
			get;
			private set;
		}

		public int MultiSampleCount
		{
			get;
			private set;
		}

		public RenderTargetUsage RenderTargetUsage
		{
			get;
			private set;
		}

		public bool IsContentLost
		{
			get
			{
				return false;
			}
		}

		#endregion

		#region Internal OpenGL Properties

		internal uint glDepthStencilBuffer
		{
			get;
			private set;
		}

		#endregion

		#region ContentLost Event

#pragma warning disable 0067
		// We never lose data, but lol XNA4 compliance -flibit
		public event EventHandler<EventArgs> ContentLost;
#pragma warning restore 0067

		#endregion

		#region Public Constructors

		public RenderTarget2D(
			GraphicsDevice graphicsDevice,
			int width,
			int height
		) : this(
			graphicsDevice,
			width,
			height,
			false,
			SurfaceFormat.Color,
			DepthFormat.None,
			0,
			RenderTargetUsage.DiscardContents
		) {
		}

		public RenderTarget2D(
			GraphicsDevice graphicsDevice,
			int width,
			int height,
			bool mipMap,
			SurfaceFormat preferredFormat,
			DepthFormat preferredDepthFormat
		) : this(
			graphicsDevice,
			width,
			height,
			mipMap,
			preferredFormat,
			preferredDepthFormat,
			0,
			RenderTargetUsage.DiscardContents
		) {
		}

		public RenderTarget2D(
			GraphicsDevice graphicsDevice,
			int width,
			int height,
			bool mipMap,
			SurfaceFormat preferredFormat,
			DepthFormat preferredDepthFormat,
			int preferredMultiSampleCount,
			RenderTargetUsage usage
		) : base(
			graphicsDevice,
			width,
			height,
			mipMap,
			preferredFormat
		) {
			DepthStencilFormat = preferredDepthFormat;
			MultiSampleCount = preferredMultiSampleCount;
			RenderTargetUsage = usage;

			// If we don't need a depth buffer then we're done.
			if (preferredDepthFormat == DepthFormat.None)
			{
				return;
			}

			Threading.ForceToMainThread(() =>
			{
				glDepthStencilBuffer = OpenGLDevice.Framebuffer.GenRenderbuffer(
					width,
					height,
					preferredDepthFormat
				);
			});
		}

		#endregion

		#region Protected Dispose Method

		protected override void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				GraphicsDevice.AddDisposeAction(() =>
				{
					if (glDepthStencilBuffer != 0)
					{
						OpenGLDevice.Instance.DeleteRenderbuffer(glDepthStencilBuffer);
					}
				});
			}
			base.Dispose(disposing);
		}

		#endregion

		#region Internal Context Reset Method

		protected internal override void GraphicsDeviceResetting()
		{
			base.GraphicsDeviceResetting();
		}

		#endregion

        #region GetData Speedup
        public override void GetData<T>(int level, Rectangle? rect, T[] data, int startIndex, int elementCount)
        {
#if OPENGL
            RenderTargetBinding[] bindings = this.GraphicsDevice.GetRenderTargets();
            bool isActiveTarget = bindings.Length == 1 && bindings[0].RenderTarget == this;

            if (isActiveTarget)
            {
                // GL.ReadPixels should be faster than reading back from the render target if we are already bound
                if (rect.HasValue)
                {
                    GL.ReadPixels(rect.Value.Left, rect.Value.Top, rect.Value.Width, rect.Value.Height, PixelFormat.Rgba, PixelType.UnsignedByte, data);
                }
                else
                {
                    GL.ReadPixels(0, 0, this.Width, this.Height, PixelFormat.Rgba, PixelType.UnsignedByte, data);
                }
            }
            else
            {
                base.GetData<T>(level, rect, data, startIndex, elementCount);
            }
#else
            base.GetData<T>(level, rect, data, startIndex, elementCount);
#endif

        }
        #endregion
    }
}
