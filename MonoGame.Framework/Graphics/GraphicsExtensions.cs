using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

#if OPENGL
#if SDL2
using OpenTK.Graphics.OpenGL;
#elif GLES
using OpenTK.Graphics.ES20;
using BlendEquationMode = OpenTK.Graphics.ES20.All;
using BlendingFactorSrc = OpenTK.Graphics.ES20.All;
using BlendingFactorDest = OpenTK.Graphics.ES20.All;
using VertexAttribPointerType = OpenTK.Graphics.ES20.All;
using PixelInternalFormat = OpenTK.Graphics.ES20.All;
using PixelType = OpenTK.Graphics.ES20.All;
using PixelFormat = OpenTK.Graphics.ES20.All;
using VertexPointerType = OpenTK.Graphics.ES20.All;
using ColorPointerType = OpenTK.Graphics.ES20.All;
using NormalPointerType = OpenTK.Graphics.ES20.All;
using TexCoordPointerType = OpenTK.Graphics.ES20.All;
using GetPName = OpenTK.Graphics.ES20.All;
using System.Diagnostics;
#endif
#endif

namespace Microsoft.Xna.Framework.Graphics
{
    public static class GraphicsExtensions
    {
#if OPENGL
        public static bool UseArbFramebuffer;
        public static bool UseDxtCompression;
        public static bool FboMultisampleSupported;

        public static bool IsRenderbuffer(uint renderbuffer)
        {
            return UseArbFramebuffer ? GL.IsRenderbuffer(renderbuffer) : GL.Ext.IsRenderbuffer(renderbuffer);
        }
        public static void BindRenderbuffer(RenderbufferTarget target, uint renderbuffer)
        {
            if (UseArbFramebuffer)  GL.BindRenderbuffer(target, renderbuffer);
            else                    GL.Ext.BindRenderbuffer(target, renderbuffer);
        }
        public static void DeleteRenderbuffers(int n, ref uint renderbuffers)
        {
            if (UseArbFramebuffer)  GL.DeleteRenderbuffers(n, ref renderbuffers);
            else                    GL.Ext.DeleteRenderbuffers(n, ref renderbuffers);
        }
        public static void GenRenderbuffers(int n, out uint renderbuffers)
        {
            if (UseArbFramebuffer)  GL.GenRenderbuffers(n, out renderbuffers);
            else                    GL.Ext.GenRenderbuffers(n, out renderbuffers);
        }
        public static void RenderbufferStorage(RenderbufferTarget target, RenderbufferStorage internalformat, int width, int height)
        {
            if (UseArbFramebuffer)  GL.RenderbufferStorage(target, internalformat, width, height);
            else                    GL.Ext.RenderbufferStorage(target, internalformat, width, height);
        }
        public static void RenderbufferStorageMultisample(RenderbufferTarget target, int samples, RenderbufferStorage internalformat, int width, int height)
        {
            if (UseArbFramebuffer)  GL.RenderbufferStorageMultisample(target, samples, internalformat, width, height);
            else                    GL.Ext.RenderbufferStorageMultisample((ExtFramebufferMultisample)target, samples, (ExtFramebufferMultisample)internalformat, width, height);
        }
        public static void GetRenderbufferParameter(RenderbufferTarget target, RenderbufferParameterName pname, RenderbufferStorage internalformat, out int @params)
        {
            if (UseArbFramebuffer)  GL.GetRenderbufferParameter(target, pname, out @params);
            else                    GL.Ext.GetRenderbufferParameter(target, pname, out @params);
        }
        public static bool IsFramebuffer(uint framebuffer)
        {
            return UseArbFramebuffer ? GL.IsFramebuffer(framebuffer) : GL.Ext.IsFramebuffer(framebuffer);
        }
        public static void BindFramebuffer(FramebufferTarget target, uint framebuffer)
        {
            if (UseArbFramebuffer)  GL.BindFramebuffer(target, framebuffer);
            else                    GL.Ext.BindFramebuffer(target, framebuffer);
        }
        public static void BindFramebuffer(FramebufferTarget target, int framebuffer)
        {
            if (UseArbFramebuffer)  GL.BindFramebuffer(target, framebuffer);
            else                    GL.Ext.BindFramebuffer(target, framebuffer);
        }
        public static void DeleteFramebuffers(int n, ref uint framebuffers)
        {
            if (UseArbFramebuffer)  GL.DeleteFramebuffers(n, ref framebuffers);
            else                    GL.Ext.DeleteFramebuffers(n, ref framebuffers);
        }
        public static void GenFramebuffers(int n, out uint framebuffers)
        {
            if (UseArbFramebuffer)  GL.GenFramebuffers(n, out framebuffers);
            else                    GL.Ext.GenFramebuffers(n, out framebuffers);
        }
        public static FramebufferErrorCode CheckFramebufferStatus(FramebufferTarget target)
        {
            return UseArbFramebuffer ? GL.CheckFramebufferStatus(target) : GL.Ext.CheckFramebufferStatus(target);
        }
        public static void FramebufferTexture2D(FramebufferTarget target, FramebufferAttachment attachment, TextureTarget textarget, uint texture, int level)
        {
            if (UseArbFramebuffer)  GL.FramebufferTexture2D(target, attachment, textarget, texture, level);
            else                    GL.Ext.FramebufferTexture2D(target, attachment, textarget, texture, level);
        }
        public static void FramebufferTexture2D(FramebufferTarget target, FramebufferAttachment attachment, TextureTarget textarget, int texture, int level)
        {
            if (UseArbFramebuffer)  GL.FramebufferTexture2D(target, attachment, textarget, texture, level);
            else                    GL.Ext.FramebufferTexture2D(target, attachment, textarget, texture, level);
        }
        public static void FramebufferRenderbuffer(FramebufferTarget target, FramebufferAttachment attachment, RenderbufferTarget renderbuffertarget, uint renderbuffer)
        {
            if (UseArbFramebuffer)  GL.FramebufferRenderbuffer(target, attachment, renderbuffertarget, renderbuffer);
            else                    GL.Ext.FramebufferRenderbuffer(target, attachment, renderbuffertarget, renderbuffer);
        }
        public static void GetFramebufferAttachmentParameter(FramebufferTarget target, FramebufferAttachment attachment, FramebufferParameterName pname, out int @params)
        {
            if (UseArbFramebuffer)  GL.GetFramebufferAttachmentParameter(target, attachment, pname, out @params);
            else                    GL.Ext.GetFramebufferAttachmentParameter(target, attachment, pname, out @params);
        }
        public static void GenerateMipmap(GenerateMipmapTarget target)
        {
            if (UseArbFramebuffer)  GL.GenerateMipmap(target);
            else                    GL.Ext.GenerateMipmap(target);
        }

