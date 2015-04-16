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
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

using SDL2;

namespace Microsoft.Xna.Framework.Input
{
    //
    // Summary:
    //     Allows retrieval of user interaction with an Xbox 360 Controller and setting
    //     of controller vibration motors. Reference page contains links to related
    //     code samples.
    public static class GamePad
    {
        // NOTE: GamePad uses SDL_GameController, SdlGamePad uses SDL_Joystick -flibit

        internal enum HapticType
        {
            Simple = 0,
            LeftRight = 1,
            LeftRightMacHack = 2
        }

        // Is this thing even running?
        private static bool enabled = false;

        // The SDL device lists
        private static IntPtr[] INTERNAL_devices = new IntPtr[4];
        private static IntPtr[] INTERNAL_haptics = new IntPtr[4];
        private static HapticType[] INTERNAL_hapticTypes = new HapticType[4];
        private static string[] INTERNAL_guids = new string[]
        {
            String.Empty, String.Empty, String.Empty, String.Empty
        };
        private static string[] INTERNAL_lightBars = new string[]
        {
            String.Empty, String.Empty, String.Empty, String.Empty
        };

        // We use this to apply XInput-like rumble effects.
        internal static SDL.SDL_HapticEffect INTERNAL_leftRightEffect = new SDL.SDL_HapticEffect
        {
            type = SDL.SDL_HAPTIC_LEFTRIGHT,
            leftright = new SDL.SDL_HapticLeftRight
            {
                type = SDL.SDL_HAPTIC_LEFTRIGHT,
                length = SDL.SDL_HAPTIC_INFINITY,
                large_magnitude = ushort.MaxValue,
                small_magnitude = ushort.MaxValue
            }
        };

        // We use this to get left/right support on OSX via a nice driver workaround!
        internal static ushort[] leftRightMacHackData = {0, 0};
        private static GCHandle leftRightMacHackPArry = GCHandle.Alloc(leftRightMacHackData, GCHandleType.Pinned);
        private static IntPtr leftRightMacHackPtr = leftRightMacHackPArry.AddrOfPinnedObject();
        internal static SDL.SDL_HapticEffect INTERNAL_leftRightMacHackEffect = new SDL.SDL_HapticEffect
        {
            type = SDL.SDL_HAPTIC_CUSTOM,
            custom = new SDL.SDL_HapticCustom
            {
                type = SDL.SDL_HAPTIC_CUSTOM,
                length = SDL.SDL_HAPTIC_INFINITY,
                channels = 2,
                period = 1,
                samples = 2,
                data = leftRightMacHackPtr
            }
        };
        
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

#if DEBUG
            Console.WriteLine("Number of joysticks: " + SDL.SDL_NumJoysticks());
#endif
            // Limit to the first 4 sticks to avoid crashes.
            int numSticks = Math.Min(4, SDL.SDL_NumJoysticks());

