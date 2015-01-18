#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

#region VIDEOPLAYER_OPENGL Option
/* By default we use a small fragment shader to perform the YUV-RGBA conversion.
 * If for some reason you need to use the software converter in TheoraPlay,
 * comment out this define.
 * -flibit
 */
#define VIDEOPLAYER_OPENGL
#endregion

#region Using Statements
using System;
using System.Collections.Generic;
using System.Diagnostics;

#if VIDEOPLAYER_OPENGL
using System.Runtime.InteropServices;
#endif

using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
#endregion

namespace Microsoft.Xna.Framework.Media
{
	public sealed class VideoPlayer : IDisposable
	{
		#region Hardware-accelerated YUV -> RGBA

#if VIDEOPLAYER_OPENGL
		private static string shader_vertex =
			"#version 110\n" +
			"attribute vec2 pos;\n" +
			"attribute vec2 tex;\n" +
			"void main() {\n" +
			"   gl_Position = vec4(pos.xy, 0.0, 1.0);\n" +
			"   gl_TexCoord[0].xy = tex;\n" +
			"}\n";
		private static string shader_fragment =
			"#version 110\n" +
			"uniform sampler2D samp0;\n" +
			"uniform sampler2D samp1;\n" +
			"uniform sampler2D samp2;\n" +
			"const vec3 offset = vec3(-0.0625, -0.5, -0.5);\n" +
			"const vec3 Rcoeff = vec3(1.164,  0.000,  1.596);\n" +
			"const vec3 Gcoeff = vec3(1.164, -0.391, -0.813);\n" +
			"const vec3 Bcoeff = vec3(1.164,  2.018,  0.000);\n" +
			"void main() {\n" +
			"   vec2 tcoord;\n" +
			"   vec3 yuv, rgb;\n" +
			"   tcoord = gl_TexCoord[0].xy;\n" +
			"   yuv.x = texture2D(samp0, tcoord).r;\n" +
			"   yuv.y = texture2D(samp1, tcoord).r;\n" +
			"   yuv.z = texture2D(samp2, tcoord).r;\n" +
			"   yuv += offset;\n" +
			"   rgb.r = dot(yuv, Rcoeff);\n" +
			"   rgb.g = dot(yuv, Gcoeff);\n" +
			"   rgb.b = dot(yuv, Bcoeff);\n" +
			"   gl_FragColor = vec4(rgb, 1.0);\n" +
			"}\n";

		private uint shaderProgram;
		private uint[] yuvTextures;
		private uint rgbaFramebuffer;

		private static float[] vert_pos = new float[]
		{
			-1.0f,  1.0f,
			 1.0f,  1.0f,
			-1.0f, -1.0f,
			 1.0f, -1.0f
		};
		private static float[] vert_tex = new float[]
		{
			0.0f, 1.0f,
			1.0f, 1.0f,
			0.0f, 0.0f,
			1.0f, 0.0f
		};
		private static GCHandle vertPosArry = GCHandle.Alloc(vert_pos, GCHandleType.Pinned);
		private static GCHandle vertTexArry = GCHandle.Alloc(vert_tex, GCHandleType.Pinned);
		private static IntPtr vertPosPtr = vertPosArry.AddrOfPinnedObject();
		private static IntPtr vertTexPtr = vertTexArry.AddrOfPinnedObject();

		// Used to restore our previous GL state.
		private OpenGLDevice.OpenGLTexture[] oldTextures;
		private int oldShader;
		private uint oldFramebuffer;

