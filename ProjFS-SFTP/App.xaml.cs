using System.Windows;

namespace ProjFS_SFTP {
	/// <summary>
	/// Interaktionslogik für "App.xaml"
	/// </summary>
	public partial class App : Application {
		private void Application_Exit(object sender, ExitEventArgs e) {
			FileProviderManager.StopAllFileProviders();
		}
	}
}
