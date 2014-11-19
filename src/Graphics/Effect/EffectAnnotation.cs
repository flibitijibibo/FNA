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

namespace Microsoft.Xna.Framework.Graphics
{
	public sealed class EffectAnnotation
	{
		#region Public Properties

		public string Name
		{
			get;
			private set;
		}

		public string Semantic
		{
			get;
			private set;
		}

		public int RowCount
		{
			get;
			private set;
		}

		public int ColumnCount
		{
			get;
			private set;
		}

		public EffectParameterClass ParameterClass
		{
			get;
			private set;
		}

		public EffectParameterType ParameterType
		{
			get;
			private set;
		}

		#endregion

		#region Private Variables

		IntPtr values;

		#endregion

		#region Internal Constructor

		internal EffectAnnotation(
			string name,
			string semantic,
			int rowCount,
			int columnCount,
			EffectParameterClass parameterClass,
			EffectParameterType parameterType,
			IntPtr data
		) {
			Name = name;
			Semantic = semantic;
			RowCount = rowCount;
			ColumnCount = columnCount;
			ParameterClass = parameterClass;
			ParameterType = parameterType;
			values = data;
		}

		#endregion

		#region Public Methods

		public bool GetValueBoolean()
		{
			return false;
		}

		public int GetValueInt32()
		{
			return 0;
		}

		public Matrix GetValueMatrix()
		{
			return Matrix.Identity;
		}

		public float GetValueSingle()
		{
			return 0.0f;
		}

		public string GetValueString()
		{
			return "TODO";
		}

		public Vector2 GetValueVector2()
		{
			return Vector2.Zero;
		}

		public Vector3 GetValueVector3()
		{
			return Vector3.Zero;
		}

		public Vector4 GetValueVector4()
		{
			return Vector4.Zero;
		}

		#endregion
	}
}
