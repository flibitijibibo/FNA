#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

#region DISABLE_FAUXBACKBUFFER Option
// #define DISABLE_FAUXBACKBUFFER
/* If you want to debug GL without the extra FBO in your way, you can use this.
 * Additionally, if you always use the desktop resolution in fullscreen mode,
 * you can use this to optimize your game and even lower the GL requirements.
 *
 * Note that this also affects OpenGLDevice_GL.cs!
 * Check DISABLE_FAUXBACKBUFFER there too.
 * -flibit
 */
#endregion

#region THREADED_GL Option
// #define THREADED_GL
/* Ah, so I see you've run into some issues with threaded GL...
 *
 * This class is designed to handle rendering coming from multiple threads, but
 * if you're too wreckless with how many threads are calling the GL, this will
 * hang.
 *
 * With THREADED_GL we instead allow you to run threaded rendering using
 * multiple GL contexts. This is more flexible, but much more dangerous.
 *
 * Basically, if you have to enable this, you should feel very bad.
 * -flibit
 */
#endregion

#region DISABLE_THREADING Option
// #define DISABLE_THREADING
/* Perhaps you read the above option and thought to yourself:
 * "Wow, only an idiot would need threads for their graphics code!"
 *
 * For those of you who are particularly well-behaved with your renderer and
 * don't ever call anything on a thread at all, you can enable this define and
 * cut out a _ton_ of garbage generation that's caused by our attempt to force
 * things to the main thread.
 *
 * Enjoy the boost, you've earned it.
 * -flibit
 */
#endregion

#region Using Statements
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

using SDL2;
#endregion

namespace Microsoft.Xna.Framework.Graphics
{
	internal partial class OpenGLDevice : IGLDevice
	{
		#region OpenGL Texture Container Class

		private class OpenGLTexture : IGLTexture
		{
			public uint Handle
			{
				get;
				private set;
			}

			public GLenum Target
			{
				get;
				private set;
			}

			public bool HasMipmaps
			{
				get;
				private set;
			}

			public TextureAddressMode WrapS;
			public TextureAddressMode WrapT;
			public TextureAddressMode WrapR;
			public TextureFilter Filter;
			public float Anistropy;
			public int MaxMipmapLevel;
			public float LODBias;

			public OpenGLTexture(
				uint handle,
				Type target,
				int levelCount
			) {
				Handle = handle;
				Target = XNAToGL.TextureType[target];
				HasMipmaps = levelCount > 1;

				WrapS = TextureAddressMode.Wrap;
				WrapT = TextureAddressMode.Wrap;
				WrapR = TextureAddressMode.Wrap;
				Filter = TextureFilter.Linear;
				Anistropy = 4.0f;
				MaxMipmapLevel = 0;
				LODBias = 0.0f;
			}

			// We can't set a SamplerState Texture to null, so use this.
			private OpenGLTexture()
			{
				Handle = 0;
				Target = GLenum.GL_TEXTURE_2D; // FIXME: Assumption! -flibit
			}
			public static readonly OpenGLTexture NullTexture = new OpenGLTexture();
		}

		#endregion

		#region OpenGL Renderbuffer Container Class

		private class OpenGLRenderbuffer : IGLRenderbuffer
		{
			public uint Handle
			{
				get;
				private set;
			}

			public OpenGLRenderbuffer(uint handle)
			{
				Handle = handle;
			}
		}

		#endregion

		#region OpenGL Buffer Container Class

		private class OpenGLBuffer : IGLBuffer
		{
			public uint Handle
			{
				get;
				private set;
			}

			public IntPtr BufferSize
			{
				get;
				private set;
			}

			public GLenum Dynamic
			{
				get;
				private set;
			}

			public OpenGLBuffer(
				uint handle,
				IntPtr bufferSize,
				GLenum dynamic
			) {
				Handle = handle;
				BufferSize = bufferSize;
				Dynamic = dynamic;
			}

			private OpenGLBuffer()
			{
				Handle = 0;
			}
			public static readonly OpenGLBuffer NullBuffer = new OpenGLBuffer();
		}

		#endregion

		#region OpenGL Effect Container Class

		private class OpenGLEffect : IGLEffect
		{
			public IntPtr EffectData
			{
				get;
				private set;
			}

			public IntPtr GLEffectData
			{
				get;
				private set;
			}

			public OpenGLEffect(IntPtr effect, IntPtr glEffect)
			{
				EffectData = effect;
				GLEffectData = glEffect;
			}
		}

		#endregion

		#region OpenGL Query Container Class

		private class OpenGLQuery : IGLQuery
		{
			public uint Handle
			{
				get;
				private set;
			}

			public OpenGLQuery(uint handle)
			{
				Handle = handle;
			}
		}

		#endregion

		#region Alpha Blending State Variables

		public Color BlendFactor
		{
			get
			{
				return blendColor;
			}
			set
			{
				if (value != blendColor)
				{
					blendColor = value;
					glBlendColor(
						blendColor.R / 255.0f,
						blendColor.G / 255.0f,
						blendColor.B / 255.0f,
						blendColor.A / 255.0f
					);
				}
			}
		}

		private bool alphaBlendEnable = false;
		private Color blendColor = Color.Transparent;
		private BlendFunction blendOp = BlendFunction.Add;
		private BlendFunction blendOpAlpha = BlendFunction.Add;
		private Blend srcBlend = Blend.One;
		private Blend dstBlend = Blend.Zero;
		private Blend srcBlendAlpha = Blend.One;
		private Blend dstBlendAlpha = Blend.Zero;
		private ColorWriteChannels colorWriteEnable = ColorWriteChannels.All;
		private ColorWriteChannels colorWriteEnable1 = ColorWriteChannels.All;
		private ColorWriteChannels colorWriteEnable2 = ColorWriteChannels.All;
		private ColorWriteChannels colorWriteEnable3 = ColorWriteChannels.All;

		#endregion

		#region Depth State Variables

		private bool zEnable = false;
		private bool zWriteEnable = false;
		private CompareFunction depthFunc = CompareFunction.Less;

		#endregion

		#region Stencil State Variables

		public int ReferenceStencil
		{
			get
			{
				return stencilRef;
			}
			set
			{
				if (value != stencilRef)
				{
					stencilRef = value;
					if (separateStencilEnable)
					{
						glStencilFuncSeparate(
							GLenum.GL_FRONT,
							XNAToGL.CompareFunc[stencilFunc],
							stencilRef,
							stencilMask
						);
						glStencilFuncSeparate(
							GLenum.GL_BACK,
							XNAToGL.CompareFunc[ccwStencilFunc],
							stencilRef,
							stencilMask
						);
					}
					else
					{
						glStencilFunc(
							XNAToGL.CompareFunc[stencilFunc],
							stencilRef,
							stencilMask
						);
					}
				}
			}
		}

		private bool stencilEnable = false;
		private int stencilWriteMask = -1; // AKA 0xFFFFFFFF, ugh -flibit
		private bool separateStencilEnable = false;
		private int stencilRef = 0;
		private int stencilMask = -1; // AKA 0xFFFFFFFF, ugh -flibit
		private CompareFunction stencilFunc = CompareFunction.Always;
		private StencilOperation stencilFail = StencilOperation.Keep;
		private StencilOperation stencilZFail = StencilOperation.Keep;
		private StencilOperation stencilPass = StencilOperation.Keep;
		private CompareFunction ccwStencilFunc = CompareFunction.Always;
		private StencilOperation ccwStencilFail = StencilOperation.Keep;
		private StencilOperation ccwStencilZFail = StencilOperation.Keep;
		private StencilOperation ccwStencilPass = StencilOperation.Keep;

		#endregion

		#region Rasterizer State Variables

		private bool scissorTestEnable = false;
		private CullMode cullFrontFace = CullMode.None;
		private FillMode fillMode = FillMode.Solid;
		private float depthBias = 0.0f;
		private float slopeScaleDepthBias = 0.0f;

		#endregion

		#region Viewport State Variables

		/* These two aren't actually empty rects by default in OpenGL,
		 * but we don't _really_ know the starting window size, so
		 * force apply this when the GraphicsDevice is initialized.
		 * -flibit
		 */
		private Rectangle scissorRectangle =  new Rectangle(
			0,
			0,
			0,
			0
		);
		private Rectangle viewport = new Rectangle(
			0,
			0,
			0,
			0
		);
		private float depthRangeMin = 0.0f;
		private float depthRangeMax = 1.0f;

		#endregion

		#region Texture Collection Variables

		private OpenGLTexture[] Textures;

		#endregion

		#region Buffer Binding Cache Variables

		private uint currentVertexBuffer = 0;
		private uint currentIndexBuffer = 0;

		// ld, or LastDrawn, effect/vertex attributes
		private int ldBaseVertex = -1;
		private VertexDeclaration ldVertexDeclaration = null;
		private IntPtr ldPointer = IntPtr.Zero;
		private IntPtr ldEffect = IntPtr.Zero;
		private IntPtr ldTechnique = IntPtr.Zero;
		private uint ldPass = 0;

		#endregion

		#region Render Target Cache Variables

		private uint currentReadFramebuffer = 0;
		private uint currentDrawFramebuffer = 0;
		private uint targetFramebuffer = 0;
		private uint[] currentAttachments;
		private GLenum[] currentAttachmentFaces;
		private int currentDrawBuffers;
		private GLenum[] drawBuffersArray;
		private uint currentRenderbuffer;
		private DepthFormat currentDepthStencilFormat;

		#endregion

		#region Clear Cache Variables

		private Vector4 currentClearColor = new Vector4(0, 0, 0, 0);
		private float currentClearDepth = 1.0f;
		private int currentClearStencil = 0;

		#endregion

		#region Private OpenGL Context Variable

		private IntPtr glContext;

		#endregion

		#region Faux-Backbuffer Variable

		private OpenGLBackbuffer backbuffer;
		public IGLBackbuffer Backbuffer
		{
			get
			{
				return backbuffer;
			}
		}

		#endregion

		#region OpenGL Device Capabilities

		public bool SupportsDxt1
		{
			get;
			private set;
		}

		public bool SupportsS3tc
		{
			get;
			private set;
		}

		public bool SupportsHardwareInstancing
		{
			get;
			private set;
		}

		public int MaxTextureSlots
		{
			get;
			private set;
		}

		public int MaxVertexTextureSlots
		{
			get;
			private set;
		}

		#endregion

		#region Private MojoShader Interop

		private string shaderProfile;
		private IntPtr shaderContext;

		private IntPtr currentEffect = IntPtr.Zero;
		private IntPtr currentTechnique = IntPtr.Zero;
		private uint currentPass = 0;

		private int flipViewport;

		private bool effectApplied = false;

		private static IntPtr glGetProcAddress(string name, IntPtr d)
		{
			return SDL.SDL_GL_GetProcAddress(name);
		}
		private static MojoShader.MOJOSHADER_glGetProcAddress GLGetProcAddress = glGetProcAddress;

		#endregion

		#region Private Graphics Object Disposal Queues

		private Queue<IGLTexture> GCTextures = new Queue<IGLTexture>();
		private Queue<IGLRenderbuffer> GCDepthBuffers = new Queue<IGLRenderbuffer>();
		private Queue<IGLBuffer> GCVertexBuffers = new Queue<IGLBuffer>();
		private Queue<IGLBuffer> GCIndexBuffers = new Queue<IGLBuffer>();
		private Queue<IGLEffect> GCEffects = new Queue<IGLEffect>();
		private Queue<IGLQuery> GCQueries = new Queue<IGLQuery>();

		#endregion

		#region Public Constructor

		public OpenGLDevice(
			PresentationParameters presentationParameters
		) {
			// Create OpenGL context
			glContext = SDL.SDL_GL_CreateContext(
				presentationParameters.DeviceWindowHandle
			);

			// Init threaded GL crap where applicable
			InitThreadedGL(
				presentationParameters.DeviceWindowHandle
			);

			// Initialize entry points
			LoadGLEntryPoints();

			shaderProfile = MojoShader.MOJOSHADER_glBestProfile(
				GLGetProcAddress,
				IntPtr.Zero,
				null,
				null,
				IntPtr.Zero
			);
			shaderContext = MojoShader.MOJOSHADER_glCreateContext(
				shaderProfile,
				GLGetProcAddress,
				IntPtr.Zero,
				null,
				null,
				IntPtr.Zero
			);
			MojoShader.MOJOSHADER_glMakeContextCurrent(shaderContext);

			// Print GL information
			System.Console.WriteLine("OpenGL Device: " + glGetString(GLenum.GL_RENDERER));
			System.Console.WriteLine("OpenGL Driver: " + glGetString(GLenum.GL_VERSION));
			System.Console.WriteLine("OpenGL Vendor: " + glGetString(GLenum.GL_VENDOR));
			System.Console.WriteLine("MojoShader Profile: " + shaderProfile);

			// Load the extension list, initialize extension-dependent components
			string extensions = glGetString(GLenum.GL_EXTENSIONS);
			SupportsS3tc = (
				extensions.Contains("GL_EXT_texture_compression_s3tc") ||
				extensions.Contains("GL_OES_texture_compression_S3TC") ||
				extensions.Contains("GL_EXT_texture_compression_dxt3") ||
				extensions.Contains("GL_EXT_texture_compression_dxt5")
			);
			SupportsDxt1 = (
				SupportsS3tc ||
				extensions.Contains("GL_EXT_texture_compression_dxt1")
			);

			// Initialize the faux-backbuffer
			backbuffer = new OpenGLBackbuffer(
				this,
				GraphicsDeviceManager.DefaultBackBufferWidth,
				GraphicsDeviceManager.DefaultBackBufferHeight,
				presentationParameters.DepthStencilFormat
			);

			// Initialize texture collection array
			int numSamplers;
			glGetIntegerv(GLenum.GL_MAX_TEXTURE_IMAGE_UNITS, out numSamplers);
			Textures = new OpenGLTexture[numSamplers];
			for (int i = 0; i < numSamplers; i += 1)
			{
				Textures[i] = OpenGLTexture.NullTexture;
			}
			MaxTextureSlots = numSamplers;

			// Initialize MaxVertexTextureSlots, but no array. You'll see why...
			glGetIntegerv(GLenum.GL_MAX_VERTEX_TEXTURE_IMAGE_UNITS, out numSamplers);
			MaxVertexTextureSlots = numSamplers;

			// Initialize render target FBO and state arrays
			int numAttachments;
			glGetIntegerv(GLenum.GL_MAX_DRAW_BUFFERS, out numAttachments);
			currentAttachments = new uint[numAttachments];
			currentAttachmentFaces = new GLenum[numAttachments];
			drawBuffersArray = new GLenum[numAttachments];
			for (int i = 0; i < numAttachments; i += 1)
			{
				currentAttachments[i] = 0;
				currentAttachmentFaces[i] = GLenum.GL_TEXTURE_2D;
				drawBuffersArray[i] = GLenum.GL_COLOR_ATTACHMENT0 + i;
			}
			currentDrawBuffers = 0;
			currentRenderbuffer = 0;
			currentDepthStencilFormat = DepthFormat.None;
			glGenFramebuffers(1, out targetFramebuffer);
		}

