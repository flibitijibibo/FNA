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
 * Also note that this affects Threading.cs and SDL2/SDL2_GamePlatform.cs!
 * Check THREADED_GL there too.
 *
 * Basically, if you have to enable this, you should feel very bad.
 * -flibit
 */
#endregion

#region Using Statements
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using SDL2;
#endregion

namespace Microsoft.Xna.Framework.Graphics
{
	internal partial class OpenGLDevice
	{
		#region OpenGL Texture Container Class

		public class OpenGLTexture
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

			public SurfaceFormat Format
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
				SurfaceFormat format,
				bool hasMipmaps
			) {
				Handle = handle;
				Target = XNAToGL.TextureType[target];
				Format = format;
				HasMipmaps = hasMipmaps;

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

		#region OpenGL Vertex Buffer Container Class

		public class OpenGLVertexBuffer
		{
			public uint Handle
			{
				get;
				private set;
			}

			public GLenum Dynamic
			{
				get;
				private set;
			}

			public OpenGLVertexBuffer(
				GraphicsDevice graphicsDevice,
				bool dynamic,
				int vertexCount,
				int vertexStride
			) {
				uint handle;
				graphicsDevice.GLDevice.glGenBuffers(1, out handle);
				Handle = handle;
				Dynamic = dynamic ? GLenum.GL_STREAM_DRAW : GLenum.GL_STATIC_DRAW;

				graphicsDevice.GLDevice.BindVertexBuffer(this);
				graphicsDevice.GLDevice.glBufferData(
					GLenum.GL_ARRAY_BUFFER,
					(IntPtr) (vertexStride * vertexCount),
					IntPtr.Zero,
					Dynamic
				);
			}

			private OpenGLVertexBuffer()
			{
				Handle = 0;
			}
			public static readonly OpenGLVertexBuffer NullBuffer = new OpenGLVertexBuffer();
		}

		#endregion

		#region OpenGL Index Buffer Container Class

		public class OpenGLIndexBuffer
		{
			public uint Handle
			{
				get;
				private set;
			}

			public GLenum Dynamic
			{
				get;
				private set;
			}

			public IntPtr BufferSize
			{
				get;
				private set;
			}

			public OpenGLIndexBuffer(
				GraphicsDevice graphicsDevice,
				bool dynamic,
				int indexCount,
				IndexElementSize elementSize
			) {
				uint handle;
				graphicsDevice.GLDevice.glGenBuffers(1, out handle);
				Handle = handle;
				Dynamic = dynamic ? GLenum.GL_STREAM_DRAW : GLenum.GL_STATIC_DRAW;
				BufferSize = (IntPtr) (indexCount * (elementSize == IndexElementSize.SixteenBits ? 2 : 4));

				graphicsDevice.GLDevice.BindIndexBuffer(this);
				graphicsDevice.GLDevice.glBufferData(
					GLenum.GL_ELEMENT_ARRAY_BUFFER,
					BufferSize,
					IntPtr.Zero,
					Dynamic
				);
			}

			private OpenGLIndexBuffer()
			{
				Handle = 0;
			}
			public static readonly OpenGLIndexBuffer NullBuffer = new OpenGLIndexBuffer();
		}

		#endregion

		#region OpenGL Effect Container Class

		public class OpenGLEffect
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

		#region Alpha Blending State Variables

		internal bool alphaBlendEnable = false;
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

		internal bool zEnable = false;
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

		internal bool scissorTestEnable = false;
		internal CullMode cullFrontFace = CullMode.None;
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

		// FIXME: This doesn't need to be public. Blame VideoPlayer. -flibit
		public OpenGLTexture[] Textures
		{
			get;
			private set;
		}

		#endregion

		#region Buffer Binding Cache Variables

		private uint currentVertexBuffer = 0;
		private uint currentIndexBuffer = 0;

		#endregion

		#region Render Target Cache Variables

		private uint currentReadFramebuffer = 0;
		public uint CurrentReadFramebuffer
		{
			get
			{
				return currentReadFramebuffer;
			}
		}

		private uint currentDrawFramebuffer = 0;
		public uint CurrentDrawFramebuffer
		{
			get
			{
				return currentDrawFramebuffer;
			}
		}

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

		public FauxBackbuffer Backbuffer
		{
			get;
			private set;
		}

		#endregion

		#region OpenGL Extensions List, Device Capabilities Variables

		public string Extensions
		{
			get;
			private set;
		}

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

		#endregion

		#region Private MojoShader Interop

		private string shaderProfile;
		private IntPtr shaderContext;

		private IntPtr currentEffect = IntPtr.Zero;
		private IntPtr currentTechnique = IntPtr.Zero;
		private uint currentPass = 0;

		private int flipViewport;

		private static IntPtr glGetProcAddress(string name, IntPtr d)
		{
			return SDL.SDL_GL_GetProcAddress(name);
		}
		private static MojoShader.MOJOSHADER_glGetProcAddress GLGetProcAddress = glGetProcAddress;

		#endregion

		#region Public Constructor

		public OpenGLDevice(
			PresentationParameters presentationParameters
		) {
			// Create OpenGL context
			glContext = SDL.SDL_GL_CreateContext(
				presentationParameters.DeviceWindowHandle
			);

#if THREADED_GL
			// Create a background context
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_SHARE_WITH_CURRENT_CONTEXT, 1);
			Threading.WindowInfo = presentationParameters.DeviceWindowHandle;
			Threading.BackgroundContext = new Threading.GL_ContextHandle()
			{
				context = SDL.SDL_GL_CreateContext(
					presentationParameters.DeviceWindowHandle
				)
			};

			// Make the foreground context current.
			SDL.SDL_GL_MakeCurrent(presentationParameters.DeviceWindowHandle, glContext);
#endif

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
			Extensions = glGetString(GLenum.GL_EXTENSIONS);
			SupportsS3tc = (
				Extensions.Contains("GL_EXT_texture_compression_s3tc") ||
				Extensions.Contains("GL_OES_texture_compression_S3TC") ||
				Extensions.Contains("GL_EXT_texture_compression_dxt3") ||
				Extensions.Contains("GL_EXT_texture_compression_dxt5")
			);
			SupportsDxt1 = (
				SupportsS3tc ||
				Extensions.Contains("GL_EXT_texture_compression_dxt1")
			);

			// Initialize the faux-backbuffer
			Backbuffer = new FauxBackbuffer(
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
			Backbuffer.Dispose();
			Backbuffer = null;
			MojoShader.MOJOSHADER_glMakeContextCurrent(IntPtr.Zero);
			MojoShader.MOJOSHADER_glDestroyContext(shaderContext);

#if THREADED_GL
			SDL.SDL_GL_DeleteContext(Threading.BackgroundContext.context);
#endif
			SDL.SDL_GL_DeleteContext(glContext);
		}

		#endregion

		#region Window SwapBuffers Method

		public void SwapBuffers(IntPtr overrideWindowHandle)
		{
#if !DISABLE_FAUXBACKBUFFER
			int windowWidth, windowHeight;
			SDL.SDL_GetWindowSize(
				overrideWindowHandle,
				out windowWidth,
				out windowHeight
			);

			if (scissorTestEnable)
			{
				glDisable(GLenum.GL_SCISSOR_TEST);
			}

			BindReadFramebuffer(Backbuffer.Handle);
			BindDrawFramebuffer(0);

			glBlitFramebuffer(
				0, 0, Backbuffer.Width, Backbuffer.Height,
				0, 0, windowWidth, windowHeight,
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
			BindFramebuffer(Backbuffer.Handle);
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

		#region State Management Methods

		public void SetViewport(Viewport vp, bool renderTargetBound)
		{
			// Flip viewport when target is not bound
			if (!renderTargetBound)
			{
				vp.Y = Backbuffer.Height - vp.Y - vp.Height;
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

			if (	texture.texture == Textures[index] &&
				sampler.AddressU == texture.texture.WrapS &&
				sampler.AddressV == texture.texture.WrapT &&
				sampler.AddressW == texture.texture.WrapR &&
				sampler.Filter == texture.texture.Filter &&
				sampler.MaxAnisotropy == texture.texture.Anistropy &&
				sampler.MaxMipLevel == texture.texture.MaxMipmapLevel &&
				sampler.MipMapLevelOfDetailBias == texture.texture.LODBias	)
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
			if (texture.texture != Textures[index])
			{
				if (texture.texture.Target != Textures[index].Target)
				{
					// If we're changing targets, unbind the old texture first!
					glBindTexture(Textures[index].Target, 0);
				}
				glBindTexture(texture.texture.Target, texture.texture.Handle);
				Textures[index] = texture.texture;
			}

			// Apply the sampler states to the GL texture
			if (sampler.AddressU != texture.texture.WrapS)
			{
				texture.texture.WrapS = sampler.AddressU;
				glTexParameteri(
					texture.texture.Target,
					GLenum.GL_TEXTURE_WRAP_S,
					(int) XNAToGL.Wrap[texture.texture.WrapS]
				);
			}
			if (sampler.AddressV != texture.texture.WrapT)
			{
				texture.texture.WrapT = sampler.AddressV;
				glTexParameteri(
					texture.texture.Target,
					GLenum.GL_TEXTURE_WRAP_T,
					(int) XNAToGL.Wrap[texture.texture.WrapT]
				);
			}
			if (sampler.AddressW != texture.texture.WrapR)
			{
				texture.texture.WrapR = sampler.AddressW;
				glTexParameteri(
					texture.texture.Target,
					GLenum.GL_TEXTURE_WRAP_R,
					(int) XNAToGL.Wrap[texture.texture.WrapR]
				);
			}
			if (	sampler.Filter != texture.texture.Filter ||
				sampler.MaxAnisotropy != texture.texture.Anistropy	)
			{
				texture.texture.Filter = sampler.Filter;
				texture.texture.Anistropy = sampler.MaxAnisotropy;
				glTexParameteri(
					texture.texture.Target,
					GLenum.GL_TEXTURE_MAG_FILTER,
					(int) XNAToGL.MagFilter[texture.texture.Filter]
				);
				glTexParameteri(
					texture.texture.Target,
					GLenum.GL_TEXTURE_MIN_FILTER,
					(int) (
						texture.texture.HasMipmaps ?
							XNAToGL.MinMipFilter[texture.texture.Filter] :
							XNAToGL.MinFilter[texture.texture.Filter]
					)
				);
				glTexParameterf(
					texture.texture.Target,
					GLenum.GL_TEXTURE_MAX_ANISOTROPY_EXT,
					(texture.texture.Filter == TextureFilter.Anisotropic) ?
						Math.Max(texture.texture.Anistropy, 1.0f) :
						1.0f
				);
			}
			if (sampler.MaxMipLevel != texture.texture.MaxMipmapLevel)
			{
				texture.texture.MaxMipmapLevel = sampler.MaxMipLevel;
				glTexParameteri(
					texture.texture.Target,
					GLenum.GL_TEXTURE_BASE_LEVEL,
					texture.texture.MaxMipmapLevel
				);
			}
			if (sampler.MipMapLevelOfDetailBias != texture.texture.LODBias)
			{
				texture.texture.LODBias = sampler.MipMapLevelOfDetailBias;
				glTexParameterf(
					texture.texture.Target,
					GLenum.GL_TEXTURE_LOD_BIAS,
					texture.texture.LODBias
				);
			}

			if (index != 0)
			{
				// Keep this state sane. -flibit
				glActiveTexture(GLenum.GL_TEXTURE0);
			}
		}

		#endregion

		#region Effect Methods

		public OpenGLEffect CreateEffect(byte[] effectCode)
		{
			IntPtr effect = IntPtr.Zero;
			IntPtr glEffect = IntPtr.Zero;
			Threading.ForceToMainThread(() =>
			{
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
			});
			return new OpenGLEffect(effect, glEffect);
		}

		public void DeleteEffect(OpenGLEffect effect)
		{
			Threading.ForceToMainThread(() =>
			{
				if (effect.GLEffectData == currentEffect)
				{
					MojoShader.MOJOSHADER_glEffectEndPass(currentEffect);
					MojoShader.MOJOSHADER_glEffectEnd(currentEffect);
					currentEffect = IntPtr.Zero;
					currentTechnique = IntPtr.Zero;
					currentPass = 0;
				}
				MojoShader.MOJOSHADER_glDeleteEffect(effect.GLEffectData);
				MojoShader.MOJOSHADER_freeEffect(effect.EffectData);
			});
		}

		public OpenGLEffect CloneEffect(OpenGLEffect cloneSource)
		{
			IntPtr effect = IntPtr.Zero;
			IntPtr glEffect = IntPtr.Zero;
			Threading.ForceToMainThread(() =>
			{
				effect = MojoShader.MOJOSHADER_cloneEffect(cloneSource.EffectData);
				glEffect = MojoShader.MOJOSHADER_glCompileEffect(effect);
				if (glEffect == IntPtr.Zero)
				{
					throw new Exception(MojoShader.MOJOSHADER_glGetError());
				}
			});
			return new OpenGLEffect(effect, glEffect);
		}

		public void ApplyEffect(
			OpenGLEffect effect,
			IntPtr technique,
			uint pass,
			ref MojoShader.MOJOSHADER_effectStateChanges stateChanges
		) {
			flipViewport = (currentDrawFramebuffer == targetFramebuffer) ? -1 : 1;
			if (effect.GLEffectData == currentEffect)
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
				effect.GLEffectData,
				out whatever,
				0,
				ref stateChanges
			);
			MojoShader.MOJOSHADER_glEffectBeginPass(
				effect.GLEffectData,
				pass
			);
			currentEffect = effect.GLEffectData;
			currentPass = pass;
		}

		#endregion

		#region glVertexAttribPointer/glVertexAttribDivisor Methods

		public void ApplyVertexAttributes(
			VertexBufferBinding[] bindings,
			int numBindings,
			int baseVertex
		) {
			/* There's this weird case where you can have multiple vertbuffers,
			 * but they will have overlapping attributes. It seems like the
			 * first buffer gets priority, so start with the last one so the
			 * first buffer's attributes are what's bound at the end.
			 * -flibit
			 */
			for (int i = numBindings - 1; i >= 0; i -= 1)
			{
				BindVertexBuffer(bindings[i].VertexBuffer.Handle);
				VertexDeclaration vertexDeclaration = bindings[i].VertexBuffer.VertexDeclaration;
				foreach (VertexElement element in vertexDeclaration.elements)
				{
					// FIXME: Attribute state caching? -flibit
					IntPtr pointer = (IntPtr) (
						(
							vertexDeclaration.VertexStride *
							(bindings[i].VertexOffset + baseVertex)
						) + element.Offset
					);
					MojoShader.MOJOSHADER_glSetVertexAttribute(
						XNAToGL.VertexAttribUsage[element.VertexElementUsage],
						element.UsageIndex,
						XNAToGL.VertexAttribSize[element.VertexElementFormat],
						XNAToGL.VertexAttribType[element.VertexElementFormat],
						XNAToGL.VertexAttribNormalized(element),
						(uint) vertexDeclaration.VertexStride,
						pointer
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
			BindVertexBuffer(OpenGLVertexBuffer.NullBuffer);
			foreach (VertexElement element in vertexDeclaration.elements)
			{
				// FIXME: Attribute state caching? -flibit
				IntPtr pointer = (IntPtr) (
					ptr.ToInt64() + (
						vertexDeclaration.VertexStride * vertexOffset
					) + element.Offset
				);
				MojoShader.MOJOSHADER_glSetVertexAttribute(
					XNAToGL.VertexAttribUsage[element.VertexElementUsage],
					element.UsageIndex,
					XNAToGL.VertexAttribSize[element.VertexElementFormat],
					XNAToGL.VertexAttribType[element.VertexElementFormat],
					XNAToGL.VertexAttribNormalized(element),
					(uint) vertexDeclaration.VertexStride,
					pointer
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
			MojoShader.MOJOSHADER_glProgramReady();
			if (flipViewport != 0)
			{
				MojoShader.MOJOSHADER_glProgramViewportFlip(flipViewport);
				flipViewport = 0;
			}
		}

		#endregion

		#region glBindBuffer Methods

		public void BindVertexBuffer(OpenGLVertexBuffer buffer)
		{
			if (buffer.Handle != currentVertexBuffer)
			{
				glBindBuffer(GLenum.GL_ARRAY_BUFFER, buffer.Handle);
				currentVertexBuffer = buffer.Handle;
			}
		}

		public void BindIndexBuffer(OpenGLIndexBuffer buffer)
		{
			if (buffer.Handle != currentIndexBuffer)
			{
				glBindBuffer(GLenum.GL_ELEMENT_ARRAY_BUFFER, buffer.Handle);
				currentIndexBuffer = buffer.Handle;
			}
		}

		#endregion

		#region glSetBufferData Methods

		public void SetVertexBufferData<T>(
			OpenGLVertexBuffer handle,
			int bufferSize,
			int elementSizeInBytes,
			int offsetInBytes,
			T[] data,
			int startIndex,
			int elementCount,
			int vertexStride,
			SetDataOptions options
		) where T : struct {
			BindVertexBuffer(handle);

			if (options == SetDataOptions.Discard)
			{
				glBufferData(
					GLenum.GL_ARRAY_BUFFER,
					(IntPtr) bufferSize,
					IntPtr.Zero,
					handle.Dynamic
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
		}

		public void SetIndexBufferData<T>(
			OpenGLIndexBuffer handle,
			int offsetInBytes,
			T[] data,
			int startIndex,
			int elementCount,
			SetDataOptions options
		) where T : struct {
			BindIndexBuffer(handle);

			if (options == SetDataOptions.Discard)
			{
				glBufferData(
					GLenum.GL_ELEMENT_ARRAY_BUFFER,
					handle.BufferSize,
					IntPtr.Zero,
					handle.Dynamic
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
		}

		#endregion

		#region glGetBufferData Methods

		public void GetVertexBufferData<T>(
			OpenGLVertexBuffer handle,
			int offsetInBytes,
			T[] data,
			int startIndex,
			int elementCount,
			int vertexStride
		) where T : struct {
			BindVertexBuffer(handle);

			IntPtr ptr = glMapBuffer(GLenum.GL_ARRAY_BUFFER, GLenum.GL_READ_ONLY);

			// Pointer to the start of data to read in the index buffer
			ptr = new IntPtr(ptr.ToInt64() + offsetInBytes);

			if (typeof(T) == typeof(byte))
			{
				/* If data is already a byte[] we can skip the temporary buffer.
				 * Copy from the vertex buffer to the destination array.
				 */
				byte[] buffer = data as byte[];
				Marshal.Copy(ptr, buffer, 0, buffer.Length);
			}
			else
			{
				// Temporary buffer to store the copied section of data
				byte[] buffer = new byte[elementCount * vertexStride - offsetInBytes];

				// Copy from the vertex buffer to the temporary buffer
				Marshal.Copy(ptr, buffer, 0, buffer.Length);

				GCHandle dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
				IntPtr dataPtr = (IntPtr) (dataHandle.AddrOfPinnedObject().ToInt64() + startIndex * Marshal.SizeOf(typeof(T)));

				// Copy from the temporary buffer to the destination array
				int dataSize = Marshal.SizeOf(typeof(T));
				if (dataSize == vertexStride)
				{
					Marshal.Copy(buffer, 0, dataPtr, buffer.Length);
				}
				else
				{
					// If the user is asking for a specific element within the vertex buffer, copy them one by one...
					for (int i = 0; i < elementCount; i += 1)
					{
						Marshal.Copy(buffer, i * vertexStride, dataPtr, dataSize);
						dataPtr = (IntPtr)(dataPtr.ToInt64() + dataSize);
					}
				}

				dataHandle.Free();
			}

			glUnmapBuffer(GLenum.GL_ARRAY_BUFFER);
		}

		public void GetIndexBufferData<T>(
			OpenGLIndexBuffer handle,
			int offsetInBytes,
			T[] data,
			int startIndex,
			int elementCount
		) where T : struct {
			BindIndexBuffer(handle);

			IntPtr ptr = glMapBuffer(GLenum.GL_ELEMENT_ARRAY_BUFFER, GLenum.GL_READ_ONLY);

			// Pointer to the start of data to read in the index buffer
			ptr = new IntPtr(ptr.ToInt64() + offsetInBytes);

			/* If data is already a byte[] we can skip the temporary buffer.
			 * Copy from the index buffer to the destination array.
			 */
			if (typeof(T) == typeof(byte))
			{
				byte[] buffer = data as byte[];
				Marshal.Copy(ptr, buffer, 0, buffer.Length);
			}
			else
			{
				int elementSizeInBytes = Marshal.SizeOf(typeof(T));
				byte[] buffer = new byte[elementCount * elementSizeInBytes];
				Marshal.Copy(ptr, buffer, 0, buffer.Length);
				Buffer.BlockCopy(buffer, 0, data, startIndex * elementSizeInBytes, elementCount * elementSizeInBytes);
			}

			glUnmapBuffer(GLenum.GL_ELEMENT_ARRAY_BUFFER);
		}

		#endregion

		#region glDeleteBuffers Methods

		public void DeleteVertexBuffer(OpenGLVertexBuffer buffer)
		{
			if (buffer.Handle == currentVertexBuffer)
			{
				glBindBuffer(GLenum.GL_ARRAY_BUFFER, 0);
				currentVertexBuffer = 0;
			}
			uint handle = buffer.Handle;
			glDeleteBuffers(1, ref handle);
		}

		public void DeleteIndexBuffer(OpenGLIndexBuffer buffer)
		{
			if (buffer.Handle == currentIndexBuffer)
			{
				glBindBuffer(GLenum.GL_ELEMENT_ARRAY_BUFFER, 0);
				currentIndexBuffer = 0;
			}
			uint handle = buffer.Handle;
			glDeleteBuffers(1, ref handle);
		}

		#endregion

		#region glCreateTexture Method

		public OpenGLTexture CreateTexture(Type target, SurfaceFormat format, bool hasMipmaps)
		{
			uint handle;
			glGenTextures(1, out handle);
			OpenGLTexture result = new OpenGLTexture(
				handle,
				target,
				format,
				hasMipmaps
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

		#endregion

		#region glBindTexture Method

		public void BindTexture(OpenGLTexture texture)
		{
			if (texture.Target != Textures[0].Target)
			{
				glBindTexture(Textures[0].Target, 0);
			}
			if (texture != Textures[0])
			{
				glBindTexture(
					texture.Target,
					texture.Handle
				);
			}
			Textures[0] = texture;
		}

		#endregion

		#region glDeleteTexture Method

		public void DeleteTexture(OpenGLTexture texture)
		{
			for (int i = 0; i < currentAttachments.Length; i += 1)
			{
				if (texture.Handle == currentAttachments[i])
				{
					// Force an attachment update, this no longer exists!
					currentAttachments[i] = uint.MaxValue;
				}
			}
			uint handle = texture.Handle;
			glDeleteTextures(1, ref handle);
		}

		#endregion

		#region glReadPixels Method

		/// <summary>
		/// Attempts to read the texture data directly from the FBO using glReadPixels
		/// </summary>
		/// <typeparam name="T">Texture data type</typeparam>
		/// <param name="texture">The texture to read from</param>
		/// <param name="level">The texture level</param>
		/// <param name="data">The texture data array</param>
		/// <param name="rect">The portion of the image to read from</param>
		/// <returns>True if we successfully read the texture data</returns>
		public bool ReadTargetIfApplicable<T>(
			OpenGLTexture texture,
			int level,
			T[] data,
			Rectangle? rect
		) where T : struct {
			if (	currentDrawBuffers == 1 &&
				currentAttachments != null &&				
				currentAttachments[0] == texture.Handle	)
			{
				uint oldReadFramebuffer = CurrentReadFramebuffer;
				if (oldReadFramebuffer != targetFramebuffer)
				{
					BindReadFramebuffer(targetFramebuffer);
				}

				/* glReadPixels should be faster than reading
				 * back from the render target if we are already bound.
				 */
				GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
				// FIXME: Try/Catch with the GCHandle -flibit
				if (rect.HasValue)
				{
					glReadPixels(
						rect.Value.Left,
						rect.Value.Top,
						rect.Value.Width,
						rect.Value.Height,
						GLenum.GL_RGBA, // FIXME: Assumption!
						GLenum.GL_UNSIGNED_BYTE,
						handle.AddrOfPinnedObject()
					);
				}
				else
				{
					// FIXME: Using two glGet calls here! D:
					int width = 0;
					int height = 0;
					BindTexture(texture);
					glGetTexLevelParameteriv(
						texture.Target,
						level,
						GLenum.GL_TEXTURE_WIDTH,
						out width
					);
					glGetTexLevelParameteriv(
						texture.Target,
						level,
						GLenum.GL_TEXTURE_HEIGHT,
						out height
					);

					glReadPixels(
						0,
						0,
						width,
						height,
						GLenum.GL_RGBA, // FIXME: Assumption
						GLenum.GL_UNSIGNED_BYTE,
						handle.AddrOfPinnedObject()
					);
				}
				handle.Free();
				BindReadFramebuffer(oldReadFramebuffer);
				return true;
			}
			return false;
		}

		#endregion

		#region glGenerateMipmap Method

		public void GenerateTargetMipmaps(OpenGLTexture target)
		{
			OpenGLTexture prevTex = Textures[0];
			BindTexture(target);
			glGenerateMipmap(target.Target);
			BindTexture(prevTex);
		}

		#endregion

		#region Framebuffer Methods

		public void BindFramebuffer(uint handle)
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

		public void BindReadFramebuffer(uint handle)
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

		public void BindDrawFramebuffer(uint handle)
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

		public uint GenRenderbuffer(int width, int height, DepthFormat format)
		{
			uint handle;
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
			return handle;
		}

		public void DeleteRenderbuffer(uint renderbuffer)
		{
			if (renderbuffer == currentRenderbuffer)
			{
				// Force a renderbuffer update, this no longer exists!
				currentRenderbuffer = uint.MaxValue;
			}
			glDeleteRenderbuffers(1, ref renderbuffer);
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
			// Move some stuff around so the glClear works...
			if (scissorTestEnable)
			{
				glDisable(GLenum.GL_SCISSOR_TEST);
			}
			if (!zWriteEnable)
			{
				glDepthMask(true);
			}
			if (stencilWriteMask != -1)
			{
				// AKA 0xFFFFFFFF, ugh -flibit
				glStencilMask(-1);
			}

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
			if ((options & ClearOptions.DepthBuffer) == ClearOptions.DepthBuffer)
			{
				clearMask |= GLenum.GL_DEPTH_BUFFER_BIT;
				if (depth != currentClearDepth)
				{
					glClearDepth((double) depth);
					currentClearDepth = depth;
				}
			}
			if ((options & ClearOptions.Stencil) == ClearOptions.Stencil)
			{
				clearMask |= GLenum.GL_STENCIL_BUFFER_BIT;
				if (stencil != currentClearStencil)
				{
					glClearStencil(stencil);
					currentClearStencil = stencil;
				}
			}

			// CLEAR!
			glClear(clearMask);

			// Clean up after ourselves.
			if (scissorTestEnable)
			{
				glEnable(GLenum.GL_SCISSOR_TEST);
			}
			if (!zWriteEnable)
			{
				glDepthMask(false);
			}
			if (stencilWriteMask != -1) // AKA 0xFFFFFFFF, ugh -flibit
			{
				glStencilMask(stencilWriteMask);
			}
		}

		#endregion

		#region SetRenderTargets Method

		public void SetRenderTargets(
			uint[] attachments,
			GLenum[] textureTargets,
			uint renderbuffer,
			DepthFormat depthFormat
		) {
			// Bind the right framebuffer, if needed
			if (attachments == null)
			{
				BindFramebuffer(Backbuffer.Handle);
				flipViewport = 1;
				return;
			}
			else
			{
				BindFramebuffer(targetFramebuffer);
				flipViewport = -1;
			}

			// Update the color attachments, DrawBuffers state
			int i = 0;
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
			if (renderbuffer != currentRenderbuffer)
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
					renderbuffer
				);
				if (currentDepthStencilFormat == DepthFormat.Depth24Stencil8)
				{
					glFramebufferRenderbuffer(
						GLenum.GL_FRAMEBUFFER,
						GLenum.GL_STENCIL_ATTACHMENT,
						GLenum.GL_RENDERBUFFER,
						renderbuffer
					);
				}
				currentRenderbuffer = renderbuffer;
			}
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
		}

		#endregion

		#region The Faux-Backbuffer

		public class FauxBackbuffer
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

			public FauxBackbuffer(
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
				GraphicsDevice graphicsDevice,
				int width,
				int height,
				DepthFormat depthFormat
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
						if (graphicsDevice.RenderTargetCount > 0)
						{
							glDevice.BindFramebuffer(
								graphicsDevice.GLDevice.targetFramebuffer
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

					if (graphicsDevice.RenderTargetCount > 0)
					{
						glDevice.BindFramebuffer(
							graphicsDevice.GLDevice.targetFramebuffer
						);
					}

					depthStencilFormat = depthFormat;
				}

				// Keep this state sane.
				glDevice.glBindTexture(
					GLenum.GL_TEXTURE_2D,
					graphicsDevice.GLDevice.Textures[0].Handle
				);
#endif
			}
		}

		#endregion
	}
}