            for (int x = 0; x < numSticks; x++)
            {
                // Initialize either a GameController or a Joystick (in SdlGamePad).
                if (SDL.SDL_IsGameController(x) == SDL.SDL_bool.SDL_TRUE)
                {
                    INTERNAL_devices[x] = SDL.SDL_GameControllerOpen(x);
                    IntPtr thisJoystick = SDL.SDL_GameControllerGetJoystick(INTERNAL_devices[x]);

                    if (SDL.SDL_JoystickIsHaptic(thisJoystick) == 1)
                    {
                        INTERNAL_haptics[x] = SDL.SDL_HapticOpenFromJoystick(thisJoystick);
                    }
                    if (INTERNAL_haptics[x] != IntPtr.Zero)
                    {
                        if (    SDL.SDL_GetPlatform().Equals("Mac OS X") &&
                                SDL.SDL_HapticEffectSupported(INTERNAL_haptics[x], ref INTERNAL_leftRightMacHackEffect) == 1    )
                        {
                            INTERNAL_hapticTypes[x] = HapticType.LeftRightMacHack;
                            SDL.SDL_HapticNewEffect(INTERNAL_haptics[x], ref INTERNAL_leftRightMacHackEffect);
                        }
                        else if (    !SDL.SDL_GetPlatform().Equals("Mac OS X") &&
                                     SDL.SDL_HapticEffectSupported(INTERNAL_haptics[x], ref INTERNAL_leftRightEffect) == 1  )
                        {
                            INTERNAL_hapticTypes[x] = HapticType.LeftRight;
                            SDL.SDL_HapticNewEffect(INTERNAL_haptics[x], ref INTERNAL_leftRightEffect);
                        }
                        else if (SDL.SDL_HapticRumbleSupported(INTERNAL_haptics[x]) == 1)
                        {
                            INTERNAL_hapticTypes[x] = HapticType.Simple;
                            SDL.SDL_HapticRumbleInit(INTERNAL_haptics[x]);
                        }
                        else
                        {
                            // We can't even play simple rumble, this haptic device is useless to us.
                            SDL.SDL_HapticClose(INTERNAL_haptics[x]);
                            INTERNAL_haptics[x] = IntPtr.Zero;
                        }
                    }

                    // Store the GUID string for this device
                    StringBuilder result = new StringBuilder();
                    byte[] resChar = new byte[33]; // FIXME: Sort of arbitrary.
                    SDL.SDL_JoystickGetGUIDString(
                        SDL.SDL_JoystickGetGUID(thisJoystick),
                        resChar,
                        resChar.Length
                    );
                    if (SDL.SDL_GetPlatform().Equals("Linux"))
                    {
                        result.Append((char) resChar[8]);
                        result.Append((char) resChar[9]);
                        result.Append((char) resChar[10]);
                        result.Append((char) resChar[11]);
                        result.Append((char) resChar[16]);
                        result.Append((char) resChar[17]);
                        result.Append((char) resChar[18]);
                        result.Append((char) resChar[19]);
                    }
                    else if (SDL.SDL_GetPlatform().Equals("Mac OS X"))
                    {
                        result.Append((char) resChar[0]);
                        result.Append((char) resChar[1]);
                        result.Append((char) resChar[2]);
                        result.Append((char) resChar[3]);
                        result.Append((char) resChar[16]);
                        result.Append((char) resChar[17]);
                        result.Append((char) resChar[18]);
                        result.Append((char) resChar[19]);
                    }
                    else if (SDL.SDL_GetPlatform().Equals("Windows"))
                    {
                        bool isXInput = true;
                        foreach (byte b in resChar)
                        {
                            if (((char) b) != '0' && b != 0)
                            {
                                isXInput = false;
                                break;
                            }
                        }
                        if (isXInput)
                        {
                            result.Append("xinput");
                        }
                        else
                        {
                            result.Append((char) resChar[0]);
                            result.Append((char) resChar[1]);
                            result.Append((char) resChar[2]);
                            result.Append((char) resChar[3]);
                            result.Append((char) resChar[4]);
                            result.Append((char) resChar[5]);
                            result.Append((char) resChar[6]);
                            result.Append((char) resChar[7]);
                        }
                    }
                    else
                    {
                        throw new Exception("SDL2_GamePad: Platform.OSVersion not handled!");
                    }
                    INTERNAL_guids[x] = result.ToString();

                    // Initialize light bar
                    if (    SDL.SDL_GetPlatform().Equals("Linux") &&
                            INTERNAL_guids[x].Equals("4c05c405")    )
                    {
                        // Get all of the individual PS4 LED instances
                        List<string> ledList = new List<string>();
                        string[] dirs = Directory.GetDirectories("/sys/class/leds/");
                        foreach (string dir in dirs)
                        {
                            if (    dir.Contains("054C:05C4") &&
                                    dir.EndsWith("blue")    )
                            {
                                ledList.Add(dir.Substring(0, dir.LastIndexOf(':') + 1));
                            }
                        }
                        // Find how many of these are already in use
                        int numLights = 0;
                        for (int i = 0; i < INTERNAL_lightBars.Length; i += 1)
                        {
                            if (!String.IsNullOrEmpty(INTERNAL_lightBars[i]))
                            {
                                numLights += 1;
                            }
                        }
                        // If all are not already in use, use the first unused light
                        if (numLights < ledList.Count)
                        {
                            INTERNAL_lightBars[x] = ledList[numLights];
                        }
                    }

                    System.Console.WriteLine(
                        "Controller " + x + ", " +
                        SDL.SDL_GameControllerName(INTERNAL_devices[x]) +
                        ", will use SDL_GameController support."
                    );
                }
            }

            // We made it!
            enabled = true;
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
            
                // The "master" button state is built from this.
                Buttons gc_buttonState = (Buttons) 0;
                
