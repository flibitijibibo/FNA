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
	public class OcclusionQuery : GraphicsResource
	{
		#region Public Properties

		public bool IsComplete
		{
			get
			{
				int resultReady = 0;
				GraphicsDevice.GLDevice.glGetQueryObjectiv(
					glQueryId,
					OpenGLDevice.GLenum.GL_QUERY_RESULT_AVAILABLE,
					out resultReady
				);
				return resultReady != 0;
			}
		}

		public int PixelCount
		{
			get
			{
				int result = 0;
				GraphicsDevice.GLDevice.glGetQueryObjectiv(
					glQueryId,
					OpenGLDevice.GLenum.GL_QUERY_RESULT,
					out result
				);
				return result;
			}
		}

		#endregion

		#region Private OpenGL Variables

		private uint glQueryId;

		#endregion

		#region Public Constructor

		public OcclusionQuery(GraphicsDevice graphicsDevice)
		{
			GraphicsDevice = graphicsDevice;
			GraphicsDevice.GLDevice.glGenQueries(
				1,
				out glQueryId
			);
		}

		#endregion

		#region Protected Dispose Method

		protected override void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				// TODO: AddDisposeQuery -flibit
				GraphicsDevice.GLDevice.glDeleteQueries(
					1,
					ref glQueryId
				);
			}
			base.Dispose(disposing);
		}

		#endregion

		#region Public Begin/End Methods

		public void Begin()
		{
			GraphicsDevice.GLDevice.glBeginQuery(
				OpenGLDevice.GLenum.GL_SAMPLES_PASSED,
				glQueryId
			);
		}

		public void End()
		{
			GraphicsDevice.GLDevice.glEndQuery(
				OpenGLDevice.GLenum.GL_SAMPLES_PASSED
			);
		}

		#endregion
	}
}

