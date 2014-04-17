#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

#region RESIZABLE_WINDOW Option
// #define RESIZABLE_WINDOW
/* So we've got this silly issue in SDL2's video API at the moment. We can't
 * add/remove the resizable property to the SDL_Window*!
 *
 * So, if you want to have your GameWindow be resizable, uncomment this define.
 * -flibit
 */
#endregion

#region THREADED_GL Option
// #define THREADED_GL
/* Ah, so I see you've run into some issues with threaded GL...
 * 
 * We use Threading.cs to handle rendering coming from multiple threads, but if
 * you're too wreckless with how many threads are calling the GL, this will
 * hang.
 *
 * With THREADED_GL we instead allow you to run threaded rendering using
 * multiple GL contexts. This is more flexible, but much more dangerous.
 *
 * Also note that this affects Threading.cs! Check THREADED_GL there too.
 *
 * Basically, if you have to enable this, you should feel very bad.
 * -flibit
 */
#endregion

#region WIIU_GAMEPAD Option
// #define WIIU_GAMEPAD
/* This is something I added for myself, because I am a complete goof.
 * You should NEVER enable this in your shipping build.
 * Let your hacker customers self-build MG-SDL2, they'll know what to do.
 * -flibit
 */
#endregion

#region Using Statements
using System;
using System.Collections.Generic;
using System.ComponentModel;

#if WIIU_GAMEPAD
using System.Runtime.InteropServices;
#endif

using SDL2;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
#endregion

namespace Microsoft.Xna.Framework
{
	public class SDL2_GameWindow : GameWindow
	{
		#region Wii U GamePad Support, libdrc Interop

#if WIIU_GAMEPAD
		private static class DRC
		{
			// FIXME: Deal with Mac/Windows LibName later.
			private const string nativeLibName = "libdrc.so";

			public enum drc_pixel_format
			{
				DRC_RGB,
				DRC_RGBA,
				DRC_BGR,
				DRC_BGRA,
				DRC_RGB565
			}

			public enum drc_flipping_mode
			{
				DRC_NO_FLIP,
				DRC_FLIP_VERTICALLY
			}

