#region License
/*
Microsoft Public License (Ms-PL)
MonoGame - Copyright © 2009 The MonoGame Team

All rights reserved.

This license governs use of the accompanying software. If you use the software, you accept this license. If you do not
accept the license, do not use the software.

1. Definitions
The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under 
U.S. copyright law.

A "contribution" is the original software, or any additions or changes to the software.
A "contributor" is any person that distributes its contribution under this license.
"Licensed patents" are a contributor's patent claims that read directly on its contribution.

2. Grant of Rights
(A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
(B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.

3. Conditions and Limitations
(A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
(B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, 
your patent license from such contributor to the software ends automatically.
(C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution 
notices that are present in the software.
(D) If you distribute any portion of the software in source code form, you may do so only under this license by including 
a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object 
code form, you may only do so under a license that complies with this license.
(E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees
or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent
permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular
purpose and non-infringement.
*/
#endregion License

using System;


namespace Microsoft.Xna.Framework.Audio
{
	public class Cue : IDisposable
	{
		AudioEngine engine;
		string name;
		XactSound[] sounds;
		float[] probs;
		XactSound curSound;
		Random variationRand;
        
        // Positional sound variables
        // Only enabled when Apply3D is called before a Play().
        
        // FIXME: Once you go positional, you can't go back. *Cough*
        private bool positionalAudio;
        private AudioListener listener;
        private AudioEmitter emitter;
		
		bool paused = false;
		float volume = 1.0f;
        float categoryVolume = 1.0f;
        
        // FIXME: This should really be an array for each RPC table and type.
        float rpcVolume = 1.0f;
		
		public bool IsPaused
		{
			get { return paused; }
		}
		
		public bool IsPlaying
		{
			get {
				if (curSound != null) {
					return curSound.Playing;
				}
				return false;
			}
		}
		
		public bool IsStopped
		{
			get {
				if (curSound != null) {
					return !curSound.Playing;
				}
				return true;
			}
		}
		
		public string Name
		{
			get { return name; }
		}
		
		internal Cue (AudioEngine engine, string cuename, XactSound sound)
		{
			this.engine = engine;
			name = cuename;
			sounds = new XactSound[1];
			sounds[0] = sound;
			
			probs = new float[1];
			probs[0] = 1.0f;
			
			variationRand = new Random();
		}
		
		internal Cue(AudioEngine engine, string cuename, XactSound[] _sounds, float[] _probs)
		{
            this.engine = engine;
			name = cuename;
			sounds = _sounds;
			probs = _probs;
			
			variationRand = new Random();
		}
		
		public void Pause()
		{
			if (curSound != null) {
				curSound.Pause();
			}
			paused = true;
		}
		
		public void Play()
		{
			//TODO: Probabilities
			curSound = sounds[variationRand.Next (sounds.Length)];
			
            // FIXME: Why was this here?
			// curSound.Volume = volume * categoryVolume;
            if (positionalAudio)
            {
                curSound.PlayPositional(listener, emitter);
            }
            else
            {
                curSound.Play();
            }
            
			paused = false;
		}
		
		public void Resume()
		{
			if (curSound != null) {
				curSound.Resume ();
			}
			paused = false;
		}
		
		public void Stop(AudioStopOptions options)
		{
			if (curSound != null) {
				curSound.Stop();
			}
			paused = false;
		}
		
		public void SetVariable (string name, float value)
		{
            // FIXME: This is pretty ugly.
			if (name == "Volume") {
				volume = value;
				if (curSound != null) {
					curSound.Volume = value * categoryVolume * rpcVolume;
				}
            } else if (name == "CategoryVolume") {
                categoryVolume = value;
                if (curSound != null) {
                    curSound.Volume = volume * value * rpcVolume;
                }
            } else if (curSound != null && curSound.rpcVariables.ContainsKey(name)) {
                curSound.rpcVariables[name] = value;
			} else {
				engine.SetGlobalVariable (name, value);
			}
		}
		
		public float GetVariable (string name)
		{
			if (name == "Volume") {
				return volume;
            } else if (curSound != null && curSound.rpcVariables.ContainsKey(name)) {
                return curSound.rpcVariables[name];
			} else {
				return engine.GetGlobalVariable (name);
			}
		}
		
		public void Apply3D(AudioListener listener, AudioEmitter emitter) {
			this.listener = listener;
            this.emitter = emitter;
            positionalAudio = true;
		}
        
        internal void Update()
        {
            if (curSound != null && IsPlaying)
            {
                // Positional audio update
                if (positionalAudio)
                {
                    curSound.UpdatePosition(listener, emitter);
                }
                UpdateRPCVariables();
            }
        }
        
        private void UpdateRPCVariables()
        {
                // RPC effects update
                if (curSound.rpcEffects != null)
                {
                    for (int i = 0; i < curSound.rpcEffects.Length; i++)
                    {
                        // The current curve from the RPC effects
                        AudioEngine.RpcCurve curve = engine.rpcCurves[curSound.rpcEffects[i]];
                        
                        // The sound property we're modifying
                        AudioEngine.RpcParameter parameter = curve.parameter;
                        
                        // The variable that this curve is looking at
                        float varValue = curSound.rpcVariables[engine.variables[curve.variable].name];
                        
                        // Applying this when we're done...
                        float curveResult = 0.0f;
                        
                        // FIXME: ALL OF THIS IS ASSUMING LINEAR CURVES!
                        
                        if (varValue == 0.0f)
                        {
                            // If it's 0, we're just at the stock value.
                            if (curve.points[0].x == 0.0f)
                            {
                                // Some curves may start x->0 elsewhere.
                                curveResult = curve.points[0].y;
                            }
                        }
                        else if (varValue <= curve.points[0].x)
                        {
                            // Zero to first defined point
                            curveResult = curve.points[0].y / (varValue / curve.points[0].x);
                        }
                        else if (varValue >= curve.points[curve.points.Length - 1].x)
                        {
                            // Last defined point to infinity
                            curveResult = curve.points[curve.points.Length - 1].y / (curve.points[curve.points.Length - 1].x / varValue);
                        }
                        else
                        {
                            // Something between points...
                            for (int x = 0; x < curve.points.Length - 1; x++)
                            {
                                // y = b
                                curveResult = curve.points[x].y;
                                if (varValue >= curve.points[x].x && varValue <= curve.points[x + 1].x)
                                {
                                    // y += mx
                                    curveResult +=
                                        ((curve.points[x + 1].y - curve.points[x].y) /
                                        (curve.points[x + 1].x - curve.points[x].x)) *
                                            (varValue - curve.points[x].x);
                                    // Pre-algebra, rockin`!
                                    break;
                                }
                            }
                        }
                            
                        // Clamp it down, we can't have massive results.
                        if (curveResult > 10000.0f)
                        {
                            curveResult = 10000.0f;
                        }
                        else if (curveResult < -10000.0f)
                        {
                            curveResult = -10000.0f;
                        }
                        
                        // FIXME: All parameter types!
                        if (parameter == AudioEngine.RpcParameter.Volume)
                        {
                            // FIXME: Multiple volumes?
                            rpcVolume = 1.0f + (curveResult / 10000.0f);
                            curSound.Volume = volume * categoryVolume * rpcVolume;
                        }
                        else
                        {
                            throw new NotImplementedException("RPC Parameter Types!");
                        }
                    }
                }
        }
		
		public bool IsDisposed { get { return false; } }
		
		
		
		#region IDisposable implementation
		public void Dispose ()
		{
			//_sound.Dispose();
		}
		#endregion
	}
}

