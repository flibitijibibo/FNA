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

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;
#endregion

namespace Microsoft.Xna.Framework
{
	public class GraphicsDeviceManager : IGraphicsDeviceService, IDisposable, IGraphicsDeviceManager
	{
		#region Public Properties

		public GraphicsProfile GraphicsProfile
		{ 
			get;
			set;
		}

		public GraphicsDevice GraphicsDevice
		{
			get
			{
				return _graphicsDevice;
			}
		}

		public bool IsFullScreen
		{
			get
			{
				if (_graphicsDevice != null)
				{
					return _graphicsDevice.PresentationParameters.IsFullScreen;
				}
				else
				{
					return _wantFullScreen;
				}
			}
			set
			{
				_wantFullScreen = value;
				if (_graphicsDevice != null)
				{
					_graphicsDevice.PresentationParameters.IsFullScreen = value;
				}
			}
		}

		public bool PreferMultiSampling
		{
			get
			{
				return _preferMultiSampling;
			}
			set
			{
				_preferMultiSampling = value;
			}
		}

		public SurfaceFormat PreferredBackBufferFormat
		{
			get
			{
				return _preferredBackBufferFormat;
			}
			set
			{
				_preferredBackBufferFormat = value;
			}
		}

		public int PreferredBackBufferHeight
		{
			get
			{
				return _preferredBackBufferHeight;
			}
			set
			{
				_preferredBackBufferHeight = value;
			}
		}

		public int PreferredBackBufferWidth
		{
			get
			{
				return _preferredBackBufferWidth;
			}
			set
			{
				_preferredBackBufferWidth = value;
			}
		}

		public DepthFormat PreferredDepthStencilFormat
		{
			get
			{
				return _preferredDepthStencilFormat;
			}
			set
			{
				_preferredDepthStencilFormat = value;
			}
		}

		public bool SynchronizeWithVerticalRetrace
		{
			get
			{
				return _synchronizedWithVerticalRetrace;
			}
			set
			{
				_synchronizedWithVerticalRetrace = value;
			}
		}

		public DisplayOrientation SupportedOrientations
		{
			get
			{
				return _supportedOrientations;
			}
			set
			{
				_supportedOrientations = value;
				if (_game.Window != null)
				{
					_game.Window.SetSupportedOrientations(_supportedOrientations);
				}
			}
		}

		#endregion

		#region Private Properties

		private bool _SynchronizedWithVerticalRetrace
		{
			get
			{
				return _synchronizedWithVerticalRetrace;
			}
		}

		#endregion

		#region Private Variables

		private Game _game;
		private GraphicsDevice _graphicsDevice;
		private int _preferredBackBufferHeight;
		private int _preferredBackBufferWidth;
		private SurfaceFormat _preferredBackBufferFormat;
		private DepthFormat _preferredDepthStencilFormat;
		private bool _preferMultiSampling;
		private DisplayOrientation _supportedOrientations;
		private bool _synchronizedWithVerticalRetrace = true;
		private bool _drawBegun;
		private bool disposed;
		private bool _wantFullScreen = false;

		#endregion

		#region Public Static Fields

		public static readonly int DefaultBackBufferHeight = 480;
		public static readonly int DefaultBackBufferWidth = 800;

		#endregion

		#region Public Constructors

		public GraphicsDeviceManager(Game game)
		{
			if (game == null)
			{
				throw new ArgumentNullException("The game cannot be null!");
			}

			_game = game;

			_supportedOrientations = DisplayOrientation.Default;

			_preferredBackBufferHeight = DefaultBackBufferHeight;
			_preferredBackBufferWidth = DefaultBackBufferWidth;

			_preferredBackBufferFormat = SurfaceFormat.Color;
			_preferredDepthStencilFormat = DepthFormat.Depth24;
			_synchronizedWithVerticalRetrace = true;

			if (_game.Services.GetService(typeof(IGraphicsDeviceManager)) != null)
			{
				throw new ArgumentException("Graphics Device Manager Already Present");
			}

			_game.Services.AddService(typeof(IGraphicsDeviceManager), this);
			_game.Services.AddService(typeof(IGraphicsDeviceService), this);
		}

