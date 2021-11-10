using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

using Microsoft.Win32;
using MvvmGen;

using ResourceManagerUI.Helpers;
using ResourceManagerUI.Models;

using RMWriter;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Threading;

namespace ResourceManagerUI.ViewModels
{
	public enum CurrentState
	{
		None = 0,
		New = 1,
		Edit = 2,
		Preview = 4
	}

	[ViewModel]
	public partial class ResourceManagerVM
	{
		[Property] private bool _windowEnabled = true;

		public ObservableCollection<ResourceItem> Resources { get; } = new();

		private ResourceItem _selectedResource = null;
		public ResourceItem SelectedResource
		{
			get { return _selectedResource; }
			set
			{
				if (_selectedResource != value)
				{
					_selectedResource = value;
					OnPropertyChanged();
					if (_selectedResource != null)
					{
						CurrentState = CurrentState.Preview;
					}
#if DEBUG
					Debug.WriteLine($"Selected resource: {_selectedResource?.Name ?? "null"}");
#endif
				}
			}
		}


		[Property]
		private int _selectedResourceIndex = -1;


		private CurrentState _currentState = CurrentState.None;
		public CurrentState CurrentState
		{
			get => _currentState;
			set
			{
				if (value != _currentState)
				{
					_currentState = value;
					OnPropertyChanged();
					HandleCurrentState();
					BeginEditResourceCommand.RaiseCanExecuteChanged();
				}
				else if (value == CurrentState.Preview) //rehandle when selected changes from x1 to x2(x1!=x2!=-1)
				{
					HandleCurrentState();
				}
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
				case CurrentState.New:
					SelectedResourceIndex = -1;
					EditableResource = new();
					break;
				case CurrentState.Edit:
					EditableResource = _selectedResource.DeepCopy();
					break;
				case CurrentState.Preview when _autoPreview:
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
					TryPreviewResource();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
					break;
				default:
					break;
			}
		}

		partial void OnInitialize()
		{
#if DEBUG
			Resources.Add(new(@"C:\Users\q2alt\Documents\itachiMoonBg.png"));
			Resources.Add(new(@"C:\Users\q2alt\Documents\recorded_ 2021-09-28 at 02h30m33s.wav"));
			Resources.Add(new(@"D:\pics\icons\8b535ddb731de210ea22241e0bb30566.png"));
			Resources.Add(new(@"C:\Users\q2alt\Documents\11729.jpg"));
			foreach (var fi in Directory.EnumerateFiles(@"D:\pics", "*.jpg"))
			{
				//Resources.Add(new(fi));
			}
			//SelectedResourceIndex = 0;
			//SelectedResource = Resources[0];
#endif
			Resources.CollectionChanged += Resources_CollectionChanged;
		}

		#region Add, edit, remove
		[Command(CanExecuteMethod = nameof(CanRemoveResource))]
		private void RemoveResource()
		{
			if (MessageBox.Show($"Are you sure you want to delete {SelectedResource.Name}?", "Warning", MessageBoxButton.YesNo)
				== MessageBoxResult.Yes)
			{
				Resources.RemoveAt(_selectedResourceIndex);
				CurrentState = CurrentState.None;
			}
		}
		[CommandInvalidate(nameof(SelectedResourceIndex))]
		private bool CanRemoveResource() => _selectedResourceIndex != -1;

		#region Editable resource
		[Property] private ResourceItem _editableResource;

		[Command]
		private void SelectPath()
		{
			var ofd = new OpenFileDialog
			{
				Multiselect = false
			};
			var res = ofd.ShowDialog();
			if (res.HasValue && res.Value)
			{
				EditableResource.Path = ofd.FileName;
			}
		}

		[Command]
		private void SaveResource()
		{
			if (CurrentState == CurrentState.New)
			{
				Resources.Add(EditableResource);
			}
			else if (CurrentState == CurrentState.Edit)
			{
				Resources[_selectedResourceIndex] = EditableResource;
			}
			CurrentState = CurrentState.None;
		}

		[Command]
		private void ClearPreview() => CurrentState = CurrentState.None;

		#region New resource
		[Command]
		private void BeginAddResource() => CurrentState = CurrentState.New;
		#endregion //New resource