        public static All OpenGL11(CullMode cull)
        {
            switch (cull)
            {
                case CullMode.CullClockwiseFace:
                    return All.Cw;
                case CullMode.CullCounterClockwiseFace:
                    return All.Ccw;
                default:
                    throw new NotImplementedException();
            }
        }

        public static int OpenGLNumberOfElements(this VertexElementFormat elementFormat)
        {
            switch (elementFormat)
            {
                case VertexElementFormat.Single:
                    return 1;

                case VertexElementFormat.Vector2:
                    return 2;

                case VertexElementFormat.Vector3:
                    return 3;

                case VertexElementFormat.Vector4:
                    return 4;

                case VertexElementFormat.Color:
                    return 4;

                case VertexElementFormat.Byte4:
                    return 4;

                case VertexElementFormat.Short2:
                    return 2;

                case VertexElementFormat.Short4:
                    return 2;

                case VertexElementFormat.NormalizedShort2:
                    return 2;

                case VertexElementFormat.NormalizedShort4:
                    return 4;

                case VertexElementFormat.HalfVector2:
                    return 2;

                case VertexElementFormat.HalfVector4:
                    return 4;
            }

            throw new NotImplementedException();
        }

        public static VertexPointerType OpenGLVertexPointerType(this VertexElementFormat elementFormat)
        {
            switch (elementFormat)
            {
                case VertexElementFormat.Single:
                    return VertexPointerType.Float;

                case VertexElementFormat.Vector2:
                    return VertexPointerType.Float;

                case VertexElementFormat.Vector3:
                    return VertexPointerType.Float;

                case VertexElementFormat.Vector4:
                    return VertexPointerType.Float;

                case VertexElementFormat.Color:
                    return VertexPointerType.Short;

                case VertexElementFormat.Byte4:
                    return VertexPointerType.Short;

                case VertexElementFormat.Short2:
                    return VertexPointerType.Short;

                case VertexElementFormat.Short4:
                    return VertexPointerType.Short;

                case VertexElementFormat.NormalizedShort2:
                    return VertexPointerType.Short;

                case VertexElementFormat.NormalizedShort4:
                    return VertexPointerType.Short;

                case VertexElementFormat.HalfVector2:
                    return VertexPointerType.Float;

                case VertexElementFormat.HalfVector4:
                    return VertexPointerType.Float;
            }

            throw new NotImplementedException();
        }

