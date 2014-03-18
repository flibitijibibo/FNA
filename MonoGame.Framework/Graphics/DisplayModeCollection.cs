#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */

/*
MIT License
Copyright  2006 The Mono.Xna Team

All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
#endregion License

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Xna.Framework.Graphics
{
    public class DisplayModeCollection : IEnumerable<DisplayMode>, IEnumerable
    {
        private readonly List<DisplayMode> modes;

        public IEnumerable<DisplayMode> this[SurfaceFormat format]
        {
            get {
                List<DisplayMode> list = new List<DisplayMode>();
                foreach (DisplayMode mode in this.modes)
                {
                    if (mode.Format == format)
                    {
                        list.Add(mode);
                    }
                }
                return list;

            }
        }

        public IEnumerator<DisplayMode> GetEnumerator()
        {
            return modes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return modes.GetEnumerator();
        }
        
        public DisplayModeCollection(List<DisplayMode> setmodes) {
            modes = setmodes;
        }
    }
}