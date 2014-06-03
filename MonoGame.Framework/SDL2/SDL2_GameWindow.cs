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
using System.Collections.Generic;
using System.ComponentModel;

using SDL2;

using Microsoft.Xna.Framework.Graphics;
#endregion

namespace Microsoft.Xna.Framework
{
    #region MonoGameConfig
    [Serializable]
    [System.Xml.Serialization.XmlType("Version")]
    public class VersionXml
    {
        public VersionXml()
        {
            this.Version = null;
        }

        public VersionXml(Version Version)
        {
            this.Version = Version;
        }

        [System.Xml.Serialization.XmlIgnore]
        public Version Version { get; set; }

        [System.Xml.Serialization.XmlText]
        [EditorBrowsable(EditorBrowsableState.Never), Browsable(false)]
        public string Value
        {
            get { return this.Version == null ? string.Empty : this.Version.ToString(); }
            set
            {
                Version temp;
                Version.TryParse(value, out temp);
                this.Version = temp;
            }
        }

        public static implicit operator Version(VersionXml VersionXml)
        {
            return VersionXml.Version;
        }

        public static implicit operator VersionXml(Version Version)
        {
            return new VersionXml(Version);
        }

        public override string ToString()
        {
            return this.Value;
        }
    }

    [Serializable]
    public struct MonoGameConfig
    {
        public VersionXml OpenGLMinVersion;

        [System.Xml.Serialization.XmlArray("OpenGLRequiredExtensions")]
        [System.Xml.Serialization.XmlArrayItem("Extension")]
        public List<String> OpenGLRequiredExtensions;

        /* So we've got this silly issue in SDL2's video API at the moment. We can't
         * add/remove the resizable property to the SDL_Window*!
         *
         * So, if you want to have your GameWindow be resizable, you must set it in the config
         * -darthdurden
         */
        public bool AllowResize;

        public MonoGameConfig(bool AllowResize, Version OpenGLMinVersion, IEnumerable<String> OpenGLRequiredExtensions)
        {
            this.AllowResize = AllowResize;
            this.OpenGLMinVersion = new VersionXml(OpenGLMinVersion);
            this.OpenGLRequiredExtensions = new List<string>(OpenGLRequiredExtensions);
        }
    }
    #endregion

    class SDL2_GameWindow : GameWindow
	{
		#region Public GameWindow Properties

		[DefaultValue(false)]
		public override bool AllowUserResizing
		{
			/* FIXME: This change should happen immediately. However, SDL2 does
			 * not yet have an SDL_SetWindowResizable, so we mostly just have
			 * this for the #define we've got at the top of this file.
			 * -flibit
			 */
			get
			{
				return (INTERNAL_sdlWindowFlags_Next & SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE) != 0;
			}
			set
			{
				// Note: This can only be used BEFORE window creation!
				if (value)
				{
					INTERNAL_sdlWindowFlags_Next |= SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE;
				}
				else
				{
					INTERNAL_sdlWindowFlags_Next &= ~SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE;
				}
			}
		}

		public override Rectangle ClientBounds
		{
			get
			{
				int x = 0, y = 0, w = 0, h = 0;
				SDL.SDL_GetWindowPosition(INTERNAL_sdlWindow, out x, out y);
				SDL.SDL_GetWindowSize(INTERNAL_sdlWindow, out w, out h);
				return new Rectangle(x, y, w, h);
			}
		}

		public override DisplayOrientation CurrentOrientation
		{
			get
			{
				// SDL2 has no orientation.
				return DisplayOrientation.LandscapeLeft;
			}
		}

		public override IntPtr Handle
		{
			get
			{
				return INTERNAL_sdlWindow;
			}
		}

		public override bool IsBorderless
		{
			get
			{
				return (INTERNAL_sdlWindowFlags_Next & SDL.SDL_WindowFlags.SDL_WINDOW_BORDERLESS) != 0;
			}
			set
			{
				if (value)
				{
					INTERNAL_sdlWindowFlags_Next |= SDL.SDL_WindowFlags.SDL_WINDOW_BORDERLESS;
				}
				else
				{
					INTERNAL_sdlWindowFlags_Next &= ~SDL.SDL_WindowFlags.SDL_WINDOW_BORDERLESS;
				}
			}
		}

