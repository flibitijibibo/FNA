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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Tao.Sdl;
using System.Xml.Serialization;

namespace Microsoft.Xna.Framework.Input
{
    // FIXME: Uhhh, these structs really shouldn't be public.
    
    [Serializable]
    public struct MonoGameJoystickValue
    {
        public InputType INPUT_TYPE;
        public int INPUT_ID;
        public bool INPUT_INVERT;
    }
    
    [Serializable]
    public struct MonoGameJoystickConfig
    {
        // public MonoGameJoystickValue BUTTON_GUIDE;
        public MonoGameJoystickValue BUTTON_START;
        public MonoGameJoystickValue BUTTON_BACK;
        public MonoGameJoystickValue BUTTON_A;
        public MonoGameJoystickValue BUTTON_B;
        public MonoGameJoystickValue BUTTON_X;
        public MonoGameJoystickValue BUTTON_Y;
        public MonoGameJoystickValue SHOULDER_LB;
        public MonoGameJoystickValue SHOULDER_RB;
        public MonoGameJoystickValue TRIGGER_RT;
        public MonoGameJoystickValue TRIGGER_LT;
        public MonoGameJoystickValue BUTTON_LSTICK;
        public MonoGameJoystickValue BUTTON_RSTICK;
        public MonoGameJoystickValue DPAD_UP;
        public MonoGameJoystickValue DPAD_DOWN;
        public MonoGameJoystickValue DPAD_LEFT;
        public MonoGameJoystickValue DPAD_RIGHT;
        public MonoGameJoystickValue AXIS_LX;
        public MonoGameJoystickValue AXIS_LY;
        public MonoGameJoystickValue AXIS_RX;
        public MonoGameJoystickValue AXIS_RY;
    }
    
    //
    // Summary:
    //     Allows retrieval of user interaction with an Xbox 360 Controller and setting
    //     of controller vibration motors. Reference page contains links to related
    //     code samples.
    public static class GamePad
    {
		static bool running;		
        static bool sdl;

        static Settings settings;
        static Settings Settings
        {
        	get
            {
                return PrepSettings();
            }
        }
        
        // Where we will load our config file into.
        static MonoGameJoystickConfig joystickConfig;

