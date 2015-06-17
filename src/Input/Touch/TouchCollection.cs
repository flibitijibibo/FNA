#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2015 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

#region Using Statements
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
#endregion

namespace Microsoft.Xna.Framework.Input.Touch
{
	/// <summary>
	/// Provides state information for a touch screen enabled device.
	/// </summary>
	public struct TouchCollection : IList<TouchLocation>
	{
		#region Public Properties

		/// <summary>
		/// States if a touch screen is available.
		/// </summary>
		public bool IsConnected 
		{
			get
			{
				return TouchPanel.GetCapabilities().IsConnected;
			}
		}

		#endregion

		#region Public IList<TouchLocation> Properties

		/// <summary>
		/// States if touch collection is read only.
		/// </summary>
		public bool IsReadOnly
		{
			get
			{
				return true;
			}
		}

		/// <summary>
		/// Returns the number of <see cref="TouchLocation"/> items that exist in the
		/// collection.
		/// </summary>
		public int Count
		{
			get
			{
				return Collection.Length;
			}
		}

		/// <summary>
		/// Gets or sets the item at the specified index of the collection.
		/// </summary>
		/// <param name="index">Position of the item.</param>
		/// <returns><see cref="TouchLocation"/></returns>
		public TouchLocation this[int index]
		{
			get
			{
				return Collection[index];
			}
			set
			{
				throw new NotSupportedException();
			}
		}

		#endregion

		#region Private Properties

		private TouchLocation[] Collection
		{
			get
			{
				return collection ?? EmptyLocationArray;
			}
		}

		#endregion

		#region Internal Static Variables

		internal static readonly TouchCollection Empty = new TouchCollection(
			new TouchLocation[] {}
		);

		#endregion

		#region Private Variables

		private TouchLocation[] collection;

		#endregion

		#region Private Static Variables

		private static readonly TouchLocation[] EmptyLocationArray = new TouchLocation[0];

		#endregion

		#region Public Constructor

		/// <summary>
		/// Initializes a new instance of the <see cref="TouchCollection"/> with a
		/// pre-determined set of touch locations.
		/// </summary>
		/// <param name="touches">
		/// Array of <see cref="TouchLocation"/> items with which to initialize.
		/// </param>
		public TouchCollection(TouchLocation[] touches)
		{
			if (touches == null)
			{
				throw new ArgumentNullException("touches");
			}
			collection = touches;
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Returns <see cref="TouchLocation"/> specified by ID.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="touchLocation"></param>
		/// <returns></returns>
		public bool FindById(int id, out TouchLocation touchLocation)
		{
			foreach (TouchLocation location in Collection)
			{
				if (location.Id == id)
				{
					touchLocation = location;
					return true;
				}
			}

			touchLocation = default(TouchLocation);
			return false;
		}

		#endregion

		#region Public IList<TouchLocation> Methods

		/// <summary>
		/// Returns the index of the first occurrence of specified <see cref="TouchLocation"/>
		/// item in the collection.
		/// </summary>
		/// <param name="item"><see cref="TouchLocation"/> to query.</param>
		/// <returns></returns>
		public int IndexOf(TouchLocation item)
		{
			for (int i = 0; i < Collection.Length; i += 1)
			{
				if (item == Collection[i])
				{
					return i;
				}
			}

			return -1;
		}

		/// <summary>
		/// Inserts a <see cref="TouchLocation"/> item into the indicated position.
		/// </summary>
		/// <param name="index">The position to insert into.</param>
		/// <param name="item">The <see cref="TouchLocation"/> item to insert.</param>
		public void Insert(int index, TouchLocation item)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Removes the <see cref="TouchLocation"/> item at specified index.
		/// </summary>
		/// <param name="index">Index of the item that will be removed from collection.</param>
		public void RemoveAt(int index)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Adds a <see cref="TouchLocation"/> to the collection.
		/// </summary>
		/// <param name="item">The <see cref="TouchLocation"/> item to be added. </param>
		public void Add(TouchLocation item)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Clears all the items in collection.
		/// </summary>
		public void Clear()
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Returns true if specified <see cref="TouchLocation"/> item exists in the
		/// collection, false otherwise./>
		/// </summary>
		/// <param name="item">The <see cref="TouchLocation"/> item to query for.</param>
		/// <returns>Returns true if queried item is found, false otherwise.</returns>
		public bool Contains(TouchLocation item)
		{
			foreach (TouchLocation location in Collection)
			{
				if (item == location)
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Copies the <see cref="TouchLocation"/> collection to specified array starting
		/// from the given index.
		/// </summary>
		/// <param name="array">The array to copy <see cref="TouchLocation"/> items.</param>
		/// <param name="arrayIndex">The starting index of the copy operation.</param>
		public void CopyTo(TouchLocation[] array, int arrayIndex)
		{
			Collection.CopyTo(array, arrayIndex);
		}

		/// <summary>
		/// Removes the specified <see cref="TouchLocation"/> item from the collection.
		/// </summary>
		/// <param name="item">The <see cref="TouchLocation"/> item to remove.</param>
		/// <returns></returns>
		public bool Remove(TouchLocation item)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Returns an enumerator for the <see cref="TouchCollection"/>.
		/// </summary>
		/// <returns>Enumerable list of <see cref="TouchLocation"/> objects.</returns>
		public Enumerator GetEnumerator()
		{
			return new Enumerator(this);
		}

		#endregion

		#region Private IEnumerator Methods

		/// <summary>
		/// Returns an enumerator for the <see cref="TouchCollection"/>.
		/// </summary>
		/// <returns>Enumerable list of <see cref="TouchLocation"/> objects.</returns>
		IEnumerator<TouchLocation> IEnumerable<TouchLocation>.GetEnumerator()
		{
			return new Enumerator(this);
		}


		/// <summary>
		/// Returns an enumerator for the <see cref="TouchCollection"/>.
		/// </summary>
		/// <returns>Enumerable list of objects.</returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return new Enumerator(this);
		}

		#endregion

		#region Enumerator

		/// <summary>
		/// Provides the ability to iterate through the TouchLocations in an TouchCollection.
		/// </summary>
		public struct Enumerator : IEnumerator<TouchLocation>
		{
			private readonly TouchCollection collection;
			private int position;

			internal Enumerator(TouchCollection collection)
			{
				this.collection = collection;
				position = -1;
			}

			/// <summary>
			/// Gets the current element in the TouchCollection.
			/// </summary>
			public TouchLocation Current
			{
				get
				{
					return collection[position];
				}
			}

			/// <summary>
			/// Advances the enumerator to the next element of the TouchCollection.
			/// </summary>
			public bool MoveNext()
			{
				position += 1;
				return (position < collection.Count);
			}

			/// <summary>
			/// Immediately releases the unmanaged resources used by this object.
			/// </summary>
			public void Dispose()
			{
			}

			object IEnumerator.Current
			{
				get
				{
					return collection[position];
				}
			}

			public void Reset()
			{
				position = -1;
			}
		}

		#endregion
	}
}
