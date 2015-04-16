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

using OpenTK.Graphics.OpenGL;
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

		public bool IsContentLost
		{
			get
			{
				/* We will just return IsDisposed, as that
				 * is the only case I can see for now.
				 */
				return IsDisposed;
			}
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
						OpenGLDevice.Instance.Backbuffer.Width,
						OpenGLDevice.Instance.Backbuffer.Height,
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

		public BlendState BlendState
		{
			get;
			set;
		}

		public DepthStencilState DepthStencilState
		{
			get;
			set;
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

				if (RenderTargetCount == 0)
				{
					value.Y = Viewport.Height - ScissorRectangle.Y - ScissorRectangle.Height;
				}

				OpenGLDevice.Instance.ScissorRectangle.Set(value);
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

				if (RenderTargetCount == 0)
				{
					value.Y = PresentationParameters.BackBufferHeight - value.Y - value.Height;
				}

				OpenGLDevice.Instance.GLViewport.Set(value.Bounds);
				OpenGLDevice.Instance.DepthRangeMin.Set(value.MinDepth);
				OpenGLDevice.Instance.DepthRangeMax.Set(value.MaxDepth);

				/* In OpenGL we have to re-apply the special "posFixup"
				 * vertex shader uniform if the viewport changes.
				 */
				vertexShaderDirty = true;
			}
		}

		#endregion

		#region Public RenderTarget Properties

		public int RenderTargetCount
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

		private readonly RenderTargetBinding[] renderTargetBindings = new RenderTargetBinding[4];

		#endregion

		#region Private Buffer Object Variables

		private VertexBufferBinding[] vertexBufferBindings;

		#endregion

		#region Shader "Stuff" (Here Be Dragons)

		private bool vertexShaderDirty;
		private bool pixelShaderDirty;

		private ShaderProgram shaderProgram;
		private readonly ShaderProgramCache programCache = new ShaderProgramCache();

		private readonly ConstantBufferCollection vertexConstantBuffers = new ConstantBufferCollection(ShaderStage.Vertex, 16);
		private readonly ConstantBufferCollection pixelConstantBuffers = new ConstantBufferCollection(ShaderStage.Pixel, 16);

		private static readonly float[] posFixup = new float[4];

		private Shader INTERNAL_vertexShader;
		internal Shader VertexShader
		{
			get
			{
				return INTERNAL_vertexShader;
			}
			set
			{
				if (value == INTERNAL_vertexShader)
				{
					return;
				}
				INTERNAL_vertexShader = value;
				vertexShaderDirty = true;
			}
		}

		private Shader INTERNAL_pixelShader;
		internal Shader PixelShader
		{
			get
			{
				return INTERNAL_pixelShader;
			}
			set
			{
				if (value == INTERNAL_pixelShader)
				{
					return;
				}
				INTERNAL_pixelShader = value;
				pixelShaderDirty = true;
			}
		}

		internal void SetConstantBuffer(ShaderStage stage, int slot, ConstantBuffer buffer)
		{
			if (stage == ShaderStage.Vertex)
			{
				vertexConstantBuffers[slot] = buffer;
			}
			else
			{
				pixelConstantBuffers[slot] = buffer;
			}
		}

		#endregion

		#region GraphicsDevice Events

		public event EventHandler<EventArgs> DeviceLost;
		public event EventHandler<EventArgs> DeviceReset;
		public event EventHandler<EventArgs> DeviceResetting;
		public event EventHandler<ResourceCreatedEventArgs> ResourceCreated;
		public event EventHandler<ResourceDestroyedEventArgs> ResourceDestroyed;
		public event EventHandler<EventArgs> Disposing;

		internal void OnDeviceLost()
		{
			if (DeviceLost != null)
			{
				DeviceLost(this, EventArgs.Empty);
			}
		}

		internal void OnDeviceReset()
		{
			if (DeviceReset != null)
			{
				DeviceReset(this, EventArgs.Empty);
			}
		}

		internal void OnDeviceResetting()
		{
			if (DeviceResetting != null)
			{
				DeviceResetting(this, EventArgs.Empty);
			}

			// FIXME: What, why, why -flibit
			// GraphicsResource.DoGraphicsDeviceResetting();
		}

		internal void OnResourceCreated()
		{
			if (ResourceCreated != null)
			{
				ResourceCreated(this, (ResourceCreatedEventArgs) EventArgs.Empty);
			}
		}

		internal void OnResourceDestroyed()
		{
			if (ResourceDestroyed != null)
			{
				ResourceDestroyed(this, (ResourceDestroyedEventArgs) EventArgs.Empty);
			}
		}

		internal void OnDisposing()
		{
			if (Disposing != null)
			{
				Disposing(this, EventArgs.Empty);
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
		public GraphicsDevice(GraphicsAdapter adapter, GraphicsProfile graphicsProfile, PresentationParameters presentationParameters)
		{
			if (presentationParameters == null)
			{
				throw new ArgumentNullException("presentationParameters");
			}

			// Set the properties from the constructor parameters.
			Adapter = adapter;
			PresentationParameters = presentationParameters;
			GraphicsProfile = graphicsProfile;

			// Force set the default render states.
			BlendState = BlendState.Opaque;
			DepthStencilState = DepthStencilState.Default;
			RasterizerState = RasterizerState.CullCounterClockwise;

			// Initialize the Texture/Sampler state containers
			Textures = new TextureCollection(OpenGLDevice.Instance.MaxTextureSlots);
			SamplerStates = new SamplerStateCollection(OpenGLDevice.Instance.MaxTextureSlots);
			Textures.Clear();
			SamplerStates.Clear();

			// Clear constant buffers
			vertexConstantBuffers.Clear();
			pixelConstantBuffers.Clear();

			// Force set the buffers and shaders on next ApplyState() call
			vertexBufferBindings = new VertexBufferBinding[OpenGLDevice.Instance.MaxVertexAttributes];

			// First draw will need to set the shaders.
			vertexShaderDirty = true;
			pixelShaderDirty = true;

			// Set the default scissor rect.
			ScissorRectangle = Viewport.Bounds;

			// Free all the cached shader programs.
			programCache.Clear();
			shaderProgram = null;
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
					// Invoke the Disposing Event
					OnDisposing();

					/* Dispose of all remaining graphics resources before
					 * disposing of the GraphicsDevice.
					 */
					GraphicsResource.DisposeAll();

					// Free all the cached shader programs.
					programCache.Dispose();
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
			OpenGLDevice.Instance.Clear(
				options,
				color,
				depth,
				stencil
			);
		}

		#endregion

		#region Public Present Method

		public void Present()
		{
			GL.Flush();

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
		}

		#endregion

		#region Public Backbuffer Methods

		public void GetBackBufferData<T>(T[] data) where T : struct
		{
			// Store off the old frame buffer components
			int prevReadBuffer = OpenGLDevice.Framebuffer.CurrentReadFramebuffer;

			OpenGLDevice.Framebuffer.BindReadFramebuffer(OpenGLDevice.Instance.Backbuffer.Handle);
			GL.ReadPixels(
				0, 0,
				OpenGLDevice.Instance.Backbuffer.Width,
				OpenGLDevice.Instance.Backbuffer.Height,
				PixelFormat.Rgba,
				PixelType.UnsignedByte,
				data
			);

			// Restore old buffer components
			OpenGLDevice.Framebuffer.BindReadFramebuffer(prevReadBuffer);

			// Now we get to do a software-based flip! Yes, really! -flibit
			int width = OpenGLDevice.Instance.Backbuffer.Width;
			int height = OpenGLDevice.Instance.Backbuffer.Height;
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
						renderTargets[i].ArraySlice != renderTargetBindings[i].ArraySlice	)
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
				OpenGLDevice.Instance.SetRenderTargets(null, 0, DepthFormat.None);

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
				// TODO: CubeMapFace, other target types??
				int[] glTarget = new int[renderTargets.Length];
				for (int i = 0; i < renderTargets.Length; i += 1)
				{
					glTarget[i] = renderTargets[i].RenderTarget.texture.Handle;
				}
				RenderTarget2D target = (RenderTarget2D) renderTargets[0].RenderTarget;
				OpenGLDevice.Instance.SetRenderTargets(glTarget, target.glDepthStencilBuffer, target.DepthStencilFormat);

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

		public void GetRenderTargets(RenderTargetBinding[] outTargets)
		{
			Array.Copy(renderTargetBindings, outTargets, RenderTargetCount);
		}

		#endregion

		#region Public Buffer Object Methods

		public void SetVertexBuffer(VertexBuffer vertexBuffer)
		{
			if (!ReferenceEquals(vertexBufferBindings[0].VertexBuffer, vertexBuffer))
			{
				vertexBufferBindings[0] = new VertexBufferBinding(vertexBuffer);
			}

			for (int vertexStreamSlot = 1; vertexStreamSlot < vertexBufferBindings.Length; vertexStreamSlot += 1)
			{
				if (vertexBufferBindings[vertexStreamSlot].VertexBuffer != null)
				{
					vertexBufferBindings[vertexStreamSlot] = VertexBufferBinding.None;
				}
			}
		}

		public void SetVertexBuffers(params VertexBufferBinding[] vertexBuffers)
		{
			if ((vertexBuffers != null) && (vertexBuffers.Length > vertexBufferBindings.Length))
			{
				throw new ArgumentOutOfRangeException("vertexBuffers", String.Format("Max Vertex Buffers supported is {0}", vertexBufferBindings.Length));
			}

			// Set vertex buffers if they are different
			int slot = 0;
			if (vertexBuffers != null)
			{
				for (; slot < vertexBuffers.Length; slot += 1)
				{
					if (!ReferenceEquals(vertexBufferBindings[slot].VertexBuffer, vertexBuffers[slot].VertexBuffer)
						|| (vertexBufferBindings[slot].VertexOffset != vertexBuffers[slot].VertexOffset)
						|| (vertexBufferBindings[slot].InstanceFrequency != vertexBuffers[slot].InstanceFrequency))
					{
						vertexBufferBindings[slot] = vertexBuffers[slot];
					}
				}
			}

			// Unset any unused vertex buffers
			for (; slot < vertexBufferBindings.Length; slot += 1)
			{
				if (vertexBufferBindings[slot].VertexBuffer != null)
				{
					vertexBufferBindings[slot] = new VertexBufferBinding(null);
				}
			}
		}

		#endregion

		#region DrawPrimitives: VertexBuffer and IndexBuffer

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

			// Set up the vertex buffers.
			foreach (VertexBufferBinding vertBuffer in vertexBufferBindings)
			{
				if (vertBuffer.VertexBuffer != null)
				{
					OpenGLDevice.Instance.BindVertexBuffer(vertBuffer.VertexBuffer.Handle);
					vertBuffer.VertexBuffer.VertexDeclaration.Apply(
						VertexShader,
						(IntPtr) (vertBuffer.VertexBuffer.VertexDeclaration.VertexStride * (vertBuffer.VertexOffset + baseVertex))
					);
				}
			}

			// Enable the appropriate vertex attributes.
			OpenGLDevice.Instance.FlushGLVertexAttributes();

			// Bind the index buffer
			OpenGLDevice.Instance.BindIndexBuffer(Indices.Handle);

			// Draw!
			GL.DrawRangeElements(
				PrimitiveTypeGL(primitiveType),
				minVertexIndex,
				minVertexIndex + numVertices,
				GetElementCountArray(primitiveType, primitiveCount),
				shortIndices ? DrawElementsType.UnsignedShort : DrawElementsType.UnsignedInt,
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
			if (!OpenGLDevice.Instance.SupportsHardwareInstancing)
			{
				throw new NoSuitableGraphicsDeviceException("Your hardware does not support hardware instancing!");
			}

			// Flush the GL state before moving on!
			ApplyState();

			// Unsigned short or unsigned int?
			bool shortIndices = Indices.IndexElementSize == IndexElementSize.SixteenBits;

			// Set up the vertex buffers
			foreach (VertexBufferBinding vertBuffer in vertexBufferBindings)
			{
				if (vertBuffer.VertexBuffer != null)
				{
					OpenGLDevice.Instance.BindVertexBuffer(vertBuffer.VertexBuffer.Handle);
					vertBuffer.VertexBuffer.VertexDeclaration.Apply(
						VertexShader,
						(IntPtr) (vertBuffer.VertexBuffer.VertexDeclaration.VertexStride * (vertBuffer.VertexOffset + baseVertex)),
						vertBuffer.InstanceFrequency
					);
				}
			}

			// Enable the appropriate vertex attributes.
			OpenGLDevice.Instance.FlushGLVertexAttributes();

			// Bind the index buffer
			OpenGLDevice.Instance.BindIndexBuffer(Indices.Handle);

			// Draw!
			GL.DrawElementsInstanced(
				PrimitiveTypeGL(primitiveType),
				GetElementCountArray(primitiveType, primitiveCount),
				shortIndices ? DrawElementsType.UnsignedShort : DrawElementsType.UnsignedInt,
				(IntPtr) (startIndex * (shortIndices ? 2 : 4)),
				instanceCount
			);
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
			OpenGLDevice.Instance.BindVertexBuffer(OpenGLDevice.OpenGLVertexBuffer.NullBuffer);

			// Pin the buffers.
			GCHandle vbHandle = GCHandle.Alloc(vertexData, GCHandleType.Pinned);

			// Setup the vertex declaration to point at the VB data.
			vertexDeclaration.GraphicsDevice = this;
			vertexDeclaration.Apply(VertexShader, vbHandle.AddrOfPinnedObject());

			// Enable the appropriate vertex attributes.
			OpenGLDevice.Instance.FlushGLVertexAttributes();

			// Draw!
			GL.DrawArrays(
				PrimitiveTypeGL(primitiveType),
				vertexOffset,
				GetElementCountArray(primitiveType, primitiveCount)
			);

			// Release the handles.
			vbHandle.Free();
		}

		#endregion

		#region DrawPrimitives: Vertex Buffer, No Indices

		public void DrawPrimitives(PrimitiveType primitiveType, int vertexStart, int primitiveCount)
		{
			// Flush the GL state before moving on!
			ApplyState();

			// Set up the vertex buffers.
			foreach (VertexBufferBinding vertBuffer in vertexBufferBindings)
			{
				if (vertBuffer.VertexBuffer != null)
				{
					OpenGLDevice.Instance.BindVertexBuffer(vertBuffer.VertexBuffer.Handle);
					vertBuffer.VertexBuffer.VertexDeclaration.Apply(
						VertexShader,
						(IntPtr) (vertBuffer.VertexBuffer.VertexDeclaration.VertexStride * vertBuffer.VertexOffset)
					);
				}
			}

			// Enable the appropriate vertex attributes.
			OpenGLDevice.Instance.FlushGLVertexAttributes();

			// Draw!
			GL.DrawArrays(
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

			// Unbind current buffer objects.
			OpenGLDevice.Instance.BindVertexBuffer(OpenGLDevice.OpenGLVertexBuffer.NullBuffer);
			OpenGLDevice.Instance.BindIndexBuffer(OpenGLDevice.OpenGLIndexBuffer.NullBuffer);

			// Pin the buffers.
			GCHandle vbHandle = GCHandle.Alloc(vertexData, GCHandleType.Pinned);
			GCHandle ibHandle = GCHandle.Alloc(indexData, GCHandleType.Pinned);

			// Setup the vertex declaration to point at the VB data.
			vertexDeclaration.GraphicsDevice = this;
			vertexDeclaration.Apply(
				VertexShader,
				(IntPtr) (vbHandle.AddrOfPinnedObject().ToInt64() + vertexDeclaration.VertexStride * vertexOffset)
			);

			// Enable the appropriate vertex attributes.
			OpenGLDevice.Instance.FlushGLVertexAttributes();

			// Draw!
			GL.DrawRangeElements(
				PrimitiveTypeGL(primitiveType),
				0,
				numVertices,
				GetElementCountArray(primitiveType, primitiveCount),
				DrawElementsType.UnsignedShort,
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

			// Unbind current buffer objects.
			OpenGLDevice.Instance.BindVertexBuffer(OpenGLDevice.OpenGLVertexBuffer.NullBuffer);
			OpenGLDevice.Instance.BindIndexBuffer(OpenGLDevice.OpenGLIndexBuffer.NullBuffer);

			// Pin the buffers.
			GCHandle vbHandle = GCHandle.Alloc(vertexData, GCHandleType.Pinned);
			GCHandle ibHandle = GCHandle.Alloc(indexData, GCHandleType.Pinned);

			// Setup the vertex declaration to point at the VB data.
			vertexDeclaration.GraphicsDevice = this;
			vertexDeclaration.Apply(
				VertexShader,
				(IntPtr) (vbHandle.AddrOfPinnedObject().ToInt64() + vertexDeclaration.VertexStride * vertexOffset)
			);

			// Enable the appropriate vertex attributes.
			OpenGLDevice.Instance.FlushGLVertexAttributes();

			// Draw!
			GL.DrawRangeElements(
				PrimitiveTypeGL(primitiveType),
				0,
				numVertices,
				GetElementCountArray(primitiveType, primitiveCount),
				DrawElementsType.UnsignedInt,
				(IntPtr) (ibHandle.AddrOfPinnedObject().ToInt64() + (indexOffset * sizeof(int)))
			);

			// Release the handles.
			ibHandle.Free();
			vbHandle.Free();
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

		private static BeginMode PrimitiveTypeGL(PrimitiveType primitiveType)
		{
			switch (primitiveType)
			{
				case PrimitiveType.LineList:
					return BeginMode.Lines;
				case PrimitiveType.LineStrip:
					return BeginMode.LineStrip;
				case PrimitiveType.TriangleList:
					return BeginMode.Triangles;
				case PrimitiveType.TriangleStrip:
					return BeginMode.TriangleStrip;
			}

			throw new ArgumentException("Should be a value defined in PrimitiveType", "primitiveType");
		}

		#endregion

		#region Private State Flush Methods

		private void ApplyState()
		{
			// Apply BlendState
			OpenGLDevice.Instance.AlphaBlendEnable.Set(
				!(	BlendState.ColorSourceBlend == Blend.One &&
					BlendState.ColorDestinationBlend == Blend.Zero &&
					BlendState.AlphaSourceBlend == Blend.One &&
					BlendState.AlphaDestinationBlend == Blend.Zero	)
			);
			OpenGLDevice.Instance.BlendColor.Set(BlendState.BlendFactor);
			OpenGLDevice.Instance.BlendOp.Set(BlendState.ColorBlendFunction);
			OpenGLDevice.Instance.BlendOpAlpha.Set(BlendState.AlphaBlendFunction);
			OpenGLDevice.Instance.SrcBlend.Set(BlendState.ColorSourceBlend);
			OpenGLDevice.Instance.DstBlend.Set(BlendState.ColorDestinationBlend);
			OpenGLDevice.Instance.SrcBlendAlpha.Set(BlendState.AlphaSourceBlend);
			OpenGLDevice.Instance.DstBlendAlpha.Set(BlendState.AlphaDestinationBlend);
			OpenGLDevice.Instance.ColorWriteEnable.Set(BlendState.ColorWriteChannels);

			// Apply DepthStencilState
			OpenGLDevice.Instance.ZEnable.Set(DepthStencilState.DepthBufferEnable);
			OpenGLDevice.Instance.ZWriteEnable.Set(DepthStencilState.DepthBufferWriteEnable);
			OpenGLDevice.Instance.DepthFunc.Set(DepthStencilState.DepthBufferFunction);
			OpenGLDevice.Instance.StencilEnable.Set(DepthStencilState.StencilEnable);
			OpenGLDevice.Instance.StencilWriteMask.Set(DepthStencilState.StencilWriteMask);
			OpenGLDevice.Instance.SeparateStencilEnable.Set(DepthStencilState.TwoSidedStencilMode);
			OpenGLDevice.Instance.StencilRef.Set(DepthStencilState.ReferenceStencil);
			OpenGLDevice.Instance.StencilMask.Set(DepthStencilState.StencilMask);
			OpenGLDevice.Instance.StencilFunc.Set(DepthStencilState.StencilFunction);
			OpenGLDevice.Instance.CCWStencilFunc.Set(DepthStencilState.CounterClockwiseStencilFunction);
			OpenGLDevice.Instance.StencilFail.Set(DepthStencilState.StencilFail);
			OpenGLDevice.Instance.StencilZFail.Set(DepthStencilState.StencilDepthBufferFail);
			OpenGLDevice.Instance.StencilPass.Set(DepthStencilState.StencilPass);
			OpenGLDevice.Instance.CCWStencilFail.Set(DepthStencilState.CounterClockwiseStencilFail);
			OpenGLDevice.Instance.CCWStencilZFail.Set(DepthStencilState.CounterClockwiseStencilDepthBufferFail);
			OpenGLDevice.Instance.CCWStencilPass.Set(DepthStencilState.CounterClockwiseStencilPass);

			// Apply RasterizerState
			if (RenderTargetCount > 0)
			{
				OpenGLDevice.Instance.CullFrontFace.Set(RasterizerState.CullMode);
			}
			else
			{
				// When not rendering offscreen the faces change order.
				if (RasterizerState.CullMode == CullMode.None)
				{
					OpenGLDevice.Instance.CullFrontFace.Set(RasterizerState.CullMode);
				}
				else
				{
					OpenGLDevice.Instance.CullFrontFace.Set(
						RasterizerState.CullMode == CullMode.CullClockwiseFace ?
							CullMode.CullCounterClockwiseFace :
							CullMode.CullClockwiseFace
					);
				}
			}
			OpenGLDevice.Instance.GLFillMode.Set(RasterizerState.FillMode);
			OpenGLDevice.Instance.ScissorTestEnable.Set(RasterizerState.ScissorTestEnable);
			OpenGLDevice.Instance.DepthBias.Set(RasterizerState.DepthBias);
			OpenGLDevice.Instance.SlopeScaleDepthBias.Set(RasterizerState.SlopeScaleDepthBias);

			// TODO: MSAA?

			if (VertexShader == null)
			{
				throw new InvalidOperationException("A vertex shader must be set!");
			}
			if (PixelShader == null)
			{
				throw new InvalidOperationException("A pixel shader must be set!");
			}

			if (vertexShaderDirty || pixelShaderDirty)
			{
				ActivateShaderProgram();
				vertexShaderDirty = pixelShaderDirty = false;
			}

			vertexConstantBuffers.SetConstantBuffers(this, shaderProgram);
			pixelConstantBuffers.SetConstantBuffers(this, shaderProgram);

			// Apply Textures/Samplers
			for (int i = 0; i < OpenGLDevice.Instance.MaxTextureSlots; i += 1)
			{
				SamplerState sampler = SamplerStates[i];
				Texture texture = Textures[i];

				if (sampler != null && texture != null)
				{
					OpenGLDevice.Instance.Samplers[i].Texture.Set(texture.texture);
					OpenGLDevice.Instance.Samplers[i].Target.Set(texture.texture.Target);
					texture.texture.WrapS.Set(sampler.AddressU);
					texture.texture.WrapT.Set(sampler.AddressV);
					texture.texture.WrapR.Set(sampler.AddressW);
					texture.texture.Filter.Set(sampler.Filter);
					texture.texture.Anistropy.Set(sampler.MaxAnisotropy);
					texture.texture.MaxMipmapLevel.Set(sampler.MaxMipLevel);
					texture.texture.LODBias.Set(sampler.MipMapLevelOfDetailBias);
				}
				else if (texture == null)
				{
					OpenGLDevice.Instance.Samplers[i].Texture.Set(OpenGLDevice.OpenGLTexture.NullTexture);
				}
			}

			// Flush the GL state!
			OpenGLDevice.Instance.FlushGLState();
		}

		/// <summary>
		/// Activates the Current Vertex/Pixel shader pair into a program.
		/// </summary>
		private void ActivateShaderProgram()
		{
			// Lookup the shader program.
			ShaderProgram program = programCache.GetProgram(VertexShader, PixelShader);
			if (program.Program == -1)
			{
				return;
			}

			// Set the new program if it has changed.
			if (shaderProgram != program)
			{
				GL.UseProgram(program.Program);
				shaderProgram = program;
			}

			int posFixupLoc = shaderProgram.GetUniformLocation("posFixup");
			if (posFixupLoc == -1)
			{
				return;
			}

			/* Apply vertex shader fix:
			 * The following two lines are appended to the end of vertex shaders
			 * to account for rendering differences between OpenGL and DirectX:
			 * 
			 * gl_Position.y = gl_Position.y * posFixup.y;
			 * gl_Position.xy += posFixup.zw * gl_Position.ww;
			 * 
			 * (the following paraphrased from wine, wined3d/state.c and
			 * wined3d/glsl_shader.c)
			 * 
			 * - We need to flip along the y-axis in case of offscreen rendering.
			 * - D3D coordinates refer to pixel centers while GL coordinates refer
			 *   to pixel corners.
			 * - D3D has a top-left filling convention. We need to maintain this
			 *   even after the y-flip mentioned above.
			 * 
			 * In order to handle the last two points, we translate by
			 * (63.0 / 128.0) / VPw and (63.0 / 128.0) / VPh. This is equivalent to
			 * translating slightly less than half a pixel. We want the difference to
			 * be large enough that it doesn't get lost due to rounding inside the
			 * driver, but small enough to prevent it from interfering with any
			 * anti-aliasing.
			 * 
			 * OpenGL coordinates specify the center of the pixel while d3d coords
			 * specify the corner. The offsets are stored in z and w in posFixup.
			 * posFixup.y contains 1.0 or -1.0 to turn the rendering upside down for
			 * offscreen rendering.
			 */

			posFixup[0] = 1.0f;
			posFixup[1] = 1.0f;
			posFixup[2] = (63.0f / 64.0f) / Viewport.Width;
			posFixup[3] = -(63.0f / 64.0f) / Viewport.Height;

			// Flip vertically if we have a render target bound (rendering offscreen)
			if (RenderTargetCount > 0)
			{
				posFixup[1] *= -1.0f;
				posFixup[3] *= -1.0f;
			}

			GL.Uniform4(posFixupLoc, 1, posFixup);
		}

		#endregion
	}
}
