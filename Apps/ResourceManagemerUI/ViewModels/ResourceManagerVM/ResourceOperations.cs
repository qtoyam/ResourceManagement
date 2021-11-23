using System;

using MvvmGen;

namespace ResourceManagerUI.ViewModels
{
	partial class ResourceManagerVM
	{
		[Property] private ResourceItemVM? _editableResource = null;

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

		[Command]
		private void SelectResourcePath()
		{
			if (MessageService.TryGetFile(out var openpath))
			{
				EditableResource!.Path = openpath;
			}
		}

		#region New resource
		[Command(CanExecuteMethod = nameof(CanAddResource))]
		private void BeginAddResource() => CurrentState = CurrentState.NewResource;

		private bool CanAddResource() => CurrentState == CurrentState.None || CurrentState == CurrentState.SelectResource;
		#endregion //New resource

		#region Edit current resource
		[Command(CanExecuteMethod = nameof(CanEditResource))]
		private void BeginEditResource() => CurrentState = CurrentState.EditResource;

		private bool CanEditResource() => CurrentState == CurrentState.SelectResource;
		#endregion //Edit current resource

		[Command]
		private void EndEditResource()
		{
			if (CurrentState == CurrentState.NewResource)
			{
				Resources.Add(EditableResource!);
			}
			else if (CurrentState == CurrentState.EditResource)
			{
				Resources[_selectedResourceIndex] = ResourceItemVM.SmartCompareUpdate(Resources[_selectedResourceIndex], _editableResource!, out var updated);
				if (updated) Unsaved = true;
			}
			else MessageService.SendException(new InvalidOperationException(nameof(EndEditResource)));
			CurrentState = CurrentState.None;
		}

		#region remove resource
		[Command(CanExecuteMethod = nameof(CanRemoveResource))]
		private void RemoveResource()
		{
			Resources[_selectedResourceIndex].ClearCache();
			Resources.RemoveAt(_selectedResourceIndex);
			CurrentState = CurrentState.None;
		}
		//manual x1
		private bool CanRemoveResource() => SelectedResourceIndex != -1;
		#endregion //remove resource
	}
}
