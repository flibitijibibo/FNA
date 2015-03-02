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
	public class Effect : GraphicsResource
	{
		#region Public Properties

		private EffectTechnique INTERNAL_currentTechnique;
		public EffectTechnique CurrentTechnique
		{
			get
			{
				return INTERNAL_currentTechnique;
			}
			set
			{
				MojoShader.MOJOSHADER_effectSetTechnique(
					glEffect.EffectData,
					value.TechniquePointer
				);
				INTERNAL_currentTechnique = value;
			}
		}

		public EffectParameterCollection Parameters
		{
			get;
			private set;
		}

		public EffectTechniqueCollection Techniques
		{
			get;
			private set;
		}

		#endregion

		#region Private Variables

		private OpenGLDevice.OpenGLEffect glEffect;
		private Dictionary<string, EffectParameter> samplerMap = new Dictionary<string, EffectParameter>();
		private MojoShader.MOJOSHADER_effectStateChanges stateChanges = new MojoShader.MOJOSHADER_effectStateChanges();

		#endregion

		#region Private State Cache Hack Variables

		/* This is a workaround to prevent excessive state allocation.
		 * Well, I say "excessive", but even this allocates a minimum of
		 * 1916 bytes per effect. Just for states.
		 *
		 * Additionally, we depend on our inaccurate behavior of letting you
		 * change the state after it's been bound to the GraphicsDevice.
		 *
		 * More accurate behavior will require hashing the current states,
		 * comparing them to the new state after applying the effect, and
		 * getting a state from a cache, generating a new one if needed.
		 * -flibit
		 */
		private BlendState[] blendCache = new BlendState[2]
		{
			new BlendState(), new BlendState()
		};
		private DepthStencilState[] depthStencilCache = new DepthStencilState[2]
		{
			new DepthStencilState(), new DepthStencilState()
		};
		private RasterizerState[] rasterizerCache = new RasterizerState[2]
		{
			new RasterizerState(), new RasterizerState()
		};
		private SamplerState[] samplerCache = GenerateSamplerCache();
		private static SamplerState[] GenerateSamplerCache()
		{
			int numSamplers = 60; // FIXME: Arbitrary! -flibit
			SamplerState[] result = new SamplerState[numSamplers];
			for (int i = 0; i < numSamplers; i += 1)
			{
				result[i] = new SamplerState();
			}
			return result;
		}

		#endregion

		#region Private Static Variables

		private Dictionary<MojoShader.MOJOSHADER_symbolType, EffectParameterType> XNAType =
			new Dictionary<MojoShader.MOJOSHADER_symbolType, EffectParameterType>()
		{
			{
				MojoShader.MOJOSHADER_symbolType.MOJOSHADER_SYMTYPE_VOID,
				EffectParameterType.Void
			},
			{
				MojoShader.MOJOSHADER_symbolType.MOJOSHADER_SYMTYPE_BOOL,
				EffectParameterType.Bool
			},
			{
				MojoShader.MOJOSHADER_symbolType.MOJOSHADER_SYMTYPE_INT,
				EffectParameterType.Int32
			},
			{
				MojoShader.MOJOSHADER_symbolType.MOJOSHADER_SYMTYPE_FLOAT,
				EffectParameterType.Single
			},
			{
				MojoShader.MOJOSHADER_symbolType.MOJOSHADER_SYMTYPE_STRING,
				EffectParameterType.String
			},
			{
				MojoShader.MOJOSHADER_symbolType.MOJOSHADER_SYMTYPE_TEXTURE,
				EffectParameterType.Texture
			},
			{
				MojoShader.MOJOSHADER_symbolType.MOJOSHADER_SYMTYPE_TEXTURE1D,
				EffectParameterType.Texture1D
			},
			{
				MojoShader.MOJOSHADER_symbolType.MOJOSHADER_SYMTYPE_TEXTURE2D,
				EffectParameterType.Texture2D
			},
			{
				MojoShader.MOJOSHADER_symbolType.MOJOSHADER_SYMTYPE_TEXTURE3D,
				EffectParameterType.Texture3D
			},
			{
				MojoShader.MOJOSHADER_symbolType.MOJOSHADER_SYMTYPE_TEXTURECUBE,
				EffectParameterType.TextureCube
			},
		};

		private static Dictionary<MojoShader.MOJOSHADER_symbolClass, EffectParameterClass> XNAClass =
			new Dictionary<MojoShader.MOJOSHADER_symbolClass, EffectParameterClass>()
		{
			{
				MojoShader.MOJOSHADER_symbolClass.MOJOSHADER_SYMCLASS_SCALAR,
				EffectParameterClass.Scalar
			},
			{
				MojoShader.MOJOSHADER_symbolClass.MOJOSHADER_SYMCLASS_VECTOR,
				EffectParameterClass.Vector
			},
			{
				MojoShader.MOJOSHADER_symbolClass.MOJOSHADER_SYMCLASS_MATRIX_ROWS,
				EffectParameterClass.Matrix
			},
			{
				MojoShader.MOJOSHADER_symbolClass.MOJOSHADER_SYMCLASS_MATRIX_COLUMNS,
				EffectParameterClass.Matrix
			},
			{
				MojoShader.MOJOSHADER_symbolClass.MOJOSHADER_SYMCLASS_OBJECT,
				EffectParameterClass.Object
			},
			{
				MojoShader.MOJOSHADER_symbolClass.MOJOSHADER_SYMCLASS_STRUCT,
				EffectParameterClass.Struct
			}
		};

		private static readonly Dictionary<MojoShader.MOJOSHADER_blendMode, Blend> XNABlend =
			new Dictionary<MojoShader.MOJOSHADER_blendMode, Blend>()
		{
			{ MojoShader.MOJOSHADER_blendMode.MOJOSHADER_BLEND_ZERO,		Blend.Zero },
			{ MojoShader.MOJOSHADER_blendMode.MOJOSHADER_BLEND_ONE,			Blend.One },
			{ MojoShader.MOJOSHADER_blendMode.MOJOSHADER_BLEND_SRCCOLOR,		Blend.SourceColor },
			{ MojoShader.MOJOSHADER_blendMode.MOJOSHADER_BLEND_INVSRCCOLOR,		Blend.InverseSourceColor },
			{ MojoShader.MOJOSHADER_blendMode.MOJOSHADER_BLEND_SRCALPHA,		Blend.SourceAlpha },
			{ MojoShader.MOJOSHADER_blendMode.MOJOSHADER_BLEND_INVSRCALPHA,		Blend.InverseSourceAlpha },
			{ MojoShader.MOJOSHADER_blendMode.MOJOSHADER_BLEND_DESTALPHA,		Blend.DestinationAlpha },
			{ MojoShader.MOJOSHADER_blendMode.MOJOSHADER_BLEND_INVDESTALPHA,	Blend.InverseDestinationAlpha },
			{ MojoShader.MOJOSHADER_blendMode.MOJOSHADER_BLEND_DESTCOLOR,		Blend.DestinationColor },
			{ MojoShader.MOJOSHADER_blendMode.MOJOSHADER_BLEND_INVDESTCOLOR,	Blend.InverseDestinationColor },
			{ MojoShader.MOJOSHADER_blendMode.MOJOSHADER_BLEND_SRCALPHASAT,		Blend.SourceAlphaSaturation },
			{ MojoShader.MOJOSHADER_blendMode.MOJOSHADER_BLEND_BLENDFACTOR,		Blend.BlendFactor },
			{ MojoShader.MOJOSHADER_blendMode.MOJOSHADER_BLEND_INVBLENDFACTOR,	Blend.InverseBlendFactor },
		};

		private static readonly Dictionary<MojoShader.MOJOSHADER_blendOp, BlendFunction> XNABlendOp =
			new Dictionary<MojoShader.MOJOSHADER_blendOp, BlendFunction>()
		{
			{ MojoShader.MOJOSHADER_blendOp.MOJOSHADER_BLENDOP_ADD,		BlendFunction.Add },
			{ MojoShader.MOJOSHADER_blendOp.MOJOSHADER_BLENDOP_SUBTRACT,	BlendFunction.Subtract },
			{ MojoShader.MOJOSHADER_blendOp.MOJOSHADER_BLENDOP_REVSUBTRACT,	BlendFunction.ReverseSubtract },
			{ MojoShader.MOJOSHADER_blendOp.MOJOSHADER_BLENDOP_MIN,		BlendFunction.Min },
			{ MojoShader.MOJOSHADER_blendOp.MOJOSHADER_BLENDOP_MAX,		BlendFunction.Max }
		};

		private static readonly Dictionary<MojoShader.MOJOSHADER_compareFunc, CompareFunction> XNACompare =
			new Dictionary<MojoShader.MOJOSHADER_compareFunc, CompareFunction>()
		{
			{ MojoShader.MOJOSHADER_compareFunc.MOJOSHADER_CMP_NEVER,		CompareFunction.Never },
			{ MojoShader.MOJOSHADER_compareFunc.MOJOSHADER_CMP_LESS,		CompareFunction.Less },
			{ MojoShader.MOJOSHADER_compareFunc.MOJOSHADER_CMP_EQUAL,		CompareFunction.Equal },
			{ MojoShader.MOJOSHADER_compareFunc.MOJOSHADER_CMP_LESSEQUAL,		CompareFunction.LessEqual },
			{ MojoShader.MOJOSHADER_compareFunc.MOJOSHADER_CMP_GREATER,		CompareFunction.Greater },
			{ MojoShader.MOJOSHADER_compareFunc.MOJOSHADER_CMP_NOTEQUAL,		CompareFunction.NotEqual },
			{ MojoShader.MOJOSHADER_compareFunc.MOJOSHADER_CMP_GREATEREQUAL,	CompareFunction.GreaterEqual },
			{ MojoShader.MOJOSHADER_compareFunc.MOJOSHADER_CMP_ALWAYS,		CompareFunction.Always }
		};

		private static readonly Dictionary<MojoShader.MOJOSHADER_stencilOp, StencilOperation> XNAStencilOp =
			new Dictionary<MojoShader.MOJOSHADER_stencilOp, StencilOperation>()
		{
			{ MojoShader.MOJOSHADER_stencilOp.MOJOSHADER_STENCILOP_KEEP,	StencilOperation.Keep },
			{ MojoShader.MOJOSHADER_stencilOp.MOJOSHADER_STENCILOP_ZERO,	StencilOperation.Zero },
			{ MojoShader.MOJOSHADER_stencilOp.MOJOSHADER_STENCILOP_REPLACE,	StencilOperation.Replace },
			{ MojoShader.MOJOSHADER_stencilOp.MOJOSHADER_STENCILOP_INCRSAT,	StencilOperation.IncrementSaturation },
			{ MojoShader.MOJOSHADER_stencilOp.MOJOSHADER_STENCILOP_DECRSAT,	StencilOperation.DecrementSaturation },
			{ MojoShader.MOJOSHADER_stencilOp.MOJOSHADER_STENCILOP_INVERT,	StencilOperation.Invert },
			{ MojoShader.MOJOSHADER_stencilOp.MOJOSHADER_STENCILOP_INCR,	StencilOperation.Increment },
			{ MojoShader.MOJOSHADER_stencilOp.MOJOSHADER_STENCILOP_DECR,	StencilOperation.Decrement }
		};

		private static readonly Dictionary<MojoShader.MOJOSHADER_textureAddress, TextureAddressMode> XNAAddress =
			new Dictionary<MojoShader.MOJOSHADER_textureAddress, TextureAddressMode>()
		{
			{ MojoShader.MOJOSHADER_textureAddress.MOJOSHADER_TADDRESS_WRAP,	TextureAddressMode.Wrap },
			{ MojoShader.MOJOSHADER_textureAddress.MOJOSHADER_TADDRESS_MIRROR,	TextureAddressMode.Mirror },
			{ MojoShader.MOJOSHADER_textureAddress.MOJOSHADER_TADDRESS_CLAMP,	TextureAddressMode.Clamp }
		};

		private static readonly Dictionary<TextureFilter, MojoShader.MOJOSHADER_textureFilterType> XNAMag =
			new Dictionary<TextureFilter, MojoShader.MOJOSHADER_textureFilterType>()
		{
			{ TextureFilter.Linear,				MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_LINEAR },
			{ TextureFilter.Point,				MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_POINT },
			{ TextureFilter.Anisotropic,			MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_ANISOTROPIC },
			{ TextureFilter.LinearMipPoint,			MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_LINEAR },
			{ TextureFilter.PointMipLinear,			MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_POINT },
			{ TextureFilter.MinLinearMagPointMipLinear,	MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_POINT },
			{ TextureFilter.MinLinearMagPointMipPoint,	MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_POINT },
			{ TextureFilter.MinPointMagLinearMipLinear,	MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_LINEAR },
			{ TextureFilter.MinPointMagLinearMipPoint,	MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_LINEAR }
		};

		private static readonly Dictionary<TextureFilter, MojoShader.MOJOSHADER_textureFilterType> XNAMin =
			new Dictionary<TextureFilter, MojoShader.MOJOSHADER_textureFilterType>()
		{
			{ TextureFilter.Linear,				MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_LINEAR },
			{ TextureFilter.Point,				MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_POINT },
			{ TextureFilter.Anisotropic,			MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_ANISOTROPIC },
			{ TextureFilter.LinearMipPoint,			MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_LINEAR },
			{ TextureFilter.PointMipLinear,			MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_POINT },
			{ TextureFilter.MinLinearMagPointMipLinear,	MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_LINEAR },
			{ TextureFilter.MinLinearMagPointMipPoint,	MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_LINEAR },
			{ TextureFilter.MinPointMagLinearMipLinear,	MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_POINT },
			{ TextureFilter.MinPointMagLinearMipPoint,	MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_POINT }
		};

		private static readonly Dictionary<TextureFilter, MojoShader.MOJOSHADER_textureFilterType> XNAMip =
			new Dictionary<TextureFilter, MojoShader.MOJOSHADER_textureFilterType>()
		{
			{ TextureFilter.Linear,				MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_LINEAR },
			{ TextureFilter.Point,				MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_POINT },
			{ TextureFilter.Anisotropic,			MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_ANISOTROPIC },
			{ TextureFilter.LinearMipPoint,			MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_POINT },
			{ TextureFilter.PointMipLinear,			MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_LINEAR },
			{ TextureFilter.MinLinearMagPointMipLinear,	MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_LINEAR },
			{ TextureFilter.MinLinearMagPointMipPoint,	MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_POINT },
			{ TextureFilter.MinPointMagLinearMipLinear,	MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_LINEAR },
			{ TextureFilter.MinPointMagLinearMipPoint,	MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_POINT }
		};

		#endregion

		#region Public Constructor

		public Effect(GraphicsDevice graphicsDevice, byte[] effectCode)
		{
			GraphicsDevice = graphicsDevice;

			// Send the blob to the GLDevice to be parsed/compiled
			glEffect = graphicsDevice.GLDevice.CreateEffect(effectCode);

			// This is where it gets ugly...
			INTERNAL_parseEffectStruct();

			// The default technique is the first technique.
			CurrentTechnique = Techniques[0];
		}

		#endregion

		#region Protected Constructor

		protected Effect(Effect cloneSource)
		{
			GraphicsDevice = cloneSource.GraphicsDevice;

			// Send the parsed data to be cloned and recompiled by MojoShader
			glEffect = GraphicsDevice.GLDevice.CloneEffect(
				cloneSource.glEffect
			);

			// Double the ugly, double the fun!
			INTERNAL_parseEffectStruct();

			// The default technique is whatever the current technique was.
			for (int i = 0; i < cloneSource.Techniques.Count; i += 1)
			{
				if (cloneSource.Techniques[i] == cloneSource.CurrentTechnique)
				{
					CurrentTechnique = Techniques[i];
				}
			}
		}

		#endregion

		#region Public Methods

		public virtual Effect Clone()
		{
			return new Effect(this);
		}

		#endregion

		#region Protected Methods

		protected override void Dispose(bool disposing)
		{
			if (!IsDisposed && disposing)
			{
				GraphicsDevice.GLDevice.AddDisposeEffect(glEffect);
			}
			base.Dispose(disposing);
		}

		protected internal virtual void OnApply()
		{
		}

		#endregion

		#region Internal Methods

		internal unsafe void INTERNAL_applyEffect(uint pass)
		{
			GraphicsDevice.GLDevice.ApplyEffect(
				glEffect,
				CurrentTechnique.TechniquePointer,
				pass,
				ref stateChanges
			);
			/* FIXME: Does this actually affect the XNA variables?
			 * There's a chance that the D3DXEffect calls do this
			 * behind XNA's back, even.
			 * -flibit
			 */
			if (stateChanges.render_state_change_count > 0)
			{
				// Used to avoid redundant device state application
				bool blendStateChanged = false;
				bool depthStencilStateChanged = false;
				bool rasterizerStateChanged = false;

				/* We're going to store these states locally, then generate a
				 * new object later if needed. Otherwise the GC loses its mind.
				 * -flibit
				 */
				BlendState oldBlendState = GraphicsDevice.BlendState;
				DepthStencilState oldDepthStencilState = GraphicsDevice.DepthStencilState;
				RasterizerState oldRasterizerState = GraphicsDevice.RasterizerState;

				// Current blend state
				BlendFunction alphaBlendFunction = oldBlendState.AlphaBlendFunction;
				Blend alphaDestinationBlend = oldBlendState.AlphaDestinationBlend;
				Blend alphaSourceBlend = oldBlendState.AlphaSourceBlend;
				BlendFunction colorBlendFunction = oldBlendState.ColorBlendFunction;
				Blend colorDestinationBlend = oldBlendState.ColorDestinationBlend;
				Blend colorSourceBlend = oldBlendState.ColorSourceBlend;
				ColorWriteChannels colorWriteChannels = oldBlendState.ColorWriteChannels;
				ColorWriteChannels colorWriteChannels1 = oldBlendState.ColorWriteChannels1;
				ColorWriteChannels colorWriteChannels2 = oldBlendState.ColorWriteChannels2;
				ColorWriteChannels colorWriteChannels3 = oldBlendState.ColorWriteChannels3;
				Color blendFactor = oldBlendState.BlendFactor;
				int multiSampleMask = oldBlendState.MultiSampleMask;
				/* FIXME: Do we actually care about this calculation, or do we
				 * just assume false each time?
				 * -flibit
				 */
				bool separateAlphaBlend = (
					colorBlendFunction != alphaBlendFunction ||
					colorDestinationBlend != alphaDestinationBlend
				);

				// Current depth/stencil state
				bool depthBufferEnable = oldDepthStencilState.DepthBufferEnable;
				bool depthBufferWriteEnable = oldDepthStencilState.DepthBufferWriteEnable;
				CompareFunction depthBufferFunction = oldDepthStencilState.DepthBufferFunction;
				bool stencilEnable = oldDepthStencilState.StencilEnable;
				CompareFunction stencilFunction = oldDepthStencilState.StencilFunction;
				StencilOperation stencilPass = oldDepthStencilState.StencilPass;
				StencilOperation stencilFail = oldDepthStencilState.StencilFail;
				StencilOperation stencilDepthBufferFail = oldDepthStencilState.StencilDepthBufferFail;
				bool twoSidedStencilMode = oldDepthStencilState.TwoSidedStencilMode;
				CompareFunction ccwStencilFunction = oldDepthStencilState.CounterClockwiseStencilFunction;
				StencilOperation ccwStencilFail = oldDepthStencilState.CounterClockwiseStencilFail;
				StencilOperation ccwStencilPass = oldDepthStencilState.CounterClockwiseStencilPass;
				StencilOperation ccwStencilDepthBufferFail = oldDepthStencilState.CounterClockwiseStencilDepthBufferFail;
				int stencilMask = oldDepthStencilState.StencilMask;
				int stencilWriteMask = oldDepthStencilState.StencilWriteMask;
				int referenceStencil = oldDepthStencilState.ReferenceStencil;

				// Current rasterizer state
				CullMode cullMode = oldRasterizerState.CullMode;
				FillMode fillMode = oldRasterizerState.FillMode;
				float depthBias = oldRasterizerState.DepthBias;
				bool multiSampleAntiAlias = oldRasterizerState.MultiSampleAntiAlias;
				bool scissorTestEnable = oldRasterizerState.ScissorTestEnable;
				float slopeScaleDepthBias = oldRasterizerState.SlopeScaleDepthBias;

				MojoShader.MOJOSHADER_effectState* states = (MojoShader.MOJOSHADER_effectState*) stateChanges.render_state_changes;
				for (int i = 0; i < stateChanges.render_state_change_count; i += 1)
				{
					MojoShader.MOJOSHADER_renderStateType type = states[i].type;
					if (type == MojoShader.MOJOSHADER_renderStateType.MOJOSHADER_RS_ZENABLE)
					{
						MojoShader.MOJOSHADER_zBufferType* val = (MojoShader.MOJOSHADER_zBufferType*) states[i].value.values;
						depthBufferEnable =
							(*val == MojoShader.MOJOSHADER_zBufferType.MOJOSHADER_ZB_TRUE) ?
								true : false;
						depthStencilStateChanged = true;
					}
					else if (type == MojoShader.MOJOSHADER_renderStateType.MOJOSHADER_RS_FILLMODE)
					{
						MojoShader.MOJOSHADER_fillMode* val = (MojoShader.MOJOSHADER_fillMode*) states[i].value.values;
						if (*val == MojoShader.MOJOSHADER_fillMode.MOJOSHADER_FILL_SOLID)
						{
							fillMode = FillMode.Solid;
						}
						else if (*val == MojoShader.MOJOSHADER_fillMode.MOJOSHADER_FILL_WIREFRAME)
						{
							fillMode = FillMode.WireFrame;
						}
						rasterizerStateChanged = true;
					}
					else if (type == MojoShader.MOJOSHADER_renderStateType.MOJOSHADER_RS_ZWRITEENABLE)
					{
						int* val = (int*) states[i].value.values;
						depthBufferWriteEnable = (*val == 1) ? true : false;
						depthStencilStateChanged = true;
					}
					else if (type == MojoShader.MOJOSHADER_renderStateType.MOJOSHADER_RS_SRCBLEND)
					{
						MojoShader.MOJOSHADER_blendMode* val = (MojoShader.MOJOSHADER_blendMode*) states[i].value.values;
						colorSourceBlend = XNABlend[*val];
						if (!separateAlphaBlend)
						{
							alphaSourceBlend = XNABlend[*val];
						}
						blendStateChanged = true;
					}
					else if (type == MojoShader.MOJOSHADER_renderStateType.MOJOSHADER_RS_DESTBLEND)
					{
						MojoShader.MOJOSHADER_blendMode* val = (MojoShader.MOJOSHADER_blendMode*) states[i].value.values;
						colorDestinationBlend = XNABlend[*val];
						if (!separateAlphaBlend)
						{
							alphaDestinationBlend = XNABlend[*val];
						}
						blendStateChanged = true;
					}
					else if (type == MojoShader.MOJOSHADER_renderStateType.MOJOSHADER_RS_CULLMODE)
					{
						MojoShader.MOJOSHADER_cullMode* val = (MojoShader.MOJOSHADER_cullMode*) states[i].value.values;
						if (*val == MojoShader.MOJOSHADER_cullMode.MOJOSHADER_CULL_NONE)
						{
							cullMode = CullMode.None;
						}
						else if (*val == MojoShader.MOJOSHADER_cullMode.MOJOSHADER_CULL_CW)
						{
							cullMode = CullMode.CullClockwiseFace;
						}
						else if (*val == MojoShader.MOJOSHADER_cullMode.MOJOSHADER_CULL_CCW)
						{
							cullMode = CullMode.CullCounterClockwiseFace;
						}
						rasterizerStateChanged = true;
					}
					else if (type == MojoShader.MOJOSHADER_renderStateType.MOJOSHADER_RS_ZFUNC)
					{
						MojoShader.MOJOSHADER_compareFunc* val = (MojoShader.MOJOSHADER_compareFunc*) states[i].value.values;
						depthBufferFunction = XNACompare[*val];
						depthStencilStateChanged = true;
					}
					else if (type == MojoShader.MOJOSHADER_renderStateType.MOJOSHADER_RS_ALPHABLENDENABLE)
					{
						// FIXME: Assuming no other blend calls are made in the effect! -flibit
						int* val = (int*) states[i].value.values;
						if (*val == 0)
						{
							colorSourceBlend = Blend.One;
							colorDestinationBlend = Blend.Zero;
							alphaSourceBlend = Blend.One;
							alphaDestinationBlend = Blend.Zero;
							blendStateChanged = true;
						}
					}
					else if (type == MojoShader.MOJOSHADER_renderStateType.MOJOSHADER_RS_STENCILENABLE)
					{
						int* val = (int*) states[i].value.values;
						stencilEnable = (*val == 1) ? true : false;
						depthStencilStateChanged = true;
					}
					else if (type == MojoShader.MOJOSHADER_renderStateType.MOJOSHADER_RS_STENCILFAIL)
					{
						MojoShader.MOJOSHADER_stencilOp* val = (MojoShader.MOJOSHADER_stencilOp*) states[i].value.values;
						stencilFail = XNAStencilOp[*val];
						depthStencilStateChanged = true;
					}
					else if (type == MojoShader.MOJOSHADER_renderStateType.MOJOSHADER_RS_STENCILZFAIL)
					{
						MojoShader.MOJOSHADER_stencilOp* val = (MojoShader.MOJOSHADER_stencilOp*) states[i].value.values;
						stencilDepthBufferFail = XNAStencilOp[*val];
						depthStencilStateChanged = true;
					}
					else if (type == MojoShader.MOJOSHADER_renderStateType.MOJOSHADER_RS_STENCILPASS)
					{
						MojoShader.MOJOSHADER_stencilOp* val = (MojoShader.MOJOSHADER_stencilOp*) states[i].value.values;
						stencilPass = XNAStencilOp[*val];
						depthStencilStateChanged = true;
					}
					else if (type == MojoShader.MOJOSHADER_renderStateType.MOJOSHADER_RS_STENCILFUNC)
					{
						MojoShader.MOJOSHADER_compareFunc* val = (MojoShader.MOJOSHADER_compareFunc*) states[i].value.values;
						stencilFunction = XNACompare[*val];
						depthStencilStateChanged = true;
					}
					else if (type == MojoShader.MOJOSHADER_renderStateType.MOJOSHADER_RS_STENCILREF)
					{
						int* val = (int*) states[i].value.values;
						referenceStencil = *val;
						depthStencilStateChanged = true;
					}
					else if (type == MojoShader.MOJOSHADER_renderStateType.MOJOSHADER_RS_STENCILMASK)
					{
						int* val = (int*) states[i].value.values;
						stencilMask = *val;
						depthStencilStateChanged = true;
					}
					else if (type == MojoShader.MOJOSHADER_renderStateType.MOJOSHADER_RS_STENCILWRITEMASK)
					{
						int* val = (int*) states[i].value.values;
						stencilWriteMask = *val;
						depthStencilStateChanged = true;
					}
					else if (type == MojoShader.MOJOSHADER_renderStateType.MOJOSHADER_RS_MULTISAMPLEANTIALIAS)
					{
						int* val = (int*) states[i].value.values;
						multiSampleAntiAlias = (*val == 1) ? true : false;
						rasterizerStateChanged = true;
					}
					else if (type == MojoShader.MOJOSHADER_renderStateType.MOJOSHADER_RS_MULTISAMPLEMASK)
					{
						int* val = (int*) states[i].value.values;
						multiSampleMask = *val;
						blendStateChanged = true;
					}
					else if (type == MojoShader.MOJOSHADER_renderStateType.MOJOSHADER_RS_COLORWRITEENABLE)
					{
						int* val = (int*) states[i].value.values;
						colorWriteChannels = (ColorWriteChannels) (*val);
						blendStateChanged = true;
					}
					else if (type == MojoShader.MOJOSHADER_renderStateType.MOJOSHADER_RS_BLENDOP)
					{
						MojoShader.MOJOSHADER_blendOp* val = (MojoShader.MOJOSHADER_blendOp*) states[i].value.values;
						colorBlendFunction = XNABlendOp[*val];
						blendStateChanged = true;
					}
					else if (type == MojoShader.MOJOSHADER_renderStateType.MOJOSHADER_RS_SCISSORTESTENABLE)
					{
						int* val = (int*) states[i].value.values;
						scissorTestEnable = (*val == 1) ? true : false;
						rasterizerStateChanged = true;
					}
					else if (type == MojoShader.MOJOSHADER_renderStateType.MOJOSHADER_RS_SLOPESCALEDEPTHBIAS)
					{
						float* val = (float*) states[i].value.values;
						slopeScaleDepthBias = *val;
						rasterizerStateChanged = true;
					}
					else if (type == MojoShader.MOJOSHADER_renderStateType.MOJOSHADER_RS_TWOSIDEDSTENCILMODE)
					{
						int* val = (int*) states[i].value.values;
						twoSidedStencilMode = (*val == 1) ? true : false;
						depthStencilStateChanged = true;
					}
					else if (type == MojoShader.MOJOSHADER_renderStateType.MOJOSHADER_RS_CCW_STENCILFAIL)
					{
						MojoShader.MOJOSHADER_stencilOp* val = (MojoShader.MOJOSHADER_stencilOp*) states[i].value.values;
						ccwStencilFail = XNAStencilOp[*val];
						depthStencilStateChanged = true;
					}
					else if (type == MojoShader.MOJOSHADER_renderStateType.MOJOSHADER_RS_CCW_STENCILZFAIL)
					{
						MojoShader.MOJOSHADER_stencilOp* val = (MojoShader.MOJOSHADER_stencilOp*) states[i].value.values;
						ccwStencilDepthBufferFail = XNAStencilOp[*val];
						depthStencilStateChanged = true;
					}
					else if (type == MojoShader.MOJOSHADER_renderStateType.MOJOSHADER_RS_CCW_STENCILPASS)
					{
						MojoShader.MOJOSHADER_stencilOp* val = (MojoShader.MOJOSHADER_stencilOp*) states[i].value.values;
						ccwStencilPass = XNAStencilOp[*val];
						depthStencilStateChanged = true;
					}
					else if (type == MojoShader.MOJOSHADER_renderStateType.MOJOSHADER_RS_CCW_STENCILFUNC)
					{
						MojoShader.MOJOSHADER_compareFunc* val = (MojoShader.MOJOSHADER_compareFunc*) states[i].value.values;
						ccwStencilFunction = XNACompare[*val];
						depthStencilStateChanged = true;
					}
					else if (type == MojoShader.MOJOSHADER_renderStateType.MOJOSHADER_RS_COLORWRITEENABLE1)
					{
						int* val = (int*) states[i].value.values;
						colorWriteChannels1 = (ColorWriteChannels) (*val);
						blendStateChanged = true;
					}
					else if (type == MojoShader.MOJOSHADER_renderStateType.MOJOSHADER_RS_COLORWRITEENABLE2)
					{
						int* val = (int*) states[i].value.values;
						colorWriteChannels2 = (ColorWriteChannels) (*val);
						blendStateChanged = true;
					}
					else if (type == MojoShader.MOJOSHADER_renderStateType.MOJOSHADER_RS_COLORWRITEENABLE3)
					{
						int* val = (int*) states[i].value.values;
						colorWriteChannels3 = (ColorWriteChannels) (*val);
						blendStateChanged = true;
					}
					else if (type == MojoShader.MOJOSHADER_renderStateType.MOJOSHADER_RS_BLENDFACTOR)
					{
						// FIXME: RGBA? -flibit
						int* val = (int*) states[i].value.values;
						blendFactor = new Color(
							(*val >> 24) & 0xFF,
							(*val >> 16) & 0xFF,
							(*val >> 8) & 0xFF,
							*val & 0xFF
						);
						blendStateChanged = true;
					}
					else if (type == MojoShader.MOJOSHADER_renderStateType.MOJOSHADER_RS_DEPTHBIAS)
					{
						float* val = (float*) states[i].value.values;
						depthBias = *val;
						rasterizerStateChanged = true;
					}
					else if (type == MojoShader.MOJOSHADER_renderStateType.MOJOSHADER_RS_SEPARATEALPHABLENDENABLE)
					{
						int* val = (int*) states[i].value.values;
						separateAlphaBlend = (*val == 1);
						// FIXME: Do we want to update the state for this...? -flibit
					}
					else if (type == MojoShader.MOJOSHADER_renderStateType.MOJOSHADER_RS_SRCBLENDALPHA)
					{
						MojoShader.MOJOSHADER_blendMode* val = (MojoShader.MOJOSHADER_blendMode*) states[i].value.values;
						alphaSourceBlend = XNABlend[*val];
						blendStateChanged = true;
					}
					else if (type == MojoShader.MOJOSHADER_renderStateType.MOJOSHADER_RS_DESTBLENDALPHA)
					{
						MojoShader.MOJOSHADER_blendMode* val = (MojoShader.MOJOSHADER_blendMode*) states[i].value.values;
						alphaDestinationBlend = XNABlend[*val];
						blendStateChanged = true;
					}
					else if (type == MojoShader.MOJOSHADER_renderStateType.MOJOSHADER_RS_BLENDOPALPHA)
					{
						MojoShader.MOJOSHADER_blendOp* val = (MojoShader.MOJOSHADER_blendOp*) states[i].value.values;
						alphaBlendFunction = XNABlendOp[*val];
						blendStateChanged = true;
					}
					else if (	type == MojoShader.MOJOSHADER_renderStateType.MOJOSHADER_RS_VERTEXSHADER ||
							type == MojoShader.MOJOSHADER_renderStateType.MOJOSHADER_RS_PIXELSHADER	)
					{
						// Skip shader states
						continue;
					}
					else
					{
						throw new Exception("Unhandled render state!");
					}
				}
				if (blendStateChanged)
				{
					// FIXME: This is part of the state cache hack! -flibit
					BlendState newBlend;
					if (GraphicsDevice.BlendState == blendCache[0])
					{
						newBlend = blendCache[1];
					}
					else
					{
						newBlend = blendCache[0];
					}
					newBlend.AlphaBlendFunction = alphaBlendFunction;
					newBlend.AlphaDestinationBlend = alphaDestinationBlend;
					newBlend.AlphaSourceBlend = alphaSourceBlend;
					newBlend.ColorBlendFunction = colorBlendFunction;
					newBlend.ColorDestinationBlend = colorDestinationBlend;
					newBlend.ColorSourceBlend = colorSourceBlend;
					newBlend.ColorWriteChannels = colorWriteChannels;
					newBlend.ColorWriteChannels1 = colorWriteChannels1;
					newBlend.ColorWriteChannels2 = colorWriteChannels2;
					newBlend.ColorWriteChannels3 = colorWriteChannels3;
					newBlend.BlendFactor = blendFactor;
					newBlend.MultiSampleMask = multiSampleMask;
					GraphicsDevice.BlendState = newBlend;
				}
				if (depthStencilStateChanged)
				{
					// FIXME: This is part of the state cache hack! -flibit
					DepthStencilState newDepthStencil;
					if (GraphicsDevice.DepthStencilState == depthStencilCache[0])
					{
						newDepthStencil = depthStencilCache[1];
					}
					else
					{
						newDepthStencil = depthStencilCache[0];
					}
					newDepthStencil.DepthBufferEnable = depthBufferEnable;
					newDepthStencil.DepthBufferWriteEnable = depthBufferWriteEnable;
					newDepthStencil.DepthBufferFunction = depthBufferFunction;
					newDepthStencil.StencilEnable = stencilEnable;
					newDepthStencil.StencilFunction = stencilFunction;
					newDepthStencil.StencilPass = stencilPass;
					newDepthStencil.StencilFail = stencilFail;
					newDepthStencil.StencilDepthBufferFail = stencilDepthBufferFail;
					newDepthStencil.TwoSidedStencilMode = twoSidedStencilMode;
					newDepthStencil.CounterClockwiseStencilFunction = ccwStencilFunction;
					newDepthStencil.CounterClockwiseStencilFail = ccwStencilFail;
					newDepthStencil.CounterClockwiseStencilPass = ccwStencilPass;
					newDepthStencil.CounterClockwiseStencilDepthBufferFail = ccwStencilDepthBufferFail;
					newDepthStencil.StencilMask = stencilMask;
					newDepthStencil.StencilWriteMask = stencilWriteMask;
					newDepthStencil.ReferenceStencil = referenceStencil;
					GraphicsDevice.DepthStencilState = newDepthStencil;
				}
				if (rasterizerStateChanged)
				{
					// FIXME: This is part of the state cache hack! -flibit
					RasterizerState newRasterizer;
					if (GraphicsDevice.RasterizerState == rasterizerCache[0])
					{
						newRasterizer = rasterizerCache[1];
					}
					else
					{
						newRasterizer = rasterizerCache[0];
					}
					newRasterizer.CullMode = cullMode;
					newRasterizer.FillMode = fillMode;
					newRasterizer.DepthBias = depthBias;
					newRasterizer.MultiSampleAntiAlias = multiSampleAntiAlias;
					newRasterizer.ScissorTestEnable = scissorTestEnable;
					newRasterizer.SlopeScaleDepthBias = slopeScaleDepthBias;
					GraphicsDevice.RasterizerState = newRasterizer;
				}
			}
			if (stateChanges.sampler_state_change_count > 0)
			{
				MojoShader.MOJOSHADER_samplerStateRegister* registers = (MojoShader.MOJOSHADER_samplerStateRegister*) stateChanges.sampler_state_changes;
				for (int i = 0; i < stateChanges.sampler_state_change_count; i += 1)
				{
					if (registers[i].sampler_state_count == 0)
					{
						// Nothing to do
						continue;
					}
					int register = (int) registers[i].sampler_register;

					/* We're going to store this state locally, then generate a
					 * new object later if needed. Otherwise the GC loses its
					 * mind.
					 * -flibit
					 */
					SamplerState oldSampler = GraphicsDevice.SamplerStates[register];

					// Used to prevent redundant sampler changes
					bool samplerChanged = false;
					bool filterChanged = false;

					// Current sampler state
					TextureAddressMode addressU = oldSampler.AddressU;
					TextureAddressMode addressV = oldSampler.AddressV;
					TextureAddressMode addressW = oldSampler.AddressW;
					int maxAnisotropy = oldSampler.MaxAnisotropy;
					int maxMipLevel = oldSampler.MaxMipLevel;
					float mipMapLODBias = oldSampler.MipMapLevelOfDetailBias;

					// Current sampler filter
					TextureFilter filter = oldSampler.Filter;
					MojoShader.MOJOSHADER_textureFilterType magFilter = XNAMag[filter];
					MojoShader.MOJOSHADER_textureFilterType minFilter = XNAMin[filter];
					MojoShader.MOJOSHADER_textureFilterType mipFilter = XNAMip[filter];

					MojoShader.MOJOSHADER_effectSamplerState* states = (MojoShader.MOJOSHADER_effectSamplerState*) registers[i].sampler_states;
					for (int j = 0; j < registers[i].sampler_state_count; j += 1)
					{
						MojoShader.MOJOSHADER_samplerStateType type = states[j].type;
						if (type == MojoShader.MOJOSHADER_samplerStateType.MOJOSHADER_SAMP_TEXTURE)
						{
							string samplerName = Marshal.PtrToStringAnsi(
								registers[i].sampler_name
							);
							if (samplerMap.ContainsKey(samplerName))
							{
								Texture texture = samplerMap[samplerName].texture;
								if (texture != null)
								{
									GraphicsDevice.Textures[register] = texture;
								}
							}
						}
						else if (type == MojoShader.MOJOSHADER_samplerStateType.MOJOSHADER_SAMP_ADDRESSU)
						{
							MojoShader.MOJOSHADER_textureAddress* val = (MojoShader.MOJOSHADER_textureAddress*) states[j].value.values;
							addressU = XNAAddress[*val];
							samplerChanged = true;
						}
						else if (type == MojoShader.MOJOSHADER_samplerStateType.MOJOSHADER_SAMP_ADDRESSV)
						{
							MojoShader.MOJOSHADER_textureAddress* val = (MojoShader.MOJOSHADER_textureAddress*) states[j].value.values;
							addressV = XNAAddress[*val];
							samplerChanged = true;
						}
						else if (type == MojoShader.MOJOSHADER_samplerStateType.MOJOSHADER_SAMP_ADDRESSW)
						{
							MojoShader.MOJOSHADER_textureAddress* val = (MojoShader.MOJOSHADER_textureAddress*) states[j].value.values;
							addressW = XNAAddress[*val];
							samplerChanged = true;
						}
						else if (type == MojoShader.MOJOSHADER_samplerStateType.MOJOSHADER_SAMP_MAGFILTER)
						{
							MojoShader.MOJOSHADER_textureFilterType* val = (MojoShader.MOJOSHADER_textureFilterType*) states[j].value.values;
							magFilter = *val;
							filterChanged = true;
						}
						else if (type == MojoShader.MOJOSHADER_samplerStateType.MOJOSHADER_SAMP_MINFILTER)
						{
							MojoShader.MOJOSHADER_textureFilterType* val = (MojoShader.MOJOSHADER_textureFilterType*) states[j].value.values;
							minFilter = *val;
							filterChanged = true;
						}
						else if (type == MojoShader.MOJOSHADER_samplerStateType.MOJOSHADER_SAMP_MIPFILTER)
						{
							MojoShader.MOJOSHADER_textureFilterType* val = (MojoShader.MOJOSHADER_textureFilterType*) states[j].value.values;
							mipFilter = *val;
							filterChanged = true;
						}
						else if (type == MojoShader.MOJOSHADER_samplerStateType.MOJOSHADER_SAMP_MIPMAPLODBIAS)
						{
							float* val = (float*) states[i].value.values;
							mipMapLODBias = *val;
							samplerChanged = true;
						}
						else if (type == MojoShader.MOJOSHADER_samplerStateType.MOJOSHADER_SAMP_MAXMIPLEVEL)
						{
							int* val = (int*) states[i].value.values;
							maxMipLevel = *val;
							samplerChanged = true;
						}
						else if (type == MojoShader.MOJOSHADER_samplerStateType.MOJOSHADER_SAMP_MAXANISOTROPY)
						{
							int* val = (int*) states[i].value.values;
							maxAnisotropy = *val;
							samplerChanged = true;
						}
						else
						{
							throw new Exception("Unhandled sampler state!");
						}
					}
					if (filterChanged)
					{
						if (	magFilter == MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_ANISOTROPIC ||
							minFilter == MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_ANISOTROPIC ||
							mipFilter == MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_ANISOTROPIC	)
						{
							// Just assume we wanted Anisotropic if any of these qualify.
							filter = TextureFilter.Anisotropic;
						}
						else if (magFilter == MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_POINT)
						{
							if (minFilter == MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_POINT)
							{
								if (	mipFilter == MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_NONE ||
									mipFilter == MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_POINT	)
								{
									filter = TextureFilter.Point;
								}
								else if (mipFilter == MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_LINEAR)
								{
									filter = TextureFilter.PointMipLinear;
								}
								else
								{
									throw new NotImplementedException("Unhandled mipfilter type!");
								}
							}
							else if (minFilter == MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_LINEAR)
							{
								if (	mipFilter == MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_NONE ||
									mipFilter == MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_POINT	)
								{
									filter = TextureFilter.MinLinearMagPointMipPoint;
								}
								else if (mipFilter == MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_LINEAR)
								{
									filter = TextureFilter.MinLinearMagPointMipLinear;
								}
								else
								{
									throw new NotImplementedException("Unhandled mipfilter type!");
								}
							}
							else
							{
								throw new NotImplementedException("Unhandled minfilter type!");
							}
						}
						else if (magFilter == MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_LINEAR)
						{
							if (minFilter == MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_POINT)
							{
								if (	mipFilter == MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_NONE ||
									mipFilter == MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_POINT	)
								{
									filter = TextureFilter.MinPointMagLinearMipPoint;
								}
								else if (mipFilter == MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_LINEAR)
								{
									filter = TextureFilter.MinPointMagLinearMipLinear;
								}
								else
								{
									throw new NotImplementedException("Unhandled mipfilter type!");
								}
							}
							else if (minFilter == MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_LINEAR)
							{
								if (	mipFilter == MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_NONE ||
									mipFilter == MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_POINT	)
								{
									filter = TextureFilter.LinearMipPoint;
								}
								else if (mipFilter == MojoShader.MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_LINEAR)
								{
									filter = TextureFilter.Linear;
								}
								else
								{
									throw new NotImplementedException("Unhandled mipfilter type!");
								}
							}
							else
							{
								throw new NotImplementedException("Unhandled minfilter type!");
							}
						}
						else
						{
							throw new NotImplementedException("Unhandled magfilter type!");
						}
						samplerChanged = true;
					}

					if (samplerChanged)
					{
						// FIXME: This is part of the state cache hack! -flibit
						SamplerState newSampler;
						if (GraphicsDevice.SamplerStates[register] == samplerCache[register])
						{
							// FIXME: 30 is arbitrary! -flibit
							newSampler = samplerCache[register + 30];
						}
						else
						{
							newSampler = samplerCache[register];
						}
						newSampler.Filter = filter;
						newSampler.AddressU = addressU;
						newSampler.AddressV = addressV;
						newSampler.AddressW = addressW;
						newSampler.MaxAnisotropy = maxAnisotropy;
						newSampler.MaxMipLevel = maxMipLevel;
						newSampler.MipMapLevelOfDetailBias = mipMapLODBias;
						GraphicsDevice.SamplerStates[register] = newSampler;
					}
				}
			}
		}

		#endregion

		#region Private Methods

		private unsafe void INTERNAL_parseEffectStruct()
		{
			MojoShader.MOJOSHADER_effect* effectPtr = (MojoShader.MOJOSHADER_effect*) glEffect.EffectData;

			// Set up Parameters
			MojoShader.MOJOSHADER_effectParam* paramPtr = (MojoShader.MOJOSHADER_effectParam*) effectPtr->parameters;
			List<EffectParameter> parameters = new List<EffectParameter>();
			for (int i = 0; i < effectPtr->param_count; i += 1)
			{
				MojoShader.MOJOSHADER_effectParam param = paramPtr[i];
				if (	param.value.value_type == MojoShader.MOJOSHADER_symbolType.MOJOSHADER_SYMTYPE_VERTEXSHADER ||
					param.value.value_type == MojoShader.MOJOSHADER_symbolType.MOJOSHADER_SYMTYPE_PIXELSHADER	)
				{
					// Skip shader objects...
					continue;
				}
				else if (	param.value.value_type >= MojoShader.MOJOSHADER_symbolType.MOJOSHADER_SYMTYPE_SAMPLER &&
						param.value.value_type <= MojoShader.MOJOSHADER_symbolType.MOJOSHADER_SYMTYPE_SAMPLERCUBE	)
				{
					string textureName = String.Empty;
					MojoShader.MOJOSHADER_effectSamplerState* states = (MojoShader.MOJOSHADER_effectSamplerState*) param.value.values;
					for (int j = 0; j < param.value.value_count; j += 1)
					{
						if (	states[j].value.value_type >= MojoShader.MOJOSHADER_symbolType.MOJOSHADER_SYMTYPE_TEXTURE &&
							states[j].value.value_type <= MojoShader.MOJOSHADER_symbolType.MOJOSHADER_SYMTYPE_TEXTURECUBE	)
						{
							MojoShader.MOJOSHADER_effectObject *objectPtr = (MojoShader.MOJOSHADER_effectObject*) effectPtr->objects;
							int* index = (int*) states[j].value.values;
							textureName = Marshal.PtrToStringAnsi(objectPtr[*index].mapping.name);
							break;
						}
					}
					/* Because textures have to be declared before the sampler,
					 * we can assume that it will always be in the list by the
					 * time we get to this point.
					 * -flibit
					 */
					for (int j = 0; j < parameters.Count; j += 1)
					{
						if (textureName.Equals(parameters[j].Name))
						{
							samplerMap[Marshal.PtrToStringAnsi(param.value.name)] = parameters[j];
							break;
						}
					}
					continue;
				}
				parameters.Add(new EffectParameter(
					Marshal.PtrToStringAnsi(param.value.name),
					Marshal.PtrToStringAnsi(param.value.semantic),
					(int) param.value.row_count,
					(int) param.value.column_count,
					(int) param.value.element_count,
					XNAClass[param.value.value_class],
					XNAType[param.value.value_type],
					null, // FIXME: See mojoshader_effects.c:readvalue -flibit
					INTERNAL_readAnnotations(
						param.annotations,
						param.annotation_count
					),
					param.value.values
				));
			}
			Parameters = new EffectParameterCollection(parameters.ToArray());

			// Set up Techniques
			MojoShader.MOJOSHADER_effectTechnique* techPtr = (MojoShader.MOJOSHADER_effectTechnique*) effectPtr->techniques;
			EffectTechnique[] techniques = new EffectTechnique[effectPtr->technique_count];
			for (int i = 0; i < techniques.Length; i += 1)
			{
				MojoShader.MOJOSHADER_effectTechnique tech = techPtr[i];

				// Set up Passes
				MojoShader.MOJOSHADER_effectPass* passPtr = (MojoShader.MOJOSHADER_effectPass*) tech.passes;
				EffectPass[] passes = new EffectPass[tech.pass_count];
				for (int j = 0; j < passes.Length; j += 1)
				{
					MojoShader.MOJOSHADER_effectPass pass = passPtr[j];
					passes[j] = new EffectPass(
						Marshal.PtrToStringAnsi(pass.name),
						INTERNAL_readAnnotations(
							pass.annotations,
							pass.annotation_count
						),
						this,
						(uint) j
					);
				}

				techniques[i] = new EffectTechnique(
					Marshal.PtrToStringAnsi(tech.name),
					(IntPtr) (techPtr + i),
					new EffectPassCollection(passes),
					INTERNAL_readAnnotations(
						tech.annotations,
						tech.annotation_count
					)
				);
			}
			Techniques = new EffectTechniqueCollection(techniques);
		}

		private unsafe EffectAnnotationCollection INTERNAL_readAnnotations(
			IntPtr rawAnnotations,
			uint numAnnotations
		) {
			MojoShader.MOJOSHADER_effectAnnotation* annoPtr = (MojoShader.MOJOSHADER_effectAnnotation*) rawAnnotations;
			EffectAnnotation[] annotations = new EffectAnnotation[numAnnotations];
			for (int i = 0; i < numAnnotations; i += 1)
			{
				MojoShader.MOJOSHADER_effectAnnotation anno = annoPtr[i];
				annotations[i] = new EffectAnnotation(
					Marshal.PtrToStringAnsi(anno.name),
					Marshal.PtrToStringAnsi(anno.semantic),
					(int) anno.row_count,
					(int) anno.column_count,
					XNAClass[anno.value_class],
					XNAType[anno.value_type],
					anno.values
				);
			}
			return new EffectAnnotationCollection(annotations);
		}

		#endregion
	}
}
