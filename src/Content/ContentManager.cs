#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2015 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

#region Using Statements
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Utilities;
#endregion

namespace Microsoft.Xna.Framework.Content
{
	public partial class ContentManager : IDisposable
	{
		#region Public ServiceProvider Property

		public IServiceProvider ServiceProvider
		{
			get
			{
				return this.serviceProvider;
			}
		}

		#endregion

		#region Public RootDirectory Property

		public string RootDirectory
		{
			get;
			set;
		}

		#endregion

		#region Internal Root Directory Path Property

		internal string RootDirectoryFullPath
		{
			get
			{
				if (Path.IsPathRooted(RootDirectory))
				{
					return RootDirectory;
				}
				return Path.Combine(TitleContainer.Location, RootDirectory);
			}
		}

		#endregion

		#region Private Variables

		private IServiceProvider serviceProvider;
		private IGraphicsDeviceService graphicsDeviceService;
		private Dictionary<string, object> loadedAssets = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
		private List<IDisposable> disposableAssets = new List<IDisposable>();
		private bool disposed;

		private static object ContentManagerLock = new object();
		private static List<WeakReference> ContentManagers = new List<WeakReference>();

		private static List<char> targetPlatformIdentifiers = new List<char>()
		{
			'w', // Windows (DirectX)
			'x', // Xbox360
			'm', // WindowsPhone
			'i', // iOS
			'a', // Android
			'd', // DesktopGL
			'X', // MacOSX
			'W', // WindowsStoreApp
			'n', // NativeClient
			'u', // Ouya
			'p', // PlayStationMobile
			'M', // WindowsPhone8
			'r', // RaspberryPi
			'P', // Playstation 4
			'g', // WindowsGL (deprecated for DesktopGL)
			'l', // Linux (deprecated for DesktopGL)
		};

		#endregion

		#region Private Static Methods

		private static void AddContentManager(ContentManager contentManager)
		{
			lock (ContentManagerLock)
			{
				/* Check if the list contains this content manager already. Also take
				 * the opportunity to prune the list of any finalized content managers.
				 */
				bool contains = false;
				for (int i = ContentManagers.Count - 1; i >= 0; i -= 1)
				{
					WeakReference contentRef = ContentManagers[i];
					if (ReferenceEquals(contentRef.Target, contentManager))
					{
						contains = true;
					}
					if (!contentRef.IsAlive)
					{
						ContentManagers.RemoveAt(i);
					}
				}
				if (!contains)
				{
					ContentManagers.Add(new WeakReference(contentManager));
				}
			}
		}

		private static void RemoveContentManager(ContentManager contentManager)
		{
			lock (ContentManagerLock)
			{
				/* Check if the list contains this content manager and remove it. Also
				 * take the opportunity to prune the list of any finalized content managers.
				 */
				for (int i = ContentManagers.Count - 1; i >= 0; i -= 1)
				{
					WeakReference contentRef = ContentManagers[i];
					if (!contentRef.IsAlive || ReferenceEquals(contentRef.Target, contentManager))
					{
						ContentManagers.RemoveAt(i);
					}
				}
			}
		}

		#endregion

		#region Internal Static Methods

		internal static void ReloadGraphicsContent()
		{
			lock (ContentManagerLock)
			{
				/* Reload the graphic assets of each content manager. Also take the
				 * opportunity to prune the list of any finalized content managers.
				 */
				for (int i = ContentManagers.Count - 1; i >= 0; i -= 1)
				{
					WeakReference contentRef = ContentManagers[i];
					if (contentRef.IsAlive)
					{
						ContentManager contentManager = (ContentManager) contentRef.Target;
						if (contentManager != null)
						{
							contentManager.ReloadGraphicsAssets();
						}
					}
					else
					{
						ContentManagers.RemoveAt(i);
					}
				}
			}
		}

		#endregion

		#region Destructor

