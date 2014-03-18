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
    public sealed class ResourceDestroyedEventArgs : EventArgs
    {
        /// <summary>
        /// The name of the destroyed resource.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// The resource manager tag of the destroyed resource.
        /// </summary>
        public Object Tag { get; internal set; }
    }
}
