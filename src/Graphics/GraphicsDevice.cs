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
using System.Runtime.InteropServices;
#endregion

namespace Microsoft.Xna.Framework.Graphics
{
	public class GraphicsDevice : IDisposable
	{
		#region Public GraphicsDevice State Properties

		public bool IsDisposed
		{
			get;
			private set;
		}

		public GraphicsDeviceStatus GraphicsDeviceStatus
		{
			get
			{
				return GraphicsDeviceStatus.Normal;
			}
		}

		public GraphicsAdapter Adapter
		{
			get;
			private set;
		}

		public GraphicsProfile GraphicsProfile
		{
			get;
			private set;
		}

		public PresentationParameters PresentationParameters
		{
			get;
			private set;
		}

		#endregion

		#region Public Graphics Display Properties

		public DisplayMode DisplayMode
		{
			get
			{
				if (PresentationParameters.IsFullScreen)
				{
					return new DisplayMode(
						GLDevice.Backbuffer.Width,
						GLDevice.Backbuffer.Height,
						SurfaceFormat.Color
					);
				}
				return Adapter.CurrentDisplayMode;
			}
		}

		#endregion

		#region Public GL State Properties

		public TextureCollection Textures
		{
			get;
			private set;
		}

		public SamplerStateCollection SamplerStates
		{
			get;
			private set;
		}

		private BlendState INTERNAL_blendState;
		public BlendState BlendState
		{
			get
			{
				return INTERNAL_blendState;
			}
			set
			{
				if (value != INTERNAL_blendState)
				{
					GLDevice.SetBlendState(value);
					INTERNAL_blendState = value;
				}
			}
		}

		private DepthStencilState INTERNAL_depthStencilState;
		public DepthStencilState DepthStencilState
		{
			get
			{
				return INTERNAL_depthStencilState;
			}
			set
			{
				if (value != INTERNAL_depthStencilState)
				{
					GLDevice.SetDepthStencilState(value);
					INTERNAL_depthStencilState = value;
				}
			}
		}

		public RasterizerState RasterizerState
		{
			get;
			set;
		}

		/* We have to store this internally because we flip the Rectangle for
		 * when we aren't rendering to a target. I'd love to remove this.
		 * -flibit
		 */
		private Rectangle INTERNAL_scissorRectangle;
		public Rectangle ScissorRectangle
		{
			get
			{
				return INTERNAL_scissorRectangle;
			}
			set
			{
				INTERNAL_scissorRectangle = value;
				GLDevice.SetScissorRect(
					value,
					RenderTargetCount > 0
				);
			}
		}

		/* We have to store this internally because we flip the Viewport for
		 * when we aren't rendering to a target. I'd love to remove this.
		 * -flibit
		 */
		private Viewport INTERNAL_viewport;
		public Viewport Viewport
		{
			get
			{
				return INTERNAL_viewport;
			}
			set
			{
				INTERNAL_viewport = value;
				GLDevice.SetViewport(
					value,
					RenderTargetCount > 0
				);
			}
		}

		// FIXME: Codename OhGodNo -flibit
		public int ReferenceStencil
		{
			get;
			set;
		}

		// FIXME: Codename OhGodNo -flibit
		public SamplerStateCollection VertexSamplerStates
		{
			get;
			private set;
		}

		#endregion

		#region Public Buffer Object Properties

		public IndexBuffer Indices
		{
			get;
			set;
		}

		#endregion

		#region Internal OpenGL Device Property

		internal OpenGLDevice GLDevice
		{
			get;
			private set;
		}

		#endregion

		#region Internal RenderTarget Properties

		internal int RenderTargetCount
		{
			get;
			private set;
		}

		#endregion

		#region Internal Sampler Change Queue

		internal readonly Queue<int> ModifiedSamplers = new Queue<int>();

		#endregion

		#region Private Disposal Variables

		private static List<Action> disposeActions = new List<Action>();
		private static object disposeActionsLock = new object();