		static void AutoConfig()
		{
			Init();
			if (!sdl) return;
            
            // Get the intended config file path.
            string osConfigFile = "";
#if MONOMAC
            osConfigFile += Environment.GetEnvironmentVariable("HOME");
            if (osConfigFile.Length == 0)
            {
                osConfigFile += "MonoGameJoystick.cfg"; // Oh well.
            }
            else
            {
                osConfigFile += "/Library/Application Support/MonoGame/MonoGameJoystick.cfg";
            }
#elif LINUX
            osConfigFile += Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            if (osConfigFile.Length == 0)
            {
                osConfigFile += Environment.GetEnvironmentVariable("HOME");
                if (osConfigFile.Length == 0)
                {
                    System.Console.WriteLine("Couldn't find XDG_CONFIG_HOME or HOME.");
                    System.Console.WriteLine("Fall back to '.'");
                    osConfigFile += "MonoGameJoystick.cfg"; // Oh well.
                }
                else
                {
                    System.Console.WriteLine("Couldn't find XDG_CONFIG_HOME.");
                    System.Console.WriteLine("Fall back to hardcoded ~/.config/MonoGame/.");
                    osConfigFile += "/.config/MonoGame/MonoGameJoystick.cfg";
                }
            }
            else
            {
                osConfigFile += "/MonoGame/MonoGameJoystick.cfg";
            }
#else
#warning Apologies, but I need you to implement a joystick config directory for your platform!
            osConfigFile = "MonoGameJoystick.cfg"; // Oh well.
#endif
            
            // Check to see if we've already got a config...
            if (File.Exists(osConfigFile))
            {
                // Load the file.
                FileStream fileIn = File.OpenRead(osConfigFile);
                
                // Load the data into our config struct.
                XmlSerializer serializer = new XmlSerializer(typeof(MonoGameJoystickConfig));
                joystickConfig = (MonoGameJoystickConfig) serializer.Deserialize(fileIn);
                
                // We out.
                fileIn.Close();
            }
            else
            {
                // First of all, just set our config to default values.
                
                // NOTE: These are based on flibit's Wii Classic Controller Pro.
                
                // Start
                joystickConfig.BUTTON_START.INPUT_TYPE = InputType.Button;
                joystickConfig.BUTTON_START.INPUT_ID = 9;
                joystickConfig.BUTTON_START.INPUT_INVERT = false;
                
                // Back
                joystickConfig.BUTTON_BACK.INPUT_TYPE = InputType.Button;
                joystickConfig.BUTTON_BACK.INPUT_ID = 8;
                joystickConfig.BUTTON_BACK.INPUT_INVERT = false;
                
                // A
                joystickConfig.BUTTON_A.INPUT_TYPE = InputType.Button;
                joystickConfig.BUTTON_A.INPUT_ID = 1;
                joystickConfig.BUTTON_A.INPUT_INVERT = false;
                
                // B
                joystickConfig.BUTTON_B.INPUT_TYPE = InputType.Button;
                joystickConfig.BUTTON_B.INPUT_ID = 0;
                joystickConfig.BUTTON_B.INPUT_INVERT = false;
                
                // X
                joystickConfig.BUTTON_X.INPUT_TYPE = InputType.Button;
                joystickConfig.BUTTON_X.INPUT_ID = 3;
                joystickConfig.BUTTON_X.INPUT_INVERT = false;
                
                // Y
                joystickConfig.BUTTON_Y.INPUT_TYPE = InputType.Button;
                joystickConfig.BUTTON_Y.INPUT_ID = 2;
                joystickConfig.BUTTON_Y.INPUT_INVERT = false;
                
                // LB
                joystickConfig.SHOULDER_LB.INPUT_TYPE = InputType.Button;
                joystickConfig.SHOULDER_LB.INPUT_ID = 4;
                joystickConfig.SHOULDER_LB.INPUT_INVERT = false;
                
                // RB
                joystickConfig.SHOULDER_RB.INPUT_TYPE = InputType.Button;
                joystickConfig.SHOULDER_RB.INPUT_ID = 5;
                joystickConfig.SHOULDER_RB.INPUT_INVERT = false;
                
                // LT
                joystickConfig.TRIGGER_LT.INPUT_TYPE = InputType.Button;
                joystickConfig.TRIGGER_LT.INPUT_ID = 6;
                joystickConfig.TRIGGER_LT.INPUT_INVERT = false;
                
                // RT
                joystickConfig.TRIGGER_RT.INPUT_TYPE = InputType.Button;
                joystickConfig.TRIGGER_RT.INPUT_ID = 7;
                joystickConfig.TRIGGER_RT.INPUT_INVERT = false;
                
                // LStick
                joystickConfig.BUTTON_LSTICK.INPUT_TYPE = InputType.Button;
                joystickConfig.BUTTON_LSTICK.INPUT_ID = -1;
                joystickConfig.BUTTON_LSTICK.INPUT_INVERT = false;
                
                // RStick
                joystickConfig.BUTTON_RSTICK.INPUT_TYPE = InputType.Button;
                joystickConfig.BUTTON_RSTICK.INPUT_ID = -1;
                joystickConfig.BUTTON_RSTICK.INPUT_INVERT = false;
                
                // DPad Up
                joystickConfig.DPAD_UP.INPUT_TYPE = InputType.PovUp;
                joystickConfig.DPAD_UP.INPUT_ID = 0;
                joystickConfig.DPAD_UP.INPUT_INVERT = false;
                
                // DPad Down
                joystickConfig.DPAD_DOWN.INPUT_TYPE = InputType.PovDown;
                joystickConfig.DPAD_DOWN.INPUT_ID = 0;
                joystickConfig.DPAD_DOWN.INPUT_INVERT = false;
                
                // DPad Left
                joystickConfig.DPAD_LEFT.INPUT_TYPE = InputType.PovLeft;
                joystickConfig.DPAD_LEFT.INPUT_ID = 0;
                joystickConfig.DPAD_LEFT.INPUT_INVERT = false;
                
                // DPad Right
                joystickConfig.DPAD_RIGHT.INPUT_TYPE = InputType.PovRight;
                joystickConfig.DPAD_RIGHT.INPUT_ID = 0;
                joystickConfig.DPAD_RIGHT.INPUT_INVERT = false;
                
                // LX
                joystickConfig.AXIS_LX.INPUT_TYPE = InputType.Axis;
                joystickConfig.AXIS_LX.INPUT_ID = 0;
                joystickConfig.AXIS_LX.INPUT_INVERT = false;
                
                // LY
                joystickConfig.AXIS_LY.INPUT_TYPE = InputType.Axis;
                joystickConfig.AXIS_LY.INPUT_ID = 1;
                joystickConfig.AXIS_LY.INPUT_INVERT = true;
                
                // RX
                joystickConfig.AXIS_RX.INPUT_TYPE = InputType.Axis;
                joystickConfig.AXIS_RX.INPUT_ID = 2;
                joystickConfig.AXIS_RX.INPUT_INVERT = false;
                
                // RY
                joystickConfig.AXIS_RY.INPUT_TYPE = InputType.Axis;
                joystickConfig.AXIS_RY.INPUT_ID = 3;
                joystickConfig.AXIS_RY.INPUT_INVERT = true;
                
                
                // Since it doesn't exist, we need to generate the default config.
                
                // ... but is our directory even there?
                string osConfigDir = osConfigFile.Substring(0, osConfigFile.IndexOf("MonoGameJoystick.cfg"));
                if (!Directory.Exists(osConfigDir))
                {
                    // Okay, jeez, we're really starting fresh.
                    Directory.CreateDirectory(osConfigDir);
                }
                
                // So, create the file.
                FileStream fileOut = File.Open(osConfigFile, FileMode.OpenOrCreate);
                XmlSerializer serializer = new XmlSerializer(typeof(MonoGameJoystickConfig));
                serializer.Serialize(fileOut, joystickConfig);
                
                // We out.
                fileOut.Close();
            }
            
#if DEBUG
			Console.WriteLine("Number of joysticks: " + Sdl.SDL_NumJoysticks());
#endif
			// Limit to the first 4 sticks to avoid crashes
			int numSticks = Math.Min(4, Sdl.SDL_NumJoysticks());
			for (int x = 0; x < numSticks; x++)
			{
				PadConfig pc = new PadConfig(Sdl.SDL_JoystickName(x), x);
				devices[x] = Sdl.SDL_JoystickOpen(pc.Index);
    
                // Start
                pc.Button_Start.ID = joystickConfig.BUTTON_START.INPUT_ID;
                pc.Button_Start.Type = joystickConfig.BUTTON_START.INPUT_TYPE;
                pc.Button_Start.Negative = joystickConfig.BUTTON_START.INPUT_INVERT;
    
                // Back
                pc.Button_Back.ID = joystickConfig.BUTTON_BACK.INPUT_ID;
                pc.Button_Back.Type = joystickConfig.BUTTON_BACK.INPUT_TYPE;
                pc.Button_Back.Negative = joystickConfig.BUTTON_BACK.INPUT_INVERT;
    
                // A
                pc.Button_A.ID = joystickConfig.BUTTON_A.INPUT_ID;
                pc.Button_A.Type = joystickConfig.BUTTON_A.INPUT_TYPE;
                pc.Button_A.Negative = joystickConfig.BUTTON_A.INPUT_INVERT;
    
                // B
                pc.Button_B.ID = joystickConfig.BUTTON_B.INPUT_ID;
                pc.Button_B.Type = joystickConfig.BUTTON_B.INPUT_TYPE;
                pc.Button_B.Negative = joystickConfig.BUTTON_B.INPUT_INVERT;
    
                // X
                pc.Button_X.ID = joystickConfig.BUTTON_X.INPUT_ID;
                pc.Button_X.Type = joystickConfig.BUTTON_X.INPUT_TYPE;
                pc.Button_X.Negative = joystickConfig.BUTTON_X.INPUT_INVERT;
    
                // Y
                pc.Button_Y.ID = joystickConfig.BUTTON_Y.INPUT_ID;
                pc.Button_Y.Type = joystickConfig.BUTTON_Y.INPUT_TYPE;
                pc.Button_Y.Negative = joystickConfig.BUTTON_Y.INPUT_INVERT;
    
                // LB
                pc.Button_LB.ID = joystickConfig.SHOULDER_LB.INPUT_ID;
                pc.Button_LB.Type = joystickConfig.SHOULDER_LB.INPUT_TYPE;
                pc.Button_LB.Negative = joystickConfig.SHOULDER_LB.INPUT_INVERT;
    
                // RB
                pc.Button_RB.ID = joystickConfig.SHOULDER_RB.INPUT_ID;
                pc.Button_RB.Type = joystickConfig.SHOULDER_RB.INPUT_TYPE;
                pc.Button_RB.Negative = joystickConfig.SHOULDER_RB.INPUT_INVERT;
                
                // LT
                pc.LeftTrigger.ID = joystickConfig.TRIGGER_LT.INPUT_ID;
                pc.LeftTrigger.Type = joystickConfig.TRIGGER_LT.INPUT_TYPE;
                pc.LeftTrigger.Negative = joystickConfig.TRIGGER_LT.INPUT_INVERT;
                
                // RT
                pc.RightTrigger.ID = joystickConfig.TRIGGER_RT.INPUT_ID;
                pc.RightTrigger.Type = joystickConfig.TRIGGER_RT.INPUT_TYPE;
                pc.RightTrigger.Negative = joystickConfig.TRIGGER_RT.INPUT_INVERT;
    
                // LStick
                pc.LeftStick.Press.ID = joystickConfig.BUTTON_LSTICK.INPUT_ID;
                pc.LeftStick.Press.Type = joystickConfig.BUTTON_LSTICK.INPUT_TYPE;
                pc.LeftStick.Press.Negative = joystickConfig.BUTTON_LSTICK.INPUT_INVERT;
    
                // RStick
                pc.RightStick.Press.ID = joystickConfig.BUTTON_RSTICK.INPUT_ID;
                pc.RightStick.Press.Type = joystickConfig.BUTTON_RSTICK.INPUT_TYPE;
                pc.RightStick.Press.Negative = joystickConfig.BUTTON_RSTICK.INPUT_INVERT;
                
                // DPad Up
                pc.Dpad.Up.ID = joystickConfig.DPAD_UP.INPUT_ID;
                pc.Dpad.Up.Type = joystickConfig.DPAD_UP.INPUT_TYPE;
                pc.Dpad.Up.Negative = joystickConfig.DPAD_UP.INPUT_INVERT;
                
                // DPad Down
                pc.Dpad.Down.ID = joystickConfig.DPAD_DOWN.INPUT_ID;
                pc.Dpad.Down.Type = joystickConfig.DPAD_DOWN.INPUT_TYPE;
                pc.Dpad.Down.Negative = joystickConfig.DPAD_DOWN.INPUT_INVERT;
                
                // DPad Left
                pc.Dpad.Left.ID = joystickConfig.DPAD_LEFT.INPUT_ID;
                pc.Dpad.Left.Type = joystickConfig.DPAD_LEFT.INPUT_TYPE;
                pc.Dpad.Left.Negative = joystickConfig.DPAD_LEFT.INPUT_INVERT;
                
                // DPad Right
                pc.Dpad.Right.ID = joystickConfig.DPAD_RIGHT.INPUT_ID;
                pc.Dpad.Right.Type = joystickConfig.DPAD_RIGHT.INPUT_TYPE;
                pc.Dpad.Right.Negative = joystickConfig.DPAD_RIGHT.INPUT_INVERT;
    
                // LX
                pc.LeftStick.X.Negative.ID = joystickConfig.AXIS_LX.INPUT_ID;
                pc.LeftStick.X.Negative.Type = joystickConfig.AXIS_LX.INPUT_TYPE;
                pc.LeftStick.X.Negative.Negative = !joystickConfig.AXIS_LX.INPUT_INVERT;
                pc.LeftStick.X.Positive.ID = joystickConfig.AXIS_LX.INPUT_ID;
                pc.LeftStick.X.Positive.Type = joystickConfig.AXIS_LX.INPUT_TYPE;
                pc.LeftStick.X.Positive.Negative = joystickConfig.AXIS_LX.INPUT_INVERT;
    
                // LY
                pc.LeftStick.Y.Negative.ID = joystickConfig.AXIS_LY.INPUT_ID;
                pc.LeftStick.Y.Negative.Type = joystickConfig.AXIS_LY.INPUT_TYPE;
                pc.LeftStick.Y.Negative.Negative = !joystickConfig.AXIS_LY.INPUT_INVERT;
                pc.LeftStick.Y.Positive.ID = joystickConfig.AXIS_LY.INPUT_ID;
                pc.LeftStick.Y.Positive.Type = joystickConfig.AXIS_LY.INPUT_TYPE;
                pc.LeftStick.Y.Positive.Negative = joystickConfig.AXIS_LY.INPUT_INVERT;
    
                // RX
                pc.RightStick.X.Negative.ID = joystickConfig.AXIS_RX.INPUT_ID;
                pc.RightStick.X.Negative.Type = joystickConfig.AXIS_RX.INPUT_TYPE;
                pc.RightStick.X.Negative.Negative = !joystickConfig.AXIS_RX.INPUT_INVERT;
                pc.RightStick.X.Positive.ID = joystickConfig.AXIS_RX.INPUT_ID;
                pc.RightStick.X.Positive.Type = joystickConfig.AXIS_RX.INPUT_TYPE;
                pc.RightStick.X.Positive.Negative = joystickConfig.AXIS_RX.INPUT_INVERT;
    
                // RY
                pc.RightStick.Y.Negative.ID = joystickConfig.AXIS_RY.INPUT_ID;
                pc.RightStick.Y.Negative.Type = joystickConfig.AXIS_RY.INPUT_TYPE;
                pc.RightStick.Y.Negative.Negative = !joystickConfig.AXIS_RY.INPUT_INVERT;
                pc.RightStick.Y.Positive.ID = joystickConfig.AXIS_RY.INPUT_ID;
                pc.RightStick.Y.Positive.Type = joystickConfig.AXIS_RY.INPUT_TYPE;
                pc.RightStick.Y.Positive.Negative = joystickConfig.AXIS_RY.INPUT_INVERT;

				// Suggestion: Xbox Guide button <=> BigButton
				//pc.BigButton.ID = 8;
				//pc.BigButton.Type = InputType.Button;

#if DEBUG
				int numbuttons = Sdl.SDL_JoystickNumButtons(devices[x]);
				Console.WriteLine("Number of buttons for joystick: " + x + " - " + numbuttons);

				int numaxes = Sdl.SDL_JoystickNumAxes(devices[x]);
				Console.WriteLine("Number of axes for joystick: " + x + " - " + numaxes);

				int numhats = Sdl.SDL_JoystickNumHats(devices[x]);
				Console.WriteLine("Number of PovHats for joystick: " + x + " - " + numhats);
#endif

				settings[x] = pc;
			}
		}

