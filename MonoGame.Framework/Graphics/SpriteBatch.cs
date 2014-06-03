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
using System.Text;
#endregion

namespace Microsoft.Xna.Framework.Graphics
{
    public interface ISpriteBatch
    {
        void Draw(Texture2D texture,
            Vector4 destinationRectangle,
            Rectangle? sourceRectangle,
            Color color,
            float rotation,
            Vector2 origin,
            SpriteEffects effect,
            float depth,
            bool autoFlush);

        void FlushIfNeeded();
    }

	// TODO: Rewrite from scratch using DynamicVertexBuffer/DynamicIndexBuffer. -flibit
    public class SpriteBatch : GraphicsResource, ISpriteBatch
	{
		#region Private Variables

		readonly SpriteBatcher _batcher;

		SpriteSortMode _sortMode;
		BlendState _blendState;
		SamplerState _samplerState;
		DepthStencilState _depthStencilState;
		RasterizerState _rasterizerState;
		Effect _effect;
		bool _beginCalled;

		Effect _spriteEffect;
		readonly EffectParameter _matrixTransform;
		readonly EffectPass _spritePass;

		Matrix _matrix;
		Rectangle _tempRect = new Rectangle(0, 0, 0, 0);
		Vector2 _texCoordTL = new Vector2(0, 0);
		Vector2 _texCoordBR = new Vector2(0, 0);

		#endregion

		#region Public Constructors

		public SpriteBatch(GraphicsDevice graphicsDevice)
		{
			if (graphicsDevice == null)
			{
				throw new ArgumentException("graphicsDevice");
			}

			GraphicsDevice = graphicsDevice;

			// Use a custom SpriteEffect so we can control the transformation matrix
			_spriteEffect = new Effect(graphicsDevice, SpriteEffect.Bytecode);
			_matrixTransform = _spriteEffect.Parameters["MatrixTransform"];
			_spritePass = _spriteEffect.CurrentTechnique.Passes[0];

			_batcher = new SpriteBatcher(graphicsDevice);

			_beginCalled = false;
		}

		#endregion

		#region Public Methods

		public void Begin()
		{
			Begin(
				SpriteSortMode.Deferred,
				BlendState.AlphaBlend,
				SamplerState.LinearClamp,
				DepthStencilState.None,
				RasterizerState.CullCounterClockwise,
				null,
				Matrix.Identity
			);
		}

		public void Begin(
			SpriteSortMode sortMode,
			BlendState blendState
		) {
			Begin(
				sortMode,
				blendState,
				SamplerState.LinearClamp,
				DepthStencilState.None,
				RasterizerState.CullCounterClockwise,
				null,
				Matrix.Identity
			);
		}

		public void Begin(
			SpriteSortMode sortMode,
			BlendState blendState,
			SamplerState samplerState,
			DepthStencilState depthStencilState,
			RasterizerState rasterizerState
		) {
			Begin(
				sortMode,
				blendState,
				samplerState,
				depthStencilState,
				rasterizerState,
				null,
				Matrix.Identity
			);
		}

		public void Begin(
			SpriteSortMode sortMode,
			BlendState blendState,
			SamplerState samplerState,
			DepthStencilState depthStencilState,
			RasterizerState rasterizerState,
			Effect effect
		) {
			Begin(
				sortMode,
				blendState,
				samplerState,
				depthStencilState,
				rasterizerState,
				effect,
				Matrix.Identity
			);
		}

		public void Begin(
			SpriteSortMode sortMode,
			BlendState blendState,
			SamplerState samplerState,
			DepthStencilState depthStencilState,
			RasterizerState rasterizerState,
			Effect effect,
			Matrix transformMatrix
		) {
			if (_beginCalled)
			{
				throw new InvalidOperationException(
					"Begin cannot be called again until End has been successfully called."
				);
			}

			// Defaults
			_sortMode = sortMode;
			_blendState = blendState ?? BlendState.AlphaBlend;
			_samplerState = samplerState ?? SamplerState.LinearClamp;
			_depthStencilState = depthStencilState ?? DepthStencilState.None;
			_rasterizerState = rasterizerState ?? RasterizerState.CullCounterClockwise;

			_effect = effect;

			_matrix = transformMatrix;

			// Setup things now so a user can chage them.
			if (sortMode == SpriteSortMode.Immediate)
			{
				Setup();
			}

			_beginCalled = true;
		}

		public void End()
		{
			_beginCalled = false;

			if (_sortMode != SpriteSortMode.Immediate)
			{
				Setup();
			}

			_batcher.DrawBatch(_sortMode);
		}

		#endregion

		#region Public Draw Methods

		public void Draw(
			Texture2D texture,
			Vector2 position,
			Color color
		) {
			Draw(
				texture,
				position,
				null,
				color
			);
		}