                // Sticks
                GamePadThumbSticks gc_sticks = new GamePadThumbSticks(
                    new Vector2(
                        (float) SDL.SDL_GameControllerGetAxis(
                            device,
                            SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX
                        ) / 32768.0f,
                        (float) SDL.SDL_GameControllerGetAxis(
                            device,
                            SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY
                        ) / -32768.0f
                    ),
                    new Vector2(
                        (float) SDL.SDL_GameControllerGetAxis(
                            device,
                            SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTX
                        ) / 32768.0f,
                        (float) SDL.SDL_GameControllerGetAxis(
                            device,
                            SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTY
                        ) / -32768.0f
                    )
                );
                gc_sticks.ApplyDeadZone(deadZone, DeadZoneSize);
                gc_buttonState |= READ_StickToButtons(
                    gc_sticks.Left,
                    Buttons.LeftThumbstickLeft,
                    Buttons.LeftThumbstickRight,
                    Buttons.LeftThumbstickUp,
                    Buttons.LeftThumbstickDown,
                    DeadZoneSize
                );
                gc_buttonState |= READ_StickToButtons(
                    gc_sticks.Right,
                    Buttons.RightThumbstickLeft,
                    Buttons.RightThumbstickRight,
                    Buttons.RightThumbstickUp,
                    Buttons.RightThumbstickDown,
                    DeadZoneSize
                );
                
                // Triggers
                GamePadTriggers gc_triggers = new GamePadTriggers(
                    (float) SDL.SDL_GameControllerGetAxis(device, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERLEFT) / 32768.0f,
                    (float) SDL.SDL_GameControllerGetAxis(device, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERRIGHT) / 32768.0f
                );
                gc_buttonState |= READ_TriggerToButton(
                    gc_triggers.Left,
                    Buttons.LeftTrigger,
                    DeadZoneSize
                );
                gc_buttonState |= READ_TriggerToButton(
                    gc_triggers.Right,
                    Buttons.RightTrigger,
                    DeadZoneSize
                );
                
                // Buttons
                if (SDL.SDL_GameControllerGetButton(device, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_A) != 0)
                {
                    gc_buttonState |= Buttons.A;
                }
                if (SDL.SDL_GameControllerGetButton(device, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_B) != 0)
                {
                    gc_buttonState |= Buttons.B;
                }
                if (SDL.SDL_GameControllerGetButton(device, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_X) != 0)
                {
                    gc_buttonState |= Buttons.X;
                }
                if (SDL.SDL_GameControllerGetButton(device, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_Y) != 0)
                {
                    gc_buttonState |= Buttons.Y;
                }
                if (SDL.SDL_GameControllerGetButton(device, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_BACK) != 0)
                {
                    gc_buttonState |= Buttons.Back;
                }
                if (SDL.SDL_GameControllerGetButton(device, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_GUIDE) != 0)
                {
                    gc_buttonState |= Buttons.BigButton;
                }
                if (SDL.SDL_GameControllerGetButton(device, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_START) != 0)
                {
                    gc_buttonState |= Buttons.Start;
                }
                if (SDL.SDL_GameControllerGetButton(device, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSTICK) != 0)
                {
                    gc_buttonState |= Buttons.LeftStick;
                }
                if (SDL.SDL_GameControllerGetButton(device, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSTICK) != 0)
                {
                    gc_buttonState |= Buttons.RightStick;
                }
                if (SDL.SDL_GameControllerGetButton(device, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSHOULDER) != 0)
                {
                    gc_buttonState |= Buttons.LeftShoulder;
                }
                if (SDL.SDL_GameControllerGetButton(device, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSHOULDER) != 0)
                {
                    gc_buttonState |= Buttons.RightShoulder;
                }
                
                // DPad
                GamePadDPad gc_dpad;
                if (SDL.SDL_GameControllerGetButton(device, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_UP) != 0)
                {
                    gc_buttonState |= Buttons.DPadUp;
                }
                if (SDL.SDL_GameControllerGetButton(device, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_DOWN) != 0)
                {
                    gc_buttonState |= Buttons.DPadDown;
                }
                if (SDL.SDL_GameControllerGetButton(device, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_LEFT) != 0)
                {
                    gc_buttonState |= Buttons.DPadLeft;
                }
                if (SDL.SDL_GameControllerGetButton(device, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_RIGHT) != 0)
                {
                    gc_buttonState |= Buttons.DPadRight;
                }
                gc_dpad = new GamePadDPad(gc_buttonState);
                
