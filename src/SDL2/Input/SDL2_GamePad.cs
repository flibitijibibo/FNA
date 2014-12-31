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
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Serialization;

using SDL2;
#endregion

namespace Microsoft.Xna.Framework.Input
{
	#region Internal Configuration Enumerations/Structures

	// FIXME: Basically everything in here is public when it shouldn't be.

	public enum InputType
	{
		PovUp =		1,
		PovRight =	1 << 1,
		PovDown =	1 << 2,
		PovLeft =	1 << 3,
		Button =	1 << 4,
		Axis =		1 << 5,
		None =		-1
	}

	[Serializable]
	public class MonoGameJoystickValue
	{
		public InputType INPUT_TYPE = InputType.None;
		public int INPUT_ID = -1;
		public bool INPUT_INVERT = false;
	}

	[Serializable]
	public class MonoGameJoystickConfig
	{
		// public MonoGameJoystickValue BUTTON_GUIDE = new MonoGameJoystickValue();
		public MonoGameJoystickValue BUTTON_START = new MonoGameJoystickValue();
		public MonoGameJoystickValue BUTTON_BACK = new MonoGameJoystickValue();
		public MonoGameJoystickValue BUTTON_A = new MonoGameJoystickValue();
		public MonoGameJoystickValue BUTTON_B = new MonoGameJoystickValue();
		public MonoGameJoystickValue BUTTON_X = new MonoGameJoystickValue();
		public MonoGameJoystickValue BUTTON_Y = new MonoGameJoystickValue();
		public MonoGameJoystickValue SHOULDER_LB = new MonoGameJoystickValue();
		public MonoGameJoystickValue SHOULDER_RB = new MonoGameJoystickValue();
		public MonoGameJoystickValue TRIGGER_RT = new MonoGameJoystickValue();
		public MonoGameJoystickValue TRIGGER_LT = new MonoGameJoystickValue();
		public MonoGameJoystickValue BUTTON_LSTICK = new MonoGameJoystickValue();
		public MonoGameJoystickValue BUTTON_RSTICK = new MonoGameJoystickValue();
		public MonoGameJoystickValue DPAD_UP = new MonoGameJoystickValue();
		public MonoGameJoystickValue DPAD_DOWN = new MonoGameJoystickValue();
		public MonoGameJoystickValue DPAD_LEFT = new MonoGameJoystickValue();
		public MonoGameJoystickValue DPAD_RIGHT = new MonoGameJoystickValue();
		public MonoGameJoystickValue AXIS_LX = new MonoGameJoystickValue();
		public MonoGameJoystickValue AXIS_LY = new MonoGameJoystickValue();
		public MonoGameJoystickValue AXIS_RX = new MonoGameJoystickValue();
		public MonoGameJoystickValue AXIS_RY = new MonoGameJoystickValue();
	}

	#endregion

	public static class GamePad
	{
		#region Internal Haptic Type Enum

		private enum HapticType
		{
			Simple = 0,
			LeftRight = 1,
			LeftRightMacHack = 2
		}

		#endregion

		#region Internal SDL2_GamePad Variables

		// Controller device information
		private static IntPtr[] INTERNAL_devices = new IntPtr[4];
		private static bool[] INTERNAL_isGameController = new bool[4];
		private static Dictionary<int, int> INTERNAL_instanceList = new Dictionary<int, int>();
		private static string[] INTERNAL_guids = new string[]
		{
			String.Empty, String.Empty, String.Empty, String.Empty
		};

		// Haptic device information
		private static IntPtr[] INTERNAL_haptics = new IntPtr[4];
		private static HapticType[] INTERNAL_hapticTypes = new HapticType[4];

		// Light bar information
		private static string[] INTERNAL_lightBars = new string[]
		{
			String.Empty, String.Empty, String.Empty, String.Empty
		};

		// Cached GamePadStates
		private static GamePadState[] INTERNAL_states = new GamePadState[4];

		// We use this to apply XInput-like rumble effects.
		private static SDL.SDL_HapticEffect INTERNAL_leftRightEffect = new SDL.SDL_HapticEffect
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
		private static ushort[] leftRightMacHackData = {0, 0};
		private static GCHandle leftRightMacHackPArry = GCHandle.Alloc(leftRightMacHackData, GCHandleType.Pinned);
		private static IntPtr leftRightMacHackPtr = leftRightMacHackPArry.AddrOfPinnedObject();
		private static SDL.SDL_HapticEffect INTERNAL_leftRightMacHackEffect = new SDL.SDL_HapticEffect
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

