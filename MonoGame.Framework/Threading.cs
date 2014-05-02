#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
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
 * Also note that this affects SDL2/SDL2_GameWindow.cs! Check THREADED_GL there too.
 *
 * Basically, if you have to enable this, you should feel very bad.
 * -flibit
 */
#endregion

#region Using Statements
using System;
using System.Collections.Generic;
using System.Threading;

#if THREADED_GL
using SDL2;
using OpenTK.Graphics.OpenGL;
#endif
#endregion

namespace Microsoft.Xna.Framework
{
	internal class Threading
	{
		#region Private Thread ID Variable

		private static int mainThreadId;

		#endregion

#if THREADED_GL
		#region Threaded GL: Public OpenGL Window/Context Handles

		internal class GL_ContextHandle
		{
			public IntPtr context;
		}
		public static GL_ContextHandle BackgroundContext;
		public static IntPtr WindowInfo;

		#endregion
#else
		#region Private GL Action Queue

		private static List<Action> actions = new List<Action>();

		#endregion
#endif

		#region Static Constructor

		static Threading()
		{
			mainThreadId = Thread.CurrentThread.ManagedThreadId;
		}

		#endregion

		#region Public Thread ID Check Method

		/// <summary>
		/// Checks if the code is currently running on the main thread.
		/// </summary>
		/// <returns>True if the code is running on the main thread.</returns>
		public static bool IsOnMainThread()
		{
			return mainThreadId == Thread.CurrentThread.ManagedThreadId;
		}

		#endregion

		#region Public Threaded GL Action Method

		/// <summary>
		/// Runs a threaded operation on the main thread, blocking the thread.
		/// If we're on the main thread, the action will run immediately.
		/// </summary>
		/// <param name="action">The action to be run on the main thread.</param>
		public static void ForceToMainThread(Action action)
		{
			// If we're already on the main thread, just call the action.
			if (mainThreadId == Thread.CurrentThread.ManagedThreadId)
			{
				action();
				return;
			}

#if THREADED_GL
			lock (BackgroundContext)
			{
				// Make the context current on this thread.
				SDL.SDL_GL_MakeCurrent(WindowInfo, BackgroundContext.context);

				// Execute the action.
				action();

				// Must flush the GL calls now before we release the context.
				GL.Flush();

				// Free the threaded context for the next threaded call...
				SDL.SDL_GL_MakeCurrent(WindowInfo, IntPtr.Zero);
			}
#else
			ManualResetEventSlim resetEvent = new ManualResetEventSlim(false);
			lock (actions)
			{
				actions.Add(() =>
				{
					action();
					resetEvent.Set();
				});
			}
			resetEvent.Wait();
#endif
		}

		#endregion

		#region Public GL Action Queue Flush Method

#if !THREADED_GL
		/// <summary>
		/// Runs all pending actions. Must be called from the main thread.
		/// </summary>
		public static void Run()
		{
			lock (actions)
			{
				foreach (Action action in actions)
				{
					action();
				}
				actions.Clear();
			}
		}
#endif

		#endregion
	}
}
