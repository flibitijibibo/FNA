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
using System.Diagnostics;
#endregion

namespace Microsoft.Xna.Framework
{
	[Serializable]
	[DebuggerDisplay("{DebugDisplayString,nq}")]
	public struct Quaternion : IEquatable<Quaternion>
	{
		#region Public Static Properties

		public static Quaternion Identity
		{
			get
			{
				return identity;
			}
		}

		#endregion

		#region Internal Properties

		internal Vector3 Xyz
		{
			get
			{
				return new Vector3(X, Y, Z);
			}
			set
			{
				X = value.X;
				Y = value.Y;
				Z = value.Z;
			}
		}

		internal string DebugDisplayString
		{
			get
			{
				if (this == Quaternion.Identity)
				{
					return "Identity";
				}

				return string.Concat(
					X.ToString(), " ",
					Y.ToString(), " ",
					Z.ToString(), " ",
					W.ToString()
				);
			}
		}

		#endregion

		#region Public Fields

		public float X;
		public float Y;
		public float Z;
		public float W;

		#endregion

		#region Private Static Variables

		private static Quaternion identity = new Quaternion(0, 0, 0, 1);

		#endregion

		#region Public Constructors

		public Quaternion(float x, float y, float z, float w)
		{
			this.X = x;
			this.Y = y;
			this.Z = z;
			this.W = w;
		}


		public Quaternion(Vector3 vectorPart, float scalarPart)
		{
			this.X = vectorPart.X;
			this.Y = vectorPart.Y;
			this.Z = vectorPart.Z;
			this.W = scalarPart;
		}

		#endregion

		#region Public Methods

		public void Conjugate()
		{
			this.X = -this.X;
			this.Y = -this.Y;
			this.Z = -this.Z;
		}

		public override bool Equals(object obj)
		{
			return (obj is Quaternion) && Equals((Quaternion) obj);
		}

		public bool Equals(Quaternion other)
		{
			return (	(MathHelper.WithinEpsilon(this.X, other.X)) &&
					(MathHelper.WithinEpsilon(this.Y, other.Y)) &&
					(MathHelper.WithinEpsilon(this.Z, other.Z)) &&
					(MathHelper.WithinEpsilon(this.W, other.W))	);
		}

		public override int GetHashCode()
		{
			return (
				this.X.GetHashCode() +
				this.Y.GetHashCode() +
				this.Z.GetHashCode() +
				this.W.GetHashCode()
			);
		}

		public float Length()
		{
			float num = (
				(this.X * this.X) +
				(this.Y * this.Y) +
				(this.Z * this.Z) +
				(this.W * this.W)
			);
			return (float) Math.Sqrt((double) num);
		}

		public float LengthSquared()
		{
			return (
				(this.X * this.X) +
				(this.Y * this.Y) +
				(this.Z * this.Z) +
				(this.W * this.W)
			);
		}

		public void Normalize()
		{
			float num2 = (
				(this.X * this.X) +
				(this.Y * this.Y) +
				(this.Z * this.Z) +
				(this.W * this.W)
			);
			float num = 1f / ((float) Math.Sqrt((double) num2));
			this.X *= num;
			this.Y *= num;
			this.Z *= num;
			this.W *= num;
		}

		public override string ToString()
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder(32);
			sb.Append("{X:");
			sb.Append(this.X);
			sb.Append(" Y:");
			sb.Append(this.Y);
			sb.Append(" Z:");
			sb.Append(this.Z);
			sb.Append(" W:");
			sb.Append(this.W);
			sb.Append("}");
			return sb.ToString();
		}

		#endregion

		#region Internal Methods

		internal Matrix ToMatrix()
		{
			Matrix matrix = Matrix.Identity;
			ToMatrix(out matrix);
			return matrix;
		}

		internal void ToMatrix(out Matrix matrix)
		{
			Quaternion.ToMatrix(this, out matrix);
		}

		#endregion

		#region Public Static Methods

		public static Quaternion Add(Quaternion quaternion1, Quaternion quaternion2)
		{
			Quaternion quaternion;
			Add(ref quaternion1, ref quaternion2, out quaternion);
			return quaternion;
		}

		public static void Add(
			ref Quaternion quaternion1,
			ref Quaternion quaternion2,
			out Quaternion result
		) {
			result.X = quaternion1.X + quaternion2.X;
			result.Y = quaternion1.Y + quaternion2.Y;
			result.Z = quaternion1.Z + quaternion2.Z;
			result.W = quaternion1.W + quaternion2.W;
		}

