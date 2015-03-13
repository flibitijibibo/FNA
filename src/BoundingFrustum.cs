#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
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
using System.Text;
#endregion

namespace Microsoft.Xna.Framework
{
	[DebuggerDisplay("{DebugDisplayString,nq}")]
	public class BoundingFrustum : IEquatable<BoundingFrustum>
	{
		#region Public Properties

		public Matrix Matrix
		{
			get
			{
				return this.matrix;
			}
			set
			{
				/* FIXME: The odds are the planes will be used a lot more often than
				 * the matrix is updated, so this should help performance. I hope. ;)
				 */
				this.matrix = value;
				this.CreatePlanes();
				this.CreateCorners();
			}
		}

		public Plane Near
		{
			get
			{
				return this.planes[0];
			}
		}

		public Plane Far
		{
			get
			{
				return this.planes[1];
			}
		}

		public Plane Left
		{
			get
			{
				return this.planes[2];
			}
		}

		public Plane Right
		{
			get
			{
				return this.planes[3];
			}
		}

		public Plane Top
		{
			get
			{
				return this.planes[4];
			}
		}

		public Plane Bottom
		{
			get
			{
				return this.planes[5];
			}
		}

		#endregion

		#region Internal Properties

		internal string DebugDisplayString
		{
			get
			{
				return string.Concat(
					"Near( ", planes[0].DebugDisplayString, " ) \r\n",
					"Far( ", planes[1].DebugDisplayString, " ) \r\n",
					"Left( ", planes[2].DebugDisplayString, " ) \r\n",
					"Right( ", planes[3].DebugDisplayString, " ) \r\n",
					"Top( ", planes[4].DebugDisplayString, " ) \r\n",
					"Bottom( ", planes[5].DebugDisplayString, " ) "
				);
			}
		}

		#endregion

		#region Public Fields

		public const int CornerCount = 8;

		#endregion

		#region Private Fields

		private Matrix matrix;
		private readonly Vector3[] corners = new Vector3[CornerCount];
		private readonly Plane[] planes = new Plane[PlaneCount];

		private const int PlaneCount = 6;

		#endregion

		#region Public Constructors

		public BoundingFrustum(Matrix value)
		{
			this.matrix = value;
			this.CreatePlanes();
			this.CreateCorners();
		}

		#endregion

		#region Public Methods

		public ContainmentType Contains(BoundingFrustum frustum)
		{
			if (this == frustum)
			{
				return ContainmentType.Contains;
			}
			bool intersects = false;
			for (int i = 0; i < PlaneCount; i += 1)
			{
				PlaneIntersectionType planeIntersectionType;
				frustum.Intersects(ref planes[i], out planeIntersectionType);
				if (planeIntersectionType == PlaneIntersectionType.Front)
				{
					return ContainmentType.Disjoint;
				}
				else if (planeIntersectionType == PlaneIntersectionType.Intersecting)
				{
					intersects = true;
				}
			}
			return intersects ? ContainmentType.Intersects : ContainmentType.Contains;
		}

		public ContainmentType Contains(BoundingBox box)
		{
			ContainmentType result = default(ContainmentType);
			this.Contains(ref box, out result);
			return result;
		}

		public void Contains(ref BoundingBox box, out ContainmentType result)
		{
			bool intersects = false;
			for (int i = 0; i < PlaneCount; i += 1)
			{
				PlaneIntersectionType planeIntersectionType = default(PlaneIntersectionType);
				box.Intersects(ref this.planes[i], out planeIntersectionType);
				switch (planeIntersectionType)
				{
				case PlaneIntersectionType.Front:
					result = ContainmentType.Disjoint;
					return;
				case PlaneIntersectionType.Intersecting:
					intersects = true;
					break;
				}
			}
			result = intersects ? ContainmentType.Intersects : ContainmentType.Contains;
		}

		public ContainmentType Contains(BoundingSphere sphere)
		{
			ContainmentType result = default(ContainmentType);
			this.Contains(ref sphere, out result);
			return result;
		}

		public void Contains(ref BoundingSphere sphere, out ContainmentType result)
		{
			bool intersects = false;
			for (int i = 0; i < PlaneCount; i += 1)
			{
				PlaneIntersectionType planeIntersectionType = default(PlaneIntersectionType);

				// TODO: We might want to inline this for performance reasons.
				sphere.Intersects(ref this.planes[i], out planeIntersectionType);
				switch (planeIntersectionType)
				{
				case PlaneIntersectionType.Front:
					result = ContainmentType.Disjoint;
					return;
				case PlaneIntersectionType.Intersecting:
					intersects = true;
					break;
				}
			}
			result = intersects ? ContainmentType.Intersects : ContainmentType.Contains;
		}

