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
using System.IO;

using Microsoft.Xna.Framework.Audio;
#endregion

namespace Microsoft.Xna.Framework.Media
{
	public sealed class Song : IEquatable<Song>, IDisposable
	{
		#region Public Properties

		/// <summary>
		/// Gets the Album on which the Song appears.
		/// </summary>
		// TODO: A real Vorbis stream would have this info.
		public Album Album
		{
			get
			{
				return null;
			}
		}

		/// <summary>
		/// Gets the Artist of the Song.
		/// </summary>
		// TODO: A real Vorbis stream would have this info.
		public Artist Artist
		{
			get
			{
				return null;
			}
		}

		/// <summary>
		/// Gets the Genre of the Song.
		/// </summary>
		// TODO: A real Vorbis stream would have this info.
		public Genre Genre
		{
			get
			{
				return null;
			}
		}

		// TODO: A real Vorbis stream would have this info.
		public TimeSpan Duration
		{
			get;
			private set;
		}

		// TODO: A real Vorbis stream would have this info.
		public TimeSpan Position
		{
			get
			{
				return new TimeSpan(0);
			}
		}

		public bool IsProtected
		{
			get
			{
				return false;
			}
		}

		public bool IsRated
		{
			get
			{
				return false;
			}
		}

		public string Name
		{
			get;
			private set;
		}

		public int PlayCount
		{
			get;
			private set;
		}

		public int Rating
		{
			get
			{
				return 0;
			}
		}

		// TODO: Could be obtained with Vorbis metadata
		public int TrackNumber
		{
			get
			{
				return 0;
			}
		}

		public bool IsDisposed
		{
			get;
			private set;
		}

		#endregion

		#region Internal Properties

		internal string FilePath
		{
			get;
			private set;
		}

		internal float Volume
		{
			get
			{
				return soundStream.Volume;
			}
			set
			{
				soundStream.Volume = value;
			}
		}

		#endregion

		#region Internal Function Pointer Types

		internal delegate void FinishedPlayingHandler(object sender, EventArgs args);

		#endregion

		#region Private Variables

		private DynamicSoundEffectInstance soundStream;
		private Vorbisfile.OggVorbis_File vorbisFile = new Vorbisfile.OggVorbis_File();
		private byte[] vorbisBuffer = new byte[4096];

		#endregion

		#region Constructors, Deconstructor, Dispose()

		internal Song(string fileName, int durationMS) : this(fileName)
		{
			Duration = TimeSpan.FromMilliseconds(durationMS);
		}

		internal Song(string fileName)
		{
			FilePath = fileName;
			Name = Path.GetFileNameWithoutExtension(FilePath);
			Vorbisfile.ov_fopen(fileName, ref vorbisFile);
			Vorbisfile.vorbis_info fileInfo = Vorbisfile.ov_info(
				ref vorbisFile,
				0
			);
			soundStream = new DynamicSoundEffectInstance(
				fileInfo.rate,
				(AudioChannels) fileInfo.channels
			);
			IsDisposed = false;
		}

		~Song()
		{
			Dispose(true);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		void Dispose(bool disposing)
		{
			if (disposing)
			{
				soundStream.Dispose();
				soundStream = null;
				Vorbisfile.ov_clear(ref vorbisFile);
			}
			IsDisposed = true;
		}

		#endregion

		#region Internal Playback Methods

		internal void Play()
		{
			soundStream.BufferNeeded += QueueBuffer;
			QueueBuffer(null, null);
			QueueBuffer(null, null);
			soundStream.Play();
			PlayCount += 1;
		}

		internal void Resume()
		{
			soundStream.Resume();
		}

		internal void Pause()
		{
			soundStream.Pause();
		}

		internal void Stop()
		{
			soundStream.Stop();
			soundStream.BufferNeeded -= QueueBuffer;
			PlayCount = 0;
		}

		#endregion

		#region Internal Event Handler Methods

		internal void QueueBuffer(object sender, EventArgs args)
		{
			// Fill a List (ugh) with a series of ov_read blocks.
			List<byte> totalBuf = new List<byte>();
			int bs = 0;
			long len = 0;
			do
			{
				len = Vorbisfile.ov_read(
					ref vorbisFile,
					vorbisBuffer,
					vorbisBuffer.Length,
					0,
					2,
					1,
					ref bs
				);
				if (len == vorbisBuffer.Length)
				{
					totalBuf.AddRange(vorbisBuffer);
				}
				else if (len > 0)
				{
					// UGH -flibit
					byte[] smallBuf = new byte[len];
					Array.Copy(vorbisBuffer, smallBuf, len);
					totalBuf.AddRange(smallBuf);
				}
			} while (len > 0 && totalBuf.Count < 16384); // 8192 16-bit samples

			// If we're at the end of the file, stop!
			if (totalBuf.Count == 0)
			{
				soundStream.BufferNeeded -= QueueBuffer;
				OnFinishedPlaying();
				return;
			}

			// Send the filled buffer to the stream.
			soundStream.SubmitBuffer(
				totalBuf.ToArray(),
				0,
				totalBuf.Count
			);
		}

		internal void OnFinishedPlaying()
		{
			MediaPlayer.OnSongFinishedPlaying(null, null);
		}

		#endregion

		#region Public Comparison Methods/Operators

		public bool Equals(Song song) 
		{
			return (((object) song) != null) && (Name == song.Name);
		}

		public override bool Equals(Object obj)
		{
			if (obj == null)
			{
				return false;
			}
			return Equals(obj as Song);
		}

		public static bool operator ==(Song song1, Song song2)
		{
			if (((object) song1) == null)
			{
				return ((object) song2) == null;
			}
			return song1.Equals(song2);
		}

		public static bool operator !=(Song song1, Song song2)
		{
			return !(song1 == song2);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		#endregion

		#region Public Static Methods

		/// <summary>
		/// Constructs a new Song object based on the specified URI.
		/// </summary>
		/// <remarks>
		/// This method matches the signature of the one in XNA4, however we currently can't play remote songs, so
		/// the URI is required to be a file name and the code uses the LocalPath property only.
		/// </remarks>
		/// <param name="name">Name of the song.</param>
		/// <param name="uri">Uri object that represents the URI.</param>
		/// <returns>Song object that can be used to play the song.</returns>
		public static Song FromUri(string name, Uri uri)
		{
			if (!uri.IsFile)
			{
				throw new InvalidOperationException("Only local file URIs are supported for now");
			}

			return new Song(uri.LocalPath)
			{
				Name = name
			};
		}

		#endregion
	}
}
