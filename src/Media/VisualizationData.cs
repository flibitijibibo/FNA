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
using System.Collections.Generic;
using System.Collections.ObjectModel;
#endregion

namespace Microsoft.Xna.Framework.Media
{
	public class VisualizationData
	{
		#region Public Properties

		public ReadOnlyCollection<float> Frequencies
		{
			get;
			private set;
		}

		public ReadOnlyCollection<float> Samples
		{
			get;
			private set;
		}

		#endregion

		#region Internal Constants

		internal const int Size = 256;

		#endregion

		#region Private Variables

		private List<float> freqList;
		private List<float> sampList;

		#endregion

		#region Public Constructor

		public VisualizationData()
		{
			freqList = new List<float>(Size);
			sampList = new List<float>(Size);
			freqList.AddRange(new float[Size]);
			sampList.AddRange(new float[Size]);
			Frequencies = new ReadOnlyCollection<float>(freqList);
			Samples = new ReadOnlyCollection<float>(sampList);
		}

		#endregion

		#region Internal Methods

		internal void CalculateData(Song curSong)
		{
			for (int i = 0; i < Size; i += 1)
			{
				sampList[i] = Song.visSamples[i * curSong.chunkStep] / 32768.0f;
				// freqList[i] = sampList[i];
			}
			// FFT256(freqList);
			Array.Copy(Song.visSamples, curSong.chunkSize, Song.visSamples, 0, Song.bufferedSamples - curSong.chunkSize);
			Song.bufferedSamples -= curSong.chunkSize;
		}

		#endregion

		#region Private Static FFT Calculation Methods

		/* FIXME: The following is based on the work of Phil Karn:
		 * http://www.setileague.org/software/karnfft.htm
		 * It doesn't work for our purposes though; the range is way off.
		 * Perhaps it needs a window function, or something...?
		 * -flibit
		 */

		private static float[] im = new float[Size];
		private static readonly float[] twiddles = GenTwiddles();
		private static float[] GenTwiddles()
		{
			float[] result = new float[256];
			result[0] = 1.0f;
			result[128] = -1.0f;
			for (int i = 1; i <= 64; i += 1)
			{
				result[i] = (float) Math.Cos(Math.PI * (i / 128.0));
				result[256 - i] = result[i];
				result[128 - i] = -result[i];
				result[128 + i] = result[128 - i];
			}
			return result;
		}

		private static void FFT256(List<float> samp)
		{
			Array.Clear(im, 0, im.Length);
			for (int i = 0; i < 64; i += 1)
			{
				FFT4G(samp, i, 64, i);
			}
			for (int i = 0; i < 256; i += 64)
			{
				FFT64(samp, i);
			}

			float temp;
			int j = 128;
			int k;
			for (int i = 1; i < 255; i += 1)
			{
				if (i < j)
				{
					temp = samp[i];
					samp[i] = samp[j];
					samp[j] = temp;
				}

				for (k = 128; k <= j; k >>= 1)
				{
					j -= k;
				}

				j += k;
			}
		}

		private static void FFT64(List<float> samp, int offset)
		{
			for (int i = 0; i < 16; i += 1)
			{
				FFT4G(samp, offset + i, 16, i * 4);
			}

			FFT16(samp, offset);
			FFT16(samp, offset + 16);
			FFT16(samp, offset + 32);
			FFT16(samp, offset + 48);
		}

		private static void FFT16(List<float> samp, int offset)
		{
			FFT4G(samp, offset, 4, 0);
			FFT4G(samp, offset + 1, 4, 16);
			FFT4G(samp, offset + 2, 4, 32);
			FFT4G(samp, offset + 3, 4, 48);

			FFT4(samp, offset);
			FFT4(samp, offset + 4);
			FFT4(samp, offset + 8);
			FFT4(samp, offset + 12);
		}

		private static void FFT4(List<float> samp, int offset)
		{
			float tmp0re, tmp0im, tmp1re, tmp1im, tmp2re, tmp2im;
			int off0 = offset, off1 = offset + 1, off2 = offset + 2, off3 = offset + 3;

			tmp2re = samp[off0] - im[off1] - samp[off2] + im[off3];
			tmp2im = im[off0] + samp[off1] - im[off2] - samp[off3];

			tmp0re = samp[off0] - samp[off1] + samp[off2] - samp[off3];
			tmp0im = im[off0] - im[off1] + im[off2] - im[off3];

			tmp1re = samp[off0] + im[off1] - samp[off2] - im[off3];
			tmp1im = im[off0] - samp[off1] - im[off2] + samp[off3];

			samp[off0] = samp[off0] + samp[off1] + samp[off2] + samp[off3];
			im[off0] = im[off0] + im[off1] + im[off2] + im[off3];

			samp[off1] = tmp0re;
			samp[off2] = tmp1re;
			samp[off3] = tmp2re;
			im[off1] = tmp0im;
			im[off2] = tmp1im;
			im[off3] = tmp2im;
		}

		private static void FFT4G(List<float> samp, int offset, int astep, int tstep)
		{
			float bre, cre, dre, bim, cim, dim;
			int astep1 = offset + astep;
			int astep2 = offset + astep * 2;
			int astep3 = offset + astep * 3;

			dre = samp[offset] - im[astep1] - samp[astep2] + im[astep3];
			dim = im[offset] + samp[astep1] - im[astep2] - samp[astep3];

			cre = samp[offset] - samp[astep1] + samp[astep2] - samp[astep3];
			cim = im[offset] - im[astep1] + im[astep2] - im[astep3];

			bre = samp[offset] + im[astep1] - samp[astep2] - im[astep3];
			bim = im[offset] - samp[astep1] - im[astep2] + samp[astep3];

			samp[offset] = samp[offset] + samp[astep1] + samp[astep2] + samp[astep3];
			im[offset] = im[offset] + im[astep1] + im[astep2] + im[astep3];

			if (tstep == 0)
			{
				samp[astep2] = bre;
				samp[astep1] = cre;
				samp[astep3] = dre;
				im[astep2] = bim;
				im[astep1] = cim;
				im[astep3] = dim;
			}
			else
			{
				samp[astep2] = bre * twiddles[tstep] - bim * twiddles[tstep + 64];
				im[astep2] = bim * twiddles[tstep] + bre * twiddles[tstep + 64];
				samp[astep1] = cre * twiddles[2 * tstep] - cim * twiddles[(2 * tstep) + 64];
				im[astep1] = cim * twiddles[2 * tstep] + cre * twiddles[(2 * tstep) + 64];
				samp[astep3] = dre * twiddles[3 * tstep] - dim * twiddles[(3 * tstep) + 64];
				im[astep3] = dim * twiddles[3 * tstep] + dre * twiddles[(3 * tstep) + 64];
			}
		}

		#endregion
	}
}
