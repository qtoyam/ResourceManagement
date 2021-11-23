using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceManagerUI.Services
{
	public interface IMessageService
	{
		public bool TryGetSaveFilePath(out string saveFile, string extension = "", string action = "");

		public bool TryGetFile(out string filePath, string? fileType = null, string[]? extensions = null);

		public bool TryGetFile(out string filePath, string fileType, string extension);

		public void SendSilentMessage(string message);

		public void SendMessage(string message);
	}
}
