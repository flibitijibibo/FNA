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
 * Note that this also affects OpenGLDevice.cs!
 * Check DISABLE_FAUXBACKBUFFER there too.
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
		#region Private OpenGL Entry Points

		public enum GLenum : int
		{
			// Hint Enum Value
			GL_DONT_CARE =				0x1100,
			// 0/1
			GL_ZERO =				0x0000,
			GL_ONE =				0x0001,
			// Types
			GL_BYTE =				0x1400,
			GL_UNSIGNED_BYTE =			0x1401,
			GL_SHORT =				0x1402,
			GL_UNSIGNED_SHORT =			0x1403,
			GL_UNSIGNED_INT =			0x1405,
			GL_FLOAT =				0x1406,
			GL_HALF_FLOAT =				0x140B,
			GL_UNSIGNED_SHORT_4_4_4_4 =		0x8033,
			GL_UNSIGNED_SHORT_5_5_5_1 =		0x8034,
			GL_UNSIGNED_INT_10_10_10_2 =		0x8036,
			GL_UNSIGNED_SHORT_5_6_5 =		0x8363,
			GL_UNSIGNED_INT_24_8 =			0x84FA,
			// Strings
			GL_VENDOR =				0x1F00,
			GL_RENDERER =				0x1F01,
			GL_VERSION =				0x1F02,
			GL_EXTENSIONS =				0x1F03,
			// Clear Mask
			GL_COLOR_BUFFER_BIT =			0x4000,
			GL_DEPTH_BUFFER_BIT =			0x0100,
			GL_STENCIL_BUFFER_BIT =			0x0400,
			// Enable Caps
			GL_SCISSOR_TEST =			0x0C11,
			GL_DEPTH_TEST =				0x0B71,
			GL_STENCIL_TEST =			0x0B90,
			// Polygons
			GL_LINE =				0x1B01,
			GL_FILL =				0x1B02,
			GL_CW =					0x0900,
			GL_CCW =				0x0901,
			GL_FRONT =				0x0404,
			GL_BACK =				0x0405,
			GL_FRONT_AND_BACK =			0x0408,
			GL_CULL_FACE =				0x0B44,
			GL_POLYGON_OFFSET_FILL =		0x8037,
			// Texture Type
			GL_TEXTURE_2D =				0x0DE1,
			GL_TEXTURE_3D =				0x806F,
			GL_TEXTURE_CUBE_MAP =			0x8513,
			GL_TEXTURE_CUBE_MAP_POSITIVE_X =	0x8515,
			// Blend Mode
			GL_BLEND =				0x0BE2,
			GL_SRC_COLOR =				0x0300,
			GL_ONE_MINUS_SRC_COLOR =		0x0301,
			GL_SRC_ALPHA =				0x0302,
			GL_ONE_MINUS_SRC_ALPHA =		0x0303,
			GL_DST_ALPHA =				0x0304,
			GL_ONE_MINUS_DST_ALPHA =		0x0305,
			GL_DST_COLOR =				0x0306,
			GL_ONE_MINUS_DST_COLOR =		0x0307,
			GL_SRC_ALPHA_SATURATE =			0x0308,
			// Equations
			GL_MIN =				0x8007,
			GL_MAX =				0x8008,
			GL_FUNC_ADD =				0x8006,
			GL_FUNC_SUBTRACT =			0x800A,
			GL_FUNC_REVERSE_SUBTRACT =		0x800B,
			// Comparisons
			GL_NEVER =				0x0200,
			GL_LESS =				0x0201,
			GL_EQUAL =				0x0202,
			GL_LEQUAL =				0x0203,
			GL_GREATER =				0x0204,
			GL_NOTEQUAL =				0x0205,
			GL_GEQUAL =				0x0206,
			GL_ALWAYS =				0x0207,
			// Stencil Operations
			GL_INVERT =				0x150A,
			GL_KEEP =				0x1E00,
			GL_REPLACE =				0x1E01,
			GL_INCR =				0x1E02,
			GL_DECR =				0x1E03,
			GL_INCR_WRAP =				0x8507,
			GL_DECR_WRAP =				0x8508,
			// Wrap Modes
			GL_REPEAT =				0x2901,
			GL_CLAMP_TO_EDGE =			0x812F,
			GL_MIRRORED_REPEAT =			0x8370,
			// Filters
			GL_NEAREST =				0x2600,
			GL_LINEAR =				0x2601,
			GL_NEAREST_MIPMAP_NEAREST =		0x2700,
			GL_NEAREST_MIPMAP_LINEAR =		0x2702,
			GL_LINEAR_MIPMAP_NEAREST =		0x2701,
			GL_LINEAR_MIPMAP_LINEAR =		0x2703,
			// Attachments
			GL_COLOR_ATTACHMENT0 =			0x8CE0,
			GL_DEPTH_ATTACHMENT =			0x8D00,
			GL_STENCIL_ATTACHMENT =			0x8D20,
			GL_DEPTH_STENCIL_ATTACHMENT =		0x821A,
			// Texture Formats
			GL_RED =				0x1903,
			GL_RGB =				0x1907,
			GL_RGBA =				0x1908,
			GL_LUMINANCE =				0x1909,
			GL_RGBA4 =				0x8056,
			GL_RGB10_A2_EXT =			0x8059,
			GL_RGBA16 =				0x805B,
			GL_BGRA =				0x80E1,
			GL_DEPTH_COMPONENT16 =			0x81A5,
			GL_DEPTH_COMPONENT24 =			0x81A6,
			GL_RG =					0x8227,
			GL_RG16 =				0x822C,
			GL_R16F =				0x822D,
			GL_R32F =				0x822E,
			GL_RG16F =				0x822F,
			GL_RG32F =				0x8230,
			GL_RG8I =				0x8237,
			GL_RGBA32F =				0x8814,
			GL_RGBA16F =				0x881A,
			GL_DEPTH24_STENCIL8 =			0x88F0,
			GL_RGBA8I =				0x8D8E,
			GL_COMPRESSED_TEXTURE_FORMATS =		0x86A3,
			GL_COMPRESSED_RGBA_S3TC_DXT1_EXT =	0x83F1,
			GL_COMPRESSED_RGBA_S3TC_DXT3_EXT =	0x83F2,
			GL_COMPRESSED_RGBA_S3TC_DXT5_EXT =	0x83F3,
			// Texture Internal Formats
			GL_DEPTH_COMPONENT =			0x1902,
			GL_DEPTH_STENCIL =			0x84F9,
			// Textures
			GL_TEXTURE_WRAP_S =			0x2802,
			GL_TEXTURE_WRAP_T =			0x2803,
			GL_TEXTURE_WRAP_R =			0x8072,
			GL_TEXTURE_MAG_FILTER =			0x2800,
			GL_TEXTURE_MIN_FILTER =			0x2801,
			GL_TEXTURE_MAX_ANISOTROPY_EXT =		0x84FE,
			GL_TEXTURE_BASE_LEVEL =			0x813C,
			GL_TEXTURE_MAX_LEVEL =			0x813D,
			GL_TEXTURE_LOD_BIAS =			0x8501,
			GL_UNPACK_ALIGNMENT =			0x0CF5,
			// Multitexture
			GL_TEXTURE0 =				0x84C0,
			GL_MAX_TEXTURE_IMAGE_UNITS =		0x8872,
			// Texture Queries
			GL_TEXTURE_WIDTH =			0x1000,
			GL_TEXTURE_HEIGHT =			0x1001,
			// Buffer objects
			GL_ARRAY_BUFFER =			0x8892,
			GL_ELEMENT_ARRAY_BUFFER =		0x8893,
			GL_STREAM_DRAW =			0x88E0,
			GL_STATIC_DRAW =			0x88E4,
			GL_READ_ONLY =				0x88B8,
			GL_MAX_VERTEX_ATTRIBS =			0x8869,
			// Render targets
			GL_FRAMEBUFFER =			0x8D40,
			GL_READ_FRAMEBUFFER =			0x8CA8,
			GL_DRAW_FRAMEBUFFER =			0x8CA9,
			GL_RENDERBUFFER =			0x8D41,
			GL_MAX_DRAW_BUFFERS =			0x8824,
			// Draw Primitives
			GL_LINES =				0x0001,
			GL_LINE_STRIP =				0x0003,
			GL_TRIANGLES =				0x0004,
			GL_TRIANGLE_STRIP =			0x0005,
			// Query Objects
			GL_QUERY_RESULT =			0x8866,
			GL_QUERY_RESULT_AVAILABLE =		0x8867,
			GL_SAMPLES_PASSED =			0x8914,
			// Source Enum Values
			GL_DEBUG_SOURCE_API_ARB =		0x8246,
			GL_DEBUG_SOURCE_WINDOW_SYSTEM_ARB =	0x8247,
			GL_DEBUG_SOURCE_SHADER_COMPILER_ARB =	0x8248,
			GL_DEBUG_SOURCE_THIRD_PARTY_ARB =	0x8249,
			GL_DEBUG_SOURCE_APPLICATION_ARB =	0x824A,
			GL_DEBUG_SOURCE_OTHER_ARB =		0x824B,
			// Type Enum Values
			GL_DEBUG_TYPE_ERROR_ARB =		0x824C,
			GL_DEBUG_TYPE_DEPRECATED_BEHAVIOR_ARB =	0x824D,
			GL_DEBUG_TYPE_UNDEFINED_BEHAVIOR_ARB =	0x824E,
			GL_DEBUG_TYPE_PORTABILITY_ARB =		0x824F,
			GL_DEBUG_TYPE_PERFORMANCE_ARB =		0x8250,
			GL_DEBUG_TYPE_OTHER_ARB =		0x8251,
			// Severity Enum Values
			GL_DEBUG_SEVERITY_HIGH_ARB =		0x9146,
			GL_DEBUG_SEVERITY_MEDIUM_ARB =		0x9147,
			GL_DEBUG_SEVERITY_LOW_ARB =		0x9148,
			// Stupid dumk stuff that's stupid
			GL_CURRENT_PROGRAM =			0x8B8D,
			GL_FRAGMENT_SHADER =			0x8B30,
			GL_VERTEX_SHADER =			0x8B31,
			GL_COMPILE_STATUS =			0x8B81,
			GL_LINK_STATUS =			0x8B82
		}

		// Entry Points

		/* BEGIN GET FUNCTIONS */

		private delegate IntPtr GetString(GLenum pname);
		private GetString INTERNAL_glGetString;
		private string glGetString(GLenum pname)
		{
			unsafe
			{
				return new string((sbyte*) INTERNAL_glGetString(pname));
			}
		}

		public delegate void GetIntegerv(GLenum pname, out int param);
		public GetIntegerv glGetIntegerv;

		/* END GET FUNCTIONS */

		/* BEGIN ENABLE/DISABLE FUNCTIONS */

		public delegate void Enable(GLenum cap);
		public Enable glEnable;

		public delegate void Disable(GLenum cap);
		public Disable glDisable;

		/* END ENABLE/DISABLE FUNCTIONS */

		/* BEGIN VIEWPORT/SCISSOR FUNCTIONS */

		public delegate void G_Viewport(
			int x,
			int y,
			int width,
			int height
		);
		public G_Viewport glViewport;

		private delegate void DepthRange(
			double near_val,
			double far_val
		);
		private DepthRange glDepthRange;

		private delegate void Scissor(
			int x,
			int y,
			int width,
			int height
		);
		private Scissor glScissor;

		/* END VIEWPORT/SCISSOR FUNCTIONS */

		/* BEGIN BLEND STATE FUNCTIONS */

		private delegate void BlendColor(
			float red,
			float green,
			float blue,
			float alpha
		);
		private BlendColor glBlendColor;

		private delegate void BlendFuncSeparate(
			GLenum srcRGB,
			GLenum dstRGB,
			GLenum srcAlpha,
			GLenum dstAlpha
		);
		private BlendFuncSeparate glBlendFuncSeparate;

		private delegate void BlendEquationSeparate(
			GLenum modeRGB,
			GLenum modeAlpha
		);
		private BlendEquationSeparate glBlendEquationSeparate;

		private delegate void ColorMask(
			bool red,
			bool green,
			bool blue,
			bool alpha
		);
		private ColorMask glColorMask;

		/* END BLEND STATE FUNCTIONS */

		/* BEGIN DEPTH/STENCIL STATE FUNCTIONS */

		private delegate void DepthMask(bool flag);
		private DepthMask glDepthMask;

		private delegate void DepthFunc(GLenum func);
		private DepthFunc glDepthFunc;

		private delegate void StencilMask(int mask);
		private StencilMask glStencilMask;

		private delegate void StencilFuncSeparate(
			GLenum face,
			GLenum func,
			int reference,
			int mask
		);
		private StencilFuncSeparate glStencilFuncSeparate;

		private delegate void StencilOpSeparate(
			GLenum face,
			GLenum sfail,
			GLenum dpfail,
			GLenum dppass
		);
		private StencilOpSeparate glStencilOpSeparate;

		private delegate void StencilFunc(
			GLenum fail,
			int reference,
			int mask
		);
		private StencilFunc glStencilFunc;

		private delegate void StencilOp(
			GLenum fail,
			GLenum zfail,
			GLenum zpass
		);
		private StencilOp glStencilOp;

		/* END DEPTH/STENCIL STATE FUNCTIONS */

		/* BEGIN RASTERIZER STATE FUNCTIONS */

		private delegate void CullFace(GLenum mode);
		private CullFace glCullFace;

		private delegate void FrontFace(GLenum mode);
		private FrontFace glFrontFace;

		private delegate void PolygonMode(GLenum face, GLenum mode);
		private PolygonMode glPolygonMode;

		private delegate void PolygonOffset(float factor, float units);
		private PolygonOffset glPolygonOffset;

		/* END RASTERIZER STATE FUNCTIONS */

		/* BEGIN TEXTURE FUNCTIONS */

		public delegate void GenTextures(int n, out uint textures);
		public GenTextures glGenTextures;

		public delegate void DeleteTextures(
			int n,
			ref uint textures
		);
		public DeleteTextures glDeleteTextures;

		public delegate void G_BindTexture(GLenum target, uint texture);
		public G_BindTexture glBindTexture;

		public delegate void TexImage2D(
			GLenum target,
			int level,
			int internalFormat,
			int width,
			int height,
			int border,
			GLenum format,
			GLenum type,
			IntPtr pixels
		);
		public TexImage2D glTexImage2D;

		public delegate void TexSubImage2D(
			GLenum target,
			int level,
			int xoffset,
			int yoffset,
			int width,
			int height,
			GLenum format,
			GLenum type,
			IntPtr pixels
		);
		public TexSubImage2D glTexSubImage2D;

		public delegate void CompressedTexImage2D(
			GLenum target,
			int level,
			int internalFormat,
			int width,
			int height,
			int border,
			int imageSize,
			IntPtr pixels
		);
		public CompressedTexImage2D glCompressedTexImage2D;

		public delegate void CompressedTexSubImage2D(
			GLenum target,
			int level,
			int xoffset,
			int yoffset,
			int width,
			int height,
			GLenum format,
			int imageSize,
			IntPtr pixels
		);
		public CompressedTexSubImage2D glCompressedTexSubImage2D;

		public delegate void TexImage3D(
			GLenum target,
			int level,
			int internalFormat,
			int width,
			int height,
			int depth,
			int border,
			GLenum format,
			GLenum type,
			IntPtr pixels
		);
		public TexImage3D glTexImage3D;

		public delegate void TexSubImage3D(
			GLenum target,
			int level,
			int xoffset,
			int yoffset,
			int zoffset,
			int width,
			int height,
			int depth,
			GLenum format,
			GLenum type,
			IntPtr pixels
		);
		public TexSubImage3D glTexSubImage3D;

		public delegate void GetTexImage(
			GLenum target,
			int level,
			GLenum format,
			GLenum type,
			IntPtr pixels
		);
		public GetTexImage glGetTexImage;

		public delegate void TexParameteri(
			GLenum target,
			GLenum pname,
			int param
		);
		public TexParameteri glTexParameteri;

		private delegate void TexParameterf(
			GLenum target,
			GLenum pname,
			float param
		);
		private TexParameterf glTexParameterf;

		public delegate void ActiveTexture(GLenum texture);
		public ActiveTexture glActiveTexture;

		private delegate void GetTexLevelParameteriv(
			GLenum target,
			int level,
			GLenum pname,
			out int param
		);
		private GetTexLevelParameteriv glGetTexLevelParameteriv;

		public delegate void PixelStorei(GLenum pname, int param);
		public PixelStorei glPixelStorei;

		/* END TEXTURE FUNCTIONS */

		/* BEGIN BUFFER FUNCTIONS */

		public delegate void GenBuffers(int n, out uint buffers);
		public GenBuffers glGenBuffers;

		private delegate void DeleteBuffers(
			int n,
			ref uint buffers
		);
		private DeleteBuffers glDeleteBuffers;

		private delegate void BindBuffer(GLenum target, uint buffer);
		private BindBuffer glBindBuffer;

		public delegate void BufferData(
			GLenum target,
			IntPtr size,
			IntPtr data,
			GLenum usage
		);
		public BufferData glBufferData;

		private delegate void BufferSubData(
			GLenum target,
			IntPtr offset,
			IntPtr size,
			IntPtr data
		);
		private BufferSubData glBufferSubData;

		private delegate IntPtr MapBuffer(GLenum target, GLenum access);
		private MapBuffer glMapBuffer;

		private delegate void UnmapBuffer(GLenum target);
		private UnmapBuffer glUnmapBuffer;

		/* END BUFFER FUNCTIONS */

		/* BEGIN VERTEX ATTRIBUTE FUNCTIONS */

		private delegate void EnableVertexAttribArray(int index);
		private EnableVertexAttribArray glEnableVertexAttribArray;

		private delegate void DisableVertexAttribArray(int index);
		private DisableVertexAttribArray glDisableVertexAttribArray;

		private delegate void VertexAttribDivisor(
			int index,
			int divisor
		);
		private VertexAttribDivisor glVertexAttribDivisor;

		public delegate void G_VertexAttribPointer(
			int index,
			int size,
			GLenum type,
			bool normalized,
			int stride,
			IntPtr pointer
		);
		public G_VertexAttribPointer glVertexAttribPointer;

		/* END VERTEX ATTRIBUTE FUNCTIONS */

		/* BEGIN CLEAR FUNCTIONS */

		private delegate void ClearColor(
			float red,
			float green,
			float blue,
			float alpha
		);
		private ClearColor glClearColor;

		private delegate void ClearDepth(double depth);
		private ClearDepth glClearDepth;

		private delegate void ClearStencil(int s);
		private ClearStencil glClearStencil;

		private delegate void G_Clear(GLenum mask);
		private G_Clear glClear;

		/* END CLEAR FUNCTIONS */

		/* BEGIN FRAMEBUFFER FUNCTIONS */

		private delegate void DrawBuffers(int n, GLenum[] bufs);
		private DrawBuffers glDrawBuffers;

		public delegate void ReadPixels(
			int x,
			int y,
			int width,
			int height,
			GLenum format,
			GLenum type,
			IntPtr pixels
		);
		public ReadPixels glReadPixels;

		public delegate void GenFramebuffers(
			int n,
			out uint framebuffers
		);
		public GenFramebuffers glGenFramebuffers;

		public delegate void DeleteFramebuffers(
			int n,
			ref uint framebuffers
		);
		public DeleteFramebuffers glDeleteFramebuffers;

		public delegate void G_BindFramebuffer(
			GLenum target,
			uint framebuffer
		);
		public G_BindFramebuffer glBindFramebuffer;

		public delegate void FramebufferTexture2D(
			GLenum target,
			GLenum attachment,
			GLenum textarget,
			uint texture,
			int level
		);
		public FramebufferTexture2D glFramebufferTexture2D;

		public delegate void FramebufferRenderbuffer(
			GLenum target,
			GLenum attachment,
			GLenum renderbuffertarget,
			uint renderbuffer
		);
		public FramebufferRenderbuffer glFramebufferRenderbuffer;

