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
using System.Reflection;
#endregion

namespace Microsoft.Xna.Framework.Content
{
	internal class ReflectiveReader<T> : ContentTypeReader
	{
		#region Reader Delegates

		delegate void ReadElement(ContentReader input, object parent);

		private List<ReadElement> readers;

		#endregion

		#region Private Variables

		private ConstructorInfo constructor;

		private ContentTypeReader baseTypeReader;

		#endregion

		#region Internal Constructor

		internal ReflectiveReader() : base(typeof(T))
		{
		}

		#endregion

		#region Protected ContentTypeReader Methods

		protected internal override void Initialize(ContentTypeReaderManager manager)
		{
			base.Initialize(manager);

			Type baseType = TargetType.BaseType;
			if (baseType != null && baseType != typeof(object))
			{
				baseTypeReader = manager.GetTypeReader(baseType);
			}

			constructor = TargetType.GetDefaultConstructor();

			PropertyInfo[] properties = TargetType.GetAllProperties();
			FieldInfo[] fields = TargetType.GetAllFields();
			readers = new List<ReadElement>(fields.Length + properties.Length);

			// Gather the properties.
			foreach (PropertyInfo property in properties)
			{
				ReadElement read = GetElementReader(manager, property);
				if (read != null)
				{
					readers.Add(read);
				}
			}

			// Gather the fields.
			foreach (FieldInfo field in fields)
			{
				ReadElement read = GetElementReader(manager, field);
				if (read != null)
				{
					readers.Add(read);
				}
			}
		}

		protected internal override object Read(ContentReader input, object existingInstance)
		{
			T obj;
			if (existingInstance != null)
			{
				obj = (T) existingInstance;
			}
			else
			{
				if (constructor == null)
				{
					obj = (T) Activator.CreateInstance(typeof(T));
				}
				else
				{
					obj = (T) constructor.Invoke(null);
				}
			}
		
			if (baseTypeReader != null)
			{
				baseTypeReader.Read(input, obj);
			}

			// Box the type.
			object  boxed = (object) obj;

			foreach (ReadElement reader in readers)
			{
				reader(input, boxed);
			}

			// Unbox it... required for value types.
			obj = (T) boxed;

			return obj;
		}

		#endregion

		#region Private Static Methods

		private static ReadElement GetElementReader(
			ContentTypeReaderManager manager,
			MemberInfo member
		) {
			PropertyInfo property = member as PropertyInfo;
			FieldInfo field = member as FieldInfo;

			// Properties must have public get and set
			if (	property != null &&
				(	property.CanWrite == false ||
					property.CanRead == false	)	)
			{
				return null;
			}

			if (property != null)
			{
				// Properties must have at least a getter.
				if (property.CanRead == false)
				{
					return null;
				}

				// Skip over indexer properties
				if (property.Name == "Item")
				{
					MethodInfo getMethod = property.GetGetMethod();
					MethodInfo setMethod = property.GetSetMethod();

					if (	(getMethod != null && getMethod.GetParameters().Length > 0) ||
						(setMethod != null && setMethod.GetParameters().Length > 0)	)	
					{
						/* This is presumably a property like this[indexer] and this
						 * should not get involved in the object deserialization
						 */
						return null;
					}
				}
			}

			// Are we explicitly asked to ignore this item?
			Attribute attr = Attribute.GetCustomAttribute(
				member,
				typeof(ContentSerializerIgnoreAttribute)
			);
			if (attr != null)
			{
				return null;
			}

			ContentSerializerAttribute contentSerializerAttribute = Attribute.GetCustomAttribute(
				member,
				typeof(ContentSerializerAttribute)
			) as ContentSerializerAttribute;
			if (contentSerializerAttribute == null)
			{
				if (property != null)
				{
					/* There is no ContentSerializerAttribute, so non-public
					 * properties cannot be deserialized.
					 */
					MethodInfo getMethod = property.GetGetMethod();
					if (getMethod == null || !getMethod.IsPublic)
					{
						return null;
					}
					MethodInfo setMethod = property.GetSetMethod();
					if (setMethod == null || !setMethod.IsPublic)
					{
						return null;
					}

					/* If the read-only property has a type reader then
					 * it is safe to deserialize into the existing type.
					 */
					if (	!property.CanWrite &&
						manager.GetTypeReader(property.PropertyType) == null	)
					{
						return null;
					}
				}
				else
				{
					/* There is no ContentSerializerAttribute, so non-public
					 * fields cannot be deserialized.
					 */
					if (!field.IsPublic)
					{
						return null;
					}

					// evolutional: Added check to skip initialise only fields
					if (field.IsInitOnly)
					{
						return null;
					}
				}
			}

			Action<object, object> setter;
			Type elementType;
			if (property != null)
			{
				elementType = property.PropertyType;
				if (property.CanWrite)
				{
					setter = (o, v) => property.SetValue(o, v, null);
				}
				else
				{
					setter = (o, v) => { };
				}
			}
			else
			{
				elementType = field.FieldType;
				setter = field.SetValue;
			}

			if (	contentSerializerAttribute != null &&
				contentSerializerAttribute.SharedResource	)
			{
				return (input, parent) =>
				{
					Action<object> action = value => setter(parent, value);
					input.ReadSharedResource(action);
				};
			}

			// We need to have a reader at this point.
			ContentTypeReader reader = manager.GetTypeReader(elementType);
			if (reader == null)
			{
				throw new ContentLoadException(string.Format(
					"Content reader could not be found for {0} type.",
					elementType.FullName
				));
			}

			/* We use the construct delegate to pick the correct existing
			 * object to be the target of deserialization.
			 */
			Func<object, object> construct = parent => null;
			if (property != null && !property.CanWrite)
			{
				construct = parent => property.GetValue(parent, null);
			}

			return (input, parent) =>
			{
				object existing = construct(parent);
				object obj2 = input.ReadObject(reader, existing);
				setter(parent, obj2);
			};
		}

		#endregion
	}
}
