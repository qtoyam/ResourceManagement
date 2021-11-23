using System;
using System.IO;
using System.Windows.Media.Imaging;

using WPFCoreEx.Bases;

namespace ResourceManagerUI.ViewModels
{
	public enum ContentType
	{
		None = 0,
		Image,
		NotSupported,
		FileNotFound
	}
	public class ContentCacheVM : NotifyPropBase
	{
		private ContentType _contentType = ContentType.None;
		public ContentType ContentType
		{
			get => _contentType;
			private set
			{
				if (_contentType != value)
				{
					_contentType = value;
					OnPropertyChanged(nameof(ContentType));
				}
			}
		}
		private object? _content = null;
		public object? Content
		{
			get => _content;
			private set
			{
				if (_content != value)
				{
					_content = value;
					OnPropertyChanged(nameof(Content));
				}
			}
		}

		public bool TryLoad(string? path)
		{
			Clear();
			if (string.IsNullOrEmpty(path) || !File.Exists(path))
			{
				ContentType = ContentType.FileNotFound;
				return false;
			}
			FileStream? fs = null;
			try
			{
				fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.None);
				if (TryLoadBitmapImage(fs, out var img))
				{
					Content = img;
					ContentType = ContentType.Image;
					return true;
				}
				else
				{
					ContentType = ContentType.NotSupported;
					return false;
				}
			}
			catch //file exception
			{
				ContentType = ContentType.FileNotFound;
				return false;
			}
			finally
			{
				fs?.Dispose();
			}
		}
		public void Clear()
		{
			ContentType = ContentType.None;
			if (Content != null)
			{
				if (Content is IDisposable disposable) disposable.Dispose();
				Content = null;
			}
		}

		private static bool TryLoadBitmapImage(FileStream file, out BitmapImage? result)
		{
			try
			{
				result = new BitmapImage();
				result.BeginInit();
				result.CacheOption = BitmapCacheOption.OnLoad;
				result.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
				result.StreamSource = file;
				result.EndInit();
				result.Freeze();
				return true;
			}
			catch
			{
				result = null;
				return false;
			}
		}
	}
}
