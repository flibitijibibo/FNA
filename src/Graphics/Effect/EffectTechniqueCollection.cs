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
	public sealed class EffectTechniqueCollection : IEnumerable<EffectTechnique>, IEnumerable
	{
		#region Public Properties

		public int Count
		{
			get
			{
				return elements.Length;
			}
		}

		public EffectTechnique this[int index]
		{
			get
			{
				return elements[index];
			}
		}

		public EffectTechnique this[string name]
		{
			get
			{
				foreach (EffectTechnique elem in elements)
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

		private EffectTechnique[] elements;

		#endregion

		#region Internal Constructor

		internal EffectTechniqueCollection(EffectTechnique[] value)
		{
			elements = value;
		}

		#endregion

		#region Public Methods

		public IEnumerator<EffectTechnique> GetEnumerator()
		{
			return ((IEnumerable<EffectTechnique>) elements).GetEnumerator();
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
