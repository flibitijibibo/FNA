#region License
// /*
// Microsoft Public License (Ms-PL)
// MonoGame - Copyright © 2009 The MonoGame Team
// 
// All rights reserved.
// 
// This license governs use of the accompanying software. If you use the software, you accept this license. If you do not
// accept the license, do not use the software.
// 
// 1. Definitions
// The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under
// U.S. copyright law.
// 
// A "contribution" is the original software, or any additions or changes to the software.
// A "contributor" is any person that distributes its contribution under this license.
// "Licensed patents" are a contributor's patent claims that read directly on its contribution.
// 
// 2. Grant of Rights
// (A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
// each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
// (B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
// each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.
// 
// 3. Conditions and Limitations
// (A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
// (B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, 
// your patent license from such contributor to the software ends automatically.
// (C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution 
// notices that are present in the software.
// (D) If you distribute any portion of the software in source code form, you may do so only under this license by including 
// a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object 
// code form, you may only do so under a license that complies with this license.
// (E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees
// or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent
// permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular
// purpose and non-infringement.
// */
#endregion License

#region Using Statements
using System;

#if MONOMAC
using MonoMac.OpenAL;
#else
using OpenTK.Audio.OpenAL;
#endif

#endregion Statements

namespace Microsoft.Xna.Framework.Audio
{
    public class SoundEffectInstance : IDisposable
    {
        readonly SoundEffect soundEffect;
        readonly int sourceId = -1;

        float volume = 1.0f;
        bool looped;
        float pan;
        float pitch;

        bool isDisposed;
        SoundState soundState = SoundState.Stopped;
        bool lowPass;

        protected SoundEffectInstance() { }
        internal SoundEffectInstance(SoundEffect soundEffect, bool forceNoFilter = false) : this()
        {
            this.soundEffect = soundEffect;
            if (OpenALSoundController.Instance != null && soundEffect.Size > 0)
                sourceId = OpenALSoundController.Instance.RegisterSfxInstance(this, forceNoFilter);
        }

        public SoundEffect SoundEffect
        {
            get { return soundEffect; }
        }

        public void Dispose()
        {
            if (isDisposed) return;

            if (soundState != SoundState.Stopped)
                Stop();

            isDisposed = true;
            if (OpenALSoundController.Instance != null && sourceId != -1)
                OpenALSoundController.Instance.ReturnSourceFor(soundEffect, sourceId);
        }

        ~SoundEffectInstance()
        {
            Dispose();
        }

        public void Pause()
        {
            if (isDisposed) throw new ObjectDisposedException("SoundEffectInstance (" + soundEffect.Name + ")");
            if (sourceId == -1 || soundState != SoundState.Playing)
                return;

            AL.SourcePause(sourceId);
            ALHelper.Check();
            soundState = SoundState.Paused;
        }

        public void Play()
        {
            if (isDisposed) throw new ObjectDisposedException("SoundEffectInstance (" + soundEffect.Name + ")");
            if (sourceId == -1 || soundState == SoundState.Playing)
                return;

            AL.SourcePlay(sourceId);
            ALHelper.Check();
            soundState = SoundState.Playing;
        }

        public void Resume()
        {
            if (isDisposed) throw new ObjectDisposedException("SoundEffectInstance (" + soundEffect.Name + ")");
            if (sourceId == -1 || soundState == SoundState.Paused)
                Play();
        }

        public void Stop(bool immediate = false)
        {
            if (isDisposed)
                return;
            if (sourceId == -1 || soundState == SoundState.Stopped)
                return;

            AL.SourceStop(sourceId);
            ALHelper.Check();
            soundState = SoundState.Stopped;
        }

        internal bool RefreshState()
        {
            if (soundState == SoundState.Playing && AL.GetSourceState(sourceId) == ALSourceState.Stopped)
            {
                ALHelper.Check();
                soundState = SoundState.Stopped;
                return true;
            }
            return false;
        }

        public bool IsDisposed
        {
            get { return isDisposed; }
        }

        public bool IsLooped
        {
            get { return looped; }
            set
            {
                if (isDisposed)
                    return;

                if (sourceId == -1) return;
                looped = value;
                AL.Source(sourceId, ALSourceb.Looping, looped);
                ALHelper.Check();
            }
        }

        public float Pan
        {
            get { return pan; }
            set
            {
                if (isDisposed)
                    return;

                if (sourceId == -1) return;
                pan = value;
                AL.Source(sourceId, ALSource3f.Position, pan / 5f, 0.0f, 0.1f);
                ALHelper.Check();
            }
        }

        public float Pitch
        {
            get { return pitch; }
            set
            {
                if (isDisposed)
                    return;

                if (sourceId == -1) return;
                pitch = value;
                AL.Source(sourceId, ALSourcef.Pitch, XnaPitchToAlPitch(pitch));
                ALHelper.Check();
            }
        }

        public bool LowPass
        {
            get { return lowPass; }
            set
            {
                if (sourceId == -1) return;
                if (lowPass != value)
                    OpenALSoundController.Instance.SetSourceFiltered(sourceId, value);
                lowPass = value;
            }
        }

        private float XnaPitchToAlPitch(float pitch)
        {
            // pitch is different in XNA and OpenAL. XNA has a pitch between -1 and 1 for one octave down/up.
            // openAL uses 0.5 to 2 for one octave down/up, while 1 is the default. The default value of 0 would make it completely silent.
            return (float)Math.Exp(0.69314718 * pitch);
        }

        public SoundState State
        {
            get { return soundState; }
        }

        public float Volume
        {
            get { return volume; }
            set
            {
                if (isDisposed)
                    return;

                if (sourceId == -1) return;
                volume = value;
                AL.Source(sourceId, ALSourcef.Gain, volume * SoundEffect.MasterVolume);
                ALHelper.Check();
            }
        }
    }
}
