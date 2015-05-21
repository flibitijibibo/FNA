#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2015 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

namespace Microsoft.Xna.Framework.Input.Touch
{
	/// <summary>
	/// Allows retrieval of capabilities information from touch panel device.
	/// </summary>
	public struct TouchPanelCapabilities
	{
		#region Public Properties

		/// <summary>
		/// Returns true if a device is available for use.
		/// </summary>
		public bool IsConnected
		{
			get
			{
				return isConnected;
			}
		}

		/// <summary>
		/// Returns the maximum number of touch locations tracked by the touch panel device.
		/// </summary>
		public int MaximumTouchCount
		{
			get
			{
				return maximumTouchCount;
			}
		}

		#endregion

		#region Private Variables

		private bool isConnected;
		private int maximumTouchCount;
		private bool initialized;

		#endregion

		#region Internal Initialize Method

		internal void Initialize()
		{
			if (!initialized)
			{
				initialized = true;
				isConnected = Game.Instance.Platform.HasTouch();
				maximumTouchCount = 8; // FIXME: Assumption!
			}
		}

		#endregion
	}
}