#if !DISABLE_FAUXBACKBUFFER
		public delegate void BlitFramebuffer(
			int srcX0,
			int srcY0,
			int srcX1,
			int srcY1,
			int dstX0,
			int dstY0,
			int dstX1,
			int dstY1,
			GLenum mask,
			GLenum filter
		);
		public BlitFramebuffer glBlitFramebuffer;
#endif

		public delegate void GenRenderbuffers(
			int n,
			out uint renderbuffers
		);
		public GenRenderbuffers glGenRenderbuffers;

		public delegate void DeleteRenderbuffers(
			int n,
			ref uint renderbuffers
		);
		public DeleteRenderbuffers glDeleteRenderbuffers;

		public delegate void BindRenderbuffer(
			GLenum target,
			uint renderbuffer
		);
		public BindRenderbuffer glBindRenderbuffer;

		public delegate void RenderbufferStorage(
			GLenum target,
			GLenum internalformat,
			int width,
			int height
		);
		public RenderbufferStorage glRenderbufferStorage;

		/* END FRAMEBUFFER FUNCTIONS */

		/* BEGIN DRAWING FUNCTIONS */

		public delegate void DrawElementsInstanced(
			GLenum mode,
			int count,
			GLenum type,
			IntPtr indices,
			int instanceCount
		);
		public DrawElementsInstanced glDrawElementsInstanced;

		public delegate void DrawRangeElements(
			GLenum mode,
			int start,
			int end,
			int count,
			GLenum type,
			IntPtr indices
		);
		public DrawRangeElements glDrawRangeElements;

		public delegate void DrawArrays(
			GLenum mode,
			int first,
			int count
		);
		public DrawArrays glDrawArrays;

		/* END DRAWING FUNCTIONS */

		/* BEGIN QUERY FUNCTIONS */

		public delegate void GenQueries(int n, out uint ids);
		public GenQueries glGenQueries;

		public delegate void DeleteQueries(int n, ref uint ids);
		public DeleteQueries glDeleteQueries;

		public delegate void BeginQuery(GLenum target, uint id);
		public BeginQuery glBeginQuery;

		public delegate void EndQuery(GLenum target);
		public EndQuery glEndQuery;

		public delegate void GetQueryObjectiv(
			uint id,
			GLenum pname,
			out int param
		);
		public GetQueryObjectiv glGetQueryObjectiv;

		/* END QUERY FUNCTIONS */

		/* BEGIN SHADER FUNCTIONS */

		public delegate uint CreateShader(GLenum type);
		public CreateShader glCreateShader;

		public delegate void DeleteShader(uint shader);
		public DeleteShader glDeleteShader;

		public delegate void ShaderSource(
			uint shader,
			int count,
			ref string source,
			ref int length
		);
		public ShaderSource glShaderSource;

		public delegate void CompileShader(uint shader);
		public CompileShader glCompileShader;

		public delegate uint CreateProgram();
		public CreateProgram glCreateProgram;

		public delegate void DeleteProgram(uint program);
		public DeleteProgram glDeleteProgram;

		public delegate void AttachShader(uint program, uint shader);
		public AttachShader glAttachShader;

		public delegate void DetachShader(uint program, uint shader);
		public DetachShader glDetachShader;

		public delegate void LinkProgram(uint program);
		public LinkProgram glLinkProgram;

		public delegate void UseProgram(uint program);
		public UseProgram glUseProgram;

		public delegate void Uniform1i(int location, int v0);
		public Uniform1i glUniform1i;

		public delegate void Uniform4fv(
			int location,
			int count,
			IntPtr value
		);
		public Uniform4fv glUniform4fv;

		public delegate void GetShaderiv(
			uint shader,
			GLenum pname,
			out int param
		);
		public GetShaderiv glGetShaderiv;

		public delegate void GetProgramiv(
			uint program,
			GLenum pname,
			out int param
		);
		public GetProgramiv glGetProgramiv;

		public delegate int GetUniformLocation(
			uint program,
			string name
		);
		public GetUniformLocation glGetUniformLocation;

		public delegate int GetAttribLocation(
			uint program,
			string name
		);
		public GetAttribLocation glGetAttribLocation;

		public delegate void BindAttribLocation(
			uint program,
			uint index,
			string name
		);
		public BindAttribLocation glBindAttribLocation;

		public delegate bool IsShader(uint shader);
		public IsShader glIsShader;

		public delegate bool IsProgram(uint program);
		public IsProgram glIsProgram;

		public delegate string GetShaderInfoLog(uint shader);
		public GetShaderInfoLog glGetShaderInfoLog;

		public delegate string GetProgramInfoLog(uint program);
		public GetProgramInfoLog glGetProgramInfoLog;

		/* END SHADER FUNCTIONS */

		/* BEGIN STUPID THREADED GL FUNCTIONS */

		public delegate void Flush();
		public Flush glFlush;

		/* END STUPID THREADED GL FUNCTIONS */