		public static Quaternion Concatenate(Quaternion value1, Quaternion value2)
		{
			Quaternion quaternion;
			Concatenate(ref value1, ref value2, out quaternion);
			return quaternion;
		}

		public static void Concatenate(
			ref Quaternion value1,
			ref Quaternion value2,
			out Quaternion result
		) {
			float x = value2.X;
			float y = value2.Y;
			float z = value2.Z;
			float w = value2.W;
			float num4 = value1.X;
			float num3 = value1.Y;
			float num2 = value1.Z;
			float num = value1.W;
			float num12 = (y * num2) - (z * num3);
			float num11 = (z * num4) - (x * num2);
			float num10 = (x * num3) - (y * num4);
			float num9 = ((x * num4) + (y * num3)) + (z * num2);
			result.X = ((x * num) + (num4 * w)) + num12;
			result.Y = ((y * num) + (num3 * w)) + num11;
			result.Z = ((z * num) + (num2 * w)) + num10;
			result.W = (w * num) - num9;
		}

		public static Quaternion Conjugate(Quaternion value)
		{
			Quaternion quaternion;
			Conjugate(ref value, out quaternion);
			return quaternion;
		}

		public static void Conjugate(ref Quaternion value, out Quaternion result)
		{
			result.X = -value.X;
			result.Y = -value.Y;
			result.Z = -value.Z;
			result.W = value.W;
		}

		public static Quaternion CreateFromAxisAngle(Vector3 axis, float angle)
		{
			Quaternion quaternion;
			CreateFromAxisAngle(ref axis, angle, out quaternion);
			return quaternion;
		}

		public static void CreateFromAxisAngle(
			ref Vector3 axis,
			float angle,
			out Quaternion result
		) {
			float num2 = angle * 0.5f;
			float num = (float) Math.Sin((double) num2);
			float num3 = (float) Math.Cos((double) num2);
			result.X = axis.X * num;
			result.Y = axis.Y * num;
			result.Z = axis.Z * num;
			result.W = num3;
		}

		public static Quaternion CreateFromRotationMatrix(Matrix matrix)
		{
			Quaternion quaternion;
			CreateFromRotationMatrix(ref matrix, out quaternion);
			return quaternion;
		}

		public static void CreateFromRotationMatrix(ref Matrix matrix, out Quaternion result)
		{
			float num8 = (matrix.M11 + matrix.M22) + matrix.M33;
			if (num8 > 0f)
			{
				float num = (float) Math.Sqrt((double) (num8 + 1f));
				result.W = num * 0.5f;
				num = 0.5f / num;
				result.X = (matrix.M23 - matrix.M32) * num;
				result.Y = (matrix.M31 - matrix.M13) * num;
				result.Z = (matrix.M12 - matrix.M21) * num;
			}
			else if ((matrix.M11 >= matrix.M22) && (matrix.M11 >= matrix.M33))
			{
				float num7 = (float) Math.Sqrt((double) (
					((1f + matrix.M11) - matrix.M22) - matrix.M33
				));
				float num4 = 0.5f / num7;
				result.X = 0.5f * num7;
				result.Y = (matrix.M12 + matrix.M21) * num4;
				result.Z = (matrix.M13 + matrix.M31) * num4;
				result.W = (matrix.M23 - matrix.M32) * num4;
			}
			else if (matrix.M22 > matrix.M33)
			{
				float num6 = (float) Math.Sqrt((double) (
					((1f + matrix.M22) - matrix.M11) - matrix.M33
				));
				float num3 = 0.5f / num6;
				result.X = (matrix.M21 + matrix.M12) * num3;
				result.Y = 0.5f * num6;
				result.Z = (matrix.M32 + matrix.M23) * num3;
				result.W = (matrix.M31 - matrix.M13) * num3;
			}
			else
			{
				float num5 = (float) Math.Sqrt((double) (
					((1f + matrix.M33) - matrix.M11) - matrix.M22
				));
				float num2 = 0.5f / num5;
				result.X = (matrix.M31 + matrix.M13) * num2;
				result.Y = (matrix.M32 + matrix.M23) * num2;
				result.Z = 0.5f * num5;
				result.W = (matrix.M12 - matrix.M21) * num2;
			}
		}

		public static Quaternion CreateFromYawPitchRoll(float yaw, float pitch, float roll)
		{
			Quaternion quaternion;
			CreateFromYawPitchRoll(yaw, pitch, roll, out quaternion);
			return quaternion;
		}

