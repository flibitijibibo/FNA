#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

namespace Microsoft.Xna.Framework
{
	public enum CurveTangent
	{
		// A Flat tangent always has a value equal to zero
		Flat,
		/* A Linear tangent at a CurveKey is equal to the difference between its Value
		 * and the Value of the preceding or succeeding CurveKey.
		 */
		Linear,
		/* A Smooth tangent smooths the inflection between a TangentIn and TangentOut
		 * by taking into account the values of both neighbors of the CurveKey.
		 */
		Smooth
	}
}
