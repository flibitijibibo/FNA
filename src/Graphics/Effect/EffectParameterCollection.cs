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
	public sealed class EffectParameterCollection : IEnumerable<EffectParameter>, IEnumerable
	{
		#region Public Properties

		public int Count
		{
			get
			{
				return elements.Length;
			}
		}

		public EffectParameter this[int index]
		{
			get
			{
				return elements[index];
			}
		}

		public EffectParameter this[string name]
		{
			get
			{
				foreach (EffectParameter elem in elements)
				{
					if (name.Equals(elem.Name))
					{
						return elem;
					}
				}
				return null; // FIXME: ArrayIndexOutOfBounds? -flibit
			}
		}

		#endregion

		#region Private Variables

		private EffectParameter[] elements;

		#endregion

		#region Internal Constructor

		internal EffectParameterCollection(EffectParameter[] value)
		{
			elements = value;
		}

		#endregion

		#region Public Methods

		public IEnumerator<EffectParameter> GetEnumerator()
		{
			return ((IEnumerable<EffectParameter>) elements).GetEnumerator();
		}

		public EffectParameter GetParameterBySemantic(string semantic)
		{
			foreach (EffectParameter elem in elements)
			{
				if (semantic.Equals(elem.Semantic))
				{
					return elem;
				}
			}
			return null;
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
