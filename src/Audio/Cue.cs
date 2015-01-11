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
using System.Collections.Generic;
#endregion

namespace Microsoft.Xna.Framework.Audio
{
	// http://msdn.microsoft.com/en-us/library/microsoft.xna.framework.audio.cue.aspx
	public sealed class Cue : IDisposable
	{
		#region Public Properties

		public bool IsCreated
		{
			get;
			private set;
		}

		public bool IsDisposed
		{
			get;
			private set;
		}

		public bool IsPaused
		{
			get
			{
				if (INTERNAL_queuedPaused)
				{
					return true;
				}
				foreach (SoundEffectInstance sfi in INTERNAL_instancePool)
				{
					if (sfi.State == SoundState.Paused)
					{
						return true;
					}
				}
				return false;
			}
		}

		public bool IsPlaying
		{
			get
			{
				if (INTERNAL_queuedPlayback)
				{
					return true;
				}
				foreach (SoundEffectInstance sfi in INTERNAL_instancePool)
				{
					if (sfi.State != SoundState.Stopped)
					{
						return true;
					}
				}
				return false;
			}
		}

		public bool IsPrepared
		{
			get;
			private set;
		}

		public bool IsPreparing
		{
			get;
			private set;
		}

		public bool IsStopped
		{
			get
			{
				return !IsPlaying;
			}
		}

		public bool IsStopping
		{
			get
			{
				// FIXME: Authored Stop Options?
				return false;
			}
		}

		public string Name
		{
			get;
			private set;
		}

		#endregion

		#region Private Variables

		private AudioEngine INTERNAL_baseEngine;

		private CueData INTERNAL_data;
		private XACTSound INTERNAL_activeSound;
		private List<SoundEffectInstance> INTERNAL_instancePool;
		private List<float> INTERNAL_instanceVolumes;
		private List<float> INTERNAL_instancePitches;

		// User-controlled sounds require a bit more trickery.
		private bool INTERNAL_userControlledPlaying;
		private float INTERNAL_controlledValue;

		private bool INTERNAL_isPositional;
		private AudioListener INTERNAL_listener;
		private AudioEmitter INTERNAL_emitter;

		private List<Variable> INTERNAL_variables;

		private AudioCategory INTERNAL_category;
		private bool INTERNAL_isManaged;

		private bool INTERNAL_queuedPlayback;
		private bool INTERNAL_queuedPaused;

		#endregion

		#region Private Static Random Number Generator

		private static Random random = new Random();

		#endregion

		#region Disposing Event

		public event EventHandler<EventArgs> Disposing;

		#endregion

		#region Internal Constructor

		internal Cue(
			AudioEngine audioEngine,
			List<string> waveBankNames,
			string name,
			CueData data,
			bool managed
		) {
			INTERNAL_baseEngine = audioEngine;

			Name = name;

			INTERNAL_data = data;
			foreach (XACTSound curSound in data.Sounds)
			{
				if (!curSound.HasLoadedTracks)
				{
					curSound.LoadTracks(
						INTERNAL_baseEngine,
						waveBankNames
					);
				}
			}

			INTERNAL_isManaged = managed;

			INTERNAL_category = INTERNAL_baseEngine.INTERNAL_initCue(
				this,
				data.Category
			);

			INTERNAL_userControlledPlaying = false;
			INTERNAL_isPositional = false;
			INTERNAL_queuedPlayback = false;
			INTERNAL_queuedPaused = false;

			INTERNAL_instancePool = new List<SoundEffectInstance>();
			INTERNAL_instanceVolumes = new List<float>();
			INTERNAL_instancePitches = new List<float>();
		}

		#endregion

		#region Destructor

		~Cue()
		{
			Dispose();
		}

		#endregion

		#region Public Dispose Method

		public void Dispose()
		{
			if (!IsDisposed)
			{
				if (Disposing != null)
				{
					Disposing.Invoke(this, null);
				}
				if (INTERNAL_instancePool != null)
				{
					foreach (SoundEffectInstance sfi in INTERNAL_instancePool)
					{
						sfi.Dispose();
					}
					INTERNAL_instancePool.Clear();
					INTERNAL_instanceVolumes.Clear();
					INTERNAL_instancePitches.Clear();
					INTERNAL_queuedPlayback = false;
				}
				INTERNAL_category.INTERNAL_removeActiveCue(this);
				IsDisposed = true;
			}
		}