		private void GL_initialize()
		{
			// Initialize the sampler storage arrays.
			oldTextures = new OpenGLDevice.OpenGLTexture[3];

			// Create the YUV textures.
			yuvTextures = new uint[3];
			// FIXME: lol at my glGenTextures func -flibit
			currentDevice.GLDevice.glGenTextures(1, out yuvTextures[0]);
			currentDevice.GLDevice.glGenTextures(1, out yuvTextures[1]);
			currentDevice.GLDevice.glGenTextures(1, out yuvTextures[2]);

			// Create the RGBA framebuffer target.
			currentDevice.GLDevice.glGenFramebuffers(
				1,
				out rgbaFramebuffer
			);

			// Create our pile of vertices.
			vert_pos = new float[2 * 4]; // 2 dimensions * 4 vertices
			vert_tex = new float[2 * 4];

			// Create the vertex/fragment shaders.
			uint vshader_id = currentDevice.GLDevice.glCreateShader(
				OpenGLDevice.GLenum.GL_VERTEX_SHADER
			);
			int len = shader_vertex.Length;
			currentDevice.GLDevice.glShaderSource(
				vshader_id,
				1,
				ref shader_vertex,
				ref len
			);
			currentDevice.GLDevice.glCompileShader(vshader_id);
			uint fshader_id = currentDevice.GLDevice.glCreateShader(
				OpenGLDevice.GLenum.GL_FRAGMENT_SHADER
			);
			len = shader_fragment.Length;
			currentDevice.GLDevice.glShaderSource(
				fshader_id,
				1,
				ref shader_fragment,
				ref len
			);
			currentDevice.GLDevice.glCompileShader(fshader_id);

			// Create the shader program.
			shaderProgram = currentDevice.GLDevice.glCreateProgram();
			currentDevice.GLDevice.glAttachShader(shaderProgram, vshader_id);
			currentDevice.GLDevice.glAttachShader(shaderProgram, fshader_id);
			currentDevice.GLDevice.glBindAttribLocation(shaderProgram, 0, "pos");
			currentDevice.GLDevice.glBindAttribLocation(shaderProgram, 1, "tex");
			currentDevice.GLDevice.glLinkProgram(shaderProgram);
			currentDevice.GLDevice.glDeleteShader(vshader_id);
			currentDevice.GLDevice.glDeleteShader(fshader_id);

			// Set uniform values now. They won't change, promise!
			currentDevice.GLDevice.glGetIntegerv(
				OpenGLDevice.GLenum.GL_CURRENT_PROGRAM,
				out oldShader
			);
			currentDevice.GLDevice.glUseProgram(shaderProgram);
			currentDevice.GLDevice.glUniform1i(
				currentDevice.GLDevice.glGetUniformLocation(shaderProgram, "samp0"),
				0
			);
			currentDevice.GLDevice.glUniform1i(
				currentDevice.GLDevice.glGetUniformLocation(shaderProgram, "samp1"),
				1
			);
			currentDevice.GLDevice.glUniform1i(
				currentDevice.GLDevice.glGetUniformLocation(shaderProgram, "samp2"),
				2
			);
			currentDevice.GLDevice.glUseProgram((uint) oldShader);
		}

		private void GL_dispose()
		{
			// Delete the shader program.
			currentDevice.GLDevice.glDeleteProgram(shaderProgram);

			// Delete the RGBA framebuffer target.
			currentDevice.GLDevice.glDeleteFramebuffers(
				1,
				ref rgbaFramebuffer
			);

			// Delete the YUV textures.
			// FIXME: lol at my glGenTextures func -flibit
			currentDevice.GLDevice.glDeleteTextures(1, ref yuvTextures[0]);
			currentDevice.GLDevice.glDeleteTextures(1, ref yuvTextures[1]);
			currentDevice.GLDevice.glDeleteTextures(1, ref yuvTextures[2]);
		}