		/* Use C# destructor syntax for finalization code.
		 * This destructor will run only if the Dispose method
		 * does not get called.
		 * It gives your base class the opportunity to finalize.
		 * Do not provide destructors in types derived from this class.
		 */
		~ContentManager()
		{
			/* Do not re-create Dispose clean-up code here.
			 * Calling Dispose(false) is optimal in terms of
			 * readability and maintainability.
			 */
			Dispose(false);
		}

		#endregion

		#region Public Constructors

		public ContentManager(IServiceProvider serviceProvider)
		{
			if (serviceProvider == null)
			{
				throw new ArgumentNullException("serviceProvider");
			}
			this.RootDirectory = string.Empty;
			this.serviceProvider = serviceProvider;
			AddContentManager(this);
		}

		public ContentManager(IServiceProvider serviceProvider, string rootDirectory)
		{
			if (serviceProvider == null)
			{
				throw new ArgumentNullException("serviceProvider");
			}
			if (rootDirectory == null)
			{
				throw new ArgumentNullException("rootDirectory");
			}
			this.RootDirectory = rootDirectory;
			this.serviceProvider = serviceProvider;
			AddContentManager(this);
		}

		#endregion

		#region Public Load Method

		public virtual T Load<T>(string assetName)
		{
			if (string.IsNullOrEmpty(assetName))
			{
				throw new ArgumentNullException("assetName");
			}
			if (disposed)
			{
				throw new ObjectDisposedException("ContentManager");
			}
			T result = default(T);

			/* On some platforms, name and slash direction matter.
			 * We store the asset by a /-separating key rather than
			 * how the path to the file was passed to us to avoid
			 * loading "content/asset1.xnb" and "content\\ASSET1.xnb"
			 * as if they were two different files. this matches
			 * stock XNA behavior. The Dictionary will ignore case
			 * differences.
			 */
			string key = assetName.Replace('\\', '/');

			// Check for a previously loaded asset first
			object asset = null;
			if (loadedAssets.TryGetValue(key, out asset))
			{
				if (asset is T)
				{
					return (T) asset;
				}
			}
			// Load the asset.
			result = ReadAsset<T>(assetName, null);
			loadedAssets[key] = result;
			return result;
		}

		#endregion

		#region Public Dispose Method

		public void Dispose()
		{
			Dispose(true);
			/* Tell the garbage collector not to call the finalizer
			 * since all the cleanup will already be done.
			 */
			GC.SuppressFinalize(this);
			// Once disposed, content manager wont be used again
			RemoveContentManager(this);
		}

		#endregion

		#region Public Unload Method

		public virtual void Unload()
		{
			// Look for disposable assets.
			foreach (IDisposable disposable in disposableAssets)
			{
				if (disposable != null)
				{
					disposable.Dispose();
				}
			}
			disposableAssets.Clear();
			loadedAssets.Clear();
		}

		#endregion

		#region Internal RecordDisposable Method

		internal void RecordDisposable(IDisposable disposable)
		{
			Debug.Assert(disposable != null, "The disposable is null!");

			/* Avoid recording disposable objects twice. ReloadAsset will try to record
			 * the disposables again. We don't know which asset recorded which
			 * disposable so just guard against storing multiple of the same instance.
			 */
			if (!disposableAssets.Contains(disposable))
			{
				disposableAssets.Add(disposable);
			}
		}

		#endregion

		#region Protected Dispose Method

		/* If disposing is true, it was called explicitly and we should dispose managed
		 * objects. If disposing is false, it was called by the finalizer and managed
		 * objects should not be disposed.
		 */
		protected virtual void Dispose(bool disposing)
		{
			if (!disposed)
			{
				if (disposing)
				{
					Unload();
				}
				disposed = true;
			}
		}

		#endregion

		#region Protected OpenStream Method