		#endregion

		#region Dispose Method

		public void Dispose()
		{
			glDeleteFramebuffers(1, ref targetFramebuffer);
			targetFramebuffer = 0;
			backbuffer.Dispose();
			backbuffer = null;
			MojoShader.MOJOSHADER_glMakeContextCurrent(IntPtr.Zero);
			MojoShader.MOJOSHADER_glDestroyContext(shaderContext);

#if THREADED_GL
			SDL.SDL_GL_DeleteContext(BackgroundContext.context);
#endif
			SDL.SDL_GL_DeleteContext(glContext);
		}

		#endregion

		#region Window SwapBuffers Method

		public void SwapBuffers(
			Rectangle? sourceRectangle,
			Rectangle? destinationRectangle,
			IntPtr overrideWindowHandle
		) {
#if !DISABLE_FAUXBACKBUFFER
			/* Only the faux-backbuffer supports presenting
			 * specific regions given to Present().
			 * -flibit
			 */
			int srcX, srcY, srcW, srcH;
			int dstX, dstY, dstW, dstH;
			if (sourceRectangle.HasValue)
			{
				srcX = sourceRectangle.Value.X;
				srcY = sourceRectangle.Value.Y;
				srcW = sourceRectangle.Value.Width;
				srcH = sourceRectangle.Value.Height;
			}
			else
			{
				srcX = 0;
				srcY = 0;
				srcW = backbuffer.Width;
				srcH = backbuffer.Height;
			}
			if (destinationRectangle.HasValue)
			{
				dstX = destinationRectangle.Value.X;
				dstY = destinationRectangle.Value.Y;
				dstW = destinationRectangle.Value.Width;
				dstH = destinationRectangle.Value.Height;
			}
			else
			{
				dstX = 0;
				dstY = 0;
				SDL.SDL_GetWindowSize(
					overrideWindowHandle,
					out dstW,
					out dstH
				);
			}

			if (scissorTestEnable)
			{
				glDisable(GLenum.GL_SCISSOR_TEST);
			}

			BindReadFramebuffer(backbuffer.Handle);
			BindDrawFramebuffer(0);

			glBlitFramebuffer(
				srcX, srcY, srcW, srcH,
				dstX, dstY, dstW, dstH,
				GLenum.GL_COLOR_BUFFER_BIT,
				GLenum.GL_LINEAR
			);

			BindFramebuffer(0);

			if (scissorTestEnable)
			{
				glEnable(GLenum.GL_SCISSOR_TEST);
			}
#endif

			SDL.SDL_GL_SwapWindow(
				overrideWindowHandle
			);
			BindFramebuffer(backbuffer.Handle);

#if !DISABLE_THREADING && !THREADED_GL
			RunActions();
#endif
			while (GCTextures.Count > 0)
			{
				DeleteTexture(GCTextures.Dequeue());
			}
			while (GCDepthBuffers.Count > 0)
			{
				DeleteRenderbuffer(GCDepthBuffers.Dequeue());
			}
			while (GCVertexBuffers.Count > 0)
			{
				DeleteVertexBuffer(GCVertexBuffers.Dequeue());
			}
			while (GCIndexBuffers.Count > 0)
			{
				DeleteIndexBuffer(GCIndexBuffers.Dequeue());
			}
			while (GCEffects.Count > 0)
			{
				DeleteEffect(GCEffects.Dequeue());
			}
			while (GCQueries.Count > 0)
			{
				DeleteQuery(GCQueries.Dequeue());
			}
		}

		#endregion

		#region GL Object Disposal Wrappers

		public void AddDisposeTexture(IGLTexture texture)
		{
			if (IsOnMainThread())
			{
				DeleteTexture(texture);
			}
			else
			{
				GCTextures.Enqueue(texture);
			}
		}

		public void AddDisposeRenderbuffer(IGLRenderbuffer renderbuffer)
		{
			if (IsOnMainThread())
			{
				DeleteRenderbuffer(renderbuffer);
			}
			else
			{
				GCDepthBuffers.Enqueue(renderbuffer);
			}
		}

		public void AddDisposeVertexBuffer(IGLBuffer buffer)
		{
			if (IsOnMainThread())
			{
				DeleteVertexBuffer(buffer);
			}
			else
			{
				GCVertexBuffers.Enqueue(buffer);
			}
		}

		public void AddDisposeIndexBuffer(IGLBuffer buffer)
		{
			if (IsOnMainThread())
			{
				DeleteIndexBuffer(buffer);
			}
			else
			{
				GCIndexBuffers.Enqueue(buffer);
			}
		}

		public void AddDisposeEffect(IGLEffect effect)
		{
			if (IsOnMainThread())
			{
				DeleteEffect(effect);
			}
			else
			{
				GCEffects.Enqueue(effect);
			}
		}

		public void AddDisposeQuery(IGLQuery query)
		{
			if (IsOnMainThread())
			{
				DeleteQuery(query);
			}
			else
			{
				GCQueries.Enqueue(query);
			}
		}

		#endregion

		#region String Marker Method

		public void SetStringMarker(string text)
		{
#if DEBUG
			byte[] chars = System.Text.Encoding.ASCII.GetBytes(text);
			glStringMarkerGREMEDY(chars.Length, chars);
#endif
		}

		#endregion

		#region Drawing Methods

