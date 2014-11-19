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
			{ // FIXME -flibit
				MojoShader.MOJOSHADER_symbolType.MOJOSHADER_SYMTYPE_SAMPLER,
				EffectParameterType.Texture
			}
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
			// FIXME: MojoShader needs MOJOSHADER_effectClone! -flibit
			throw new NotImplementedException("See MojoShader!");
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
			if (disposing)
			{
				GraphicsDevice.GLDevice.DeleteEffect(glEffect);
			}
		}

		protected internal virtual void OnApply()
		{
		}

		#endregion

		#region Internal Methods

		internal void INTERNAL_applyEffect(uint pass)
		{
			GraphicsDevice.GLDevice.ApplyEffect(
				glEffect,
				pass
			);
		}

		#endregion

		#region Private Methods

		private unsafe void INTERNAL_parseEffectStruct()
		{
			MojoShader.MOJOSHADER_effect* effectPtr = (MojoShader.MOJOSHADER_effect*) glEffect.EffectData;

			// Set up Parameters
			MojoShader.MOJOSHADER_effectParam* paramPtr = (MojoShader.MOJOSHADER_effectParam*) effectPtr->parameters;
			EffectParameter[] parameters = new EffectParameter[effectPtr->param_count];
			for (int i = 0; i < parameters.Length; i += 1)
			{
				MojoShader.MOJOSHADER_effectParam param = paramPtr[i];
				parameters[i] = new EffectParameter(
					Marshal.PtrToStringAnsi(param.value.name),
					Marshal.PtrToStringAnsi(param.value.semantic),
					(int) param.value.row_count,
					(int) param.value.column_count,
					XNAClass[param.value.value_class],
					XNAType[param.value.value_type],
					null, // FIXME: See mojoshader_effects.c:readvalue -flibit
					INTERNAL_readAnnotations(
						param.annotations,
						param.annotation_count
					),
					param.value.values
				);
			}
			Parameters = new EffectParameterCollection(parameters);

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
