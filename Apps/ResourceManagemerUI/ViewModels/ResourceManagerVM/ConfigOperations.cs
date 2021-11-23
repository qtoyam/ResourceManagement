using System;
using System.IO;
using System.Threading.Tasks;

using MvvmGen;

using ResourceManagerUI.Core;

namespace ResourceManagerUI.ViewModels
{
	//TODO: idk check conf await false
	partial class ResourceManagerVM
	{
		[Property] private FileInfo? _currentConfig = null;

		[Command(CanExecuteMethod = nameof(NotEmpty))]
		private async Task SaveAsync()
		{
			Free = false;
			try
			{
				await SaveCoreAsync(false);
			}
			catch (Exception ex)
			{
				MessageService.SendException(ex);
			}
			finally
			{
				Free = true;
			}
		}

		[Command(CanExecuteMethod = nameof(NotEmpty))]
		private async Task SaveAsAsync()
		{
			Free = false;
			try
			{
				await SaveCoreAsync(true);
			}
			catch(Exception ex)
			{
				MessageService.SendException(ex);
			}
			finally
			{
				Free = true;
			}
		}

		private Task SaveCoreAsync(bool forceNew)
		{
			if (CurrentConfig == null || forceNew)
			{
				if (MessageService.TryGetSaveFilePath(out var saveFile, ConfigIO.ConfigExtension, "Save config as"))
				{
					CurrentConfig = new(saveFile);
				}
				else
				{
					return Task.CompletedTask;
				}
			}
			return ConfigIO.WriteAsync(CurrentConfig.FullName, Resources);
		}

		[Command]
		private async Task LoadConfigAsync()
		{
			Free = false;
			try
			{
				if (MessageService.TryGetFile(out var cfgPath, "RM cfg", ConfigIO.ConfigExtension))
				{
					ClearResourcesCache();
					Resources.Clear();
					await ConfigIO.ReadAsync(cfgPath, Resources);
				}
			}
			catch (Exception ex)
			{
				MessageService.SendException(ex);
			}
			finally
			{
				Free = true;
			}
		}
	}
}
