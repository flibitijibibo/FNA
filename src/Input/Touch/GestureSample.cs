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
	/// Represents data from a multi-touch gesture over a span of time.
	/// </summary>
	public struct GestureSample
	{

		#region Public Properties

		/// <summary>
		/// Gets the type of the gesture.
		/// </summary>
		public GestureType GestureType
		{
			get
			{
				return gestureType;
			}
		}

		/// <summary>
		/// Gets the starting time for this multi-touch gesture sample.
		/// </summary>
		public TimeSpan Timestamp
		{
			get
			{
				return timestamp;
			}
		}

		/// <summary>
		/// Gets the position of the first touch-point in the gesture sample.
		/// </summary>
		public Vector2 Position
		{
			get
			{
				return position;
			}
		}

		/// <summary>
		/// Gets the position of the second touch-point in the gesture sample.
		/// </summary>
		public Vector2 Position2
		{
			get
			{
				return position2;
			}
		}

		/// <summary>
		/// Gets the delta information for the first touch-point in the gesture sample.
		/// </summary>
		public Vector2 Delta
		{
			get
			{
				return delta;
			}
		}

		/// <summary>
		/// Gets the delta information for the second touch-point in the gesture sample.
		/// </summary>
		public Vector2 Delta2
		{
			get
			{
				return delta2;
			}
		}

		#endregion

		#region Private Variables

		private GestureType gestureType;
		private TimeSpan timestamp;
		private Vector2 position;
		private Vector2 position2;
		private Vector2 delta;
		private Vector2 delta2;

		#endregion

		#region Public Constructor

		/// <summary>
		/// Initializes a new <see cref="GestureSample"/>.
		/// </summary>
		/// <param name="gestureType"><see cref="GestureType"/></param>
		/// <param name="timestamp"></param>
		/// <param name="position"></param>
		/// <param name="position2"></param>
		/// <param name="delta"></param>
		/// <param name="delta2"></param>
		public GestureSample(
			GestureType gestureType,
			TimeSpan timestamp,
			Vector2 position,
			Vector2 position2,
			Vector2 delta,
			Vector2 delta2
		) {
			this.gestureType = gestureType;
			this.timestamp = timestamp;
			this.position = position;
			this.position2 = position2;
			this.delta = delta;
			this.delta2 = delta2;
		}

		#endregion
	}
}
