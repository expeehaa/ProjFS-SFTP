using System.Windows;

namespace ProjFS_SFTP {
	public partial class App : Application {
		private void Application_Exit(object sender, ExitEventArgs e) {
			FileProviderManager.StopAllFileProviders();
		}
	}
}
