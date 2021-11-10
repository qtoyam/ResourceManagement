using System.Windows;

using ResourceManagerUI.Views;

namespace ResourceManagerUI
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public App() :	base()
		{
			MainWindow = new ResourceManagerWindow();
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			MainWindow.Show();
			//var control = Application.Current.FindResource(typeof(DataGridColumnHeader));
			//using (XmlTextWriter writer = new XmlTextWriter(@"defaultTemplate.xml", System.Text.Encoding.UTF8))
			//{
			//	writer.Formatting = Formatting.Indented;
			//	XamlWriter.Save(control, writer);
			//}
			base.OnStartup(e);
		}
	}
}
