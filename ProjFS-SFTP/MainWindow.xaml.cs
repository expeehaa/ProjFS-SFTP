using System.Windows;

namespace ProjFS_SFTP {
	public partial class MainWindow : Window {
		public MainWindow() {
			InitializeComponent();
		}

		private void Button_Click(object sender, RoutedEventArgs e) {
			var hostname = boxHostname.Text;
			var username = boxUsername.Text;
			var password = boxPassword.Password;

			if(!int.TryParse(boxPort.Text, out var port)) {
				port = 22;
			}

			FileProviderManager.CreateFileProvider(hostname, username, password, port);
		}
	}
}