		public void DrawIndexedPrimitives(
			PrimitiveType primitiveType,
			int baseVertex,
			int minVertexIndex,
			int numVertices,
			int startIndex,
			int primitiveCount,
			IndexBuffer indices
		) {
			// Unsigned short or unsigned int?
			bool shortIndices = indices.IndexElementSize == IndexElementSize.SixteenBits;

			// Bind the index buffer
			BindIndexBuffer(indices.buffer);

			// Draw!
			glDrawRangeElements(
				XNAToGL.Primitive[primitiveType],
				minVertexIndex,
				minVertexIndex + numVertices - 1,
				XNAToGL.PrimitiveVerts(primitiveType, primitiveCount),
				shortIndices ?
					GLenum.GL_UNSIGNED_SHORT :
					GLenum.GL_UNSIGNED_INT,
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
			int instanceCount,
			IndexBuffer indices
		) {
			// Note that minVertexIndex and numVertices are NOT used!

			// Bind the index buffer
			BindIndexBuffer(indices.buffer);

			// Unsigned short or unsigned int?
			bool shortIndices = indices.IndexElementSize == IndexElementSize.SixteenBits;

			// Draw!
			glDrawElementsInstanced(
				XNAToGL.Primitive[primitiveType],
				XNAToGL.PrimitiveVerts(primitiveType, primitiveCount),
				shortIndices ?
					GLenum.GL_UNSIGNED_SHORT :
					GLenum.GL_UNSIGNED_INT,
				(IntPtr) (startIndex * (shortIndices ? 2 : 4)),
				instanceCount
			);
		}

		public void DrawPrimitives(
			PrimitiveType primitiveType,
			int vertexStart,
			int primitiveCount
		) {
			// Draw!
			glDrawArrays(
				XNAToGL.Primitive[primitiveType],
				vertexStart,
				XNAToGL.PrimitiveVerts(primitiveType, primitiveCount)
			);
		}

		public void DrawUserIndexedPrimitives(
			PrimitiveType primitiveType,
			IntPtr vertexData,
			int vertexOffset,
			int numVertices,
			IntPtr indexData,
			int indexOffset,
			IndexElementSize indexElementSize,
			int primitiveCount
		) {
			// Unbind current index buffer.
			BindIndexBuffer(OpenGLBuffer.NullBuffer);

			// Unsigned short or unsigned int?
			bool shortIndices = indexElementSize == IndexElementSize.SixteenBits;

			// Draw!
			glDrawRangeElements(
				XNAToGL.Primitive[primitiveType],
				0,
				numVertices - 1,
				XNAToGL.PrimitiveVerts(primitiveType, primitiveCount),
				shortIndices ?
					GLenum.GL_UNSIGNED_SHORT :
					GLenum.GL_UNSIGNED_INT,
				(IntPtr) (
					indexData.ToInt64() +
					(indexOffset * (shortIndices ? 2 : 4))
				)
			);
		}

		public void DrawUserPrimitives(
			PrimitiveType primitiveType,
			IntPtr vertexData,
			int vertexOffset,
			int primitiveCount
		) {
			// Draw!
			glDrawArrays(
				XNAToGL.Primitive[primitiveType],
				vertexOffset,
				XNAToGL.PrimitiveVerts(primitiveType, primitiveCount)
			);
		}

		#endregion

		#region State Management Methods

		public void SetViewport(Viewport vp, bool renderTargetBound)
		{
			// Flip viewport when target is not bound
			if (!renderTargetBound)
			{
				vp.Y = backbuffer.Height - vp.Y - vp.Height;
			}

			if (vp.Bounds != viewport)
			{
				viewport = vp.Bounds;
				glViewport(
					viewport.X,
					viewport.Y,
					viewport.Width,
					viewport.Height
				);
			}

			if (vp.MinDepth != depthRangeMin || vp.MaxDepth != depthRangeMax)
			{
				depthRangeMin = vp.MinDepth;
				depthRangeMax = vp.MaxDepth;
				glDepthRange((double) depthRangeMin, (double) depthRangeMax);
			}
		}

		public void SetScissorRect(
			Rectangle scissorRect,
			bool renderTargetBound
		) {
			// Flip rectangle when target is not bound
			if (!renderTargetBound)
			{
				scissorRect.Y = viewport.Height - scissorRect.Y - scissorRect.Height;
			}

			if (scissorRect != scissorRectangle)
			{
				scissorRectangle = scissorRect;
				glScissor(
					scissorRectangle.X,
					scissorRectangle.Y,
					scissorRectangle.Width,
					scissorRectangle.Height
				);
			}
		}

		public void SetBlendState(BlendState blendState)
		{
			bool newEnable = (
				!(	blendState.ColorSourceBlend == Blend.One &&
					blendState.ColorDestinationBlend == Blend.Zero &&
					blendState.AlphaSourceBlend == Blend.One &&
					blendState.AlphaDestinationBlend == Blend.Zero	)
			);
			if (newEnable != alphaBlendEnable)
			{
				alphaBlendEnable = newEnable;
				ToggleGLState(GLenum.GL_BLEND, alphaBlendEnable);
			}

			if (alphaBlendEnable)
			{
				if (blendState.BlendFactor != blendColor)
				{
					blendColor = blendState.BlendFactor;
					glBlendColor(
						blendColor.R / 255.0f,
						blendColor.G / 255.0f,
						blendColor.B / 255.0f,
						blendColor.A / 255.0f
					);
				}

				if (	blendState.ColorSourceBlend != srcBlend ||
					blendState.ColorDestinationBlend != dstBlend ||
					blendState.AlphaSourceBlend != srcBlendAlpha ||
					blendState.AlphaDestinationBlend != dstBlendAlpha	)
				{
					srcBlend = blendState.ColorSourceBlend;
					dstBlend = blendState.ColorDestinationBlend;
					srcBlendAlpha = blendState.AlphaSourceBlend;
					dstBlendAlpha = blendState.AlphaDestinationBlend;
					glBlendFuncSeparate(
						XNAToGL.BlendMode[srcBlend],
						XNAToGL.BlendMode[dstBlend],
						XNAToGL.BlendMode[srcBlendAlpha],
						XNAToGL.BlendMode[dstBlendAlpha]
					);
				}

				if (	blendState.ColorBlendFunction != blendOp ||
					blendState.AlphaBlendFunction != blendOpAlpha	)
				{
					blendOp = blendState.ColorBlendFunction;
					blendOpAlpha = blendState.AlphaBlendFunction;
					glBlendEquationSeparate(
						XNAToGL.BlendEquation[blendOp],
						XNAToGL.BlendEquation[blendOpAlpha]
					);
				}
			}

			if (blendState.ColorWriteChannels != colorWriteEnable)
			{
				colorWriteEnable = blendState.ColorWriteChannels;
				glColorMask(
					(colorWriteEnable & ColorWriteChannels.Red) != 0,
					(colorWriteEnable & ColorWriteChannels.Green) != 0,
					(colorWriteEnable & ColorWriteChannels.Blue) != 0,
					(colorWriteEnable & ColorWriteChannels.Alpha) != 0
				);
			}
			/* FIXME: So how exactly do we factor in
			 * COLORWRITEENABLE for buffer 0? Do we just assume that
			 * the default is just buffer 0, and all other calls
			 * update the other write masks afterward? Or do we
			 * assume that COLORWRITEENABLE only touches 0, and the
			 * other 3 buffers are left alone unless we don't have
			 * EXT_draw_buffers2?
			 * -flibit
			 */
			if (blendState.ColorWriteChannels1 != colorWriteEnable1)
			{
				colorWriteEnable1 = blendState.ColorWriteChannels1;
				glColorMaskIndexedEXT(
					1,
					(colorWriteEnable1 & ColorWriteChannels.Red) != 0,
					(colorWriteEnable1 & ColorWriteChannels.Green) != 0,
					(colorWriteEnable1 & ColorWriteChannels.Blue) != 0,
					(colorWriteEnable1 & ColorWriteChannels.Alpha) != 0
				);
			}
			if (blendState.ColorWriteChannels2 != colorWriteEnable2)
			{
				colorWriteEnable2 = blendState.ColorWriteChannels2;
				glColorMaskIndexedEXT(
					2,
					(colorWriteEnable2 & ColorWriteChannels.Red) != 0,
					(colorWriteEnable2 & ColorWriteChannels.Green) != 0,
					(colorWriteEnable2 & ColorWriteChannels.Blue) != 0,
					(colorWriteEnable2 & ColorWriteChannels.Alpha) != 0
				);
			}
			if (blendState.ColorWriteChannels3 != colorWriteEnable3)
			{
				colorWriteEnable3 = blendState.ColorWriteChannels3;
				glColorMaskIndexedEXT(
					3,
					(colorWriteEnable3 & ColorWriteChannels.Red) != 0,
					(colorWriteEnable3 & ColorWriteChannels.Green) != 0,
					(colorWriteEnable3 & ColorWriteChannels.Blue) != 0,
					(colorWriteEnable3 & ColorWriteChannels.Alpha) != 0
				);
			}
		}

		public void SetDepthStencilState(DepthStencilState depthStencilState)
		{
			if (depthStencilState.DepthBufferEnable != zEnable)
			{
				zEnable = depthStencilState.DepthBufferEnable;
				ToggleGLState(GLenum.GL_DEPTH_TEST, zEnable);
			}

			if (zEnable)
			{
				if (depthStencilState.DepthBufferWriteEnable != zWriteEnable)
				{
					zWriteEnable = depthStencilState.DepthBufferWriteEnable;
					glDepthMask(zWriteEnable);
				}

				if (depthStencilState.DepthBufferFunction != depthFunc)
				{
					depthFunc = depthStencilState.DepthBufferFunction;
					glDepthFunc(XNAToGL.CompareFunc[depthFunc]);
				}
			}

			if (depthStencilState.StencilEnable != stencilEnable)
			{
				stencilEnable = depthStencilState.StencilEnable;
				ToggleGLState(GLenum.GL_STENCIL_TEST, stencilEnable);
			}

			if (stencilEnable)
			{
				if (depthStencilState.StencilWriteMask != stencilWriteMask)
				{
					stencilWriteMask = depthStencilState.StencilWriteMask;
					glStencilMask(stencilWriteMask);
				}

				// TODO: Can we split StencilFunc/StencilOp up nicely? -flibit
				if (	depthStencilState.TwoSidedStencilMode != separateStencilEnable ||
					depthStencilState.ReferenceStencil != stencilRef ||
					depthStencilState.StencilMask != stencilMask ||
					depthStencilState.StencilFunction != stencilFunc ||
					depthStencilState.CounterClockwiseStencilFunction != ccwStencilFunc ||
					depthStencilState.StencilFail != stencilFail ||
					depthStencilState.StencilDepthBufferFail != stencilZFail ||
					depthStencilState.StencilPass != stencilPass ||
					depthStencilState.CounterClockwiseStencilFail != ccwStencilFail ||
					depthStencilState.CounterClockwiseStencilDepthBufferFail != ccwStencilZFail ||
					depthStencilState.CounterClockwiseStencilPass != ccwStencilPass	)
				{
					separateStencilEnable = depthStencilState.TwoSidedStencilMode;
					stencilRef = depthStencilState.ReferenceStencil;
					stencilMask = depthStencilState.StencilMask;
					stencilFunc = depthStencilState.StencilFunction;
					stencilFail = depthStencilState.StencilFail;
					stencilZFail = depthStencilState.StencilDepthBufferFail;
					stencilPass = depthStencilState.StencilPass;
					if (separateStencilEnable)
					{
						ccwStencilFunc = depthStencilState.CounterClockwiseStencilFunction;
						ccwStencilFail = depthStencilState.CounterClockwiseStencilFail;
						ccwStencilZFail = depthStencilState.CounterClockwiseStencilDepthBufferFail;
						ccwStencilPass = depthStencilState.CounterClockwiseStencilPass;
						glStencilFuncSeparate(
							GLenum.GL_FRONT,
							XNAToGL.CompareFunc[stencilFunc],
							stencilRef,
							stencilMask
						);
						glStencilFuncSeparate(
							GLenum.GL_BACK,
							XNAToGL.CompareFunc[ccwStencilFunc],
							stencilRef,
							stencilMask
						);
						glStencilOpSeparate(
							GLenum.GL_FRONT,
							XNAToGL.GLStencilOp[stencilFail],
							XNAToGL.GLStencilOp[stencilZFail],
							XNAToGL.GLStencilOp[stencilPass]
						);
						glStencilOpSeparate(
							GLenum.GL_BACK,
							XNAToGL.GLStencilOp[ccwStencilFail],
							XNAToGL.GLStencilOp[ccwStencilZFail],
							XNAToGL.GLStencilOp[ccwStencilPass]
						);
					}
					else
					{
						glStencilFunc(
							XNAToGL.CompareFunc[stencilFunc],
							stencilRef,
							stencilMask
						);
						glStencilOp(
							XNAToGL.GLStencilOp[stencilFail],
							XNAToGL.GLStencilOp[stencilZFail],
							XNAToGL.GLStencilOp[stencilPass]
						);
					}
				}
			}
		}

		public void ApplyRasterizerState(
			RasterizerState rasterizerState,
			bool renderTargetBound
		) {
			if (rasterizerState.ScissorTestEnable != scissorTestEnable)
			{
				scissorTestEnable = rasterizerState.ScissorTestEnable;
				ToggleGLState(GLenum.GL_SCISSOR_TEST, scissorTestEnable);
			}

			CullMode actualMode;
			if (renderTargetBound)
			{
				actualMode = rasterizerState.CullMode;
			}
			else
			{
				// When not rendering offscreen the faces change order.
				if (rasterizerState.CullMode == CullMode.None)
				{
					actualMode = rasterizerState.CullMode;
				}
				else
				{
					actualMode = (
						rasterizerState.CullMode == CullMode.CullClockwiseFace ?
							CullMode.CullCounterClockwiseFace :
							CullMode.CullClockwiseFace
					);
				}
			}
			if (actualMode != cullFrontFace)
			{
				if ((actualMode == CullMode.None) != (cullFrontFace == CullMode.None))
				{
					ToggleGLState(GLenum.GL_CULL_FACE, actualMode != CullMode.None);
					if (actualMode != CullMode.None)
					{
						// FIXME: XNA/FNA-specific behavior? -flibit
						glCullFace(GLenum.GL_BACK);
					}
				}
				cullFrontFace = actualMode;
				if (cullFrontFace != CullMode.None)
				{
					glFrontFace(XNAToGL.FrontFace[cullFrontFace]);
				}
			}

			if (rasterizerState.FillMode != fillMode)
			{
				fillMode = rasterizerState.FillMode;
				glPolygonMode(
					GLenum.GL_FRONT_AND_BACK,
					XNAToGL.GLFillMode[fillMode]
				);
			}

			if (zEnable)
			{
				if (	rasterizerState.DepthBias != depthBias ||
					rasterizerState.SlopeScaleDepthBias != slopeScaleDepthBias	)
				{
					depthBias = rasterizerState.DepthBias;
					slopeScaleDepthBias = rasterizerState.SlopeScaleDepthBias;
					if (depthBias == 0.0f && slopeScaleDepthBias == 0.0f)
					{
						glDisable(GLenum.GL_POLYGON_OFFSET_FILL);
					}
					else
					{
						glEnable(GLenum.GL_POLYGON_OFFSET_FILL);
						glPolygonOffset(slopeScaleDepthBias, depthBias);
					}
				}
			}
		}

		public void VerifySampler(int index, Texture texture, SamplerState sampler)
		{
			if (texture == null)
			{
				if (Textures[index] != OpenGLTexture.NullTexture)
				{
					if (index != 0)
					{
						glActiveTexture(GLenum.GL_TEXTURE0 + index);
					}
					glBindTexture(Textures[index].Target, 0);
					if (index != 0)
					{
						// Keep this state sane. -flibit
						glActiveTexture(GLenum.GL_TEXTURE0);
					}
					Textures[index] = OpenGLTexture.NullTexture;
				}
				return;
			}

			OpenGLTexture tex = texture.texture as OpenGLTexture;

			if (	tex == Textures[index] &&
				sampler.AddressU == tex.WrapS &&
				sampler.AddressV == tex.WrapT &&
				sampler.AddressW == tex.WrapR &&
				sampler.Filter == tex.Filter &&
				sampler.MaxAnisotropy == tex.Anistropy &&
				sampler.MaxMipLevel == tex.MaxMipmapLevel &&
				sampler.MipMapLevelOfDetailBias == tex.LODBias	)
			{
				// Nothing's changing, forget it.
				return;
			}

			// Set the active texture slot
			if (index != 0)
			{
				glActiveTexture(GLenum.GL_TEXTURE0 + index);
			}

			// Bind the correct texture
			if (tex != Textures[index])
			{
				if (tex.Target != Textures[index].Target)
				{
					// If we're changing targets, unbind the old texture first!
					glBindTexture(Textures[index].Target, 0);
				}
				glBindTexture(tex.Target, tex.Handle);
				Textures[index] = tex;
			}

			// Apply the sampler states to the GL texture
			if (sampler.AddressU != tex.WrapS)
			{
				tex.WrapS = sampler.AddressU;
				glTexParameteri(
					tex.Target,
					GLenum.GL_TEXTURE_WRAP_S,
					(int) XNAToGL.Wrap[tex.WrapS]
				);
			}
			if (sampler.AddressV != tex.WrapT)
			{
				tex.WrapT = sampler.AddressV;
				glTexParameteri(
					tex.Target,
					GLenum.GL_TEXTURE_WRAP_T,
					(int) XNAToGL.Wrap[tex.WrapT]
				);
			}
			if (sampler.AddressW != tex.WrapR)
			{
				tex.WrapR = sampler.AddressW;
				glTexParameteri(
					tex.Target,
					GLenum.GL_TEXTURE_WRAP_R,
					(int) XNAToGL.Wrap[tex.WrapR]
				);
			}
			if (	sampler.Filter != tex.Filter ||
				sampler.MaxAnisotropy != tex.Anistropy	)
			{
				tex.Filter = sampler.Filter;
				tex.Anistropy = sampler.MaxAnisotropy;
				glTexParameteri(
					tex.Target,
					GLenum.GL_TEXTURE_MAG_FILTER,
					(int) XNAToGL.MagFilter[tex.Filter]
				);
				glTexParameteri(
					tex.Target,
					GLenum.GL_TEXTURE_MIN_FILTER,
					(int) (
						tex.HasMipmaps ?
							XNAToGL.MinMipFilter[tex.Filter] :
							XNAToGL.MinFilter[tex.Filter]
					)
				);
				glTexParameterf(
					tex.Target,
					GLenum.GL_TEXTURE_MAX_ANISOTROPY_EXT,
					(tex.Filter == TextureFilter.Anisotropic) ?
						Math.Max(tex.Anistropy, 1.0f) :
						1.0f
				);
			}
			if (sampler.MaxMipLevel != tex.MaxMipmapLevel)
			{
				tex.MaxMipmapLevel = sampler.MaxMipLevel;
				glTexParameteri(
					tex.Target,
					GLenum.GL_TEXTURE_BASE_LEVEL,
					tex.MaxMipmapLevel
				);
			}
			if (sampler.MipMapLevelOfDetailBias != tex.LODBias)
			{
				tex.LODBias = sampler.MipMapLevelOfDetailBias;
				glTexParameterf(
					tex.Target,
					GLenum.GL_TEXTURE_LOD_BIAS,
					tex.LODBias
				);
			}

			if (index != 0)
			{
				// Keep this state sane. -flibit
				glActiveTexture(GLenum.GL_TEXTURE0);
			}
		}

		public void VerifyVertexSampler(int index, Texture texture, SamplerState sampler)
		{
			/* TODO: Yup, these two are no different right now!
			 * This is really tough to fix, since the sampler registers
			 * are shared by the vertex and fragment shaders. This could in
			 * theory make sense if the vertex shader uses the regular sampler
			 * registers, but there are of course the vertex sampler registers
			 * to worry about. When someone actually uses them, fix that case
			 * and odds are you'll figure out the rest as well.
			 * -flibit
			 */
			VerifySampler(index, texture, sampler);
		}

		#endregion

		#region Effect Methods

		public IGLEffect CreateEffect(byte[] effectCode)
		{
			IntPtr effect = IntPtr.Zero;
			IntPtr glEffect = IntPtr.Zero;

#if !DISABLE_THREADING
			ForceToMainThread(() => {
#endif

			effect = MojoShader.MOJOSHADER_parseEffect(
				shaderProfile,
				effectCode,
				(uint) effectCode.Length,
				null,
				0,
				null,
				0,
				null,
				null,
				IntPtr.Zero
			);
			glEffect = MojoShader.MOJOSHADER_glCompileEffect(effect);
			if (glEffect == IntPtr.Zero)
			{
				throw new Exception(MojoShader.MOJOSHADER_glGetError());
			}

#if !DISABLE_THREADING
			});
#endif

			return new OpenGLEffect(effect, glEffect);
		}

		private void DeleteEffect(IGLEffect effect)
		{
			IntPtr glEffectData = (effect as OpenGLEffect).GLEffectData;
			if (glEffectData == currentEffect)
			{
				MojoShader.MOJOSHADER_glEffectEndPass(currentEffect);
				MojoShader.MOJOSHADER_glEffectEnd(currentEffect);
				currentEffect = IntPtr.Zero;
				currentTechnique = IntPtr.Zero;
				currentPass = 0;
			}
			MojoShader.MOJOSHADER_glDeleteEffect(glEffectData);
			MojoShader.MOJOSHADER_freeEffect(effect.EffectData);
		}

		public IGLEffect CloneEffect(IGLEffect cloneSource)
		{
			IntPtr effect = IntPtr.Zero;
			IntPtr glEffect = IntPtr.Zero;

#if !DISABLE_THREADING
			ForceToMainThread(() => {
#endif

			effect = MojoShader.MOJOSHADER_cloneEffect(cloneSource.EffectData);
			glEffect = MojoShader.MOJOSHADER_glCompileEffect(effect);
			if (glEffect == IntPtr.Zero)
			{
				throw new Exception(MojoShader.MOJOSHADER_glGetError());
			}

#if !DISABLE_THREADING
			});
#endif

			return new OpenGLEffect(effect, glEffect);
		}

		public void ApplyEffect(
			IGLEffect effect,
			IntPtr technique,
			uint pass,
			ref MojoShader.MOJOSHADER_effectStateChanges stateChanges
		) {
			effectApplied = true;
			flipViewport = (currentDrawFramebuffer == targetFramebuffer) ? -1 : 1;
			IntPtr glEffectData = (effect as OpenGLEffect).GLEffectData;
			if (glEffectData == currentEffect)
			{
				if (technique == currentTechnique && pass == currentPass)
				{
					MojoShader.MOJOSHADER_glEffectCommitChanges(currentEffect);
					return;
				}
				MojoShader.MOJOSHADER_glEffectEndPass(currentEffect);
				MojoShader.MOJOSHADER_glEffectBeginPass(currentEffect, pass);
				currentTechnique = technique;
				currentPass = pass;
				return;
			}
			else if (currentEffect != IntPtr.Zero)
			{
				MojoShader.MOJOSHADER_glEffectEndPass(currentEffect);
				MojoShader.MOJOSHADER_glEffectEnd(currentEffect);
			}
			uint whatever;
			MojoShader.MOJOSHADER_glEffectBegin(
				glEffectData,
				out whatever,
				0,
				ref stateChanges
			);
			MojoShader.MOJOSHADER_glEffectBeginPass(
				glEffectData,
				pass
			);
			currentEffect = glEffectData;
			currentTechnique = technique;
			currentPass = pass;
		}

		public void BeginPassRestore(
			IGLEffect effect,
			ref MojoShader.MOJOSHADER_effectStateChanges changes
		) {
			IntPtr glEffectData = (effect as OpenGLEffect).GLEffectData;
			uint whatever;
			MojoShader.MOJOSHADER_glEffectBegin(
				glEffectData,
				out whatever,
				1,
				ref changes
			);
			MojoShader.MOJOSHADER_glEffectBeginPass(
				glEffectData,
				0
			);
			effectApplied = true;
		}

		public void EndPassRestore(IGLEffect effect)
		{
			IntPtr glEffectData = (effect as OpenGLEffect).GLEffectData;
			MojoShader.MOJOSHADER_glEffectEndPass(glEffectData);
			MojoShader.MOJOSHADER_glEffectEnd(glEffectData);
			effectApplied = true;
		}

		#endregion

		#region glVertexAttribPointer/glVertexAttribDivisor Methods

		public void ApplyVertexAttributes(
			VertexBufferBinding[] bindings,
			int numBindings,
			bool bindingsUpdated,
			int baseVertex
		) {
			if (	bindingsUpdated ||
				baseVertex != ldBaseVertex ||
				currentEffect != ldEffect ||
				currentTechnique != ldTechnique ||
				currentPass != ldPass ||
				effectApplied	)
			{
				/* There's this weird case where you can have multiple vertbuffers,
				 * but they will have overlapping attributes. It seems like the
				 * first buffer gets priority, so start with the last one so the
				 * first buffer's attributes are what's bound at the end.
				 * -flibit
				 */
				for (int i = numBindings - 1; i >= 0; i -= 1)
				{
					BindVertexBuffer(bindings[i].VertexBuffer.buffer);
					VertexDeclaration vertexDeclaration = bindings[i].VertexBuffer.VertexDeclaration;
					IntPtr basePtr = (IntPtr) (
						vertexDeclaration.VertexStride *
						(bindings[i].VertexOffset + baseVertex)
					);
					foreach (VertexElement element in vertexDeclaration.elements)
					{
						MojoShader.MOJOSHADER_glSetVertexAttribute(
							XNAToGL.VertexAttribUsage[element.VertexElementUsage],
							element.UsageIndex,
							XNAToGL.VertexAttribSize[element.VertexElementFormat],
							XNAToGL.VertexAttribType[element.VertexElementFormat],
							XNAToGL.VertexAttribNormalized(element),
							(uint) vertexDeclaration.VertexStride,
							basePtr + element.Offset
						);
						if (SupportsHardwareInstancing)
						{
							MojoShader.MOJOSHADER_glSetVertexAttribDivisor(
								XNAToGL.VertexAttribUsage[element.VertexElementUsage],
								element.UsageIndex,
								(uint) bindings[i].InstanceFrequency
							);
						}
					}
				}

				ldBaseVertex = baseVertex;
				ldEffect = currentEffect;
				ldTechnique = currentTechnique;
				ldPass = currentPass;
				effectApplied = false;
				ldVertexDeclaration = null;
				ldPointer = IntPtr.Zero;
			}

			MojoShader.MOJOSHADER_glProgramReady();
			if (flipViewport != 0)
			{
				MojoShader.MOJOSHADER_glProgramViewportFlip(flipViewport);
				flipViewport = 0;
			}
		}

		public void ApplyVertexAttributes(
			VertexDeclaration vertexDeclaration,
			IntPtr ptr,
			int vertexOffset
		) {
			BindVertexBuffer(OpenGLBuffer.NullBuffer);
			IntPtr basePtr = ptr + (vertexDeclaration.VertexStride * vertexOffset);

			if (	vertexDeclaration != ldVertexDeclaration ||
				basePtr != ldPointer ||
				currentEffect != ldEffect ||
				currentTechnique != ldTechnique ||
				currentPass != ldPass ||
				effectApplied	)
			{
				foreach (VertexElement element in vertexDeclaration.elements)
				{
					MojoShader.MOJOSHADER_glSetVertexAttribute(
						XNAToGL.VertexAttribUsage[element.VertexElementUsage],
						element.UsageIndex,
						XNAToGL.VertexAttribSize[element.VertexElementFormat],
						XNAToGL.VertexAttribType[element.VertexElementFormat],
						XNAToGL.VertexAttribNormalized(element),
						(uint) vertexDeclaration.VertexStride,
						basePtr + element.Offset
					);
					if (SupportsHardwareInstancing)
					{
						MojoShader.MOJOSHADER_glSetVertexAttribDivisor(
							XNAToGL.VertexAttribUsage[element.VertexElementUsage],
							element.UsageIndex,
							0
						);
					}
				}

				ldVertexDeclaration = vertexDeclaration;
				ldPointer = ptr;
				ldEffect = currentEffect;
				ldTechnique = currentTechnique;
				ldPass = currentPass;
				effectApplied = false;
				ldBaseVertex = -1;
			}

			MojoShader.MOJOSHADER_glProgramReady();
			if (flipViewport != 0)
			{
				MojoShader.MOJOSHADER_glProgramViewportFlip(flipViewport);
				flipViewport = 0;
			}
		}

		#endregion

		#region glGenBuffers Methods

		public IGLBuffer GenVertexBuffer(
			bool dynamic,
			int vertexCount,
			int vertexStride
		) {
			OpenGLBuffer result = null;

#if !DISABLE_THREADING
			ForceToMainThread(() => {
#endif

			uint handle;
			glGenBuffers(1, out handle);

			result = new OpenGLBuffer(
				handle,
				(IntPtr) (vertexStride * vertexCount),
				dynamic ? GLenum.GL_STREAM_DRAW : GLenum.GL_STATIC_DRAW
			);

			BindVertexBuffer(result);
			glBufferData(
				GLenum.GL_ARRAY_BUFFER,
				result.BufferSize,
				IntPtr.Zero,
				result.Dynamic
			);

#if !DISABLE_THREADING
			});
#endif

			return result;
		}

		public IGLBuffer GenIndexBuffer(
			bool dynamic,
			int indexCount,
			IndexElementSize indexElementSize
		) {
			OpenGLBuffer result = null;

#if !DISABLE_THREADING
			ForceToMainThread(() => {
#endif

			uint handle;
			glGenBuffers(1, out handle);

			result = new OpenGLBuffer(
				handle,
				(IntPtr) (indexCount * (indexElementSize == IndexElementSize.SixteenBits ? 2 : 4)),
				dynamic ? GLenum.GL_STREAM_DRAW : GLenum.GL_STATIC_DRAW
			);

			BindIndexBuffer(result);
			glBufferData(
				GLenum.GL_ELEMENT_ARRAY_BUFFER,
				result.BufferSize,
				IntPtr.Zero,
				result.Dynamic
			);

#if !DISABLE_THREADING
			});
#endif

			return result;
		}

		#endregion

		#region glBindBuffer Methods

		private void BindVertexBuffer(IGLBuffer buffer)
		{
			uint handle = (buffer as OpenGLBuffer).Handle;
			if (handle != currentVertexBuffer)
			{
				glBindBuffer(GLenum.GL_ARRAY_BUFFER, handle);
				currentVertexBuffer = handle;
			}
		}

		private void BindIndexBuffer(IGLBuffer buffer)
		{
			uint handle = (buffer as OpenGLBuffer).Handle;
			if (handle != currentIndexBuffer)
			{
				glBindBuffer(GLenum.GL_ELEMENT_ARRAY_BUFFER, handle);
				currentIndexBuffer = handle;
			}
		}

		#endregion

		#region glSetBufferData Methods

		public void SetVertexBufferData<T>(
			IGLBuffer buffer,
			int elementSizeInBytes,
			int offsetInBytes,
			T[] data,
			int startIndex,
			int elementCount,
			SetDataOptions options
		) where T : struct {
#if !DISABLE_THREADING
			ForceToMainThread(() => {
#endif

			BindVertexBuffer(buffer);

			if (options == SetDataOptions.Discard)
			{
				glBufferData(
					GLenum.GL_ARRAY_BUFFER,
					buffer.BufferSize,
					IntPtr.Zero,
					(buffer as OpenGLBuffer).Dynamic
				);
			}

			GCHandle dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);

			glBufferSubData(
				GLenum.GL_ARRAY_BUFFER,
				(IntPtr) offsetInBytes,
				(IntPtr) (elementSizeInBytes * elementCount),
				(IntPtr) (dataHandle.AddrOfPinnedObject().ToInt64() + startIndex * elementSizeInBytes)
			);

			dataHandle.Free();

#if !DISABLE_THREADING
			});
#endif
		}

