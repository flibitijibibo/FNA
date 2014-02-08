// vim: ts=4:sw=4:et

using System;
using OpenTK.Graphics.OpenGL;

namespace Microsoft.Xna.Framework.Graphics
{
    internal class OpenGLDevice
    {
        #region The OpenGL Device Instance

        public static OpenGLDevice Instance;

        #endregion

        #region OpenGL State Container Class

        public class OpenGLState<T>
        {
            private T current;
            private T latch;

            public OpenGLState(T defaultValue)
            {
                current = defaultValue;
                latch = defaultValue;
            }

            public T Get()
            {
                return latch;
            }

            public T GetCurrent()
            {
                return current;
            }

            public void Set(T newValue)
            {
                latch = newValue;
            }

            public bool NeedsFlush()
            {
                return !current.Equals(latch);
            }

            public T Flush()
            {
                current = latch;
                return current;
            }
        }

        #endregion

        #region OpenGL Sampler State Container Class

        public class OpenGLSampler
        {
            public OpenGLState<int> Texture;
            public OpenGLState<TextureAddressMode> WrapS;
            public OpenGLState<TextureAddressMode> WrapT;
            public OpenGLState<TextureAddressMode> WrapR;
            public OpenGLState<TextureFilter> Filter;
            public OpenGLState<TextureTarget> Target;

            public OpenGLSampler()
            {
                Texture = new OpenGLState<int>(0);
                WrapS = new OpenGLState<TextureAddressMode>(TextureAddressMode.Wrap);
                WrapT = new OpenGLState<TextureAddressMode>(TextureAddressMode.Wrap);
                WrapR = new OpenGLState<TextureAddressMode>(TextureAddressMode.Wrap);
                Filter = new OpenGLState<TextureFilter>(TextureFilter.Linear);
                Target = new OpenGLState<TextureTarget>(TextureTarget.Texture2D);
            }
        }

        #endregion

        #region OpenGL Vertex Attribute State Container Class

        public class OpenGLVertexAttribute
        {
            // Checked in FlushVertexAttributes
            public OpenGLState<bool> Enabled;
            public OpenGLState<int> Divisor;

            // Checked in VertexAttribPointer
            public int CurrentBuffer;
            public int CurrentSize;
            public VertexAttribPointerType CurrentType;
            public bool CurrentNormalized;
            public int CurrentStride;
            public IntPtr CurrentPointer;

            public OpenGLVertexAttribute()
            {
                Enabled = new OpenGLState<bool>(false);
                Divisor = new OpenGLState<int>(0);
                CurrentBuffer = 0;
                CurrentSize = 4;
                CurrentType = VertexAttribPointerType.Float;
                CurrentNormalized = false;
                CurrentStride = 0;
                CurrentPointer = IntPtr.Zero;
            }
        }

        #endregion

        #region Alpha Blending State Variables

        public OpenGLState<bool> AlphaBlendEnable = new OpenGLState<bool>(false);

        public OpenGLState<bool> AlphaTestEnable = new OpenGLState<bool>(false);

        public OpenGLState<bool> SeparateAlphaBlendEnable = new OpenGLState<bool>(false);

        public OpenGLState<Blend> SrcBlend = new OpenGLState<Blend>(Blend.One);

        public OpenGLState<Blend> DstBlend = new OpenGLState<Blend>(Blend.Zero);

        public OpenGLState<Blend> SrcBlendAlpha = new OpenGLState<Blend>(Blend.One);

        public OpenGLState<Blend> DstBlendAlpha = new OpenGLState<Blend>(Blend.Zero);

        public OpenGLState<BlendFunction> BlendOp = new OpenGLState<BlendFunction>(BlendFunction.Add);

        public OpenGLState<BlendFunction> BlendOpAlpha = new OpenGLState<BlendFunction>(BlendFunction.Add);

        // TODO: glAlphaFunc? -flibit

        #endregion

        #region Depth State Variables

        public OpenGLState<bool> ZEnable = new OpenGLState<bool>(false);

        public OpenGLState<bool> ZWriteEnable = new OpenGLState<bool>(false);

        public OpenGLState<CompareFunction> DepthFunc = new OpenGLState<CompareFunction>(CompareFunction.Less);