		#endregion

		#region Private Clear Variables

		/* On Intel Integrated graphics, there is a fast hw unit for doing
		 * clears to colors where all components are either 0 or 255.
		 * Despite XNA4 using Purple here, we use black (in Release) to avoid
		 * performance warnings on Intel/Mesa.
		 * -sulix
		 */
#if DEBUG
		private static readonly Color DiscardColor = new Color(68, 34, 136, 255);
#else
		private static readonly Color DiscardColor = new Color(0, 0, 0, 255);
#endif

		#endregion

		#region Private RenderTarget Variables

		// 4, per XNA4 HiDef spec
		private readonly RenderTargetBinding[] renderTargetBindings = new RenderTargetBinding[4];

		#endregion

		#region Private Buffer Object Variables

		// 16, per XNA4 HiDef spec
		private VertexBufferBinding[] vertexBufferBindings = new VertexBufferBinding[16];
		private int vertexBufferCount = 0;

		#endregion

		#region GraphicsDevice Events

#pragma warning disable 0067
		// We never lose devices, but lol XNA4 compliance -flibit
		public event EventHandler<EventArgs> DeviceLost;
#pragma warning restore 0067
		public event EventHandler<EventArgs> DeviceReset;
		public event EventHandler<EventArgs> DeviceResetting;
		public event EventHandler<ResourceCreatedEventArgs> ResourceCreated;
		public event EventHandler<ResourceDestroyedEventArgs> ResourceDestroyed;
		public event EventHandler<EventArgs> Disposing;

		// TODO: Hook this up to GraphicsResource
		internal void OnResourceCreated()
		{
			if (ResourceCreated != null)
			{
				ResourceCreated(this, (ResourceCreatedEventArgs) EventArgs.Empty);
			}
		}

		// TODO: Hook this up to GraphicsResource
		internal void OnResourceDestroyed()
		{
			if (ResourceDestroyed != null)
			{
				ResourceDestroyed(this, (ResourceDestroyedEventArgs) EventArgs.Empty);
			}
		}

		#endregion

		#region Constructor, Deconstructor, Dispose Methods

		/// <summary>
		/// Initializes a new instance of the <see cref="GraphicsDevice" /> class.
		/// </summary>
		/// <param name="adapter">The graphics adapter.</param>
		/// <param name="graphicsProfile">The graphics profile.</param>
		/// <param name="presentationParameters">The presentation options.</param>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="presentationParameters"/> is <see langword="null"/>.
		/// </exception>
		public GraphicsDevice(
			GraphicsAdapter adapter,
			GraphicsProfile graphicsProfile,
			PresentationParameters presentationParameters
		) {
			if (presentationParameters == null)
			{
				throw new ArgumentNullException("presentationParameters");
			}

			// Set the properties from the constructor parameters.
			Adapter = adapter;
			PresentationParameters = presentationParameters;
			GraphicsProfile = graphicsProfile;

			// Set up the OpenGL Device. Loads entry points.
			GLDevice = new OpenGLDevice(PresentationParameters);

			// Force set the default render states.
			BlendState = BlendState.Opaque;
			DepthStencilState = DepthStencilState.Default;
			RasterizerState = RasterizerState.CullCounterClockwise;

			// Initialize the Texture/Sampler state containers
			Textures = new TextureCollection(this);
			SamplerStates = new SamplerStateCollection(this);

			// Set the default viewport and scissor rect.
			Viewport = new Viewport(PresentationParameters.Bounds);
			ScissorRectangle = Viewport.Bounds;
		}

		~GraphicsDevice()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				if (disposing)
				{
					// We're about to dispose, notify the application.
					if (Disposing != null)
					{
						Disposing(this, EventArgs.Empty);
					}

					/* Dispose of all remaining graphics resources before
					 * disposing of the GraphicsDevice.
					 */
					GraphicsResource.DisposeAll();

					// Dispose of the GL Device/Context
					GLDevice.Dispose();
				}

