#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

using System;

namespace Microsoft.Xna.Framework.Graphics
{
    public class PresentationParameters : IDisposable
    {
        #region Constants

        public const int DefaultPresentRate = 60;

        #endregion Constants

        #region Private Fields

        private DepthFormat depthStencilFormat;
        private SurfaceFormat backBufferFormat;
        private int backBufferHeight = GraphicsDeviceManager.DefaultBackBufferHeight;
        private int backBufferWidth = GraphicsDeviceManager.DefaultBackBufferWidth;
        private IntPtr deviceWindowHandle;
        private bool isFullScreen;
        private int multiSampleCount;
        private bool disposed;       	

        #endregion Private Fields

        #region Constructors

        public PresentationParameters()
        {
            Clear();
        }

        ~PresentationParameters()
        {
            Dispose(false);
        }

        #endregion Constructors

        #region Properties

        public SurfaceFormat BackBufferFormat
        {
            get { return backBufferFormat; }
            set { backBufferFormat = value; }
        }

        public int BackBufferHeight
        {
            get { return backBufferHeight; }
            set { backBufferHeight = value; }
        }

        public int BackBufferWidth
        {
            get { return backBufferWidth; }
            set { backBufferWidth = value; }
        }

        public Rectangle Bounds 
        {
            get { return new Rectangle(0, 0, backBufferWidth, backBufferHeight); }
        }

        public IntPtr DeviceWindowHandle
        {
            get { return deviceWindowHandle; }
            set { deviceWindowHandle = value; }
        }

        public DepthFormat DepthStencilFormat
        {
            get { return depthStencilFormat; }
            set { depthStencilFormat = value; }
        }

        public bool IsFullScreen
        {
			get
            {
				 return isFullScreen;
            }
            set
            {
            	isFullScreen=value;
			}
        }
		
        public int MultiSampleCount
        {
            get { return multiSampleCount; }
            set { multiSampleCount = value; }
        }
		
        public PresentInterval PresentationInterval { get; set; }

		public DisplayOrientation DisplayOrientation 
		{ 
			get; 
			set; 
		}
		
		public RenderTargetUsage RenderTargetUsage { get; set; }

        #endregion Properties


        #region Methods

        public void Clear()
        {
            backBufferFormat = SurfaceFormat.Color;
            backBufferWidth = GraphicsDeviceManager.DefaultBackBufferWidth;
            backBufferHeight = GraphicsDeviceManager.DefaultBackBufferHeight;     
            deviceWindowHandle = IntPtr.Zero;
            // isFullScreen = false;
            depthStencilFormat = DepthFormat.None;
            multiSampleCount = 0;
            PresentationInterval = PresentInterval.Default;
            DisplayOrientation = DisplayOrientation.Default;
        }

        public PresentationParameters Clone()
        {
            PresentationParameters clone = new PresentationParameters();
            clone.backBufferFormat = this.backBufferFormat;
            clone.backBufferHeight = this.backBufferHeight;
            clone.backBufferWidth = this.backBufferWidth;
            clone.deviceWindowHandle = this.deviceWindowHandle;
            clone.disposed = this.disposed;
            clone.IsFullScreen = this.IsFullScreen;
            clone.depthStencilFormat = this.depthStencilFormat;
            clone.multiSampleCount = this.multiSampleCount;
            clone.PresentationInterval = this.PresentationInterval;
            clone.DisplayOrientation = this.DisplayOrientation;
            return clone;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                disposed = true;
                if (disposing)
                {
                    // Dispose managed resources
                }
                // Dispose unmanaged resources
            }
        }

        #endregion Methods

    }
}