		public override string ScreenDeviceName
		{
			get
			{
				return INTERNAL_deviceName;
			}
		}

		#endregion

		#region Private Game Instance

		private Game Game;

		#endregion

		#region Private SDL2 Window Variables

		private IntPtr INTERNAL_sdlWindow;

		private SDL.SDL_WindowFlags INTERNAL_sdlWindowFlags_Current;
		private SDL.SDL_WindowFlags INTERNAL_sdlWindowFlags_Next;

		private string INTERNAL_deviceName;

        private MonoGameConfig INTERNAL_config;

		#endregion

		#region Internal Constructor

		internal SDL2_GameWindow(Game game)
        {
            #region MonoGame Config
            if (System.IO.File.Exists("MonoGame.cfg"))
            {
                // Load the file.
                System.IO.FileStream fileIn = System.IO.File.OpenRead("MonoGame.cfg");

                // Load the data into our config struct.
                System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(MonoGameConfig));
                INTERNAL_config = (MonoGameConfig)serializer.Deserialize(fileIn);

                // We out.
                fileIn.Close();
            }
            else
            {
                INTERNAL_config = new MonoGameConfig(false, new Version(3, 0), new[] { "GL_ARB_framebuffer_object" });
            }
            #endregion

            Game = game;

			INTERNAL_sdlWindowFlags_Next = (
				SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL |
				SDL.SDL_WindowFlags.SDL_WINDOW_HIDDEN |
				SDL.SDL_WindowFlags.SDL_WINDOW_INPUT_FOCUS |
				SDL.SDL_WindowFlags.SDL_WINDOW_MOUSE_FOCUS
			);

            AllowUserResizing = INTERNAL_config.AllowResize;

			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_RED_SIZE, 8);
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_GREEN_SIZE, 8);
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_BLUE_SIZE, 8);
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_ALPHA_SIZE, 8);
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DEPTH_SIZE, 24);
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_STENCIL_SIZE, 8);
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DOUBLEBUFFER, 1);
#if DEBUG
			SDL.SDL_GL_SetAttribute(
				SDL.SDL_GLattr.SDL_GL_CONTEXT_FLAGS,
				(int) SDL.SDL_GLcontext.SDL_GL_CONTEXT_DEBUG_FLAG
			);
