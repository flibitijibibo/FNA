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

using SDL2;
#endregion

namespace Microsoft.Xna.Framework.Input
{
	/// <summary>
	/// Allows reading position and button click information from mouse.
	/// </summary>
	public static class Mouse
	{
		#region Public Properties

		public static IntPtr WindowHandle
		{
			get
			{
				return PrimaryWindow.Handle;
			}
		}

		#endregion

		#region Internal Variables

		internal static GameWindow PrimaryWindow;

		internal static int INTERNAL_WindowWidth = 800;
		internal static int INTERNAL_WindowHeight = 600;

		internal static int INTERNAL_MouseWheel = 0;

		internal static bool INTERNAL_IsWarped = false;

		/* Many tap-to-click trackpads will wait until the user lifts their finger to send both
		 * a down and up event in quick succession. These cases make the polling nature of Mouse
		 * troublesome as these clicks are entirely missed. This flag allows the platform to force
		 * the left button to be marked as pressed in cases where both a down and up event are 
		 * seen on the same frame.
		 */
		internal static bool INTERNAL_ForceLeftButton = false;

		#endregion

		#region Public Interface

		/// <summary>
		/// Gets mouse state information that includes position and button
		/// presses for the provided window
		/// </summary>
		/// <returns>Current state of the mouse.</returns>
		public static MouseState GetState(GameWindow window)
		{
			int x, y;
			uint flags = SDL.SDL_GetMouseState(out x, out y);

			// Scale the mouse coordinates for the faux-backbuffer
			x = (int) ((double) x * Graphics.OpenGLDevice.Instance.Backbuffer.Width / INTERNAL_WindowWidth);
			y = (int) ((double) y * Graphics.OpenGLDevice.Instance.Backbuffer.Height / INTERNAL_WindowHeight);

			if (!INTERNAL_IsWarped)
			{
				// If we warped the mouse, we've already done this.
				window.MouseState.X = x;
				window.MouseState.Y = y;
			}

			window.MouseState.LeftButton =
				INTERNAL_ForceLeftButton ? ButtonState.Pressed : (ButtonState) (flags & SDL.SDL_BUTTON_LMASK);
			window.MouseState.MiddleButton =	(ButtonState) ((flags & SDL.SDL_BUTTON_MMASK) >> 1);
			window.MouseState.RightButton =		(ButtonState) ((flags & SDL.SDL_BUTTON_RMASK) >> 2);
			window.MouseState.XButton1 =		(ButtonState) ((flags & SDL.SDL_BUTTON_X1MASK) >> 3);
			window.MouseState.XButton2 =		(ButtonState) ((flags & SDL.SDL_BUTTON_X2MASK) >> 4);

			window.MouseState.ScrollWheelValue = INTERNAL_MouseWheel;

			return window.MouseState;
		}

		/// <summary>
		/// Gets mouse state information that includes position and button presses
		/// for the primary window
		/// </summary>
		/// <returns>Current state of the mouse.</returns>
		public static MouseState GetState()
		{
			return GetState(PrimaryWindow);
		}

		/// <summary>
		/// Sets mouse cursor's relative position to game-window.
		/// </summary>
		/// <param name="x">Relative horizontal position of the cursor.</param>
		/// <param name="y">Relative vertical position of the cursor.</param>
		public static void SetPosition(int x, int y)
		{
			// Scale the mouse coordinates for the faux-backbuffer
			x = (int) ((double) x * INTERNAL_WindowWidth / Graphics.OpenGLDevice.Instance.Backbuffer.Width);
			y = (int) ((double) y * INTERNAL_WindowHeight / Graphics.OpenGLDevice.Instance.Backbuffer.Height);

			PrimaryWindow.MouseState.X = x;
			PrimaryWindow.MouseState.Y = y;

			SDL.SDL_WarpMouseInWindow(WindowHandle, x, y);
			INTERNAL_IsWarped = true;
		}

		#endregion
	}
}