		public void SetIndexBufferData<T>(
			IGLBuffer buffer,
			int offsetInBytes,
			T[] data,
			int startIndex,
			int elementCount,
			SetDataOptions options
		) where T : struct {
#if !DISABLE_THREADING
			ForceToMainThread(() => {
#endif

			BindIndexBuffer(buffer);

			if (options == SetDataOptions.Discard)
			{
				glBufferData(
					GLenum.GL_ELEMENT_ARRAY_BUFFER,
					buffer.BufferSize,
					IntPtr.Zero,
					(buffer as OpenGLBuffer).Dynamic
				);
			}

			GCHandle dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);

			int elementSizeInBytes = Marshal.SizeOf(typeof(T));
			glBufferSubData(
				GLenum.GL_ELEMENT_ARRAY_BUFFER,
				(IntPtr) offsetInBytes,
				(IntPtr) (elementSizeInBytes * elementCount),
				(IntPtr) (dataHandle.AddrOfPinnedObject().ToInt64() + startIndex * elementSizeInBytes)
			);

			dataHandle.Free();

#if !DISABLE_THREADING
			});
#endif
		}

		#endregion

		#region glGetBufferData Methods

		public void GetVertexBufferData<T>(
			IGLBuffer buffer,
			int offsetInBytes,
			T[] data,
			int startIndex,
			int elementCount,
			int vertexStride
		) where T : struct {
#if !DISABLE_THREADING
			ForceToMainThread(() => {
#endif

			BindVertexBuffer(buffer);

			IntPtr ptr = glMapBuffer(GLenum.GL_ARRAY_BUFFER, GLenum.GL_READ_ONLY);

			// Pointer to the start of data to read in the index buffer
			ptr = new IntPtr(ptr.ToInt64() + offsetInBytes);

			if (typeof(T) == typeof(byte))
			{
				/* If data is already a byte[] we can skip the temporary buffer.
				 * Copy from the vertex buffer to the destination array.
				 */
				byte[] buf = data as byte[];
				Marshal.Copy(ptr, buf, 0, buf.Length);
			}
			else
			{
				// Temporary buffer to store the copied section of data
				byte[] temp = new byte[elementCount * vertexStride - offsetInBytes];

				// Copy from the vertex buffer to the temporary buffer
				Marshal.Copy(ptr, temp, 0, temp.Length);

				GCHandle dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
				IntPtr dataPtr = (IntPtr) (dataHandle.AddrOfPinnedObject().ToInt64() + startIndex * Marshal.SizeOf(typeof(T)));

				// Copy from the temporary buffer to the destination array
				int dataSize = Marshal.SizeOf(typeof(T));
				if (dataSize == vertexStride)
				{
					Marshal.Copy(temp, 0, dataPtr, temp.Length);
				}
				else
				{
					// If the user is asking for a specific element within the vertex buffer, copy them one by one...
					for (int i = 0; i < elementCount; i += 1)
					{
						Marshal.Copy(temp, i * vertexStride, dataPtr, dataSize);
						dataPtr = (IntPtr)(dataPtr.ToInt64() + dataSize);
					}
				}

				dataHandle.Free();
			}

			glUnmapBuffer(GLenum.GL_ARRAY_BUFFER);

#if !DISABLE_THREADING
			});
#endif
		}

		public void GetIndexBufferData<T>(
			IGLBuffer buffer,
			int offsetInBytes,
			T[] data,
			int startIndex,
			int elementCount
		) where T : struct {
#if !DISABLE_THREADING
			ForceToMainThread(() => {
#endif

			BindIndexBuffer(buffer);

			IntPtr ptr = glMapBuffer(GLenum.GL_ELEMENT_ARRAY_BUFFER, GLenum.GL_READ_ONLY);

			// Pointer to the start of data to read in the index buffer
			ptr = new IntPtr(ptr.ToInt64() + offsetInBytes);

			/* If data is already a byte[] we can skip the temporary buffer.
			 * Copy from the index buffer to the destination array.
			 */
			if (typeof(T) == typeof(byte))
			{
				byte[] buf = data as byte[];
				Marshal.Copy(ptr, buf, 0, buf.Length);
			}
			else
			{
				int elementSizeInBytes = Marshal.SizeOf(typeof(T));
				byte[] temp = new byte[elementCount * elementSizeInBytes];
				Marshal.Copy(ptr, temp, 0, temp.Length);
				Buffer.BlockCopy(temp, 0, data, startIndex * elementSizeInBytes, elementCount * elementSizeInBytes);
			}

			glUnmapBuffer(GLenum.GL_ELEMENT_ARRAY_BUFFER);

#if !DISABLE_THREADING
			});
#endif
		}

		#endregion

		#region glDeleteBuffers Methods

		private void DeleteVertexBuffer(IGLBuffer buffer)
		{
			uint handle = (buffer as OpenGLBuffer).Handle;
			if (handle == currentVertexBuffer)
			{
				glBindBuffer(GLenum.GL_ARRAY_BUFFER, 0);
				currentVertexBuffer = 0;
			}
			glDeleteBuffers(1, ref handle);
		}

		private void DeleteIndexBuffer(IGLBuffer buffer)
		{
			uint handle = (buffer as OpenGLBuffer).Handle;
			if (handle == currentIndexBuffer)
			{
				glBindBuffer(GLenum.GL_ELEMENT_ARRAY_BUFFER, 0);
				currentIndexBuffer = 0;
			}
			glDeleteBuffers(1, ref handle);
		}

		#endregion

		#region glCreateTexture Methods

		private OpenGLTexture CreateTexture(
			Type target,
			int levelCount
		) {
			uint handle;
			glGenTextures(1, out handle);
			OpenGLTexture result = new OpenGLTexture(
				handle,
				target,
				levelCount
			);
			BindTexture(result);
			glTexParameteri(
				result.Target,
				GLenum.GL_TEXTURE_WRAP_S,
				(int) XNAToGL.Wrap[result.WrapS]
			);
			glTexParameteri(
				result.Target,
				GLenum.GL_TEXTURE_WRAP_T,
				(int) XNAToGL.Wrap[result.WrapT]
			);
			glTexParameteri(
				result.Target,
				GLenum.GL_TEXTURE_WRAP_R,
				(int) XNAToGL.Wrap[result.WrapR]
			);
			glTexParameteri(
				result.Target,
				GLenum.GL_TEXTURE_MAG_FILTER,
				(int) XNAToGL.MagFilter[result.Filter]
			);
			glTexParameteri(
				result.Target,
				GLenum.GL_TEXTURE_MIN_FILTER,
				(int) (result.HasMipmaps ? XNAToGL.MinMipFilter[result.Filter] : XNAToGL.MinFilter[result.Filter])
			);
			glTexParameterf(
				result.Target,
				GLenum.GL_TEXTURE_MAX_ANISOTROPY_EXT,
				(result.Filter == TextureFilter.Anisotropic) ? Math.Max(result.Anistropy, 1.0f) : 1.0f
			);
			glTexParameteri(
				result.Target,
				GLenum.GL_TEXTURE_BASE_LEVEL,
				result.MaxMipmapLevel
			);
			glTexParameterf(
				result.Target,
				GLenum.GL_TEXTURE_LOD_BIAS,
				result.LODBias
			);
			return result;
		}

