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
	/* This class is meant to be a compact container for platform-specific
	 * effect work. Keep general XACT stuff out of here.
	 * -flibit
	 */
	internal abstract class DSPEffect
	{
		#region Public Properties

		public uint Handle
		{
			get;
			private set;
		}

		#endregion

		#region Protected Variables

		protected uint effectHandle;

		#endregion

		#region Public Constructor

		public DSPEffect()
		{
			// Generate the EffectSlot and Effect
			uint handle;
			EFX.alGenAuxiliaryEffectSlots((IntPtr) 1, out handle);
			Handle = handle;
			EFX.alGenEffects((IntPtr) 1, out effectHandle);
		}

		#endregion

		#region Public Dispose Method

		public void Dispose()
		{
			// Delete EFX data
			uint handle = Handle;
			EFX.alDeleteAuxiliaryEffectSlots((IntPtr) 1, ref handle);
			EFX.alDeleteEffects((IntPtr) 1, ref effectHandle);
		}

		#endregion

		#region Abstract Methods

		public abstract void CommitChanges();

		#endregion
	}

	internal class DSPReverbEffect : DSPEffect
	{
		#region Public Constructor

		public DSPReverbEffect(DSPParameter[] parameters) : base()
		{
			// Set up the Reverb Effect
			EFX.alEffecti(
				effectHandle,
				EFX.AL_EFFECT_TYPE,
				EFX.AL_EFFECT_EAXREVERB
			);

			// Apply initial values
			SetReflectionsDelay(parameters[0].Value);
			SetReverbDelay(parameters[1].Value);
			SetPositionLeft(parameters[2].Value);
			SetPositionRight(parameters[3].Value);
			SetPositionLeftMatrix(parameters[4].Value);
			SetPositionRightMatrix(parameters[5].Value);
			SetEarlyDiffusion(parameters[6].Value);
			SetLateDiffusion(parameters[7].Value);
			SetLowEQGain(parameters[8].Value);
			SetLowEQCutoff(parameters[9].Value);
			SetHighEQGain(parameters[10].Value);
			SetHighEQCutoff(parameters[11].Value);
			SetRearDelay(parameters[12].Value);
			SetRoomFilterFrequency(parameters[13].Value);
			SetRoomFilterMain(parameters[14].Value);
			SetRoomFilterHighFrequency(parameters[15].Value);
			SetReflectionsGain(parameters[16].Value);
			SetReverbGain(parameters[17].Value);
			SetDecayTime(parameters[18].Value);
			SetDensity(parameters[19].Value);
			SetRoomSize(parameters[20].Value);
			SetWetDryMix(parameters[21].Value);

			// Bind the Effect to the EffectSlot. XACT will use the EffectSlot.
			EFX.alAuxiliaryEffectSloti(
				Handle,
				EFX.AL_EFFECTSLOT_EFFECT,
				(int) effectHandle
			);
		}

		#endregion

		#region Public Methods

		public void SetReflectionsDelay(float value)
		{
			EFX.alEffectf(
				effectHandle,
				EFX.AL_EAXREVERB_REFLECTIONS_DELAY,
				value / 1000.0f
			);
		}

		public void SetReverbDelay(float value)
		{
			EFX.alEffectf(
				effectHandle,
				EFX.AL_EAXREVERB_LATE_REVERB_DELAY,
				value / 1000.0f
			);
		}

		public void SetPositionLeft(float value)
		{
			// No known mapping :(
		}

		public void SetPositionRight(float value)
		{
			// No known mapping :(
		}

		public void SetPositionLeftMatrix(float value)
		{
			// No known mapping :(
		}

		public void SetPositionRightMatrix(float value)
		{
			// No known mapping :(
		}

		public void SetEarlyDiffusion(float value)
		{
			// Same as late diffusion, whatever... -flibit
			EFX.alEffectf(
				effectHandle,
				EFX.AL_EAXREVERB_DIFFUSION,
				value / 15.0f
			);
		}

		public void SetLateDiffusion(float value)
		{
			// Same as early diffusion, whatever... -flibit
			EFX.alEffectf(
				effectHandle,
				EFX.AL_EAXREVERB_DIFFUSION,
				value / 15.0f
			);
		}

		public void SetLowEQGain(float value)
		{
			// Cutting off volumes from 0db to 4db! -flibit
			EFX.alEffectf(
				effectHandle,
				EFX.AL_EAXREVERB_GAINLF,
				Math.Min(
					XACTCalculator.CalculateAmplitudeRatio(
						value - 8.0f
					),
					1.0f
				)
			);
		}

		public void SetLowEQCutoff(float value)
		{
			EFX.alEffectf(
				effectHandle,
				EFX.AL_EAXREVERB_LFREFERENCE,
				(value * 50.0f) + 50.0f
			);
		}

		public void SetHighEQGain(float value)
		{
			EFX.alEffectf(
				effectHandle,
				EFX.AL_EAXREVERB_GAINHF,
				XACTCalculator.CalculateAmplitudeRatio(
					value - 8.0f
				)
			);
		}

		public void SetHighEQCutoff(float value)
		{
			EFX.alEffectf(
				effectHandle,
				EFX.AL_EAXREVERB_HFREFERENCE,
				(value * 500.0f) + 1000.0f
			);
		}

		public void SetRearDelay(float value)
		{
			// No known mapping :(
		}

		public void SetRoomFilterFrequency(float value)
		{
			// No known mapping :(
		}

		public void SetRoomFilterMain(float value)
		{
			// No known mapping :(
		}

		public void SetRoomFilterHighFrequency(float value)
		{
			// No known mapping :(
		}

		public void SetReflectionsGain(float value)
		{
			// Cutting off possible float values above 3.16, for EFX -flibit
			EFX.alEffectf(
				effectHandle,
				EFX.AL_EAXREVERB_REFLECTIONS_GAIN,
				Math.Min(
					XACTCalculator.CalculateAmplitudeRatio(value),
					3.16f
				)
			);
		}

		public void SetReverbGain(float value)
		{
			// Cutting off volumes from 0db to 20db! -flibit
			EFX.alEffectf(
				effectHandle,
				EFX.AL_EAXREVERB_GAIN,
				Math.Min(
					XACTCalculator.CalculateAmplitudeRatio(value),
					1.0f
				)
			);
		}

		public void SetDecayTime(float value)
		{
			/* FIXME: WTF is with this XACT value?
			 * XACT: 0-30 equal to 0.1-inf seconds?!
			 * EFX: 0.1-20 seconds
			 * -flibit
			EFX.alEffectf(
				effectHandle,
				EFX.AL_EAXREVERB_GAIN,
				value
			);
			*/
		}

		public void SetDensity(float value)
		{
			EFX.alEffectf(
				effectHandle,
				EFX.AL_EAXREVERB_DENSITY,
				value / 100.0f
			);
		}

		public void SetRoomSize(float value)
		{
			// No known mapping :(
		}

		public void SetWetDryMix(float value)
		{
			// No known mapping :(
		}

		#endregion

		#region DSPEffect Methods

		public override void CommitChanges()
		{
			EFX.alAuxiliaryEffectSloti(
				Handle,
				EFX.AL_EFFECTSLOT_EFFECT,
				(int) effectHandle
			);
		}

		#endregion
	}
}
