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
	// http://msdn.microsoft.com/en-us/library/microsoft.xna.framework.audio.soundeffectinstance.aspx
	public class SoundEffectInstance : IDisposable
	{
		#region Public Properties

		public bool IsDisposed
		{
			get;
			protected set;
		}

		private bool INTERNAL_looped = false;
		public virtual bool IsLooped
		{
			get
			{
				return INTERNAL_looped;
			}
			set
			{
				INTERNAL_looped = value;
				if (INTERNAL_alSource != 0)
				{
					AL10.alSourcei(
						INTERNAL_alSource,
						AL10.AL_LOOPING,
						Convert.ToInt32(INTERNAL_looped)
					);
				}
			}
		}

		private float INTERNAL_pan = 0.0f;
		public float Pan
		{
			get
			{
				return INTERNAL_pan;
			}
			set
			{
				INTERNAL_pan = value;
				if (INTERNAL_alSource != 0)
				{
					AL10.alSource3f(
						INTERNAL_alSource,
						AL10.AL_POSITION,
						INTERNAL_pan,
						0.0f,
						(float) Math.Sqrt(1 - Math.Pow(INTERNAL_pan, 2))
					);
				}
			}
		}

		private float INTERNAL_pitch = 0f;
		public float Pitch
		{
			get
			{
				return INTERNAL_pitch;
			}
			set
			{
				INTERNAL_pitch = value;
				if (INTERNAL_alSource != 0)
				{
					AL10.alSourcef(
						INTERNAL_alSource,
						AL10.AL_PITCH,
						INTERNAL_XNA_To_AL_Pitch(INTERNAL_pitch)
					);
				}
			}
		}

		public SoundState State
		{
			get
			{
				if (INTERNAL_alSource == 0)
				{
					return SoundState.Stopped;
				}
				int state;
				AL10.alGetSourcei(
					INTERNAL_alSource,
					AL10.AL_SOURCE_STATE,
					out state
				);
				if (state == AL10.AL_PLAYING)
				{
					return SoundState.Playing;
				}
				else if (state == AL10.AL_PAUSED)
				{
					return SoundState.Paused;
				}
				return SoundState.Stopped;
			}
		}

		private float INTERNAL_volume = 1.0f;
		public float Volume
		{
			get
			{
				return INTERNAL_volume;
			}
			set
			{
				INTERNAL_volume = value;
				if (INTERNAL_alSource != 0)
				{
					AL10.alSourcef(
						INTERNAL_alSource,
						AL10.AL_GAIN,
						INTERNAL_volume * SoundEffect.MasterVolume
					);
				}
			}
		}

		#endregion

		#region Internal Properties

		internal int FilterType
		{
			get;
			set;
		}

		#endregion

		#region Private Variables: XNA Implementation

		private SoundEffect INTERNAL_parentEffect;

		/* FNA' XACT runtime wraps around SoundEffect for audio output.
		 * Only problem: XACT pitch has no boundaries, SoundEffect does.
		 * So, we're going to use this to tell the pitch clamp to STFU.
		 * -flibit
		 */
		internal bool INTERNAL_isXACTSource = false;

		#endregion

		#region Private Variables: OpenAL Source, EffectSlot

		[CLSCompliant(false)]
		protected uint INTERNAL_alSource = 0;
		private uint INTERNAL_alEffectSlot = 0;

		#endregion

		#region Private Variables: 3D Audio

		protected Vector3 position = new Vector3(0.0f, 0.0f, 0.1f);

		// Used to prevent outdated positional audio data from being used
		protected bool INTERNAL_positionalAudio = false;

		#endregion

		#region Private XNA-to-OpenAL Pitch Converter

		private float INTERNAL_XNA_To_AL_Pitch(float xnaPitch)
		{
			/* XNA sets pitch bounds to [-1.0f, 1.0f], each end being one octave.
			 * OpenAL's AL_PITCH boundaries are (0.0f, INF).
			 * Consider the function f(x) = 2 ^ x
			 * The domain is (-INF, INF) and the range is (0, INF).
			 * 0.0f is the original pitch for XNA, 1.0f is the original pitch for OpenAL.
			 * Note that f(0) = 1, f(1) = 2, f(-1) = 0.5, and so on.
			 * XNA's pitch values are on the domain, OpenAL's are on the range.
			 * Remember: the XNA limit is arbitrarily between two octaves on the domain.
			 * To convert, we just plug XNA pitch into f(x).
			 * -flibit
			 */
			if (!INTERNAL_isXACTSource && (xnaPitch < -1.0f || xnaPitch > 1.0f))
			{
				throw new Exception("XNA PITCH MUST BE WITHIN [-1.0f, 1.0f]!");
			}
			return (float) Math.Pow(2, xnaPitch);
		}

		#endregion

		#region Internal Constructor

		internal SoundEffectInstance(SoundEffect parent)
		{
			INTERNAL_parentEffect = parent;
		}

		#endregion

		#region Destructor

		~SoundEffectInstance()
		{
			Dispose();
		}

		#endregion

		#region Public Dispose Method

		public virtual void Dispose()
		{
			if (!IsDisposed)
			{
				Stop(true);
				IsDisposed = true;
			}
		}

		#endregion

		#region Public 3D Audio Methods

		public void Apply3D(AudioListener listener, AudioEmitter emitter)
		{
			if (INTERNAL_alSource == 0)
			{
				return;
			}

			// Set up orientation matrix
			Matrix orientation = Matrix.CreateWorld(Vector3.Zero, listener.Forward, listener.Up);

			// Set up our final position according to orientation of listener
			position = Vector3.Transform(emitter.Position - listener.Position, orientation);
			if (position != Vector3.Zero)
			{
				position.Normalize();
			}

			// Set the position based on relative positon
			AL10.alSource3f(
				INTERNAL_alSource,
				AL10.AL_POSITION,
				position.X,
				position.Y,
				position.Z
			);

			// We positional now
			INTERNAL_positionalAudio = true;
		}

		public void Apply3D(AudioListener[] listeners, AudioEmitter emitter)
		{
			throw new NotSupportedException("OpenAL can only make use of one listener.");
		}

		#endregion

		#region Public Playback Methods

		public virtual void Play()
		{
			if (State != SoundState.Stopped && INTERNAL_alSource != 0) // FIXME: alSource check part of timer hack!
			{
				return;
			}

			if (INTERNAL_alSource != 0)
			{
				// The sound has stopped, but hasn't cleaned up yet...
				AL10.alSourceStop(INTERNAL_alSource);
				AL10.alDeleteSources((IntPtr) 1, ref INTERNAL_alSource);
				INTERNAL_alSource = 0;
			}

			AL10.alGenSources((IntPtr) 1, out INTERNAL_alSource);
			if (INTERNAL_alSource == 0)
			{
				System.Console.WriteLine("WARNING: AL SOURCE WAS NOT AVAILABLE. SKIPPING.");
				return;
			}

			// Attach the buffer to this source
			AL10.alSourcei(
				INTERNAL_alSource,
				AL10.AL_BUFFER,
				(int) INTERNAL_parentEffect.INTERNAL_buffer
			);

			// Apply Pan/Position
			if (INTERNAL_positionalAudio)
			{
				INTERNAL_positionalAudio = false;
				AL10.alSource3f(
					INTERNAL_alSource,
					AL10.AL_POSITION,
					position.X,
					position.Y,
					position.Z
				);
			}
			else
			{
				Pan = Pan;
			}

			// Reassign Properties, in case the AL properties need to be applied.
			Volume = Volume;
			IsLooped = IsLooped;
			Pitch = Pitch;

			// Apply EFX
			if (INTERNAL_alEffectSlot != 0)
			{
				AL10.alSource3i(
					INTERNAL_alSource,
					EFX.AL_AUXILIARY_SEND_FILTER,
					(int) INTERNAL_alEffectSlot,
					0,
					0
				);
			}

			AL10.alSourcePlay(INTERNAL_alSource);
		}

		public void Pause()
		{
			if (INTERNAL_alSource != 0 && State == SoundState.Playing)
			{
				AL10.alSourcePause(INTERNAL_alSource);
			}
		}

		public void Resume()
		{
			if (INTERNAL_alSource != 0 && State == SoundState.Paused)
			{
				AL10.alSourcePlay(INTERNAL_alSource);
			}
		}

		public void Stop()
		{
			if (INTERNAL_alSource != 0)
			{
				AL10.alSourceStop(INTERNAL_alSource);
				AL10.alDeleteSources((IntPtr) 1, ref INTERNAL_alSource);
				INTERNAL_alSource = 0;
			}
		}

		public void Stop(bool immediate)
		{
			Stop();
		}

		#endregion

		#region Internal EFX Methods

		internal void INTERNAL_applyEffect(DSPEffect effectSlotHandle)
		{
			INTERNAL_alEffectSlot = effectSlotHandle.Handle;
			if (INTERNAL_alSource != 0)
			{
				AL10.alSource3i(
					INTERNAL_alSource,
					EFX.AL_AUXILIARY_SEND_FILTER,
					(int) INTERNAL_alEffectSlot,
					0,
					0
				);
			}
		}

		internal void INTERNAL_applyFilter(
			DSPFilter filter,
			float hfGain,
			float lfGain
		) {
			if (FilterType == -1)
			{
				return;
			}
			else if (FilterType == 0)
			{
				filter.ApplyLowPassFilter(INTERNAL_alSource, hfGain);
			}
			else if (FilterType == 1)
			{
				filter.ApplyHighPassFilter(INTERNAL_alSource, lfGain);
			}
			else if (FilterType == 2)
			{
				filter.ApplyBandPassFilter(INTERNAL_alSource, hfGain, lfGain);
			}
			if (INTERNAL_alSource != 0)
			{
				AL10.alSourcei(
					INTERNAL_alSource,
					EFX.AL_DIRECT_FILTER,
					(int) filter.Handle
				);
			}
		}

		#endregion
	}
}
