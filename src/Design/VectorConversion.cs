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
using System.ComponentModel;
using System.Globalization;
using Microsoft.Xna.Framework.Graphics.PackedVector;
#endregion

namespace Microsoft.Xna.Framework.Design
{
	internal static class VectorConversion
	{
		public static bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			if (destinationType == typeof(float))
			{
				return true;
			}
			if (destinationType == typeof(Vector2))
			{
				return true;
			}
			if (destinationType == typeof(Vector3))
			{
				return true;
			}
			if (destinationType == typeof(Vector4))
			{
				return true;
			}
			if (destinationType.GetInterface("IPackedVector") != null)
			{
				return true;
			}

			return false;
		}

		public static object ConvertToFromVector4(ITypeDescriptorContext context, CultureInfo culture, Vector4 value, Type destinationType)
		{
			if (destinationType == typeof(float))
			{
				return value.X;
			}
			if (destinationType == typeof(Vector2))
			{
				return new Vector2(value.X, value.Y);
			}
			if (destinationType == typeof(Vector3))
			{
				return new Vector3(value.X, value.Y, value.Z);
			}
			if (destinationType == typeof(Vector4))
			{
				return new Vector4(value.X, value.Y, value.Z, value.W);
			}
			if (destinationType.GetInterface("IPackedVector") != null)
			{
				IPackedVector packedVec = (IPackedVector) Activator.CreateInstance(destinationType);
				packedVec.PackFromVector4(value);
				return packedVec;
			}

			return null;
		}
	}
}
