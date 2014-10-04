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
#endregion

namespace Microsoft.Xna.Framework.Graphics
{
	public class Model
	{
		#region Public Properties

		/// <summary>
		/// Gets a collection of ModelBone objects which describe how each mesh in the
		/// Meshes collection for this model relates to its parent mesh.
		/// </summary>
		public ModelBoneCollection Bones
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets a collection of ModelMesh objects which compose the model. Each ModelMesh
		/// in a model may be moved independently and may be composed of multiple materials
		/// identified as ModelMeshPart objects.
		/// </summary>
		public ModelMeshCollection Meshes
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the root bone for this model.
		/// </summary>
		public ModelBone Root
		{
			get;
			internal set;
		}

		/// <summary>
		/// Gets or sets an object identifying this model.
		/// </summary>
		public object Tag
		{
			get;
			set;
		}

		#endregion

		#region Private Static Variables

		private static Matrix[] sharedDrawBoneMatrices;

		#endregion

		#region Public Constructors

		public Model()
		{
		}

		public Model(GraphicsDevice graphicsDevice, List<ModelBone> bones, List<ModelMesh> meshes)
		{
			Bones = new ModelBoneCollection(bones);
			Meshes = new ModelMeshCollection(meshes);
		}

		#endregion

		#region Public Methods

		public void Draw(Matrix world, Matrix view, Matrix projection)
		{
			int boneCount = Bones.Count;

			if (sharedDrawBoneMatrices == null ||
				sharedDrawBoneMatrices.Length < boneCount)
			{
				sharedDrawBoneMatrices = new Matrix[boneCount];
			}

			// Look up combined bone matrices for the entire model.
			CopyAbsoluteBoneTransformsTo(sharedDrawBoneMatrices);

			// Draw the model.
			foreach (ModelMesh mesh in Meshes)
			{
				foreach (Effect effect in mesh.Effects)
				{
					IEffectMatrices effectMatricies = effect as IEffectMatrices;
					if (effectMatricies == null)
					{
						throw new InvalidOperationException();
					}
					effectMatricies.World = sharedDrawBoneMatrices[mesh.ParentBone.Index] * world;
					effectMatricies.View = view;
					effectMatricies.Projection = projection;
				}

				mesh.Draw();
			}
		}

		public void CopyAbsoluteBoneTransformsTo(Matrix[] destinationBoneTransforms)
		{
			if (destinationBoneTransforms == null)
			{
				throw new ArgumentNullException("destinationBoneTransforms");
			}
			if (destinationBoneTransforms.Length < Bones.Count)
			{
				throw new ArgumentOutOfRangeException("destinationBoneTransforms");
			}

			int count = Bones.Count;
			for (int index1 = 0; index1 < count; index1 += 1)
			{
				ModelBone modelBone = Bones[index1];
				if (modelBone.Parent == null)
				{
					destinationBoneTransforms[index1] = modelBone.Transform;
				}
				else
				{
					int index2 = modelBone.Parent.Index;
					Matrix modelBoneTransform = modelBone.Transform;
					Matrix.Multiply(
						ref modelBoneTransform,
						ref destinationBoneTransforms[index2],
						out destinationBoneTransforms[index1]
					);
				}
			}
		}

		#endregion

		#region Internal Methods

		internal void BuildHierarchy()
		{
			Matrix globalScale = Matrix.CreateScale(0.01f);

			foreach (ModelBone node in Root.Children)
			{
				BuildHierarchy(node, Root.Transform * globalScale, 0);
			}
		}

		#endregion

		#region Private Methods

		private void BuildHierarchy(ModelBone node, Matrix parentTransform, int level)
		{
			node.ModelTransform = node.Transform * parentTransform;
			
			foreach (ModelBone child in node.Children)
			{
				BuildHierarchy(child, node.ModelTransform, level + 1);
			}
		}

		#endregion
	}
}
