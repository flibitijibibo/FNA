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
using System.Threading;

using OpenTK;
using OpenTK.Audio.OpenAL;
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

		#region Public EFX Entry Points

		public EffectsExtension EFX
		{
			get;
			private set;
		}

		#endregion

		#region Private ALC Variables

		// OpenAL Device/Context Handles
		private IntPtr alDevice;
		private ContextHandle alContext;

		#endregion

		#region Private SoundEffect Management Variables

		// Used to store SoundEffectInstances generated internally.
		internal List<SoundEffectInstance> instancePool;

		// Used to store all DynamicSoundEffectInstances, to check buffer counts.
		internal List<DynamicSoundEffectInstance> dynamicInstancePool;

		#endregion

		#region Private Audio Thread Variables

		private Thread audioThread;
		private bool exitThread;

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
				System.Console.WriteLine("OpenAL not found! Need SDL2-CS.dll.config?");
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
			alDevice = Alc.OpenDevice(string.Empty);
			if (CheckALCError("Could not open AL device") || alDevice == IntPtr.Zero)
			{
				throw new Exception("Could not open AL device!");
			}

			int[] attribute = new int[0];
			alContext = Alc.CreateContext(alDevice, attribute);
			if (CheckALCError("Could not create OpenAL context") || alContext == ContextHandle.Zero)
			{
				Dispose();
				throw new Exception("Could not create OpenAL context");
			}

			Alc.MakeContextCurrent(alContext);
			if (CheckALCError("Could not make OpenAL context current"))
			{
				Dispose();
				throw new Exception("Could not make OpenAL context current");
			}

			EFX = new EffectsExtension();

			float[] ori = new float[]
			{
				0.0f, 0.0f, -1.0f, 0.0f, 1.0f, 0.0f
			};
			AL.Listener(ALListenerfv.Orientation, ref ori);
			AL.Listener(ALListener3f.Position, 0.0f, 0.0f, 0.0f);
			AL.Listener(ALListener3f.Velocity, 0.0f, 0.0f, 0.0f);
			AL.Listener(ALListenerf.Gain, 1.0f);

			// We do NOT use automatic attenuation! XNA does not do this!
			AL.DistanceModel(ALDistanceModel.None);

			instancePool = new List<SoundEffectInstance>();
			dynamicInstancePool = new List<DynamicSoundEffectInstance>();

			exitThread = false;
			audioThread = new Thread(AudioThread);
			audioThread.Start();
		}

		#endregion

		#region Public Dispose Method

		public void Dispose()
		{
			exitThread = true;
			audioThread.Join();
			Alc.MakeContextCurrent(ContextHandle.Zero);
			if (alContext != ContextHandle.Zero)
			{
				Alc.DestroyContext(alContext);
				alContext = ContextHandle.Zero;
			}
			if (alDevice != IntPtr.Zero)
			{
				Alc.CloseDevice(alDevice);
				alDevice = IntPtr.Zero;
			}
			Instance = null;
		}

		#endregion

		#region Private Audio Thread Method

		private void AudioThread()
		{
			while (!exitThread)
			{
				CheckALError();

				lock (instancePool)
				{
					for (int i = 0; i < instancePool.Count; i += 1)
					{
						if (instancePool[i].State == SoundState.Stopped)
						{
							instancePool[i].Dispose();
							instancePool.RemoveAt(i);
							i -= 1;
						}
					}
				}

				lock (dynamicInstancePool)
				{
					for (int i = 0; i < dynamicInstancePool.Count; i += 1)
					{
						if (!dynamicInstancePool[i].Update())
						{
							dynamicInstancePool.Remove(dynamicInstancePool[i]);
							i -= 1;
						}
					}
				}

				// Arbitrarily 1 frame in a 60Hz game -flibit
				Thread.Sleep(16);
			}
		}

		#endregion

		#region Private OpenAL Error Check Methods

		private void CheckALError()
		{
			ALError err = AL.GetError();

			if (err == ALError.NoError)
			{
				return;
			}

			System.Console.WriteLine("OpenAL Error: " + err.ToString());
		}

		private bool CheckALCError(string message)
		{
			bool retVal = false;
			AlcError err = Alc.GetError(alDevice);

			if (err != AlcError.NoError)
			{
				System.Console.WriteLine("OpenAL Error: " + err.ToString());
				retVal = true;
			}

			return retVal;
		}

		#endregion
	}
}