        public OpenGLState<float> DepthBias = new OpenGLState<float>(0.0f);

        public OpenGLState<float> SlopeScaleDepthBias = new OpenGLState<float>(0.0f);

        #endregion

        #region Stencil State Variables

        public OpenGLState<bool> StencilEnable = new OpenGLState<bool>(false);

        // TODO: glStencilMask(StencilWriteMask)? -flibit

        public OpenGLState<bool> SeparateStencilEnable = new OpenGLState<bool>(false);

        public OpenGLState<uint> StencilRef = new OpenGLState<uint>(0);

        public OpenGLState<uint> StencilMask = new OpenGLState<uint>(0xFFFFFFFF);

        public OpenGLState<CompareFunction> StencilFunc = new OpenGLState<CompareFunction>(CompareFunction.Always);

        public OpenGLState<StencilOperation> StencilFail = new OpenGLState<StencilOperation>(StencilOperation.Keep);

        public OpenGLState<StencilOperation> StencilZFail = new OpenGLState<StencilOperation>(StencilOperation.Keep);

        public OpenGLState<StencilOperation> StencilPass = new OpenGLState<StencilOperation>(StencilOperation.Keep);

        public OpenGLState<CompareFunction> CCWStencilFunc = new OpenGLState<CompareFunction>(CompareFunction.Always);

        public OpenGLState<StencilOperation> CCWStencilFail = new OpenGLState<StencilOperation>(StencilOperation.Keep);

        public OpenGLState<StencilOperation> CCWStencilZFail = new OpenGLState<StencilOperation>(StencilOperation.Keep);

        public OpenGLState<StencilOperation> CCWStencilPass = new OpenGLState<StencilOperation>(StencilOperation.Keep);

        #endregion

        #region Miscellaneous Write State Variables

        public OpenGLState<bool> ScissorTestEnable = new OpenGLState<bool>(false);

        public OpenGLState<CullMode> CullFrontFace = new OpenGLState<CullMode>(CullMode.None);

        public OpenGLState<FillMode> GLFillMode = new OpenGLState<FillMode>(FillMode.Solid);

        public OpenGLState<ColorWriteChannels> ColorWriteEnable = new OpenGLState<ColorWriteChannels>(ColorWriteChannels.All);

        public OpenGLState<Rectangle> GLViewport = new OpenGLState<Rectangle>(
            new Rectangle(
                0,
                0,
                GraphicsDeviceManager.DefaultBackBufferWidth,
                GraphicsDeviceManager.DefaultBackBufferHeight
            )
        );

        #endregion

        #region Framebuffer State Variables

        public OpenGLState<uint> BoundReadFramebuffer = new OpenGLState<uint>(0);
        public OpenGLState<uint> BoundDrawFramebuffer = new OpenGLState<uint>(0);
        // This function emulates the behaviour of glBindFramebuffer's GL_FRAMEBUFFER target,
        // which binds the given FBO to both the READ and DRAW framebuffers.
        public void BindFramebuffer(uint frameBufferObject)
        {
            BoundReadFramebuffer.Set(frameBufferObject);
            BoundDrawFramebuffer.Set(frameBufferObject);
        }
        // We're setting the default to GL_BACK here, but it should default to GL_FRONT on single buffered contexts.
        // Given single-buffered contexts are terrible, and we never use them because we like not having horrible flickering,
        // this shouldn't be a problem. Note that for FBOs, you'll want to cast a FramebufferAttachment to DrawBufferMode when
        // setting this.
        public OpenGLState<DrawBufferMode> CurrentDrawBufferMode = new OpenGLState<DrawBufferMode>(DrawBufferMode.Back);

        #endregion

        #region Sampler State Variables

        public OpenGLSampler[] Samplers
        {
            get;
            private set;
        }

        #endregion

        #region Vertex Attribute State Variables

        public OpenGLVertexAttribute[] Attributes
        {
            get;
            private set;
        }

        #endregion

        #region Buffer Binding Cache Variables

        private int currentVertexBuffer = 0;
        private int currentIndexBuffer = 0;

        #endregion

        #region Constructor