		#endregion

		#region Public Methods

		public void Apply3D(AudioListener listener, AudioEmitter emitter)
		{
			if (IsPlaying && !INTERNAL_isPositional)
			{
				throw new InvalidOperationException("Apply3D call after Play!");
			}
			if (listener == null)
			{
				throw new ArgumentNullException("listener");
			}
			if (emitter == null)
			{
				throw new ArgumentNullException("emitter");
			}
			INTERNAL_listener = listener;
			INTERNAL_emitter = emitter;
			SetVariable(
				"Distance",
				Vector3.Distance(
					INTERNAL_emitter.Position,
					INTERNAL_listener.Position
				)
			);
			// TODO: DopplerPitchScaler, OrientationAngle
			INTERNAL_isPositional = true;
		}

		public float GetVariable(string name)
		{
			if (String.IsNullOrEmpty(name))
			{
				throw new ArgumentNullException("name");
			}
			foreach (Variable curVar in INTERNAL_variables)
			{
				if (name.Equals(curVar.Name))
				{
					return curVar.GetValue();
				}
			}
			throw new Exception("Instance variable not found!");
		}

		public void Pause()
		{
			if (!IsPlaying)
			{
				if (INTERNAL_queuedPlayback)
				{
					INTERNAL_queuedPaused = true;
				}
				return;
			}
			foreach (SoundEffectInstance sfi in INTERNAL_instancePool)
			{
				sfi.Pause();
			}
		}

		public void Play()
		{
			if (IsPlaying)
			{
				throw new InvalidOperationException("Cue already playing!");
			}

			INTERNAL_category.INTERNAL_initCue(this);

			if (GetVariable("NumCueInstances") >= INTERNAL_data.InstanceLimit)
			{
				if (INTERNAL_data.MaxCueBehavior == MaxInstanceBehavior.Fail)
				{
					return; // Just ignore us...
				}
				else if (INTERNAL_data.MaxCueBehavior == MaxInstanceBehavior.Queue)
				{
					throw new Exception("Cue Queueing not handled!");
				}
				else if (INTERNAL_data.MaxCueBehavior == MaxInstanceBehavior.ReplaceOldest)
				{
					INTERNAL_category.INTERNAL_removeOldestCue(Name);
				}
				else if (INTERNAL_data.MaxCueBehavior == MaxInstanceBehavior.ReplaceQuietest)
				{
					INTERNAL_category.INTERNAL_removeQuietestCue(Name);
				}
				else if (INTERNAL_data.MaxCueBehavior == MaxInstanceBehavior.ReplaceLowestPriority)
				{
					// FIXME: Priority?
					INTERNAL_category.INTERNAL_removeOldestCue(Name);
				}
			}

			if (!INTERNAL_category.INTERNAL_addCue(this))
			{
				return;
			}

			if (!INTERNAL_calculateNextSound())
			{
				return;
			}

			INTERNAL_setupSounds();

			if (INTERNAL_isPositional)
			{
				foreach (SoundEffectInstance sfi in INTERNAL_instancePool)
				{
					sfi.Apply3D(
						INTERNAL_listener,
						INTERNAL_emitter
					);
				}
			}

			INTERNAL_queuedPlayback = true;
		}

		public void Resume()
		{
			if (!IsPaused)
			{
				return;
			}
			foreach (SoundEffectInstance sfi in INTERNAL_instancePool)
			{
				sfi.Resume();
			}
		}

		public void SetVariable(string name, float value)
		{
			if (String.IsNullOrEmpty(name))
			{
				throw new ArgumentNullException("name");
			}
			foreach (Variable curVar in INTERNAL_variables)
			{
				if (name.Equals(curVar.Name))
				{
					curVar.SetValue(value);
					return;
				}
			}
			throw new Exception("Instance variable not found!");
		}

