using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NVorbis;
using OpenTK.Audio.OpenAL;

namespace Microsoft.Xna.Framework.Audio
{
    internal static class ALHelper
    {
        public static readonly XRamExtension XRam;
        public static readonly EffectsExtension Efx;

        static ALHelper()
        {
            XRam = new XRamExtension();
            Efx = new EffectsExtension();
        }

        public static bool TryCheck()
        {
            return AL.GetError() != ALError.NoError;
        }
        [Conditional("DEBUG")]
        public static void Check()
        {
            ALError error;
            if ((error = AL.GetError()) != ALError.NoError)
#if DEBUG
                throw new InvalidOperationException(AL.GetErrorString(error));
#else
//                Log("AL Error : " + AL.GetErrorString(error));
                Console.WriteLine("AL Error : " + AL.GetErrorString(error));
#endif
        }

        public static void Log(string message, string module = "OpenAL")
        {
            try
            {
                Console.WriteLine("({0}) [{1}] {2}", DateTime.Now.ToString("HH:mm:ss.fff"), module, message);
                using (var stream = File.Open(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\FEZ\\Debug Log.txt", FileMode.Append))
                {
                    using (var writer = new StreamWriter(stream))
                    {
                        writer.WriteLine("({0}) [{1}] {2}", DateTime.Now.ToString("HH:mm:ss.fff"), module, message);
                    }
                }
            }
            catch (Exception ex)
            {
                // NOT THAT BIG A DEAL GUYS
            }
        }
    }

    public class OggStream : IDisposable
    {
        const int DefaultBufferCount = 6;

        internal int alSourceId;
        internal readonly int[] alBufferIds;
        internal readonly Stack<int> bufferStack;

        Stream underlyingStream;

        internal readonly ReaderWriterLockSlim PreparationLock = new ReaderWriterLockSlim();
        internal readonly ReaderWriterLockSlim StoppingLock = new ReaderWriterLockSlim();

        internal VorbisReader Reader { get; private set; }
        internal bool Precaching { get; private set; }
        internal bool FirstBufferPrecached { get; set; }

        internal int QueuedBuffers { get; set; }
        internal int ProcessedBuffers { get; set; }

        public int BufferCount { get; private set; }
        public string Name { get; private set; }
        public string RealName { get; set; }

        public OggStream(string filename, int bufferCount = DefaultBufferCount) : this(File.OpenRead(filename), bufferCount)
        {
            Name = filename;
        }
        public OggStream(Stream stream, int bufferCount = DefaultBufferCount)
        {
            ALHelper.Check();

            BufferCount = bufferCount;
#if !FAKE
            alBufferIds = OpenALSoundController.Instance.TakeBuffers(BufferCount);
            bufferStack = new Stack<int>(alBufferIds);
            alSourceId = OpenALSoundController.Instance.TakeSource();
#endif
            if (ALHelper.XRam.IsInitialized)
            {
                ALHelper.XRam.SetBufferMode(BufferCount, ref alBufferIds[0], XRamExtension.XRamStorage.Hardware);
                ALHelper.Check();
            }

            Volume = 1;
            lowPass = true;

            underlyingStream = stream;
        }

        public void Prepare(bool asynchronous = false)
        {
            var state = AL.GetSourceState(alSourceId);

            StoppingLock.EnterReadLock();
            {
                switch (state)
                {
                    case ALSourceState.Playing:
                    case ALSourceState.Paused:
                        StoppingLock.ExitReadLock();
                        return;
                }

                PreparationLock.EnterWriteLock();
                {
                    if (Reader == null)
                        Open();

                    if (!Precaching)
                    {
                        Precaching = true;
                        Precache(asynchronous: asynchronous);
                    }
                }
                PreparationLock.ExitWriteLock();
            }
            StoppingLock.ExitReadLock();
        }

        bool lowPass;
        public bool LowPass
        {
            get { return lowPass; }
            set
            {
#if !FAKE
                if (lowPass != value)
                    OpenALSoundController.Instance.SetSourceFiltered(alSourceId, value);
#endif
                lowPass = value;
            }
        }

        public void Play()
        {
            var state = AL.GetSourceState(alSourceId);

            switch (state)
            {
                case ALSourceState.Playing: return;
                case ALSourceState.Paused:
                    Resume();
                    return;
            }

            if (bufferStack.Count == BufferCount)
                Prepare();
            else if (!FirstBufferPrecached)
                ALHelper.Log("Buffers lost for " + RealName + " with source " + alSourceId);

            AL.SourcePlay(alSourceId);
            ALHelper.Check();

            Precaching = false;

            OggStreamer.Instance.AddStream(this);
        }

        public void Pause()
        {
            if (AL.GetSourceState(alSourceId) != ALSourceState.Playing)
                return;

            OggStreamer.Instance.RemoveStream(this);
            AL.SourcePause(alSourceId);
            ALHelper.Check();
        }

        public void Resume()
        {
            if (AL.GetSourceState(alSourceId) != ALSourceState.Paused)
                return;

            OggStreamer.Instance.AddStream(this);
            AL.SourcePlay(alSourceId);
            ALHelper.Check();
        }

        public void Stop()
        {
            var state = AL.GetSourceState(alSourceId);
            if (state == ALSourceState.Playing || state == ALSourceState.Paused)
                StopPlayback();

            StoppingLock.EnterWriteLock();
            {
                if (OggStreamer.HasInstance)
                    OggStreamer.Instance.RemoveStream(this);
            }
            StoppingLock.ExitWriteLock();
        }

        float volume;
        public float Volume
        {
            get { return volume; }
            set
            {
                AL.Source(alSourceId, ALSourcef.Gain, MathHelper.Clamp((volume = value) * globalVolume, 0, 1));
                ALHelper.Check();
            }
        }

        float globalVolume = 1;
        string category;

        public float GlobalVolume
        {
            set
            {
                globalVolume = value;
                Volume = volume;
            }
        }

        public string Category
        {
            get { return category; }
            set
            {
                category = value;
                if (OggStreamer.HasInstance)
                    GlobalVolume = category == "Ambience"
                                       ? OggStreamer.Instance.AmbienceVolume
                                       : OggStreamer.Instance.MusicVolume;
            }
        }

        public bool IsLooped { get; set; }

        public bool IsStopped
        {
            get
            {
                var state = AL.GetSourceState(alSourceId);
                return state == ALSourceState.Stopped;
            }
        }
        public bool IsPlaying
        {
            get
            {
                var state = AL.GetSourceState(alSourceId);
                return state == ALSourceState.Playing;
            }
        }

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;

            //Trace.WriteLine("[OpenAL] Disposing " + RealName + " with source " + alSourceId);

            PreparationLock.EnterWriteLock();
            {
                if (OggStreamer.HasInstance)
                    OggStreamer.Instance.RemoveStream(this);

                StopPlayback();
                Empty();
                Close();
            
#if !FAKE
                if (OpenALSoundController.Instance != null)
                {
                    OpenALSoundController.Instance.ReturnSource(alSourceId);
                    OpenALSoundController.Instance.ReturnBuffers(alBufferIds);
                }
#endif
            }
            PreparationLock.ExitWriteLock();
        }

        void StopPlayback()
        {
            AL.SourceStop(alSourceId);
            ALHelper.Check();
        }

        void Empty()
        {
            int queued;
            AL.GetSource(alSourceId, ALGetSourcei.BuffersQueued, out queued);
            if (queued > 0)
            {
                AL.SourceUnqueueBuffers(alSourceId, queued);

                if (!ALHelper.TryCheck())
                {
                    // This is no good... let's regenerate the source
                    //Console.WriteLine("Source " + alSourceId + " was corrupted and needs to be regenerated (used for " + RealName + ")");

                    AL.DeleteSource(alSourceId);
                    alSourceId = AL.GenSource();
                }
            }
        }

        void Open()
        {
            underlyingStream.Seek(0, SeekOrigin.Begin);
            OggStreamer.decodeLock.EnterWriteLock();
            Reader = new VorbisReader(underlyingStream, true);
            OggStreamer.decodeLock.ExitWriteLock();
        }

        void Precache(bool asynchronous = false)
        {
            if (!asynchronous)
            {
                // Fill first buffer synchronously
                int buffer = bufferStack.Pop();
                OggStreamer.Instance.FillBuffer(this, buffer);
                AL.SourceQueueBuffer(alSourceId, buffer);
                ALHelper.Check();

                //Trace.WriteLine("[fp] Q (1) : " + RealName + " - s = " + alSourceId);

                FirstBufferPrecached = true;
            }
            else
                Interlocked.Increment(ref OggStreamer.Instance.PendingPrecaches);

            // Schedule the others asynchronously
            OggStreamer.Instance.AddStream(this);
        }

        void Close()
        {
            if (Reader != null)
            {
                Reader.Dispose();
                Reader = null;

                underlyingStream.Close();
                underlyingStream.Dispose();
                underlyingStream = null;
            }
        }
    }