		public ContainmentType Contains(Vector3 point)
		{
			ContainmentType result = default(ContainmentType);
			this.Contains(ref point, out result);
			return result;
		}

		public void Contains(ref Vector3 point, out ContainmentType result)
		{
			bool intersects = false;
			for (int i = 0; i < PlaneCount; i += 1)
			{
				float classifyPoint = (
					(point.X * planes[i].Normal.X) +
					(point.Y * planes[i].Normal.Y) +
					(point.Z * planes[i].Normal.Z) +
					planes[i].D
				);
				if (classifyPoint > 0)
				{
					result = ContainmentType.Disjoint;
					return;
				}
				else if (classifyPoint == 0)
				{
					intersects = true;
					break;
				}
			}
			result = intersects ? ContainmentType.Intersects : ContainmentType.Contains;
		}

		public bool Equals(BoundingFrustum other)
		{
			return (this == other);
		}

		public Vector3[] GetCorners()
		{
			return (Vector3[]) this.corners.Clone();
		}

		public void GetCorners(Vector3[] corners)
		{
			if (corners == null)
			{
				throw new ArgumentNullException("corners");
			}
			if (corners.Length < CornerCount)
			{
				throw new ArgumentOutOfRangeException("corners");
			}

			this.corners.CopyTo(corners, 0);
		}

		public override int GetHashCode()
		{
			return this.matrix.GetHashCode();
		}

		public bool Intersects(BoundingFrustum frustum)
		{
			return (Contains(frustum) != ContainmentType.Disjoint);
		}

		public bool Intersects(BoundingBox box)
		{
			bool result = false;
			this.Intersects(ref box, out result);
			return result;
		}

		public void Intersects(ref BoundingBox box, out bool result)
		{
			ContainmentType containment = default(ContainmentType);
			this.Contains(ref box, out containment);
			result = containment != ContainmentType.Disjoint;
		}

		public bool Intersects(BoundingSphere sphere)
		{
			bool result = default(bool);
			this.Intersects(ref sphere, out result);
			return result;
		}

		public void Intersects(ref BoundingSphere sphere, out bool result)
		{
			ContainmentType containment = default(ContainmentType);
			this.Contains(ref sphere, out containment);
			result = containment != ContainmentType.Disjoint;
		}

		public PlaneIntersectionType Intersects(Plane plane)
		{
			PlaneIntersectionType result;
			Intersects(ref plane, out result);
			return result;
		}

		public void Intersects(ref Plane plane, out PlaneIntersectionType result)
		{
			result = plane.Intersects(ref corners[0]);
			for (int i = 1; i < corners.Length; i += 1)
			{
				if (plane.Intersects(ref corners[i]) != result)
				{
					result = PlaneIntersectionType.Intersecting;
				}
			}
		}

		public float? Intersects(Ray ray)
		{
			float? result;
			Intersects(ref ray, out result);
			return result;
		}

		public void Intersects(ref Ray ray, out float? result)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region Private Methods

		private void CreateCorners()
		{
			IntersectionPoint(
				ref this.planes[0],
				ref this.planes[2],
				ref this.planes[4],
				out this.corners[0]
			);
			IntersectionPoint(
				ref this.planes[0],
				ref this.planes[3],
				ref this.planes[4],
				out this.corners[1]
			);
			IntersectionPoint(
				ref this.planes[0],
				ref this.planes[3],
				ref this.planes[5],
				out this.corners[2]
			);
			IntersectionPoint(
				ref this.planes[0],
				ref this.planes[2],
				ref this.planes[5],
				out this.corners[3]
			);
			IntersectionPoint(
				ref this.planes[1],
				ref this.planes[2],
				ref this.planes[4],
				out this.corners[4]
			);
			IntersectionPoint(
				ref this.planes[1],
				ref this.planes[3],
				ref this.planes[4],
				out this.corners[5]
			);
			IntersectionPoint(
				ref this.planes[1],
				ref this.planes[3],
				ref this.planes[5],
				out this.corners[6]
			);
			IntersectionPoint(
				ref this.planes[1],
				ref this.planes[2],
				ref this.planes[5],
				out this.corners[7]
			);
		}