		private void GL_internal_genTexture(
			uint texID,
			int width,
			int height
		) {
			// Bind the desired texture.
			currentDevice.GLDevice.glBindTexture(
				OpenGLDevice.GLenum.GL_TEXTURE_2D,
				texID
			);

			// Set the texture parameters, for completion/consistency's sake.
			currentDevice.GLDevice.glTexParameteri(
				OpenGLDevice.GLenum.GL_TEXTURE_2D,
				OpenGLDevice.GLenum.GL_TEXTURE_WRAP_S,
				(int) OpenGLDevice.GLenum.GL_CLAMP_TO_EDGE
			);
			currentDevice.GLDevice.glTexParameteri(
				OpenGLDevice.GLenum.GL_TEXTURE_2D,
				OpenGLDevice.GLenum.GL_TEXTURE_WRAP_T,
				(int) OpenGLDevice.GLenum.GL_CLAMP_TO_EDGE
			);
			currentDevice.GLDevice.glTexParameteri(
				OpenGLDevice.GLenum.GL_TEXTURE_2D,
				OpenGLDevice.GLenum.GL_TEXTURE_MIN_FILTER,
				(int) OpenGLDevice.GLenum.GL_LINEAR
			);
			currentDevice.GLDevice.glTexParameteri(
				OpenGLDevice.GLenum.GL_TEXTURE_2D,
				OpenGLDevice.GLenum.GL_TEXTURE_MAG_FILTER,
				(int) OpenGLDevice.GLenum.GL_LINEAR
			);
			currentDevice.GLDevice.glTexParameteri(
				OpenGLDevice.GLenum.GL_TEXTURE_2D,
				OpenGLDevice.GLenum.GL_TEXTURE_BASE_LEVEL,
				0
			);
			currentDevice.GLDevice.glTexParameteri(
				OpenGLDevice.GLenum.GL_TEXTURE_2D,
				OpenGLDevice.GLenum.GL_TEXTURE_MAX_LEVEL,
				0
			);

			// Allocate the texture data.
			currentDevice.GLDevice.glTexImage2D(
				OpenGLDevice.GLenum.GL_TEXTURE_2D,
				0,
				(int) OpenGLDevice.GLenum.GL_LUMINANCE,
				width,
				height,
				0,
				OpenGLDevice.GLenum.GL_LUMINANCE,
				OpenGLDevice.GLenum.GL_UNSIGNED_BYTE,
				IntPtr.Zero
			);
		}

		private void GL_setupTargets(int width, int height)
		{
			// We're going to mess with sampler 0's texture.
			OpenGLDevice.OpenGLTexture prevTexture = currentDevice.GLDevice.Textures[0];

			// Attach the Texture2D to the framebuffer.
			uint prevReadFramebuffer = currentDevice.GLDevice.CurrentReadFramebuffer;
			uint prevDrawFramebuffer = currentDevice.GLDevice.CurrentDrawFramebuffer;
			currentDevice.GLDevice.BindFramebuffer(rgbaFramebuffer);
			currentDevice.GLDevice.glFramebufferTexture2D(
				OpenGLDevice.GLenum.GL_FRAMEBUFFER,
				OpenGLDevice.GLenum.GL_COLOR_ATTACHMENT0,
				OpenGLDevice.GLenum.GL_TEXTURE_2D,
				videoTexture.texture.Handle,
				0
			);
			currentDevice.GLDevice.BindReadFramebuffer(prevReadFramebuffer);
			currentDevice.GLDevice.BindDrawFramebuffer(prevDrawFramebuffer);

			// Be careful about non-2D textures currently bound...
			if (prevTexture.Target != OpenGLDevice.GLenum.GL_TEXTURE_2D)
			{
				currentDevice.GLDevice.glBindTexture(prevTexture.Target, 0);
			}

			// Allocate YUV GL textures
			GL_internal_genTexture(
				yuvTextures[0],
				width,
				height
			);
			GL_internal_genTexture(
				yuvTextures[1],
				width / 2,
				height / 2
			);
			GL_internal_genTexture(
				yuvTextures[2],
				width / 2,
				height / 2
			);

			// Aaand we should be set now.
			if (prevTexture.Target != OpenGLDevice.GLenum.GL_TEXTURE_2D)
			{
				currentDevice.GLDevice.glBindTexture(
					OpenGLDevice.GLenum.GL_TEXTURE_2D,
					0
				);
			}
			currentDevice.GLDevice.glBindTexture(prevTexture.Target, prevTexture.Handle);
		}

