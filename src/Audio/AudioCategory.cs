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
using System.Diagnostics;
#endregion

namespace Microsoft.Xna.Framework.Audio
{
	// http://msdn.microsoft.com/en-us/library/microsoft.xna.framework.audio.audiocategory.aspx
	public struct AudioCategory : IEquatable<AudioCategory>
	{
		#region Private Primitive Type Container Class

		private class PrimitiveInstance<T>
		{
			public T Value;
			public PrimitiveInstance(T initial)
			{
				Value = initial;
			}
		}

		#endregion

		#region Public Properties

		private string INTERNAL_name;
		public string Name
		{
			get
			{
				return INTERNAL_name;
			}
		}

		#endregion

		#region Private Variables

		private List<Cue> activeCues;

		private Dictionary<string, int> cueInstanceCounts;

		// Grumble, struct returns...
		private PrimitiveInstance<float> INTERNAL_volume;

		private byte maxCueInstances;
		private MaxInstanceBehavior maxCueBehavior;
		private ushort maxFadeInMS;
		private ushort maxFadeOutMS;
		private CrossfadeType crossfadeType;

		// TODO: Right now only Queue has fade behavior. -flibit
		private PrimitiveInstance<bool> fading;
		private PrimitiveInstance<Cue> fadingCue; // FIXME: May need to be a Queue? -flibit
		private PrimitiveInstance<Cue> queuedCue; // FIXME: May need to be a Queue? -flibit
		private PrimitiveInstance<float> fadingCueVolume;
		private Stopwatch fadeTimer;

		#endregion

		#region Internal Constructor

		internal AudioCategory(
			string name,
			float volume,
			byte maxInstances,
			int maxBehavior,
			ushort fadeInMS,
			ushort fadeOutMS,
			int fadeType
		) {
			INTERNAL_name = name;
			INTERNAL_volume = new PrimitiveInstance<float>(volume);
			activeCues = new List<Cue>();
			cueInstanceCounts = new Dictionary<string, int>();

			maxCueInstances = maxInstances;
			maxCueBehavior = (MaxInstanceBehavior) maxBehavior;
			maxFadeInMS = fadeInMS;
			maxFadeOutMS = fadeOutMS;
			crossfadeType = (CrossfadeType) fadeType;

			fading = new PrimitiveInstance<bool>(false);
			fadingCue = new PrimitiveInstance<Cue>(null);
			queuedCue = new PrimitiveInstance<Cue>(null);
			fadingCueVolume = new PrimitiveInstance<float>(0.0f);
			fadeTimer = new Stopwatch();
		}

		#endregion

		#region Public Methods

		public void Pause()
		{
			lock (activeCues)
			{
				foreach (Cue curCue in activeCues)
				{
					curCue.Pause();
				}
			}
		}

		public void Resume()
		{
			lock (activeCues)
			{
				foreach (Cue curCue in activeCues)
				{
					curCue.Resume();
				}
			}
		}

		public void SetVolume(float volume)
		{
			lock (activeCues)
			{
				INTERNAL_volume.Value = volume;
				foreach (Cue curCue in activeCues)
				{
					curCue.SetVariable("Volume", volume);
				}
			}
		}

		public void Stop(AudioStopOptions options)
		{
			lock (activeCues)
			{
				while (activeCues.Count > 0)
				{
					Cue curCue = activeCues[0];
					curCue.Stop(options);
					curCue.SetVariable("NumCueInstances", 0);
					cueInstanceCounts[curCue.Name] -= 1;
				}
				activeCues.Clear();
			}
		}

		public override int GetHashCode()
		{
			return Name.GetHashCode();
		}

		public bool Equals(AudioCategory other)
		{
			return (GetHashCode() == other.GetHashCode());
		}

		public override bool Equals(Object obj)
		{
			if (obj is AudioCategory)
			{
				return Equals((AudioCategory) obj);
			}
			return false;
		}

		public static bool operator ==(
			AudioCategory value1,
			AudioCategory value2
		) {
			return value1.Equals(value2);
		}

		public static bool operator !=(
			AudioCategory value1,
			AudioCategory value2
		) {
			return !(value1.Equals(value2));
		}

		#endregion

		#region Internal Methods

