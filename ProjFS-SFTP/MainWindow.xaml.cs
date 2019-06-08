using System.Windows;

namespace ProjFS_SFTP {
	/// <summary>
	/// Interaktionslogik für MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		public MainWindow() {
			InitializeComponent();
		}

		private void Button_Click(object sender, RoutedEventArgs e) {
			var hostname = boxHostname.Text;
			var username = boxUsername.Text;
			var password = boxPassword.Password;

			FileProviderManager.CreateFileProvider(hostname, username, password);
		}
	}
}