		private void GL_pushState()
		{
			/* Argh, a glGet!
			 * We could in theory store this, but when we do direct MojoShader,
			 * that will be obscured away. It sucks, but at least it's just
			 * this one time!
			 * -flibit
			 */
			currentDevice.GLDevice.glGetIntegerv(
				OpenGLDevice.GLenum.GL_CURRENT_PROGRAM,
				out oldShader
			);

			// Prep our samplers
			for (int i = 0; i < 3; i += 1)
			{
				oldTextures[i] = currentDevice.GLDevice.Textures[i];
				if (oldTextures[i].Target != OpenGLDevice.GLenum.GL_TEXTURE_2D)
				{
					currentDevice.GLDevice.glActiveTexture(
						OpenGLDevice.GLenum.GL_TEXTURE0 + i
					);
					currentDevice.GLDevice.glBindTexture(oldTextures[i].Target, 0);
				}
			}

			// Store the current framebuffer, may be backbuffer or target FBO
			oldFramebuffer = currentDevice.GLDevice.CurrentDrawFramebuffer;

			// Disable various GL options
			if (currentDevice.GLDevice.alphaBlendEnable)
			{
				currentDevice.GLDevice.glDisable(
					OpenGLDevice.GLenum.GL_BLEND
				);
			}
			if (currentDevice.GLDevice.zEnable)
			{
				currentDevice.GLDevice.glDisable(
					OpenGLDevice.GLenum.GL_DEPTH_TEST
				);
			}
			if (currentDevice.GLDevice.cullFrontFace != CullMode.None)
			{
				currentDevice.GLDevice.glDisable(
					OpenGLDevice.GLenum.GL_CULL_FACE
				);
			}
			if (currentDevice.GLDevice.scissorTestEnable)
			{
				currentDevice.GLDevice.glDisable(
					OpenGLDevice.GLenum.GL_SCISSOR_TEST
				);
			}
		}

		private void GL_popState()
		{
			// Flush the viewport, reset.
			Rectangle oldViewport = currentDevice.Viewport.Bounds;
			currentDevice.GLDevice.glViewport(
				oldViewport.X,
				oldViewport.Y,
				oldViewport.Width,
				oldViewport.Height
			);

			// Restore the program we got from glGet :(
			currentDevice.GLDevice.glUseProgram((uint) oldShader);

			// Restore the sampler bindings
			for (int i = 0; i < 3; i += 1)
			{
				currentDevice.GLDevice.glActiveTexture(
					OpenGLDevice.GLenum.GL_TEXTURE0 + i
				);
				if (oldTextures[i].Target != OpenGLDevice.GLenum.GL_TEXTURE_2D)
				{
					currentDevice.GLDevice.glBindTexture(
						OpenGLDevice.GLenum.GL_TEXTURE_2D,
						0
					);
				}
				currentDevice.GLDevice.glBindTexture(oldTextures[i].Target, oldTextures[i].Handle);
			}

			// Keep this state sane.
			currentDevice.GLDevice.glActiveTexture(
				OpenGLDevice.GLenum.GL_TEXTURE0
			);

			// Restore the active framebuffer
			currentDevice.GLDevice.BindDrawFramebuffer(oldFramebuffer);

			// Flush various GL states, if applicable
			if (currentDevice.GLDevice.scissorTestEnable)
			{
				currentDevice.GLDevice.glEnable(
					OpenGLDevice.GLenum.GL_SCISSOR_TEST
				);
			}
			if (currentDevice.GLDevice.cullFrontFace != CullMode.None)
			{
				currentDevice.GLDevice.glEnable(
					OpenGLDevice.GLenum.GL_CULL_FACE
				);
			}
			if (currentDevice.GLDevice.zEnable)
			{
				currentDevice.GLDevice.glEnable(
					OpenGLDevice.GLenum.GL_DEPTH_TEST
				);
			}
			if (currentDevice.GLDevice.alphaBlendEnable)
			{
				currentDevice.GLDevice.glEnable(
					OpenGLDevice.GLenum.GL_BLEND
				);
			}
		}
#endif

		#endregion

		#region Public Member Data: XNA VideoPlayer Implementation

		public bool IsDisposed
		{
			get;
			private set;
		}

		public bool IsLooped
		{
			get;
			set;
		}

		private bool backing_ismuted;
		public bool IsMuted
		{
			get
			{
				return backing_ismuted;
			}
			set
			{
				backing_ismuted = value;
				UpdateVolume();
			}
		}

		public TimeSpan PlayPosition
		{
			get
			{
				return timer.Elapsed;
			}
		}

		public MediaState State
		{
			get;
			private set;
		}

		public Video Video
		{
			get;
			private set;
		}

		private float backing_volume;
		public float Volume
		{
			get
			{
				return backing_volume;
			}
			set
			{
				if (value > 1.0f)
				{
					backing_volume = 1.0f;
				}
				else if (value < 0.0f)
				{
					backing_volume = 0.0f;
				}
				else
				{
					backing_volume = value;
				}
				UpdateVolume();
			}
		}