		public void Draw(
			Texture2D texture,
			Rectangle rectangle,
			Color color
		) {
			Draw(
				texture,
				rectangle,
				null,
				color
			);
		}

		public void DrawString(
			SpriteFont spriteFont,
			string text,
			Vector2 position,
			Color color
		) {
			CheckValid(spriteFont, text);

			SpriteFont.CharacterSource source = new SpriteFont.CharacterSource(text);
			spriteFont.DrawInto(
				this,
				ref source,
				position,
				color,
				0,
				Vector2.Zero,
				Vector2.One,
				SpriteEffects.None,
				0f
			);
		}

		public void Draw(
			Texture2D texture,
			Vector2 position,
			Rectangle? sourceRectangle,
			Color color
		) {
			Draw(
				texture,
				position,
				sourceRectangle,
				color,
				0f,
				Vector2.Zero,
				1f,
				SpriteEffects.None,
				0f
			);
		}

		public void Draw(
			Texture2D texture,
			Rectangle destinationRectangle,
			Rectangle? sourceRectangle,
			Color color
		) {
			Draw(
				texture,
				destinationRectangle,
				sourceRectangle,
				color,
				0,
				Vector2.Zero,
				SpriteEffects.None,
				0f
			);
		}

		/// <summary>
		/// This is a MonoGame Extension method for calling Draw() using named parameters.
		/// It is not available in the standard XNA Framework.
		/// </summary>
		/// <param name='texture'>
		/// The Texture2D to draw. Required.
		/// </param>
		/// <param name='position'>
		/// The position to draw at. If left empty, the method will draw at drawRectangle
		/// instead.
		/// </param>
		/// <param name='drawRectangle'>
		/// The rectangle to draw at. If left empty, the method will draw at position instead.
		/// </param>
		/// <param name='sourceRectangle'>
		/// The source rectangle of the texture. Default is null
		/// </param>
		/// <param name='origin'>
		/// Origin of the texture. Default is Vector2.Zero
		/// </param>
		/// <param name='rotation'>
		/// Rotation of the texture. Default is 0f
		/// </param>
		/// <param name='scale'>
		/// The scale of the texture as a Vector2. Default is Vector2.One
		/// </param>
		/// <param name='color'>
		/// Color of the texture. Default is Color.White
		/// </param>
		/// <param name='effect'>
		/// SpriteEffect to draw with. Default is SpriteEffects.None
		/// </param>
		/// <param name='depth'>
		/// Draw depth. Default is 0f.
		/// </param>
		public void Draw(
			Texture2D texture,
			Vector2? position = null,
			Rectangle? drawRectangle = null,
			Rectangle? sourceRectangle = null,
			Vector2? origin = null,
			float rotation = 0f,
			Vector2? scale = null,
			Color? color = null,
			SpriteEffects effect = SpriteEffects.None,
			float depth = 0f
		) {
			// Assign default values to null parameters here, as they are not compile-time constants
			if (!color.HasValue)
			{
				color = Color.White;
			}

			if (!origin.HasValue)
			{
				origin = Vector2.Zero;
			}

			if (!scale.HasValue)
			{
				scale = Vector2.One;
			}

			/* If both drawRectangle and position are null, or if both have been
			 * assigned a value, raise an error
			 */
			if ((drawRectangle.HasValue) == (position.HasValue))
			{
				throw new InvalidOperationException(
					"Expected drawRectangle or position, but received neither or both."
				);
			}
			else if (position != null)
			{
				// Call Draw() using position
				Draw(
					texture,
					(Vector2) position,
					sourceRectangle,
					(Color) color,
					rotation,
					(Vector2) origin,
					(Vector2) scale,
					effect,
					depth
				);
			}
			else
			{
				// Call Draw() using drawRectangle
				Draw(
					texture,
					(Rectangle) drawRectangle,
					sourceRectangle,
					(Color) color,
					rotation,
					(Vector2) origin,
					effect,
					depth
				);
			}
		}

		public void Draw(
			Texture2D texture,
			Vector2 position,
			Rectangle? sourceRectangle,
			Color color,
			float rotation,
			Vector2 origin,
			float scale,
			SpriteEffects effect,
			float depth
		) {
			CheckValid(texture);

			float w = texture.Width * scale;
			float h = texture.Height * scale;
			if (sourceRectangle.HasValue)
			{
				w = sourceRectangle.Value.Width * scale;
				h = sourceRectangle.Value.Height * scale;
			}

			DrawInternal(
				texture,
				new Vector4(position.X, position.Y, w, h),
				sourceRectangle,
				color,
				rotation,
				origin * scale,
				effect,
				depth,
				true
			);
		}

