using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

using MvvmGen;

using ResourceManagerUI.Models;

using RMWriter;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Threading;
using WPFCoreEx.Services;
using System.Text;
using ResourceManagerUI.Core;
using WPFCoreEx.Abstractions.Services;

namespace ResourceManagerUI.ViewModels
{
	public enum CurrentState
	{
		None,
		NewResource,
		EditResource,
		SelectResource
	}

	[Inject(typeof(IMessageService))]
	[ViewModel]
	public sealed partial class ResourceManagerVM : IDisposable
	{
		private bool _unsaved = false;
		public bool Unsaved
		{
			get => _unsaved;
			set
			{
				if (_unsaved != value)
				{
					_unsaved = value;
					OnPropertyChanged(nameof(Unsaved));
				}
			}
		}

		//TODO: title

		partial void OnInitialize()
		{
#if DEBUG
			//Resources.Add(new(@"C:\Users\q2alt\Documents\itachiMoonBg.png"));
			//Resources.Add(new(@"C:\Users\q2alt\Documents\recorded_ 2021-09-28 at 02h30m33s.wav"));
			//Resources.Add(new(@"D:\pics\icons\8b535ddb731de210ea22241e0bb30566.png"));
			//Resources.Add(new(@"C:\Users\q2alt\Documents\11729.jpg"));
			////foreach (var fi in Directory.EnumerateFiles(@"D:\pics", "*.jpg"))
			//{
			//	//Resources.Add(new(fi));
			//}
			////SelectedResourceIndex = 0;
			////SelectedResource = Resources[0];
#endif
			Resources.CollectionChanged += Resources_CollectionChanged;
		}

		[Property] private bool _free = true;

		public ObservableCollection<ResourceItemVM> Resources { get; private set; } = new();

		//private readonly WorkQueue _workQueue = new();

		private int _selectedResourceIndex = -1;
		public int SelectedResourceIndex
		{
			get => _selectedResourceIndex;
			set
			{
				if (_selectedResourceIndex != value)
				{
					_selectedResourceIndex = value;
					OnPropertyChanged(nameof(SelectedResourceIndex));
					RemoveResourceCommand.RaiseCanExecuteChanged();
					if (_selectedResourceIndex != -1)
					{
						CurrentState = CurrentState.SelectResource;
					}
				}
			}
		}
		[Property] private ResourceItemVM _selectedResource = null!;

		#region Current state
		private CurrentState _currentState = CurrentState.None;
		public CurrentState CurrentState
		{
			get => _currentState;
			private set
			{
				if (value != _currentState)
				{
					_currentState = value;
					OnPropertyChanged();
					BeginAddResourceCommand.RaiseCanExecuteChanged();
					BeginEditResourceCommand.RaiseCanExecuteChanged();
					BuildAsyncCommand.RaiseCanExecuteChanged();
				}
				HandleCurrentState();
			}
		}
		private void HandleCurrentState()
		{
			switch (_currentState)
			{
				case CurrentState.None:
					SelectedResourceIndex = -1;
					EditableResource = null;
					break;
				case CurrentState.NewResource:
					SelectedResourceIndex = -1;
					EditableResource = new();
					break;
				case CurrentState.EditResource:
					EditableResource = Resources[_selectedResourceIndex].DeepCopy();
					break;
				case CurrentState.SelectResource:
					EditableResource = null;
					if (_autoPreview) TryPreviewResource();
					break;
				default:
					break;
			}
		}
		#endregion //Current state

		[Command]
		private void SetDefaultState() => CurrentState = CurrentState.None;

		private bool NotEmpty() => Resources.Count > 0;
		private void Resources_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			BuildAsyncCommand.RaiseCanExecuteChanged();
			RefreshResourcesCommand.RaiseCanExecuteChanged();
			SaveAsyncCommand.RaiseCanExecuteChanged();
			SaveAsAsyncCommand.RaiseCanExecuteChanged();
		}
		#region build, refresh
		[Command(CanExecuteMethod = nameof(CanBuildAsync))]
		private async Task BuildAsync()
		{
			Free = false;
			try
			{
				var resources_copy = new List<FileInfo>(Resources.Count);
				foreach (var r in Resources.Where(x => x.Index.HasValue))
				{
					if (string.IsNullOrEmpty(r.Path))
					{
						MessageService.SendMessage($"No path specified, resource [{r.Index}]");
						return;
					}
					try
					{
						var f = new FileInfo(r.Path);
						if (!f.Exists)
						{
							MessageService.SendMessage($"File doesnt exist, resource[{r.Index}]");
							return;
						}
						resources_copy[r.Index!.Value] = f;
					}
					catch (Exception ex)
					{
						MessageService.SendMessage(ex.Message);
						return;
					}
				}
				if(resources_copy.Count == 0)
				{
					MessageService.SendMessage("No resources to save!");
					return;
				}
				if (MessageService.TryGetSaveFilePath(out var saveFilePath, extension: "qrm", action: "Build resource"))
				{
					try
					{
						await ResourcePacker.SaveToSlim(saveFilePath, resources_copy);
						MessageService.SendSilentMessage($"Resources built to {saveFilePath}");
					}
					catch (Exception ex)
					{
						MessageService.SendMessage(ex.Message);
					}
				}
			}
			finally
			{
				Free = true;
			}
		}

		private bool CanBuildAsync() => NotEmpty() && (CurrentState == CurrentState.None || CurrentState == CurrentState.SelectResource);

		[Command(CanExecuteMethod = nameof(NotEmpty))]
		private void RefreshResources()
		{
			Free = false;
			CurrentState = CurrentState.None;
			ClearResourcesCache();
			foreach (var r in Resources)
			{
				r.Refresh();
			}
			Free = true;
		}
		#endregion //build, refresh

		#region Preview resource
		private bool _autoPreview = false;
		public bool AutoPreview
		{
			get => _autoPreview;
			set
			{
				if (value != _autoPreview)
				{
					_autoPreview = value;
					OnPropertyChanged();
					if (_autoPreview && _selectedResourceIndex != -1) //auto preview current resource(if selected)
					{
						TryPreviewResource();
					}
				}
			}
		}

		private readonly LinkedList<ResourceItemVM> _resourcesCache = new();
		private const int MAX_CACHE = 10; //TODO: change cache limit to bytes

		[Command]
		private void TryPreviewResource()
		{
			var r = Resources[_selectedResourceIndex];
			if (!r.IsContentNeedBeLoaded()) return;
			Free = false;
			//await _workQueue.Enqueue(() =>
			//{
			if (r.TryLoadCache())
			{
				CacheResource(r);
			}
			//});
			Free = true;
		}
		#endregion //Preview resource

		private void ClearResourcesCache()
		{
			var rc = _resourcesCache.First;
			while (rc != null)
			{
				rc.Value.ClearCache();
				_resourcesCache.RemoveFirst();
				rc = rc.Next;
			}
		}

		private void CacheResource(ResourceItemVM resourceItem)
		{
			_resourcesCache.AddLast(resourceItem);
			if (_resourcesCache.Count > MAX_CACHE)
			{
				_resourcesCache.First!.Value.ClearCache();
				_resourcesCache.RemoveFirst();
			}
		}

		public void Dispose()
		{
			Resources.CollectionChanged -= Resources_CollectionChanged;
			ClearResourcesCache();
			//_workQueue.Dispose();
		}
	}
}