		#endregion

		#region Private Member Data: XNA VideoPlayer Implementation

		// We use this to update our PlayPosition.
		private Stopwatch timer;

		// Store this to optimize things on our end.
		private Texture2D videoTexture;

		// We need to access the GLDevice frequently.
		private GraphicsDevice currentDevice;

		#endregion

		#region Private Member Data: TheoraPlay

		// Grabbed from the Video streams.
		private TheoraPlay.THEORAPLAY_VideoFrame currentVideo;
		private TheoraPlay.THEORAPLAY_VideoFrame nextVideo;
		private IntPtr previousFrame;

		#endregion

		#region Private Member Data: OpenAL

		private DynamicSoundEffectInstance audioStream;

		#endregion

		#region Private Methods: XNA VideoPlayer Implementation

		private void checkDisposed()
		{
			if (IsDisposed)
			{
				throw new ObjectDisposedException("VideoPlayer");
			}
		}

		#endregion

		#region Private Methods: OpenAL

		private void UpdateVolume()
		{
			if (audioStream == null)
			{
				return;
			}
			if (IsMuted)
			{
				audioStream.Volume = 0.0f;
			}
			else
			{
				audioStream.Volume = Volume;
			}
		}

		#endregion

		#region Public Methods: XNA VideoPlayer Implementation

		public VideoPlayer()
		{
			// Initialize public members.
			IsDisposed = false;
			IsLooped = false;
			IsMuted = false;
			State = MediaState.Stopped;
			Volume = 1.0f;

			// Initialize private members.
			timer = new Stopwatch();

			// The VideoPlayer will use the GraphicsDevice that is set now.
			currentDevice = Game.Instance.GraphicsDevice;

			// Initialize this here to prevent null GetTexture returns.
			videoTexture = new Texture2D(
				currentDevice,
				1280,
				720
			);

#if VIDEOPLAYER_OPENGL
			// Initialize the OpenGL bits.
			GL_initialize();
#endif
		}

		public void Dispose()
		{
			// Stop the VideoPlayer. This gets almost everything...
			Stop();

#if VIDEOPLAYER_OPENGL
			// Destroy the OpenGL bits.
			GL_dispose();
#endif

			// Dispose the DynamicSoundEffectInstance
			if (audioStream != null)
			{
				audioStream.Dispose();
				audioStream = null;
			}

			// Dispose the Texture.
			videoTexture.Dispose();

			// Okay, we out.
			IsDisposed = true;
		}

