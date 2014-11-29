#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

namespace Microsoft.Xna.Framework.Graphics
{
	public sealed class SamplerStateCollection
	{
		#region Public Array Access Property

		public SamplerState this[int index]
		{
			get
			{
				return samplers[index];
			}
			set
			{
				// FIXME: Bring this back after the IGLDevice is established.
				// if (samplers[index] != value)
				{
					samplers[index] = value;
					if (!graphicsDevice.ModifiedSamplers.Contains(index))
					{
						graphicsDevice.ModifiedSamplers.Enqueue(index);
					}
				}
			}
		}

		#endregion

		#region Private Variables

		private readonly SamplerState[] samplers;
		private readonly GraphicsDevice graphicsDevice;

		#endregion

		#region Internal Constructor

		internal SamplerStateCollection(GraphicsDevice parentDevice)
		{
			samplers = new SamplerState[parentDevice.GLDevice.MaxTextureSlots];
			graphicsDevice = parentDevice;
			for (int i = 0; i < samplers.Length; i += 1)
			{
				samplers[i] = SamplerState.LinearWrap;
			}
		}

		#endregion
	}
}