		public static void CreateFromYawPitchRoll(
			float yaw,
			float pitch,
			float roll,
			out Quaternion result)
		{
			float num9 = roll * 0.5f;
			float num6 = (float) Math.Sin((double) num9);
			float num5 = (float) Math.Cos((double) num9);
			float num8 = pitch * 0.5f;
			float num4 = (float) Math.Sin((double) num8);
			float num3 = (float) Math.Cos((double) num8);
			float num7 = yaw * 0.5f;
			float num2 = (float) Math.Sin((double) num7);
			float num = (float) Math.Cos((double) num7);
			result.X = ((num * num4) * num5) + ((num2 * num3) * num6);
			result.Y = ((num2 * num3) * num5) - ((num * num4) * num6);
			result.Z = ((num * num3) * num6) - ((num2 * num4) * num5);
			result.W = ((num * num3) * num5) + ((num2 * num4) * num6);
		}

		public static Quaternion Divide(Quaternion quaternion1, Quaternion quaternion2)
		{
			Quaternion quaternion;
			Divide(ref quaternion1, ref quaternion2, out quaternion);
			return quaternion;
		}

		public static void Divide(
			ref Quaternion quaternion1,
			ref Quaternion quaternion2,
			out Quaternion result
		) {
			float x = quaternion1.X;
			float y = quaternion1.Y;
			float z = quaternion1.Z;
			float w = quaternion1.W;
			float num14 = (
				(quaternion2.X * quaternion2.X) +
				(quaternion2.Y * quaternion2.Y) +
				(quaternion2.Z * quaternion2.Z) +
				(quaternion2.W * quaternion2.W)
			);
			float num5 = 1f / num14;
			float num4 = -quaternion2.X * num5;
			float num3 = -quaternion2.Y * num5;
			float num2 = -quaternion2.Z * num5;
			float num = quaternion2.W * num5;
			float num13 = (y * num2) - (z * num3);
			float num12 = (z * num4) - (x * num2);
			float num11 = (x * num3) - (y * num4);
			float num10 = ((x * num4) + (y * num3)) + (z * num2);
			result.X = ((x * num) + (num4 * w)) + num13;
			result.Y = ((y * num) + (num3 * w)) + num12;
			result.Z = ((z * num) + (num2 * w)) + num11;
			result.W = (w * num) - num10;
		}


		public static float Dot(Quaternion quaternion1, Quaternion quaternion2)
		{
			return (
				(quaternion1.X * quaternion2.X) +
				(quaternion1.Y * quaternion2.Y) +
				(quaternion1.Z * quaternion2.Z) +
				(quaternion1.W * quaternion2.W)
			);
		}


		public static void Dot(
			ref Quaternion quaternion1,
			ref Quaternion quaternion2,
			out float result
		) {
			result = (
				(quaternion1.X * quaternion2.X) +
				(quaternion1.Y * quaternion2.Y) +
				(quaternion1.Z * quaternion2.Z) +
				(quaternion1.W * quaternion2.W)
			);
		}

		public static Quaternion Inverse(Quaternion quaternion)
		{
			Quaternion inverse;
			Inverse(ref quaternion, out inverse);
			return inverse;
		}

		public static void Inverse(ref Quaternion quaternion, out Quaternion result)
		{
			float num2 = (
				(quaternion.X * quaternion.X) +
				(quaternion.Y * quaternion.Y) +
				(quaternion.Z * quaternion.Z) +
				(quaternion.W * quaternion.W)
			);
			float num = 1f / num2;
			result.X = -quaternion.X * num;
			result.Y = -quaternion.Y * num;
			result.Z = -quaternion.Z * num;
			result.W = quaternion.W * num;
		}

		public static Quaternion Lerp(
			Quaternion quaternion1,
			Quaternion quaternion2,
			float amount
		) {
			Quaternion quaternion;
			Lerp(ref quaternion1, ref quaternion2, amount, out quaternion);
			return quaternion;
		}

		public static void Lerp(
			ref Quaternion quaternion1,
			ref Quaternion quaternion2,
			float amount,
			out Quaternion result
		) {
			float num = amount;
			float num2 = 1f - num;
			float num5 = (
				(quaternion1.X * quaternion2.X) +
				(quaternion1.Y * quaternion2.Y) +
				(quaternion1.Z * quaternion2.Z) +
				(quaternion1.W * quaternion2.W)
			);
			if (num5 >= 0f)
			{
				result.X = (num2 * quaternion1.X) + (num * quaternion2.X);
				result.Y = (num2 * quaternion1.Y) + (num * quaternion2.Y);
				result.Z = (num2 * quaternion1.Z) + (num * quaternion2.Z);
				result.W = (num2 * quaternion1.W) + (num * quaternion2.W);
			}
			else
			{
				result.X = (num2 * quaternion1.X) - (num * quaternion2.X);
				result.Y = (num2 * quaternion1.Y) - (num * quaternion2.Y);
				result.Z = (num2 * quaternion1.Z) - (num * quaternion2.Z);
				result.W = (num2 * quaternion1.W) - (num * quaternion2.W);
			}
			float num4 = (
				(result.X * result.X) +
				(result.Y * result.Y) +
				(result.Z * result.Z) +
				(result.W * result.W)
			);
			float num3 = 1f / ((float) Math.Sqrt((double) num4));
			result.X *= num3;
			result.Y *= num3;
			result.Z *= num3;
			result.W *= num3;
		}

