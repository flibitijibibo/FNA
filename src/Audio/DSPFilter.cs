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

using OpenAL;
#endregion

namespace Microsoft.Xna.Framework.Audio
{
	internal class DSPFilter
	{
		#region Public Properties

		public uint Handle
		{
			get;
			private set;
		}

		#endregion

		#region Public Constructor

		public DSPFilter()
		{
			uint handle;
			EFX.alGenFilters((IntPtr) 1, out handle);
			Handle = handle;
		}

		#endregion

		#region Public Dispose Method

		public void Dispose()
		{
			uint handle = Handle;
			EFX.alDeleteFilters((IntPtr) 1, ref handle);
		}

		#endregion

		#region Public Methods

		public void ApplyLowPassFilter(uint source, float hfGain)
		{
			EFX.alFilteri(Handle, EFX.AL_FILTER_TYPE, EFX.AL_FILTER_LOWPASS);
			EFX.alFilterf(Handle, EFX.AL_LOWPASS_GAINHF, hfGain);
		}

		public void ApplyHighPassFilter(uint source, float lfGain)
		{
			EFX.alFilteri(Handle, EFX.AL_FILTER_TYPE, EFX.AL_FILTER_HIGHPASS);
			EFX.alFilterf(Handle, EFX.AL_HIGHPASS_GAINLF, lfGain);
		}

		public void ApplyBandPassFilter(uint source, float hfGain, float lfGain)
		{
			EFX.alFilteri(Handle, EFX.AL_FILTER_TYPE, EFX.AL_FILTER_BANDPASS);
			EFX.alFilterf(Handle, EFX.AL_BANDPASS_GAINHF, hfGain);
			EFX.alFilterf(Handle, EFX.AL_BANDPASS_GAINLF, lfGain);
		}

		#endregion
	}
}
