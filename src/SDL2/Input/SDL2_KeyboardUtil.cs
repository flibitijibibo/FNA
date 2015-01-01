#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

#region Using Statements
using System.Collections.Generic;

using SDL2;
#endregion

namespace Microsoft.Xna.Framework.Input
{
	internal static class SDL2_KeyboardUtil
	{
		#region Private SDL2->XNA Key Hashmap

		/* From: http://blogs.msdn.com/b/shawnhar/archive/2007/07/02/twin-paths-to-garbage-collector-nirvana.aspx
		 * "If you use an enum type as a dictionary key, internal dictionary operations will cause boxing.
		 * You can avoid this by using integer keys, and casting your enum values to ints before adding
		 * them to the dictionary."
		 */
		private static Dictionary<int, Keys> INTERNAL_map;

		#endregion

		#region Hashmap Initializer Constructor

		static SDL2_KeyboardUtil()
		{
			// Create the dictionary...
			INTERNAL_map = new Dictionary<int, Keys>();

			// Then fill it with known keys that match up to XNA Keys.
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_a,			Keys.A);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_b,			Keys.B);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_c,			Keys.C);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_d,			Keys.D);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_e,			Keys.E);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_f,			Keys.F);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_g,			Keys.G);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_h,			Keys.H);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_i,			Keys.I);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_j,			Keys.J);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_k,			Keys.K);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_l,			Keys.L);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_m,			Keys.M);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_n,			Keys.N);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_o,			Keys.O);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_p,			Keys.P);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_q,			Keys.Q);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_r,			Keys.R);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_s,			Keys.S);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_t,			Keys.T);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_u,			Keys.U);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_v,			Keys.V);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_w,			Keys.W);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_x,			Keys.X);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_y,			Keys.Y);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_z,			Keys.Z);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_0,			Keys.D0);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_1,			Keys.D1);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_2,			Keys.D2);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_3,			Keys.D3);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_4,			Keys.D4);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_5,			Keys.D5);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_6,			Keys.D6);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_7,			Keys.D7);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_8,			Keys.D8);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_9,			Keys.D9);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_KP_0,		Keys.NumPad0);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_KP_1,		Keys.NumPad1);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_KP_2,		Keys.NumPad2);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_KP_3,		Keys.NumPad3);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_KP_4,		Keys.NumPad4);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_KP_5,		Keys.NumPad5);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_KP_6,		Keys.NumPad6);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_KP_7,		Keys.NumPad7);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_KP_8,		Keys.NumPad8);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_KP_9,		Keys.NumPad9);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_KP_CLEAR,		Keys.OemClear);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_KP_DECIMAL,		Keys.Decimal);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_KP_DIVIDE,		Keys.Divide);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_KP_ENTER,		Keys.Enter);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_KP_MINUS,		Keys.OemMinus);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_KP_MULTIPLY,	Keys.Multiply);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_KP_PERIOD,		Keys.OemPeriod);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_KP_PLUS,		Keys.Add);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_F1,			Keys.F1);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_F2,			Keys.F2);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_F3,			Keys.F3);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_F4,			Keys.F4);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_F5,			Keys.F5);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_F6,			Keys.F6);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_F7,			Keys.F7);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_F8,			Keys.F8);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_F9,			Keys.F9);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_F10,		Keys.F10);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_F11,		Keys.F11);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_F12,		Keys.F12);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_F13,		Keys.F13);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_F14,		Keys.F14);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_F15,		Keys.F15);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_F16,		Keys.F16);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_F17,		Keys.F17);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_F18,		Keys.F18);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_F19,		Keys.F19);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_F20,		Keys.F20);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_F21,		Keys.F21);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_F22,		Keys.F22);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_F23,		Keys.F23);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_F24,		Keys.F24);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_SPACE,		Keys.Space);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_UP,			Keys.Up);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_DOWN,		Keys.Down);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_LEFT,		Keys.Left);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_RIGHT,		Keys.Right);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_LALT,		Keys.LeftAlt);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_RALT,		Keys.RightAlt);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_LCTRL,		Keys.LeftControl);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_RCTRL,		Keys.RightControl);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_LGUI,		Keys.LeftWindows);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_RGUI,		Keys.RightWindows);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_LSHIFT,		Keys.LeftShift);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_RSHIFT,		Keys.RightShift);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_APPLICATION,	Keys.Apps);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_SLASH,		Keys.OemQuestion);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_BACKSLASH,		Keys.OemBackslash);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_LEFTBRACKET,	Keys.OemOpenBrackets);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_RIGHTBRACKET,	Keys.OemCloseBrackets);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_CAPSLOCK,		Keys.CapsLock);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_COMMA,		Keys.OemComma);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_DELETE,		Keys.Delete);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_END,		Keys.End);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_BACKSPACE,		Keys.Back);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_RETURN,		Keys.Enter);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_ESCAPE,		Keys.Escape);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_HOME,		Keys.Home);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_INSERT,		Keys.Insert);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_MINUS,		Keys.OemMinus);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_NUMLOCKCLEAR,	Keys.NumLock);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_PAGEUP,		Keys.PageUp);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_PAGEDOWN,		Keys.PageDown);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_PAUSE,		Keys.Pause);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_PERIOD,		Keys.OemPeriod);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_EQUALS,		Keys.OemPlus);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_PRINTSCREEN,	Keys.PrintScreen);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_QUOTE,		Keys.OemQuotes);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_SCROLLLOCK,		Keys.Scroll);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_SEMICOLON,		Keys.OemSemicolon);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_SLEEP,		Keys.Sleep);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_TAB,		Keys.Tab);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_BACKQUOTE,		Keys.OemTilde);
			INTERNAL_map.Add((int) SDL.SDL_Keycode.SDLK_UNKNOWN,		Keys.None);
		}

		#endregion

		#region Public SDL2->XNA Key Conversion Method

		public static Keys ToXNA(SDL.SDL_Keycode key)
		{
			Keys retVal;
			if (INTERNAL_map.TryGetValue((int) key, out retVal))
			{
				return retVal;
			}
			else
			{
				System.Console.WriteLine("KEY MISSING FROM SDL2->XNA DICTIONARY: " + key.ToString());
				return Keys.None;
			}
		}

		#endregion
	}
}