        static Settings PrepSettings()
        {
            if (settings == null)
            {
                    settings = new Settings();
					AutoConfig();		
            }
            else if (!running)
            {
                Init();
                return settings;
            }
            if (!running)
                Init();
            return settings;
        }
        

        static IntPtr[] devices = new IntPtr[4];
        //Inits SDL and grabs the sticks
        static void Init ()
        {
        	running = true;
		    try 
            {
         	    Joystick.Init ();
				sdl = true;
			}
			catch (Exception) 
            {

			}
        	for (int i = 0; i < 4; i++)
            {
        		PadConfig pc = settings[i];
        		if (pc != null)
                {
        			devices[i] = Sdl.SDL_JoystickOpen (pc.Index);
			    }
		    }


        }
        //Disposes of SDL
        static void Cleanup()
        {
            Joystick.Cleanup();
            running = false;
        }

        static IntPtr GetDevice(PlayerIndex index)
        {
            return devices[(int)index];
        }

        static PadConfig GetConfig(PlayerIndex index)
        {
            return Settings[(int)index];
        }

        static Buttons ReadButtons(IntPtr device, PadConfig c, float deadZoneSize)
        {
            short DeadZone = (short)(deadZoneSize * short.MaxValue);
            Buttons b = (Buttons)0;

            if (c.Button_A.ReadBool(device, DeadZone))
                b |= Buttons.A;
            if (c.Button_B.ReadBool(device, DeadZone))
                b |= Buttons.B;
            if (c.Button_X.ReadBool(device, DeadZone))
                b |= Buttons.X;
            if (c.Button_Y.ReadBool(device, DeadZone))
                b |= Buttons.Y;

            if (c.Button_LB.ReadBool(device, DeadZone))
                b |= Buttons.LeftShoulder;
            if (c.Button_RB.ReadBool(device, DeadZone))
                b |= Buttons.RightShoulder;

            if (c.Button_Back.ReadBool(device, DeadZone))
                b |= Buttons.Back;
            if (c.Button_Start.ReadBool(device, DeadZone))
                b |= Buttons.Start;

            if (c.LeftStick.Press.ReadBool(device, DeadZone))
                b |= Buttons.LeftStick;
            if (c.RightStick.Press.ReadBool(device, DeadZone))
                b |= Buttons.RightStick;

            if (c.Dpad.Up.ReadBool(device, DeadZone))
                b |= Buttons.DPadUp;
            if (c.Dpad.Down.ReadBool(device, DeadZone))
                b |= Buttons.DPadDown;
            if (c.Dpad.Left.ReadBool(device, DeadZone))
                b |= Buttons.DPadLeft;
            if (c.Dpad.Right.ReadBool(device, DeadZone))
                b |= Buttons.DPadRight;

            return b;
        }
		