		public static Quaternion Slerp(
			Quaternion quaternion1,
			Quaternion quaternion2,
			float amount
		) {
			Quaternion quaternion;
			Slerp(ref quaternion1, ref quaternion2, amount, out quaternion);
			return quaternion;
		}

		public static void Slerp(
			ref Quaternion quaternion1,
			ref Quaternion quaternion2,
			float amount,
			out Quaternion result
		) {
			float num2;
			float num3;
			float num = amount;
			float num4 = (
				(quaternion1.X * quaternion2.X) +
				(quaternion1.Y * quaternion2.Y) +
				(quaternion1.Z * quaternion2.Z) +
				(quaternion1.W * quaternion2.W)
			);
			float flag = 1.0f;
			if (num4 < 0f)
			{
				flag = -1.0f;
				num4 = -num4;
			}
			if (num4 > 0.999999f)
			{
				num3 = 1f - num;
				num2 = num * flag;
			}
			else
			{
				float num5 = (float) Math.Acos((double) num4);
				float num6 = (float) (1.0 / Math.Sin((double) num5));
				num3 = ((float) Math.Sin((double) ((1f - num) * num5))) * num6;
				num2 = flag * (((float) Math.Sin((double) (num * num5))) * num6);
			}
			result.X = (num3 * quaternion1.X) + (num2 * quaternion2.X);
			result.Y = (num3 * quaternion1.Y) + (num2 * quaternion2.Y);
			result.Z = (num3 * quaternion1.Z) + (num2 * quaternion2.Z);
			result.W = (num3 * quaternion1.W) + (num2 * quaternion2.W);
		}

		public static Quaternion Subtract(Quaternion quaternion1, Quaternion quaternion2)
		{
			Quaternion quaternion;
			Subtract(ref quaternion1, ref quaternion2, out quaternion);
			return quaternion;
		}

		public static void Subtract(
			ref Quaternion quaternion1,
			ref Quaternion quaternion2,
			out Quaternion result
		) {
			result.X = quaternion1.X - quaternion2.X;
			result.Y = quaternion1.Y - quaternion2.Y;
			result.Z = quaternion1.Z - quaternion2.Z;
			result.W = quaternion1.W - quaternion2.W;
		}

		public static Quaternion Multiply(Quaternion quaternion1, Quaternion quaternion2)
		{
			Quaternion quaternion;
			Multiply(ref quaternion1, ref quaternion2, out quaternion);
			return quaternion;
		}

		public static Quaternion Multiply(Quaternion quaternion1, float scaleFactor)
		{
			Quaternion quaternion;
			Multiply(ref quaternion1, scaleFactor, out quaternion);
			return quaternion;
		}

		public static void Multiply(
			ref Quaternion quaternion1,
			float scaleFactor,
			out Quaternion result
		) {
			result.X = quaternion1.X * scaleFactor;
			result.Y = quaternion1.Y * scaleFactor;
			result.Z = quaternion1.Z * scaleFactor;
			result.W = quaternion1.W * scaleFactor;
		}

		public static void Multiply(
			ref Quaternion quaternion1,
			ref Quaternion quaternion2,
			out Quaternion result
		) {
			float x = quaternion1.X;
			float y = quaternion1.Y;
			float z = quaternion1.Z;
			float w = quaternion1.W;
			float num4 = quaternion2.X;
			float num3 = quaternion2.Y;
			float num2 = quaternion2.Z;
			float num = quaternion2.W;
			float num12 = (y * num2) - (z * num3);
			float num11 = (z * num4) - (x * num2);
			float num10 = (x * num3) - (y * num4);
			float num9 = ((x * num4) + (y * num3)) + (z * num2);
			result.X = ((x * num) + (num4 * w)) + num12;
			result.Y = ((y * num) + (num3 * w)) + num11;
			result.Z = ((z * num) + (num2 * w)) + num10;
			result.W = (w * num) - num9;
		}