		public static VertexAttribPointerType OpenGLVertexAttribPointerType(this VertexElementFormat elementFormat)
        {
            switch (elementFormat)
            {
                case VertexElementFormat.Single:
                    return VertexAttribPointerType.Float;

                case VertexElementFormat.Vector2:
                    return VertexAttribPointerType.Float;

                case VertexElementFormat.Vector3:
                    return VertexAttribPointerType.Float;

                case VertexElementFormat.Vector4:
                    return VertexAttribPointerType.Float;

                case VertexElementFormat.Color:
					return VertexAttribPointerType.UnsignedByte;

                case VertexElementFormat.Byte4:
					return VertexAttribPointerType.UnsignedByte;

                case VertexElementFormat.Short2:
                    return VertexAttribPointerType.Short;

                case VertexElementFormat.Short4:
                    return VertexAttribPointerType.Short;

                case VertexElementFormat.NormalizedShort2:
                    return VertexAttribPointerType.Short;

                case VertexElementFormat.NormalizedShort4:
                    return VertexAttribPointerType.Short;
                
#if SDL2
               case VertexElementFormat.HalfVector2:
                    return VertexAttribPointerType.HalfFloat;

                case VertexElementFormat.HalfVector4:
                    return VertexAttribPointerType.HalfFloat;
#endif
            }

            throw new NotImplementedException();
        }

        public static bool OpenGLVertexAttribNormalized(this VertexElement element)
        {
            // TODO: This may or may not be the right behavor.  
            //
            // For instance the VertexElementFormat.Byte4 format is not supposed
            // to be normalized, but this line makes it so.
            //
            // The question is in MS XNA are types normalized based on usage or
            // normalized based to their format?
            //
            if (element.VertexElementUsage == VertexElementUsage.Color)
                return true;

            switch (element.VertexElementFormat)
            {
                case VertexElementFormat.NormalizedShort2:
                case VertexElementFormat.NormalizedShort4:
                    return true;

                default:
                    return false;
            }
        }

        public static ColorPointerType OpenGLColorPointerType(this VertexElementFormat elementFormat)
        {
            switch (elementFormat)
            {
                case VertexElementFormat.Single:
                    return ColorPointerType.Float;

                case VertexElementFormat.Vector2:
                    return ColorPointerType.Float;

                case VertexElementFormat.Vector3:
                    return ColorPointerType.Float;

                case VertexElementFormat.Vector4:
                    return ColorPointerType.Float;

                case VertexElementFormat.Color:
                    return ColorPointerType.UnsignedByte;

                case VertexElementFormat.Byte4:
                    return ColorPointerType.UnsignedByte;

                case VertexElementFormat.Short2:
                    return ColorPointerType.Short;

                case VertexElementFormat.Short4:
                    return ColorPointerType.Short;

                case VertexElementFormat.NormalizedShort2:
                    return ColorPointerType.UnsignedShort;

                case VertexElementFormat.NormalizedShort4:
                    return ColorPointerType.UnsignedShort;
				
#if SDL2
                case VertexElementFormat.HalfVector2:
                    return ColorPointerType.HalfFloat;

                case VertexElementFormat.HalfVector4:
                    return ColorPointerType.HalfFloat;
#endif
			}

            throw new NotImplementedException();
        }

