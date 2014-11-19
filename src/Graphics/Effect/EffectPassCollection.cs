#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

#region Using Statements
using System.Collections;
using System.Collections.Generic;
#endregion

namespace Microsoft.Xna.Framework.Graphics
{
	public sealed class EffectPassCollection : IEnumerable<EffectPass>, IEnumerable
	{
		#region Public Properties

		public int Count
		{
			get
			{
				return elements.Length;
			}
		}

		public EffectPass this[int index]
		{
			get
			{
				return elements[index];
			}
		}

		public EffectPass this[string name]
		{
			get
			{
				foreach (EffectPass elem in elements)
				{
					if (name.Equals(elem.Name))
					{
						return elem;
					}
				}
				return null; // FIXME: ArrayIndexOutOfBounds?
			}
		}

		#endregion

		#region Private Variables

		private EffectPass[] elements;

		#endregion

		#region Internal Constructor

		internal EffectPassCollection(EffectPass[] value)
		{
			elements = value;
		}

		#endregion

		#region Public Methods

		public IEnumerator<EffectPass> GetEnumerator()
		{
			return ((IEnumerable<EffectPass>) elements).GetEnumerator();
		}

		#endregion

		#region IEnumerator Methods

		IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return elements.GetEnumerator();
		}

		#endregion
	}
}
