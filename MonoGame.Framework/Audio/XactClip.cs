using System;
using System.IO;

namespace Microsoft.Xna.Framework.Audio
{
	internal class XactClip
	{
		float volume;
		
		abstract class ClipEvent {
			public XactClip clip;
			
			public abstract void Play();
            public abstract void PlayPositional(AudioListener listener, AudioEmitter emitter);
            public abstract void UpdatePosition(AudioListener listener, AudioEmitter emitter);
			public abstract void Stop();
			public abstract void Pause();
			public abstract bool Playing { get; }
			public abstract float Volume { get; set; }
		}
		
		class EventPlayWave : ClipEvent {
			public SoundEffectInstance wave;
			public override void Play() {
				wave.Volume = clip.Volume;
				if (wave.State == SoundState.Playing) wave.Stop ();
				wave.Play ();
			}
            public override void PlayPositional(AudioListener listener, AudioEmitter emitter) {
                wave.Volume =  clip.Volume;
                if (wave.State == SoundState.Playing) wave.Stop();
                wave.Apply3D(listener, emitter);
                wave.Play();
            }
            public override void UpdatePosition(AudioListener listener, AudioEmitter emitter) {
                wave.Apply3D(listener, emitter);
            }
			public override void Stop() {
				wave.Stop ();
			}
			public override void Pause() {
				wave.Pause ();
			}
			public override bool Playing {
				get {
					return wave.State == SoundState.Playing;
				}
			}
			public override float Volume {
				get {
					return wave.Volume;
				}
				set {
					wave.Volume = value;
				}
			}
		}
		
		ClipEvent[] events;
		
		public XactClip (SoundBank soundBank, BinaryReader clipReader, uint clipOffset)
		{
			long oldPosition = clipReader.BaseStream.Position;
			clipReader.BaseStream.Seek (clipOffset, SeekOrigin.Begin);
			
			byte numEvents = clipReader.ReadByte();
			events = new ClipEvent[numEvents];
			
			for (int i=0; i<numEvents; i++) {
				uint eventInfo = clipReader.ReadUInt32();
				
				uint eventId = eventInfo & 0x1F;
				switch (eventId) {
				case 1:
					EventPlayWave evnt = new EventPlayWave();
					
					
					clipReader.ReadUInt32 (); //unkn
					uint trackIndex = clipReader.ReadUInt16 ();
					byte waveBankIndex = clipReader.ReadByte ();
					
					//unkn
					clipReader.ReadByte ();
					clipReader.ReadUInt16 ();
					clipReader.ReadUInt16 ();
					
					evnt.wave = soundBank.GetWave(waveBankIndex, trackIndex);
					
					events[i] = evnt;
                    
                    // FIXME: TERRIBLE HACK!!! Oh hey look flibit is making assumptions about clips!
                    evnt.wave.IsLooped = true;
                    
					break;
				default:
					throw new NotImplementedException();
				}
				
				events[i].clip = this;
			}
			
			
			clipReader.BaseStream.Seek (oldPosition, SeekOrigin.Begin);
		}
		
		public void Play() {
			//TODO: run events
			events[0].Play ();
		}
		
		public void Stop() {
			events[0].Stop ();
		}
		
		public void Pause() {
			events[0].Pause();
		}
		
		public bool Playing {
			get {
				return events[0].Playing;
			}
		}
		
		public float Volume {
			get {
				return volume;
			}
			set {
				volume = value;
				events[0].Volume = value;
			}
		}
        
        // Needed for positional audio
        internal void PlayPositional(AudioListener listener, AudioEmitter emitter) {
            // TODO: run events
            events[0].PlayPositional(listener, emitter);
        }
        
        internal void UpdatePosition(AudioListener listener, AudioEmitter emitter) {
            // TODO: run events
            events[0].UpdatePosition(listener, emitter);
        }
		
	}
}

