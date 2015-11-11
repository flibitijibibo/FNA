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
using System.Runtime.InteropServices;
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

		// FIXME: Remove KISS FFT dependency!
		[DllImport("kiss.dll")]
		private static extern IntPtr kiss_fftr_alloc(int a, int b, IntPtr c, IntPtr d);
		[DllImport("kiss.dll")]
		private static extern void kiss_fftr(IntPtr cfg, float[] fin, kiss_fft_cpx[] fout);
		[DllImport("msvcrt.dll")]
		private static extern void free(IntPtr mem);
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		private struct kiss_fft_cpx
		{
			public float r;
			public float i;
		}

		internal void CalculateData(Song curSong)
		{
			/* FIXME: This function is thoroughly inefficient.
			 * Also, it's wrong. I dunno why yet.
			 * -flibit
			 */

			// The frequency data will be 256, but we need more data than that
			int signalSize = (Size - 1) * 2;

			// Calculate the window function (yeah, it's static, sorry...)
			float[] window = new float[signalSize];
			float windowSum = 0.0f;
			for (int i = 0; i < window.Length; i += 1)
			{
				window[i] = (float) (0.54 - 0.46 * Math.Cos((Math.PI * 2.0 * i) / (signalSize - 1.0)));
				windowSum += window[i];
			}
			float normalizer = 2.0f / windowSum;

			// Apply the window function to the PCM in floating-point format
			float[] windowedSignal = new float[signalSize];
			for (int i = 0; i < signalSize; i += 1)
			{
				// The 2 is because we're assuming stereo data!
				windowedSignal[i] = Song.visSamples[i * 2] / 32768.0f * window[i];
			}

			// FFT!
			IntPtr kiss = kiss_fftr_alloc(signalSize, 0, IntPtr.Zero, IntPtr.Zero);
			kiss_fft_cpx[] cx_out = new kiss_fft_cpx[Size];
			kiss_fftr(kiss, windowedSignal, cx_out);
			free(kiss);

			for (int i = 0; i < Size; i += 1)
			{
				sampList[i] = Song.visSamples[i * curSong.chunkStep] / 32768.0f;
				freqList[i] = (float) (Math.Sqrt(cx_out[i].r * cx_out[i].r + cx_out[i].i * cx_out[i].i) * normalizer);
			}
			Array.Copy(Song.visSamples, curSong.chunkSize, Song.visSamples, 0, Song.bufferedSamples - curSong.chunkSize);
			Song.bufferedSamples -= curSong.chunkSize;
		}

		#endregion
	}
}