		protected virtual Stream OpenStream(string assetName)
		{
			Stream stream;
			try
			{
				string assetPath = FileHelpers.NormalizeFilePathSeparators(
					Path.Combine(RootDirectoryFullPath, assetName) + ".xnb"
				);
				stream = File.OpenRead(assetPath);

			}
			catch (FileNotFoundException fileNotFound)
			{
				throw new ContentLoadException("The content file was not found.", fileNotFound);
			}
			catch (DirectoryNotFoundException directoryNotFound)
			{
				throw new ContentLoadException("The directory was not found.", directoryNotFound);
			}
			catch (Exception exception)
			{
				throw new ContentLoadException("Opening stream error.", exception);
			}
			return stream;
		}

		#endregion

		#region Protected ReadAsset Method

		protected T ReadAsset<T>(string assetName, Action<IDisposable> recordDisposableObject)
		{
			if (string.IsNullOrEmpty(assetName))
			{
				throw new ArgumentNullException("assetName");
			}
			if (disposed)
			{
				throw new ObjectDisposedException("ContentManager");
			}
			string originalAssetName = assetName;
			object result = null;
			if (this.graphicsDeviceService == null)
			{
				this.graphicsDeviceService = serviceProvider.GetService(typeof(IGraphicsDeviceService)) as IGraphicsDeviceService;
				if (this.graphicsDeviceService == null)
				{
					throw new InvalidOperationException("No Graphics Device Service");
				}
			}
			Stream stream = null;
			try
			{
				// Try to load it traditionally
				stream = OpenStream(assetName);
				// Try to load as XNB file
				try
				{
					using (BinaryReader xnbReader = new BinaryReader(stream))
					{
						using (ContentReader reader = GetContentReaderFromXnb(assetName, ref stream, xnbReader, recordDisposableObject))
						{
							result = reader.ReadAsset<T>();
							GraphicsResource resource = result as GraphicsResource;
							if (resource != null)
							{
								resource.Name = originalAssetName;
							}
						}
					}
				}
				finally
				{
					if (stream != null)
					{
						stream.Dispose();
					}
				}
			}
			catch (ContentLoadException ex)
			{
				// Try to load as a non-content file
				assetName = FileHelpers.NormalizeFilePathSeparators(
					Path.Combine(RootDirectoryFullPath, assetName)
				);
				assetName = Normalize<T>(assetName);
				if (string.IsNullOrEmpty(assetName))
				{
					throw new ContentLoadException(
						"Could not load " +
						originalAssetName +
						" asset as a non-content file!",
						ex
					);
				}
				result = ReadRawAsset<T>(assetName, originalAssetName);
				/* Because Raw Assets skip the ContentReader step, they need to have their
				 * disposables recorded here. Doing it outside of this catch will
				 * result in disposables being logged twice.
				 */
				IDisposable disposableResult = result as IDisposable;

				if (disposableResult != null)
				{
					if (recordDisposableObject != null)
					{
						recordDisposableObject(disposableResult);
					}
					else
					{
						disposableAssets.Add(disposableResult);
					}
				}
			}

			if (result == null)
			{
				throw new ContentLoadException("Could not load " + originalAssetName + " asset!");
			}

			return (T) result;
		}

		#endregion

		#region Protected Filename Normalizer Method

		protected virtual string Normalize<T>(string assetName)
		{
			if (typeof(T) == typeof(Texture2D) || typeof(T) == typeof(Texture))
			{
				return Texture2DReader.Normalize(assetName);
			}
			else if ((typeof(T) == typeof(SpriteFont)))
			{
				return SpriteFontReader.Normalize(assetName);
			}
			else if ((typeof(T) == typeof(Song)))
			{
				return SongReader.Normalize(assetName);
			}
			else if ((typeof(T) == typeof(SoundEffect)))
			{
				return SoundEffectReader.Normalize(assetName);
			}
			else if ((typeof(T) == typeof(Video)))
			{
				return VideoReader.Normalize(assetName);
			}
			else if ((typeof(T) == typeof(Effect)))
			{
				return EffectReader.Normalize(assetName);
			}
			return null;
		}

		#endregion

		#region Protected ReadRawAsset Method

