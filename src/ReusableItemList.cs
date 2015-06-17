#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2015 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */

/* Derived from code by the Mono.Xna Team (Copyright 2006).
 * Released under the MIT License. See monoxna.LICENSE for details.
 */
#endregion

#region Using Statements
using System;
using System.Collections.Generic;
#endregion

namespace Microsoft.Xna.Framework
{
	internal class ReusableItemList<T> : ICollection<T>, IEnumerator<T>
	{
		#region Public ICollection<T> Properties

		public T this[int index]
		{
			get
			{
				if (index >= _listTop)
				{
					throw new IndexOutOfRangeException();
				}
				return _list[index];
			}
			set
			{
				if (index >= _listTop)
				{
					throw new IndexOutOfRangeException();
				}
				_list[index] = value;
			}
		}

		public int Count
		{
			get
			{
				return _listTop;
			}
		}

		public bool IsReadOnly
		{
			get
			{
				return false;
			}
		}

		#endregion

		#region Public IEnumerator<T> Properties

		public T Current
		{
			get
			{
				return _list[_iteratorIndex];
			}
		}

		#endregion

		#region Private IEnumerator Properties

		object System.Collections.IEnumerator.Current
		{
			get
			{
				return _list[_iteratorIndex];
			}
		}

		#endregion

		#region Private Variables

		private readonly List<T> _list = new List<T>();
		private int _listTop = 0;
		private int _iteratorIndex;

		#endregion

		#region Public ICollection<T> Methods

		public void Add(T item)
		{
			if (_list.Count > _listTop)
			{
				_list[_listTop] = item;
			}
			else
			{
				_list.Add(item);
			}

			_listTop += 1;
		}

		public void Sort(IComparer<T> comparison)
		{
			_list.Sort(comparison);
		}


		public T GetNewItem()
		{
			if (_listTop < _list.Count)
			{
				return _list[_listTop++];
			}
			else
			{
				/* FIXME: Mono fails at this:
				 * return (T) Activator.CreateInstance(typeof(T));
				 */
				return default(T);
			}
		}

		public void Clear()
		{
			_listTop = 0;
		}

		public void Reset()
		{
			Clear();
			_list.Clear();
		}

		public bool Contains(T item)
		{
			return _list.Contains(item);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			_list.CopyTo(array,arrayIndex);
		}

		public bool Remove(T item)
		{
			throw new NotSupportedException();
		}

		#endregion

		#region Public IEnumerable<T> Methods

		public IEnumerator<T> GetEnumerator()
		{
			_iteratorIndex = -1;
			return this;
		}

		#endregion

		#region Public IEnumerator Methods

		public bool MoveNext()
		{
			_iteratorIndex += 1;
			return (_iteratorIndex < _listTop);
		}

		#endregion

		#region Public IDisposable Methods

		public void Dispose()
		{
		}

		#endregion

		#region Private IEnumerable Methods

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			_iteratorIndex = -1;
			return this;
		}

		#endregion
	}
}