		public static Quaternion Negate(Quaternion quaternion)
		{
			Quaternion quaternion2;
			Negate(ref quaternion, out quaternion2);
			return quaternion2;
		}

		public static void Negate(ref Quaternion quaternion, out Quaternion result)
		{
			result.X = -quaternion.X;
			result.Y = -quaternion.Y;
			result.Z = -quaternion.Z;
			result.W = -quaternion.W;
		}

		public static Quaternion Normalize(Quaternion quaternion)
		{
			Quaternion quaternion2;
			Normalize(ref quaternion, out quaternion2);
			return quaternion2;
		}

		public static void Normalize(ref Quaternion quaternion, out Quaternion result)
		{
			float num2 = (
				(quaternion.X * quaternion.X) +
				(quaternion.Y * quaternion.Y) +
				(quaternion.Z * quaternion.Z) +
				(quaternion.W * quaternion.W)
			);
			float num = 1f / ((float) Math.Sqrt((double) num2));
			result.X = quaternion.X * num;
			result.Y = quaternion.Y * num;
			result.Z = quaternion.Z * num;
			result.W = quaternion.W * num;
		}

		public static Quaternion operator +(Quaternion quaternion1, Quaternion quaternion2)
		{
			Quaternion quaternion;
			Add(ref quaternion1, ref quaternion2, out quaternion);
			return quaternion;
		}

		public static Quaternion operator /(Quaternion quaternion1, Quaternion quaternion2)
		{
			Quaternion quaternion;
			Divide(ref quaternion1, ref quaternion2, out quaternion);
			return quaternion;
		}

		public static bool operator ==(Quaternion quaternion1, Quaternion quaternion2)
		{
			return quaternion1.Equals(quaternion2);
		}

		public static bool operator !=(Quaternion quaternion1, Quaternion quaternion2)
		{
			return !quaternion1.Equals(quaternion2);
		}

		public static Quaternion operator *(Quaternion quaternion1, Quaternion quaternion2)
		{
			Quaternion quaternion;
			Multiply(ref quaternion1, ref quaternion2, out quaternion);
			return quaternion;
		}

		public static Quaternion operator *(Quaternion quaternion1, float scaleFactor)
		{
			Quaternion quaternion;
			Multiply(ref quaternion1, scaleFactor, out quaternion);
			return quaternion;
		}

		public static Quaternion operator -(Quaternion quaternion1, Quaternion quaternion2)
		{
			Quaternion quaternion;
			Subtract(ref quaternion1, ref quaternion2, out quaternion);
			return quaternion;
		}

		public static Quaternion operator -(Quaternion quaternion)
		{
			Quaternion quaternion2;
			Negate(ref quaternion, out quaternion2);
			return quaternion2;
		}

		#endregion

		#region Internal Static Methods

		internal static void ToMatrix(Quaternion quaternion, out Matrix matrix)
		{
			// source -> http://content.gpwiki.org/index.php/OpenGL:Tutorials:Using_Quaternions_to_represent_rotation#Quaternion_to_Matrix
			float x2 = quaternion.X * quaternion.X;
			float y2 = quaternion.Y * quaternion.Y;
			float z2 = quaternion.Z * quaternion.Z;
			float xy = quaternion.X * quaternion.Y;
			float xz = quaternion.X * quaternion.Z;
			float yz = quaternion.Y * quaternion.Z;
			float wx = quaternion.W * quaternion.X;
			float wy = quaternion.W * quaternion.Y;
			float wz = quaternion.W * quaternion.Z;

			/* This calculation would be a lot more complicated for non-unit length
			 * quaternions.
			 * Note: The constructor of Matrix4 expects the Matrix in column-major
			 * format like expected by OpenGL.
			 */
			matrix.M11 = 1.0f - 2.0f * (y2 + z2);
			matrix.M12 = 2.0f * (xy - wz);
			matrix.M13 = 2.0f * (xz + wy);
			matrix.M14 = 0.0f;

			matrix.M21 = 2.0f * (xy + wz);
			matrix.M22 = 1.0f - 2.0f * (x2 + z2);
			matrix.M23 = 2.0f * (yz - wx);
			matrix.M24 = 0.0f;

			matrix.M31 = 2.0f * (xz - wy);
			matrix.M32 = 2.0f * (yz + wx);
			matrix.M33 = 1.0f - 2.0f * (x2 + y2);
			matrix.M34 = 0.0f;

			matrix.M41 = 2.0f * (xz - wy);
			matrix.M42 = 2.0f * (yz + wx);
			matrix.M43 = 1.0f - 2.0f * (x2 + y2);
			matrix.M44 = 0.0f;
		}

		#endregion
	}
}