		public void Draw(
			Texture2D texture,
			Vector2 position,
			Rectangle? sourceRectangle,
			Color color,
			float rotation,
			Vector2 origin,
			Vector2 scale,
			SpriteEffects effect,
			float depth
		) {
			CheckValid(texture);

			float w = texture.Width * scale.X;
			float h = texture.Height * scale.Y;
			if (sourceRectangle.HasValue)
			{
				w = sourceRectangle.Value.Width * scale.X;
				h = sourceRectangle.Value.Height * scale.Y;
			}

			DrawInternal(
				texture,
				new Vector4(position.X, position.Y, w, h),
				sourceRectangle,
				color,
				rotation,
				origin * scale,
				effect,
				depth,
				true
			);
		}

		public void Draw(
			Texture2D texture,
			Rectangle destinationRectangle,
			Rectangle? sourceRectangle,
			Color color,
			float rotation,
			Vector2 origin,
			SpriteEffects effect,
			float depth
		) {
			CheckValid(texture);

			DrawInternal(
				texture,
				new Vector4(
					destinationRectangle.X,
					destinationRectangle.Y,
					destinationRectangle.Width,
					destinationRectangle.Height
				),
				sourceRectangle,
				color,
				rotation,
				new Vector2(
					origin.X
						* ((float) destinationRectangle.Width
							/ (float) ((sourceRectangle.HasValue && sourceRectangle.Value.Width != 0)
								? sourceRectangle.Value.Width : texture.Width)
						),
					origin.Y
						* ((float) destinationRectangle.Height)
							/ (float) ((sourceRectangle.HasValue && sourceRectangle.Value.Height != 0)
								? sourceRectangle.Value.Height : texture.Height)
				),
				effect,
				depth,
				true
			);
		}

		public void DrawString(
			SpriteFont spriteFont,
			StringBuilder text,
			Vector2 position,
			Color color
		) {
			CheckValid(spriteFont, text);

			SpriteFont.CharacterSource source = new SpriteFont.CharacterSource(text);
			spriteFont.DrawInto(
				this,
				ref source,
				position,
				color,
				0,
				Vector2.Zero,
				Vector2.One,
				SpriteEffects.None,
				0f
			);
		}

		public void DrawString(
			SpriteFont spriteFont,
			string text,
			Vector2 position,
			Color color,
			float rotation,
			Vector2 origin,
			float scale,
			SpriteEffects effects,
			float depth
		) {
			CheckValid(spriteFont, text);

			Vector2 scaleVec = new Vector2(scale, scale);
			SpriteFont.CharacterSource source = new SpriteFont.CharacterSource(text);
			spriteFont.DrawInto(
				this,
				ref source,
				position,
				color,
				rotation,
				origin,
				scaleVec,
				effects,
				depth
			);
		}

		public void DrawString(
			SpriteFont spriteFont,
			string text,
			Vector2 position,
			Color color,
			float rotation,
			Vector2 origin,
			Vector2 scale,
			SpriteEffects effect,
			float depth
		) {
			CheckValid(spriteFont, text);

			SpriteFont.CharacterSource source = new SpriteFont.CharacterSource(text);
			spriteFont.DrawInto(
				this,
				ref source,
				position,
				color,
				rotation,
				origin,
				scale,
				effect,
				depth
			);
		}

		public void DrawString(
			SpriteFont spriteFont,
			StringBuilder text,
			Vector2 position,
			Color color,
			float rotation,
			Vector2 origin,
			float scale,
			SpriteEffects effects,
			float depth
		) {
			CheckValid(spriteFont, text);

			Vector2 scaleVec = new Vector2(scale, scale);
			SpriteFont.CharacterSource source = new SpriteFont.CharacterSource(text);
			spriteFont.DrawInto(
				this,
				ref source,
				position,
				color,
				rotation,
				origin,
				scaleVec,
				effects,
				depth
			);
		}

		public void DrawString(
			SpriteFont spriteFont,
			StringBuilder text,
			Vector2 position,
			Color color,
			float rotation,
			Vector2 origin,
			Vector2 scale,
			SpriteEffects effect,
			float depth
		) {
			CheckValid(spriteFont, text);

			SpriteFont.CharacterSource source = new SpriteFont.CharacterSource(text);
			spriteFont.DrawInto(
				this,
				ref source,
				position,
				color,
				rotation,
				origin,
				scale,
				effect,
				depth
			);
		}

		#endregion

		#region Internal Methods

