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
	[Flags]
	public enum ColorWriteChannels
	{
		None = 0,
		Red = 1,
		Green = 2,
		Blue = 4,
		Alpha = 8,
		All = 15
	}
}