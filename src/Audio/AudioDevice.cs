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
using System.Collections.ObjectModel;
#endregion

namespace Microsoft.Xna.Framework.Audio
{
	internal static class AudioDevice
	{
		#region RendererDetail List

		public static ReadOnlyCollection<RendererDetail> Renderers;

		#endregion

		#region Internal AL Device

		// FIXME: readonly? -flibit
		public static IALDevice ALDevice;

		#endregion

		#region SoundEffect Management Variables

		// FIXME: readonly? -flibit

		// Used to store SoundEffectInstances generated internally.
		public static List<SoundEffectInstance> InstancePool;

		// Used to store all DynamicSoundEffectInstances, to check buffer counts.
		public static List<DynamicSoundEffectInstance> DynamicInstancePool;

		#endregion

		#region Public Static Initialize Method

		public static void Initialize()
		{
			// We should only have one of these!
			if (ALDevice != null)
			{
				System.Console.WriteLine("ALDevice already exists, overwriting!");
			}

			bool disableSound = Environment.GetEnvironmentVariable(
				"FNA_AUDIO_DISABLE_SOUND"
			) == "1";

			if (disableSound)
			{
				ALDevice = new NullDevice();
			}
			else
			{
				try
				{
					ALDevice = new OpenALDevice();
				}
				catch(DllNotFoundException e)
				{
					System.Console.WriteLine("OpenAL not found! Need FNA.dll.config?");
					throw e;
				}
				catch(Exception)
				{
					/* We ignore and device creation exceptions,
					 * as they are handled down the line with Instance != null
					 */
				}
			}

			// Populate device list
			if (ALDevice != null)
			{
				Renderers = ALDevice.GetDevices();

				InstancePool = new List<SoundEffectInstance>();
				DynamicInstancePool = new List<DynamicSoundEffectInstance>();
			}
			else
			{
				Renderers = new ReadOnlyCollection<RendererDetail>(new List<RendererDetail>());
			}
		}

		#endregion

		#region Public Static Dispose Method

		public static void Dispose()
		{
			if (ALDevice != null)
			{
				InstancePool.Clear();
				DynamicInstancePool.Clear();
				ALDevice.Dispose();
				// ALDevice = null; <- May cause Exceptions!
			}
		}

		#endregion

		#region Public Static Update Methods

		public static void Update()
		{
			if (ALDevice != null)
			{
				ALDevice.Update();

				for (int i = 0; i < InstancePool.Count; i += 1)
				{
					if (InstancePool[i].State == SoundState.Stopped)
					{
						InstancePool[i].Dispose();
						InstancePool.RemoveAt(i);
						i -= 1;
					}
				}

				for (int i = 0; i < DynamicInstancePool.Count; i += 1)
				{
					if (!DynamicInstancePool[i].Update())
					{
						DynamicInstancePool.RemoveAt(i);
						i -= 1;
					}
				}
			}
		}

		#endregion

		#region Public Static Buffer Methods

		public static IALBuffer GenBuffer()
		{
			if (ALDevice == null)
			{
				ThrowNoAudioException();
			}
			return ALDevice.GenBuffer();
		}

		public static IALBuffer GenBuffer(
			byte[] data,
			uint sampleRate,
			uint channels,
			uint loopStart,
			uint loopEnd,
			bool isADPCM,
			uint formatParameter
		) {
			if (ALDevice == null)
			{
				ThrowNoAudioException();
			}
			return ALDevice.GenBuffer(
				data,
				sampleRate,
				channels,
				loopStart,
				loopEnd,
				isADPCM,
				formatParameter
			);
		}

		#endregion

		#region Private Static Error Method

		private static void ThrowNoAudioException()
		{
			Game.Instance.Platform.ShowRuntimeError(
				Game.Instance.Window.Title,
				"Could not find a suitable audio device. " +
				" Verify that a sound card is\ninstalled," +
				" and check the driver properties to make" +
				" sure it is not disabled."
			);
			throw new NoAudioHardwareException();
		}

		#endregion
	}
}