		#region Edit current resource
		[Command(CanExecuteMethod = nameof(CanEditResource))]
		private void BeginEditResource() => CurrentState = CurrentState.Edit;

		private bool CanEditResource() => CurrentState == CurrentState.Preview;
		#endregion //Edit current resource

		#endregion //Editable resource


		#endregion //Add, edit, remove

		#region Save, save as, build, refresh
		[Command]
		private void Save()
		{

		}


		//TODO: save implement, save type in SFD (add)
		[Command]
		private void SaveAs()
		{
		}

		[Command(CanExecuteMethod = nameof(CheckResourcesNotEmpty))]
		private async Task BuildAsync()
		{
			WindowEnabled = false;
			var sfd = new SaveFileDialog()
			{
				AddExtension = true,
				DefaultExt = "qrm",
				OverwritePrompt = true,
				Title = "Build resource",
				Filter = "qrm files (*.qrm)|*.qrm|All files (*.*)|*.*"
			};
			var res = sfd.ShowDialog();
			if (res.HasValue && res.Value)
			{
				await ResourcePacker.SaveToAsync(sfd.FileName, Resources.Select(x => (x.Name, x.FileInfo)).ToArray());
			}
			WindowEnabled = true;
		}

		[Command(CanExecuteMethod = nameof(CheckResourcesNotEmpty))]
		private void RefreshResources()
		{
			WindowEnabled = false;
			CurrentState = CurrentState.None;
			foreach (var r in Resources)
			{
				r.ClearContent();
				r.RefreshFileInfo();
			}
			_resourcesCache.Clear();
			WindowEnabled = true;
		}

		private bool CheckResourcesNotEmpty() => Resources.Count > 0;

		private void Resources_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			RefreshResourcesCommand.RaiseCanExecuteChanged();
			BuildAsyncCommand.RaiseCanExecuteChanged();
		}
		#endregion //Save, save as, build, refresh

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
					if (_autoPreview && _selectedResource != null)
					{
						TryPreviewResource();
					}
				}
			}
		}

		private readonly LinkedList<ResourceItem> _resourcesCache = new();
		private const int MAX_CACHE = 10; //TODO: change cache limit to bytes

		[Command]
		private async Task TryPreviewResource()
		{
			WindowEnabled = false;
			_selectedResource.RefreshFileInfo();
			if ((_selectedResource.ContentType != ContentType.None && _selectedResource.ContentPreview != null) ||
				_selectedResource.ContentType == ContentType.NotSupported ||
				_selectedResource.ContentType == ContentType.FileNotFound)
			{
#if DEBUG
				Debug.WriteLine($"Content loading skipped, content type: {_selectedResource.ContentType}");
#endif
				return;
			}
			await Task.Run(() =>
			{
				using (var fs = new FileStream(_selectedResource.Path, FileMode.Open, FileAccess.Read, FileShare.None))
				{
					if (PreviewLoader.TryLoadBitmapImage(fs, out var content))
					{
						_selectedResource.SetContent(content, ContentType.Image);
						_resourcesCache.AddLast(_selectedResource);
#if DEBUG
						Debug.WriteLine($"Image resource added to cache. {_selectedResource.Name}");
#endif
					}
					else
					{
						_selectedResource.SetContent(null, ContentType.NotSupported);
#if DEBUG
						Debug.WriteLine("Content set to not supported.");
#endif
					}
				}
				if (_resourcesCache.Count > MAX_CACHE)
				{
#pragma warning disable CS8602 // Dereference of a possibly null reference.
					_resourcesCache.First.Value.ClearPreviewCache();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#if DEBUG
					Debug.WriteLine($"Resource uncached. {_resourcesCache.First.Value.Name}");
#endif
					_resourcesCache.RemoveFirst();
				}
			});
			WindowEnabled = true;
		}
		#endregion //Preview resource

		[Command]
		private void ChangeAllInclude()
		{
			if (Resources.Count > 0)
			{
				var val = !Resources[0].Include;
				foreach (var r in Resources)
				{
					r.Include = val;
				}
			}
		}

		[Command]
		private void SelectShorcut()
		{
			if (_selectedResource != null)
			{
				_selectedResource.Include = !_selectedResource.Include;
			}
		}

	}
}