		internal void INTERNAL_update()
		{
			/* Believe it or not, someone might run the update on a thread.
			 * So, we're going to give a lock to this method.
			 * -flibit
			 */
			lock (activeCues)
			{
				if (fading.Value)
				{
					float fadeOutPerc;
					float fadeInPerc;
					if (crossfadeType == CrossfadeType.Linear)
					{
						fadeOutPerc = (maxFadeOutMS - fadeTimer.ElapsedMilliseconds) / (float) maxFadeOutMS;
						fadeInPerc = fadeTimer.ElapsedMilliseconds / (float) maxFadeInMS;
					}
					else
					{
						throw new NotImplementedException("Unhandled CrossfadeType!");
					}
					if (fadeInPerc >= 1.0f && fadeOutPerc <= 0.0f)
					{
						fadingCue.Value.Stop(AudioStopOptions.Immediate);
						queuedCue.Value.SetVariable("Volume", INTERNAL_volume.Value);
						fadingCue = null;
						queuedCue = null;
						fading.Value = false;
						fadeTimer.Stop();
					}
					if (fadeOutPerc > 0.0f)
					{
						fadingCue.Value.SetVariable(
							"Volume",
							fadingCueVolume.Value * fadeOutPerc
						);
					}
					if (fadeInPerc < 1.0f)
					{
						queuedCue.Value.SetVariable(
							"Volume",
							INTERNAL_volume.Value * fadeInPerc
						);
					}
				}
				for (int i = 0; i < activeCues.Count; i += 1)
				{
					if (!activeCues[i].INTERNAL_update())
					{
						i -= 1;
					}
				}
				foreach (Cue curCue in activeCues)
				{
					curCue.SetVariable(
						"NumCueInstances",
						cueInstanceCounts[curCue.Name]
					);
				}
			}
		}

		internal void INTERNAL_initCue(Cue newCue)
		{
			lock (activeCues)
			{
				if (!cueInstanceCounts.ContainsKey(newCue.Name))
				{
					cueInstanceCounts.Add(newCue.Name, 0);
				}
				newCue.SetVariable("NumCueInstances", cueInstanceCounts[newCue.Name]);
				newCue.SetVariable("Volume", INTERNAL_volume.Value);
			}
		}

		internal bool INTERNAL_addCue(Cue newCue)
		{
			lock (activeCues)
			{
				if (activeCues.Count >= maxCueInstances)
				{
					if (maxCueBehavior == MaxInstanceBehavior.Fail)
					{
						return false; // Just ignore us...
					}
					else if (maxCueBehavior == MaxInstanceBehavior.Queue)
					{
						newCue.SetVariable("Volume", 0.0f);
						queuedCue.Value = newCue;
						fadingCue.Value = activeCues[0];
						fadingCueVolume.Value = activeCues[0].GetVariable("Volume");
						fadeTimer.Reset();
						fadeTimer.Start();
						fading.Value = true;
					}
					else if (maxCueBehavior == MaxInstanceBehavior.ReplaceOldest)
					{
						INTERNAL_removeOldestCue(activeCues[0].Name);
					}
					else if (maxCueBehavior == MaxInstanceBehavior.ReplaceQuietest)
					{
						float lowestVolume = float.MaxValue;
						int lowestIndex = -1;
						for (int i = 0; i < activeCues.Count; i += 1)
						{
							if (activeCues[i].GetVariable("Volume") < lowestVolume)
							{
								lowestVolume = activeCues[i].GetVariable("Volume");
								lowestIndex = i;
							}
						}
						if (lowestIndex > -1)
						{
							cueInstanceCounts[activeCues[lowestIndex].Name] -= 1;
							activeCues[lowestIndex].Stop(AudioStopOptions.AsAuthored);
						}
					}
					else if (maxCueBehavior == MaxInstanceBehavior.ReplaceLowestPriority)
					{
						// FIXME: Priority?
						INTERNAL_removeOldestCue(activeCues[0].Name);
					}
				}
				cueInstanceCounts[newCue.Name] += 1;
				newCue.SetVariable("NumCueInstances", cueInstanceCounts[newCue.Name]);
				activeCues.Add(newCue);
			}
			return true;
		}

		internal void INTERNAL_removeLatestCue()
		{
			lock (activeCues)
			{
				Cue toDie = activeCues[activeCues.Count - 1];
				cueInstanceCounts[toDie.Name] -= 1;
				activeCues.RemoveAt(activeCues.Count - 1);
			}
		}

		internal void INTERNAL_removeOldestCue(string name)
		{
			lock (activeCues)
			{
				for (int i = 0; i < activeCues.Count; i += 1)
				{
					if (activeCues[i].Name.Equals(name))
					{
						activeCues[i].Stop(AudioStopOptions.AsAuthored);
						return;
					}
				}
			}
		}

		internal void INTERNAL_removeQuietestCue(string name)
		{
			float lowestVolume = float.MaxValue;
			int lowestIndex = -1;

			lock (activeCues)
			{
				for (int i = 0; i < activeCues.Count; i += 1)
				{
					if (	activeCues[i].Name.Equals(name) &&
						activeCues[i].GetVariable("Volume") < lowestVolume	)
					{
						lowestVolume = activeCues[i].GetVariable("Volume");
						lowestIndex = i;
					}
				}

				if (lowestIndex > -1)
				{
					cueInstanceCounts[name] -= 1;
					activeCues[lowestIndex].Stop(AudioStopOptions.AsAuthored);
				}
			}
		}

		internal void INTERNAL_removeActiveCue(Cue cue)
		{
			if (activeCues != null)
			{
				lock (activeCues)
				{
					if (activeCues.Contains(cue))
					{
						activeCues.Remove(cue);
						cueInstanceCounts[cue.Name] -= 1;
					}
				}
			}
		}

		#endregion
	}
}
