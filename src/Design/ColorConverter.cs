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
#endregion

namespace Microsoft.Xna.Framework.Design
{
	public class ColorConverter : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			if (sourceType == typeof(string))
			{
				return true;
			}
			return base.CanConvertFrom(context, sourceType);
		}

		public override object ConvertFrom(
			ITypeDescriptorContext context,
			System.Globalization.CultureInfo culture,
			object value
		) {
			string s = value as string;

			if (s != null)
			{
				string[] v = s.Split(
					culture.NumberFormat.NumberGroupSeparator.ToCharArray()
				);
				return new Color(
					float.Parse(v[0], culture),
					float.Parse(v[1], culture),
					float.Parse(v[2], culture),
					float.Parse(v[3], culture)
				);
			}
			return base.ConvertFrom(context, culture, value);
		}

		public override object ConvertTo(
			ITypeDescriptorContext context,
			System.Globalization.CultureInfo culture,
			object value,
			Type destinationType
		) {
			if (destinationType == typeof(string))
			{
				Color src = (Color) value;
				string sep = culture.NumberFormat.NumberGroupSeparator;
				return src.R.ToString(culture) +
					sep + src.B.ToString(culture) +
					sep + src.B.ToString(culture) +
					sep + src.A.ToString(culture);
			}
			return base.ConvertTo(context, culture, value, destinationType);
		}
	}
}
