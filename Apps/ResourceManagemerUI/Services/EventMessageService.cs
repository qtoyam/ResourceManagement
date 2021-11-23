using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using Microsoft.Win32;

namespace ResourceManagerUI.Services
{
	internal class EventMessageService : IMessageService
	{
		private readonly Window _owner;

		public EventMessageService(Window owner)
		{
			_owner = owner;
		}

		public static EventMessageService CreateForDebug(Window owner)
		{
			var r = new EventMessageService(owner);
			r.MessageReceived = r.SilentMessageReceived = r.WarningReceived = r.ErrorReceived = (msg) => MessageBox.Show(r._owner, msg);
			r.ExceptionReceived = (ex) => MessageBox.Show(r._owner, ex.Message);
			return r;
		}

		public void SendMessage(string message) => MessageReceived?.Invoke(message);
		public void SendSilentMessage(string message) => SilentMessageReceived?.Invoke(message);
		public void SendWarning(string message) => WarningReceived?.Invoke(message);
		public void SendError(string message) => ErrorReceived?.Invoke(message);
		public void SendException(Exception ex) => ExceptionReceived?.Invoke(ex);

		public event Action<string>? SilentMessageReceived;
		public event Action<string>? MessageReceived;
		public event Action<string>? WarningReceived;
		public event Action<string>? ErrorReceived;
		public event Action<Exception>? ExceptionReceived;

		public bool TryGetFile(out string filePath, string? fileType = null, string[]? extensions = null)
		{
			var openFileDialog = new OpenFileDialog()
			{
				Multiselect = false,
				CheckFileExists = true,
				CheckPathExists = true
			};
			if (fileType != null && extensions != null)
			{
				StringBuilder sb = new StringBuilder();
				sb.Append(fileType);
				sb.Append('|');
				foreach (var extension in extensions)
				{
					sb.Append("*.");
					sb.Append(extension);
					sb.Append(';');
				}
				openFileDialog.Filter = sb.ToString();
			}
			if (openFileDialog.ShowDialog(_owner) == true)
			{
				filePath = openFileDialog.FileName;
				return true;
			}
			filePath = string.Empty;
			return false;
		}

		public bool TryGetFile(out string filePath, string fileType, string extension)
		{
			var openFileDialog = new OpenFileDialog()
			{
				Multiselect = false,
				CheckFileExists = true,
				CheckPathExists = true
			};
			StringBuilder sb = new StringBuilder();
			sb.Append(fileType);
			sb.Append("|*.");
			sb.Append(extension);

			openFileDialog.Filter = sb.ToString();
			if (openFileDialog.ShowDialog(_owner) == true)
			{
				filePath = openFileDialog.FileName;
				return true;
			}
			filePath = string.Empty;
			return false;
		}

		public bool TryGetSaveFilePath(out string saveFilePath, string extension = "", string action = "")
		{
			var saveFileDialog = new SaveFileDialog()
			{
				OverwritePrompt = true
			};
			if (!string.IsNullOrEmpty(extension))
			{
				saveFileDialog.DefaultExt = extension;
				saveFileDialog.Filter = $"(*.{extension})|*.{extension}|All files (*.*)|*.*";
			}
			if (!string.IsNullOrEmpty(action))
			{
				saveFileDialog.Title = "Build resource";
			}
			if (saveFileDialog.ShowDialog(_owner) == true)
			{
				saveFilePath = saveFileDialog.FileName;
				return true;
			}
			saveFilePath = string.Empty;
			return false;
		}
	}
}