		protected virtual object ReadRawAsset<T>(string assetName, string originalAssetName)
		{
			if (typeof(T) == typeof(Texture2D) || typeof(T) == typeof(Texture))
			{
				using (Stream assetStream = File.OpenRead(assetName))
				{
					Texture2D texture = Texture2D.FromStream(
						graphicsDeviceService.GraphicsDevice,
						assetStream
					);
					texture.Name = originalAssetName;
					return texture;
				}
			}
			else if ((typeof(T) == typeof(SpriteFont)))
			{
				throw new NotImplementedException();
			}
			else if ((typeof(T) == typeof(Song)))
			{
				return new Song(assetName);
			}
			else if ((typeof(T) == typeof(SoundEffect)))
			{
				using (Stream s = File.OpenRead(assetName))
				{
					return SoundEffect.FromStream(s);
				}
			}
			else if ((typeof(T) == typeof(Video)))
			{
				return new Video(assetName);
			}
			else if ((typeof(T) == typeof(Effect)))
			{
				using (Stream assetStream = File.OpenRead(assetName))
				{
					byte[] data = new byte[assetStream.Length];
					assetStream.Read(data, 0, (int) assetStream.Length);
					return new Effect(this.graphicsDeviceService.GraphicsDevice, data);
				}
			}
			return null;
		}

		#endregion

		#region Protected LoadedAssets Property

		/// <summary>
		/// Virtual property to allow a derived ContentManager to have it's assets reloaded
		/// </summary>
		protected virtual Dictionary<string, object> LoadedAssets
		{
			get
			{
				return loadedAssets;
			}
		}

		#endregion

		#region Protected Asset Reloading Methods

		protected virtual void ReloadGraphicsAssets()
		{
			foreach (KeyValuePair<string, object> asset in LoadedAssets)
			{
				/* This never executes as asset.Key is never null. This just forces the
				 * linker to include the ReloadAsset function when AOT compiled.
				 */
				if (asset.Key == null)
				{
					ReloadAsset(asset.Key, Convert.ChangeType(asset.Value, asset.Value.GetType()));
				}

				MethodInfo methodInfo = typeof(ContentManager).GetMethod("ReloadAsset", BindingFlags.NonPublic | BindingFlags.Instance);
				MethodInfo genericMethod = methodInfo.MakeGenericMethod(asset.Value.GetType());
				genericMethod.Invoke(this, new object[] { asset.Key, Convert.ChangeType(asset.Value, asset.Value.GetType()) });
			}
		}

		protected virtual void ReloadAsset<T>(string originalAssetName, T currentAsset)
		{
			string assetName = originalAssetName;
			if (string.IsNullOrEmpty(assetName))
			{
				throw new ArgumentNullException("assetName");
			}
			if (disposed)
			{
				throw new ObjectDisposedException("ContentManager");
			}

			if (this.graphicsDeviceService == null)
			{
				this.graphicsDeviceService = serviceProvider.GetService(typeof(IGraphicsDeviceService)) as IGraphicsDeviceService;
				if (this.graphicsDeviceService == null)
				{
					throw new InvalidOperationException("No Graphics Device Service");
				}
			}

			Stream stream = null;
			try
			{
				// Try to load it traditionally
				stream = OpenStream(assetName);
				// Try to load as XNB file
				try
				{
					using (BinaryReader xnbReader = new BinaryReader(stream))
					{
						using (ContentReader reader = GetContentReaderFromXnb(assetName, ref stream, xnbReader, null))
						{
							reader.InitializeTypeReaders();
							reader.ReadObject<T>(currentAsset);
							reader.ReadSharedResources();
						}
					}
				}
				finally
				{
					if (stream != null)
					{
						stream.Dispose();
					}
				}
			}
			catch (ContentLoadException)
			{
				// Try to reload as a non-xnb file.
				assetName = FileHelpers.NormalizeFilePathSeparators(
					Path.Combine(RootDirectoryFullPath, assetName)
				);
				assetName = Normalize<T>(assetName);
				ReloadRawAsset(currentAsset, assetName, originalAssetName);
			}
		}

