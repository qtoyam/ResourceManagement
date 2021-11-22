using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using ResourceManagerUI.Services;
using ResourceManagerUI.ViewModels;

namespace ResourceManagerUI.Views
{
	/// <summary>
	/// Interaction logic for ResourceManagerWindow.xaml
	/// </summary>
	public partial class ResourceManagerWindow : Window
	{
		public ResourceManagerWindow()
		{
			InitializeComponent();
			EventMessageService msgServ = new(this);
			DataContext = new ResourceManagerVM(msgServ);
		}

		private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
		{
			if (e.Delta > 0)
				((ScrollViewer)sender).LineLeft();
			else
				((ScrollViewer)sender).LineRight();
			e.Handled = true;
		}

		protected override void OnClosed(EventArgs e)
		{
			((ResourceManagerVM)DataContext).Dispose();
			base.OnClosed(e);
		}
	}
}
