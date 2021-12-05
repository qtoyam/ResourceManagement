using System;
using System.IO;

using WPFCoreEx.Bases;
using ResourceManagerUI.Helpers;
using ResourceManagerUI.Models;

namespace ResourceManagerUI.ViewModels
{
	public class ResourceItemVM : NotifyPropBase, IResourceItem
	{
		private FileInfo? _file = null;
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

		private int? _index = null;
		public int? Index
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
					_file = string.IsNullOrEmpty(value) ? null : new(value);
					UpdateData();
					OnPropertyChanged(nameof(Path));
				}
			}
		}
		public long? Size => _file?.Exists == true ? _file.Length : null;

		public string? FormattedSize => Size.HasValue ? FileHelper.NormalizeSize(Size.Value) : null;

		private void UpdateData()
		{
			if (_file != null && Name == null)
			{
				Name = _file.Name;
			}
			OnPropertyChanged(nameof(Size)); //update cauze might change in size
			OnPropertyChanged(nameof(FormattedSize));
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

		public static ResourceItemVM SmartCompareUpdate(ResourceItemVM originalResource, ResourceItemVM newResource, out bool updated)
		{
			updated = true;
			if (newResource.Path == originalResource.Path)
			{
				if(originalResource.Name == newResource.Name && originalResource.Index == newResource.Index)
				{
					updated = false;
				}
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