		public IGLTexture CreateTexture2D(
			SurfaceFormat format,
			int width,
			int height,
			int levelCount
		) {
			OpenGLTexture result = null;

#if !DISABLE_THREADING
			ForceToMainThread(() => {
#endif

			result = CreateTexture(
				typeof(Texture2D),
				levelCount
			);

			GLenum glFormat = XNAToGL.TextureFormat[format];
			GLenum glInternalFormat = XNAToGL.TextureInternalFormat[format];
			if (glFormat == GLenum.GL_COMPRESSED_TEXTURE_FORMATS)
			{
				for (int i = 0; i < levelCount; i += 1)
				{
					int levelWidth = Math.Max(width >> i, 1);
					int levelHeight = Math.Max(height >> i, 1);
					glCompressedTexImage2D(
						GLenum.GL_TEXTURE_2D,
						i,
						(int) glInternalFormat,
						levelWidth,
						levelHeight,
						0,
						((levelWidth + 3) / 4) * ((levelHeight + 3) / 4) * Texture.GetFormatSize(format),
						IntPtr.Zero
					);
				}
			}
			else
			{
				GLenum glType = XNAToGL.TextureDataType[format];
				for (int i = 0; i < levelCount; i += 1)
				{
					glTexImage2D(
						GLenum.GL_TEXTURE_2D,
						i,
						(int) glInternalFormat,
						Math.Max(width >> i, 1),
						Math.Max(height >> i, 1),
						0,
						glFormat,
						glType,
						IntPtr.Zero
					);
				}
			}

#if !DISABLE_THREADING
			});
#endif

			return result;
		}

		public IGLTexture CreateTexture3D(
			SurfaceFormat format,
			int width,
			int height,
			int depth,
			int levelCount
		) {
			OpenGLTexture result = null;

#if !DISABLE_THREADING
			ForceToMainThread(() => {
#endif

			result = CreateTexture(
				typeof(Texture3D),
				levelCount
			);

			GLenum glFormat = XNAToGL.TextureFormat[format];
			GLenum glInternalFormat = XNAToGL.TextureInternalFormat[format];
			GLenum glType = XNAToGL.TextureDataType[format];
			for (int i = 0; i < levelCount; i += 1)
			{
				glTexImage3D(
					GLenum.GL_TEXTURE_3D,
					i,
					(int) glInternalFormat,
					Math.Max(width >> i, 1),
					Math.Max(height >> i, 1),
					depth,
					0,
					glFormat,
					glType,
					IntPtr.Zero
				);
			}

#if !DISABLE_THREADING
			});
#endif

			return result;
		}

		public IGLTexture CreateTextureCube(
			SurfaceFormat format,
			int size,
			int levelCount
		) {
			OpenGLTexture result = null;

#if !DISABLE_THREADING
			ForceToMainThread(() => {
#endif

			result = CreateTexture(
				typeof(TextureCube),
				levelCount
			);

			GLenum glFormat = XNAToGL.TextureFormat[format];
			GLenum glInternalFormat = XNAToGL.TextureInternalFormat[format];
			if (glFormat == GLenum.GL_COMPRESSED_TEXTURE_FORMATS)
			{
				for (int i = 0; i < 6; i += 1)
				{
					for (int l = 0; l < levelCount; l += 1)
					{
						int levelSize = Math.Max(size >> l, 1);
						glCompressedTexImage2D(
							GLenum.GL_TEXTURE_CUBE_MAP_POSITIVE_X + i,
							l,
							(int) glInternalFormat,
							levelSize,
							levelSize,
							0,
							((levelSize + 3) / 4) * ((levelSize + 3) / 4) * Texture.GetFormatSize(format),
							IntPtr.Zero
						);
					}
				}
			}
			else
			{
				GLenum glType = XNAToGL.TextureDataType[format];
				for (int i = 0; i < 6; i += 1)
				{
					for (int l = 0; l < levelCount; l += 1)
					{
						int levelSize = Math.Max(size >> l, 1);
						glTexImage2D(
							GLenum.GL_TEXTURE_CUBE_MAP_POSITIVE_X + i,
							l,
							(int) glInternalFormat,
							levelSize,
							levelSize,
							0,
							glFormat,
							glType,
							IntPtr.Zero
						);
					}
				}
			}

#if !DISABLE_THREADING
			});
#endif

			return result;
		}

		#endregion

		#region glTexSubImage Methods

		public void SetTextureData2D<T>(
			IGLTexture texture,
			SurfaceFormat format,
			int x,
			int y,
			int w,
			int h,
			int level,
			T[] data,
			int startIndex,
			int elementCount
		) where T : struct {
#if !DISABLE_THREADING
			ForceToMainThread(() => {
#endif
			BindTexture(texture);

			GCHandle dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
			int elementSizeInBytes = Marshal.SizeOf(typeof(T));
			int startByte = startIndex * elementSizeInBytes;
			IntPtr dataPtr = (IntPtr) (dataHandle.AddrOfPinnedObject().ToInt64() + startByte);

			GLenum glFormat = XNAToGL.TextureFormat[format];
			try
			{
				if (glFormat == GLenum.GL_COMPRESSED_TEXTURE_FORMATS)
				{
					int dataLength;
					if (elementCount > 0)
					{
						dataLength = elementCount * elementSizeInBytes;
					}
					else
					{
						dataLength = data.Length - startByte;
					}

					/* Note that we're using glInternalFormat, not glFormat.
					 * In this case, they should actually be the same thing,
					 * but we use glFormat somewhat differently for
					 * compressed textures.
					 * -flibit
					 */
					glCompressedTexSubImage2D(
						GLenum.GL_TEXTURE_2D,
						level,
						x,
						y,
						w,
						h,
						XNAToGL.TextureInternalFormat[format],
						dataLength,
						dataPtr
					);
				}
				else
				{
					// Set pixel alignment to match texel size in bytes
					int packSize = Texture.GetFormatSize(format);
					if (packSize != 4)
					{
						glPixelStorei(
							GLenum.GL_UNPACK_ALIGNMENT,
							packSize
						);
					}

					glTexSubImage2D(
						GLenum.GL_TEXTURE_2D,
						level,
						x,
						y,
						w,
						h,
						glFormat,
						XNAToGL.TextureDataType[format],
						dataPtr
					);

					// Keep this state sane -flibit
					if (packSize != 4)
					{
						glPixelStorei(
							GLenum.GL_UNPACK_ALIGNMENT,
							4
						);
					}
				}
			}
			finally
			{
				dataHandle.Free();
			}

#if !DISABLE_THREADING
			});
#endif
		}

		public void SetTextureData3D<T>(
			IGLTexture texture,
			SurfaceFormat format,
			int level,
			int left,
			int top,
			int right,
			int bottom,
			int front,
			int back,
			T[] data,
			int startIndex,
			int elementCount
		) where T : struct {
#if !DISABLE_THREADING
			ForceToMainThread(() => {
#endif
			BindTexture(texture);

			GCHandle dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
			try
			{
				glTexSubImage3D(
					GLenum.GL_TEXTURE_3D,
					level,
					left,
					top,
					front,
					right - left,
					bottom - top,
					back - front,
					XNAToGL.TextureFormat[format],
					XNAToGL.TextureDataType[format],
					(IntPtr) (dataHandle.AddrOfPinnedObject().ToInt64() + startIndex * Marshal.SizeOf(typeof(T)))
				);
			}
			finally
			{
				dataHandle.Free();
			}

#if !DISABLE_THREADING
			});
#endif
		}

		public void SetTextureDataCube<T>(
			IGLTexture texture,
			SurfaceFormat format,
			int xOffset,
			int yOffset,
			int width,
			int height,
			CubeMapFace cubeMapFace,
			int level,
			T[] data,
			int startIndex,
			int elementCount
		) where T : struct {
#if !DISABLE_THREADING
			ForceToMainThread(() => {
#endif
			BindTexture(texture);

			GCHandle dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
			int elementSizeInBytes = Marshal.SizeOf(typeof(T));
			int startByte = startIndex * elementSizeInBytes;
			IntPtr dataPtr = (IntPtr) (dataHandle.AddrOfPinnedObject().ToInt64() + startByte);

			GLenum glFormat = XNAToGL.TextureFormat[format];
			try
			{
				if (glFormat == GLenum.GL_COMPRESSED_TEXTURE_FORMATS)
				{
					int dataLength;
					if (elementCount > 0)
					{
						dataLength = elementCount * elementSizeInBytes;
					}
					else
					{
						dataLength = data.Length - startByte;
					}

					/* Note that we're using glInternalFormat, not glFormat.
					 * In this case, they should actually be the same thing,
					 * but we use glFormat somewhat differently for
					 * compressed textures.
					 * -flibit
					 */
					glCompressedTexSubImage2D(
						GLenum.GL_TEXTURE_CUBE_MAP_POSITIVE_X + (int) cubeMapFace,
						level,
						xOffset,
						yOffset,
						width,
						height,
						XNAToGL.TextureInternalFormat[format],
						dataLength,
						dataPtr
					);
				}
				else
				{
					glTexSubImage2D(
						GLenum.GL_TEXTURE_CUBE_MAP_POSITIVE_X + (int) cubeMapFace,
						level,
						xOffset,
						yOffset,
						width,
						height,
						glFormat,
						XNAToGL.TextureDataType[format],
						dataPtr
					);
				}
			}
			finally
			{
				dataHandle.Free();
			}

#if !DISABLE_THREADING
			});
#endif
		}

		public void SetTextureData2DPointer(
			Texture2D texture,
			IntPtr ptr
		) {
			BindTexture(texture.texture);
			glTexSubImage2D(
				GLenum.GL_TEXTURE_2D,
				0,
				0,
				0,
				texture.Width,
				texture.Height,
				XNAToGL.TextureFormat[texture.Format],
				XNAToGL.TextureDataType[texture.Format],
				ptr
			);
		}

		#endregion

		#region glGetTexImage Methods

		public void GetTextureData2D<T>(
			IGLTexture texture,
			SurfaceFormat format,
			int width,
			int height,
			int level,
			Rectangle? rect,
			T[] data,
			int startIndex,
			int elementCount
		) where T : struct {
#if !DISABLE_THREADING
			ForceToMainThread(() => {
#endif

			if (ReadTargetIfApplicable(
				texture,
				width,
				height,
				level,
				data,
				rect
			)) {
				return;
			}

			BindTexture(texture);
			GLenum glFormat = XNAToGL.TextureFormat[format];
			if (glFormat == GLenum.GL_COMPRESSED_TEXTURE_FORMATS)
			{
				throw new NotImplementedException("GetData, CompressedTexture");
			}
			else if (rect == null)
			{
				// Just throw the whole texture into the user array.
				GCHandle ptr = GCHandle.Alloc(data, GCHandleType.Pinned);
				try
				{
					glGetTexImage(
						GLenum.GL_TEXTURE_2D,
						0,
						glFormat,
						XNAToGL.TextureDataType[format],
						ptr.AddrOfPinnedObject()
					);
				}
				finally
				{
					ptr.Free();
				}
			}
			else
			{
				// Get the whole texture...
				T[] texData = new T[width * height];
				GCHandle ptr = GCHandle.Alloc(texData, GCHandleType.Pinned);
				try
				{
					glGetTexImage(
						GLenum.GL_TEXTURE_2D,
						0,
						glFormat,
						XNAToGL.TextureDataType[format],
						ptr.AddrOfPinnedObject()
					);
				}
				finally
				{
					ptr.Free();
				}

				// Now, blit the rect region into the user array.
				Rectangle region = rect.Value;
				int curPixel = -1;
				for (int row = region.Y; row < region.Y + region.Height; row += 1)
				{
					for (int col = region.X; col < region.X + region.Width; col += 1)
					{
						curPixel += 1;
						if (curPixel < startIndex)
						{
							// If we're not at the start yet, just keep going...
							continue;
						}
						if (curPixel > elementCount)
						{
							// If we're past the end, we're done!
							return;
						}
						data[curPixel - startIndex] = texData[(row * width) + col];
					}
				}
			}

#if !DISABLE_THREADING
			});
#endif
		}

		public void GetTextureDataCube<T>(
			IGLTexture texture,
			SurfaceFormat format,
			int size,
			CubeMapFace cubeMapFace,
			int level,
			Rectangle? rect,
			T[] data,
			int startIndex,
			int elementCount
		) where T : struct {
#if !DISABLE_THREADING
			ForceToMainThread(() => {
#endif

			BindTexture(texture);
			GLenum glFormat = XNAToGL.TextureFormat[format];
			if (glFormat == GLenum.GL_COMPRESSED_TEXTURE_FORMATS)
			{
				throw new NotImplementedException("GetData, CompressedTexture");
			}
			else if (rect == null)
			{
				// Just throw the whole texture into the user array.
				GCHandle ptr = GCHandle.Alloc(data, GCHandleType.Pinned);
				try
				{
					glGetTexImage(
						GLenum.GL_TEXTURE_CUBE_MAP_POSITIVE_X + (int) cubeMapFace,
						0,
						glFormat,
						XNAToGL.TextureDataType[format],
						ptr.AddrOfPinnedObject()
					);
				}
				finally
				{
					ptr.Free();
				}
			}
			else
			{
				// Get the whole texture...
				T[] texData = new T[size * size];
				GCHandle ptr = GCHandle.Alloc(texData, GCHandleType.Pinned);
				try
				{
					glGetTexImage(
						GLenum.GL_TEXTURE_CUBE_MAP_POSITIVE_X + (int) cubeMapFace,
						0,
						glFormat,
						XNAToGL.TextureDataType[format],
						ptr.AddrOfPinnedObject()
					);
				}
				finally
				{
					ptr.Free();
				}

				// Now, blit the rect region into the user array.
				Rectangle region = rect.Value;
				int curPixel = -1;
				for (int row = region.Y; row < region.Y + region.Height; row += 1)
				{
					for (int col = region.X; col < region.X + region.Width; col += 1)
					{
						curPixel += 1;
						if (curPixel < startIndex)
						{
							// If we're not at the start yet, just keep going...
							continue;
						}
						if (curPixel > elementCount)
						{
							// If we're past the end, we're done!
							return;
						}
						data[curPixel - startIndex] = texData[(row * size) + col];
					}
				}
			}

#if !DISABLE_THREADING
			});
#endif
		}

		#endregion

		#region glBindTexture Method