		public void Stop(AudioStopOptions options)
		{
			if (INTERNAL_queuedPlayback)
			{
				INTERNAL_queuedPlayback = false;
				INTERNAL_category.INTERNAL_removeActiveCue(this);
				return;
			}
			foreach (SoundEffectInstance sfi in INTERNAL_instancePool)
			{
				sfi.Stop();
				sfi.Dispose();
			}
			INTERNAL_instancePool.Clear();
			INTERNAL_instanceVolumes.Clear();
			INTERNAL_instancePitches.Clear();
			INTERNAL_userControlledPlaying = false;
			INTERNAL_category.INTERNAL_removeActiveCue(this);

			// If this is a managed Cue, we're done here.
			if (INTERNAL_isManaged)
			{
				Dispose();
			}
		}

		#endregion

		#region Internal Methods

		internal bool INTERNAL_update()
		{
			// If this is our first update, time to play!
			if (INTERNAL_queuedPlayback)
			{
				INTERNAL_queuedPlayback = false;
				foreach (SoundEffectInstance sfi in INTERNAL_instancePool)
				{
					sfi.Play();
					if (INTERNAL_queuedPaused)
					{
						sfi.Pause();
					}
				}
				INTERNAL_queuedPaused = false;
			}

			for (int i = 0; i < INTERNAL_instancePool.Count; i += 1)
			{
				if (INTERNAL_instancePool[i].INTERNAL_timer.ElapsedMilliseconds > INTERNAL_instancePool[i].INTERNAL_delayMS)
				{
					// Okay, play this NOW!
					INTERNAL_instancePool[i].Play();
					if (IsPaused)
					{
						INTERNAL_instancePool[i].Pause();
					}
				}
				if (INTERNAL_instancePool[i].State == SoundState.Stopped)
				{
					INTERNAL_instancePool[i].Dispose();
					INTERNAL_instancePool.RemoveAt(i);
					INTERNAL_instanceVolumes.RemoveAt(i);
					INTERNAL_instancePitches.RemoveAt(i);
					i -= 1;
				}
			}

			// User control updates
			if (INTERNAL_data.IsUserControlled)
			{
				string varName = INTERNAL_data.UserControlVariable;
				if (	INTERNAL_userControlledPlaying &&
					(INTERNAL_baseEngine.INTERNAL_isGlobalVariable(varName) ?
						(INTERNAL_controlledValue != INTERNAL_baseEngine.GetGlobalVariable(varName)) :
						(INTERNAL_controlledValue != GetVariable(INTERNAL_data.UserControlVariable)))	)
				{
					// TODO: Crossfading
					foreach (SoundEffectInstance sfi in INTERNAL_instancePool)
					{
						sfi.Stop();
						sfi.Dispose();
					}
					INTERNAL_instancePool.Clear();
					INTERNAL_instanceVolumes.Clear();
					INTERNAL_instancePitches.Clear();
					if (!INTERNAL_calculateNextSound())
					{
						// Nothing to play, bail.
						return true;
					}
					INTERNAL_setupSounds();
					foreach (SoundEffectInstance sfi in INTERNAL_instancePool)
					{
						sfi.Play();
					}
				}

				if (INTERNAL_activeSound == null)
				{
					return INTERNAL_userControlledPlaying;
				}
			}

			if (INTERNAL_isPositional)
			{
				foreach (SoundEffectInstance sfi in INTERNAL_instancePool)
				{
					sfi.Apply3D(
						INTERNAL_listener,
						INTERNAL_emitter
					);
				}
			}

			float rpcVolume = 1.0f;
			float rpcPitch = 0.0f;
			foreach (uint curCode in INTERNAL_activeSound.RPCCodes)
			{
				RPC curRPC = INTERNAL_baseEngine.INTERNAL_getRPC(curCode);
				float result;
				if (!INTERNAL_baseEngine.INTERNAL_isGlobalVariable(curRPC.Variable))
				{
					result = curRPC.CalculateRPC(GetVariable(curRPC.Variable));
				}
				else
				{
					// It's a global variable we're looking for!
					result = curRPC.CalculateRPC(
						INTERNAL_baseEngine.GetGlobalVariable(
							curRPC.Variable
						)
					);
				}
				if (curRPC.Parameter == RPCParameter.Volume)
				{
					rpcVolume *= XACTCalculator.CalculateAmplitudeRatio(result / 100.0);
				}
				else if (curRPC.Parameter == RPCParameter.Pitch)
				{
					rpcPitch += result / 1000.0f;
				}
				else if (curRPC.Parameter == RPCParameter.FilterFrequency)
				{
					// TODO: Filters?
				}
				else
				{
					throw new Exception("RPC Parameter Type: " + curRPC.Parameter.ToString());
				}
			}
			for (int i = 0; i < INTERNAL_instancePool.Count; i += 1)
			{
				/* The final volume should be the combination of the
				 * authored volume, Volume variable and RPC volume results.
				 */
				INTERNAL_instancePool[i].Volume = INTERNAL_instanceVolumes[i] * GetVariable("Volume") * rpcVolume;

				/* The final pitch should be the combination of the
				 * authored pitch and RPC pitch results.
				 */
				INTERNAL_instancePool[i].Pitch = INTERNAL_instancePitches[i] + rpcPitch;
			}

			// Finally, check if we're still active.
			if (IsStopped && !INTERNAL_queuedPlayback && !INTERNAL_userControlledPlaying)
			{
				// If this is managed, we're done here.
				if (INTERNAL_isManaged)
				{
					Dispose();
				}
				else
				{
					INTERNAL_category.INTERNAL_removeActiveCue(this);
				}
				return false;
			}
			return true;
		}