                // Compile the master buttonstate
                GamePadButtons gc_buttons = new GamePadButtons(gc_buttonState);
                
                return new GamePadState(
                    gc_sticks,
                    gc_triggers,
                    gc_buttons,
                    gc_dpad
                );
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
            if (SDL.SDL_IsGameController((int) playerIndex) == SDL.SDL_bool.SDL_TRUE)
            {
                // An SDL_GameController will _always_ be feature-complete.
                return new GamePadCapabilities()
                {
                    IsConnected = INTERNAL_devices[(int) playerIndex] != IntPtr.Zero,
                    HasAButton = true,
                    HasBButton = true,
                    HasXButton = true,
                    HasYButton = true,
                    HasBackButton = true,
                    HasStartButton = true,
                    HasDPadDownButton = true,
                    HasDPadLeftButton = true,
                    HasDPadRightButton = true,
                    HasDPadUpButton = true,
                    HasLeftShoulderButton = true,
                    HasRightShoulderButton = true,
                    HasLeftStickButton = true,
                    HasRightStickButton = true,
                    HasLeftTrigger = true,
                    HasRightTrigger = true,
                    HasLeftXThumbStick = true,
                    HasLeftYThumbStick = true,
                    HasRightXThumbStick = true,
                    HasRightYThumbStick = true,
                    HasBigButton = true,
                    HasLeftVibrationMotor = INTERNAL_haptics[(int) playerIndex] != IntPtr.Zero,
                    HasRightVibrationMotor = INTERNAL_haptics[(int) playerIndex] != IntPtr.Zero,
                    HasVoiceSupport = false
                };
            }
            
            return new GamePadCapabilities();
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
            if (!enabled)
            {
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
        public static void SetVibration(PlayerIndex playerIndex, float leftMotor, float rightMotor)
        {
            IntPtr haptic = INTERNAL_haptics[(int) playerIndex];
            HapticType type = INTERNAL_hapticTypes[(int) playerIndex];

            if (haptic == IntPtr.Zero)
            {
                return;
            }

            if (leftMotor <= 0.0f && rightMotor <= 0.0f)
            {
                SDL.SDL_HapticStopAll(haptic);
            }
            else if (type == HapticType.LeftRight)
            {
                INTERNAL_leftRightEffect.leftright.large_magnitude = (ushort) (65535.0f * leftMotor);
                INTERNAL_leftRightEffect.leftright.small_magnitude = (ushort) (65535.0f * rightMotor);
                SDL.SDL_HapticUpdateEffect(
                    haptic,
                    0,
                    ref INTERNAL_leftRightEffect
                );
                SDL.SDL_HapticRunEffect(
                    haptic,
                    0,
                    1
                );
            }
            else if (type == HapticType.LeftRightMacHack)
            {
                leftRightMacHackData[0] = (ushort) (65535.0f * leftMotor);
                leftRightMacHackData[1] = (ushort) (65535.0f * rightMotor);
                SDL.SDL_HapticUpdateEffect(
                    haptic,
                    0,
                    ref INTERNAL_leftRightMacHackEffect
                );
                SDL.SDL_HapticRunEffect(
                    haptic,
                    0,
                    1
                );
            }
            else
            {
                SDL.SDL_HapticRumblePlay(
                    haptic,
                    Math.Max(leftMotor, rightMotor),
                    SDL.SDL_HAPTIC_INFINITY // Oh dear...
                );
            }
        }

        public static void SetLightBarEXT(PlayerIndex playerIndex, Color color)
        {
            if (String.IsNullOrEmpty(INTERNAL_lightBars[(int) playerIndex]))
            {
                return;
            }

            string baseDir = INTERNAL_lightBars[(int) playerIndex];
            try
            {
                File.WriteAllText(baseDir + "red/brightness", color.R.ToString());
                File.WriteAllText(baseDir + "green/brightness", color.G.ToString());
                File.WriteAllText(baseDir + "blue/brightness", color.B.ToString());
            }
            catch
            {
                // If something went wrong, assume the worst and just remove it.
                INTERNAL_lightBars[(int) playerIndex] = String.Empty;
            }
        }
    }
}