		protected virtual void ReloadRawAsset<T>(T asset, string assetName, string originalAssetName)
		{
			// FIXME: Is this needed? -flibit
		}

		#endregion

		#region Private GetContentReaderFromXnb Method

		private ContentReader GetContentReaderFromXnb(string originalAssetName, ref Stream stream, BinaryReader xnbReader, Action<IDisposable> recordDisposableObject)
		{
			// The first 4 bytes should be the "XNB" header.
			byte x = xnbReader.ReadByte();
			byte n = xnbReader.ReadByte();
			byte b = xnbReader.ReadByte();
			byte platform = xnbReader.ReadByte();
			if (	x != 'X' || n != 'N' || b != 'B' ||
				!(targetPlatformIdentifiers.Contains((char) platform)) )
			{
				throw new ContentLoadException("Asset does not appear to be a valid XNB file. Did you process your content for Windows?");
			}
			byte version = xnbReader.ReadByte();
			byte flags = xnbReader.ReadByte();
			bool compressed = (flags & 0x80) != 0;
			if (version != 5 && version != 4)
			{
				throw new ContentLoadException("Invalid XNB version");
			}
			// The next int32 is the length of the XNB file
			int xnbLength = xnbReader.ReadInt32();
			ContentReader reader;
			if (compressed)
			{
				/* Decompress the XNB
				 * Thanks to ShinAli (https://bitbucket.org/alisci01/xnbdecompressor)
				 */
				int compressedSize = xnbLength - 14;
				int decompressedSize = xnbReader.ReadInt32();
				MemoryStream decompressedStream = new MemoryStream(decompressedSize);
				// Default window size for XNB encoded files is 64Kb (need 16 bits to represent it)
				LzxDecoder dec = new LzxDecoder(16);
				int decodedBytes = 0;
				long startPos = stream.Position;
				long pos = startPos;

				while (pos - startPos < compressedSize)
				{
					/* The compressed stream is separated into blocks that will
					 * decompress into 32kB or some other size if specified.
					 * Normal, 32kB output blocks will have a short indicating
					 * the size of the block before the block starts. Blocks
					 * that have a defined output will be preceded by a byte of
					 * value 0xFF (255), then a short indicating the output size
					 * and another for the block size. All shorts for these
					 * cases are encoded in big endian order.
					 */
					int hi = stream.ReadByte();
					int lo = stream.ReadByte();
					int block_size = (hi << 8) | lo;
					int frame_size = 0x8000; // Frame size is 32kB by default
					// Does this block define a frame size?
					if (hi == 0xFF)
					{
						hi = lo;
						lo = (byte) stream.ReadByte();
						frame_size = (hi << 8) | lo;
						hi = (byte) stream.ReadByte();
						lo = (byte) stream.ReadByte();
						block_size = (hi << 8) | lo;
						pos += 5;
					}
					else
					{
						pos += 2;
					}
					// Either says there is nothing to decode
					if (block_size == 0 || frame_size == 0)
					{
						break;
					}
					dec.Decompress(stream, block_size, decompressedStream, frame_size);
					pos += block_size;
					decodedBytes += frame_size;
					/* Reset the position of the input just in case the bit
					 * buffer read in some unused bytes.
					 */
					stream.Seek(pos, SeekOrigin.Begin);
				}
				if (decompressedStream.Position != decompressedSize)
				{
					throw new ContentLoadException(
						"Decompression of " + originalAssetName + " failed. "
					);
				}
				decompressedStream.Seek(0, SeekOrigin.Begin);
				reader = new ContentReader(
					this,
					decompressedStream,
					this.graphicsDeviceService.GraphicsDevice,
					originalAssetName,
					version,
					recordDisposableObject
				);
			}
			else
			{
				reader = new ContentReader(
					this,
					stream,
					this.graphicsDeviceService.GraphicsDevice,
					originalAssetName,
					version,
					recordDisposableObject
				);
			}
			return reader;
		}

		#endregion
	}
}