		internal void DrawInternal(
			Texture2D texture,
			Vector4 destinationRectangle,
			Rectangle? sourceRectangle,
			Color color,
			float rotation,
			Vector2 origin,
			SpriteEffects effect,
			float depth,
			bool autoFlush
		) {
			SpriteBatchItem item = _batcher.CreateBatchItem();

			item.Depth = depth;
			item.Texture = texture;

			if (sourceRectangle.HasValue)
			{
				_tempRect = sourceRectangle.Value;
			}
			else
			{
				_tempRect.X = 0;
				_tempRect.Y = 0;
				_tempRect.Width = texture.Width;
				_tempRect.Height = texture.Height;
			}

			_texCoordTL.X = _tempRect.X / (float) texture.Width;
			_texCoordTL.Y = _tempRect.Y / (float) texture.Height;
			_texCoordBR.X = (_tempRect.X + _tempRect.Width) / (float) texture.Width;
			_texCoordBR.Y = (_tempRect.Y + _tempRect.Height) / (float) texture.Height;

			if ((effect & SpriteEffects.FlipVertically) != 0)
			{
				float temp = _texCoordBR.Y;
				_texCoordBR.Y = _texCoordTL.Y;
				_texCoordTL.Y = temp;
			}
			if ((effect & SpriteEffects.FlipHorizontally) != 0)
			{
				float temp = _texCoordBR.X;
				_texCoordBR.X = _texCoordTL.X;
				_texCoordTL.X = temp;
			}

			item.Set(
				destinationRectangle.X,
				destinationRectangle.Y,
				-origin.X,
				-origin.Y,
				destinationRectangle.Z,
				destinationRectangle.W,
				(float) Math.Sin(rotation),
				(float) Math.Cos(rotation),
				color,
				_texCoordTL,
				_texCoordBR
			);

			if (autoFlush)
			{
				FlushIfNeeded();
			}
		}

		// Mark the end of a draw operation for Immediate SpriteSortMode.
		public void FlushIfNeeded()
		{
			if (_sortMode == SpriteSortMode.Immediate)
			{
				_batcher.DrawBatch(_sortMode);
			}
		}

		#endregion

		#region Protected Dispose Method

		protected override void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				if (disposing)
				{
					if (_spriteEffect != null)
					{
						_spriteEffect.Dispose();
						_spriteEffect = null;
					}
				}
			}
			base.Dispose(disposing);
		}

		#endregion

        #region ISpriteBatch
        void ISpriteBatch.Draw(Texture2D texture,
            Vector4 destinationRectangle,
            Rectangle? sourceRectangle,
            Color color,
            float rotation,
            Vector2 origin,
            SpriteEffects effect,
            float depth,
            bool autoFlush)
        {
            this.DrawInternal(texture, destinationRectangle, sourceRectangle, color, rotation, origin, effect, depth, autoFlush);
        }
        #endregion

        #region Private Methods

        void Setup()
		{
			GraphicsDevice gd = GraphicsDevice;
			gd.BlendState = _blendState;
			gd.DepthStencilState = _depthStencilState;
			gd.RasterizerState = _rasterizerState;
			gd.SamplerStates[0] = _samplerState;

			// Setup the default sprite effect.
			Viewport vp = gd.Viewport;

			Matrix projection;
			// GL requires a half pixel offset to match DX.
			Matrix.CreateOrthographicOffCenter(0, vp.Width, vp.Height, 0, 0, 1, out projection);
			projection.M41 += -0.5f * projection.M11;
			projection.M42 += -0.5f * projection.M22;
			Matrix.Multiply(ref _matrix, ref projection, out projection);

			_matrixTransform.SetValue(projection);
			_spritePass.Apply();

			/* If the user supplied a custom effect then apply
			 * it now to override the sprite effect.
			 */
			if (_effect != null)
			{
				_effect.CurrentTechnique.Passes[0].Apply();
			}
		}

		void CheckValid(Texture2D texture)
		{
			if (texture == null)
			{
				throw new ArgumentNullException("texture");
			}

			if (!_beginCalled)
			{
				throw new InvalidOperationException("Draw was called, but Begin has not yet been called. " +
					"Begin must be called successfully before you can call Draw.");
			}
		}

		void CheckValid(SpriteFont spriteFont, string text)
		{
			if (spriteFont == null)
			{
				throw new ArgumentNullException("spriteFont");
			}

			if (text == null)
			{
				throw new ArgumentNullException("text");
			}

			if (!_beginCalled)
			{
				throw new InvalidOperationException("DrawString was called, but Begin has not yet been called. " +
					"Begin must be called successfully before you can call DrawString.");
			}
		}

		void CheckValid(SpriteFont spriteFont, StringBuilder text)
		{
			if (spriteFont == null)
			{
				throw new ArgumentNullException("spriteFont");
			}

			if (text == null)
			{
				throw new ArgumentNullException("text");
			}

			if (!_beginCalled)
			{
				throw new InvalidOperationException("DrawString was called, but Begin has not yet been called. " +
					"Begin must be called successfully before you can call DrawString.");
			}
		}

		#endregion
	}
}