#if DEBUG
		/* BEGIN DEBUG OUTPUT FUNCTIONS */

		private delegate void DebugMessageCallback(
			DebugProc callback,
			IntPtr userParam
		);
		private DebugMessageCallback glDebugMessageCallbackARB;

		private delegate void DebugMessageControl(
			GLenum source,
			GLenum type,
			GLenum severity,
			int count,
			IntPtr ids, // const GLuint*
			bool enabled
		);
		private DebugMessageControl glDebugMessageControlARB;

		// ARB_debug_output callback
		private delegate void DebugProc(
			GLenum source,
			GLenum type,
			uint id,
			GLenum severity,
			int length,
			IntPtr message, // const GLchar*
			IntPtr userParam // const GLvoid*
		);
		private DebugProc DebugCall = DebugCallback;
		private static void DebugCallback(
			GLenum source,
			GLenum type,
			uint id,
			GLenum severity,
			int length,
			IntPtr message, // const GLchar*
			IntPtr userParam // const GLvoid*
		) {
			System.Console.WriteLine(
				"{0}\n\tSource: {1}\n\tType: {2}\n\tSeverity: {3}",
				Marshal.PtrToStringAnsi(message),
				source.ToString(),
				type.ToString(),
				severity.ToString()
			);
			if (type == GLenum.GL_DEBUG_TYPE_ERROR_ARB)
			{
				throw new Exception("ARB_debug_output found an error.");
			}
		}

		/* END DEBUG OUTPUT FUNCTIONS */

		/* BEGIN STRING MARKER FUNCTIONS */

		private delegate void StringMarkerGREMEDY(int length, byte[] chars);
		private StringMarkerGREMEDY glStringMarkerGREMEDY;

		/* END STRING MARKER FUNCTIONS */