       public static NormalPointerType OpenGLNormalPointerType(this VertexElementFormat elementFormat)
        {
            switch (elementFormat)
            {
                case VertexElementFormat.Single:
                    return NormalPointerType.Float;

                case VertexElementFormat.Vector2:
                    return NormalPointerType.Float;

                case VertexElementFormat.Vector3:
                    return NormalPointerType.Float;

                case VertexElementFormat.Vector4:
                    return NormalPointerType.Float;

                case VertexElementFormat.Color:
                    return NormalPointerType.Byte;

                case VertexElementFormat.Byte4:
                    return NormalPointerType.Byte;

                case VertexElementFormat.Short2:
                    return NormalPointerType.Short;

                case VertexElementFormat.Short4:
                    return NormalPointerType.Short;

                case VertexElementFormat.NormalizedShort2:
                    return NormalPointerType.Short;

                case VertexElementFormat.NormalizedShort4:
                    return NormalPointerType.Short;
				
#if SDL2
                case VertexElementFormat.HalfVector2:
                    return NormalPointerType.HalfFloat;

                case VertexElementFormat.HalfVector4:
                    return NormalPointerType.HalfFloat;
#endif
			}

            throw new NotImplementedException();
        }

       public static TexCoordPointerType OpenGLTexCoordPointerType(this VertexElementFormat elementFormat)
        {
            switch (elementFormat)
            {
                case VertexElementFormat.Single:
                    return TexCoordPointerType.Float;

                case VertexElementFormat.Vector2:
                    return TexCoordPointerType.Float;

                case VertexElementFormat.Vector3:
                    return TexCoordPointerType.Float;

                case VertexElementFormat.Vector4:
                    return TexCoordPointerType.Float;

                case VertexElementFormat.Color:
                    return TexCoordPointerType.Float;

                case VertexElementFormat.Byte4:
                    return TexCoordPointerType.Float;

                case VertexElementFormat.Short2:
                    return TexCoordPointerType.Short;

                case VertexElementFormat.Short4:
                    return TexCoordPointerType.Short;

                case VertexElementFormat.NormalizedShort2:
                    return TexCoordPointerType.Short;

                case VertexElementFormat.NormalizedShort4:
                    return TexCoordPointerType.Short;
				
#if SDL2
                case VertexElementFormat.HalfVector2:
                    return TexCoordPointerType.HalfFloat;

                case VertexElementFormat.HalfVector4:
                    return TexCoordPointerType.HalfFloat;
#endif
			}

            throw new NotImplementedException();
        }

		
		public static BlendEquationMode GetBlendEquationMode (this BlendFunction function)
		{
			switch (function) {
			case BlendFunction.Add:
				return BlendEquationMode.FuncAdd;
#if IOS
			case BlendFunction.Max:
				return BlendEquationMode.MaxExt;
			case BlendFunction.Min:
				return BlendEquationMode.MinExt;
#else 
			case BlendFunction.Max:
				return BlendEquationMode.Max;
			case BlendFunction.Min:
				return BlendEquationMode.Min;
#endif
			case BlendFunction.ReverseSubtract:
				return BlendEquationMode.FuncReverseSubtract;
			case BlendFunction.Subtract:
				return BlendEquationMode.FuncSubtract;

			default:
                throw new NotImplementedException();
			}
		}

		public static BlendingFactorSrc GetBlendFactorSrc (this Blend blend)
		{
			switch (blend) {
			case Blend.DestinationAlpha:
				return BlendingFactorSrc.DstAlpha;
			case Blend.DestinationColor:
				return BlendingFactorSrc.DstColor;
			case Blend.InverseDestinationAlpha:
				return BlendingFactorSrc.OneMinusDstAlpha;
			case Blend.InverseDestinationColor:
				return BlendingFactorSrc.OneMinusDstColor;
			case Blend.InverseSourceAlpha:
				return BlendingFactorSrc.OneMinusSrcAlpha;
			case Blend.InverseSourceColor:
#if SDL2
				return (BlendingFactorSrc)All.OneMinusSrcColor;
#else
				return BlendingFactorSrc.OneMinusSrcColor;
#endif
			case Blend.One:
				return BlendingFactorSrc.One;
			case Blend.SourceAlpha:
				return BlendingFactorSrc.SrcAlpha;
			case Blend.SourceAlphaSaturation:
				return BlendingFactorSrc.SrcAlphaSaturate;
			case Blend.SourceColor:
#if SDL2
				return (BlendingFactorSrc)All.SrcColor;
#else
				return BlendingFactorSrc.SrcColor;
#endif
			case Blend.Zero:
				return BlendingFactorSrc.Zero;
			default:
				return BlendingFactorSrc.One;
			}

		}

