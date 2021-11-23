using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceManagerUI.Models
{
	public interface IResourceItem
	{
		string? Name { get; set; }
		string? Path { get; set; }
		int Index { get; set; }
	}
}