        public OpenGLDevice()
        {
            // Load OpenGL entry points
            GL.LoadAll();

            // Initialize sampler state array
            int numSamplers;
            GL.GetInteger(GetPName.MaxTextureUnits, out numSamplers);
            Samplers = new OpenGLSampler[numSamplers];
            for (int i = 0; i < numSamplers; i += 1)
            {
                Samplers[i] = new OpenGLSampler();
            }

            // Initialize vertex attribute state array
            int numAttributes;
            GL.GetInteger(GetPName.MaxVertexAttribs, out numAttributes);
            Attributes = new OpenGLVertexAttribute[numAttributes];
            for (int i = 0; i < numAttributes; i += 1)
            {
                Attributes[i] = new OpenGLVertexAttribute();
            }

            // Flush GL state to default state
            FlushGLState(true);
        }

        #endregion

        #region glEnable/glDisable Method

        private void ToggleGLState(EnableCap feature, bool enable)
        {
            if (enable)
            {
                GL.Enable(feature);
            }
            else
            {
                GL.Disable(feature);
            }
        }

        #endregion

        #region Flush State Method

        public void FlushGLState(bool force = false)
        {
            // ALPHA BLENDING STATES

            if (force || AlphaBlendEnable.NeedsFlush())
            {
                ToggleGLState(EnableCap.Blend, AlphaBlendEnable.Flush());
            }

            if (force || AlphaTestEnable.NeedsFlush())
            {
                ToggleGLState(EnableCap.AlphaTest, AlphaTestEnable.Flush());
            }

            if (    force ||
                    SeparateAlphaBlendEnable.NeedsFlush() ||
                    SrcBlend.NeedsFlush() ||
                    DstBlend.NeedsFlush() ||
                    SrcBlendAlpha.NeedsFlush() ||
                    DstBlendAlpha.NeedsFlush()  )
            {
                if (SeparateAlphaBlendEnable.Flush())
                {
                    GL.BlendFuncSeparate(
                        GetBlendModeSrc(SrcBlend.Flush()),
                        GetBlendModeDst(DstBlend.Flush()),
                        GetBlendModeSrc(SrcBlendAlpha.Flush()),
                        GetBlendModeDst(DstBlendAlpha.Flush())
                    );
                }
                else
                {
                    GL.BlendFunc(
                        GetBlendModeSrc(SrcBlend.Flush()),
                        GetBlendModeDst(DstBlend.Flush())
                    );
                    SrcBlendAlpha.Flush();
                    DstBlendAlpha.Flush();
                }
            }

            if (force || BlendOp.NeedsFlush() || BlendOpAlpha.NeedsFlush())
            {
                GL.BlendEquationSeparate(
                    GetBlendEquation(BlendOp.Flush()),
                    GetBlendEquation(BlendOpAlpha.Flush())
                );
            }

            // TODO: glAlphaFunc? -flibit

            // END ALPHA BLENDING STATES

            // DEPTH STATES

            if (force || ZEnable.NeedsFlush())
            {
                ToggleGLState(EnableCap.DepthTest, ZEnable.Flush());
            }

            if (force || ZWriteEnable.NeedsFlush())
            {
                GL.DepthMask(ZWriteEnable.Flush());
            }

            if (force || DepthFunc.NeedsFlush())
            {
                GL.DepthFunc(GetDepthFunc(DepthFunc.Flush()));
            }

            if (    force ||
                    DepthBias.NeedsFlush() ||
                    SlopeScaleDepthBias.NeedsFlush()    )
            {
                float depthBias = DepthBias.Flush();
                float slopeScaleDepthBias = SlopeScaleDepthBias.Flush();
                if (depthBias == 0.0f && slopeScaleDepthBias == 0.0f)
                {
                    ToggleGLState(EnableCap.PolygonOffsetFill, false);
                }
                else
                {
                    ToggleGLState(EnableCap.PolygonOffsetFill, true);
                    GL.PolygonOffset(slopeScaleDepthBias, depthBias);
                }
            }

            // END DEPTH STATES

            // STENCIL STATES

            if (force || StencilEnable.NeedsFlush())
            {
                ToggleGLState(EnableCap.StencilTest, StencilEnable.Flush());
            }

            // TODO: glStencilMask? -flibit

            if (    force ||
                    SeparateStencilEnable.NeedsFlush() ||
                    StencilRef.NeedsFlush() ||
                    StencilMask.NeedsFlush() ||
                    StencilFunc.NeedsFlush() ||
                    CCWStencilFunc.NeedsFlush() )
            {
                if (SeparateStencilEnable.Flush())
                {
                    GL.StencilFuncSeparate(
                        (Version20) CullFaceMode.Front,
                        GetStencilFunc(StencilFunc.Flush()),
                        (int) StencilRef.Flush(),
                        StencilMask.Flush()
                    );
                    GL.StencilFuncSeparate(
                        (Version20) CullFaceMode.Back,
                        GetStencilFunc(CCWStencilFunc.Flush()),
                        (int) StencilRef.Flush(),
                        StencilMask.Flush()
                    );
                }
                else
                {
                    GL.StencilFunc(
                        GetStencilFunc(StencilFunc.Flush()),
                        (int) StencilRef.Flush(),
                        StencilMask.Flush()
                    );
                }
            }

            if (    force ||
                    SeparateStencilEnable.NeedsFlush() ||
                    StencilFail.NeedsFlush() ||
                    StencilZFail.NeedsFlush() ||
                    StencilPass.NeedsFlush() ||
                    CCWStencilFail.NeedsFlush() ||
                    CCWStencilZFail.NeedsFlush() ||
                    CCWStencilPass.NeedsFlush() )
            {
                if (SeparateStencilEnable.Flush())
                {
                    GL.StencilOpSeparate(
                        StencilFace.Front,
                        GetStencilOp(StencilFail.Flush()),
                        GetStencilOp(StencilZFail.Flush()),
                        GetStencilOp(StencilPass.Flush())
                    );
                    GL.StencilOpSeparate(
                        StencilFace.Back,
                        GetStencilOp(CCWStencilFail.Flush()),
                        GetStencilOp(CCWStencilZFail.Flush()),
                        GetStencilOp(CCWStencilPass.Flush())
                    );
                }
                else
                {
                    GL.StencilOp(
                        GetStencilOp(StencilFail.Flush()),
                        GetStencilOp(StencilZFail.Flush()),
                        GetStencilOp(StencilPass.Flush())
                    );
                }
            }

            // END STENCIL STATES

            // MISCELLANEOUS WRITE STATES

            if (force || ScissorTestEnable.NeedsFlush())
            {
                ToggleGLState(EnableCap.ScissorTest, ScissorTestEnable.Flush());
            }

            if (force || CullFrontFace.NeedsFlush())
            {
                CullMode current = CullFrontFace.GetCurrent();
                CullMode latched = CullFrontFace.Flush();
                if (force || (latched == CullMode.None) != (current == CullMode.None))
                {
                    ToggleGLState(EnableCap.CullFace, latched != CullMode.None);
                }
                if (latched != CullMode.None)
                {
                    GL.FrontFace(GetFrontFace(latched));
                }
            }

            if (force || GLFillMode.NeedsFlush())
            {
                GL.PolygonMode(
                    MaterialFace.FrontAndBack,
                    GetFillMode(GLFillMode.Flush())
                );
            }

            if (force || ColorWriteEnable.NeedsFlush())
            {
                ColorWriteChannels write = ColorWriteEnable.Flush();
                GL.ColorMask(
                    (write & ColorWriteChannels.Red) != 0,
                    (write & ColorWriteChannels.Green) != 0,
                    (write & ColorWriteChannels.Blue) != 0,
                    (write & ColorWriteChannels.Alpha) != 0
                );
            }

            if (force || GLViewport.NeedsFlush())
            {
                Rectangle viewport = GLViewport.Flush();
                GL.Viewport(
                    viewport.X,
                    viewport.Y,
                    viewport.Width,
                    viewport.Height
                );
            }

            // END MISCELLANEOUS WRITE STATES

            // SAMPLER STATES

            int activeTexture = 0;
            for (int i = 0; i < Samplers.Length; i += 1)
            {
                OpenGLSampler sampler = Samplers[i];
                if (    force ||
                        sampler.Target.NeedsFlush() ||
                        sampler.Texture.NeedsFlush() ||
                        sampler.WrapS.NeedsFlush() ||
                        sampler.WrapT.NeedsFlush() ||
                        sampler.WrapR.NeedsFlush() ||
                        sampler.Filter.NeedsFlush() )
                {
                    // Nothing changed in this sampler, skip it.
                    continue;
                }

                activeTexture = i;
                GL.ActiveTexture(TextureUnit.Texture0 + i);

                TextureTarget target = sampler.Target.GetCurrent();
                bool targetForce = force;
                if (force || sampler.Target.NeedsFlush())
                {
                    force = true; // Reset the ENTIRE state when we change target!
                    GL.BindTexture(sampler.Target.GetCurrent(), 0);
                    target = sampler.Target.Flush();
                }

                if (force || sampler.Texture.NeedsFlush())
                {
                    GL.BindTexture(target, sampler.Texture.Flush());
                }

                if (force || sampler.WrapS.NeedsFlush())
                {
                    GL.TexParameter(
                        target,
                        TextureParameterName.TextureWrapS,
                        (int) GetWrap(sampler.WrapS.Flush())
                    );
                }

                if (force || sampler.WrapT.NeedsFlush())
                {
                    GL.TexParameter(
                        target,
                        TextureParameterName.TextureWrapT,
                        (int) GetWrap(sampler.WrapT.Flush())
                    );
                }

                if (force || sampler.WrapR.NeedsFlush())
                {
                    GL.TexParameter(
                        target,
                        TextureParameterName.TextureWrapR,
                        (int) GetWrap(sampler.WrapR.Flush())
                    );
                }

                if (force || sampler.Filter.NeedsFlush())
                {
                    TextureFilter filter = sampler.Filter.Flush();
                    TextureMagFilter magFilter;
                    TextureMinFilter minmipFilter;
                    float anistropicFilter = 1.0f;
                    if (filter == TextureFilter.Point)
                    {
                        magFilter = TextureMagFilter.Nearest;
                        minmipFilter = TextureMinFilter.NearestMipmapNearest;
                    }
                    else if (filter == TextureFilter.Linear)
                    {
                        magFilter = TextureMagFilter.Linear;
                        minmipFilter = TextureMinFilter.LinearMipmapLinear;
                    }
                    else if (filter == TextureFilter.Anisotropic)
                    {
                        anistropicFilter = 4.0f;
                        magFilter = TextureMagFilter.Linear;
                        minmipFilter = TextureMinFilter.LinearMipmapLinear;
                    }
                    else if (filter == TextureFilter.LinearMipPoint)
                    {
                        magFilter = TextureMagFilter.Linear;
                        minmipFilter = TextureMinFilter.LinearMipmapNearest;
                    }
                    else if (filter == TextureFilter.MinPointMagLinearMipPoint)
                    {
                        magFilter = TextureMagFilter.Linear;
                        minmipFilter = TextureMinFilter.NearestMipmapNearest;
                    }
                    else if (filter == TextureFilter.MinPointMagLinearMipLinear)
                    {
                        magFilter = TextureMagFilter.Linear;
                        minmipFilter = TextureMinFilter.NearestMipmapLinear;
                    }
                    else if (filter == TextureFilter.MinLinearMagPointMipPoint)
                    {
                        magFilter = TextureMagFilter.Nearest;
                        minmipFilter = TextureMinFilter.LinearMipmapNearest;
                    }
                    else if (filter == TextureFilter.MinLinearMagPointMipLinear)
                    {
                        magFilter = TextureMagFilter.Nearest;
                        minmipFilter = TextureMinFilter.LinearMipmapLinear;
                    }
                    else
                    {
                        throw new Exception("Unhandled TextureFilter!");
                    }
                    GL.TexParameter(
                        target,
                        TextureParameterName.TextureMagFilter,
                        (int) magFilter
                    );
                    GL.TexParameter(
                        target,
                        TextureParameterName.TextureMinFilter,
                        (int) minmipFilter
                    );
                    GL.TexParameter(
                        target,
                        (TextureParameterName) ExtTextureFilterAnisotropic.TextureMaxAnisotropyExt,
                        anistropicFilter
                    );
                }

                // You didn't see nothin'.
                force = targetForce;
            }
            if (activeTexture != 0)
            {
                // Keep this state sane. -flibit
                GL.ActiveTexture(TextureUnit.Texture0);
            }

            // END SAMPLER STATES

            // FRAMEBUFFER STATES

            if (force || BoundReadFramebuffer.NeedsFlush())
            {
                GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, BoundReadFramebuffer.Flush());
            }