#endif

		public void LoadGLEntryPoints()
		{
			/* Basic entry points. If you don't have these, you're screwed. */
			try
			{
				INTERNAL_glGetString = (GetString) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glGetString"),
					typeof(GetString)
				);
				glGetIntegerv = (GetIntegerv) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glGetIntegerv"),
					typeof(GetIntegerv)
				);
				glEnable = (Enable) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glEnable"),
					typeof(Enable)
				);
				glDisable = (Disable) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glDisable"),
					typeof(Disable)
				);
				glViewport = (G_Viewport) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glViewport"),
					typeof(G_Viewport)
				);
				glDepthRange = (DepthRange) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glDepthRange"),
					typeof(DepthRange)
				);
				glScissor = (Scissor) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glScissor"),
					typeof(Scissor)
				);
				glBlendColor = (BlendColor) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glBlendColor"),
					typeof(BlendColor)
				);
				glBlendFuncSeparate = (BlendFuncSeparate) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glBlendFuncSeparate"),
					typeof(BlendFuncSeparate)
				);
				glBlendEquationSeparate = (BlendEquationSeparate) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glBlendEquationSeparate"),
					typeof(BlendEquationSeparate)
				);
				glColorMask = (ColorMask) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glColorMask"),
					typeof(ColorMask)
				);
				glDepthMask = (DepthMask) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glDepthMask"),
					typeof(DepthMask)
				);
				glDepthFunc = (DepthFunc) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glDepthFunc"),
					typeof(DepthFunc)
				);
				glStencilMask = (StencilMask) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glStencilMask"),
					typeof(StencilMask)
				);
				glStencilFuncSeparate = (StencilFuncSeparate) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glStencilFuncSeparate"),
					typeof(StencilFuncSeparate)
				);
				glStencilOpSeparate = (StencilOpSeparate) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glStencilOpSeparate"),
					typeof(StencilOpSeparate)
				);
				glStencilFunc = (StencilFunc) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glStencilFunc"),
					typeof(StencilFunc)
				);
				glStencilOp = (StencilOp) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glStencilOp"),
					typeof(StencilOp)
				);
				glCullFace = (CullFace) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glCullFace"),
					typeof(CullFace)
				);
				glFrontFace = (FrontFace) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glFrontFace"),
					typeof(FrontFace)
				);
				glPolygonMode = (PolygonMode) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glPolygonMode"),
					typeof(PolygonMode)
				);
				glPolygonOffset = (PolygonOffset) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glPolygonOffset"),
					typeof(PolygonOffset)
				);
				glGenTextures = (GenTextures) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glGenTextures"),
					typeof(GenTextures)
				);
				glDeleteTextures = (DeleteTextures) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glDeleteTextures"),
					typeof(DeleteTextures)
				);
				glBindTexture = (G_BindTexture) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glBindTexture"),
					typeof(G_BindTexture)
				);
				glTexImage2D = (TexImage2D) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glTexImage2D"),
					typeof(TexImage2D)
				);
				glTexSubImage2D = (TexSubImage2D) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glTexSubImage2D"),
					typeof(TexSubImage2D)
				);
				glCompressedTexImage2D = (CompressedTexImage2D) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glCompressedTexImage2D"),
					typeof(CompressedTexImage2D)
				);
				glCompressedTexSubImage2D = (CompressedTexSubImage2D) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glCompressedTexSubImage2D"),
					typeof(CompressedTexSubImage2D)
				);
				glTexImage3D = (TexImage3D) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glTexImage3D"),
					typeof(TexImage3D)
				);
				glTexSubImage3D = (TexSubImage3D) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glTexSubImage3D"),
					typeof(TexSubImage3D)
				);
				glGetTexImage = (GetTexImage) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glGetTexImage"),
					typeof(GetTexImage)
				);
				glTexParameteri = (TexParameteri) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glTexParameteri"),
					typeof(TexParameteri)
				);
				glTexParameterf = (TexParameterf) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glTexParameterf"),
					typeof(TexParameterf)
				);
				glActiveTexture = (ActiveTexture) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glActiveTexture"),
					typeof(ActiveTexture)
				);
				glGetTexLevelParameteriv = (GetTexLevelParameteriv) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glGetTexLevelParameteriv"),
					typeof(GetTexLevelParameteriv)
				);
				glPixelStorei = (PixelStorei) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glPixelStorei"),
					typeof(PixelStorei)
				);
				glGenBuffers = (GenBuffers) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glGenBuffers"),
					typeof(GenBuffers)
				);
				glDeleteBuffers = (DeleteBuffers) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glDeleteBuffers"),
					typeof(DeleteBuffers)
				);
				glBindBuffer = (BindBuffer) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glBindBuffer"),
					typeof(BindBuffer)
				);
				glBufferData = (BufferData) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glBufferData"),
					typeof(BufferData)
				);
				glBufferSubData = (BufferSubData) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glBufferSubData"),
					typeof(BufferSubData)
				);
				glMapBuffer = (MapBuffer) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glMapBuffer"),
					typeof(MapBuffer)
				);
				glUnmapBuffer = (UnmapBuffer) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glUnmapBuffer"),
					typeof(UnmapBuffer)
				);
				glEnableVertexAttribArray = (EnableVertexAttribArray) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glEnableVertexAttribArray"),
					typeof(EnableVertexAttribArray)
				);
				glDisableVertexAttribArray = (DisableVertexAttribArray) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glDisableVertexAttribArray"),
					typeof(DisableVertexAttribArray)
				);
				glVertexAttribPointer = (G_VertexAttribPointer) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glVertexAttribPointer"),
					typeof(G_VertexAttribPointer)
				);
				glClearColor = (ClearColor) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glClearColor"),
					typeof(ClearColor)
				);
				glClearDepth = (ClearDepth) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glClearDepth"),
					typeof(ClearDepth)
				);
				glClearStencil = (ClearStencil) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glClearStencil"),
					typeof(ClearStencil)
				);
				glClear = (G_Clear) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glClear"),
					typeof(G_Clear)
				);
				glDrawBuffers = (DrawBuffers) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glDrawBuffers"),
					typeof(DrawBuffers)
				);
				glReadPixels = (ReadPixels) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glReadPixels"),
					typeof(ReadPixels)
				);
				glDrawRangeElements = (DrawRangeElements) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glDrawRangeElements"),
					typeof(DrawRangeElements)
				);
				glDrawArrays = (DrawArrays) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glDrawArrays"),
					typeof(DrawArrays)
				);
				glGenQueries = (GenQueries) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glGenQueries"),
					typeof(GenQueries)
				);
				glDeleteQueries = (DeleteQueries) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glDeleteQueries"),
					typeof(DeleteQueries)
				);
				glBeginQuery = (BeginQuery) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glBeginQuery"),
					typeof(BeginQuery)
				);
				glEndQuery = (EndQuery) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glEndQuery"),
					typeof(EndQuery)
				);
				glGetQueryObjectiv = (GetQueryObjectiv) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glGetQueryObjectiv"),
					typeof(GetQueryObjectiv)
				);
				glCreateShader = (CreateShader) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glCreateShader"),
					typeof(CreateShader)
				);
				glDeleteShader = (DeleteShader) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glDeleteShader"),
					typeof(DeleteShader)
				);
				glShaderSource = (ShaderSource) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glShaderSource"),
					typeof(ShaderSource)
				);
				glCompileShader = (CompileShader) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glCompileShader"),
					typeof(CompileShader)
				);
				glCreateProgram = (CreateProgram) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glCreateProgram"),
					typeof(CreateProgram)
				);
				glDeleteProgram = (DeleteProgram) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glDeleteProgram"),
					typeof(DeleteProgram)
				);
				glAttachShader = (AttachShader) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glAttachShader"),
					typeof(AttachShader)
				);
				glDetachShader = (DetachShader) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glDetachShader"),
					typeof(DetachShader)
				);
				glLinkProgram = (LinkProgram) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glLinkProgram"),
					typeof(LinkProgram)
				);
				glUseProgram = (UseProgram) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glUseProgram"),
					typeof(UseProgram)
				);
				glUniform1i = (Uniform1i) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glUniform1i"),
					typeof(Uniform1i)
				);
				glUniform4fv = (Uniform4fv) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glUniform4fv"),
					typeof(Uniform4fv)
				);
				glGetShaderiv = (GetShaderiv) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glGetShaderiv"),
					typeof(GetShaderiv)
				);
				glGetProgramiv = (GetProgramiv) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glGetProgramiv"),
					typeof(GetProgramiv)
				);
				glGetUniformLocation = (GetUniformLocation) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glGetUniformLocation"),
					typeof(GetUniformLocation)
				);
				glGetAttribLocation = (GetAttribLocation) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glGetAttribLocation"),
					typeof(GetAttribLocation)
				);
				glBindAttribLocation = (BindAttribLocation) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glBindAttribLocation"),
					typeof(BindAttribLocation)
				);
				glIsShader = (IsShader) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glIsShader"),
					typeof(IsShader)
				);
				glIsProgram = (IsProgram) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glIsProgram"),
					typeof(IsProgram)
				);
				glGetShaderInfoLog = (GetShaderInfoLog) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glGetShaderInfoLog"),
					typeof(GetShaderInfoLog)
				);
				glGetProgramInfoLog = (GetProgramInfoLog) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glGetProgramInfoLog"),
					typeof(GetProgramInfoLog)
				);
				glFlush = (Flush) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glFlush"),
					typeof(Flush)
				);
			}
			catch
			{
				throw new NoSuitableGraphicsDeviceException("OpenGL 2.1 support is required!");
			}

			/* ARB_framebuffer_object. We're flexible, but not _that_ flexible. */
			try
			{
				glGenFramebuffers = (GenFramebuffers) Marshal.GetDelegateForFunctionPointer(
					TryGetFramebufferEP("glGenFramebuffers"),
					typeof(GenFramebuffers)
				);
				glDeleteFramebuffers = (DeleteFramebuffers) Marshal.GetDelegateForFunctionPointer(
					TryGetFramebufferEP("glDeleteFramebuffers"),
					typeof(DeleteFramebuffers)
				);
				glBindFramebuffer = (G_BindFramebuffer) Marshal.GetDelegateForFunctionPointer(
					TryGetFramebufferEP("glBindFramebuffer"),
					typeof(G_BindFramebuffer)
				);
				glFramebufferTexture2D = (FramebufferTexture2D) Marshal.GetDelegateForFunctionPointer(
					TryGetFramebufferEP("glFramebufferTexture2D"),
					typeof(FramebufferTexture2D)
				);
				glFramebufferRenderbuffer = (FramebufferRenderbuffer) Marshal.GetDelegateForFunctionPointer(
					TryGetFramebufferEP("glFramebufferRenderbuffer"),
					typeof(FramebufferRenderbuffer)
				);
#if !DISABLE_FAUXBACKBUFFER
				glBlitFramebuffer = (BlitFramebuffer) Marshal.GetDelegateForFunctionPointer(
					TryGetFramebufferEP("glBlitFramebuffer"),
					typeof(BlitFramebuffer)
				);
#endif
				glGenRenderbuffers = (GenRenderbuffers) Marshal.GetDelegateForFunctionPointer(
					TryGetFramebufferEP("glGenRenderbuffers"),
					typeof(GenRenderbuffers)
				);
				glDeleteRenderbuffers = (DeleteRenderbuffers) Marshal.GetDelegateForFunctionPointer(
					TryGetFramebufferEP("glDeleteRenderbuffers"),
					typeof(DeleteRenderbuffers)
				);
				glBindRenderbuffer = (BindRenderbuffer) Marshal.GetDelegateForFunctionPointer(
					TryGetFramebufferEP("glBindRenderbuffer"),
					typeof(BindRenderbuffer)
				);
				glRenderbufferStorage = (RenderbufferStorage) Marshal.GetDelegateForFunctionPointer(
					TryGetFramebufferEP("glRenderbufferStorage"),
					typeof(RenderbufferStorage)
				);
			}
			catch
			{
				throw new NoSuitableGraphicsDeviceException("OpenGL framebuffer support is required!");
			}

			/* ARB_instanced_arrays/ARB_draw_instanced are almost optional. */
			SupportsHardwareInstancing = true;
			try
			{
				glVertexAttribDivisor = (VertexAttribDivisor) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glVertexAttribDivisor"),
					typeof(VertexAttribDivisor)
				);
				glDrawElementsInstanced = (DrawElementsInstanced) Marshal.GetDelegateForFunctionPointer(
					SDL.SDL_GL_GetProcAddress("glDrawElementsInstanced"),
					typeof(DrawElementsInstanced)
				);
			}
			catch
			{
				SupportsHardwareInstancing = false;
			}

