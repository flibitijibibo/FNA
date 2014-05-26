#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
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
#if WIIU_GAMEPAD
using System.Runtime.InteropServices;
#endif

using SDL2;

using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
#endregion

namespace Microsoft.Xna.Framework
{
	class SDL2_GamePlatform : GamePlatform
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

		#region Public GamePlatform Properties

		public override GameRunBehavior DefaultRunBehavior
		{
			get
			{
				return GameRunBehavior.Synchronous;
			}
		}

		#endregion

		#region Private Window/GLContext Variables

		private IntPtr INTERNAL_GLContext;

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

		#region Private DisplayMode List

		private DisplayModeCollection supportedDisplayModes = null;

		#endregion

		#region Public Constructor

		public SDL2_GamePlatform(Game game) : base(game, SDL.SDL_GetPlatform())
		{
			/* SDL2 might complain if an OS that uses SDL_main has not actually
			 * used SDL_main by the time you initialize SDL2.
			 * The only platform that is affected is Windows, but we can skip
			 * their WinMain. This was only added to prevent iOS from exploding.
			 * -flibit
			 */
			SDL.SDL_SetMainReady();

			// This _should_ be the first real SDL call we make...
			SDL.SDL_Init(SDL.SDL_INIT_VIDEO);

			// Set and initialize the SDL2 window
			Window = new SDL2_GameWindow(game);

			// Disable the screensaver.
			SDL.SDL_DisableScreenSaver();

			// We hide the mouse cursor by default.
			if (IsMouseVisible)
			{
				IsMouseVisible = false;
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
			if (OSVersion.Equals("Mac OS X"))
			{
				string hint = SDL.SDL_GetHint(SDL.SDL_HINT_VIDEO_MAC_FULLSCREEN_SPACES);
				INTERNAL_useFullscreenSpaces = (String.IsNullOrEmpty(hint) || hint.Equals("1"));
			}
			else
			{
				INTERNAL_useFullscreenSpaces = false;
			}

			// Create OpenGL context
			INTERNAL_GLContext = SDL.SDL_GL_CreateContext(Window.Handle);
			OpenTK.Graphics.GraphicsContext.CurrentContext = INTERNAL_GLContext;

#if THREADED_GL
			// Create a background context
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_SHARE_WITH_CURRENT_CONTEXT, 1);
			Threading.WindowInfo = Window.Handle;
			Threading.BackgroundContext = new GL_ContextHandle()
			{
				context = SDL.SDL_GL_CreateContext(Window.Handle)
			};

			// Make the foreground context current.
			SDL.SDL_GL_MakeCurrent(Window.Handle, INTERNAL_GLContext);
#endif

			// Set up the OpenGL Device. Loads entry points.
			OpenGLDevice.Initialize();

			// Create the OpenAL device
			OpenALDevice.Initialize();

			// Initialize Active Key List
			keys = new List<Keys>();

			// Setup Text Input Control Character Arrays (Only 4 control keys supported at this time)
			INTERNAL_TextInputControlDown = new bool[4];
			INTERNAL_TextInputControlRepeat = new int[4];

			// Assume we will have focus.
			IsActive = true;

			// Ready to run the loop!
			INTERNAL_runApplication = true;

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
			wiiuPixelData = new byte[
				OpenGLDevice.Instance.Backbuffer.Width *
				OpenGLDevice.Instance.Backbuffer.Height *
				4
			];
#endif
		}

		#endregion

		#region Public GamePlatform Methods

