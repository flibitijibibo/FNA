#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2015 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

#region Using Statements
using System;
#endregion

namespace Microsoft.Xna.Framework.Input.Touch
{
	/// <summary>
	/// Allows retrieval of information from Touch Panel device.
	/// </summary>
	public static class TouchPanel
	{
		#region Public Static Properties

		/// <summary>
		/// The window handle of the touch panel. Purely for Xna compatibility.
		/// </summary>
		public static IntPtr WindowHandle
		{
			get
			{
				return PrimaryWindow.TouchPanelState.WindowHandle;
			}
			set
			{
				PrimaryWindow.TouchPanelState.WindowHandle = value;
			}
		}

		/// <summary>
		/// Gets or sets the display height of the touch panel.
		/// </summary>
		public static int DisplayHeight
		{
			get
			{
				return PrimaryWindow.TouchPanelState.DisplayHeight;
			}
			set
			{
				PrimaryWindow.TouchPanelState.DisplayHeight = value;
			}
		}

		/// <summary>
		/// Gets or sets the display orientation of the touch panel.
		/// </summary>
		public static DisplayOrientation DisplayOrientation
		{
			get
			{
				return PrimaryWindow.TouchPanelState.DisplayOrientation;
			}
			set
			{
				PrimaryWindow.TouchPanelState.DisplayOrientation = value;
			}
		}

		/// <summary>
		/// Gets or sets the display width of the touch panel.
		/// </summary>
		public static int DisplayWidth
		{
			get
			{
				return PrimaryWindow.TouchPanelState.DisplayWidth;
			}
			set
			{
				PrimaryWindow.TouchPanelState.DisplayWidth = value;
			}
		}

		/// <summary>
		/// Gets or sets enabled gestures.
		/// </summary>
		public static GestureType EnabledGestures
		{
			get
			{
				return PrimaryWindow.TouchPanelState.EnabledGestures;
			}
			set
			{
				PrimaryWindow.TouchPanelState.EnabledGestures = value;
			}
		}

		public static bool EnableMouseTouchPoint
		{
			get
			{
				return PrimaryWindow.TouchPanelState.EnableMouseTouchPoint;
			}
			set
			{
				PrimaryWindow.TouchPanelState.EnableMouseTouchPoint = value;
			}
		}

		public static bool EnableMouseGestures
		{
			get
			{
				return PrimaryWindow.TouchPanelState.EnableMouseGestures;
			}
			set
			{
				PrimaryWindow.TouchPanelState.EnableMouseGestures = value;
			}
		}

		/// <summary>
		/// Returns true if a touch gesture is available.
		/// </summary>
		public static bool IsGestureAvailable
		{
			get
			{
				return PrimaryWindow.TouchPanelState.IsGestureAvailable;
			}
		}

		#endregion

		#region Internal Static Variables

		internal static GameWindow PrimaryWindow;

		#endregion

		#region Public Static Methods

		/// <summary>
		/// Gets the current state of the touch panel.
		/// </summary>
		/// <returns><see cref="TouchCollection"/></returns>
		public static TouchCollection GetState()
		{
			return PrimaryWindow.TouchPanelState.GetState();
		}

		public static TouchPanelState GetState(GameWindow window)
		{
			return window.TouchPanelState;
		}

		public static TouchPanelCapabilities GetCapabilities()
		{
			return PrimaryWindow.TouchPanelState.GetCapabilities();
		}

		/// <summary>
		/// Returns the next available gesture on touch panel device.
		/// </summary>
		/// <returns><see cref="GestureSample"/></returns>
		public static GestureSample ReadGesture()
		{
			return PrimaryWindow.TouchPanelState.GestureList.Dequeue();
		}

		#endregion

		#region Internal Static Methods

		internal static void AddEvent(
			int id,
			TouchLocationState state,
			Vector2 position
		) {
			AddEvent(id, state, position, false);
		}

		internal static void AddEvent(
			int id,
			TouchLocationState state,
			Vector2 position,
			bool isMouse
		) {
			PrimaryWindow.TouchPanelState.AddEvent(
				id,
				state,
				position,
				isMouse
			);
		}

		#endregion
	}
}
