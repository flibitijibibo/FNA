#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */

/*
MIT License
Copyright (C) 2006 The Mono.Xna Team

All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
#endregion

#region Using Statements
using System;
using System.Runtime.Serialization;
#endregion

namespace Microsoft.Xna.Framework
{
	// TODO [TypeConverter(ExpandableObjectConverter)]
	[DataContract]
	public class Curve
	{
		#region Public Properties

		[DataMember]
		public bool IsConstant
		{
			get
			{
				return Keys.Count <= 1;
			}
		}

		[DataMember]
		public CurveKeyCollection Keys
		{
			get;
			private set;
		}

		[DataMember]
		public CurveLoopType PostLoop
		{
			get;
			set;
		}

		[DataMember]
		public CurveLoopType PreLoop
		{
			get;
			set;
		}

		#endregion

		#region Public Constructors

		public Curve()
		{
			Keys = new CurveKeyCollection();
		}

		#endregion

		#region Private Constructors

		private Curve(CurveKeyCollection keys)
		{
			Keys = keys;
		}

		#endregion

		#region Public Methods

		public Curve Clone()
		{
			Curve curve = new Curve(Keys.Clone());
			curve.PreLoop = PreLoop;
			curve.PostLoop = PostLoop;
			return curve;
		}

		public float Evaluate(float position)
		{
			CurveKey first = Keys[0];
			CurveKey last = Keys[Keys.Count - 1];

			if (position < first.Position)
			{
				switch (this.PreLoop)
				{
					case CurveLoopType.Constant:
						return first.Value;

					case CurveLoopType.Linear:
						// Linear y = a*x +b with a tangeant of last point.
						return first.Value - first.TangentIn * (first.Position - position);

					case CurveLoopType.Cycle:
						// Start -> end / start -> end...
						int cycle = GetNumberOfCycle(position);
						float virtualPos = position - (cycle * (last.Position - first.Position));
						return GetCurvePosition(virtualPos);

					case CurveLoopType.CycleOffset:
						/* Make the curve continue (with no step) so must up
						 * the curve each cycle of delta(value).
						 */
						cycle = GetNumberOfCycle(position);
						virtualPos = position - (cycle * (last.Position - first.Position));
						return (GetCurvePosition(virtualPos) + cycle * (last.Value - first.Value));

					case CurveLoopType.Oscillate:
						/* Go back on curve from end and target start
						 * Start-> end / end -> start...
						 */
						cycle = GetNumberOfCycle(position);
						
						if (0 == cycle % 2f)
						{
							virtualPos = position - (cycle * (last.Position - first.Position));
						}
						else
						{
							virtualPos = last.Position - position + first.Position + (cycle * (last.Position - first.Position));
						}
						return GetCurvePosition(virtualPos);
				}
			}
			else if (position > last.Position)
			{
				int cycle;
				switch (this.PostLoop)
				{
					case CurveLoopType.Constant:
						return last.Value;

					case CurveLoopType.Linear:
						// Linear y = a*x +b with a tangeant of last point.
						return last.Value + first.TangentOut * (position - last.Position);

					case CurveLoopType.Cycle:
						// Start -> end / start -> end...
						cycle = GetNumberOfCycle(position);
						float virtualPos = position - (cycle * (last.Position - first.Position));
						return GetCurvePosition(virtualPos);

					case CurveLoopType.CycleOffset:
						/* Make the curve continue (with no step) so must up
						 * the curve each cycle of delta(value).
						 */
						cycle = GetNumberOfCycle(position);
						virtualPos = position - (cycle * (last.Position - first.Position));
						return (GetCurvePosition(virtualPos) + cycle * (last.Value - first.Value));

					case CurveLoopType.Oscillate:
						/* Go back on curve from end and target start.
						 * Start-> end / end -> start...
						 */
						cycle = GetNumberOfCycle(position);
						virtualPos = position - (cycle * (last.Position - first.Position));

						if (0 == cycle % 2f)
						{
							virtualPos = position - (cycle * (last.Position - first.Position));
						}
						else
						{
							virtualPos =
								last.Position - position + first.Position +
								(cycle * (last.Position - first.Position)
							);
						}
						return GetCurvePosition(virtualPos);
				}
			}

			// In curve.
			return GetCurvePosition(position);
		}

		public void ComputeTangents (CurveTangent tangentType )
		{
			ComputeTangents(tangentType, tangentType);
		}

		public void ComputeTangents(CurveTangent tangentInType, CurveTangent tangentOutType)
		{
			for (int i = 0; i < Keys.Count; i += 1)
			{
				ComputeTangent(i, tangentInType, tangentOutType);
			}
		}

		public void ComputeTangent(int keyIndex, CurveTangent tangentType)
		{
			ComputeTangent(keyIndex, tangentType, tangentType);
		}

		public void ComputeTangent(
			int keyIndex,
			CurveTangent tangentInType,
			CurveTangent tangentOutType
		) {
			// See http://msdn.microsoft.com/en-us/library/microsoft.xna.framework.curvetangent.aspx

			CurveKey key = Keys[keyIndex];

			float p0, p, p1;
			p0 = p = p1 = key.Position;

			float v0, v, v1;
			v0 = v = v1 = key.Value;

			if (keyIndex > 0)
			{
				p0 = Keys[keyIndex - 1].Position;
				v0 = Keys[keyIndex - 1].Value;
			}

			if (keyIndex < Keys.Count-1)
			{
				p1 = Keys[keyIndex + 1].Position;
				v1 = Keys[keyIndex + 1].Value;
			}

			switch (tangentInType)
			{
				case CurveTangent.Flat:
					key.TangentIn = 0;
					break;
				case CurveTangent.Linear:
					key.TangentIn = v - v0;
					break;
				case CurveTangent.Smooth:
					float pn = p1 - p0;
					if (MathHelper.WithinEpsilon(pn, 0.0f))
					{
						key.TangentIn = 0;
					}
					else
					{
						key.TangentIn = (v1 - v0) * ((p - p0) / pn);
					}
					break;
			}

			switch (tangentOutType)
			{
				case CurveTangent.Flat:
					key.TangentOut = 0;
					break;
				case CurveTangent.Linear:
					key.TangentOut = v1 - v;
					break;
				case CurveTangent.Smooth:
					float pn = p1 - p0;
					if (Math.Abs(pn) < float.Epsilon)
					{
						key.TangentOut = 0;
					}
					else
					{
						key.TangentOut = (v1 - v0) * ((p1 - p) / pn);
					}
					break;
			}
		}

		#endregion

		#region Private Methods

		private int GetNumberOfCycle(float position)
		{
			float cycle = (position - Keys[0].Position) /
				(Keys[Keys.Count - 1].Position - Keys[0].Position);
			if (cycle < 0f)
			{
				cycle -= 1;
			}
			return (int) cycle;
		}

		private float GetCurvePosition(float position)
		{
			// Only for position in curve
			CurveKey prev = Keys[0];
			CurveKey next;
			for (int i = 1; i < Keys.Count; i++)
			{
				next = Keys[i];
				if (next.Position >= position)
				{
					if (prev.Continuity == CurveContinuity.Step)
					{
						if (position >= 1f)
						{
							return next.Value;
						}
						return prev.Value;
					}
					// To have t in [0,1]
					float t = (
						(position - prev.Position) /
						(next.Position - prev.Position)
					);
					float ts = t * t;
					float tss = ts * t;
					/* After a lot of search on internet I have found all about
					 * spline function and bezier (phi'sss ancien) but finally
					 * used hermite curve:
					 * http://en.wikipedia.org/wiki/Cubic_Hermite_spline
					 * P(t) = (2*t^3 - 3t^2 + 1)*P0 + (t^3 - 2t^2 + t)m0 +
					 *        (-2t^3 + 3t^2)P1 + (t^3-t^2)m1
					 * with P0.value = prev.value , m0 = prev.tangentOut,
					 *      P1= next.value, m1 = next.TangentIn.
					 */
					return (
						(2 * tss - 3 * ts + 1f) * prev.Value +
						(tss - 2 * ts + t) * prev.TangentOut +
						(3 * ts - 2 * tss) * next.Value +
						(tss - ts) * next.TangentIn
					);
				}
				prev = next;
			}
			return 0f;
		}

		#endregion
	}
}