		private void BindTexture(IGLTexture texture)
		{
			OpenGLTexture tex = texture as OpenGLTexture;
			if (tex.Target != Textures[0].Target)
			{
				glBindTexture(Textures[0].Target, 0);
			}
			if (tex != Textures[0])
			{
				glBindTexture(
					tex.Target,
					tex.Handle
				);
			}
			Textures[0] = tex;
		}

		#endregion

		#region glDeleteTexture Method

		private void DeleteTexture(IGLTexture texture)
		{
			uint handle = (texture as OpenGLTexture).Handle;
			for (int i = 0; i < currentAttachments.Length; i += 1)
			{
				if (handle == currentAttachments[i])
				{
					// Force an attachment update, this no longer exists!
					currentAttachments[i] = uint.MaxValue;
				}
			}
			glDeleteTextures(1, ref handle);
		}

		#endregion

		#region glReadPixels Methods

		public void ReadBackbuffer<T>(
			T[] data,
			int startIndex,
			int elementCount,
			Rectangle? rect
		) where T : struct {
			if (startIndex > 0 || elementCount != data.Length)
			{
				throw new NotImplementedException(
					"ReadBackbuffer startIndex/elementCount"
				);
			}

			uint prevReadBuffer = currentReadFramebuffer;
			BindReadFramebuffer(backbuffer.Handle);

			int x;
			int y;
			int w;
			int h;
			if (!rect.HasValue)
			{
				x = rect.Value.X;
				y = rect.Value.Y;
				w = rect.Value.Width;
				h = rect.Value.Height;
			}
			else
			{
				x = 0;
				y = 0;
				w = backbuffer.Width;
				h = backbuffer.Height;
			}

			GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
			try
			{
				glReadPixels(
					x,
					y,
					w,
					h,
					GLenum.GL_RGBA,
					GLenum.GL_UNSIGNED_BYTE,
					handle.AddrOfPinnedObject()
				);
			}
			finally
			{
				handle.Free();
			}

			BindReadFramebuffer(prevReadBuffer);

			// Now we get to do a software-based flip! Yes, really! -flibit
			int pitch = w * 4 / Marshal.SizeOf(typeof(T));
			T[] tempRow = new T[pitch];
			for (int row = 0; row < h / 2; row += 1)
			{
				Array.Copy(data, row * pitch, tempRow, 0, pitch);
				Array.Copy(data, (h - row - 1) * pitch, data, row * pitch, pitch);
				Array.Copy(tempRow, 0, data, (h - row - 1) * pitch, pitch);
			}
		}

		/// <summary>
		/// Attempts to read the texture data directly from the FBO using glReadPixels
		/// </summary>
		/// <typeparam name="T">Texture data type</typeparam>
		/// <param name="texture">The texture to read from</param>
		/// <param name="width">The texture width</param>
		/// <param name="height">The texture height</param>
		/// <param name="level">The texture level</param>
		/// <param name="data">The texture data array</param>
		/// <param name="rect">The portion of the image to read from</param>
		/// <returns>True if we successfully read the texture data</returns>
		private bool ReadTargetIfApplicable<T>(
			IGLTexture texture,
			int width,
			int height,
			int level,
			T[] data,
			Rectangle? rect
		) where T : struct {
			if (	currentDrawBuffers != 1 ||
				currentAttachments[0] != (texture as OpenGLTexture).Handle	)
			{
				return false;
			}

			uint prevReadBuffer = currentReadFramebuffer;
			BindReadFramebuffer(targetFramebuffer);

			int x;
			int y;
			int w;
			int h;
			if (rect.HasValue)
			{
				x = rect.Value.X;
				y = rect.Value.Y;
				w = rect.Value.Width;
				h = rect.Value.Height;
			}
			else
			{
				x = 0;
				y = 0;
				w = width;
				h = height;
			}

			/* glReadPixels should be faster than reading
			 * back from the render target if we are already bound.
			 */
			GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
			try
			{
				glReadPixels(
					x,
					y,
					w,
					h,
					GLenum.GL_RGBA, // FIXME: Assumption!
					GLenum.GL_UNSIGNED_BYTE,
					handle.AddrOfPinnedObject()
				);
			}
			finally
			{
				handle.Free();
			}

			BindReadFramebuffer(prevReadBuffer);
			return true;
		}

		#endregion

		#region glGenerateMipmap Method

		public void GenerateTargetMipmaps(IGLTexture target)
		{
			OpenGLTexture prevTex = Textures[0];
			BindTexture(target);
			glGenerateMipmap((target as OpenGLTexture).Target);
			BindTexture(prevTex);
		}

		#endregion

		#region Framebuffer Methods

		private void BindFramebuffer(uint handle)
		{
			if (	currentReadFramebuffer != handle &&
				currentDrawFramebuffer != handle	)
			{
				glBindFramebuffer(
					GLenum.GL_FRAMEBUFFER,
					handle
				);
				currentReadFramebuffer = handle;
				currentDrawFramebuffer = handle;
			}
			else if (currentReadFramebuffer != handle)
			{
				BindReadFramebuffer(handle);
			}
			else if (currentDrawFramebuffer != handle)
			{
				BindDrawFramebuffer(handle);
			}
		}

		private void BindReadFramebuffer(uint handle)
		{
			if (handle == currentReadFramebuffer)
			{
				return;
			}

			glBindFramebuffer(
				GLenum.GL_READ_FRAMEBUFFER,
				handle
			);

			currentReadFramebuffer = handle;
		}

		private void BindDrawFramebuffer(uint handle)
		{
			if (handle == currentDrawFramebuffer)
			{
				return;
			}

			glBindFramebuffer(
				GLenum.GL_DRAW_FRAMEBUFFER,
				handle
			);

			currentDrawFramebuffer = handle;
		}

		#endregion

		#region Renderbuffer Methods

		public IGLRenderbuffer GenRenderbuffer(int width, int height, DepthFormat format)
		{
			uint handle = 0;

#if !DISABLE_THREADING
			ForceToMainThread(() => {
#endif

			glGenRenderbuffers(1, out handle);
			glBindRenderbuffer(
				GLenum.GL_RENDERBUFFER,
				handle
			);
			glRenderbufferStorage(
				GLenum.GL_RENDERBUFFER,
				XNAToGL.DepthStorage[format],
				width,
				height
			);
			glBindRenderbuffer(
				GLenum.GL_RENDERBUFFER,
				0
			);

#if !DISABLE_THREADING
			});
#endif

			return new OpenGLRenderbuffer(handle);
		}

		private void DeleteRenderbuffer(IGLRenderbuffer renderbuffer)
		{
			uint handle = (renderbuffer as OpenGLRenderbuffer).Handle;
			if (handle == currentRenderbuffer)
			{
				// Force a renderbuffer update, this no longer exists!
				currentRenderbuffer = uint.MaxValue;
			}
			glDeleteRenderbuffers(1, ref handle);
		}

		#endregion

		#region glEnable/glDisable Method

		private void ToggleGLState(GLenum feature, bool enable)
		{
			if (enable)
			{
				glEnable(feature);
			}
			else
			{
				glDisable(feature);
			}
		}

		#endregion

		#region glClear Method

		public void Clear(ClearOptions options, Vector4 color, float depth, int stencil)
		{
			// glClear depends on the scissor rectangle!
			if (scissorTestEnable)
			{
				glDisable(GLenum.GL_SCISSOR_TEST);
			}

			bool clearDepth = (options & ClearOptions.DepthBuffer) == ClearOptions.DepthBuffer;
			bool clearStencil = (options & ClearOptions.Stencil) == ClearOptions.Stencil;

			// Get the clear mask, set the clear properties if needed
			GLenum clearMask = GLenum.GL_ZERO;
			if ((options & ClearOptions.Target) == ClearOptions.Target)
			{
				clearMask |= GLenum.GL_COLOR_BUFFER_BIT;
				if (!color.Equals(currentClearColor))
				{
					glClearColor(
						color.X,
						color.Y,
						color.Z,
						color.W
					);
					currentClearColor = color;
				}
			}
			if (clearDepth)
			{
				clearMask |= GLenum.GL_DEPTH_BUFFER_BIT;
				if (depth != currentClearDepth)
				{
					glClearDepth((double) depth);
					currentClearDepth = depth;
				}
				// glClear depends on the depth write mask!
				if (!zWriteEnable)
				{
					glDepthMask(true);
				}
			}
			if (clearStencil)
			{
				clearMask |= GLenum.GL_STENCIL_BUFFER_BIT;
				if (stencil != currentClearStencil)
				{
					glClearStencil(stencil);
					currentClearStencil = stencil;
				}
				// glClear depends on the stencil write mask!
				if (stencilWriteMask != -1)
				{
					// AKA 0xFFFFFFFF, ugh -flibit
					glStencilMask(-1);
				}
			}

			// CLEAR!
			glClear(clearMask);

			// Clean up after ourselves.
			if (scissorTestEnable)
			{
				glEnable(GLenum.GL_SCISSOR_TEST);
			}
			if (clearDepth && !zWriteEnable)
			{
				glDepthMask(false);
			}
			if (clearStencil && stencilWriteMask != -1) // AKA 0xFFFFFFFF, ugh -flibit
			{
				glStencilMask(stencilWriteMask);
			}
		}

		#endregion

		#region SetRenderTargets Method

		public void SetRenderTargets(
			RenderTargetBinding[] renderTargets,
			IGLRenderbuffer renderbuffer,
			DepthFormat depthFormat
		) {
			// Bind the right framebuffer, if needed
			if (renderTargets == null)
			{
				BindFramebuffer(backbuffer.Handle);
				flipViewport = 1;
				return;
			}
			else
			{
				BindFramebuffer(targetFramebuffer);
				flipViewport = -1;
			}

			int i;
			uint[] attachments = new uint[renderTargets.Length];
			GLenum[] textureTargets = new GLenum[renderTargets.Length];
			for (i = 0; i < renderTargets.Length; i += 1)
			{
				attachments[i] = (renderTargets[i].RenderTarget.texture as OpenGLTexture).Handle;
				if (renderTargets[i].RenderTarget is RenderTarget2D)
				{
					textureTargets[i] = GLenum.GL_TEXTURE_2D;
				}
				else
				{
					textureTargets[i] = GLenum.GL_TEXTURE_CUBE_MAP_POSITIVE_X + (int) renderTargets[i].CubeMapFace;
				}
			}

			// Update the color attachments, DrawBuffers state
			for (i = 0; i < attachments.Length; i += 1)
			{
				if (	attachments[i] != currentAttachments[i] ||
					textureTargets[i] != currentAttachmentFaces[i]	)
				{
					glFramebufferTexture2D(
						GLenum.GL_FRAMEBUFFER,
						GLenum.GL_COLOR_ATTACHMENT0 + i,
						textureTargets[i],
						attachments[i],
						0
					);
					currentAttachments[i] = attachments[i];
					currentAttachmentFaces[i] = textureTargets[i];
				}
			}
			while (i < currentAttachments.Length)
			{
				if (currentAttachments[i] != 0)
				{
					glFramebufferTexture2D(
						GLenum.GL_FRAMEBUFFER,
						GLenum.GL_COLOR_ATTACHMENT0 + i,
						GLenum.GL_TEXTURE_2D,
						0,
						0
					);
					currentAttachments[i] = 0;
					currentAttachmentFaces[i] = GLenum.GL_TEXTURE_2D;
				}
				i += 1;
			}
			if (attachments.Length != currentDrawBuffers)
			{
				glDrawBuffers(attachments.Length, drawBuffersArray);
				currentDrawBuffers = attachments.Length;
			}

			// Update the depth/stencil attachment
			/* FIXME: Notice that we do separate attach calls for the stencil.
			 * We _should_ be able to do a single attach for depthstencil, but
			 * some drivers (like Mesa) cannot into GL_DEPTH_STENCIL_ATTACHMENT.
			 * Use XNAToGL.DepthStencilAttachment when this isn't a problem.
			 * -flibit
			 */
			uint handle;
			if (renderbuffer == null)
			{
				handle = 0;
			}
			else
			{
				handle = (renderbuffer as OpenGLRenderbuffer).Handle;
			}
			if (handle != currentRenderbuffer)
			{
				if (currentDepthStencilFormat == DepthFormat.Depth24Stencil8)
				{
					glFramebufferRenderbuffer(
						GLenum.GL_FRAMEBUFFER,
						GLenum.GL_STENCIL_ATTACHMENT,
						GLenum.GL_RENDERBUFFER,
						0
					);
				}
				currentDepthStencilFormat = depthFormat;
				glFramebufferRenderbuffer(
					GLenum.GL_FRAMEBUFFER,
					GLenum.GL_DEPTH_ATTACHMENT,
					GLenum.GL_RENDERBUFFER,
					handle
				);
				if (currentDepthStencilFormat == DepthFormat.Depth24Stencil8)
				{
					glFramebufferRenderbuffer(
						GLenum.GL_FRAMEBUFFER,
						GLenum.GL_STENCIL_ATTACHMENT,
						GLenum.GL_RENDERBUFFER,
						handle
					);
				}
				currentRenderbuffer = handle;
			}
		}

		#endregion

		#region Query Object Methods

		public IGLQuery CreateQuery()
		{
			uint handle;
			glGenQueries(1, out handle);
			return new OpenGLQuery(handle);
		}

		private void DeleteQuery(IGLQuery query)
		{
			uint handle = (query as OpenGLQuery).Handle;
			glDeleteQueries(
				1,
				ref handle
			);
		}

		public void QueryBegin(IGLQuery query)
		{
			glBeginQuery(
				GLenum.GL_SAMPLES_PASSED,
				(query as OpenGLQuery).Handle
			);
		}

		public void QueryEnd(IGLQuery query)
		{
			// May need to check active queries...?
			glEndQuery(
				GLenum.GL_SAMPLES_PASSED
			);
		}

		public bool QueryComplete(IGLQuery query)
		{
			int result;
			glGetQueryObjectiv(
				(query as OpenGLQuery).Handle,
				GLenum.GL_QUERY_RESULT_AVAILABLE,
				out result
			);
			return result != 0;
		}

		public int QueryPixelCount(IGLQuery query)
		{
			int result;
			glGetQueryObjectiv(
				(query as OpenGLQuery).Handle,
				GLenum.GL_QUERY_RESULT,
				out result
			);
			return result;
		}

		#endregion

		#region XNA->GL Enum Conversion Class

		private static class XNAToGL
		{
			/* Ideally we would be using arrays, rather than Dictionaries.
			 * The problem is that we don't support every enum, and dealing
			 * with gaps would be a headache. So whatever, Dictionaries!
			 * -flibit
			 */

			public static readonly Dictionary<Type, GLenum> TextureType = new Dictionary<Type, GLenum>()
			{
				{ typeof(Texture2D), GLenum.GL_TEXTURE_2D },
				{ typeof(Texture3D), GLenum.GL_TEXTURE_3D },
				{ typeof(TextureCube), GLenum.GL_TEXTURE_CUBE_MAP }
			};