		public Texture2D GetTexture()
		{
			checkDisposed();

			// Be sure we can even get something from TheoraPlay...
			if (	State == MediaState.Stopped ||
				Video.theoraDecoder == IntPtr.Zero ||
				TheoraPlay.THEORAPLAY_isInitialized(Video.theoraDecoder) == 0 ||
				TheoraPlay.THEORAPLAY_hasVideoStream(Video.theoraDecoder) == 0	)
			{
				return videoTexture; // Screw it, give them the old one.
			}

			// Get the latest video frames.
			bool missedFrame = false;
			while (nextVideo.playms <= timer.ElapsedMilliseconds && !missedFrame)
			{
				currentVideo = nextVideo;
				IntPtr nextFrame = TheoraPlay.THEORAPLAY_getVideo(Video.theoraDecoder);
				if (nextFrame != IntPtr.Zero)
				{
					TheoraPlay.THEORAPLAY_freeVideo(previousFrame);
					previousFrame = Video.videoStream;
					Video.videoStream = nextFrame;
					nextVideo = TheoraPlay.getVideoFrame(Video.videoStream);
					missedFrame = false;
				}
				else
				{
					// Don't mind me, just ignoring that complete failure above!
					missedFrame = true;
				}

				if (TheoraPlay.THEORAPLAY_isDecoding(Video.theoraDecoder) == 0)
				{
					// FIXME: This is part of the Duration hack!
					Video.Duration = new TimeSpan(0, 0, 0, 0, (int) currentVideo.playms);

					// Stop and reset the timer. If we're looping, the loop will start it again.
					timer.Stop();
					timer.Reset();

					// If looping, go back to the start. Otherwise, we'll be exiting.
					if (IsLooped && State == MediaState.Playing)
					{
						// Kill the audio, no matter what.
						if (audioStream != null)
						{
							audioStream.Stop();
							audioStream.Dispose();
							audioStream = null;
						}

						// Free everything and start over.
						TheoraPlay.THEORAPLAY_freeVideo(previousFrame);
						previousFrame = IntPtr.Zero;
						Video.AttachedToPlayer = false;
						Video.Dispose();
						Video.AttachedToPlayer = true;
						Video.Initialize();

						// Grab the initial audio again.
						if (TheoraPlay.THEORAPLAY_hasAudioStream(Video.theoraDecoder) != 0)
						{
							InitAudioStream();
						}

						// Grab the initial video again.
						if (TheoraPlay.THEORAPLAY_hasVideoStream(Video.theoraDecoder) != 0)
						{
							currentVideo = TheoraPlay.getVideoFrame(Video.videoStream);
							previousFrame = Video.videoStream;
							do
							{
								// The decoder miiight not be ready yet.
								Video.videoStream = TheoraPlay.THEORAPLAY_getVideo(Video.theoraDecoder);
							} while (Video.videoStream == IntPtr.Zero);
							nextVideo = TheoraPlay.getVideoFrame(Video.videoStream);
						}

						// Start! Again!
						timer.Start();
						if (audioStream != null)
						{
							audioStream.Play();
						}
					}
					else
					{
						// Stop everything, clean up. We out.
						State = MediaState.Stopped;
						if (audioStream != null)
						{
							audioStream.Stop();
							audioStream.Dispose();
							audioStream = null;
						}
						TheoraPlay.THEORAPLAY_freeVideo(previousFrame);
						Video.AttachedToPlayer = false;
						Video.Dispose();

						// We're done, so give them the last frame.
						return videoTexture;
					}
				}
			}

#if VIDEOPLAYER_OPENGL
			// Set up an environment to muck about in.
			GL_pushState();

			// Bind our shader program.
			currentDevice.GLDevice.glUseProgram(shaderProgram);

			// We're using client-side arrays like CAVEMEN
			currentDevice.GLDevice.BindVertexBuffer(OpenGLDevice.OpenGLVertexBuffer.NullBuffer);

			// We're also setting pointers without any checks, like MAVERICKS
			currentDevice.GLDevice.glVertexAttribPointer(
				0,
				2,
				OpenGLDevice.GLenum.GL_FLOAT,
				false,
				2 * sizeof(float),
				vertPosPtr
			);
			currentDevice.GLDevice.glVertexAttribPointer(
				1,
				2,
				OpenGLDevice.GLenum.GL_FLOAT,
				false,
				2 * sizeof(float),
				vertTexPtr
			);

			// Bind our target framebuffer.
			currentDevice.GLDevice.BindDrawFramebuffer(rgbaFramebuffer);

			// Prepare YUV GL textures with our current frame data
			currentDevice.GLDevice.glActiveTexture(
				OpenGLDevice.GLenum.GL_TEXTURE0
			);
			currentDevice.GLDevice.glBindTexture(
				OpenGLDevice.GLenum.GL_TEXTURE_2D,
				yuvTextures[0]
			);
			currentDevice.GLDevice.glTexSubImage2D(
				OpenGLDevice.GLenum.GL_TEXTURE_2D,
				0,
				0,
				0,
				(int) currentVideo.width,
				(int) currentVideo.height,
				OpenGLDevice.GLenum.GL_LUMINANCE,
				OpenGLDevice.GLenum.GL_UNSIGNED_BYTE,
				currentVideo.pixels
			);
			currentDevice.GLDevice.glActiveTexture(
				OpenGLDevice.GLenum.GL_TEXTURE0 + 1
			);
			currentDevice.GLDevice.glBindTexture(
				OpenGLDevice.GLenum.GL_TEXTURE_2D,
				yuvTextures[1]
			);
			currentDevice.GLDevice.glTexSubImage2D(
				OpenGLDevice.GLenum.GL_TEXTURE_2D,
				0,
				0,
				0,
				(int) (currentVideo.width / 2),
				(int) (currentVideo.height / 2),
				OpenGLDevice.GLenum.GL_LUMINANCE,
				OpenGLDevice.GLenum.GL_UNSIGNED_BYTE,
				new IntPtr(
					currentVideo.pixels.ToInt64() +
					(currentVideo.width * currentVideo.height)
				)
			);
			currentDevice.GLDevice.glActiveTexture(
				OpenGLDevice.GLenum.GL_TEXTURE0 + 2
			);
			currentDevice.GLDevice.glBindTexture(
				OpenGLDevice.GLenum.GL_TEXTURE_2D,
				yuvTextures[2]
			);
			currentDevice.GLDevice.glTexSubImage2D(
				OpenGLDevice.GLenum.GL_TEXTURE_2D,
				0,
				0,
				0,
				(int) (currentVideo.width / 2),
				(int) (currentVideo.height / 2),
				OpenGLDevice.GLenum.GL_LUMINANCE,
				OpenGLDevice.GLenum.GL_UNSIGNED_BYTE,
				new IntPtr(
					currentVideo.pixels.ToInt64() +
					(currentVideo.width * currentVideo.height) +
					(currentVideo.width / 2 * currentVideo.height / 2)
				)
			);

			// Flip the viewport, because loldirectx
			currentDevice.GLDevice.glViewport(
				0,
				0,
				(int) currentVideo.width,
				(int) currentVideo.height
			);

			// Draw the YUV textures to the framebuffer with our shader.
			currentDevice.GLDevice.glDrawArrays(
				OpenGLDevice.GLenum.GL_TRIANGLE_STRIP,
				0,
				4
			);

			// Clean up after ourselves.
			GL_popState();
#else
			// Just copy it to an array, since it's RGBA anyway.
			try
			{
				byte[] theoraPixels = TheoraPlay.getPixels(
					currentVideo.pixels,
					(int) currentVideo.width * (int) currentVideo.height * 4
				);

				// TexImage2D.
				videoTexture.SetData<byte>(theoraPixels);
			}
			catch(Exception e)
			{
				// I hope we've still got something in videoTexture!
				System.Console.WriteLine(
					"WARNING: THEORA FRAME COPY FAILED: " +
					e.Message
				);
			}
#endif

			return videoTexture;
		}