    public class OggStreamer : IDisposable
    {
        const float DefaultUpdateRate = 60;
        const int DefaultBufferSize = 22050;

        static readonly ReaderWriterLockSlim singletonLock = new ReaderWriterLockSlim();
        static readonly ReaderWriterLockSlim iterationLock = new ReaderWriterLockSlim();

        public static readonly ReaderWriterLockSlim decodeLock = new ReaderWriterLockSlim();

        readonly float[] readSampleBuffer;
        readonly short[] castBuffer;

        readonly HashSet<OggStream> streams = new HashSet<OggStream>();
        readonly List<OggStream> threadLocalStreams = new List<OggStream>();

        readonly Thread underlyingThread;
        volatile bool cancelled;

        public int PendingPrecaches;

        public float UpdateRate { get; private set; }
        public int BufferSize { get; private set; }

        static OggStreamer instance;
        public static OggStreamer Instance
        {
            get
            {
                singletonLock.EnterReadLock();
                if (instance == null)
                    throw new InvalidOperationException("No instance running");
                OggStreamer retValue = instance;
                singletonLock.ExitReadLock();
                return retValue;
            }
            private set { instance = value; }
        }
        public static bool HasInstance
        {
            get
            {
                singletonLock.EnterReadLock();
                bool retValue = instance != null;
                singletonLock.ExitReadLock();
                return retValue;
            }
        }

