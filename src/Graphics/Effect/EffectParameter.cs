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
	public sealed class EffectParameter
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

		public EffectParameterCollection StructureMembers
		{
			get;
			private set;
		}

		public EffectAnnotationCollection Annotations
		{
			get;
			private set;
		}

		#endregion

		#region Private Variables

		private IntPtr values;

		#endregion

		#region Internal Constructor

		internal EffectParameter(
			string name,
			string semantic,
			int rowCount,
			int columnCount,
			EffectParameterClass parameterClass,
			EffectParameterType parameterType,
			EffectParameterCollection structureMembers,
			EffectAnnotationCollection annotations,
			IntPtr data
		) {
			Name = name;
			Semantic = semantic;
			RowCount = rowCount;
			ColumnCount = columnCount;
			ParameterClass = parameterClass;
			ParameterType = parameterType;
			StructureMembers = structureMembers;
			Annotations = annotations;
			values = data;
		}

		#endregion

		#region Public Get Methods

		public bool GetValueBoolean()
		{
			return false;
		}

		public bool[] GetValueBooleanArray(int count)
		{
			return null;
		}

		public int GetValueInt32()
		{
			return 0;
		}

		public int[] GetValueInt32Array(int count)
		{
			return null;
		}

		public Matrix GetValueMatrix()
		{
			return Matrix.Identity;
		}

		public Matrix[] GetValueMatrixArray(int count)
		{
			return null;
		}

		public Matrix GetValueMatrixTranspose()
		{
			return Matrix.Identity;
		}

		public Matrix[] GetValueMatrixTransposeArray(int count)
		{
			return null;
		}

		public Quaternion GetValueQuaternion()
		{
			return new Quaternion();
		}

		public Quaternion[] GetValueQuaternionArray(int count)
		{
			return null;
		}

		public float GetValueSingle()
		{
			return 0.0f;
		}

		public float[] GetValueSingleArray(int count)
		{
			return null;
		}

		public string GetValueString()
		{
			return "TODO";
		}

		public Texture2D GetValueTexture2D()
		{
			return null;
		}

		public Texture3D GetValueTexture3D()
		{
			return null;
		}

		public TextureCube GetValueTextureCube()
		{
			return null;
		}

		public Vector2 GetValueVector2()
		{
			return Vector2.Zero;
		}

		public Vector2[] GetValueVector2Array(int count)
		{
			return null;
		}

		public Vector3 GetValueVector3()
		{
			return Vector3.Zero;
		}

		public Vector3[] GetValueVector3Array(int count)
		{
			return null;
		}

		public Vector4 GetValueVector4()
		{
			return Vector4.Zero;
		}

		public Vector4[] GetValueVector4Array(int count)
		{
			return null;
		}

		#endregion

		#region Public Set Methods

		public void SetValue(bool value)
		{
		}

		public void SetValue(bool[] value)
		{
		}

		public void SetValue(int value)
		{
		}

		public void SetValue(int[] value)
		{
		}

		public void SetValue(Matrix value)
		{
		}

		public void SetValue(Matrix[] value)
		{
		}

		public void SetValueTranspose(Matrix value)
		{
		}

		public void SetValueTranspose(Matrix[] value)
		{
		}

		public void SetValue(Quaternion value)
		{
		}

		public void SetValue(Quaternion[] value)
		{
		}

		public void SetValue(float value)
		{
		}

		public void SetValue(float[] value)
		{
		}

		public void SetValue(string value)
		{
		}

		public void SetValue(Texture value)
		{
		}

		public void SetValue(Vector2 value)
		{
		}

		public void SetValue(Vector2[] value)
		{
		}

		public void SetValue(Vector3 value)
		{
		}

		public void SetValue(Vector3[] value)
		{
		}

		public void SetValue(Vector4 value)
		{
		}

		public void SetValue(Vector4[] value)
		{
		}

		#endregion
	}
}