		public void Play(Video video)
		{
			checkDisposed();

			// We need to assign this regardless of what happens next.
			Video = video;
			video.AttachedToPlayer = true;

			// FIXME: This is a part of the Duration hack!
			Video.Duration = TimeSpan.MaxValue;

			// Check the player state before attempting anything.
			if (State != MediaState.Stopped)
			{
				return;
			}

			// Update the player state now, for the thread we're about to make.
			State = MediaState.Playing;

			// Start the video if it hasn't been yet.
			if (Video.IsDisposed)
			{
				video.Initialize();
			}

			// Grab the first bit of audio. We're trying to start the decoding ASAP.
			if (TheoraPlay.THEORAPLAY_hasAudioStream(Video.theoraDecoder) != 0)
			{
				InitAudioStream();
			}

			// Grab the first bit of video, set up the texture.
			if (TheoraPlay.THEORAPLAY_hasVideoStream(Video.theoraDecoder) != 0)
			{
				currentVideo = TheoraPlay.getVideoFrame(Video.videoStream);
				previousFrame = Video.videoStream;
				do
				{
					// The decoder miiight not be ready yet.
					Video.videoStream = TheoraPlay.THEORAPLAY_getVideo(Video.theoraDecoder);
				} while (Video.videoStream == IntPtr.Zero);
				nextVideo = TheoraPlay.getVideoFrame(Video.videoStream);

				Texture2D overlap = videoTexture;
				videoTexture = new Texture2D(
					currentDevice,
					(int) currentVideo.width,
					(int) currentVideo.height,
					false,
					SurfaceFormat.Color
				);
				overlap.Dispose();
#if VIDEOPLAYER_OPENGL
				GL_setupTargets(
					(int) currentVideo.width,
					(int) currentVideo.height
				);
#endif
			}

			// Initialize the thread!
			System.Console.Write("Starting Theora player...");
			timer.Start();
			if (audioStream != null)
			{
				audioStream.Play();
			}
			System.Console.WriteLine(" Done!");
		}