        public OggStreamer(int bufferSize = DefaultBufferSize, float updateRate = DefaultUpdateRate)
        {
            try
            {
                singletonLock.EnterUpgradeableReadLock();
                if (instance != null)
                    throw new InvalidOperationException("Already running");

                singletonLock.EnterWriteLock();
                Instance = this;
                singletonLock.ExitWriteLock();
            }
            finally
            {
                singletonLock.ExitUpgradeableReadLock();
            }

            underlyingThread = new Thread(EnsureBuffersFilled) { Priority = ThreadPriority.Normal, Name = "Ogg Streamer" };
            underlyingThread.Start();

            UpdateRate = updateRate;
            BufferSize = bufferSize;

            readSampleBuffer = new float[bufferSize];
            castBuffer = new short[bufferSize];
        }

        public void Dispose()
        {
            singletonLock.EnterWriteLock();
            Debug.Assert(Instance == this, "Two instances running, or double disposal!");

            cancelled = true;
            
            iterationLock.EnterWriteLock();
            streams.Clear();
            iterationLock.ExitWriteLock();

            Instance = null;
            singletonLock.ExitWriteLock();
        }

        public float LowPassHFGain
        {
            set
            {
#if !FAKE
                OpenALSoundController.Instance.LowPassHFGain = value;
#endif
            }
        }