		#endregion

		#region Deconstructor

		~GraphicsDeviceManager()
		{
			Dispose(false);
		}

		#endregion

		#region IGraphicsDeviceService Events

		public event EventHandler<EventArgs> DeviceCreated;
		public event EventHandler<EventArgs> DeviceDisposing;
		public event EventHandler<EventArgs> DeviceReset;
		public event EventHandler<EventArgs> DeviceResetting;
		public event EventHandler<PreparingDeviceSettingsEventArgs> PreparingDeviceSettings;

		#endregion

		#region Public Methods

		public void CreateDevice()
		{
			Initialize();

			OnDeviceCreated(EventArgs.Empty);
		}

		public bool BeginDraw()
		{
			if (_graphicsDevice == null)
			{
				return false;
			}

			_drawBegun = true;
			return true;
		}

		public void EndDraw()
		{
			if (_graphicsDevice != null && _drawBegun)
			{
				_drawBegun = false;
				_graphicsDevice.Present();
			}
		}

		public void ApplyChanges()
		{
			// Calling ApplyChanges() before CreateDevice() should have no effect.
			if (_graphicsDevice == null)
			{
				return;
			}

			// Notify DeviceResetting EventHandlers.
			OnDeviceResetting(null);
			GraphicsDevice.OnDeviceResetting();

			// Apply the GraphicsDevice changes internally.
			GraphicsDevice.PresentationParameters.BackBufferFormat =
				PreferredBackBufferFormat;
			GraphicsDevice.PresentationParameters.BackBufferWidth =
				PreferredBackBufferWidth;
			GraphicsDevice.PresentationParameters.BackBufferHeight =
				PreferredBackBufferHeight;
			GraphicsDevice.PresentationParameters.DepthStencilFormat =
				PreferredDepthStencilFormat;
			IsFullScreen = _wantFullScreen;

			// Make the Platform device changes.
			_game.Platform.BeginScreenDeviceChange(
				GraphicsDevice.PresentationParameters.IsFullScreen
			);
			_game.Platform.EndScreenDeviceChange(
				"FNA",
				GraphicsDevice.PresentationParameters.BackBufferWidth,
				GraphicsDevice.PresentationParameters.BackBufferHeight
			);

			// This platform uses VSyncEnabled rather than PresentationInterval...
			_game.Platform.VSyncEnabled = SynchronizeWithVerticalRetrace;

			// ... but we still need to apply the PresentInterval.
			GraphicsDevice.PresentationParameters.PresentationInterval = (
				SynchronizeWithVerticalRetrace ?
					PresentInterval.One :
					PresentInterval.Immediate
			);

			// Notify DeviceReset EventHandlers.
			OnDeviceReset(null);
			GraphicsDevice.OnDeviceReset();


			/* Set the new display size on the touch panel.
			 * 
			 * TODO: In XNA this seems to be done as part of the
			 * GraphicsDevice.DeviceReset event... we need to get
			 * those working.
			 */
			TouchPanel.DisplayWidth =
				_graphicsDevice.PresentationParameters.BackBufferWidth;
			TouchPanel.DisplayHeight =
				_graphicsDevice.PresentationParameters.BackBufferHeight;
		}

