using System;
using System.IO;

using WPFCoreEx.Bases;
using ResourceManagerUI.Helpers;
using System.Text;
using System.Text.Json.Serialization;
using ResourceManagerUI.Models;

namespace ResourceManagerUI.ViewModels
{
	public class ResourceItemVM : NotifyPropBase, IResourceItem
	{
		private FileInfo? _file = null;
		[JsonIgnore]
		public ContentCacheVM ContentCache { get; } = new();

		private string? _name = null;
		public string? Name
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

		private int _index = -1;
		public int Index
		{
			get => _index;
			set
			{
				if (_index != value)
				{
					_index = value;
					OnPropertyChanged(nameof(Index));
				}
			}
		}

		public string? Path
		{
			get => _file?.FullName;
			set
			{
				if (value != Path)
				{
					_file = value == null ? null : new(value);
					UpdateData();
					OnPropertyChanged(nameof(Path));
				}
			}
		}
		public long Size => _file?.Length ?? -1;

		private string _formattedSize = string.Empty;
		public string FormattedSize
		{
			get => _formattedSize;
			private set
			{
				if (value != _formattedSize)
				{
					_formattedSize = value;
					OnPropertyChanged(nameof(FormattedSize));
				}
			}
		}

		private void UpdateData()
		{
			FormattedSize = FileHelper.NormalizeSize(_file!.Exists ? _file.Length : -1);
			OnPropertyChanged(nameof(Size)); //update cauze might change in size

			if (Name == string.Empty)
			{
				Name = _file.Name;
			}

			ClearCache();
		}

		/// <summary>
		/// Refreshes file state and clears cache.
		/// </summary>
		/// <returns></returns>
		public bool Refresh()
		{
			if (_file == null) return false;
			_file.Refresh();
			UpdateData();
			return _file.Exists;
		}

		public bool TryLoadCache() => ContentCache.TryLoad(Path);

		public void ClearCache() => ContentCache.Clear();

		public bool IsContentNeedBeLoaded() => ContentCache.ContentType == ContentType.None;

		public ResourceItemVM DeepCopy()
		{
			var res = new ResourceItemVM()
			{
				Name = this.Name,
				Index = this.Index
			};
			res.Path = this.Path;
			return res;
		}

		public static ResourceItemVM SmartCompareExchange(ResourceItemVM originalResource, ResourceItemVM newResource)
		{
			if (newResource.Path == originalResource.Path)
			{
				originalResource.Name = newResource.Name;
				originalResource.Index = newResource.Index;
				newResource.ClearCache();
				return originalResource;
			}
			originalResource.ClearCache();
			return newResource;
		}
	}
}
