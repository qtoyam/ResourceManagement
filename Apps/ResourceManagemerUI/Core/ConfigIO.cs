using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using ResourceManagerUI.Models;

namespace ResourceManagerUI.Core
{
	internal static class ConfigIO
	{
		internal const string ConfigExtension = "rmcfg";
		private const string XML_ResourcesArray_name = "Resources";
		private const string XML_Resource_name = "Resource";

		internal static async Task WriteAsync(string path, IEnumerable<IResourceItem> resources)
		{
			bool fileCreated = false;
			try
			{
				await using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write,
					FileShare.Read, 0, FileOptions.Asynchronous | FileOptions.SequentialScan))
				{
					fileCreated = true;
					await using (var xw = XmlWriter.Create(fs, new() { Async = true, Indent = true }))
					{
						await xw.WriteStartDocumentAsync();
						await xw.WriteStartElementAsync(null, XML_ResourcesArray_name, null);
						foreach (var r in resources)
						{
							await xw.WriteStartElementAsync(null, XML_Resource_name, null);
							if (r.Index.HasValue)
							{
								await xw.WriteAttributeStringAsync(null, nameof(IResourceItem.Index), null, r.Index.Value.ToString());
							}
							if (!string.IsNullOrEmpty(r.Name))
							{
								await xw.WriteAttributeStringAsync(null, nameof(IResourceItem.Name), null, r.Name);
							}
							await xw.WriteStringAsync(r.Path);
							await xw.WriteFullEndElementAsync();
						}
						await xw.WriteFullEndElementAsync();
						await xw.WriteEndDocumentAsync();
					}
				}
			}
			catch
			{
				if (fileCreated) File.Delete(path);
				throw;
			}
		}

		internal static async Task ReadAsync<T>(string path, ICollection<T> dest) where T : IResourceItem, new()
		{
			await using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read,
				FileShare.Read, 0, FileOptions.Asynchronous | FileOptions.SequentialScan))
			using (var xr = XmlReader.Create(fs, new() { Async = true, CloseInput = false }))
			{
				xr.ReadStartElement(XML_ResourcesArray_name);
				T r;
				while (xr.IsStartElement(XML_Resource_name))
				{
					r = new();
					if (xr.MoveToAttribute("Index"))
{
						r.Index = xr.ReadContentAsInt();
					}
					if (xr.MoveToAttribute("Name"))
					{
						r.Name = await xr.ReadContentAsStringAsync();
					}
					xr.MoveToElement();
					r.Path = await xr.ReadElementContentAsStringAsync();
					dest.Add(r);
				}
				xr.ReadEndElement();
			}
		}
	}
}
