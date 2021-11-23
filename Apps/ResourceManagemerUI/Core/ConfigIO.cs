using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

using ResourceManagerUI.Models;

namespace ResourceManagerUI.Core
{
	internal static class ConfigIO
	{
		private const char Cfg_Separator = ',';
		private readonly static string[] _configExtension = new[] { "rmcfg" };

		internal static async Task WriteAsync(string path, IEnumerable<IResourceItem> resources)
		{
			await using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write,
				FileShare.Read, 0, FileOptions.Asynchronous | FileOptions.SequentialScan))
			await using (var sw = new StreamWriter(fs, Encoding.UTF8, 4096, true))
			{
				bool f = true;
				foreach (var r in resources)
				{
					if (!f) sw.Write('\n');
					else f = false;
					sw.Write(r.Index);
					sw.Write(Cfg_Separator);
					if (r.Name != null)
					{
						sw.Write('"');
						await sw.WriteAsync(r.Name);
						sw.Write('"');
					}
					sw.Write(Cfg_Separator);
					if (r.Path != null)
					{
						await sw.WriteAsync(r.Path);
					}
				}
			}
		}

		internal static async Task ReadAsync<T>(string path, ICollection<T> dest) where T : IResourceItem
		{
			await using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write,
				FileShare.Read, 0, FileOptions.Asynchronous | FileOptions.SequentialScan))
			using (var sr = new StreamReader(fs, Encoding.UTF8, false, 4096, true))
			{
				string? line;
				while((line = await sr.ReadLineAsync()) != null)
				{
					var sb = new StringBuilder(line, line.Length);
					
				}
			}
		}
	}
}