		// Where we will load our config file into.
		private static MonoGameJoystickConfig INTERNAL_joystickConfig;

		// Used as a "blank" state
		private static GamePadState InitializedState = new GamePadState();

		#endregion

		#region Device List, Open/Close Devices

		internal static void INTERNAL_AddInstance(int which)
		{
			if (which > 3)
			{
				return; // Ignoring more than 4 controllers.
			}

			// Clear the error buffer. We're about to do a LOT of dangerous stuff.
			SDL.SDL_ClearError();

			// We use this when dealing with Haptic/GUID initialization.
			IntPtr thisJoystick;

			// Initialize either a GameController or a Joystick.
			INTERNAL_isGameController[which] = SDL.SDL_IsGameController(which) == SDL.SDL_bool.SDL_TRUE;
			if (INTERNAL_isGameController[which])
			{
				INTERNAL_devices[which] = SDL.SDL_GameControllerOpen(which);
				thisJoystick = SDL.SDL_GameControllerGetJoystick(INTERNAL_devices[which]);
			}
			else
			{
				INTERNAL_devices[which] = SDL.SDL_JoystickOpen(which);
				thisJoystick = INTERNAL_devices[which];
			}

			// Pair up the instance ID to the player index.
			INTERNAL_instanceList.Add(SDL.SDL_JoystickInstanceID(thisJoystick), which);

			// Start with a fresh state.
			INTERNAL_states[which] = InitializedState;

			// Initialize the haptics for the joystick, if applicable.
			if (SDL.SDL_JoystickIsHaptic(thisJoystick) == 1)
			{
				INTERNAL_haptics[which] = SDL.SDL_HapticOpenFromJoystick(thisJoystick);
				if (INTERNAL_haptics[which] == IntPtr.Zero)
				{
					System.Console.WriteLine("HAPTIC OPEN ERROR: " + SDL.SDL_GetError());
				}
			}
			if (INTERNAL_haptics[which] != IntPtr.Zero)
			{
				if (	Game.Instance.Platform.OSVersion.Equals("Mac OS X") &&
					SDL.SDL_HapticEffectSupported(INTERNAL_haptics[which], ref INTERNAL_leftRightMacHackEffect) == 1	)
				{
					INTERNAL_hapticTypes[which] = HapticType.LeftRightMacHack;
					SDL.SDL_HapticNewEffect(INTERNAL_haptics[which], ref INTERNAL_leftRightMacHackEffect);
				}
				else if (	!Game.Instance.Platform.OSVersion.Equals("Mac OS X") &&
						SDL.SDL_HapticEffectSupported(INTERNAL_haptics[which], ref INTERNAL_leftRightEffect) == 1	)
				{
					INTERNAL_hapticTypes[which] = HapticType.LeftRight;
					SDL.SDL_HapticNewEffect(INTERNAL_haptics[which], ref INTERNAL_leftRightEffect);
				}
				else if (SDL.SDL_HapticRumbleSupported(INTERNAL_haptics[which]) == 1)
				{
					INTERNAL_hapticTypes[which] = HapticType.Simple;
					SDL.SDL_HapticRumbleInit(INTERNAL_haptics[which]);
				}
				else
				{
					// We can't even play simple rumble, this haptic device is useless to us.
					SDL.SDL_HapticClose(INTERNAL_haptics[which]);
					INTERNAL_haptics[which] = IntPtr.Zero;
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
			if (Game.Instance.Platform.OSVersion.Equals("Linux"))
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
			else if (Game.Instance.Platform.OSVersion.Equals("Mac OS X"))
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
			else if (Game.Instance.Platform.OSVersion.Equals("Windows"))
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
			INTERNAL_guids[which] = result.ToString();

			// Initialize light bar
			if (	Game.Instance.Platform.OSVersion.Equals("Linux") &&
				INTERNAL_guids[which].Equals("4c05c405")	)
			{
				// Get all of the individual PS4 LED instances
				List<string> ledList = new List<string>();
				string[] dirs = Directory.GetDirectories("/sys/class/leds/");
				foreach (string dir in dirs)
				{
					if (	dir.Contains("054C:05C4") &&
						dir.EndsWith("blue")	)
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
					INTERNAL_lightBars[which] = ledList[numLights];
				}
			}

			// Check for an SDL_GameController configuration first!
			if (INTERNAL_isGameController[which])
			{
				System.Console.WriteLine(
					"Controller " + which.ToString() + ", " +
					SDL.SDL_GameControllerName(INTERNAL_devices[which]) +
					", will use SDL_GameController support."
				);
			}
			else
			{
				System.Console.WriteLine(
					"Controller " + which.ToString() + ", " +
					SDL.SDL_JoystickName(INTERNAL_devices[which]) +
					", will use generic MonoGameJoystick support."
				);
			}
		}

		internal static void INTERNAL_RemoveInstance(int which)
		{
			int output;
			if (!INTERNAL_instanceList.TryGetValue(which, out output))
			{
				// Odds are, this is controller 5+ getting removed.
				return;
			}
			INTERNAL_instanceList.Remove(which);
			if (INTERNAL_haptics[output] != IntPtr.Zero)
			{
				SDL.SDL_HapticClose(INTERNAL_haptics[output]);
				INTERNAL_haptics[output] = IntPtr.Zero;
			}
			if (INTERNAL_isGameController[output])
			{
				// Not no mores, it ain't.
				INTERNAL_isGameController[output] = false;
				SDL.SDL_GameControllerClose(INTERNAL_devices[output]);
			}
			else
			{
				SDL.SDL_JoystickClose(INTERNAL_devices[output]);
			}
			INTERNAL_devices[output] = IntPtr.Zero;
			INTERNAL_states[output] = InitializedState;
			INTERNAL_guids[output] = String.Empty;

			// A lot of errors can happen here, but honestly, they can be ignored...
			SDL.SDL_ClearError();

			System.Console.WriteLine("Removed device, player: " + output.ToString());
		}

		#endregion

		#region MonoGameJoystick.cfg Load Method

		// Prepare the MonoGameJoystick configuration system
		internal static void INTERNAL_InitMonoGameJoystick()
		{
			// Get the intended config file path.
			string osConfigFile = String.Empty;
			if (Game.Instance.Platform.OSVersion.Equals("Windows"))
			{
				osConfigFile = "MonoGameJoystick.cfg"; // Oh well.
			}
			else if (Game.Instance.Platform.OSVersion.Equals("Mac OS X"))
			{
				osConfigFile += Environment.GetEnvironmentVariable("HOME");
				if (osConfigFile.Length == 0)
				{
					// Home wasn't found, so just try file in current directory.
					osConfigFile = "MonoGameJoystick.cfg";
				}
				else
				{
					osConfigFile += "/Library/Application Support/MonoGame/MonoGameJoystick.cfg";
				}
			}
			else if (Game.Instance.Platform.OSVersion.Equals("Linux"))
			{
				// Assuming a non-OSX Unix platform will follow the XDG. Which it should.
				osConfigFile += Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
				if (osConfigFile.Length == 0)
				{
					osConfigFile += Environment.GetEnvironmentVariable("HOME");
					if (osConfigFile.Length == 0)
					{
						// Home wasn't found, so just try file in current directory.
						osConfigFile = "MonoGameJoystick.cfg";
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
				throw new Exception("SDL2_GamePad: Platform.OSVersion not handled!");
			}

			// Check to see if we've already got a config...
			if (File.Exists(osConfigFile))
			{
				using (FileStream fileIn = File.OpenRead(osConfigFile))
				{
					// Load the data into our config struct.
					XmlSerializer serializer = new XmlSerializer(typeof(MonoGameJoystickConfig));
					INTERNAL_joystickConfig = (MonoGameJoystickConfig) serializer.Deserialize(fileIn);
				}
			}
			else
			{
				// Since it doesn't exist, we need to generate the default config.
				INTERNAL_joystickConfig = new MonoGameJoystickConfig();

				// ... but is our directory even there?
				string osConfigDir = osConfigFile.Substring(0, osConfigFile.IndexOf("MonoGameJoystick.cfg"));
				if (!String.IsNullOrEmpty(osConfigDir) && !Directory.Exists(osConfigDir))
				{
					// Okay, jeez, we're really starting fresh.
					Directory.CreateDirectory(osConfigDir);
				}

				// Now, create the file.
				using (FileStream fileOut = File.Open(osConfigFile, FileMode.OpenOrCreate))
				{
					XmlSerializer serializer = new XmlSerializer(typeof(MonoGameJoystickConfig));
					serializer.Serialize(fileOut, INTERNAL_joystickConfig);
				}
			}
		}

		#endregion

		#region SDL_Joystick-to-Value Helper Methods

		private static bool READTYPE_ReadBool(MonoGameJoystickValue input, IntPtr device, short deadZone)
		{
			if (input.INPUT_TYPE == InputType.Axis)
			{
				short axis = SDL.SDL_JoystickGetAxis(device, input.INPUT_ID);
				if (input.INPUT_INVERT)
				{
					return (axis < -deadZone);
				}
				return (axis > deadZone);
			}
			else if (input.INPUT_TYPE == InputType.Button)
			{
				return ((SDL.SDL_JoystickGetButton(device, input.INPUT_ID) > 0) ^ input.INPUT_INVERT);
			}
			else if (	input.INPUT_TYPE == InputType.PovUp ||
					input.INPUT_TYPE == InputType.PovDown ||
					input.INPUT_TYPE == InputType.PovLeft ||
					input.INPUT_TYPE == InputType.PovRight	)
			{
				return (((SDL.SDL_JoystickGetHat(device, input.INPUT_ID) & (byte) input.INPUT_TYPE) > 0) ^ input.INPUT_INVERT);
			}
			return false;
		}

		private static float READTYPE_ReadFloat(MonoGameJoystickValue input, IntPtr device)
		{
			float inputMask = input.INPUT_INVERT ? -1.0f : 1.0f;
			if (input.INPUT_TYPE == InputType.Axis)
			{
				// SDL2 clamps to 32767 on both sides.
				float maxRange = input.INPUT_INVERT ? -32767.0f : 32767.0f;
				return ((float) SDL.SDL_JoystickGetAxis(device, input.INPUT_ID)) / maxRange;
			}
			else if (input.INPUT_TYPE == InputType.Button)
			{
				return SDL.SDL_JoystickGetButton(device, input.INPUT_ID) * inputMask;
			}
			else if (	input.INPUT_TYPE == InputType.PovUp ||
					input.INPUT_TYPE == InputType.PovDown ||
					input.INPUT_TYPE == InputType.PovLeft ||
					input.INPUT_TYPE == InputType.PovRight	)
			{
				return (SDL.SDL_JoystickGetHat(device, input.INPUT_ID) & (byte) input.INPUT_TYPE) * inputMask;
			}
			return 0.0f;
		}

		#endregion

		#region Value-To-Input Helper Methods

		// Button reader for GetState
		private static Buttons READ_ReadButtons(IntPtr device, float deadZoneSize)
		{
			short DeadZone = (short) (deadZoneSize * short.MaxValue);
			Buttons b = (Buttons) 0;

			// A B X Y
			if (READTYPE_ReadBool(INTERNAL_joystickConfig.BUTTON_A, device, DeadZone))
			{
				b |= Buttons.A;
			}
			if (READTYPE_ReadBool(INTERNAL_joystickConfig.BUTTON_B, device, DeadZone))
			{
				b |= Buttons.B;
			}
			if (READTYPE_ReadBool(INTERNAL_joystickConfig.BUTTON_X, device, DeadZone))
			{
				b |= Buttons.X;
			}
			if (READTYPE_ReadBool(INTERNAL_joystickConfig.BUTTON_Y, device, DeadZone))
			{
				b |= Buttons.Y;
			}

			// Shoulder buttons
			if (READTYPE_ReadBool(INTERNAL_joystickConfig.SHOULDER_LB, device, DeadZone))
			{
				b |= Buttons.LeftShoulder;
			}
			if (READTYPE_ReadBool(INTERNAL_joystickConfig.SHOULDER_RB, device, DeadZone))
			{
				b |= Buttons.RightShoulder;
			}

			// Back/Start
			if (READTYPE_ReadBool(INTERNAL_joystickConfig.BUTTON_BACK, device, DeadZone))
			{
				b |= Buttons.Back;
			}
			if (READTYPE_ReadBool(INTERNAL_joystickConfig.BUTTON_START, device, DeadZone))
			{
				b |= Buttons.Start;
			}

			// Stick buttons
			if (READTYPE_ReadBool(INTERNAL_joystickConfig.BUTTON_LSTICK, device, DeadZone))
			{
				b |= Buttons.LeftStick;
			}
			if (READTYPE_ReadBool(INTERNAL_joystickConfig.BUTTON_RSTICK, device, DeadZone))
			{
				b |= Buttons.RightStick;
			}

			// DPad
			if (READTYPE_ReadBool(INTERNAL_joystickConfig.DPAD_UP, device, DeadZone))
			{
				b |= Buttons.DPadUp;
			}
			if (READTYPE_ReadBool(INTERNAL_joystickConfig.DPAD_DOWN, device, DeadZone))
			{
				b |= Buttons.DPadDown;
			}
			if (READTYPE_ReadBool(INTERNAL_joystickConfig.DPAD_LEFT, device, DeadZone))
			{
				b |= Buttons.DPadLeft;
			}
			if (READTYPE_ReadBool(INTERNAL_joystickConfig.DPAD_RIGHT, device, DeadZone))
			{
				b |= Buttons.DPadRight;
			}

			return b;
		}

		// GetState can convert stick values to button values
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

		// GetState can convert trigger values to button values
		private static Buttons READ_TriggerToButton(float trigger, Buttons button, float DeadZoneSize)
		{
			Buttons b = (Buttons) 0;

			if (trigger > DeadZoneSize)
			{
				b |= button;
			}

			return b;
		}

		#endregion

		#region Public GamePad API

		public static GamePadCapabilities GetCapabilities(PlayerIndex playerIndex)
		{
			// SDL_GameController Capabilities

			if (INTERNAL_isGameController[(int) playerIndex])
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

			// SDL_Joystick Capabilities

			IntPtr d = INTERNAL_devices[(int) playerIndex];

			if (d == IntPtr.Zero)
			{
				return new GamePadCapabilities();
			}

			return new GamePadCapabilities()
			{
				IsConnected = true,

				HasAButton =			INTERNAL_joystickConfig.BUTTON_A.INPUT_TYPE		!= InputType.None,
				HasBButton =			INTERNAL_joystickConfig.BUTTON_B.INPUT_TYPE		!= InputType.None,
				HasXButton =			INTERNAL_joystickConfig.BUTTON_X.INPUT_TYPE		!= InputType.None,
				HasYButton =			INTERNAL_joystickConfig.BUTTON_Y.INPUT_TYPE		!= InputType.None,
				HasBackButton =			INTERNAL_joystickConfig.BUTTON_BACK.INPUT_TYPE		!= InputType.None,
				HasStartButton =		INTERNAL_joystickConfig.BUTTON_START.INPUT_TYPE		!= InputType.None,
				HasDPadDownButton =		INTERNAL_joystickConfig.DPAD_DOWN.INPUT_TYPE		!= InputType.None,
				HasDPadLeftButton =		INTERNAL_joystickConfig.DPAD_LEFT.INPUT_TYPE		!= InputType.None,
				HasDPadRightButton =		INTERNAL_joystickConfig.DPAD_RIGHT.INPUT_TYPE		!= InputType.None,
				HasDPadUpButton =		INTERNAL_joystickConfig.DPAD_UP.INPUT_TYPE		!= InputType.None,
				HasLeftShoulderButton =		INTERNAL_joystickConfig.SHOULDER_LB.INPUT_TYPE		!= InputType.None,
				HasRightShoulderButton =	INTERNAL_joystickConfig.SHOULDER_RB.INPUT_TYPE		!= InputType.None,
				HasLeftStickButton =		INTERNAL_joystickConfig.BUTTON_LSTICK.INPUT_TYPE	!= InputType.None,
				HasRightStickButton =		INTERNAL_joystickConfig.BUTTON_RSTICK.INPUT_TYPE	!= InputType.None,
				HasLeftTrigger =		INTERNAL_joystickConfig.TRIGGER_LT.INPUT_TYPE		!= InputType.None,
				HasRightTrigger =		INTERNAL_joystickConfig.TRIGGER_RT.INPUT_TYPE		!= InputType.None,
				HasLeftXThumbStick =		INTERNAL_joystickConfig.AXIS_LX.INPUT_TYPE		!= InputType.None,
				HasLeftYThumbStick =		INTERNAL_joystickConfig.AXIS_LY.INPUT_TYPE		!= InputType.None,
				HasRightXThumbStick =		INTERNAL_joystickConfig.AXIS_RX.INPUT_TYPE		!= InputType.None,
				HasRightYThumbStick =		INTERNAL_joystickConfig.AXIS_RY.INPUT_TYPE		!= InputType.None,

				HasLeftVibrationMotor = INTERNAL_haptics[(int) playerIndex] != IntPtr.Zero,
				HasRightVibrationMotor = INTERNAL_haptics[(int) playerIndex] != IntPtr.Zero,
				HasVoiceSupport = false,
				HasBigButton = false
			};
		}

		public static GamePadState GetState(PlayerIndex playerIndex)
		{
			return GetState(playerIndex, GamePadDeadZone.IndependentAxes);
		}

		public static GamePadState GetState(PlayerIndex playerIndex, GamePadDeadZone deadZoneMode)
		{
			IntPtr device = INTERNAL_devices[(int) playerIndex];
			if (device == IntPtr.Zero)
			{
				return InitializedState;
			}

			// Do not attempt to understand this number at all costs!
			const float DeadZoneSize = 0.27f;

			// SDL_GameController
			if (INTERNAL_isGameController[(int) playerIndex])
			{
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
					),
					deadZoneMode
				);
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
					(float) SDL.SDL_GameControllerGetAxis(
						device,
						SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERLEFT
					) / 32768.0f,
					(float) SDL.SDL_GameControllerGetAxis(
						device,
						SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERRIGHT
					) / 32768.0f
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

				// Build the GamePadState, increment PacketNumber if state changed.
				GamePadState gc_builtState = new GamePadState(
					gc_sticks,
					gc_triggers,
					gc_buttons,
					gc_dpad
				);
				gc_builtState.PacketNumber = INTERNAL_states[(int) playerIndex].PacketNumber;
				if (gc_builtState != INTERNAL_states[(int) playerIndex])
				{
					gc_builtState.PacketNumber += 1;
					INTERNAL_states[(int) playerIndex] = gc_builtState;
				}

				return gc_builtState;
			}

			// SDL_Joystick

			// We will interpret the joystick values into this.
			Buttons buttonState = (Buttons) 0;

			// Sticks
			GamePadThumbSticks sticks = new GamePadThumbSticks(
				new Vector2(
					READTYPE_ReadFloat(INTERNAL_joystickConfig.AXIS_LX, device),
					-READTYPE_ReadFloat(INTERNAL_joystickConfig.AXIS_LY, device)
				),
				new Vector2(
					READTYPE_ReadFloat(INTERNAL_joystickConfig.AXIS_RX, device),
					-READTYPE_ReadFloat(INTERNAL_joystickConfig.AXIS_RY, device)
				),
				deadZoneMode
			);
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
			buttonState = READ_ReadButtons(device, DeadZoneSize);

			// Triggers
			GamePadTriggers triggers = new GamePadTriggers(
				READTYPE_ReadFloat(INTERNAL_joystickConfig.TRIGGER_LT, device),
				READTYPE_ReadFloat(INTERNAL_joystickConfig.TRIGGER_RT, device)
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

			// Build the GamePadState, increment PacketNumber if state changed.
			GamePadState builtState = new GamePadState(
				sticks,
				triggers,
				buttons,
				dpad
			);
			builtState.PacketNumber = INTERNAL_states[(int) playerIndex].PacketNumber;
			if (builtState != INTERNAL_states[(int) playerIndex])
			{
				builtState.PacketNumber += 1;
				INTERNAL_states[(int) playerIndex] = builtState;
			}

			return builtState;
		}

		public static bool SetVibration(PlayerIndex playerIndex, float leftMotor, float rightMotor)
		{
			IntPtr haptic = INTERNAL_haptics[(int) playerIndex];
			HapticType type = INTERNAL_hapticTypes[(int) playerIndex];

			if (haptic == IntPtr.Zero)
			{
				return false;
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
			return true;
		}

		#endregion

		#region Public GamePad API, FNA Extensions

		public static string GetGUIDEXT(PlayerIndex playerIndex)
		{
			return INTERNAL_guids[(int) playerIndex];
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

		#endregion
	}
}
