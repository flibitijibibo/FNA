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
using System.Xml.Serialization;
using System.Runtime.InteropServices;

using SDL2;

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
    public static class SdlGamePad
    {
        // NOTE: SdlGamePad uses SDL_Joystick, GamePad uses SDL_GameController -flibit

        // The SDL device lists
        private static IntPtr[] INTERNAL_devices = new IntPtr[4];
        private static IntPtr[] INTERNAL_haptics = new IntPtr[4];
  
        // The XNA Input Settings for the GamePad system
        private static Settings INTERNAL_settings;
        
        // Where we will load our config file into.
        private static MonoGameJoystickConfig INTERNAL_joystickConfig;
        
        // Call this when you're done, if you don't want to depend on SDL_Quit();
        internal static void Cleanup()
        {
            if (SDL.SDL_WasInit(SDL.SDL_INIT_GAMECONTROLLER) == 1)
            {
                SDL.SDL_QuitSubSystem(SDL.SDL_INIT_GAMECONTROLLER);
            }
            if (SDL.SDL_WasInit(SDL.SDL_INIT_JOYSTICK) == 1)
            {
                SDL.SDL_QuitSubSystem(SDL.SDL_INIT_JOYSTICK);
            }
            if (SDL.SDL_WasInit(SDL.SDL_INIT_HAPTIC) == 1)
            {
                SDL.SDL_QuitSubSystem(SDL.SDL_INIT_HAPTIC);
            }
        }
        
        // Convenience method to check for Rumble support
        private static bool INTERNAL_HapticSupported(PlayerIndex playerIndex)
        {
            IntPtr haptic = INTERNAL_haptics[(int) playerIndex];
            return (    haptic != IntPtr.Zero &&
                        (    (  SDL.SDL_HapticEffectSupported(haptic, ref GamePad.INTERNAL_Custom_effect) == 1 && (SDL.SDL_HapticNumAxes(haptic) == 2)  ) ||
                        SDL.SDL_HapticEffectSupported(haptic, ref GamePad.INTERNAL_LeftRight_effect) == 1 ||
                             SDL.SDL_HapticRumbleSupported(haptic) == 1  )       );
        }
  
        // Prepare the MonoGameJoystick configuration system
        private static void INTERNAL_AutoConfig()
        {
            if (SDL.SDL_WasInit(SDL.SDL_INIT_JOYSTICK) == 0)
            {
                SDL.SDL_InitSubSystem(SDL.SDL_INIT_JOYSTICK);
            }
            if (SDL.SDL_WasInit(SDL.SDL_INIT_GAMECONTROLLER) == 0)
            {
                SDL.SDL_InitSubSystem(SDL.SDL_INIT_GAMECONTROLLER);
            }
            if (SDL.SDL_WasInit(SDL.SDL_INIT_HAPTIC) == 0)
            {
                SDL.SDL_InitSubSystem(SDL.SDL_INIT_HAPTIC);
            }
            
            // Get the intended config file path.
            string osConfigFile = "";
            if (    Environment.OSVersion.Platform == PlatformID.MacOSX ||
                    (   System.IO.Directory.Exists("/Users/") &&
                        Environment.OSVersion.Platform != PlatformID.Win32NT    )   )
            {
                osConfigFile += Environment.GetEnvironmentVariable("HOME");
                if (osConfigFile.Length == 0)
                {
                    osConfigFile += "MonoGameJoystick.cfg"; // Oh well.
                }
                else
                {
                    osConfigFile += "/Library/Application Support/MonoGame/MonoGameJoystick.cfg";
                }
            }
            else if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                // Assuming a non-OSX Unix platform will follow the XDG. Which it should.
                osConfigFile += Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
                if (osConfigFile.Length == 0)
                {
                    osConfigFile += Environment.GetEnvironmentVariable("HOME");
                    if (osConfigFile.Length == 0)
                    {
                        osConfigFile += "MonoGameJoystick.cfg"; // Oh well.
                    }
                    else
                    {
                        osConfigFile += "/.config/MonoGame/MonoGameJoystick.cfg";
                    }
                }
                else
                {
                    osConfigFile += "/MonoGame/MonoGameJoystick.cfg";
                }
            }
            else
            {
                osConfigFile = "MonoGameJoystick.cfg"; // Oh well.
            }
            
            // Check to see if we've already got a config...
            if (File.Exists(osConfigFile))
            {
                // Load the file.
                FileStream fileIn = File.OpenRead(osConfigFile);
                
                // Load the data into our config struct.
                XmlSerializer serializer = new XmlSerializer(typeof(MonoGameJoystickConfig));
                INTERNAL_joystickConfig = (MonoGameJoystickConfig) serializer.Deserialize(fileIn);
                
                // We out.
                fileIn.Close();
            }
            else
            {
                // First of all, just set our config to default values.
                
                // NOTE: These are based on a 360 controller on Linux.
                
                // Start
                INTERNAL_joystickConfig.BUTTON_START.INPUT_TYPE = InputType.Button;
                INTERNAL_joystickConfig.BUTTON_START.INPUT_ID = 7;
                INTERNAL_joystickConfig.BUTTON_START.INPUT_INVERT = false;
                
                // Back
                INTERNAL_joystickConfig.BUTTON_BACK.INPUT_TYPE = InputType.Button;
                INTERNAL_joystickConfig.BUTTON_BACK.INPUT_ID = 6;
                INTERNAL_joystickConfig.BUTTON_BACK.INPUT_INVERT = false;
                
                // A
                INTERNAL_joystickConfig.BUTTON_A.INPUT_TYPE = InputType.Button;
                INTERNAL_joystickConfig.BUTTON_A.INPUT_ID = 0;
                INTERNAL_joystickConfig.BUTTON_A.INPUT_INVERT = false;
                
                // B
                INTERNAL_joystickConfig.BUTTON_B.INPUT_TYPE = InputType.Button;
                INTERNAL_joystickConfig.BUTTON_B.INPUT_ID = 1;
                INTERNAL_joystickConfig.BUTTON_B.INPUT_INVERT = false;
                
                // X
                INTERNAL_joystickConfig.BUTTON_X.INPUT_TYPE = InputType.Button;
                INTERNAL_joystickConfig.BUTTON_X.INPUT_ID = 2;
                INTERNAL_joystickConfig.BUTTON_X.INPUT_INVERT = false;
                
                // Y
                INTERNAL_joystickConfig.BUTTON_Y.INPUT_TYPE = InputType.Button;
                INTERNAL_joystickConfig.BUTTON_Y.INPUT_ID = 3;
                INTERNAL_joystickConfig.BUTTON_Y.INPUT_INVERT = false;
                
                // LB
                INTERNAL_joystickConfig.SHOULDER_LB.INPUT_TYPE = InputType.Button;
                INTERNAL_joystickConfig.SHOULDER_LB.INPUT_ID = 4;
                INTERNAL_joystickConfig.SHOULDER_LB.INPUT_INVERT = false;
                
                // RB
                INTERNAL_joystickConfig.SHOULDER_RB.INPUT_TYPE = InputType.Button;
                INTERNAL_joystickConfig.SHOULDER_RB.INPUT_ID = 5;
                INTERNAL_joystickConfig.SHOULDER_RB.INPUT_INVERT = false;
                
                // LT
                INTERNAL_joystickConfig.TRIGGER_LT.INPUT_TYPE = InputType.Axis;
                INTERNAL_joystickConfig.TRIGGER_LT.INPUT_ID = 2;
                INTERNAL_joystickConfig.TRIGGER_LT.INPUT_INVERT = false;
                
                // RT
                INTERNAL_joystickConfig.TRIGGER_RT.INPUT_TYPE = InputType.Axis;
                INTERNAL_joystickConfig.TRIGGER_RT.INPUT_ID = 5;
                INTERNAL_joystickConfig.TRIGGER_RT.INPUT_INVERT = false;
                
                // LStick
                INTERNAL_joystickConfig.BUTTON_LSTICK.INPUT_TYPE = InputType.Button;
                INTERNAL_joystickConfig.BUTTON_LSTICK.INPUT_ID = 9;
                INTERNAL_joystickConfig.BUTTON_LSTICK.INPUT_INVERT = false;
                
                // RStick
                INTERNAL_joystickConfig.BUTTON_RSTICK.INPUT_TYPE = InputType.Button;
                INTERNAL_joystickConfig.BUTTON_RSTICK.INPUT_ID = 10;
                INTERNAL_joystickConfig.BUTTON_RSTICK.INPUT_INVERT = false;
                
                // DPad Up
                INTERNAL_joystickConfig.DPAD_UP.INPUT_TYPE = InputType.PovUp;
                INTERNAL_joystickConfig.DPAD_UP.INPUT_ID = 0;
                INTERNAL_joystickConfig.DPAD_UP.INPUT_INVERT = false;
                
                // DPad Down
                INTERNAL_joystickConfig.DPAD_DOWN.INPUT_TYPE = InputType.PovDown;
                INTERNAL_joystickConfig.DPAD_DOWN.INPUT_ID = 0;
                INTERNAL_joystickConfig.DPAD_DOWN.INPUT_INVERT = false;
                
                // DPad Left
                INTERNAL_joystickConfig.DPAD_LEFT.INPUT_TYPE = InputType.PovLeft;
                INTERNAL_joystickConfig.DPAD_LEFT.INPUT_ID = 0;
                INTERNAL_joystickConfig.DPAD_LEFT.INPUT_INVERT = false;
                
                // DPad Right
                INTERNAL_joystickConfig.DPAD_RIGHT.INPUT_TYPE = InputType.PovRight;
                INTERNAL_joystickConfig.DPAD_RIGHT.INPUT_ID = 0;
                INTERNAL_joystickConfig.DPAD_RIGHT.INPUT_INVERT = false;
                
                // LX
                INTERNAL_joystickConfig.AXIS_LX.INPUT_TYPE = InputType.Axis;
                INTERNAL_joystickConfig.AXIS_LX.INPUT_ID = 0;
                INTERNAL_joystickConfig.AXIS_LX.INPUT_INVERT = false;
                
                // LY
                INTERNAL_joystickConfig.AXIS_LY.INPUT_TYPE = InputType.Axis;
                INTERNAL_joystickConfig.AXIS_LY.INPUT_ID = 1;
                INTERNAL_joystickConfig.AXIS_LY.INPUT_INVERT = false;
                
                // RX
                INTERNAL_joystickConfig.AXIS_RX.INPUT_TYPE = InputType.Axis;
                INTERNAL_joystickConfig.AXIS_RX.INPUT_ID = 3;
                INTERNAL_joystickConfig.AXIS_RX.INPUT_INVERT = false;
                
                // RY
                INTERNAL_joystickConfig.AXIS_RY.INPUT_TYPE = InputType.Axis;
                INTERNAL_joystickConfig.AXIS_RY.INPUT_ID = 4;
                INTERNAL_joystickConfig.AXIS_RY.INPUT_INVERT = false;
                
                
                // Since it doesn't exist, we need to generate the default config.
                
                // ... but is our directory even there?
                string osConfigDir = osConfigFile.Substring(0, osConfigFile.IndexOf("MonoGameJoystick.cfg"));
                if (!String.IsNullOrEmpty(osConfigDir) && !Directory.Exists(osConfigDir))
                {
                    // Okay, jeez, we're really starting fresh.
                    Directory.CreateDirectory(osConfigDir);
                }
                
                // So, create the file.
                FileStream fileOut = File.Open(osConfigFile, FileMode.OpenOrCreate);
                XmlSerializer serializer = new XmlSerializer(typeof(MonoGameJoystickConfig));
                serializer.Serialize(fileOut, INTERNAL_joystickConfig);
                
                // We out.
                fileOut.Close();
            }
            
#if DEBUG
            Console.WriteLine("Number of joysticks: " + SDL.SDL_NumJoysticks());
#endif
            // Limit to the first 4 sticks to avoid crashes.
            int numSticks = Math.Min(4, SDL.SDL_NumJoysticks());
            
            for (int x = 0; x < numSticks; x++)
            {
                // Initialize Joysticks only!
                if (SDL.SDL_IsGameController(x) == SDL.SDL_bool.SDL_TRUE)
                {
                    continue;
                }

                // K, we're a joystick. Let us begin!
                INTERNAL_devices[x] = SDL.SDL_JoystickOpen(x);
                
                // Initialize the haptics for each joystick.
                if (SDL.SDL_JoystickIsHaptic(INTERNAL_devices[x]) == 1)
                {
                    INTERNAL_haptics[x] = SDL.SDL_HapticOpenFromJoystick(INTERNAL_devices[x]);
                }
                if (INTERNAL_haptics[x] != IntPtr.Zero)
                {
                    if (SDL.SDL_HapticEffectSupported(INTERNAL_haptics[x], ref GamePad.INTERNAL_LeftRight_effect) == 1)
                    {
                        SDL.SDL_HapticNewEffect(INTERNAL_haptics[x], ref GamePad.INTERNAL_LeftRight_effect);
                    }
                    else if (SDL.SDL_HapticEffectSupported(INTERNAL_haptics[x], ref GamePad.INTERNAL_Custom_effect) == 1)
                    {
                        SDL.SDL_HapticNewEffect(INTERNAL_haptics[x], ref GamePad.INTERNAL_Custom_effect);
                    }
                    else if (SDL.SDL_HapticRumbleSupported(INTERNAL_haptics[x]) == 1)
                    {
                        SDL.SDL_HapticRumbleInit(INTERNAL_haptics[x]);
                    }
                }
    
                System.Console.WriteLine(
                    "Controller " + x + ", " +
                    SDL.SDL_JoystickName(INTERNAL_devices[x]) +
                    ", will use generic MonoGameJoystick support."
                );
                
                // Where the joystick configurations will be adapted to
                PadConfig pc = new PadConfig(SDL.SDL_JoystickName(INTERNAL_devices[x]), x);
                
                // Start
                pc.Button_Start.ID = INTERNAL_joystickConfig.BUTTON_START.INPUT_ID;
                pc.Button_Start.Type = INTERNAL_joystickConfig.BUTTON_START.INPUT_TYPE;
                pc.Button_Start.Negative = INTERNAL_joystickConfig.BUTTON_START.INPUT_INVERT;
    
                // Back
                pc.Button_Back.ID = INTERNAL_joystickConfig.BUTTON_BACK.INPUT_ID;
                pc.Button_Back.Type = INTERNAL_joystickConfig.BUTTON_BACK.INPUT_TYPE;
                pc.Button_Back.Negative = INTERNAL_joystickConfig.BUTTON_BACK.INPUT_INVERT;
    
                // A
                pc.Button_A.ID = INTERNAL_joystickConfig.BUTTON_A.INPUT_ID;
                pc.Button_A.Type = INTERNAL_joystickConfig.BUTTON_A.INPUT_TYPE;
                pc.Button_A.Negative = INTERNAL_joystickConfig.BUTTON_A.INPUT_INVERT;
    
                // B
                pc.Button_B.ID = INTERNAL_joystickConfig.BUTTON_B.INPUT_ID;
                pc.Button_B.Type = INTERNAL_joystickConfig.BUTTON_B.INPUT_TYPE;
                pc.Button_B.Negative = INTERNAL_joystickConfig.BUTTON_B.INPUT_INVERT;
    
                // X
                pc.Button_X.ID = INTERNAL_joystickConfig.BUTTON_X.INPUT_ID;
                pc.Button_X.Type = INTERNAL_joystickConfig.BUTTON_X.INPUT_TYPE;
                pc.Button_X.Negative = INTERNAL_joystickConfig.BUTTON_X.INPUT_INVERT;
    
                // Y
                pc.Button_Y.ID = INTERNAL_joystickConfig.BUTTON_Y.INPUT_ID;
                pc.Button_Y.Type = INTERNAL_joystickConfig.BUTTON_Y.INPUT_TYPE;
                pc.Button_Y.Negative = INTERNAL_joystickConfig.BUTTON_Y.INPUT_INVERT;
    
                // LB
                pc.Button_LB.ID = INTERNAL_joystickConfig.SHOULDER_LB.INPUT_ID;
                pc.Button_LB.Type = INTERNAL_joystickConfig.SHOULDER_LB.INPUT_TYPE;
                pc.Button_LB.Negative = INTERNAL_joystickConfig.SHOULDER_LB.INPUT_INVERT;
    
                // RB
                pc.Button_RB.ID = INTERNAL_joystickConfig.SHOULDER_RB.INPUT_ID;
                pc.Button_RB.Type = INTERNAL_joystickConfig.SHOULDER_RB.INPUT_TYPE;
                pc.Button_RB.Negative = INTERNAL_joystickConfig.SHOULDER_RB.INPUT_INVERT;
                
                // LT
                pc.LeftTrigger.ID = INTERNAL_joystickConfig.TRIGGER_LT.INPUT_ID;
                pc.LeftTrigger.Type = INTERNAL_joystickConfig.TRIGGER_LT.INPUT_TYPE;
                pc.LeftTrigger.Negative = INTERNAL_joystickConfig.TRIGGER_LT.INPUT_INVERT;
                
                // RT
                pc.RightTrigger.ID = INTERNAL_joystickConfig.TRIGGER_RT.INPUT_ID;
                pc.RightTrigger.Type = INTERNAL_joystickConfig.TRIGGER_RT.INPUT_TYPE;
                pc.RightTrigger.Negative = INTERNAL_joystickConfig.TRIGGER_RT.INPUT_INVERT;
    
                // LStick
                pc.LeftStick.Press.ID = INTERNAL_joystickConfig.BUTTON_LSTICK.INPUT_ID;
                pc.LeftStick.Press.Type = INTERNAL_joystickConfig.BUTTON_LSTICK.INPUT_TYPE;
                pc.LeftStick.Press.Negative = INTERNAL_joystickConfig.BUTTON_LSTICK.INPUT_INVERT;
    
                // RStick
                pc.RightStick.Press.ID = INTERNAL_joystickConfig.BUTTON_RSTICK.INPUT_ID;
                pc.RightStick.Press.Type = INTERNAL_joystickConfig.BUTTON_RSTICK.INPUT_TYPE;
                pc.RightStick.Press.Negative = INTERNAL_joystickConfig.BUTTON_RSTICK.INPUT_INVERT;
                
                // DPad Up
                pc.Dpad.Up.ID = INTERNAL_joystickConfig.DPAD_UP.INPUT_ID;
                pc.Dpad.Up.Type = INTERNAL_joystickConfig.DPAD_UP.INPUT_TYPE;
                pc.Dpad.Up.Negative = INTERNAL_joystickConfig.DPAD_UP.INPUT_INVERT;
                
                // DPad Down
                pc.Dpad.Down.ID = INTERNAL_joystickConfig.DPAD_DOWN.INPUT_ID;
                pc.Dpad.Down.Type = INTERNAL_joystickConfig.DPAD_DOWN.INPUT_TYPE;
                pc.Dpad.Down.Negative = INTERNAL_joystickConfig.DPAD_DOWN.INPUT_INVERT;
                
                // DPad Left
                pc.Dpad.Left.ID = INTERNAL_joystickConfig.DPAD_LEFT.INPUT_ID;
                pc.Dpad.Left.Type = INTERNAL_joystickConfig.DPAD_LEFT.INPUT_TYPE;
                pc.Dpad.Left.Negative = INTERNAL_joystickConfig.DPAD_LEFT.INPUT_INVERT;
                
                // DPad Right
                pc.Dpad.Right.ID = INTERNAL_joystickConfig.DPAD_RIGHT.INPUT_ID;
                pc.Dpad.Right.Type = INTERNAL_joystickConfig.DPAD_RIGHT.INPUT_TYPE;
                pc.Dpad.Right.Negative = INTERNAL_joystickConfig.DPAD_RIGHT.INPUT_INVERT;
    
                // LX
                pc.LeftStick.X.Negative.ID = INTERNAL_joystickConfig.AXIS_LX.INPUT_ID;
                pc.LeftStick.X.Negative.Type = INTERNAL_joystickConfig.AXIS_LX.INPUT_TYPE;
                pc.LeftStick.X.Negative.Negative = !INTERNAL_joystickConfig.AXIS_LX.INPUT_INVERT;
                pc.LeftStick.X.Positive.ID = INTERNAL_joystickConfig.AXIS_LX.INPUT_ID;
                pc.LeftStick.X.Positive.Type = INTERNAL_joystickConfig.AXIS_LX.INPUT_TYPE;
                pc.LeftStick.X.Positive.Negative = INTERNAL_joystickConfig.AXIS_LX.INPUT_INVERT;
    
                // LY
                pc.LeftStick.Y.Negative.ID = INTERNAL_joystickConfig.AXIS_LY.INPUT_ID;
                pc.LeftStick.Y.Negative.Type = INTERNAL_joystickConfig.AXIS_LY.INPUT_TYPE;
                pc.LeftStick.Y.Negative.Negative = !INTERNAL_joystickConfig.AXIS_LY.INPUT_INVERT;
                pc.LeftStick.Y.Positive.ID = INTERNAL_joystickConfig.AXIS_LY.INPUT_ID;
                pc.LeftStick.Y.Positive.Type = INTERNAL_joystickConfig.AXIS_LY.INPUT_TYPE;
                pc.LeftStick.Y.Positive.Negative = INTERNAL_joystickConfig.AXIS_LY.INPUT_INVERT;
    
                // RX
                pc.RightStick.X.Negative.ID = INTERNAL_joystickConfig.AXIS_RX.INPUT_ID;
                pc.RightStick.X.Negative.Type = INTERNAL_joystickConfig.AXIS_RX.INPUT_TYPE;
                pc.RightStick.X.Negative.Negative = !INTERNAL_joystickConfig.AXIS_RX.INPUT_INVERT;
                pc.RightStick.X.Positive.ID = INTERNAL_joystickConfig.AXIS_RX.INPUT_ID;
                pc.RightStick.X.Positive.Type = INTERNAL_joystickConfig.AXIS_RX.INPUT_TYPE;
                pc.RightStick.X.Positive.Negative = INTERNAL_joystickConfig.AXIS_RX.INPUT_INVERT;
    
                // RY
                pc.RightStick.Y.Negative.ID = INTERNAL_joystickConfig.AXIS_RY.INPUT_ID;
                pc.RightStick.Y.Negative.Type = INTERNAL_joystickConfig.AXIS_RY.INPUT_TYPE;
                pc.RightStick.Y.Negative.Negative = !INTERNAL_joystickConfig.AXIS_RY.INPUT_INVERT;
                pc.RightStick.Y.Positive.ID = INTERNAL_joystickConfig.AXIS_RY.INPUT_ID;
                pc.RightStick.Y.Positive.Type = INTERNAL_joystickConfig.AXIS_RY.INPUT_TYPE;
                pc.RightStick.Y.Positive.Negative = INTERNAL_joystickConfig.AXIS_RY.INPUT_INVERT;

                // Suggestion: Xbox Guide button <=> BigButton
                // pc.BigButton.ID = 8;
                // pc.BigButton.Type = InputType.Button;

#if DEBUG
                int numbuttons = SDL.SDL_JoystickNumButtons(INTERNAL_devices[x]);
                Console.WriteLine("Number of buttons for joystick: " + x + " - " + numbuttons);

                int numaxes = SDL.SDL_JoystickNumAxes(INTERNAL_devices[x]);
                Console.WriteLine("Number of axes for joystick: " + x + " - " + numaxes);

                int numhats = SDL.SDL_JoystickNumHats(INTERNAL_devices[x]);
                Console.WriteLine("Number of PovHats for joystick: " + x + " - " + numhats);
#endif
                
                // Assign our results, finally.
                INTERNAL_settings[x] = pc;
            }
        }
  
        // Button reader for ReadState
        private static Buttons READ_ReadButtons(IntPtr device, PadConfig c, float deadZoneSize)
        {
            short DeadZone = (short) (deadZoneSize * short.MaxValue);
            Buttons b = (Buttons) 0;
   
            // A B X Y
            if (c.Button_A.ReadBool(device, DeadZone))
            {
                b |= Buttons.A;
            }
            if (c.Button_B.ReadBool(device, DeadZone))
            {
                b |= Buttons.B;
            }
            if (c.Button_X.ReadBool(device, DeadZone))
            {
                b |= Buttons.X;
            }
            if (c.Button_Y.ReadBool(device, DeadZone))
            {
                b |= Buttons.Y;
            }
   
            // Shoulder buttons
            if (c.Button_LB.ReadBool(device, DeadZone))
            {
                b |= Buttons.LeftShoulder;
            }
            if (c.Button_RB.ReadBool(device, DeadZone))
            {
                b |= Buttons.RightShoulder;
            }
   
            // Back/Start
            if (c.Button_Back.ReadBool(device, DeadZone))
            {
                b |= Buttons.Back;
            }
            if (c.Button_Start.ReadBool(device, DeadZone))
            {
                b |= Buttons.Start;
            }
   
            // Stick buttons
            if (c.LeftStick.Press.ReadBool(device, DeadZone))
            {
                b |= Buttons.LeftStick;
            }
            if (c.RightStick.Press.ReadBool(device, DeadZone))
            {
                b |= Buttons.RightStick;
            }
   
            // DPad
            if (c.Dpad.Up.ReadBool(device, DeadZone))
            {
                b |= Buttons.DPadUp;
            }
            if (c.Dpad.Down.ReadBool(device, DeadZone))
            {
                b |= Buttons.DPadDown;
            }
            if (c.Dpad.Left.ReadBool(device, DeadZone))
            {
                b |= Buttons.DPadLeft;
            }
            if (c.Dpad.Right.ReadBool(device, DeadZone))
            {
                b |= Buttons.DPadRight;
            }

            return b;
        }
        
        // ReadState can convert stick values to button values
        private static Buttons READ_StickToButtons(Vector2 stick, Buttons left, Buttons right, Buttons up , Buttons down, float DeadZoneSize)
        {
            Buttons b = (Buttons) 0;

            if (stick.X > DeadZoneSize)
            {
                b |= right;
            }
            if (stick.X < -DeadZoneSize)
            {
                b |= left;
            }
            if (stick.Y > DeadZoneSize)
            {
                b |= up;
            }
            if (stick.Y < -DeadZoneSize)
            {
                b |= down;
            }
            
            return b;
        }
        
        // ReadState can convert trigger values to button values
        private static Buttons READ_TriggerToButton(float trigger, Buttons button, float DeadZoneSize)
        {
            Buttons b = (Buttons)0;
            
            if (trigger > DeadZoneSize)
            {
                b |= button;
            }
            
            return b;
        }
        
        // This is where we actually read in the controller input!
        private static GamePadState ReadState(PlayerIndex index, GamePadDeadZone deadZone)
        {
            IntPtr device = INTERNAL_devices[(int) index];
            if (device == IntPtr.Zero)
            {
                return GamePadState.InitializedState;
            }
            
            // Do not attempt to understand this number at all costs!
            const float DeadZoneSize = 0.27f;
            
            PadConfig config = INTERNAL_settings[(int) index];
            if (config == null)
            {
                return GamePadState.InitializedState;
            }
            
            // We will interpret the joystick values into this.
            Buttons buttonState = (Buttons) 0;
            
            // Sticks
            GamePadThumbSticks sticks = new GamePadThumbSticks(
                config.LeftStick.ReadAxisPair(device),
                config.RightStick.ReadAxisPair(device)
            );
            sticks.ApplyDeadZone(deadZone, DeadZoneSize);
            buttonState |= READ_StickToButtons(
                sticks.Left,
                Buttons.LeftThumbstickLeft,
                Buttons.LeftThumbstickRight,
                Buttons.LeftThumbstickUp,
                Buttons.LeftThumbstickDown,
                DeadZoneSize
            );
            buttonState |= READ_StickToButtons(
                sticks.Right,
                Buttons.RightThumbstickLeft,
                Buttons.RightThumbstickRight,
                Buttons.RightThumbstickUp,
                Buttons.RightThumbstickDown,
                DeadZoneSize
            );
            
            // Buttons
            buttonState = READ_ReadButtons(device, config, DeadZoneSize);
            
            // Triggers
            GamePadTriggers triggers = new GamePadTriggers(
                config.LeftTrigger.ReadFloat(device),
                config.RightTrigger.ReadFloat(device)
            );
            buttonState |= READ_TriggerToButton(
                triggers.Left,
                Buttons.LeftTrigger,
                DeadZoneSize
            );
            buttonState |= READ_TriggerToButton(
                triggers.Right,
                Buttons.RightTrigger,
                DeadZoneSize
            );
            
            // Compile the GamePadButtons with our Buttons state
            GamePadButtons buttons = new GamePadButtons(buttonState);
            
            // DPad
            GamePadDPad dpad = new GamePadDPad(buttons.buttons);
   
            // Return the compiled GamePadState.
            return new GamePadState(sticks, triggers, buttons, dpad);
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
            IntPtr d = INTERNAL_devices[(int) playerIndex];
            PadConfig c = INTERNAL_settings[(int) playerIndex];

            if (    c == null ||
                    (   String.IsNullOrEmpty(c.JoystickName) &&
                        d == IntPtr.Zero    )   )
            {
                return new GamePadCapabilities();
            }

            return new GamePadCapabilities()
            {
                IsConnected = (d != IntPtr.Zero),
                
                HasAButton =                c.Button_A.Type     != InputType.None,
                HasBButton =                c.Button_B.Type     != InputType.None,
                HasXButton =                c.Button_X.Type     != InputType.None,
                HasYButton =                c.Button_Y.Type     != InputType.None,
                HasBackButton =             c.Button_Back.Type  != InputType.None,
                HasStartButton =            c.Button_Start.Type != InputType.None,
                HasDPadDownButton =         c.Dpad.Down.Type    != InputType.None,
                HasDPadLeftButton =         c.Dpad.Left.Type    != InputType.None,
                HasDPadRightButton =        c.Dpad.Right.Type   != InputType.None,
                HasDPadUpButton =           c.Dpad.Up.Type      != InputType.None,
                HasLeftShoulderButton =     c.Button_LB.Type    != InputType.None,
                HasRightShoulderButton =    c.Button_RB.Type    != InputType.None,
                HasLeftStickButton =        c.LeftStick.Press.Type  != InputType.None,
                HasRightStickButton =       c.RightStick.Press.Type != InputType.None,
                HasLeftTrigger =            c.LeftTrigger.Type  != InputType.None,
                HasRightTrigger =           c.RightTrigger.Type != InputType.None,
                HasLeftXThumbStick =        c.LeftStick.X.Type  != InputType.None,
                HasLeftYThumbStick =        c.LeftStick.Y.Type  != InputType.None,
                HasRightXThumbStick =       c.RightStick.X.Type != InputType.None,
                HasRightYThumbStick =       c.RightStick.Y.Type != InputType.None,

                HasLeftVibrationMotor = INTERNAL_HapticSupported(playerIndex),
                HasRightVibrationMotor = INTERNAL_HapticSupported(playerIndex),
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
            if (INTERNAL_settings == null)
            {
                INTERNAL_settings = new Settings();
                INTERNAL_AutoConfig();
            }
            if (SDL.SDL_WasInit(SDL.SDL_INIT_JOYSTICK) == 1)
            {
                SDL.SDL_JoystickUpdate();
            }
            if (SDL.SDL_WasInit(SDL.SDL_INIT_GAMECONTROLLER) == 1)
            {
                SDL.SDL_GameControllerUpdate();
            }
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
            if (!INTERNAL_HapticSupported(playerIndex))
            {
                return false;
            }
            
            if (leftMotor <= 0.0f && rightMotor <= 0.0f)
            {
                SDL.SDL_HapticStopAll(INTERNAL_haptics[(int)playerIndex]);
                return true;
            }
            else if (SDL.SDL_HapticEffectSupported(INTERNAL_haptics[(int) playerIndex], ref GamePad.INTERNAL_LeftRight_effect) == 1) {
                GamePad.INTERNAL_LeftRight_effect.leftright.large_magnitude = (ushort)(65535.0f * leftMotor);
                GamePad.INTERNAL_LeftRight_effect.leftright.small_magnitude = (ushort)(65535.0f * rightMotor);
                SDL.SDL_HapticUpdateEffect (
                    INTERNAL_haptics[(int) playerIndex],
                    0,
                    ref GamePad.INTERNAL_LeftRight_effect
                );
                SDL.SDL_HapticRunEffect (
                    INTERNAL_haptics[(int) playerIndex],
                    0,
                    1
                );
            }
            else if (SDL.SDL_HapticEffectSupported (INTERNAL_haptics [(int)playerIndex], ref GamePad.INTERNAL_Custom_effect) == 1)
            {
                ushort[] data = {(ushort)(65535.0f * leftMotor), (ushort)(65535.0f * rightMotor)};
                GCHandle pArry = GCHandle.Alloc(data, GCHandleType.Pinned);
                IntPtr ptr = pArry.AddrOfPinnedObject();
                GamePad.INTERNAL_Custom_effect.custom.data = ptr;
                SDL.SDL_HapticUpdateEffect(
                    INTERNAL_haptics[(int) playerIndex],
                    0,
                    ref GamePad.INTERNAL_Custom_effect
                );
                SDL.SDL_HapticRunEffect(
                    INTERNAL_haptics[(int) playerIndex],
                    0,
                    1
                );
                pArry.Free();
            }
            else
            {
                float strength;
                if (leftMotor >= rightMotor)
                {
                    strength = leftMotor;
                }
                else
                {
                    strength = rightMotor;
                }
                SDL.SDL_HapticRumblePlay(
                    INTERNAL_haptics[(int)playerIndex],
                    strength,
                    uint.MaxValue // Oh dear...
                );
            }
            return true;
        }

        // BEGIN FEZ SPECIFIC STUFF

        public static int GetPadCount()
        {
            return INTERNAL_devices.Count(x => x != IntPtr.Zero);
        }

        public static PadConfig GetConfig(PlayerIndex index)
        {
            return INTERNAL_settings[(int) index];
        }

        public static int? GetPressedButtonId()
        {
            for (int i = 0; i < INTERNAL_devices.Length; i++)
            {
                IntPtr device = INTERNAL_devices[(int) PlayerIndex.One + i];
                for (int j = 0; j < SDL.SDL_JoystickNumButtons(device); j++)
                {
                    if (SDL.SDL_JoystickGetButton(device, j) > 0)
                    {
                        return j;
                    }
                }
            }
            return null;
        }

        // END FEZ SPECIFIC STUFF
    }
}
