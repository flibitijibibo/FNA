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
#endregion

namespace Microsoft.Xna.Framework.Input
{
	public static class TextInputEXT
	{
		#region Event

		/// <summary>
		/// Use this event to retrieve text for objects like textboxes.
		/// This event is not raised by noncharacter keys.
		/// This event also supports key repeat.
		/// For more information this event is based off:
		/// http://msdn.microsoft.com/en-AU/library/system.windows.forms.control.keypress.aspx
		/// </summary>
		public static event EventHandler<TextInputEventArgs> TextInput;

		#endregion

		#region EventArgs

		/// <summary>
		/// This class is used for the TextInput event as EventArgs.
		/// </summary>
		public class TextInputEventArgs : EventArgs
		{
			public char Character
			{
				get;
				private set;
			}

			public TextInputEventArgs(char character)
			{
				Character = character;
			}
		}

		#endregion

		#region Internal Event Access Method

		internal static void OnTextInput(object sender, TextInputEventArgs e)
		{
			if (TextInput != null)
			{
				TextInput(sender, e);
			}
		}

		#endregion
	}
}
