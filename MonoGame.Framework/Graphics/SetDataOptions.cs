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
    public enum SetDataOptions
    { 
        None = 0,
		Discard = 1,
        NoOverwrite = 2
    }
}