#endif

            string title = MonoGame.Utilities.AssemblyHelper.GetDefaultWindowTitle();

            # region Ensure we have the right version of OpenGL if using Monogame
            // Creating hidden dummy window to use for making an OpenGL context
            IntPtr dummyWindow = SDL.SDL_CreateWindow(
                title,
                SDL.SDL_WINDOWPOS_CENTERED,
                SDL.SDL_WINDOWPOS_CENTERED,
                GraphicsDeviceManager.DefaultBackBufferWidth,
                GraphicsDeviceManager.DefaultBackBufferHeight,
                INTERNAL_sdlWindowFlags_Next | SDL.SDL_WindowFlags.SDL_WINDOW_HIDDEN
            );
            INTERNAL_SetIcon(dummyWindow, title);

            IntPtr dummyContext = SDL.SDL_GL_CreateContext(dummyWindow);
            OpenTK.Graphics.GraphicsContext.CurrentContext = dummyContext;
            OpenTK.Graphics.OpenGL.GL.LoadAll();

            // A window, then a context were created, then .LoadAll(), so OpenGL can now be used.
            string versionString = OpenTK.Graphics.OpenGL.GL.GetString(OpenTK.Graphics.OpenGL.StringName.Version);
            string extensionsString = OpenTK.Graphics.OpenGL.GL.GetString(OpenTK.Graphics.OpenGL.StringName.Extensions);

            // Despose the temporary window/context (they were ONLY needed to run GL.GetString()).
            SDL.SDL_DestroyWindow(dummyWindow);
            SDL.SDL_GL_DeleteContext(dummyContext);

            if (versionString.Contains(" "))
            {
                // If the version string contains additional text after the actual number, remove it here.
                versionString = versionString.Substring(0, versionString.IndexOf(' '));
            }
            Version openGlVersion = new Version(versionString);

            List<String> unsupportedExtensions = new List<string>();
            for (int i = 0; i < INTERNAL_config.OpenGLRequiredExtensions.Count; i++)
            {
                if (!extensionsString.Contains(INTERNAL_config.OpenGLRequiredExtensions[i]))
                {
                    unsupportedExtensions.Add(INTERNAL_config.OpenGLRequiredExtensions[i]);
                }
            }

            if (openGlVersion < INTERNAL_config.OpenGLMinVersion || unsupportedExtensions.Count > 0)
            {
                // Use ExceptionGame to display this exception to the user.
                string errorMessage = "Sorry! Your computer cannot run this game.\n\n" +
                                            "This game requires OpenGL version " +
                                            INTERNAL_config.OpenGLMinVersion + " or better,\n" +
                                            "but your graphics card only has version " + versionString + "\n\n\n";

                if (unsupportedExtensions.Count > 0 && openGlVersion >= INTERNAL_config.OpenGLMinVersion)
                {
                    errorMessage = "Sorry! Your computer cannot run this game.\n\n" +
                                            "The following required extension(s) are not supported by your graphics card:\n\n";

                    for (int i = 0; i < unsupportedExtensions.Count; i++)
                    {
                        errorMessage += unsupportedExtensions[i];

                        if (i < unsupportedExtensions.Count - 1)
                        {
                            errorMessage += ",\n";
                        }
                    }
                }
                PlatformNotSupportedException openGlVersionException = new PlatformNotSupportedException(errorMessage);
                throw openGlVersionException;
            }
            #endregion

			INTERNAL_sdlWindow = SDL.SDL_CreateWindow(
				title,
				SDL.SDL_WINDOWPOS_CENTERED,
				SDL.SDL_WINDOWPOS_CENTERED,
				GraphicsDeviceManager.DefaultBackBufferWidth,
				GraphicsDeviceManager.DefaultBackBufferHeight,
				INTERNAL_sdlWindowFlags_Next
			);
			INTERNAL_SetIcon(INTERNAL_sdlWindow, title);

			INTERNAL_sdlWindowFlags_Current = INTERNAL_sdlWindowFlags_Next;
		}

		#endregion

		#region Public GameWindow Methods

		public override void BeginScreenDeviceChange(bool willBeFullScreen)
		{
			// Fullscreen windowflag
			if (willBeFullScreen)
			{
				INTERNAL_sdlWindowFlags_Next |= SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP;
			}
			else
			{
				INTERNAL_sdlWindowFlags_Next &= ~SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP;
			}
		}

		public override void EndScreenDeviceChange(
			string screenDeviceName,
			int clientWidth,
			int clientHeight
		) {
			// Set screen device name, not that we use it...
			INTERNAL_deviceName = screenDeviceName;

			// Fullscreen (Note: this only reads the fullscreen flag)
			SDL.SDL_SetWindowFullscreen(INTERNAL_sdlWindow, (uint) INTERNAL_sdlWindowFlags_Next);

			// Bordered
			SDL.SDL_SetWindowBordered(
				INTERNAL_sdlWindow,
				IsBorderless ? SDL.SDL_bool.SDL_FALSE : SDL.SDL_bool.SDL_TRUE
			);

			/* Because Mac windows resizes from the bottom, we have to get the position before changing
			 * the size so we can keep the window centered when resizing in windowed mode.
			 */
			int prevX = 0;
			int prevY = 0;
			if ((INTERNAL_sdlWindowFlags_Next & SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP) == 0)
			{
				SDL.SDL_GetWindowPosition(INTERNAL_sdlWindow, out prevX, out prevY);
			}

			// Window bounds
			SDL.SDL_SetWindowSize(INTERNAL_sdlWindow, clientWidth, clientHeight);

			// Window position
			if (	(INTERNAL_sdlWindowFlags_Current & SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP) == SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP &&
				(INTERNAL_sdlWindowFlags_Next & SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP) == 0	)
			{
				// If exiting fullscreen, just center the window on the desktop.
				SDL.SDL_SetWindowPosition(
					INTERNAL_sdlWindow,
					SDL.SDL_WINDOWPOS_CENTERED,
					SDL.SDL_WINDOWPOS_CENTERED
				);
			}
			else if ((INTERNAL_sdlWindowFlags_Next & SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP) == 0)
			{
				SDL.SDL_SetWindowPosition(
					INTERNAL_sdlWindow,
					prevX + ((OpenGLDevice.Instance.Backbuffer.Width - clientWidth) / 2),
					prevY + ((OpenGLDevice.Instance.Backbuffer.Height - clientHeight) / 2)
				);
			}

			// Current flags have just been updated.
			INTERNAL_sdlWindowFlags_Current = INTERNAL_sdlWindowFlags_Next;

			// Now, update the viewport
			Game.GraphicsDevice.Viewport = new Viewport(
				0,
				0,
				clientWidth,
				clientHeight
			);

			// Update the scissor rectangle to our new default target size
			Game.GraphicsDevice.ScissorRectangle = new Rectangle(
				0,
				0,
				clientWidth,
				clientHeight
			);

			OpenGLDevice.Instance.Backbuffer.ResetFramebuffer(
				clientWidth,
				clientHeight,
				Game.GraphicsDevice.PresentationParameters.DepthStencilFormat
			);
		}

		#endregion

		#region Internal Methods

		internal void INTERNAL_ClientSizeChanged()
		{
			OnClientSizeChanged();
		}

		#endregion

		#region Protected GameWindow Methods

		protected internal override void SetSupportedOrientations(DisplayOrientation orientations)
		{
			// No-op. SDL2 has no orientation.
		}

		protected override void SetTitle(string title)
		{
			SDL.SDL_SetWindowTitle(
				INTERNAL_sdlWindow,
				title
			);
		}

		#endregion

		#region Private Window Icon Method

		private void INTERNAL_SetIcon(IntPtr Window, string title)
		{
			string fileIn = String.Empty;

			/* If the game's using SDL2_image, provide the option to use a PNG
			 * instead of a BMP. Nice for anyone who cares about transparency.
			 * -flibit
			 */
			try
			{
				fileIn = INTERNAL_GetIconName(title, ".png");
				if (!String.IsNullOrEmpty(fileIn))
				{
					IntPtr icon = SDL_image.IMG_Load(fileIn);
                    SDL.SDL_SetWindowIcon(Window, icon);
					SDL.SDL_FreeSurface(icon);
					return;
				}
			}
			catch(DllNotFoundException)
			{
				// Not that big a deal guys.
			}

			fileIn = INTERNAL_GetIconName(title, ".bmp");
			if (!String.IsNullOrEmpty(fileIn))
			{
				IntPtr icon = SDL.SDL_LoadBMP(fileIn);
                SDL.SDL_SetWindowIcon(Window, icon);
				SDL.SDL_FreeSurface(icon);
			}
		}

		#endregion

		#region Private Static Icon Filename Method

		private static string INTERNAL_GetIconName(string title, string extension)
		{
			string fileIn = String.Empty;
			if (System.IO.File.Exists(title + extension))
			{
				// If the title and filename work, it just works. Fine.
				fileIn = title + extension;
			}
			else
			{
				// But sometimes the title has invalid characters inside.

				/* In addition to the filesystem's invalid charset, we need to
				 * blacklist the Windows standard set too, no matter what.
				 * -flibit
				 */
				char[] hardCodeBadChars = new char[]
				{
					'<',
					'>',
					':',
					'"',
					'/',
					'\\',
					'|',
					'?',
					'*'
				};
				List<char> badChars = new List<char>();
				badChars.AddRange(System.IO.Path.GetInvalidFileNameChars());
				badChars.AddRange(hardCodeBadChars);

				string stripChars = title;
				foreach (char c in badChars)
				{
					stripChars = stripChars.Replace(c.ToString(), "");
				}
				stripChars += extension;

				if (System.IO.File.Exists(stripChars))
				{
					fileIn = stripChars;
				}
			}
			return fileIn;
		}

		#endregion
	}
}