#if DEBUG
			/* ARB_debug_output, for debug contexts */
			IntPtr messageCallback = SDL.SDL_GL_GetProcAddress("glDebugMessageCallbackARB");
			IntPtr messageControl = SDL.SDL_GL_GetProcAddress("glDebugMessageControlARB");
			if (messageCallback == IntPtr.Zero || messageControl == IntPtr.Zero)
			{
				System.Console.WriteLine("ARB_debug_output not supported!");
			}
			else
			{
				glDebugMessageCallbackARB = (DebugMessageCallback) Marshal.GetDelegateForFunctionPointer(
					messageCallback,
					typeof(DebugMessageCallback)
				);
				glDebugMessageControlARB = (DebugMessageControl) Marshal.GetDelegateForFunctionPointer(
					messageControl,
					typeof(DebugMessageControl)
				);
				glDebugMessageCallbackARB(DebugCall, IntPtr.Zero);
				glDebugMessageControlARB(
					GLenum.GL_DONT_CARE,
					GLenum.GL_DONT_CARE,
					GLenum.GL_DONT_CARE,
					0,
					IntPtr.Zero,
					true
				);
				glDebugMessageControlARB(
					GLenum.GL_DONT_CARE,
					GLenum.GL_DEBUG_TYPE_OTHER_ARB,
					GLenum.GL_DEBUG_SEVERITY_LOW_ARB,
					0,
					IntPtr.Zero,
					false
				);
			}

			/* GREMEDY_string_marker, for apitrace */
			IntPtr stringMarkerCallback = SDL.SDL_GL_GetProcAddress("glStringMarkerGREMEDY");
			if (stringMarkerCallback == IntPtr.Zero)
			{
				System.Console.WriteLine("GREMEDY_string_marker not supported!");
			}
			else
			{
				glStringMarkerGREMEDY = (StringMarkerGREMEDY) Marshal.GetDelegateForFunctionPointer(
					stringMarkerCallback,
					typeof(StringMarkerGREMEDY)
				);
			}
#endif
		}

		private IntPtr TryGetFramebufferEP(string ep)
		{
			IntPtr result;
			result = SDL.SDL_GL_GetProcAddress(ep);
			if (result == IntPtr.Zero)
			{
				result = SDL.SDL_GL_GetProcAddress(ep + "EXT");
				if (result == IntPtr.Zero)
				{
					throw new NoSuitableGraphicsDeviceException();
				}
			}
			return result;
		}

		#endregion
	}
}