        float musicVolume = 1;
        public float MusicVolume
        {
            get { return musicVolume; }
            set
            {
                musicVolume = value;
                iterationLock.EnterReadLock();
                foreach (var s in streams) 
                    if (s.Category == "Music") s.GlobalVolume = value;
                iterationLock.ExitReadLock();
            }
        }
        float ambienceVolume = 1;
        public float AmbienceVolume
        {
            get { return ambienceVolume; }
            set
            {
                ambienceVolume = value;
                iterationLock.EnterReadLock();
                foreach (var s in streams) 
                    if (s.Category == "Ambience") s.GlobalVolume = value;
                iterationLock.ExitReadLock();
            }
        }

        internal bool AddStream(OggStream stream)
        {
            //stream.GlobalVolume = stream.Category == "Music" ? musicVolume : ambienceVolume;

            iterationLock.EnterWriteLock();
            bool added = streams.Add(stream);
            iterationLock.ExitWriteLock();

            //if (added)
            //    Trace.WriteLine("[OpenAL] Stream " + stream.RealName + " added with source " + stream.alSourceId);

            return added;
        }
        internal bool RemoveStream(OggStream stream)
        {
            iterationLock.EnterWriteLock();
            bool removed = streams.Remove(stream);
            iterationLock.ExitWriteLock();

            //if (removed)
            //    Trace.WriteLine("[OpenAL] Stream " + stream.RealName + " removed with source " + stream.alSourceId);

            if (removed && !stream.FirstBufferPrecached)
                Interlocked.Decrement(ref PendingPrecaches);

            return removed;
        }

        public bool FillBuffer(OggStream stream, int bufferId)
        {
            int readSamples;
            decodeLock.EnterWriteLock();
            {
                if (stream.IsDisposed)
                {
                    decodeLock.ExitWriteLock();
                    return true;
                }

                readSamples = stream.Reader.ReadSamples(readSampleBuffer, 0, BufferSize);

                for (int i = 0; i < readSamples; i++)
                    castBuffer[i] = (short)(short.MaxValue * readSampleBuffer[i]);

                AL.BufferData(bufferId, stream.Reader.Channels == 1 ? ALFormat.Mono16 : ALFormat.Stereo16, castBuffer,
                              readSamples * sizeof(short), stream.Reader.SampleRate);
                ALHelper.Check();
            }
            decodeLock.ExitWriteLock();

            return readSamples != BufferSize;
        }