            if (force || BoundDrawFramebuffer.NeedsFlush())
            {
                GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, BoundDrawFramebuffer.Flush());
            }

            if (force || CurrentDrawBufferMode.NeedsFlush())
            {
                GL.DrawBuffer(CurrentDrawBufferMode.Flush());
            }

            // END FRAMEBUFFER STATES

            // Check for errors.
            GraphicsExtensions.CheckGLError();
        }

        #endregion

        #region Flush Vertex Attribute Method

        public void FlushGLVertexAttributes(bool force)
        {
            for (int i = 0; i < Attributes.Length; i += 1)
            {
                OpenGLVertexAttribute attrib = Attributes[i];

                if (force || attrib.Enabled.NeedsFlush())
                {
                    if (attrib.Enabled.Flush())
                    {
                        GL.EnableVertexAttribArray(i);
                    }
                    else
                    {
                        GL.DisableVertexAttribArray(i);
                    }
                }

                if (force || attrib.Divisor.NeedsFlush())
                {
                    GL.VertexAttribDivisor(i, attrib.Divisor.Flush());
                }
            }
        }

        #endregion

        #region glVertexAttribPointer Method

        public void VertexAttribPointer(
            int location,
            int size,
            VertexAttribPointerType type,
            bool normalized,
            int stride,
            IntPtr pointer
        ) {
            if (    Attributes[location].CurrentBuffer != currentVertexBuffer ||
                    Attributes[location].CurrentPointer != pointer ||
                    Attributes[location].CurrentSize != size ||
                    Attributes[location].CurrentType != type ||
                    Attributes[location].CurrentNormalized != normalized ||
                    Attributes[location].CurrentStride != stride    )
            {
                GL.VertexAttribPointer(
                    location,
                    size,
                    type,
                    normalized,
                    stride,
                    pointer
                );
                Attributes[location].CurrentBuffer = currentVertexBuffer;
                Attributes[location].CurrentPointer = pointer;
                Attributes[location].CurrentSize = size;
                Attributes[location].CurrentType = type;
                Attributes[location].CurrentNormalized = normalized;
                Attributes[location].CurrentStride = stride;
            }
        }

        #endregion

        #region glBindBuffer Methods

        public void BindVertexBuffer(int buffer)
        {
            if (buffer != currentVertexBuffer)
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, buffer);
                currentVertexBuffer = buffer;
            }
        }

        public void BindIndexBuffer(int buffer)
        {
            if (buffer != currentIndexBuffer)
            {
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, buffer);
                currentIndexBuffer = buffer;
            }
        }

        #endregion

        #region Private XNA->GL Enum Conversion Methods

        private BlendingFactorSrc GetBlendModeSrc(Blend mode)
        {
            // TODO
            return BlendingFactorSrc.One;
        }

        private BlendingFactorDest GetBlendModeDst(Blend mode)
        {
            // TODO
            return BlendingFactorDest.Zero;
        }

        private BlendEquationMode GetBlendEquation(BlendFunction func)
        {
            // TODO
            return BlendEquationMode.FuncAdd;
        }

        private DepthFunction GetDepthFunc(CompareFunction func)
        {
            // TODO
            return DepthFunction.Always;
        }

        private StencilFunction GetStencilFunc(CompareFunction func)
        {
            // TODO
            return StencilFunction.Always;
        }

        private StencilOp GetStencilOp(StencilOperation op)
        {
            // TODO
            return StencilOp.Keep;
        }

        private FrontFaceDirection GetFrontFace(CullMode mode)
        {
            // TODO
            return FrontFaceDirection.Cw;
        }

        private PolygonMode GetFillMode(FillMode mode)
        {
            // TODO
            return PolygonMode.Fill;
        }

        private TextureWrapMode GetWrap(TextureAddressMode mode)
        {
            // TODO
            return TextureWrapMode.Repeat;
        }

        #endregion
    }
}