			/* IntPtr refers to a drc_streamer* */
			[DllImportAttribute(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
			public static extern IntPtr drc_new_streamer();

			/* self refers to a drc_streamer* */
			[DllImportAttribute(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
			public static extern void drc_delete_streamer(IntPtr self);

			/* self refers to a drc_streamer* */
			[DllImportAttribute(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
			public static extern int drc_start_streamer(IntPtr self);

			/* self refers to a drc_streamer* */
			[DllImportAttribute(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
			public static extern void drc_stop_streamer(IntPtr self);

			/* self refers to a drc_streamer* */
			[DllImportAttribute(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
			public static extern int drc_push_vid_frame(
				IntPtr self,
				byte[] buffer,
				uint size,
				ushort width,
				ushort height,
				drc_pixel_format pixfmt,
				drc_flipping_mode flipmode
			);

			/* self refers to a drc_streamer* */
			[DllImportAttribute(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
			public static extern void drc_enable_system_input_feeder(IntPtr self);
		}

		private IntPtr wiiuStream;
		private byte[] wiiuPixelData;
#endif

		#endregion

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

		private IntPtr INTERNAL_GLContext;

		private string INTERNAL_deviceName;

		private bool INTERNAL_useFullscreenSpaces;

		#endregion

		#region Private Game Loop Sentinel

		private bool INTERNAL_runApplication;

		#endregion

		#region Private Active XNA Key List

		private List<Keys> keys;

		#endregion

		#region Private Text Input Variables

		private int[] INTERNAL_TextInputControlRepeat;
		private bool[] INTERNAL_TextInputControlDown;
		private bool INTERNAL_TextInputSuppress;

		#endregion

		#region Internal Constructor

		internal SDL2_GameWindow(Game game, SDL2_GamePlatform platform)
		{
			Game = game;

			int startWidth = GraphicsDeviceManager.DefaultBackBufferWidth;
			int startHeight = GraphicsDeviceManager.DefaultBackBufferHeight;

			/* SDL2 might complain if an OS that uses SDL_main has not actually
			 * used SDL_main by the time you initialize SDL2.
			 * The only platform that is affected is Windows, but we can skip
			 * their WinMain. This was only added to prevent iOS from exploding.
			 * -flibit
			 */
			SDL.SDL_SetMainReady();

			// This _should_ be the first SDL call we make...
			SDL.SDL_Init(SDL.SDL_INIT_VIDEO);

			INTERNAL_runApplication = true;

			// Initialize Active Key List
			keys = new List<Keys>();

			INTERNAL_sdlWindowFlags_Next = (
				SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL |
				SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN |
				SDL.SDL_WindowFlags.SDL_WINDOW_INPUT_FOCUS |
				SDL.SDL_WindowFlags.SDL_WINDOW_MOUSE_FOCUS
			);
#if RESIZABLE_WINDOW
			AllowUserResizing = true;
#else
			AllowUserResizing = false;
#endif

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

			INTERNAL_sdlWindow = SDL.SDL_CreateWindow(
				"MonoGame-SDL2 Window",
				SDL.SDL_WINDOWPOS_CENTERED,
				SDL.SDL_WINDOWPOS_CENTERED,
				startWidth,
				startHeight,
				INTERNAL_sdlWindowFlags_Next
			);

			INTERNAL_sdlWindowFlags_Current = INTERNAL_sdlWindowFlags_Next;

			// Disable the screensaver.
			SDL.SDL_DisableScreenSaver();

			// We hide the mouse cursor by default.
			if (platform.IsMouseVisible)
			{
				platform.IsMouseVisible = false;
			}
			else
			{
				/* Since IsMouseVisible is already false, OnMouseVisibleChanged
				 * will NOT be called! So we get to do it ourselves.
				 * -flibit
				 */
				SDL.SDL_ShowCursor(0);
			}

			// OSX has some fancy fullscreen features, let's use them!
			if (SDL2_GamePlatform.OSVersion.Equals("Mac OS X"))
			{
				string hint = SDL.SDL_GetHint(SDL.SDL_HINT_VIDEO_MAC_FULLSCREEN_SPACES);
				INTERNAL_useFullscreenSpaces = (String.IsNullOrEmpty(hint) || hint.Equals("1"));
			}
			else
			{
				INTERNAL_useFullscreenSpaces = false;
			}

			// Initialize OpenGL
			INTERNAL_GLContext = SDL.SDL_GL_CreateContext(INTERNAL_sdlWindow);
			OpenTK.Graphics.GraphicsContext.CurrentContext = INTERNAL_GLContext;

			// Assume we will have focus.
			platform.IsActive = true;

#if THREADED_GL
			// Create a background context
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_SHARE_WITH_CURRENT_CONTEXT, 1);
			Threading.WindowInfo = INTERNAL_sdlWindow;
			Threading.BackgroundContext = new GL_ContextHandle()
			{
				context = SDL.SDL_GL_CreateContext(INTERNAL_sdlWindow)
			};

			// Make the foreground context current.
			SDL.SDL_GL_MakeCurrent(INTERNAL_sdlWindow, INTERNAL_GLContext);
#endif

			// Set up the OpenGL Device. Loads entry points.
			new OpenGLDevice();

			// Setup Text Input Control Character Arrays (Only 4 control keys supported at this time)
			INTERNAL_TextInputControlDown = new bool[4];
			INTERNAL_TextInputControlRepeat = new int[4];

#if WIIU_GAMEPAD
			wiiuStream = DRC.drc_new_streamer();
			if (wiiuStream == IntPtr.Zero)
			{
				System.Console.WriteLine("Failed to alloc GamePad stream!");
				return;
			}
			if (DRC.drc_start_streamer(wiiuStream) < 1) // ???
			{
				System.Console.WriteLine("Failed to start GamePad stream!");
				DRC.drc_delete_streamer(wiiuStream);
				wiiuStream = IntPtr.Zero;
				return;
			}
			DRC.drc_enable_system_input_feeder(wiiuStream);
			wiiuPixelData = new byte[startWidth * startHeight * 4];
#endif
		}

		#endregion

		#region Internal GamePlatform Interaction Methods

		internal void INTERNAL_RunLoop()
		{
			SDL.SDL_Event evt;

			while (INTERNAL_runApplication)
			{
#if !THREADED_GL
				Threading.Run();
#endif
				while (SDL.SDL_PollEvent(out evt) == 1)
				{
					// TODO: All events...

					// Keyboard
					if (evt.type == SDL.SDL_EventType.SDL_KEYDOWN)
					{
						Keys key = SDL2_KeyboardUtil.ToXNA(evt.key.keysym.scancode);
						if (!keys.Contains(key))
						{
							keys.Add(key);
							INTERNAL_TextInputIn(key);
						}
					}
					else if (evt.type == SDL.SDL_EventType.SDL_KEYUP)
					{
						Keys key = SDL2_KeyboardUtil.ToXNA(evt.key.keysym.scancode);
						if (keys.Contains(key))
						{
							keys.Remove(key);
							INTERNAL_TextInputOut(key);
						}
					}

					// Mouse Motion Event
					else if (evt.type == SDL.SDL_EventType.SDL_MOUSEMOTION)
					{
						Mouse.INTERNAL_IsWarped = false;
					}

					// Various Window Events...
					else if (evt.type == SDL.SDL_EventType.SDL_WINDOWEVENT)
					{
						// Window Focus
						if (evt.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_GAINED)
						{
							Game.Platform.IsActive = true;

							if (!INTERNAL_useFullscreenSpaces)
							{
								// If we alt-tab away, we lose the 'fullscreen desktop' flag on some WMs
								SDL.SDL_SetWindowFullscreen(INTERNAL_sdlWindow, (uint) INTERNAL_sdlWindowFlags_Current);
							}

							// Disable the screensaver when we're back.
							SDL.SDL_DisableScreenSaver();
						}
						else if (evt.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_LOST)
						{
							Game.Platform.IsActive = false;

							if (!INTERNAL_useFullscreenSpaces)
							{
								SDL.SDL_SetWindowFullscreen(INTERNAL_sdlWindow, 0);
							}

							// Give the screensaver back, we're not that important now.
							SDL.SDL_EnableScreenSaver();
						}

						// Window Resize
						else if (evt.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_RESIZED)
						{
							Mouse.INTERNAL_WindowWidth = evt.window.data1;
							Mouse.INTERNAL_WindowHeight = evt.window.data2;

							// Should be called on user resize only, NOT ApplyChanges!
							OnClientSizeChanged();
						}
						else if (evt.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_SIZE_CHANGED)
						{
							Mouse.INTERNAL_WindowWidth = evt.window.data1;
							Mouse.INTERNAL_WindowHeight = evt.window.data2;
						}

						// Mouse Focus
						else if (evt.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_ENTER)
						{
							SDL.SDL_DisableScreenSaver();
						}
						else if (evt.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_LEAVE)
						{
							SDL.SDL_EnableScreenSaver();
						}
					}

					// Mouse Wheel
					else if (evt.type == SDL.SDL_EventType.SDL_MOUSEWHEEL)
					{
						Mouse.INTERNAL_MouseWheel += evt.wheel.y;
					}

					// Controller device management
					else if (evt.type == SDL.SDL_EventType.SDL_JOYDEVICEADDED)
					{
						Input.GamePad.INTERNAL_AddInstance(evt.jdevice.which);
					}
					else if (evt.type == SDL.SDL_EventType.SDL_JOYDEVICEREMOVED)
					{
						Input.GamePad.INTERNAL_RemoveInstance(evt.jdevice.which);
					}

					// Text Input
					else if (evt.type == SDL.SDL_EventType.SDL_TEXTINPUT && !INTERNAL_TextInputSuppress)
					{
						string text;
						unsafe { text = new string((char*) evt.text.text); }
						if (text.Length > 0)
						{
							OnTextInput(evt, new TextInputEventArgs(text[0]));
						}
					}

					// Quit
					else if (evt.type == SDL.SDL_EventType.SDL_QUIT)
					{
						INTERNAL_runApplication = false;
						break;
					}
				}
				// Text Input Controls Key Handling
				INTERNAL_TextInputUpdate();

				if (keys.Contains(Keys.LeftAlt) && keys.Contains(Keys.F4))
				{
					INTERNAL_runApplication = false;
				}

				Keyboard.SetKeys(keys);
				Game.Tick();
			}

			// We out.
			Game.Exit();
		}

		internal void INTERNAL_SwapBuffers()
		{
			int windowWidth, windowHeight;
			SDL.SDL_GetWindowSize(INTERNAL_sdlWindow, out windowWidth, out windowHeight);
			OpenGLDevice.Framebuffer.BlitToBackbuffer(
				OpenGLDevice.Instance.Backbuffer.Width,
				OpenGLDevice.Instance.Backbuffer.Height,
				windowWidth,
				windowHeight
			);
			SDL.SDL_GL_SwapWindow(INTERNAL_sdlWindow);
			OpenGLDevice.Framebuffer.BindFramebuffer(OpenGLDevice.Instance.Backbuffer.Handle);

#if WIIU_GAMEPAD
			if (wiiuStream != IntPtr.Zero)
			{
				OpenTK.Graphics.OpenGL.GL.ReadPixels(
					0,
					0,
					OpenGLDevice.Instance.Backbuffer.Width,
					OpenGLDevice.Instance.Backbuffer.Height,
					OpenTK.Graphics.OpenGL.PixelFormat.Rgba,
					OpenTK.Graphics.OpenGL.PixelType.UnsignedByte,
					wiiuPixelData
				);
				DRC.drc_push_vid_frame(
					wiiuStream,
					wiiuPixelData,
					(uint) wiiuPixelData.Length,
					(ushort) Graphics.OpenGLDevice.Instance.Backbuffer.Width,
					(ushort) Graphics.OpenGLDevice.Instance.Backbuffer.Height,
					DRC.drc_pixel_format.DRC_RGBA,
					DRC.drc_flipping_mode.DRC_FLIP_VERTICALLY
				);
			}
#endif
		}

		internal void INTERNAL_StopLoop()
		{
			INTERNAL_runApplication = false;
		}

		internal void INTERNAL_Destroy()
		{
			/* Some window managers might try to minimize the window as we're
			 * destroying it. This looks pretty stupid and could cause problems,
			 * so set this hint right before we destroy everything.
			 * -flibit
			 */
			SDL.SDL_SetHintWithPriority(
				SDL.SDL_HINT_VIDEO_MINIMIZE_ON_FOCUS_LOSS,
				"0",
				SDL.SDL_HintPriority.SDL_HINT_OVERRIDE
			);

			OpenGLDevice.Instance.Dispose();

#if THREADED_GL
			SDL.SDL_GL_DeleteContext(Threading.BackgroundContext.context);
#endif

			SDL.SDL_GL_DeleteContext(INTERNAL_GLContext);

			SDL.SDL_DestroyWindow(INTERNAL_sdlWindow);

#if WIIU_GAMEPAD
			if (wiiuStream != IntPtr.Zero)
			{
				DRC.drc_stop_streamer(wiiuStream);
				DRC.drc_delete_streamer(wiiuStream);
				wiiuStream = IntPtr.Zero;
			}
#endif

			// This _should_ be the last SDL call we make...
			SDL.SDL_Quit();
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
				// Try to center the window around the old window position.
				int x = 0;
				int y = 0;
				SDL.SDL_GetWindowPosition(INTERNAL_sdlWindow, out x, out y);
				SDL.SDL_SetWindowPosition(
					INTERNAL_sdlWindow,
					x + ((OpenGLDevice.Instance.Backbuffer.Width - clientWidth) / 2),
					y + ((OpenGLDevice.Instance.Backbuffer.Height - clientHeight) / 2)
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

#if WIIU_GAMEPAD
			wiiuPixelData = new byte[clientWidth * clientHeight * 4];
#endif
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

			string fileIn = String.Empty;
			if (System.IO.File.Exists(title + ".bmp"))
			{
				// If the title and filename work, it just works. Fine.
				fileIn = title + ".bmp";
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
				stripChars += ".bmp";

				if (System.IO.File.Exists(stripChars))
				{
					fileIn = stripChars;
				}
			}

			if (!String.IsNullOrEmpty(fileIn))
			{
				IntPtr icon = SDL.SDL_LoadBMP(fileIn);
				SDL.SDL_SetWindowIcon(INTERNAL_sdlWindow, icon);
				SDL.SDL_FreeSurface(icon);
			}
		}

		#endregion

		#region Private TextInput Methods

		private void INTERNAL_TextInputIn(Keys key)
		{
			if (key == Keys.Back)
			{
				INTERNAL_TextInputControlDown[0] = true;
				INTERNAL_TextInputControlRepeat[0] = Environment.TickCount + 400;
				OnTextInput(null, new TextInputEventArgs((char)8)); // Backspace
			}
			else if (key == Keys.Tab)
			{
				INTERNAL_TextInputControlDown[1] = true;
				INTERNAL_TextInputControlRepeat[1] = Environment.TickCount + 400;
				OnTextInput(null, new TextInputEventArgs((char)9)); // Tab
			}
			else if (key == Keys.Enter)
			{
				INTERNAL_TextInputControlDown[2] = true;
				INTERNAL_TextInputControlRepeat[2] = Environment.TickCount + 400;
				OnTextInput(null, new TextInputEventArgs((char)13)); // Enter
			}
			else if (keys.Contains(Keys.LeftControl) && key == Keys.V) // Control-V Pasting support
			{
				INTERNAL_TextInputControlDown[3] = true;
				INTERNAL_TextInputControlRepeat[3] = Environment.TickCount + 400;
				OnTextInput(null, new TextInputEventArgs((char)22)); // Control-V (Paste)
				INTERNAL_TextInputSuppress = true;
			}
		}

		private void INTERNAL_TextInputOut(Keys key)
		{
			if (key == Keys.Back)
			{
				INTERNAL_TextInputControlDown[0] = false;
			}
			else if (key == Keys.Tab)
			{
				INTERNAL_TextInputControlDown[1] = false;
			}
			else if (key == Keys.Enter)
			{
				INTERNAL_TextInputControlDown[2] = false;
			}
			else if ((!keys.Contains(Keys.LeftControl) && INTERNAL_TextInputControlDown[3]) || key == Keys.V)
			{
				INTERNAL_TextInputControlDown[3] = false;
				INTERNAL_TextInputSuppress = false;
			}
		}

		private void INTERNAL_TextInputUpdate()
		{
			if (INTERNAL_TextInputControlDown[0] && INTERNAL_TextInputControlRepeat[0] <= Environment.TickCount)
			{
				OnTextInput(null, new TextInputEventArgs((char)8));
			}
			if (INTERNAL_TextInputControlDown[1] && INTERNAL_TextInputControlRepeat[1] <= Environment.TickCount)
			{
				OnTextInput(null, new TextInputEventArgs((char)9));
			}
			if (INTERNAL_TextInputControlDown[2] && INTERNAL_TextInputControlRepeat[2] <= Environment.TickCount)
			{
				OnTextInput(null, new TextInputEventArgs((char)13));
			}
			if (INTERNAL_TextInputControlDown[3] && INTERNAL_TextInputControlRepeat[3] <= Environment.TickCount)
			{
				OnTextInput(null, new TextInputEventArgs((char)22));
			}
		}

		#endregion
	}
}