		public static BlendingFactorDest GetBlendFactorDest (this Blend blend)
		{
			switch (blend) {
			case Blend.DestinationAlpha:
				return BlendingFactorDest.DstAlpha;
//			case Blend.DestinationColor:
//				return BlendingFactorDest.DstColor;
			case Blend.InverseDestinationAlpha:
				return BlendingFactorDest.OneMinusDstAlpha;
//			case Blend.InverseDestinationColor:
//				return BlendingFactorDest.OneMinusDstColor;
			case Blend.InverseSourceAlpha:
				return BlendingFactorDest.OneMinusSrcAlpha;
			case Blend.InverseSourceColor:
#if SDL2
				return (BlendingFactorDest)All.OneMinusSrcColor;
#else
				return BlendingFactorDest.OneMinusSrcColor;
#endif
			case Blend.One:
				return BlendingFactorDest.One;
			case Blend.SourceAlpha:
				return BlendingFactorDest.SrcAlpha;
//			case Blend.SourceAlphaSaturation:
//				return BlendingFactorDest.SrcAlphaSaturate;
			case Blend.SourceColor:
#if SDL2
				return (BlendingFactorDest)All.SrcColor;
#else
				return BlendingFactorDest.SrcColor;
#endif
			case Blend.Zero:
				return BlendingFactorDest.Zero;
			default:
				return BlendingFactorDest.One;
			}

		}
		
		
		internal static void GetGLFormat (this SurfaceFormat format,
		                                 out PixelInternalFormat glInternalFormat,
		                                 out PixelFormat glFormat,
		                                 out PixelType glType)
		{
			glInternalFormat = PixelInternalFormat.Rgba;
			glFormat = PixelFormat.Rgba;
			glType = PixelType.UnsignedByte;
			
			switch (format) {
			case SurfaceFormat.Color:
				glInternalFormat = PixelInternalFormat.Rgba;
				glFormat = PixelFormat.Rgba;
				glType = PixelType.UnsignedByte;
				break;
			case SurfaceFormat.Bgr565:
				glInternalFormat = PixelInternalFormat.Rgb;
				glFormat = PixelFormat.Rgb;
				glType = PixelType.UnsignedShort565;
				break;
			case SurfaceFormat.Bgra4444:
#if IOS || ANDROID
				glInternalFormat = PixelInternalFormat.Rgba;
#else
				glInternalFormat = PixelInternalFormat.Rgba4;
#endif
				glFormat = PixelFormat.Rgba;
				glType = PixelType.UnsignedShort4444;
				break;
			case SurfaceFormat.Bgra5551:
				glInternalFormat = PixelInternalFormat.Rgba;
				glFormat = PixelFormat.Rgba;
				glType = PixelType.UnsignedShort5551;
				break;
			case SurfaceFormat.Alpha8:
				glInternalFormat = PixelInternalFormat.Luminance;
				glFormat = PixelFormat.Luminance;
				glType = PixelType.UnsignedByte;
				break;
#if !IOS && !ANDROID
			case SurfaceFormat.Dxt1:
				glInternalFormat = PixelInternalFormat.CompressedRgbaS3tcDxt1Ext;
				glFormat = (PixelFormat)All.CompressedTextureFormats;
				break;
			case SurfaceFormat.Dxt3:
				glInternalFormat = PixelInternalFormat.CompressedRgbaS3tcDxt3Ext;
				glFormat = (PixelFormat)All.CompressedTextureFormats;
				break;
			case SurfaceFormat.Dxt5:
				glInternalFormat = PixelInternalFormat.CompressedRgbaS3tcDxt5Ext;
				glFormat = (PixelFormat)All.CompressedTextureFormats;
				break;
			
			case SurfaceFormat.Single:
				glInternalFormat = PixelInternalFormat.R32f;
				glFormat = PixelFormat.Red;
				glType = PixelType.Float;
				break;
#endif
				
#if IOS || ANDROID
			case SurfaceFormat.RgbPvrtc2Bpp:
				glInternalFormat = PixelInternalFormat.CompressedRgbPvrtc2Bppv1Img;
				glFormat = (PixelFormat)All.CompressedTextureFormats;
				break;
			case SurfaceFormat.RgbPvrtc4Bpp:
				glInternalFormat = PixelInternalFormat.CompressedRgbPvrtc4Bppv1Img;
				glFormat = (PixelFormat)All.CompressedTextureFormats;
				break;
			case SurfaceFormat.RgbaPvrtc2Bpp:
				glInternalFormat = PixelInternalFormat.CompressedRgbaPvrtc2Bppv1Img;
				glFormat = (PixelFormat)All.CompressedTextureFormats;
				break;
			case SurfaceFormat.RgbaPvrtc4Bpp:
				glInternalFormat = PixelInternalFormat.CompressedRgbaPvrtc4Bppv1Img;
				glFormat = (PixelFormat)All.CompressedTextureFormats;
				break;
#endif
			default:
				throw new NotSupportedException();
			}
		}

#endif // OPENGL

