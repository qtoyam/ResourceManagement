using System;
using System.IO;

using RMWriter;

using WPFCoreEx.Bases;

namespace ResourceManagerUI.Models
{
	public enum ContentType : int
	{
		None = 0b_0,
		Image = 0b_1,
		NotSupported = 0b_1000_0000_0000_0000,
		FileNotFound = 0b_1001_0000_0000_0000
	}

	public class ResourceItem : NotifyPropBase
	{
		//public static readonly object EmptyContent = new();

		private string _name = string.Empty;
		public string Name
		{
			get => _name;
			set
			{
				if (_name != value)
				{
					_name = value;
					OnPropertyChanged();
				}
			}
		}

		private string _path = string.Empty;
		public string Path
		{
			get => _path;
			set
			{
				if (_path != value)
				{
					_path = value;
					OnPropertyChanged();
					SetNewFile(value);
				}
			}
		}

		private long? _size = null;
		public long? Size
		{
			get => _size;
			private set
			{
				if (_size != value)
				{
					_size = value;
					OnPropertyChanged();
					UpdateFormattedSize();
				}
			}
		}

		private void UpdateFormattedSize()
		{
			if (_size == null) return;
			double s = _size.Value;
			string unit = "B";
			if (s > (1 << 30))
			{
				s /= 1 << 30;
				unit = "GB";
			}
			else if (s > (1 << 20))
			{
				s /= 1 << 20;
				unit = "MB";
			}
			else if (s > (1 << 10))
			{
				s /= 1 << 10;
				unit = "KB";
			}
			FormattedSize = $"{Math.Round(s, 1)} {unit}";
		}

		private string? _formattedSize = null;
		public string? FormattedSize
		{
			get => _formattedSize;
			private set
			{
				if (value != _formattedSize)
				{
					_formattedSize = value;
					OnPropertyChanged();
				}
			}
		}

		private bool _include = false;
		public bool Include
		{
			get => _include;
			set
			{
				if (_include != value)
				{
					_include = value;
					OnPropertyChanged();
				}
			}
		}

		private object? _contentPreview;
		public object? ContentPreview
		{
			get => _contentPreview;
			private set
			{
				if (_contentPreview != value)
				{
					_contentPreview = value;
					OnPropertyChanged();
				}
			}
		}
		private ContentType _contentType;
		public ContentType ContentType
		{
			get => _contentType;
			private set
			{
				if (_contentType != value)
				{
					_contentType = value;
					OnPropertyChanged();
				}
			}
		}

		internal void SetContent(object? content, ContentType contentType)
		{
			ContentPreview = content;
			ContentType = contentType;
		}

		internal void ClearPreviewCache()
		{
			ContentPreview = null;
		}

		internal void ClearContent()
		{
			ContentPreview = null;
			ContentType = ContentType.None;
		}


#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		public ResourceItem() { }

		public ResourceItem(string path, string name = "")
		{
			Name = name;
			Path = path;
		}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

		private FileInfo _file;
		public FileInfo File => _file;
		private void SetNewFile(string value)
		{
			_file = new(value);
			RefreshFileInfo();
		}

		//TODO: check what happens when file is not allowed to read (locked)
		internal void RefreshFileInfo()
		{
			_file.Refresh();
			if (_file.Exists)
			{
				if (string.IsNullOrEmpty(Name)) Name = _file.Name;
				Size = _file.Length;
				if(_contentType == ContentType.FileNotFound)
				{
					ContentType = ContentType.None;
				}
			}
			else
			{
				Size = null;
				SetContent(null, ContentType.FileNotFound);
			}
		}

		public ResourceItem DeepCopy()
		{
			return new ResourceItem()
			{
				_file = new(this._path),
				_include = this._include,
				_name = this._name,
				_path = this._path,
				_size = this._size,
				_contentPreview = this._contentPreview,
				_contentType = this._contentType,
				_formattedSize = this._formattedSize
			};
		}

		public override string? ToString() =>
			$"Name: \"{this.Name}\", Size: {this.Size ?? -1} bytes";
	}
}