		public override void RunLoop()
		{
			SDL.SDL_ShowWindow(Window.Handle);

			SDL.SDL_Event evt;

			while (INTERNAL_runApplication)
			{
#if !THREADED_GL
				Threading.Run();
#endif

				// Reset the mouse button forcing flag each frame
				Mouse.INTERNAL_ForceLeftButton = false;

				/* To properly set the ForceLeftButton flag, we track whether we've seen
				 * both a left mouse button down and left mouse button up event in the
				 * same frame.
				 */
				bool leftMouseDown = false;
				bool leftMouseUp = false;

				while (SDL.SDL_PollEvent(out evt) == 1)
				{
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
						if (keys.Remove(key))
						{
							INTERNAL_TextInputOut(key);
						}
					}

					// Left mouse button tracking for ForceLeftButton flag
					else if (evt.type == SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN)
					{
						if (evt.button.button == SDL.SDL_BUTTON_LEFT)
						{
							leftMouseDown = true;
						}
					}
					else if (evt.type == SDL.SDL_EventType.SDL_MOUSEBUTTONUP)
					{
						if (evt.button.button == SDL.SDL_BUTTON_LEFT)
						{
							leftMouseUp = true;
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
							IsActive = true;

							if (!INTERNAL_useFullscreenSpaces)
							{
								// If we alt-tab away, we lose the 'fullscreen desktop' flag on some WMs
								SDL.SDL_SetWindowFullscreen(
									Window.Handle,
									Game.GraphicsDevice.PresentationParameters.IsFullScreen ?
										(uint) SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP :
										0
								);
							}

							// Disable the screensaver when we're back.
							SDL.SDL_DisableScreenSaver();
						}
						else if (evt.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_LOST)
						{
							IsActive = false;

							if (!INTERNAL_useFullscreenSpaces)
							{
								SDL.SDL_SetWindowFullscreen(Window.Handle, 0);
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
							((SDL2_GameWindow) Window).INTERNAL_ClientSizeChanged();
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
						// 120 units per notch. Because reasons.
						Mouse.INTERNAL_MouseWheel += evt.wheel.y * 120;
					}

					// Controller device management
					else if (evt.type == SDL.SDL_EventType.SDL_JOYDEVICEADDED)
					{
						GamePad.INTERNAL_AddInstance(evt.jdevice.which);
					}
					else if (evt.type == SDL.SDL_EventType.SDL_JOYDEVICEREMOVED)
					{
						GamePad.INTERNAL_RemoveInstance(evt.jdevice.which);
					}

					// Text Input
					else if (evt.type == SDL.SDL_EventType.SDL_TEXTINPUT && !INTERNAL_TextInputSuppress)
					{
						string text;
						unsafe { text = new string((char*) evt.text.text); }
						if (text.Length > 0)
						{
							Game.OnTextInput(evt, new TextInputEventArgs(text[0]));
						}
					}

					// Quit
					else if (evt.type == SDL.SDL_EventType.SDL_QUIT)
					{
						INTERNAL_runApplication = false;
						break;
					}
				}

				// If both a down and up event were sent in one frame, force the mouse left button for the next frame.
				Mouse.INTERNAL_ForceLeftButton = leftMouseDown && leftMouseUp;

				// Text Input Controls Key Handling
				INTERNAL_TextInputUpdate();

				Keyboard.SetKeys(keys);
				Game.Tick();
			}

			// We out.
			Game.Exit();
		}

		public override void StartRunLoop()
		{
			throw new NotSupportedException();
		}

		public override void Exit()
		{
			// Stop the game loop
			INTERNAL_runApplication = false;

			// Close SDL2_mixer if needed
			Media.Song.closeMixer();
		}

		public override bool BeforeUpdate(GameTime gameTime)
		{
			// Update our OpenAL context
			OpenALDevice.Instance.Update();

			return true;
		}

		public override bool BeforeDraw(GameTime gameTime)
		{
			return true;
		}

		public override void BeginScreenDeviceChange(bool willBeFullScreen)
		{
			Window.BeginScreenDeviceChange(willBeFullScreen);
		}

		public override void EndScreenDeviceChange(string screenDeviceName, int clientWidth, int clientHeight)
		{
			Window.EndScreenDeviceChange(screenDeviceName, clientWidth, clientHeight);

#if WIIU_GAMEPAD
			wiiuPixelData = new byte[clientWidth * clientHeight * 4];
#endif
		}

		public override void Log(string Message)
		{
			Console.WriteLine(Message);
		}

		public override void Present()
		{
			base.Present();

			GraphicsDevice device = Game.GraphicsDevice;
			if (device != null)
			{
				device.Present();
			}

			if (Window != null)
			{
				int windowWidth, windowHeight;
				SDL.SDL_GetWindowSize(Window.Handle, out windowWidth, out windowHeight);
				OpenGLDevice.Framebuffer.BlitToBackbuffer(
					OpenGLDevice.Instance.Backbuffer.Width,
					OpenGLDevice.Instance.Backbuffer.Height,
					windowWidth,
					windowHeight
				);
				SDL.SDL_GL_SwapWindow(Window.Handle);
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
		}

		#endregion

		#region Internal GameWindow Methods

		internal override DisplayMode GetCurrentDisplayMode()
		{
			SDL.SDL_DisplayMode mode;
			SDL.SDL_GetCurrentDisplayMode(0, out mode);
			return new DisplayMode(
				mode.w,
				mode.h,
				SurfaceFormat.Color
			);
		}

		internal override DisplayModeCollection GetDisplayModes()
		{
			if (supportedDisplayModes == null)
			{
				List<DisplayMode> modes = new List<DisplayMode>(new DisplayMode[] { GetCurrentDisplayMode(), });
				SDL.SDL_DisplayMode filler = new SDL.SDL_DisplayMode();
				int numModes = SDL.SDL_GetNumDisplayModes(0);
				for (int i = 0; i < numModes; i += 1)
				{
					SDL.SDL_GetDisplayMode(0, i, out filler);

					// Check for dupes caused by varying refresh rates.
					bool dupe = false;
					foreach (DisplayMode mode in modes)
					{
						if (filler.w == mode.Width && filler.h == mode.Height)
						{
							dupe = true;
						}
					}
					if (!dupe)
					{
						modes.Add(
							new DisplayMode(
								filler.w,
								filler.h,
								SurfaceFormat.Color // FIXME: Assumption!
							)
						);
					}
				}
				supportedDisplayModes = new DisplayModeCollection(modes);
			}
			return supportedDisplayModes;
		}

		internal override void SetPresentationInterval(PresentInterval interval)
		{
			if (interval == PresentInterval.Default || interval == PresentInterval.One)
			{
				if (OSVersion.Equals("Mac OS X"))
				{
					// Apple is a big fat liar about swap_control_tear. Use stock VSync.
					SDL.SDL_GL_SetSwapInterval(1);
				}
				else
				{
					if (SDL.SDL_GL_SetSwapInterval(-1) != -1)
					{
						System.Console.WriteLine("Using EXT_swap_control_tear VSync!");
					}
					else
					{
						System.Console.WriteLine("EXT_swap_control_tear unsupported. Fall back to standard VSync.");
						SDL.SDL_ClearError();
						SDL.SDL_GL_SetSwapInterval(1);
					}
				}
			}
			else if (interval == PresentInterval.Immediate)
			{
				SDL.SDL_GL_SetSwapInterval(0);
			}
			else if (interval == PresentInterval.Two)
			{
				SDL.SDL_GL_SetSwapInterval(2);
			}
			else
			{
				throw new Exception("Unrecognized PresentInterval!");
			}
		}

		#endregion

		#region Protected GameWindow Methods

		protected override void OnIsMouseVisibleChanged()
		{
			SDL.SDL_ShowCursor(IsMouseVisible ? 1 : 0);
		}

		protected override void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				if (Window != null)
				{
					OpenGLDevice.Instance.Dispose();

#if THREADED_GL
					SDL.SDL_GL_DeleteContext(Threading.BackgroundContext.context);
#endif
					SDL.SDL_GL_DeleteContext(INTERNAL_GLContext);

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

					SDL.SDL_DestroyWindow(Window.Handle);

					Window = null;
				}

				if (OpenALDevice.Instance != null)
				{
					OpenALDevice.Instance.Dispose();
				}

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

			base.Dispose(disposing);
		}

		#endregion

		#region Private TextInput Methods

		private void INTERNAL_TextInputIn(Keys key)
		{
			if (key == Keys.Back)
			{
				INTERNAL_TextInputControlDown[0] = true;
				INTERNAL_TextInputControlRepeat[0] = Environment.TickCount + 400;
				Game.OnTextInput(null, new TextInputEventArgs((char)8)); // Backspace
			}
			else if (key == Keys.Tab)
			{
				INTERNAL_TextInputControlDown[1] = true;
				INTERNAL_TextInputControlRepeat[1] = Environment.TickCount + 400;
				Game.OnTextInput(null, new TextInputEventArgs((char)9)); // Tab
			}
			else if (key == Keys.Enter)
			{
				INTERNAL_TextInputControlDown[2] = true;
				INTERNAL_TextInputControlRepeat[2] = Environment.TickCount + 400;
				Game.OnTextInput(null, new TextInputEventArgs((char)13)); // Enter
			}
			else if (keys.Contains(Keys.LeftControl) && key == Keys.V) // Control-V Pasting support
			{
				INTERNAL_TextInputControlDown[3] = true;
				INTERNAL_TextInputControlRepeat[3] = Environment.TickCount + 400;
				Game.OnTextInput(null, new TextInputEventArgs((char)22)); // Control-V (Paste)
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
				Game.OnTextInput(null, new TextInputEventArgs((char)8));
			}
			if (INTERNAL_TextInputControlDown[1] && INTERNAL_TextInputControlRepeat[1] <= Environment.TickCount)
			{
				Game.OnTextInput(null, new TextInputEventArgs((char)9));
			}
			if (INTERNAL_TextInputControlDown[2] && INTERNAL_TextInputControlRepeat[2] <= Environment.TickCount)
			{
				Game.OnTextInput(null, new TextInputEventArgs((char)13));
			}
			if (INTERNAL_TextInputControlDown[3] && INTERNAL_TextInputControlRepeat[3] <= Environment.TickCount)
			{
				Game.OnTextInput(null, new TextInputEventArgs((char)22));
			}
		}

		#endregion
	}
}
