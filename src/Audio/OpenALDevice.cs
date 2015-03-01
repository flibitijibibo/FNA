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

using OpenAL;
#endregion

namespace Microsoft.Xna.Framework.Audio
{
	internal sealed class OpenALDevice
	{
		#region The OpenAL Device Instance

		public static OpenALDevice Instance
		{
			get;
			private set;
		}

		#endregion

		#region Private ALC Variables

		// OpenAL Device/Context Handles
		private IntPtr alDevice;
		private IntPtr alContext;

		#endregion

		#region Private SoundEffect Management Variables

		// Used to store SoundEffectInstances generated internally.
		internal List<SoundEffectInstance> instancePool;

		// Used to store all DynamicSoundEffectInstances, to check buffer counts.
		internal List<DynamicSoundEffectInstance> dynamicInstancePool;

		#endregion

		#region Public Initializer

		public static void Initialize()
		{
			// We should only have one of these!
			if (Instance != null)
			{
				throw new Exception("OpenALDevice already created!");
			}

			try
			{
				Instance = new OpenALDevice();
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

		#endregion

		#region Private Constructor

		private OpenALDevice()
		{
			alDevice = ALC10.alcOpenDevice(string.Empty);
			if (CheckALCError() || alDevice == IntPtr.Zero)
			{
				throw new Exception("Could not open audio device!");
			}

			int[] attribute = new int[0];
			alContext = ALC10.alcCreateContext(alDevice, attribute);
			if (CheckALCError() || alContext == IntPtr.Zero)
			{
				Dispose();
				throw new Exception("Could not create OpenAL context");
			}

			ALC10.alcMakeContextCurrent(alContext);
			if (CheckALCError())
			{
				Dispose();
				throw new Exception("Could not make OpenAL context current");
			}

			float[] ori = new float[]
			{
				0.0f, 0.0f, -1.0f, 0.0f, 1.0f, 0.0f
			};
			AL10.alListenerfv(AL10.AL_ORIENTATION, ori);
			AL10.alListener3f(AL10.AL_POSITION, 0.0f, 0.0f, 0.0f);
			AL10.alListener3f(AL10.AL_VELOCITY, 0.0f, 0.0f, 0.0f);
			AL10.alListenerf(AL10.AL_GAIN, 1.0f);

			// We do NOT use automatic attenuation! XNA does not do this!
			AL10.alDistanceModel(AL10.AL_NONE);

			instancePool = new List<SoundEffectInstance>();
			dynamicInstancePool = new List<DynamicSoundEffectInstance>();
		}

		#endregion

		#region Public Dispose Method

		public void Dispose()
		{
			ALC10.alcMakeContextCurrent(IntPtr.Zero);
			if (alContext != IntPtr.Zero)
			{
				ALC10.alcDestroyContext(alContext);
				alContext = IntPtr.Zero;
			}
			if (alDevice != IntPtr.Zero)
			{
				ALC10.alcCloseDevice(alDevice);
				alDevice = IntPtr.Zero;
			}
			Instance = null;
		}

		#endregion

		#region Public Update Methods

		public void Update()
		{
#if DEBUG
			CheckALError();
#endif

			for (int i = 0; i < instancePool.Count; i += 1)
			{
				if (instancePool[i].State == SoundState.Stopped)
				{
					instancePool[i].Dispose();
					instancePool.RemoveAt(i);
					i -= 1;
				}
			}

			for (int i = 0; i < dynamicInstancePool.Count; i += 1)
			{
				if (!dynamicInstancePool[i].Update())
				{
					dynamicInstancePool.Remove(dynamicInstancePool[i]);
					i -= 1;
				}
			}
		}

		#endregion

		#region Private OpenAL Error Check Methods

		private void CheckALError()
		{
			int err = AL10.alGetError();

			if (err == AL10.AL_NO_ERROR)
			{
				return;
			}

			System.Console.WriteLine("OpenAL Error: " + err.ToString());
		}

		private bool CheckALCError()
		{
			bool retVal = false;
			int err = ALC10.alcGetError(alDevice);

			if (err == ALC10.ALC_NO_ERROR)
			{
				return false;
			}

			System.Console.WriteLine("OpenAL Device Error: " + err.ToString());
			return true;
		}

		#endregion
	}
}
