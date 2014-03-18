#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.Xna.Framework.Graphics
{
    public sealed class GraphicsAdapter : IDisposable
    {
        private static ReadOnlyCollection<GraphicsAdapter> adapters;
        
        internal GraphicsAdapter()
        {
        }
        
        public void Dispose()
        {
        }

        public DisplayMode CurrentDisplayMode
        {
            get
            {
                SDL2.SDL.SDL_DisplayMode mode;
                SDL2.SDL.SDL_GetCurrentDisplayMode(0, out mode);
                return new DisplayMode(
                    mode.w,
                    mode.h,
                    SurfaceFormat.Color
                );
            }
        }

        public static GraphicsAdapter DefaultAdapter
        {
            get { return Adapters[0]; }
        }
        
        public static ReadOnlyCollection<GraphicsAdapter> Adapters {
            get {
                if (adapters == null) {
                    adapters = new ReadOnlyCollection<GraphicsAdapter>(
						new GraphicsAdapter[] {new GraphicsAdapter()});
                }
                return adapters;
            }
        } 
		
        /*
		public bool QueryRenderTargetFormat(
			GraphicsProfile graphicsProfile,
			SurfaceFormat format,
			DepthFormat depthFormat,
			int multiSampleCount,
			out SurfaceFormat selectedFormat,
			out DepthFormat selectedDepthFormat,
			out int selectedMultiSampleCount)
		{
			throw new NotImplementedException();
		}

		public bool QueryBackBufferFormat(
			GraphicsProfile graphicsProfile,
			SurfaceFormat format,
			DepthFormat depthFormat,
			int multiSampleCount,
			out SurfaceFormat selectedFormat,
			out DepthFormat selectedDepthFormat,
			out int selectedMultiSampleCount)
		{
			throw new NotImplementedException("flibit put this here.");
		}

        public string Description
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int DeviceId
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Guid DeviceIdentifier
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string DeviceName
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string DriverDll
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Version DriverVersion
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool IsDefaultAdapter
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool IsWideScreen
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IntPtr MonitorHandle
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int Revision
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int SubSystemId
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        */

        private DisplayModeCollection supportedDisplayModes = null;
        
        public DisplayModeCollection SupportedDisplayModes
        {
            get
            {

                if (supportedDisplayModes == null)
                {
                    List<DisplayMode> modes = new List<DisplayMode>(new DisplayMode[] { CurrentDisplayMode, });
                    SDL2.SDL.SDL_DisplayMode filler = new SDL2.SDL.SDL_DisplayMode();
                    int numModes = SDL2.SDL.SDL_GetNumDisplayModes(0);
                    for (int i = 0; i < numModes; i += 1)
                    {
                        SDL2.SDL.SDL_GetDisplayMode(0, i, out filler);

                        // Check for dupes caused by varying refresh rates.
                        bool dupe = false;
                        foreach (DisplayMode mode in modes)
                        {
                            if (filler.w == mode.Width && filler.h == mode.Height)
                            {
                                dupe = true;
                            }
                        }
                        if (dupe)
                        {
                            continue;
                        }

                        modes.Add(
                            new DisplayMode(
                                filler.w,
                                filler.h,
                                SurfaceFormat.Color // FIXME: Assumption!
                            )
                        );
                    }
                    supportedDisplayModes = new DisplayModeCollection(modes);
                }
                return supportedDisplayModes;
            }
        }

        /*
        public int VendorId
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        */
    }
}