		private void CreatePlanes()
		{
			this.planes[0] = new Plane(
				-this.matrix.M13,
				-this.matrix.M23,
				-this.matrix.M33,
				-this.matrix.M43
			);
			this.planes[1] = new Plane(
				this.matrix.M13 - this.matrix.M14,
				this.matrix.M23 - this.matrix.M24,
				this.matrix.M33 - this.matrix.M34,
				this.matrix.M43 - this.matrix.M44
			);
			this.planes[2] = new Plane(
				-this.matrix.M14 - this.matrix.M11,
				-this.matrix.M24 - this.matrix.M21,
				-this.matrix.M34 - this.matrix.M31,
				-this.matrix.M44 - this.matrix.M41
			);
			this.planes[3] = new Plane(
				this.matrix.M11 - this.matrix.M14,
				this.matrix.M21 - this.matrix.M24,
				this.matrix.M31 - this.matrix.M34,
				this.matrix.M41 - this.matrix.M44
			);
			this.planes[4] = new Plane(
				this.matrix.M12 - this.matrix.M14,
				this.matrix.M22 - this.matrix.M24,
				this.matrix.M32 - this.matrix.M34,
				this.matrix.M42 - this.matrix.M44
			);
			this.planes[5] = new Plane(
				-this.matrix.M14 - this.matrix.M12,
				-this.matrix.M24 - this.matrix.M22,
				-this.matrix.M34 - this.matrix.M32,
				-this.matrix.M44 - this.matrix.M42
			);

			this.NormalizePlane(ref this.planes[0]);
			this.NormalizePlane(ref this.planes[1]);
			this.NormalizePlane(ref this.planes[2]);
			this.NormalizePlane(ref this.planes[3]);
			this.NormalizePlane(ref this.planes[4]);
			this.NormalizePlane(ref this.planes[5]);
		}

		private void NormalizePlane(ref Plane p)
		{
			float factor = 1f / p.Normal.Length();
			p.Normal.X *= factor;
			p.Normal.Y *= factor;
			p.Normal.Z *= factor;
			p.D *= factor;
		}

		#endregion

		#region Private Static Methods

		private static void IntersectionPoint(
			ref Plane a,
			ref Plane b,
			ref Plane c,
			out Vector3 result
		) {
			/* Formula used
			 *                d1 ( N2 * N3 ) + d2 ( N3 * N1 ) + d3 ( N1 * N2 )
			 * P =   -------------------------------------------------------------------
			 *                             N1 . ( N2 * N3 )
			 *
			 * Note: N refers to the normal, d refers to the displacement. '.' means dot
			 * product. '*' means cross product
			 */

			Vector3 v1, v2, v3;
			Vector3 cross;

			Vector3.Cross(ref b.Normal, ref c.Normal, out cross);

			float f;
			Vector3.Dot(ref a.Normal, ref cross, out f);
			f *= -1.0f;

			Vector3.Cross(ref b.Normal, ref c.Normal, out cross);
			Vector3.Multiply(ref cross, a.D, out v1);
			// v1 = (a.D * (Vector3.Cross(b.Normal, c.Normal)));


			Vector3.Cross(ref c.Normal, ref a.Normal, out cross);
			Vector3.Multiply(ref cross, b.D, out v2);
			// v2 = (b.D * (Vector3.Cross(c.Normal, a.Normal)));


			Vector3.Cross(ref a.Normal, ref b.Normal, out cross);
			Vector3.Multiply(ref cross, c.D, out v3);
			// v3 = (c.D * (Vector3.Cross(a.Normal, b.Normal)));

			result.X = (v1.X + v2.X + v3.X) / f;
			result.Y = (v1.Y + v2.Y + v3.Y) / f;
			result.Z = (v1.Z + v2.Z + v3.Z) / f;
		}

		#endregion

		#region Public Static Operators and Override Methods

		public static bool operator ==(BoundingFrustum a, BoundingFrustum b)
		{
			if (object.Equals(a, null))
			{
				return (object.Equals(b, null));
			}

			if (object.Equals(b, null))
			{
				return (object.Equals(a, null));
			}

			return a.matrix == (b.matrix);
		}

		public static bool operator !=(BoundingFrustum a, BoundingFrustum b)
		{
			return !(a == b);
		}

		public override bool Equals(object obj)
		{
			BoundingFrustum f = obj as BoundingFrustum;
			return (object.Equals(f, null)) ? false : (this == f);
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder(256);
			sb.Append("{Near:");
			sb.Append(this.planes[0].ToString());
			sb.Append(" Far:");
			sb.Append(this.planes[1].ToString());
			sb.Append(" Left:");
			sb.Append(this.planes[2].ToString());
			sb.Append(" Right:");
			sb.Append(this.planes[3].ToString());
			sb.Append(" Top:");
			sb.Append(this.planes[4].ToString());
			sb.Append(" Bottom:");
			sb.Append(this.planes[5].ToString());
			sb.Append("}");
			return sb.ToString();
		}

		#endregion
	}
}

