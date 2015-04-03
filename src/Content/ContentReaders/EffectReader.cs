#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

#region Using Statements
using System.Linq;

using Microsoft.Xna.Framework.Graphics;
#endregion

namespace Microsoft.Xna.Framework.Content
{
	internal class EffectReader : ContentTypeReader<Effect>
	{
		#region Private Supported File Extensions Variable

		static string[] supportedExtensions = new string[] {".fxb" };

		#endregion

		#region Public Constructor

		public EffectReader()
		{
		}

		#endregion

		// FIXME: Shouldn't this be internal?
		#region Public Filename Normalizer Method

		public static string Normalize(string FileName)
		{
			return ContentTypeReader.Normalize(FileName, supportedExtensions);
		}

		#endregion

		// FIXME: Are these ever even used?
		#region Private Static Methods

		private static string TryFindAnyCased(
			string search,
			string[] arr,
			params string[] extensions
		) {
			return arr.FirstOrDefault(
				s => extensions.Any(
					ext => s.ToLowerInvariant() == (search.ToLowerInvariant() + ext)
				)
			);
		}

		private static bool Contains(string search, string[] arr)
		{
			return arr.Any(s => s == search);
		}

		#endregion

		#region Protected Read Method

		protected internal override Effect Read(
			ContentReader input,
			Effect existingInstance
		) {
			int length = input.ReadInt32();
			input.ReadInt32(); // 0xBCF00BCF
			int offset = input.ReadInt32();
			input.ReadBytes(offset - 8); // ???
			Effect effect = new Effect(
				input.GraphicsDevice,
				input.ReadBytes(length - offset)
			);
			effect.Name = input.AssetName;
			return effect;
		}

		#endregion
	}
}
