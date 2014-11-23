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
		private MojoShader.MOJOSHADER_effectStateChanges stateChanges;
		private Dictionary<string, EffectParameter> samplerMap = new Dictionary<string, EffectParameter>();

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
			if (!IsDisposed && disposing)
			{
				GraphicsDevice.GLDevice.DeleteEffect(glEffect);
			}
			base.Dispose(disposing);
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
				pass,
				ref stateChanges
			);
			// TODO: stateChanges!
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