		static Buttons StickToButtons( Vector2 stick, Buttons left, Buttons right, Buttons up , Buttons down, float DeadZoneSize )
		{
			Buttons b = (Buttons)0;

			if ( stick.X > DeadZoneSize )
				b |= right;
			if ( stick.X < -DeadZoneSize )
				b |= left;
			if ( stick.Y > DeadZoneSize )
				b |= up;
			if ( stick.Y < -DeadZoneSize )
				b |= down;
			
			return b;
		}
		
		static Buttons TriggerToButton( float trigger, Buttons button, float DeadZoneSize )
		{
			Buttons b = (Buttons)0;
            
			if ( trigger > DeadZoneSize )
				b |= button;

			return b;
		}
		
        static GamePadState ReadState(PlayerIndex index, GamePadDeadZone deadZone)
        {
            const float DeadZoneSize = 0.27f;
            IntPtr device = GetDevice(index);
            PadConfig c = GetConfig(index);
            if (device == IntPtr.Zero || c == null)
                return GamePadState.InitializedState;

            var leftStick = c.LeftStick.ReadAxisPair(device);
            var rightStick = c.RightStick.ReadAxisPair(device);
            GamePadThumbSticks sticks = new GamePadThumbSticks(new Vector2(leftStick.X, leftStick.Y), new Vector2(rightStick.X, rightStick.Y));
            sticks.ApplyDeadZone(deadZone, DeadZoneSize);
            GamePadTriggers triggers = new GamePadTriggers(c.LeftTrigger.ReadFloat(device), c.RightTrigger.ReadFloat(device));
			Buttons buttonState = ReadButtons(device, c, DeadZoneSize);
			buttonState |= StickToButtons(sticks.Left, Buttons.LeftThumbstickLeft, Buttons.LeftThumbstickRight, Buttons.LeftThumbstickUp, Buttons.LeftThumbstickDown, DeadZoneSize);
			buttonState |= StickToButtons(sticks.Right, Buttons.RightThumbstickLeft, Buttons.RightThumbstickRight, Buttons.RightThumbstickUp, Buttons.RightThumbstickDown, DeadZoneSize);
			buttonState |= TriggerToButton(triggers.Left, Buttons.LeftTrigger, DeadZoneSize);
			buttonState |= TriggerToButton(triggers.Right, Buttons.RightTrigger, DeadZoneSize);
            GamePadButtons buttons = new GamePadButtons(buttonState);
            GamePadDPad dpad = new GamePadDPad(buttons.buttons);

            GamePadState g = new GamePadState(sticks, triggers, buttons, dpad);
            return g;
        }

