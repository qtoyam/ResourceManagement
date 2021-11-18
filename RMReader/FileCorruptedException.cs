using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMReader
{
	public class FileCorruptedException : Exception
	{
		public FileCorruptedException(string file) : base($"File {file} corrupted.") { }
	}
}
