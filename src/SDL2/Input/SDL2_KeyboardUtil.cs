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
		#region Private SDL2->XNA Key Hashmaps

		/* From: http://blogs.msdn.com/b/shawnhar/archive/2007/07/02/twin-paths-to-garbage-collector-nirvana.aspx
		 * "If you use an enum type as a dictionary key, internal dictionary operations will cause boxing.
		 * You can avoid this by using integer keys, and casting your enum values to ints before adding
		 * them to the dictionary."
		 */
		private static Dictionary<int, Keys> INTERNAL_keyMap;
		private static Dictionary<int, Keys> INTERNAL_scanMap;

		#endregion

		#region Hashmap Initializer Constructor

		static SDL2_KeyboardUtil()
		{
			// Create the dictionaries...
			INTERNAL_keyMap = new Dictionary<int, Keys>();
			INTERNAL_scanMap = new Dictionary<int, Keys>();

			// Then fill them with known keys that match up to XNA Keys.

			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_a,		Keys.A);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_b,		Keys.B);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_c,		Keys.C);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_d,		Keys.D);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_e,		Keys.E);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_f,		Keys.F);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_g,		Keys.G);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_h,		Keys.H);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_i,		Keys.I);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_j,		Keys.J);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_k,		Keys.K);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_l,		Keys.L);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_m,		Keys.M);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_n,		Keys.N);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_o,		Keys.O);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_p,		Keys.P);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_q,		Keys.Q);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_r,		Keys.R);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_s,		Keys.S);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_t,		Keys.T);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_u,		Keys.U);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_v,		Keys.V);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_w,		Keys.W);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_x,		Keys.X);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_y,		Keys.Y);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_z,		Keys.Z);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_0,		Keys.D0);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_1,		Keys.D1);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_2,		Keys.D2);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_3,		Keys.D3);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_4,		Keys.D4);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_5,		Keys.D5);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_6,		Keys.D6);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_7,		Keys.D7);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_8,		Keys.D8);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_9,		Keys.D9);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_KP_0,		Keys.NumPad0);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_KP_1,		Keys.NumPad1);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_KP_2,		Keys.NumPad2);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_KP_3,		Keys.NumPad3);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_KP_4,		Keys.NumPad4);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_KP_5,		Keys.NumPad5);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_KP_6,		Keys.NumPad6);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_KP_7,		Keys.NumPad7);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_KP_8,		Keys.NumPad8);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_KP_9,		Keys.NumPad9);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_KP_CLEAR,	Keys.OemClear);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_KP_DECIMAL,	Keys.Decimal);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_KP_DIVIDE,	Keys.Divide);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_KP_ENTER,	Keys.Enter);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_KP_MINUS,	Keys.OemMinus);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_KP_MULTIPLY,	Keys.Multiply);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_KP_PERIOD,	Keys.OemPeriod);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_KP_PLUS,		Keys.Add);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_F1,		Keys.F1);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_F2,		Keys.F2);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_F3,		Keys.F3);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_F4,		Keys.F4);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_F5,		Keys.F5);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_F6,		Keys.F6);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_F7,		Keys.F7);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_F8,		Keys.F8);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_F9,		Keys.F9);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_F10,		Keys.F10);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_F11,		Keys.F11);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_F12,		Keys.F12);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_F13,		Keys.F13);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_F14,		Keys.F14);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_F15,		Keys.F15);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_F16,		Keys.F16);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_F17,		Keys.F17);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_F18,		Keys.F18);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_F19,		Keys.F19);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_F20,		Keys.F20);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_F21,		Keys.F21);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_F22,		Keys.F22);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_F23,		Keys.F23);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_F24,		Keys.F24);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_SPACE,		Keys.Space);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_UP,		Keys.Up);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_DOWN,		Keys.Down);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_LEFT,		Keys.Left);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_RIGHT,		Keys.Right);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_LALT,		Keys.LeftAlt);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_RALT,		Keys.RightAlt);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_LCTRL,		Keys.LeftControl);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_RCTRL,		Keys.RightControl);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_LGUI,		Keys.LeftWindows);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_RGUI,		Keys.RightWindows);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_LSHIFT,		Keys.LeftShift);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_RSHIFT,		Keys.RightShift);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_APPLICATION,	Keys.Apps);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_SLASH,		Keys.OemQuestion);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_BACKSLASH,	Keys.OemBackslash);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_LEFTBRACKET,	Keys.OemOpenBrackets);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_RIGHTBRACKET,	Keys.OemCloseBrackets);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_CAPSLOCK,	Keys.CapsLock);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_COMMA,		Keys.OemComma);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_DELETE,		Keys.Delete);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_END,		Keys.End);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_BACKSPACE,	Keys.Back);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_RETURN,		Keys.Enter);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_ESCAPE,		Keys.Escape);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_HOME,		Keys.Home);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_INSERT,		Keys.Insert);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_MINUS,		Keys.OemMinus);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_NUMLOCKCLEAR,	Keys.NumLock);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_PAGEUP,		Keys.PageUp);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_PAGEDOWN,	Keys.PageDown);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_PAUSE,		Keys.Pause);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_PERIOD,		Keys.OemPeriod);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_EQUALS,		Keys.OemPlus);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_PRINTSCREEN,	Keys.PrintScreen);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_QUOTE,		Keys.OemQuotes);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_SCROLLLOCK,	Keys.Scroll);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_SEMICOLON,	Keys.OemSemicolon);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_SLEEP,		Keys.Sleep);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_TAB,		Keys.Tab);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_BACKQUOTE,	Keys.OemTilde);
			INTERNAL_keyMap.Add((int) SDL.SDL_Keycode.SDLK_UNKNOWN,		Keys.None);

			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_A,		Keys.A);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_B,		Keys.B);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_C,		Keys.C);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_D,		Keys.D);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_E,		Keys.E);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_F,		Keys.F);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_G,		Keys.G);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_H,		Keys.H);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_I,		Keys.I);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_J,		Keys.J);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_K,		Keys.K);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_L,		Keys.L);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_M,		Keys.M);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_N,		Keys.N);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_O,		Keys.O);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_P,		Keys.P);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_Q,		Keys.Q);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_R,		Keys.R);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_S,		Keys.S);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_T,		Keys.T);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_U,		Keys.U);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_V,		Keys.V);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_W,		Keys.W);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_X,		Keys.X);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_Y,		Keys.Y);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_Z,		Keys.Z);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_0,		Keys.D0);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_1,		Keys.D1);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_2,		Keys.D2);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_3,		Keys.D3);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_4,		Keys.D4);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_5,		Keys.D5);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_6,		Keys.D6);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_7,		Keys.D7);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_8,		Keys.D8);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_9,		Keys.D9);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_KP_0,		Keys.NumPad0);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_KP_1,		Keys.NumPad1);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_KP_2,		Keys.NumPad2);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_KP_3,		Keys.NumPad3);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_KP_4,		Keys.NumPad4);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_KP_5,		Keys.NumPad5);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_KP_6,		Keys.NumPad6);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_KP_7,		Keys.NumPad7);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_KP_8,		Keys.NumPad8);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_KP_9,		Keys.NumPad9);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_KP_CLEAR,	Keys.OemClear);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_KP_DECIMAL,	Keys.Decimal);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_KP_DIVIDE,	Keys.Divide);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_KP_ENTER,	Keys.Enter);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_KP_MINUS,	Keys.OemMinus);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_KP_MULTIPLY,	Keys.Multiply);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_KP_PERIOD,	Keys.OemPeriod);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_KP_PLUS,	Keys.Add);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_F1,		Keys.F1);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_F2,		Keys.F2);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_F3,		Keys.F3);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_F4,		Keys.F4);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_F5,		Keys.F5);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_F6,		Keys.F6);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_F7,		Keys.F7);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_F8,		Keys.F8);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_F9,		Keys.F9);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_F10,		Keys.F10);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_F11,		Keys.F11);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_F12,		Keys.F12);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_F13,		Keys.F13);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_F14,		Keys.F14);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_F15,		Keys.F15);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_F16,		Keys.F16);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_F17,		Keys.F17);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_F18,		Keys.F18);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_F19,		Keys.F19);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_F20,		Keys.F20);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_F21,		Keys.F21);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_F22,		Keys.F22);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_F23,		Keys.F23);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_F24,		Keys.F24);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_SPACE,		Keys.Space);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_UP,		Keys.Up);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_DOWN,		Keys.Down);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_LEFT,		Keys.Left);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_RIGHT,		Keys.Right);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_LALT,		Keys.LeftAlt);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_RALT,		Keys.RightAlt);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_LCTRL,		Keys.LeftControl);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_RCTRL,		Keys.RightControl);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_LGUI,		Keys.LeftWindows);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_RGUI,		Keys.RightWindows);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_LSHIFT,	Keys.LeftShift);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_RSHIFT,	Keys.RightShift);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_APPLICATION,	Keys.Apps);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_SLASH,		Keys.OemQuestion);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_BACKSLASH,	Keys.OemBackslash);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_LEFTBRACKET,	Keys.OemOpenBrackets);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_RIGHTBRACKET,	Keys.OemCloseBrackets);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_CAPSLOCK,	Keys.CapsLock);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_COMMA,		Keys.OemComma);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_DELETE,	Keys.Delete);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_END,		Keys.End);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_BACKSPACE,	Keys.Back);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_RETURN,	Keys.Enter);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_ESCAPE,	Keys.Escape);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_HOME,		Keys.Home);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_INSERT,	Keys.Insert);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_MINUS,		Keys.OemMinus);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_NUMLOCKCLEAR,	Keys.NumLock);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_PAGEUP,	Keys.PageUp);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_PAGEDOWN,	Keys.PageDown);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_PAUSE,		Keys.Pause);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_PERIOD,	Keys.OemPeriod);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_EQUALS,	Keys.OemPlus);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_PRINTSCREEN,	Keys.PrintScreen);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_APOSTROPHE,	Keys.OemQuotes);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_SCROLLLOCK,	Keys.Scroll);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_SEMICOLON,	Keys.OemSemicolon);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_SLEEP,		Keys.Sleep);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_TAB,		Keys.Tab);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_GRAVE,		Keys.OemTilde);
			INTERNAL_scanMap.Add((int) SDL.SDL_Scancode.SDL_SCANCODE_UNKNOWN,	Keys.None);
		}

		#endregion

		#region Public SDL2->XNA Key Conversion Methods

		public static Keys ToXNA(SDL.SDL_Keycode key)
		{
			Keys retVal;
			if (INTERNAL_keyMap.TryGetValue((int) key, out retVal))
			{
				return retVal;
			}
			else
			{
				System.Console.WriteLine("KEY MISSING FROM SDL2->XNA DICTIONARY: " + key.ToString());
				return Keys.None;
			}
		}

		public static Keys ToXNA(SDL.SDL_Scancode key)
		{
			Keys retVal;
			if (INTERNAL_scanMap.TryGetValue((int) key, out retVal))
			{
				return retVal;
			}
			else
			{
				System.Console.WriteLine("SCANCODE MISSING FROM SDL2->XNA DICTIONARY: " + key.ToString());
				return Keys.None;
			}
		}

		#endregion
	}
}