			public static readonly Dictionary<SurfaceFormat, GLenum> TextureFormat = new Dictionary<SurfaceFormat, GLenum>()
			{
				{ SurfaceFormat.Color,			GLenum.GL_RGBA },
				{ SurfaceFormat.Bgr565,			GLenum.GL_RGB },
				{ SurfaceFormat.Bgra5551,		GLenum.GL_BGRA },
				{ SurfaceFormat.Bgra4444,		GLenum.GL_BGRA },
				{ SurfaceFormat.Dxt1,			GLenum.GL_COMPRESSED_TEXTURE_FORMATS },
				{ SurfaceFormat.Dxt3,			GLenum.GL_COMPRESSED_TEXTURE_FORMATS },
				{ SurfaceFormat.Dxt5,			GLenum.GL_COMPRESSED_TEXTURE_FORMATS },
				{ SurfaceFormat.NormalizedByte2,	GLenum.GL_RG }, // Unconfirmed!
				{ SurfaceFormat.NormalizedByte4,	GLenum.GL_RGBA }, // Unconfirmed!
				{ SurfaceFormat.Rgba1010102,		GLenum.GL_RGBA },
				{ SurfaceFormat.Rg32,			GLenum.GL_RG },
				{ SurfaceFormat.Rgba64,			GLenum.GL_RGBA },
				{ SurfaceFormat.Alpha8,			GLenum.GL_LUMINANCE },
				{ SurfaceFormat.Single,			GLenum.GL_RED },
				{ SurfaceFormat.Vector2,		GLenum.GL_RG },
				{ SurfaceFormat.Vector4,		GLenum.GL_RGBA },
				{ SurfaceFormat.HalfSingle,		GLenum.GL_RED },
				{ SurfaceFormat.HalfVector2,		GLenum.GL_RG },
				{ SurfaceFormat.HalfVector4,		GLenum.GL_RGBA },
				{ SurfaceFormat.HdrBlendable,		GLenum.GL_RGBA }
			};

			public static readonly Dictionary<SurfaceFormat, GLenum> TextureInternalFormat = new Dictionary<SurfaceFormat, GLenum>()
			{
				{ SurfaceFormat.Color,			GLenum.GL_RGBA },
				{ SurfaceFormat.Bgr565,			GLenum.GL_RGB },
				{ SurfaceFormat.Bgra5551,		GLenum.GL_RGBA },
				{ SurfaceFormat.Bgra4444,		GLenum.GL_RGBA4 },
				{ SurfaceFormat.Dxt1,			GLenum.GL_COMPRESSED_RGBA_S3TC_DXT1_EXT },
				{ SurfaceFormat.Dxt3,			GLenum.GL_COMPRESSED_RGBA_S3TC_DXT3_EXT },
				{ SurfaceFormat.Dxt5,			GLenum.GL_COMPRESSED_RGBA_S3TC_DXT5_EXT },
				{ SurfaceFormat.NormalizedByte2,	GLenum.GL_RG8I }, // Unconfirmed!
				{ SurfaceFormat.NormalizedByte4,	GLenum.GL_RGBA8I }, // Unconfirmed!
				{ SurfaceFormat.Rgba1010102,		GLenum.GL_RGB10_A2_EXT },
				{ SurfaceFormat.Rg32,			GLenum.GL_RG16 },
				{ SurfaceFormat.Rgba64,			GLenum.GL_RGBA16 },
				{ SurfaceFormat.Alpha8,			GLenum.GL_LUMINANCE },
				{ SurfaceFormat.Single,			GLenum.GL_R32F },
				{ SurfaceFormat.Vector2,		GLenum.GL_RG32F },
				{ SurfaceFormat.Vector4,		GLenum.GL_RGBA32F },
				{ SurfaceFormat.HalfSingle,		GLenum.GL_R16F },
				{ SurfaceFormat.HalfVector2,		GLenum.GL_RG16F },
				{ SurfaceFormat.HalfVector4,		GLenum.GL_RGBA16F },
				{ SurfaceFormat.HdrBlendable,		GLenum.GL_RGBA16F }
			};

			public static readonly Dictionary<SurfaceFormat, GLenum> TextureDataType = new Dictionary<SurfaceFormat, GLenum>()
			{
				{ SurfaceFormat.Color,			GLenum.GL_UNSIGNED_BYTE },
				{ SurfaceFormat.Bgr565,			GLenum.GL_UNSIGNED_SHORT_5_6_5 },
				{ SurfaceFormat.Bgra5551,		GLenum.GL_UNSIGNED_SHORT_5_5_5_1 },
				{ SurfaceFormat.Bgra4444,		GLenum.GL_UNSIGNED_SHORT_4_4_4_4 },
				// Ignoring Dxt1, Dxt3, Dxt5
				{ SurfaceFormat.NormalizedByte2,	GLenum.GL_BYTE }, // Unconfirmed!
				{ SurfaceFormat.NormalizedByte4,	GLenum.GL_BYTE }, // Unconfirmed!
				{ SurfaceFormat.Rgba1010102,		GLenum.GL_UNSIGNED_INT_10_10_10_2 },
				{ SurfaceFormat.Rg32,			GLenum.GL_UNSIGNED_SHORT },
				{ SurfaceFormat.Rgba64,			GLenum.GL_UNSIGNED_SHORT },
				{ SurfaceFormat.Alpha8,			GLenum.GL_UNSIGNED_BYTE },
				{ SurfaceFormat.Single,			GLenum.GL_FLOAT },
				{ SurfaceFormat.Vector2,		GLenum.GL_FLOAT },
				{ SurfaceFormat.Vector4,		GLenum.GL_FLOAT },
				{ SurfaceFormat.HalfSingle,		GLenum.GL_HALF_FLOAT },
				{ SurfaceFormat.HalfVector2,		GLenum.GL_HALF_FLOAT },
				{ SurfaceFormat.HalfVector4,		GLenum.GL_HALF_FLOAT },
				{ SurfaceFormat.HdrBlendable,		GLenum.GL_HALF_FLOAT }
			};

			public static readonly Dictionary<Blend, GLenum> BlendMode = new Dictionary<Blend, GLenum>()
			{
				{ Blend.DestinationAlpha,		GLenum.GL_DST_ALPHA },
				{ Blend.DestinationColor,		GLenum.GL_DST_COLOR },
				{ Blend.InverseDestinationAlpha,	GLenum.GL_ONE_MINUS_DST_ALPHA },
				{ Blend.InverseDestinationColor,	GLenum.GL_ONE_MINUS_DST_COLOR },
				{ Blend.InverseSourceAlpha,		GLenum.GL_ONE_MINUS_SRC_ALPHA },
				{ Blend.InverseSourceColor,		GLenum.GL_ONE_MINUS_SRC_COLOR },
				{ Blend.One,				GLenum.GL_ONE },
				{ Blend.SourceAlpha,			GLenum.GL_SRC_ALPHA },
				{ Blend.SourceAlphaSaturation,		GLenum.GL_SRC_ALPHA_SATURATE },
				{ Blend.SourceColor,			GLenum.GL_SRC_COLOR },
				{ Blend.Zero,				GLenum.GL_ZERO }
			};

			public static readonly Dictionary<BlendFunction, GLenum> BlendEquation = new Dictionary<BlendFunction, GLenum>()
			{
				{ BlendFunction.Add,			GLenum.GL_FUNC_ADD },
				{ BlendFunction.Max,			GLenum.GL_MAX },
				{ BlendFunction.Min,			GLenum.GL_MIN },
				{ BlendFunction.ReverseSubtract,	GLenum.GL_FUNC_REVERSE_SUBTRACT },
				{ BlendFunction.Subtract,		GLenum.GL_FUNC_SUBTRACT }
			};

			public static readonly Dictionary<CompareFunction, GLenum> CompareFunc = new Dictionary<CompareFunction, GLenum>()
			{
				{ CompareFunction.Always,	GLenum.GL_ALWAYS },
				{ CompareFunction.Equal,	GLenum.GL_EQUAL },
				{ CompareFunction.Greater,	GLenum.GL_GREATER },
				{ CompareFunction.GreaterEqual,	GLenum.GL_GEQUAL },
				{ CompareFunction.Less,		GLenum.GL_LESS },
				{ CompareFunction.LessEqual,	GLenum.GL_LEQUAL },
				{ CompareFunction.Never,	GLenum.GL_NEVER },
				{ CompareFunction.NotEqual,	GLenum.GL_NOTEQUAL }
			};

			public static readonly Dictionary<StencilOperation, GLenum> GLStencilOp = new Dictionary<StencilOperation, GLenum>()
			{
				{ StencilOperation.Decrement,		GLenum.GL_DECR_WRAP },
				{ StencilOperation.DecrementSaturation,	GLenum.GL_DECR },
				{ StencilOperation.Increment,		GLenum.GL_INCR_WRAP },
				{ StencilOperation.IncrementSaturation,	GLenum.GL_INCR },
				{ StencilOperation.Invert,		GLenum.GL_INVERT },
				{ StencilOperation.Keep,		GLenum.GL_KEEP },
				{ StencilOperation.Replace,		GLenum.GL_REPLACE },
				{ StencilOperation.Zero,		GLenum.GL_ZERO }
			};

			public static readonly Dictionary<CullMode, GLenum> FrontFace = new Dictionary<CullMode, GLenum>()
			{
				{ CullMode.CullClockwiseFace,		GLenum.GL_CW },
				{ CullMode.CullCounterClockwiseFace,	GLenum.GL_CCW }
			};

			public static readonly Dictionary<FillMode, GLenum> GLFillMode = new Dictionary<FillMode, GLenum>()
			{
				{ FillMode.Solid,	GLenum.GL_FILL },
				{ FillMode.WireFrame,	GLenum.GL_LINE }
			};

			public static readonly Dictionary<TextureAddressMode, GLenum> Wrap = new Dictionary<TextureAddressMode, GLenum>()
			{
				{ TextureAddressMode.Clamp,	GLenum.GL_CLAMP_TO_EDGE },
				{ TextureAddressMode.Mirror,	GLenum.GL_MIRRORED_REPEAT },
				{ TextureAddressMode.Wrap,	GLenum.GL_REPEAT }
			};

			public static readonly Dictionary<TextureFilter, GLenum> MagFilter = new Dictionary<TextureFilter, GLenum>()
			{
				{ TextureFilter.Point,				GLenum.GL_NEAREST },
				{ TextureFilter.Linear,				GLenum.GL_LINEAR },
				{ TextureFilter.Anisotropic,			GLenum.GL_LINEAR },
				{ TextureFilter.LinearMipPoint,			GLenum.GL_LINEAR },
				{ TextureFilter.MinPointMagLinearMipPoint,	GLenum.GL_LINEAR },
				{ TextureFilter.MinPointMagLinearMipLinear,	GLenum.GL_LINEAR },
				{ TextureFilter.MinLinearMagPointMipPoint,	GLenum.GL_NEAREST },
				{ TextureFilter.MinLinearMagPointMipLinear,	GLenum.GL_NEAREST }
			};

			public static readonly Dictionary<TextureFilter, GLenum> MinMipFilter = new Dictionary<TextureFilter, GLenum>()
			{
				{ TextureFilter.Point,				GLenum.GL_NEAREST_MIPMAP_NEAREST },
				{ TextureFilter.Linear,				GLenum.GL_LINEAR_MIPMAP_LINEAR },
				{ TextureFilter.Anisotropic,			GLenum.GL_LINEAR_MIPMAP_LINEAR },
				{ TextureFilter.LinearMipPoint,			GLenum.GL_LINEAR_MIPMAP_NEAREST },
				{ TextureFilter.MinPointMagLinearMipPoint,	GLenum.GL_NEAREST_MIPMAP_NEAREST },
				{ TextureFilter.MinPointMagLinearMipLinear,	GLenum.GL_NEAREST_MIPMAP_LINEAR },
				{ TextureFilter.MinLinearMagPointMipPoint,	GLenum.GL_LINEAR_MIPMAP_NEAREST },
				{ TextureFilter.MinLinearMagPointMipLinear,	GLenum.GL_LINEAR_MIPMAP_LINEAR }
			};

			public static readonly Dictionary<TextureFilter, GLenum> MinFilter = new Dictionary<TextureFilter, GLenum>()
			{
				{ TextureFilter.Point,				GLenum.GL_NEAREST },
				{ TextureFilter.Linear,				GLenum.GL_LINEAR },
				{ TextureFilter.Anisotropic,			GLenum.GL_LINEAR },
				{ TextureFilter.LinearMipPoint,			GLenum.GL_LINEAR },
				{ TextureFilter.MinPointMagLinearMipPoint,	GLenum.GL_NEAREST },
				{ TextureFilter.MinPointMagLinearMipLinear,	GLenum.GL_NEAREST },
				{ TextureFilter.MinLinearMagPointMipPoint,	GLenum.GL_LINEAR },
				{ TextureFilter.MinLinearMagPointMipLinear,	GLenum.GL_LINEAR }
			};

			public static readonly Dictionary<DepthFormat, GLenum> DepthStencilAttachment = new Dictionary<DepthFormat, GLenum>()
			{
				{ DepthFormat.Depth16,		GLenum.GL_DEPTH_ATTACHMENT },
				{ DepthFormat.Depth24,		GLenum.GL_DEPTH_ATTACHMENT },
				{ DepthFormat.Depth24Stencil8,	GLenum.GL_DEPTH_STENCIL_ATTACHMENT }
			};

			public static readonly Dictionary<DepthFormat, GLenum> DepthStorage = new Dictionary<DepthFormat, GLenum>()
			{
				{ DepthFormat.Depth16,		GLenum.GL_DEPTH_COMPONENT16 },
				{ DepthFormat.Depth24,		GLenum.GL_DEPTH_COMPONENT24 },
				{ DepthFormat.Depth24Stencil8,	GLenum.GL_DEPTH24_STENCIL8 }
			};

			public static readonly Dictionary<DepthFormat, GLenum> GLDepthFormat = new Dictionary<DepthFormat, GLenum>()
			{
				{ DepthFormat.Depth16,		GLenum.GL_DEPTH_COMPONENT },
				{ DepthFormat.Depth24,		GLenum.GL_DEPTH_COMPONENT },
				{ DepthFormat.Depth24Stencil8,	GLenum.GL_DEPTH_STENCIL }
			};

			public static readonly Dictionary<DepthFormat, GLenum> DepthType = new Dictionary<DepthFormat, GLenum>()
			{
				{ DepthFormat.Depth16,		GLenum.GL_UNSIGNED_BYTE },
				{ DepthFormat.Depth24,		GLenum.GL_UNSIGNED_BYTE },
				{ DepthFormat.Depth24Stencil8,	GLenum.GL_UNSIGNED_INT_24_8 }
			};

