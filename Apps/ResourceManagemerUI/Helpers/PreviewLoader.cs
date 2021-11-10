using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ResourceManagerUI.Helpers
{
	internal static class PreviewLoader
	{
		internal static bool TryLoadBitmapImage(FileStream file, out BitmapImage? result)
		{
			try
			{
				result = new BitmapImage();
				result.BeginInit();
				result.CacheOption = BitmapCacheOption.OnLoad;
				result.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
				result.StreamSource = file;
				result.EndInit();
				result.Freeze();
				return true;
			}
			catch
			{
				result = null;
				return false;
			}
		}
	}
}