				IsDisposed = true;
			}
		}

		#endregion

		#region Internal Resource Disposal Method

		/// <summary>
		/// Adds a dispose action to the list of pending dispose actions. These are executed
		/// at the end of each call to Present(). This allows GL resources to be disposed
		/// from other threads, such as the finalizer.
		/// </summary>
		/// <param name="disposeAction">The action to execute for the dispose.</param>
		internal static void AddDisposeAction(Action disposeAction)
		{
			if (disposeAction == null)
			{
				throw new ArgumentNullException("disposeAction");
			}
			if (Threading.IsOnMainThread())
			{
				disposeAction();
			}
			else
			{
				lock (disposeActionsLock)
				{
					disposeActions.Add(disposeAction);
				}
			}
		}

		#endregion

		#region Public Present Method

		public void Present()
		{
			// Dispose of any GL resources that were disposed in another thread
			lock (disposeActionsLock)
			{
				if (disposeActions.Count > 0)
				{
					foreach (Action action in disposeActions)
					{
						action();
					}
					disposeActions.Clear();
				}
			}
			GLDevice.SwapBuffers(PresentationParameters.DeviceWindowHandle);
		}

		#endregion

		#region Public Reset Methods

		public void Reset()
		{
			Reset(PresentationParameters, Adapter);
		}

		public void Reset(PresentationParameters presentationParameters)
		{
			Reset(presentationParameters, Adapter);
		}

		public void Reset(
			PresentationParameters presentationParameters,
			GraphicsAdapter graphicsAdapter
		) {
			if (presentationParameters == null)
			{
				throw new ArgumentNullException("presentationParameters");
			}

			// We're about to reset, let the application know.
			if (DeviceResetting != null)
			{
				DeviceResetting(this, EventArgs.Empty);
			}

			// Set the new PresentationParameters first.
			PresentationParameters = presentationParameters;

			/* Reset the backbuffer first, before doing anything else.
			 * The GLDevice needs to know what we're up to right away.
			 * -flibit
			 */
			GLDevice.Backbuffer.ResetFramebuffer(
				this,
				PresentationParameters.BackBufferWidth,
				PresentationParameters.BackBufferHeight,
				PresentationParameters.DepthStencilFormat
			);

			// Now, update the viewport
			Viewport = new Viewport(
				0,
				0,
				PresentationParameters.BackBufferWidth,
				PresentationParameters.BackBufferHeight
			);

			// Update the scissor rectangle to our new default target size
			ScissorRectangle = new Rectangle(
				0,
				0,
				PresentationParameters.BackBufferWidth,
				PresentationParameters.BackBufferHeight
			);

			// FIXME: This should probably mean something. -flibit
			Adapter = graphicsAdapter;

			// We just reset, let the application know.
			if (DeviceReset != null)
			{
				DeviceReset(this, EventArgs.Empty);
			}
		}

		#endregion

		#region Public Clear Methods

		public void Clear(Color color)
		{
			Clear(
				ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil,
				color.ToVector4(),
				Viewport.MaxDepth,
				0
			);
		}

		public void Clear(ClearOptions options, Color color, float depth, int stencil)
		{
			Clear(
				options,
				color.ToVector4(),
				depth,
				stencil
			);
		}

		public void Clear(ClearOptions options, Vector4 color, float depth, int stencil)
		{
			GLDevice.Clear(
				options,
				color,
				depth,
				stencil
			);
		}

		#endregion

		#region Public Backbuffer Methods

		public void GetBackBufferData<T>(T[] data) where T : struct
		{
			// Store off the old frame buffer components
			uint prevReadBuffer = GLDevice.CurrentReadFramebuffer;

			GLDevice.BindReadFramebuffer(GLDevice.Backbuffer.Handle);
			GCHandle ptr = GCHandle.Alloc(data, GCHandleType.Pinned);
			try
			{
				GLDevice.glReadPixels(
					0, 0,
					GLDevice.Backbuffer.Width,
					GLDevice.Backbuffer.Height,
					OpenGLDevice.GLenum.GL_RGBA,
					OpenGLDevice.GLenum.GL_UNSIGNED_BYTE,
					ptr.AddrOfPinnedObject()
				);
			}
			finally
			{
				ptr.Free();
			}

			// Restore old buffer components
			GLDevice.BindReadFramebuffer(prevReadBuffer);

			// Now we get to do a software-based flip! Yes, really! -flibit
			int width = GLDevice.Backbuffer.Width;
			int height = GLDevice.Backbuffer.Height;
			int pitch = width * 4 / Marshal.SizeOf(typeof(T));
			T[] tempRow = new T[pitch];
			for (int row = 0; row < height / 2; row += 1)
			{
				Array.Copy(data, row * pitch, tempRow, 0, pitch);
				Array.Copy(data, (height - row - 1) * pitch, data, row * pitch, pitch);
				Array.Copy(tempRow, 0, data, (height - row - 1) * pitch, pitch);
			}
		}

		#endregion

		#region Public RenderTarget Methods

		public void SetRenderTarget(RenderTarget2D renderTarget)
		{
			if (renderTarget == null)
			{
				SetRenderTargets(null);
			}
			else
			{
				SetRenderTargets(new RenderTargetBinding(renderTarget));
			}
		}

		public void SetRenderTarget(RenderTargetCube renderTarget, CubeMapFace cubeMapFace)
		{
			if (renderTarget == null)
			{
				SetRenderTargets(null);
			}
			else
			{
				SetRenderTargets(new RenderTargetBinding(renderTarget, cubeMapFace));
			}
		}

		public void SetRenderTargets(params RenderTargetBinding[] renderTargets)
		{
			// Checking for redundant SetRenderTargets...
			if (renderTargets == null && RenderTargetCount == 0)
			{
				return;
			}
			else if (renderTargets != null && renderTargets.Length == RenderTargetCount)
			{
				bool isRedundant = true;
				for (int i = 0; i < renderTargets.Length; i += 1)
				{
					if (	renderTargets[i].RenderTarget != renderTargetBindings[i].RenderTarget ||
						renderTargets[i].CubeMapFace != renderTargetBindings[i].CubeMapFace	)
					{
						isRedundant = false;
					}
				}
				if (isRedundant)
				{
					return;
				}
			}

			Array.Clear(renderTargetBindings, 0, renderTargetBindings.Length);
			if (renderTargets == null || renderTargets.Length == 0)
			{
				GLDevice.SetRenderTargets(null, null, 0, DepthFormat.None);

				RenderTargetCount = 0;

				// Set the viewport to the size of the backbuffer.
				Viewport = new Viewport(0, 0, PresentationParameters.BackBufferWidth, PresentationParameters.BackBufferHeight);

				// Set the scissor rectangle to the size of the backbuffer.
				ScissorRectangle = new Rectangle(0, 0, PresentationParameters.BackBufferWidth, PresentationParameters.BackBufferHeight);

				if (PresentationParameters.RenderTargetUsage == RenderTargetUsage.DiscardContents)
				{
					Clear(DiscardColor);
				}
			}
			else
			{
				uint[] glTarget = new uint[renderTargets.Length];
				OpenGLDevice.GLenum[] glTargetFace = new OpenGLDevice.GLenum[renderTargets.Length];
				for (int i = 0; i < renderTargets.Length; i += 1)
				{
					glTarget[i] = renderTargets[i].RenderTarget.texture.Handle;
					if (renderTargets[i].RenderTarget is RenderTarget2D)
					{
						glTargetFace[i] = OpenGLDevice.GLenum.GL_TEXTURE_2D;
					}
					else
					{
						glTargetFace[i] = OpenGLDevice.GLenum.GL_TEXTURE_CUBE_MAP_POSITIVE_X + (int) renderTargets[i].CubeMapFace;
					}
				}
				IRenderTarget target = renderTargets[0].RenderTarget as IRenderTarget;
				GLDevice.SetRenderTargets(
					glTarget,
					glTargetFace,
					target.DepthStencilBuffer,
					target.DepthStencilFormat
				);

				Array.Copy(renderTargets, renderTargetBindings, renderTargets.Length);
				RenderTargetCount = renderTargets.Length;

				// Set the viewport to the size of the first render target.
				Viewport = new Viewport(0, 0, target.Width, target.Height);

				// Set the scissor rectangle to the size of the first render target.
				ScissorRectangle = new Rectangle(0, 0, target.Width, target.Height);

				if (target.RenderTargetUsage == RenderTargetUsage.DiscardContents)
				{
					Clear(DiscardColor);
				}
			}
		}

		public RenderTargetBinding[] GetRenderTargets()
		{
			// Return a correctly sized copy our internal array.
			RenderTargetBinding[] bindings = new RenderTargetBinding[RenderTargetCount];
			Array.Copy(renderTargetBindings, bindings, RenderTargetCount);
			return bindings;
		}

		#endregion

		#region Public Buffer Object Methods

		public void SetVertexBuffer(VertexBuffer vertexBuffer)
		{
			SetVertexBuffer(vertexBuffer, 0);
		}

		public void SetVertexBuffer(VertexBuffer vertexBuffer, int vertexOffset)
		{
			if (vertexBuffer == null)
			{
				for (int i = 0; i < vertexBufferCount; i += 1)
				{
					vertexBufferBindings[i] = VertexBufferBinding.None;
				}
				vertexBufferCount = 0;
				return;
			}

			if (	!ReferenceEquals(vertexBufferBindings[0].VertexBuffer, vertexBuffer) ||
				vertexBufferBindings[0].VertexOffset != vertexOffset	)
			{
				vertexBufferBindings[0] = new VertexBufferBinding(
					vertexBuffer,
					vertexOffset
				);
			}

			for (int i = 1; i < vertexBufferCount; i += 1)
			{
				vertexBufferBindings[i] = VertexBufferBinding.None;
			}

			vertexBufferCount = 1;
		}

		public void SetVertexBuffers(params VertexBufferBinding[] vertexBuffers)
		{
			if (vertexBuffers == null)
			{
				for (int j = 0; j < vertexBufferCount; j += 1)
				{
					vertexBufferBindings[j] = VertexBufferBinding.None;
				}
				vertexBufferCount = 0;
				return;
			}

			if (vertexBuffers.Length > vertexBufferBindings.Length)
			{
				throw new ArgumentOutOfRangeException(
					"vertexBuffers",
					String.Format(
						"Max Vertex Buffers supported is {0}",
						vertexBufferBindings.Length
					)
				);
			}

			int i = 0;
			while (i < vertexBuffers.Length)
			{
				if (	!ReferenceEquals(vertexBufferBindings[i].VertexBuffer, vertexBuffers[i].VertexBuffer) ||
					vertexBufferBindings[i].VertexOffset != vertexBuffers[i].VertexOffset ||
					vertexBufferBindings[i].InstanceFrequency != vertexBuffers[i].InstanceFrequency	)
				{
					vertexBufferBindings[i] = vertexBuffers[i];
				}
				i += 1;
			}
			while (i < vertexBufferCount)
			{
				vertexBufferBindings[i] = VertexBufferBinding.None;
				i += 1;
			}

			vertexBufferCount = vertexBuffers.Length;
		}

		public VertexBufferBinding[] GetVertexBuffers()
		{
			VertexBufferBinding[] result = new VertexBufferBinding[vertexBufferCount];
			Array.Copy(
				vertexBufferBindings,
				result,
				vertexBufferCount
			);
			return result;
		}

		#endregion

		#region DrawPrimitives: VertexBuffer, IndexBuffer

		/// <summary>
		/// Draw geometry by indexing into the vertex buffer.
		/// </summary>
		/// <param name="primitiveType">The type of primitives in the index buffer.</param>
		/// <param name="baseVertex">
		/// Used to offset the vertex range indexed from the vertex buffer.
		/// </param>
		/// <param name="minVertexIndex">
		/// A hint of the lowest vertex indexed relative to baseVertex.
		/// </param>
		/// <param name="numVertices">An hint of the maximum vertex indexed.</param>
		/// <param name="startIndex">
		/// The index within the index buffer to start drawing from.
		/// </param>
		/// <param name="primitiveCount">
		/// The number of primitives to render from the index buffer.
		/// </param>
		/// <remarks>
		/// Note that minVertexIndex and numVertices are unused in MonoGame and will be ignored.
		/// </remarks>
		public void DrawIndexedPrimitives(
			PrimitiveType primitiveType,
			int baseVertex,
			int minVertexIndex,
			int numVertices,
			int startIndex,
			int primitiveCount
		) {
			// Flush the GL state before moving on!
			ApplyState();

			// Unsigned short or unsigned int?
			bool shortIndices = Indices.IndexElementSize == IndexElementSize.SixteenBits;

			// Bind the index buffer
			GLDevice.BindIndexBuffer(Indices.Handle);

			// Set up the vertex buffers
			GLDevice.ApplyVertexAttributes(
				vertexBufferBindings,
				vertexBufferCount,
				baseVertex
			);

			// Draw!
			GLDevice.glDrawRangeElements(
				PrimitiveTypeGL(primitiveType),
				minVertexIndex,
				minVertexIndex + numVertices - 1,
				GetElementCountArray(primitiveType, primitiveCount),
				shortIndices ?
					OpenGLDevice.GLenum.GL_UNSIGNED_SHORT :
					OpenGLDevice.GLenum.GL_UNSIGNED_INT,
				(IntPtr) (startIndex * (shortIndices ? 2 : 4))
			);
		}

		public void DrawInstancedPrimitives(
			PrimitiveType primitiveType,
			int baseVertex,
			int minVertexIndex,
			int numVertices,
			int startIndex,
			int primitiveCount,
			int instanceCount
		) {
			// Note that minVertexIndex and numVertices are NOT used!

			// If this device doesn't have the support, just explode now before it's too late.
			if (!GLDevice.SupportsHardwareInstancing)
			{
				throw new NoSuitableGraphicsDeviceException("Your hardware does not support hardware instancing!");
			}

			// Flush the GL state before moving on!
			ApplyState();

			// Unsigned short or unsigned int?
			bool shortIndices = Indices.IndexElementSize == IndexElementSize.SixteenBits;

			// Bind the index buffer
			GLDevice.BindIndexBuffer(Indices.Handle);

			// Set up the vertex buffers
			GLDevice.ApplyVertexAttributes(
				vertexBufferBindings,
				vertexBufferCount,
				baseVertex
			);

			// Draw!
			GLDevice.glDrawElementsInstanced(
				PrimitiveTypeGL(primitiveType),
				GetElementCountArray(primitiveType, primitiveCount),
				shortIndices ?
					OpenGLDevice.GLenum.GL_UNSIGNED_SHORT :
					OpenGLDevice.GLenum.GL_UNSIGNED_INT,
				(IntPtr) (startIndex * (shortIndices ? 2 : 4)),
				instanceCount
			);
		}

		#endregion

		#region DrawPrimitives: VertexBuffer, No Indices

		public void DrawPrimitives(PrimitiveType primitiveType, int vertexStart, int primitiveCount)
		{
			// Flush the GL state before moving on!
			ApplyState();

			// Set up the vertex buffers
			GLDevice.ApplyVertexAttributes(
				vertexBufferBindings,
				vertexBufferCount,
				0
			);

			// Draw!
			GLDevice.glDrawArrays(
				PrimitiveTypeGL(primitiveType),
				vertexStart,
				GetElementCountArray(primitiveType, primitiveCount)
			);
		}

		#endregion

		#region DrawPrimitives: Vertex Arrays, Index Arrays

		public void DrawUserIndexedPrimitives<T>(
			PrimitiveType primitiveType,
			T[] vertexData,
			int vertexOffset,
			int numVertices,
			short[] indexData,
			int indexOffset,
			int primitiveCount
		) where T : struct, IVertexType {
			DrawUserIndexedPrimitives<T>(
				primitiveType,
				vertexData,
				vertexOffset,
				numVertices,
				indexData,
				indexOffset,
				primitiveCount,
				VertexDeclarationCache<T>.VertexDeclaration
			);
		}

		public void DrawUserIndexedPrimitives<T>(
			PrimitiveType primitiveType,
			T[] vertexData,
			int vertexOffset,
			int numVertices,
			short[] indexData,
			int indexOffset,
			int primitiveCount,
			VertexDeclaration vertexDeclaration
		) where T : struct {
			// Flush the GL state before moving on!
			ApplyState();

			// Unbind current index buffer.
			GLDevice.BindIndexBuffer(OpenGLDevice.OpenGLIndexBuffer.NullBuffer);

			// Pin the buffers.
			GCHandle vbHandle = GCHandle.Alloc(vertexData, GCHandleType.Pinned);
			GCHandle ibHandle = GCHandle.Alloc(indexData, GCHandleType.Pinned);

			// Setup the vertex declaration to point at the vertex data.
			vertexDeclaration.GraphicsDevice = this;
			GLDevice.ApplyVertexAttributes(
				vertexDeclaration,
				vbHandle.AddrOfPinnedObject(),
				vertexOffset
			);

			// Draw!
			GLDevice.glDrawRangeElements(
				PrimitiveTypeGL(primitiveType),
				0,
				numVertices - 1,
				GetElementCountArray(primitiveType, primitiveCount),
				OpenGLDevice.GLenum.GL_UNSIGNED_SHORT,
				(IntPtr) (ibHandle.AddrOfPinnedObject().ToInt64() + (indexOffset * sizeof(short)))
			);

			// Release the handles.
			ibHandle.Free();
			vbHandle.Free();
		}

		public void DrawUserIndexedPrimitives<T>(
			PrimitiveType primitiveType,
			T[] vertexData,
			int vertexOffset,
			int numVertices,
			int[] indexData,
			int indexOffset,
			int primitiveCount
		) where T : struct, IVertexType {
			DrawUserIndexedPrimitives<T>(
				primitiveType,
				vertexData,
				vertexOffset,
				numVertices,
				indexData,
				indexOffset,
				primitiveCount,
				VertexDeclarationCache<T>.VertexDeclaration
			);
		}

		public void DrawUserIndexedPrimitives<T>(
			PrimitiveType primitiveType,
			T[] vertexData,
			int vertexOffset,
			int numVertices,
			int[] indexData,
			int indexOffset,
			int primitiveCount,
			VertexDeclaration vertexDeclaration
		) where T : struct, IVertexType {
			// Flush the GL state before moving on!
			ApplyState();

			// Unbind current index buffer.
			GLDevice.BindIndexBuffer(OpenGLDevice.OpenGLIndexBuffer.NullBuffer);

			// Pin the buffers.
			GCHandle vbHandle = GCHandle.Alloc(vertexData, GCHandleType.Pinned);
			GCHandle ibHandle = GCHandle.Alloc(indexData, GCHandleType.Pinned);

			// Setup the vertex declaration to point at the vertex data.
			vertexDeclaration.GraphicsDevice = this;
			GLDevice.ApplyVertexAttributes(
				vertexDeclaration,
				vbHandle.AddrOfPinnedObject(),
				vertexOffset
			);

			// Draw!
			GLDevice.glDrawRangeElements(
				PrimitiveTypeGL(primitiveType),
				0,
				numVertices - 1,
				GetElementCountArray(primitiveType, primitiveCount),
				OpenGLDevice.GLenum.GL_UNSIGNED_INT,
				(IntPtr) (ibHandle.AddrOfPinnedObject().ToInt64() + (indexOffset * sizeof(int)))
			);

			// Release the handles.
			ibHandle.Free();
			vbHandle.Free();
		}

		#endregion

		#region DrawPrimitives: Vertex Arrays, No Indices

		public void DrawUserPrimitives<T>(
			PrimitiveType primitiveType,
			T[] vertexData,
			int vertexOffset,
			int primitiveCount
		) where T : struct, IVertexType {
			DrawUserPrimitives(
				primitiveType,
				vertexData,
				vertexOffset,
				primitiveCount,
				VertexDeclarationCache<T>.VertexDeclaration
			);
		}

		public void DrawUserPrimitives<T>(
			PrimitiveType primitiveType,
			T[] vertexData,
			int vertexOffset,
			int primitiveCount,
			VertexDeclaration vertexDeclaration
		) where T : struct {
			// Flush the GL state before moving on!
			ApplyState();

			// Unbind current VBOs.
			GLDevice.BindVertexBuffer(OpenGLDevice.OpenGLVertexBuffer.NullBuffer);

			// Pin the buffers.
			GCHandle vbHandle = GCHandle.Alloc(vertexData, GCHandleType.Pinned);

			// Setup the vertex declaration to point at the vertex data.
			vertexDeclaration.GraphicsDevice = this;
			GLDevice.ApplyVertexAttributes(
				vertexDeclaration,
				vbHandle.AddrOfPinnedObject(),
				0
			);

			// Draw!
			GLDevice.glDrawArrays(
				PrimitiveTypeGL(primitiveType),
				vertexOffset,
				GetElementCountArray(primitiveType, primitiveCount)
			);

			// Release the handles.
			vbHandle.Free();
		}

		#endregion

		#region FNA Extensions

		public void SetStringMarkerEXT(string text)
		{
			GLDevice.SetStringMarker(text);
		}

		#endregion

		#region Private XNA->GL Conversion Methods

		private static int GetElementCountArray(PrimitiveType primitiveType, int primitiveCount)
		{
			switch (primitiveType)
			{
				case PrimitiveType.LineList:
					return primitiveCount * 2;
				case PrimitiveType.LineStrip:
					return primitiveCount + 1;
				case PrimitiveType.TriangleList:
					return primitiveCount * 3;
				case PrimitiveType.TriangleStrip:
					return 3 + (primitiveCount - 1);
			}

			throw new NotSupportedException();
		}

		private static OpenGLDevice.GLenum PrimitiveTypeGL(PrimitiveType primitiveType)
		{
			switch (primitiveType)
			{
				case PrimitiveType.LineList:
					return OpenGLDevice.GLenum.GL_LINES;
				case PrimitiveType.LineStrip:
					return OpenGLDevice.GLenum.GL_LINE_STRIP;
				case PrimitiveType.TriangleList:
					return OpenGLDevice.GLenum.GL_TRIANGLES;
				case PrimitiveType.TriangleStrip:
					return OpenGLDevice.GLenum.GL_TRIANGLE_STRIP;
			}

			throw new ArgumentException("Should be a value defined in PrimitiveType", "primitiveType");
		}

		#endregion

		#region Private State Flush Methods

		private void ApplyState()
		{
			// Apply RasterizerState now, as it depends on other device states
			GLDevice.ApplyRasterizerState(
				RasterizerState,
				RenderTargetCount > 0
			);

			while (ModifiedSamplers.Count > 0)
			{
				int sampler = ModifiedSamplers.Dequeue();
				GLDevice.VerifySampler(
					sampler,
					Textures[sampler],
					SamplerStates[sampler]
				);
			}
		}

		#endregion
	}
}