			public static readonly Dictionary<VertexElementUsage, MojoShader.MOJOSHADER_usage> VertexAttribUsage = new Dictionary<VertexElementUsage, MojoShader.MOJOSHADER_usage>()
			{
				{ VertexElementUsage.Position,		MojoShader.MOJOSHADER_usage.MOJOSHADER_USAGE_POSITION },
				{ VertexElementUsage.Color,		MojoShader.MOJOSHADER_usage.MOJOSHADER_USAGE_COLOR },
				{ VertexElementUsage.TextureCoordinate,	MojoShader.MOJOSHADER_usage.MOJOSHADER_USAGE_TEXCOORD },
				{ VertexElementUsage.Normal,		MojoShader.MOJOSHADER_usage.MOJOSHADER_USAGE_NORMAL },
				{ VertexElementUsage.Binormal,		MojoShader.MOJOSHADER_usage.MOJOSHADER_USAGE_BINORMAL },
				{ VertexElementUsage.Tangent,		MojoShader.MOJOSHADER_usage.MOJOSHADER_USAGE_TANGENT },
				{ VertexElementUsage.BlendIndices,	MojoShader.MOJOSHADER_usage.MOJOSHADER_USAGE_BLENDINDICES },
				{ VertexElementUsage.BlendWeight,	MojoShader.MOJOSHADER_usage.MOJOSHADER_USAGE_BLENDWEIGHT },
				{ VertexElementUsage.Depth,		MojoShader.MOJOSHADER_usage.MOJOSHADER_USAGE_DEPTH },
				{ VertexElementUsage.Fog,		MojoShader.MOJOSHADER_usage.MOJOSHADER_USAGE_FOG },
				{ VertexElementUsage.PointSize,		MojoShader.MOJOSHADER_usage.MOJOSHADER_USAGE_POINTSIZE },
				{ VertexElementUsage.Sample,		MojoShader.MOJOSHADER_usage.MOJOSHADER_USAGE_SAMPLE },
				{ VertexElementUsage.TessellateFactor,	MojoShader.MOJOSHADER_usage.MOJOSHADER_USAGE_TESSFACTOR }
			};

			public static readonly Dictionary<VertexElementFormat, uint> VertexAttribSize = new Dictionary<VertexElementFormat, uint>()
			{
				{ VertexElementFormat.Single,		1 },
				{ VertexElementFormat.Vector2,		2 },
				{ VertexElementFormat.Vector3,		3 },
				{ VertexElementFormat.Vector4,		4 },
				{ VertexElementFormat.Color,		4 },
				{ VertexElementFormat.Byte4,		4 },
				{ VertexElementFormat.Short2,		2 },
				{ VertexElementFormat.Short4,		2 },
				{ VertexElementFormat.NormalizedShort2,	2 },
				{ VertexElementFormat.NormalizedShort4,	4 },
				{ VertexElementFormat.HalfVector2,	2 },
				{ VertexElementFormat.HalfVector4,	4 }
			};

			public static readonly Dictionary<VertexElementFormat, MojoShader.MOJOSHADER_attributeType> VertexAttribType = new Dictionary<VertexElementFormat, MojoShader.MOJOSHADER_attributeType>()
			{
				{ VertexElementFormat.Single,		MojoShader.MOJOSHADER_attributeType.MOJOSHADER_ATTRIBUTE_FLOAT },
				{ VertexElementFormat.Vector2,		MojoShader.MOJOSHADER_attributeType.MOJOSHADER_ATTRIBUTE_FLOAT },
				{ VertexElementFormat.Vector3,		MojoShader.MOJOSHADER_attributeType.MOJOSHADER_ATTRIBUTE_FLOAT },
				{ VertexElementFormat.Vector4,		MojoShader.MOJOSHADER_attributeType.MOJOSHADER_ATTRIBUTE_FLOAT },
				{ VertexElementFormat.Color,		MojoShader.MOJOSHADER_attributeType.MOJOSHADER_ATTRIBUTE_UBYTE },
				{ VertexElementFormat.Byte4,		MojoShader.MOJOSHADER_attributeType.MOJOSHADER_ATTRIBUTE_UBYTE },
				{ VertexElementFormat.Short2,		MojoShader.MOJOSHADER_attributeType.MOJOSHADER_ATTRIBUTE_SHORT },
				{ VertexElementFormat.Short4,		MojoShader.MOJOSHADER_attributeType.MOJOSHADER_ATTRIBUTE_SHORT },
				{ VertexElementFormat.NormalizedShort2,	MojoShader.MOJOSHADER_attributeType.MOJOSHADER_ATTRIBUTE_SHORT },
				{ VertexElementFormat.NormalizedShort4,	MojoShader.MOJOSHADER_attributeType.MOJOSHADER_ATTRIBUTE_SHORT },
				{ VertexElementFormat.HalfVector2,	MojoShader.MOJOSHADER_attributeType.MOJOSHADER_ATTRIBUTE_HALF_FLOAT },
				{ VertexElementFormat.HalfVector4,	MojoShader.MOJOSHADER_attributeType.MOJOSHADER_ATTRIBUTE_HALF_FLOAT }
			};

			public static int VertexAttribNormalized(VertexElement element)
			{
				if (element.VertexElementUsage == VertexElementUsage.Color)
				{
					return 1;
				}
				if (	element.VertexElementFormat == VertexElementFormat.NormalizedShort2 ||
					element.VertexElementFormat == VertexElementFormat.NormalizedShort4	)
				{
					return 1;
				}
				return 0;
			}

			public static Dictionary<PrimitiveType, GLenum> Primitive = new Dictionary<PrimitiveType, GLenum>()
			{
				{ PrimitiveType.LineList,	GLenum.GL_LINES },
				{ PrimitiveType.LineStrip,	GLenum.GL_LINE_STRIP },
				{ PrimitiveType.TriangleList,	GLenum.GL_TRIANGLES },
				{ PrimitiveType.TriangleStrip,	GLenum.GL_TRIANGLE_STRIP }
			};

			public static int PrimitiveVerts(PrimitiveType primitiveType, int primitiveCount)
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
						return primitiveCount + 2;
				}
				throw new NotSupportedException();
			}
		}

		#endregion

		#region The Faux-Backbuffer

		private class OpenGLBackbuffer : IGLBackbuffer
		{
			public uint Handle
			{
				get;
				private set;
			}

			public int Width
			{
				get;
				private set;
			}

			public int Height
			{
				get;
				private set;
			}

#if !DISABLE_FAUXBACKBUFFER
			private uint colorAttachment;
			private uint depthStencilAttachment;
			private DepthFormat depthStencilFormat;
			private OpenGLDevice glDevice;
#endif

			public OpenGLBackbuffer(
				OpenGLDevice device,
				int width,
				int height,
				DepthFormat depthFormat
			) {
				Width = width;
				Height = height;
#if DISABLE_FAUXBACKBUFFER
				Handle = 0;
#else
				glDevice = device;
				depthStencilFormat = depthFormat;

				// Generate and bind the FBO.
				uint handle;
				glDevice.glGenFramebuffers(1, out handle);
				Handle = handle;
				glDevice.BindFramebuffer(Handle);

				// Create and attach the color buffer
				glDevice.glGenTextures(1, out colorAttachment);
				glDevice.glBindTexture(GLenum.GL_TEXTURE_2D, colorAttachment);
				glDevice.glTexImage2D(
					GLenum.GL_TEXTURE_2D,
					0,
					(int) GLenum.GL_RGBA,
					width,
					height,
					0,
					GLenum.GL_RGBA,
					GLenum.GL_UNSIGNED_BYTE,
					IntPtr.Zero
				);
				glDevice.glFramebufferTexture2D(
					GLenum.GL_FRAMEBUFFER,
					GLenum.GL_COLOR_ATTACHMENT0,
					GLenum.GL_TEXTURE_2D,
					colorAttachment,
					0
				);

				if (depthFormat == DepthFormat.None)
				{
					// Don't bother creating a depth/stencil texture.
					depthStencilAttachment = 0;

					// Keep this state sane.
					glDevice.glBindTexture(GLenum.GL_TEXTURE_2D, 0);

					return;
				}

				// Create and attach the depth/stencil buffer
				glDevice.glGenTextures(1, out depthStencilAttachment);
				glDevice.glBindTexture(GLenum.GL_TEXTURE_2D, depthStencilAttachment);
				glDevice.glTexImage2D(
					GLenum.GL_TEXTURE_2D,
					0,
					(int) XNAToGL.DepthStorage[depthFormat],
					width,
					height,
					0,
					XNAToGL.GLDepthFormat[depthFormat],
					XNAToGL.DepthType[depthFormat],
					IntPtr.Zero
				);
				glDevice.glFramebufferTexture2D(
					GLenum.GL_FRAMEBUFFER,
					XNAToGL.DepthStencilAttachment[depthFormat],
					GLenum.GL_TEXTURE_2D,
					depthStencilAttachment,
					0
				);

				// Keep this state sane.
				glDevice.glBindTexture(GLenum.GL_TEXTURE_2D, 0);
#endif
			}

			public void Dispose()
			{
#if !DISABLE_FAUXBACKBUFFER
				uint handle = Handle;
				glDevice.glDeleteFramebuffers(1, ref handle);
				glDevice.glDeleteTextures(1, ref colorAttachment);
				if (depthStencilAttachment != 0)
				{
					glDevice.glDeleteTextures(1, ref depthStencilAttachment);
				}
				glDevice = null;
				Handle = 0;
#endif
			}

			public void ResetFramebuffer(
				int width,
				int height,
				DepthFormat depthFormat,
				bool renderTargetBound
			) {
				Width = width;
				Height = height;
#if !DISABLE_FAUXBACKBUFFER
				// Update our color attachment to the new resolution.
				glDevice.glBindTexture(GLenum.GL_TEXTURE_2D, colorAttachment);
				glDevice.glTexImage2D(
					GLenum.GL_TEXTURE_2D,
					0,
					(int) GLenum.GL_RGBA,
					width,
					height,
					0,
					GLenum.GL_RGBA,
					GLenum.GL_UNSIGNED_BYTE,
					IntPtr.Zero
				);

				if (depthFormat == DepthFormat.None)
				{
					// Remove depth/stencil attachment, if applicable
					if (depthStencilAttachment != 0)
					{
						glDevice.BindFramebuffer(Handle);
						glDevice.glFramebufferTexture2D(
							GLenum.GL_FRAMEBUFFER,
							XNAToGL.DepthStencilAttachment[depthStencilFormat],
							GLenum.GL_TEXTURE_2D,
							0,
							0
						);
						glDevice.glDeleteTextures(
							1,
							ref depthStencilAttachment
						);
						depthStencilAttachment = 0;
						if (renderTargetBound)
						{
							glDevice.BindFramebuffer(
								glDevice.targetFramebuffer
							);
						}
						depthStencilFormat = DepthFormat.None;
					}
					return;
				}
				else if (depthStencilAttachment == 0)
				{
					// Generate a depth/stencil texture, if needed
					glDevice.glGenTextures(
						1,
						out depthStencilAttachment
					);
				}

				// Update the depth/stencil texture
				glDevice.glBindTexture(GLenum.GL_TEXTURE_2D, depthStencilAttachment);
				glDevice.glTexImage2D(
					GLenum.GL_TEXTURE_2D,
					0,
					(int) XNAToGL.DepthStorage[depthFormat],
					width,
					height,
					0,
					XNAToGL.GLDepthFormat[depthFormat],
					XNAToGL.DepthType[depthFormat],
					IntPtr.Zero
				);

				// If the depth format changes, detach before reattaching!
				if (depthFormat != depthStencilFormat)
				{
					glDevice.BindFramebuffer(Handle);

					// Detach and reattach the depth texture
					if (depthStencilFormat != DepthFormat.None)
					{
						glDevice.glFramebufferTexture2D(
							GLenum.GL_FRAMEBUFFER,
							XNAToGL.DepthStencilAttachment[depthStencilFormat],
							GLenum.GL_TEXTURE_2D,
							0,
							0
						);
					}
					glDevice.glFramebufferTexture2D(
						GLenum.GL_FRAMEBUFFER,
						XNAToGL.DepthStencilAttachment[depthFormat],
						GLenum.GL_TEXTURE_2D,
						depthStencilAttachment,
						0
					);

					if (renderTargetBound)
					{
						glDevice.BindFramebuffer(
							glDevice.targetFramebuffer
						);
					}

					depthStencilFormat = depthFormat;
				}

				// Keep this state sane.
				glDevice.glBindTexture(
					GLenum.GL_TEXTURE_2D,
					glDevice.Textures[0].Handle
				);
#endif
			}
		}

		#endregion

		#region Threaded GL Nonsense

		private int mainThreadId;

		private bool IsOnMainThread()
		{
			return mainThreadId == Thread.CurrentThread.ManagedThreadId;
		}

		private void InitThreadedGL(IntPtr window)
		{
			mainThreadId = Thread.CurrentThread.ManagedThreadId;
#if THREADED_GL
			// Create a background context
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_SHARE_WITH_CURRENT_CONTEXT, 1);
			WindowInfo = window;
			BackgroundContext = new GL_ContextHandle()
			{
				context = SDL.SDL_GL_CreateContext(window)
			};

			// Make the foreground context current.
			SDL.SDL_GL_MakeCurrent(window, glContext);

			// We're going to need glFlush, so load this entry point.
			glFlush = (Flush) Marshal.GetDelegateForFunctionPointer(
				SDL.SDL_GL_GetProcAddress("glFlush"),
				typeof(Flush)
			);
#endif
		}

#if !DISABLE_THREADING

#if THREADED_GL
		private class GL_ContextHandle
		{
			public IntPtr context;
		}
		private GL_ContextHandle BackgroundContext;
		private IntPtr WindowInfo;
		private delegate void Flush();
		private Flush glFlush;

#else
		private List<Action> actions = new List<Action>();
		private void RunActions()
		{
			lock (actions)
			{
				foreach (Action action in actions)
				{
					action();
				}
				actions.Clear();
			}
		}
#endif

		private void ForceToMainThread(Action action)
		{
			// If we're already on the main thread, just call the action.
			if (mainThreadId == Thread.CurrentThread.ManagedThreadId)
			{
				action();
				return;
			}

#if THREADED_GL
			lock (BackgroundContext)
			{
				// Make the context current on this thread.
				SDL.SDL_GL_MakeCurrent(WindowInfo, BackgroundContext.context);

				// Execute the action.
				action();

				// Must flush the GL calls now before we release the context.
				glFlush();

				// Free the threaded context for the next threaded call...
				SDL.SDL_GL_MakeCurrent(WindowInfo, IntPtr.Zero);
			}
#else
			ManualResetEventSlim resetEvent = new ManualResetEventSlim(false);
			lock (actions)
			{
				actions.Add(() =>
				{
					action();
					resetEvent.Set();
				});
			}
			resetEvent.Wait();
#endif
		}

#endif // !DISABLE_THREADING

		#endregion
	}
}