		internal void INTERNAL_genVariables(List<Variable> cueVariables)
		{
			INTERNAL_variables = cueVariables;
		}

		#endregion

		#region Private Methods

		private bool INTERNAL_calculateNextSound()
		{
			INTERNAL_activeSound = null;

			// Pick a sound based on a Cue instance variable
			if (INTERNAL_data.IsUserControlled)
			{
				INTERNAL_userControlledPlaying = true;
				if (INTERNAL_baseEngine.INTERNAL_isGlobalVariable(INTERNAL_data.UserControlVariable))
				{
					INTERNAL_controlledValue = INTERNAL_baseEngine.GetGlobalVariable(
						INTERNAL_data.UserControlVariable
					);
				}
				else
				{
					INTERNAL_controlledValue = GetVariable(
						INTERNAL_data.UserControlVariable
					);
				}
				for (int i = 0; i < INTERNAL_data.Probabilities.Length / 2; i += 1)
				{
					if (	INTERNAL_controlledValue <= INTERNAL_data.Probabilities[i, 0] &&
						INTERNAL_controlledValue >= INTERNAL_data.Probabilities[i, 1]	)
					{
						INTERNAL_activeSound = INTERNAL_data.Sounds[i];
						return true;
					}
				}

				/* This should only happen when the
				 * UserControlVariable is none of the sound
				 * probabilities, in which case we are just
				 * silent. But, we are still claiming to be
				 * "playing" in the meantime.
				 * -flibit
				 */
				return false;
			}

			// Randomly pick a sound
			double max = 0.0;
			for (int i = 0; i < INTERNAL_data.Probabilities.GetLength(0); i += 1)
			{
				max += INTERNAL_data.Probabilities[i, 0] - INTERNAL_data.Probabilities[i, 1];
			}
			double next = random.NextDouble() * max;

			for (int i = INTERNAL_data.Probabilities.GetLength(0) - 1; i >= 0; i -= 1)
			{
				if (next > max - (INTERNAL_data.Probabilities[i, 0] - INTERNAL_data.Probabilities[i, 1]))
				{
					INTERNAL_activeSound = INTERNAL_data.Sounds[i];
					break;
				}
				max -= INTERNAL_data.Probabilities[i, 0] - INTERNAL_data.Probabilities[i, 1];
			}

			return true;
		}

		private void INTERNAL_setupSounds()
		{
			INTERNAL_activeSound.GenerateInstances(
				INTERNAL_instancePool,
				INTERNAL_instanceVolumes,
				INTERNAL_instancePitches
			);

			foreach (uint curDSP in INTERNAL_activeSound.DSPCodes)
			{
				DSPEffect handle = INTERNAL_baseEngine.INTERNAL_getDSP(curDSP);
				foreach (SoundEffectInstance sfi in INTERNAL_instancePool)
				{
					// FIXME: This only applies the last DSP!
					sfi.INTERNAL_applyEffect(handle);
				}
			}
		}

		#endregion
	}
}
