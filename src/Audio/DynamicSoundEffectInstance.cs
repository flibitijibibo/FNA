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
	// http://msdn.microsoft.com/en-us/library/microsoft.xna.framework.audio.dynamicsoundeffectinstance.aspx
	public sealed class DynamicSoundEffectInstance : SoundEffectInstance
	{
		#region Public Properties

		public int PendingBufferCount
		{
			get;
			private set;
		}

		#endregion

		#region Private XNA Variables

		private int sampleRate;
		private AudioChannels channels;

		#endregion

		#region Private OpenAL Variables

		private Queue<uint> queuedBuffers;
		private Queue<uint> buffersToQueue;
		private Queue<uint> availableBuffers;

		#endregion

		#region Private Static XNA->AL Dictionaries

		private static readonly Dictionary<AudioChannels, int> XNAToShort = new Dictionary<AudioChannels, int>
		{
			{ AudioChannels.Mono, AL10.AL_FORMAT_MONO16 },
			{ AudioChannels.Stereo, AL10.AL_FORMAT_STEREO16 }
		};

		private static readonly Dictionary<AudioChannels, int> XNAToFloat = new Dictionary<AudioChannels, int>
		{
			{ AudioChannels.Mono, ALEXT.AL_FORMAT_MONO_FLOAT32 },
			{ AudioChannels.Stereo, ALEXT.AL_FORMAT_STEREO_FLOAT32 }
		};

		#endregion

		#region BufferNeeded Event

		public event EventHandler<EventArgs> BufferNeeded;

		#endregion

		#region Public Constructor

		public DynamicSoundEffectInstance(int sampleRate, AudioChannels channels) : base(null)
		{
			this.sampleRate = sampleRate;
			this.channels = channels;

			PendingBufferCount = 0;

			queuedBuffers = new Queue<uint>();
			buffersToQueue = new Queue<uint>();
			availableBuffers = new Queue<uint>();
		}

		#endregion

		#region Destructor

		~DynamicSoundEffectInstance()
		{
			Dispose();
		}

		#endregion

		#region Public Dispose Method

		public override void Dispose()
		{
			if (!IsDisposed)
			{
				base.Dispose(); // Will call Stop(true);

				// Delete all known buffer objects
				uint qBuffer;
				while (queuedBuffers.Count > 0)
				{
					qBuffer = queuedBuffers.Dequeue();
					AL10.alDeleteBuffers((IntPtr) 1, ref qBuffer);
				}
				queuedBuffers = null;
				while (availableBuffers.Count > 0)
				{
					qBuffer = availableBuffers.Dequeue();
					AL10.alDeleteBuffers((IntPtr) 1, ref qBuffer);
				}
				availableBuffers = null;
				while (buffersToQueue.Count > 0)
				{
					qBuffer = buffersToQueue.Dequeue();
					AL10.alDeleteBuffers((IntPtr) 1, ref qBuffer);
				}
				buffersToQueue = null;

				IsDisposed = true;
			}
		}

		#endregion

		#region Public Time/Sample Information Methods

		public TimeSpan GetSampleDuration(int sizeInBytes)
		{
			return SoundEffect.GetSampleDuration(
				sizeInBytes,
				sampleRate,
				channels
			);
		}

		public int GetSampleSizeInBytes(TimeSpan duration)
		{
			return SoundEffect.GetSampleSizeInBytes(
				duration,
				sampleRate,
				channels
			);
		}

		#endregion

		#region Public SubmitBuffer Methods

		public void SubmitBuffer(byte[] buffer)
		{
			this.SubmitBuffer(buffer, 0, buffer.Length);
		}

		public void SubmitBuffer(byte[] buffer, int offset, int count)
		{
			// Generate a buffer if we don't have any to use.
			if (availableBuffers.Count == 0)
			{
				uint buf;
				AL10.alGenBuffers((IntPtr) 1, out buf);
				availableBuffers.Enqueue(buf);
			}

			// Push the data to OpenAL.
			uint newBuf = availableBuffers.Dequeue();
			AL10.alBufferData(
				newBuf,
				XNAToShort[channels],
				buffer, // TODO: offset -flibit
				(IntPtr) count,
				(IntPtr) sampleRate
			);

			// If we're already playing, queue immediately.
			if (State == SoundState.Playing)
			{
				AL10.alSourceQueueBuffers(
					INTERNAL_alSource,
					(IntPtr) 1,
					ref newBuf
				);
				queuedBuffers.Enqueue(newBuf);
			}
			else
			{
				buffersToQueue.Enqueue(newBuf);
			}

			PendingBufferCount += 1;
		}

		#endregion

		#region Public Play Method

		public override void Play()
		{
			Play(true);
		}

		#endregion

		#region Internal Play Method

		internal void Play(bool isManaged)
		{
			if (State != SoundState.Stopped)
			{
				return; // No-op if we're already playing.
			}

			if (INTERNAL_alSource != 0)
			{
				// The sound has stopped, but hasn't cleaned up yet...
				AL10.alSourceStop(INTERNAL_alSource);
				AL10.alDeleteSources((IntPtr) 1, ref INTERNAL_alSource);
				INTERNAL_alSource = 0;
			}
			while (queuedBuffers.Count > 0)
			{
				availableBuffers.Enqueue(queuedBuffers.Dequeue());
			}

			AL10.alGenSources((IntPtr) 1, out INTERNAL_alSource);
			if (INTERNAL_alSource == 0)
			{
				System.Console.WriteLine("WARNING: AL SOURCE WAS NOT AVAILABLE. SKIPPING.");
				return;
			}

			// Queue the buffers to this source
			while (buffersToQueue.Count > 0)
			{
				uint nextBuf = buffersToQueue.Dequeue();
				queuedBuffers.Enqueue(nextBuf);
				AL10.alSourceQueueBuffers(
					INTERNAL_alSource,
					(IntPtr) 1,
					ref nextBuf
				);
			}

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

			// Finally.
			AL10.alSourcePlay(INTERNAL_alSource);
			if (isManaged)
			{
				OpenALDevice.Instance.dynamicInstancePool.Add(this);
			}

			// ... but wait! What if we need moar buffers?
			if (PendingBufferCount <= 2 && BufferNeeded != null)
			{
				BufferNeeded(this, null);
			}
		}

		#endregion

		#region Internal Update Method

		internal bool Update()
		{
			if (State == SoundState.Stopped)
			{
				/* If we've stopped, remove ourselves from the list.
				 * Do NOT do anything else, Play/Stop/Dispose do this!
				 * -flibit
				 */
				return false;
			}

			// Get the processed buffers.
			int finishedBuffers;
			AL10.alGetSourcei(
				INTERNAL_alSource,
				AL10.AL_BUFFERS_PROCESSED,
				out finishedBuffers
			);
			if (finishedBuffers == 0)
			{
				// Nothing to do... yet.
				return true;
			}

			uint[] bufs = new uint[finishedBuffers];
			AL10.alSourceUnqueueBuffers(
				INTERNAL_alSource,
				(IntPtr) finishedBuffers,
				bufs
			);
			PendingBufferCount -= finishedBuffers;
			if (BufferNeeded != null)
			{
				// PendingBufferCount changed during playback, trigger now!
				BufferNeeded(this, null);
			}

			// Error check our queuedBuffers list.
			for (int i = 0; i < finishedBuffers; i += 1)
			{
				uint newBuf = queuedBuffers.Dequeue();
				if (newBuf != bufs[i])
				{
					throw new Exception("Buffer desync!");
				}
				availableBuffers.Enqueue(newBuf);
			}

			// Notify the application that we need moar buffers!
			if (PendingBufferCount <= 2 && BufferNeeded != null)
			{
				BufferNeeded(this, null);
			}

			return true;
		}

		#endregion

		#region Public FNA Extension Methods

		/* THIS IS AN EXTENSION OF THE XNA4 API! */
		public void SubmitFloatBufferEXT(float[] buffer)
		{
			/* Float samples are the typical format received from decoders.
			 * We currently use this for the VideoPlayer.
			 * -flibit
			 */

			// Generate a buffer if we don't have any to use.
			if (availableBuffers.Count == 0)
			{
				uint buf;
				AL10.alGenBuffers((IntPtr) 1, out buf);
				availableBuffers.Enqueue(buf);
			}

			// Push the data to OpenAL.
			uint newBuf = availableBuffers.Dequeue();
			AL10.alBufferData(
				newBuf,
				XNAToFloat[channels],
				buffer,
				(IntPtr) (buffer.Length * 4),
				(IntPtr) sampleRate
			);

			// If we're already playing, queue immediately.
			if (State == SoundState.Playing)
			{
				AL10.alSourceQueueBuffers(
					INTERNAL_alSource,
					(IntPtr) 1,
					ref newBuf
				);
				queuedBuffers.Enqueue(newBuf);
			}
			else
			{
				buffersToQueue.Enqueue(newBuf);
			}

			PendingBufferCount += 1;
		}

		#endregion
	}
}
