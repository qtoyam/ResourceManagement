using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceManagerUI.Helpers
{
	internal static class FileHelper
	{
		internal static string NormalizeSize(long size)
		{
			double s = size;
			string unit = "B";
			if (s > (1 << 30))
			{
				s /= 1 << 30;
				unit = "GB";
			}
			else if (s > (1 << 20))
			{
				s /= 1 << 20;
				unit = "MB";
			}
			else if (s > (1 << 10))
			{
				s /= 1 << 10;
				unit = "KB";
			}
			else if (s == 0)
			{
				return "Empty file";
			}
			else if (s < 0)
			{
				return "File not found";
			}
			return $"{Math.Round(s, 1)} {unit}";
		}
	}
}