		public void Stop()
		{
			checkDisposed();

			// Check the player state before attempting anything.
			if (State == MediaState.Stopped)
			{
				return;
			}

			// Update the player state.
			State = MediaState.Stopped;

			// Wait for the player to end if it's still going.
			System.Console.Write("Signaled Theora player to stop, waiting...");
			timer.Stop();
			timer.Reset();
			if (audioStream != null)
			{
				audioStream.Stop();
				audioStream.Dispose();
				audioStream = null;
			}
			if (previousFrame != IntPtr.Zero)
			{
				TheoraPlay.THEORAPLAY_freeVideo(previousFrame);
			}
			Video.AttachedToPlayer = false;
			Video.Dispose();
			System.Console.WriteLine(" Done!");
		}

		public void Pause()
		{
			checkDisposed();

			// Check the player state before attempting anything.
			if (State != MediaState.Playing)
			{
				return;
			}

			// Update the player state.
			State = MediaState.Paused;

			// Pause timer, audio.
			timer.Stop();
			if (audioStream != null)
			{
				audioStream.Pause();
			}
		}

		public void Resume()
		{
			checkDisposed();

			// Check the player state before attempting anything.
			if (State != MediaState.Paused)
			{
				return;
			}

			// Update the player state.
			State = MediaState.Playing;

			// Unpause timer, audio.
			timer.Start();
			if (audioStream != null)
			{
				audioStream.Resume();
			}
		}

		#endregion

		#region Private Theora Audio Stream Methods

		private bool StreamAudio()
		{
			// The size of our abstracted buffer.
			const int BUFFER_SIZE = 4096 * 2;

			// Store our abstracted buffer into here.
			List<float> data = new List<float>();

			// We'll store this here, so alBufferData can use it too.
			TheoraPlay.THEORAPLAY_AudioPacket currentAudio;
			currentAudio.channels = 0;
			currentAudio.freq = 0;

			// There might be an initial period of silence, so forcibly push through.
			while (	audioStream.State == SoundState.Stopped &&
				TheoraPlay.THEORAPLAY_availableAudio(Video.theoraDecoder) == 0	);

			// Add to the buffer from the decoder until it's large enough.
			while (	data.Count < BUFFER_SIZE &&
				TheoraPlay.THEORAPLAY_availableAudio(Video.theoraDecoder) > 0	)
			{
				IntPtr audioPtr = TheoraPlay.THEORAPLAY_getAudio(Video.theoraDecoder);
				currentAudio = TheoraPlay.getAudioPacket(audioPtr);
				data.AddRange(
					TheoraPlay.getSamples(
						currentAudio.samples,
						currentAudio.frames * currentAudio.channels
					)
				);
				TheoraPlay.THEORAPLAY_freeAudio(audioPtr);
			}

			// If we actually got data, buffer it into OpenAL.
			if (data.Count > 0)
			{
				audioStream.SubmitFloatBufferEXT(data.ToArray());
				return true;
			}
			return false;
		}

		private void OnBufferRequest(object sender, EventArgs args)
		{
			if (!StreamAudio())
			{
				// Okay, we ran out. No need for this!
				audioStream.BufferNeeded -= OnBufferRequest;
			}
		}

		private void InitAudioStream()
		{
			// The number of buffers to queue into the source.
			const int NUM_BUFFERS = 4;

			// Generate the source.
			IntPtr audioPtr = IntPtr.Zero;
			do
			{
				audioPtr = TheoraPlay.THEORAPLAY_getAudio(Video.theoraDecoder);
			} while (audioPtr == IntPtr.Zero);
			TheoraPlay.THEORAPLAY_AudioPacket packet = TheoraPlay.getAudioPacket(audioPtr);
			audioStream = new DynamicSoundEffectInstance(
				packet.freq,
				(AudioChannels) packet.channels
			);
			audioStream.BufferNeeded += OnBufferRequest;
			UpdateVolume();

			// Fill and queue the buffers.
			for (int i = 0; i < NUM_BUFFERS; i += 1)
			{
				if (!StreamAudio())
				{
					break;
				}
			}
		}

		#endregion
	}
}