        //
        // Summary:
        //     Retrieves the capabilities of an Xbox 360 Controller.
        //
        // Parameters:
        //   playerIndex:
        //     Index of the controller to query.
        public static GamePadCapabilities GetCapabilities(PlayerIndex playerIndex)
        {
            IntPtr d = GetDevice(playerIndex);
            PadConfig c = GetConfig(playerIndex);

            if (c == null || ((c.JoystickName == null || c.JoystickName == string.Empty) && d == IntPtr.Zero))
                return new GamePadCapabilities();

            return new GamePadCapabilities()
            {
                IsConnected = d != IntPtr.Zero,
                HasAButton = c.Button_A.Type != InputType.None,
                HasBButton = c.Button_B.Type != InputType.None,
                HasXButton = c.Button_X.Type != InputType.None,
                HasYButton = c.Button_Y.Type != InputType.None,
                HasBackButton = c.Button_Back.Type != InputType.None,
                HasStartButton = c.Button_Start.Type != InputType.None,
                HasDPadDownButton = c.Dpad.Down.Type != InputType.None,
                HasDPadLeftButton = c.Dpad.Left.Type != InputType.None,
                HasDPadRightButton = c.Dpad.Right.Type != InputType.None,
                HasDPadUpButton = c.Dpad.Up.Type != InputType.None,
                HasLeftShoulderButton = c.Button_LB.Type != InputType.None,
                HasRightShoulderButton = c.Button_RB.Type != InputType.None,
                HasLeftStickButton = c.LeftStick.Press.Type != InputType.None,
                HasRightStickButton = c.RightStick.Press.Type != InputType.None,
                HasLeftTrigger = c.LeftTrigger.Type != InputType.None,
                HasRightTrigger = c.RightTrigger.Type != InputType.None,
                HasLeftXThumbStick = c.LeftStick.X.Type != InputType.None,
                HasLeftYThumbStick = c.LeftStick.Y.Type != InputType.None,
                HasRightXThumbStick = c.RightStick.X.Type != InputType.None,
                HasRightYThumbStick = c.RightStick.Y.Type != InputType.None,

                HasLeftVibrationMotor = false,
                HasRightVibrationMotor = false,
                HasVoiceSupport = false,
                HasBigButton = false
            };
        }
        //
        // Summary:
        //     Gets the current state of a game pad controller. Reference page contains
        //     links to related code samples.
        //
        // Parameters:
        //   playerIndex:
        //     Player index for the controller you want to query.
        public static GamePadState GetState(PlayerIndex playerIndex)
        {
            return GetState(playerIndex, GamePadDeadZone.IndependentAxes);
        }
        //
        // Summary:
        //     Gets the current state of a game pad controller, using a specified dead zone
        //     on analog stick positions. Reference page contains links to related code
        //     samples.
        //
        // Parameters:
        //   playerIndex:
        //     Player index for the controller you want to query.
        //
        //   deadZoneMode:
        //     Enumerated value that specifies what dead zone type to use.
        public static GamePadState GetState(PlayerIndex playerIndex, GamePadDeadZone deadZoneMode)
        {
            PrepSettings();
            if (sdl)
				Sdl.SDL_JoystickUpdate();
            return ReadState(playerIndex, deadZoneMode);
        }
        //
        // Summary:
        //     Sets the vibration motor speeds on an Xbox 360 Controller. Reference page
        //     contains links to related code samples.
        //
        // Parameters:
        //   playerIndex:
        //     Player index that identifies the controller to set.
        //
        //   leftMotor:
        //     The speed of the left motor, between 0.0 and 1.0. This motor is a low-frequency
        //     motor.
        //
        //   rightMotor:
        //     The speed of the right motor, between 0.0 and 1.0. This motor is a high-frequency
        //     motor.
        public static bool SetVibration(PlayerIndex playerIndex, float leftMotor, float rightMotor)
        {
            return false;
        }
    }
}