        void EnsureBuffersFilled()
        {
            while (!cancelled)
            {
                //Thread.Sleep((int)(1000 / UpdateRate));
                if (cancelled) break;

                threadLocalStreams.Clear();
                iterationLock.EnterReadLock();
                threadLocalStreams.AddRange(streams);
                iterationLock.ExitReadLock();

                if (threadLocalStreams.Count == 0) continue;

                // look through buffers to know what the heck we should be doing
                int lowestPlayingBufferCount = int.MaxValue;
                for (int i = threadLocalStreams.Count - 1; i >= 0; i--)
                {
                    var stream = threadLocalStreams[i];

                    iterationLock.EnterReadLock();
                    bool notFound = !streams.Contains(stream);
                    iterationLock.ExitReadLock();
                    if (notFound)
                    {
                        threadLocalStreams.RemoveAt(i);
                        continue;
                    }

                    int queued;
                    AL.GetSource(stream.alSourceId, ALGetSourcei.BuffersQueued, out queued);
                    ALHelper.Check();
                    stream.QueuedBuffers = queued;

                    int processed;
                    AL.GetSource(stream.alSourceId, ALGetSourcei.BuffersProcessed, out processed);
                    ALHelper.Check();
                    stream.ProcessedBuffers = processed;

                    if (!stream.Precaching)
                        lowestPlayingBufferCount = Math.Min(lowestPlayingBufferCount, queued - processed);
                }

                //if (lowestPlayingBufferCount < 5)
                //    Console.WriteLine("lpbc : " + lowestPlayingBufferCount);

                foreach (var stream in threadLocalStreams)
                {
                    stream.PreparationLock.EnterReadLock();
                    {
                        iterationLock.EnterReadLock();
                        bool notFound = !streams.Contains(stream);
                        iterationLock.ExitReadLock();
                        if (notFound)
                        {
                            stream.PreparationLock.ExitReadLock();
                            continue;
                        }

                        if (stream.ProcessedBuffers == 0 && stream.bufferStack.Count == 0)
                        {
#if DEBUG
                            if (stream.QueuedBuffers != stream.BufferCount)
                                ALHelper.Log("Buffers were lost for " + stream.RealName + " with source " + stream.alSourceId);
#endif
                            stream.PreparationLock.ExitReadLock();
                            continue;
                        }
#if DEBUG
                        if (stream.QueuedBuffers + stream.bufferStack.Count != stream.BufferCount)
                            ALHelper.Log("Stray buffers in source for " + stream.RealName + " with source " + stream.alSourceId);
#endif
                        if (stream.QueuedBuffers - stream.ProcessedBuffers > lowestPlayingBufferCount)
                        {
                            stream.PreparationLock.ExitReadLock();
                            continue;
                        }
                        if (stream.Precaching && lowestPlayingBufferCount <= stream.BufferCount * 2 / 3)
                        {
                            stream.PreparationLock.ExitReadLock();
                            continue;
                        }

                        int buffer;
                        if (stream.ProcessedBuffers > 0)
                            buffer = AL.SourceUnqueueBuffer(stream.alSourceId);
                        else
                            buffer = stream.bufferStack.Pop();

                        bool finished = FillBuffer(stream, buffer);
                        if (finished)
                        {
                            if (stream.IsLooped)
                                stream.Reader.DecodedTime = TimeSpan.Zero;
                            else
                            {
                                iterationLock.EnterWriteLock();
                                streams.Remove(stream);
                                iterationLock.ExitWriteLock();
                            }
                        }

                        AL.SourceQueueBuffer(stream.alSourceId, buffer);
                        ALHelper.Check();
                        //Trace.WriteLine((stream.Precaching ? "[p] " : "") + "Q (" + (stream.QueuedBuffers - stream.ProcessedBuffers + 1) + ") : " + stream.RealName + " - s = " + stream.alSourceId + " | lpbc = " + lowestPlayingBufferCount);

                        if (!stream.FirstBufferPrecached)
                        {
                            Interlocked.Decrement(ref PendingPrecaches);
                            stream.FirstBufferPrecached = true;
                            //Trace.WriteLine("[OpenAL] Buffer " + stream.RealName + " precached with source " + stream.alSourceId);
                        }

                        if (finished && !stream.IsLooped)
                        {
                            stream.PreparationLock.ExitReadLock();
                            continue;
                        }
                    }
                    stream.PreparationLock.ExitReadLock();

                    stream.StoppingLock.EnterReadLock();
                    {
                        if (stream.Precaching)
                        {
                            stream.StoppingLock.ExitReadLock();
                            continue;
                        }

                        iterationLock.EnterReadLock();
                        bool notFound = !streams.Contains(stream);
                        iterationLock.ExitReadLock();
                        if (notFound)
                        {
                            stream.StoppingLock.ExitReadLock();
                            continue;
                        }

                        var state = AL.GetSourceState(stream.alSourceId);
                        ALHelper.Check();
                        if (state == ALSourceState.Stopped)
                        {
                            ALHelper.Log("Buffer underrun on " + stream.RealName + " with source " + stream.alSourceId);
                            AL.SourcePlay(stream.alSourceId);
                            ALHelper.Check();
                        }
                    }
                    stream.StoppingLock.ExitReadLock();
                }
            }
        }
    }
}