        public static int Size(this SurfaceFormat surfaceFormat)
        {
            switch (surfaceFormat)
            {
                case SurfaceFormat.Color:
                    return 4;
                case SurfaceFormat.Dxt3:
                    return 4;
                case SurfaceFormat.Bgr565:
                    return 2;
                case SurfaceFormat.Bgra4444:
                    return 2;
                case SurfaceFormat.Bgra5551:
                    return 2;
                case SurfaceFormat.Alpha8:
                    return 1;
				case SurfaceFormat.NormalizedByte4:
                    return 4;
                default:
                    throw new NotImplementedException();
            }
        }
		
        public static int GetTypeSize(this VertexElementFormat elementFormat)
        {
            switch (elementFormat)
            {
                case VertexElementFormat.Single:
                    return 4;

                case VertexElementFormat.Vector2:
                    return 8;

                case VertexElementFormat.Vector3:
                    return 12;

                case VertexElementFormat.Vector4:
                    return 0x10;

                case VertexElementFormat.Color:
                    return 4;

                case VertexElementFormat.Byte4:
                    return 4;

                case VertexElementFormat.Short2:
                    return 4;

                case VertexElementFormat.Short4:
                    return 8;

                case VertexElementFormat.NormalizedShort2:
                    return 4;

                case VertexElementFormat.NormalizedShort4:
                    return 8;

                case VertexElementFormat.HalfVector2:
                    return 4;

                case VertexElementFormat.HalfVector4:
                    return 8;
            }
            return 0;
        }

#if OPENGL

        public static int GetBoundTexture2D()
        {
            var prevTexture = 0;
#if GLES
            GL.GetInteger(GetPName.TextureBinding2D, ref prevTexture);
#else
            GL.GetInteger(GetPName.TextureBinding2D, out prevTexture);
#endif
            GraphicsExtensions.LogGLError("GraphicsExtensions.GetBoundTexture2D() GL.GetInteger");
            return prevTexture;
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void CheckGLError()
        {
#if GLES
            All error = GL.GetError();
            if (error != All.False)
                throw new MonoGameGLException("GL.GetError() returned " + error.ToString());
#elif OPENGL
            if (Threading.IsOnUIThread())
            {
                ErrorCode error = GL.GetError();
                if (error != ErrorCode.NoError)
                    throw new MonoGameGLException("GL.GetError() returned " + error.ToString());
            }
#endif

        }
#endif

#if OPENGL
        [System.Diagnostics.Conditional("DEBUG")]
        public static void LogGLError(string location)
        {
            try
            {
                GraphicsExtensions.CheckGLError();
            }
            catch (MonoGameGLException ex)
            {
#if ANDROID
                // Todo: Add generic MonoGame logging interface
                Android.Util.Log.Debug("MonoGame", "MonoGameGLException at " + location + " - " + ex.Message);
#else
                LogToFile(LogSeverity.Error, ex.ToString());
#endif
            }
        }
#endif

        public enum LogSeverity { Information, Warning, Error }
        public static void LogToFile(string message)
        {
            LogToFile(LogSeverity.Information, message);
        }
        public static void LogToFile(LogSeverity severity, string message)
        {
            const string TimeFormat = "HH:mm:ss.fff";
            try 
            {
                using (var stream = File.Open("Debug Log.txt", FileMode.Append))
                {
                    using (var writer = new StreamWriter(stream))
                    {
                        writer.WriteLine("({0}) [{1}] {2} : {3}", 
                            DateTime.Now.ToString(TimeFormat), "MonoGame", severity.ToString().ToUpper(System.Globalization.CultureInfo.InvariantCulture), message);
                    }
                };
            }
            catch (Exception ex)
            {
                // NOT THAT BIG A DEAL GUYS
            }
        }
    }

    public class MonoGameGLException : Exception
    {
        public MonoGameGLException(string message)
            : base(message)
        {
        }
    }
}
