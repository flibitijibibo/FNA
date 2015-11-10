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

		#region Private Variables

		private const int Size = 256;

		private List<float> freqList;
		private List<float> sampList;

		#endregion

		#region Public Constructor

		public VisualizationData()
		{
			freqList = new List<float>(Size);
			sampList = new List<float>(Size);
			freqList.AddRange(new float[256]);
			sampList.AddRange(new float[256]);
			Frequencies = new ReadOnlyCollection<float>(freqList);
			Samples = new ReadOnlyCollection<float>(sampList);
		}

		#endregion

		#region Internal Methods

		// static Random r = new Random();
		internal void CalculateData(Song curSong)
		{
			/* TODO: This is just for testing purposes. Need the real stuff! -flibit
			for (int i = 0; i < Size; i += 1)
			{
				freqList[i] = (float) r.NextDouble();
				sampList[i] = (float) r.NextDouble() * 2.0f - 1.0f;
			}
			*/
		}

		#endregion
	}
}
