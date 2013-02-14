using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Xna.Framework.Audio
{
	public struct AudioCategory : IEquatable<AudioCategory>
	{
		string name;
		AudioEngine engine;

		internal float volume;
		internal bool isBackgroundMusic;
		internal bool isPublic;

		internal bool instanceLimit;
		internal int maxInstances;
        
        internal List<Cue> categoryCues;

		//insatnce limiting behaviour
		internal enum MaxInstanceBehaviour {
			FailToPlay,
			Queue,
			ReplaceOldest,
			ReplaceQuietest,
			ReplaceLowestPriority,
		}
		internal MaxInstanceBehaviour instanceBehaviour;

		internal enum CrossfadeType {
			Linear,
			Logarithmic,
			EqualPower,
		}
		internal CrossfadeType fadeType;
		internal float fadeIn;
		internal float fadeOut;

		
		internal AudioCategory (AudioEngine audioengine, string name, BinaryReader reader)
		{
			this.name = name;
			engine = audioengine;
            
            categoryCues = new List<Cue>();

			maxInstances = reader.ReadByte ();
			instanceLimit = maxInstances != 0xff;

			fadeIn = (reader.ReadUInt16 () / 1000f);
			fadeOut = (reader.ReadUInt16 () / 1000f);

			byte instanceFlags = reader.ReadByte ();
			fadeType = (CrossfadeType)(instanceFlags & 0x7);
			instanceBehaviour = (MaxInstanceBehaviour)(instanceFlags >> 3);

			reader.ReadUInt16 (); //unkn

			byte vol = reader.ReadByte (); //volume in unknown format
			//lazy 4-param fitting:
			//0xff 6.0
			//0xca 2.0
			//0xbf 1.0
			//0xb4 0.0
			//0x8f -4.0
			//0x5a -12.0
			//0x14 -38.0
			//0x00 -96.0
			var a = -96.0;
			var b = 0.432254984608615;
			var c = 80.1748600297963;
			var d = 67.7385212334047;
			volume = (float)(((a-d)/(1+(Math.Pow(vol/c, b)))) + d);

			byte visibilityFlags = reader.ReadByte ();
			isBackgroundMusic = (visibilityFlags & 0x1) != 0;
			isPublic = (visibilityFlags & 0x2) != 0;
		}

		public string Name { get { return name; } }

		public void Pause ()
		{
			foreach(Cue curCue in categoryCues)
            {
                curCue.Pause();
            }
		}

		public void Resume ()
		{
            foreach(Cue curCue in categoryCues)
            {
                curCue.Resume();
            }
		}

		public void Stop ()
		{
            foreach(Cue curCue in categoryCues)
            {
                curCue.Stop(AudioStopOptions.AsAuthored);
            }
		}

		public void Stop (AudioStopOptions option)
		{
            foreach(Cue curCue in categoryCues)
            {
                curCue.Stop(option);
            }
		}
		public void SetVolume(float volume)
        {
            foreach(Cue curCue in categoryCues)
            {
                curCue.SetVariable("CategoryVolume", volume);
            }
		}

		
		public bool Equals(AudioCategory other)
		{
            // FIXME: Not actually thorough!
            return name == other.name;
		}
		
	}
}