		public void ToggleFullScreen()
		{
			IsFullScreen = !IsFullScreen;

			/* FIXME: This wasn't here before.
			 * Shouldn't this toggle happen immediately?
			 * -flibit
			 */
			if (IsFullScreen)
			{
				_game.Platform.EnterFullScreen();
			}
			else
			{
				_game.Platform.ExitFullScreen();
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion

		#region Protected Methods

		protected virtual void Dispose(bool disposing)
		{
			if (!disposed)
			{
				if (disposing)
				{
					if (_graphicsDevice != null)
					{
						_graphicsDevice.Dispose();
						_graphicsDevice = null;
					}
				}
				disposed = true;
			}
		}

		#endregion

		#region Internal Methods

		/// <summary>
		/// This method is used by MonoGame Android to adjust the game's drawn to area to fill
		/// as much of the screen as possible whilst retaining the aspect ratio inferred from
		/// aspectRatio = (PreferredBackBufferWidth / PreferredBackBufferHeight).
		///
		/// NOTE: this is a hack that should be removed if proper back buffer to screen scaling
		/// is implemented. To disable it's effect, in the game's constructor use:
		///
		///	 graphics.IsFullScreen = true;
		///	 graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
		///	 graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
		///
		/// </summary>
		internal void ResetClientBounds()
		{
		}

		#endregion

		#region Internal IGraphicsDeviceService Methods

		/* FIXME: Why does the GraphicsDeviceManager not know enough about the
		 * GraphicsDevice to raise these events without help?
		 */
		internal void OnDeviceDisposing(EventArgs e)
		{
			Raise(DeviceDisposing, e);
		}

		/* FIXME: Why does the GraphicsDeviceManager not know enough about the
		 * GraphicsDevice to raise these events without help?
		 */
		internal void OnDeviceResetting(EventArgs e)
		{
			Raise(DeviceResetting, e);
		}

		/* FIXME: Why does the GraphicsDeviceManager not know enough about the
		 * GraphicsDevice to raise these events without help?
		 */
		internal void OnDeviceReset(EventArgs e)
		{
			Raise(DeviceReset, e);
		}

		/* FIXME: Why does the GraphicsDeviceManager not know enough about the
		 * GraphicsDevice to raise these events without help?
		 */
		internal void OnDeviceCreated(EventArgs e)
		{
			Raise(DeviceCreated, e);
		}

		#endregion

		#region Private Methods

		private void Initialize()
		{
			PresentationParameters presentationParameters = new PresentationParameters();
			presentationParameters.DepthStencilFormat = DepthFormat.Depth24;

			// It's bad practice to start fullscreen.
			presentationParameters.IsFullScreen = false;

			// TODO: Implement multisampling (aka anti-aliasing) for all platforms!
			if (PreparingDeviceSettings != null)
			{
				GraphicsDeviceInformation gdi = new GraphicsDeviceInformation();

				// Microsoft defaults this to Reach.
				gdi.GraphicsProfile = GraphicsProfile;

				gdi.Adapter = GraphicsAdapter.DefaultAdapter;
				gdi.PresentationParameters = presentationParameters;
				PreparingDeviceSettingsEventArgs pe =
					new PreparingDeviceSettingsEventArgs(gdi);
				PreparingDeviceSettings(this, pe);
				presentationParameters =
					pe.GraphicsDeviceInformation.PresentationParameters;

				/* FIXME: PreparingDeviceSettings may change these parameters,
				 * update ours too? -flibit
				 */
				PreferredBackBufferFormat = presentationParameters.BackBufferFormat;
				PreferredDepthStencilFormat = presentationParameters.DepthStencilFormat;

				GraphicsProfile = pe.GraphicsDeviceInformation.GraphicsProfile;
			}

			// Needs to be before ApplyChanges().
			_graphicsDevice = new GraphicsDevice(
				GraphicsAdapter.DefaultAdapter,
				GraphicsProfile,
				presentationParameters
			);

			ApplyChanges();

			/* Set the new display size on the touch panel.
			 * 
			 * TODO: In XNA this seems to be done as part of the
			 * GraphicsDevice.DeviceReset event... we need to get
			 * those working.
			 */
			TouchPanel.DisplayWidth =
				_graphicsDevice.PresentationParameters.BackBufferWidth;
			TouchPanel.DisplayHeight =
				_graphicsDevice.PresentationParameters.BackBufferHeight;
			TouchPanel.DisplayOrientation =
				_graphicsDevice.PresentationParameters.DisplayOrientation;
		}

		#endregion

		#region IGraphicsDeviceService Private Methods

		private void Raise<TEventArgs>(EventHandler<TEventArgs> handler, TEventArgs e)
			where TEventArgs : EventArgs
		{
			if (handler != null)
			{
				handler(this, e);
			}
		}

		#endregion
	}
}
