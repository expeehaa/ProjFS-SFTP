using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Renci.SshNet;

namespace ProjFS_SFTP {
	/// <summary>
	/// Interaktionslogik für MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {

		private readonly HashAlgorithm hashAlgorithm = SHA256.Create();
		private readonly ConcurrentDictionary<string, FileProvider> openConnections = new ConcurrentDictionary<string, FileProvider>();

		public MainWindow() {
			InitializeComponent();
		}

		private void Button_Click(object sender, RoutedEventArgs e) {
			var hostname = boxHostname.Text;
			var username = boxUsername.Text;

			var hash = CreateStringHash(hostname, username);
			if(openConnections.ContainsKey(hash)) {
				MessageBox.Show($"Connection to {username}@{hostname} already exists!");
				return;
			}

			var password = boxPassword.Password;
			var rootdirName = Path.Combine(Path.GetTempPath(), "ProjFS-SFTP", Path.GetFileNameWithoutExtension(Path.GetRandomFileName()));

			ConnectionInfo conInfo;
			try {
				conInfo = new ConnectionInfo(hostname, username, new PasswordAuthenticationMethod(username, password));
			} catch(Exception ex) {
				MessageBox.Show($"Failed to create connection:\n{ex.Message}\n{ex.StackTrace}");
				new DirectoryInfo(rootdirName).Delete(true);
				return;
			}

			var fileProvider = new FileProvider(conInfo, rootdirName);
			if(fileProvider.InitProjection()) {
				openConnections.TryAdd(hash, fileProvider);
			}
		}

		private string CreateStringHash(params string[] strings)
			=> Encoding.UTF8.GetString(hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(string.Join("", strings))));

		private string GetAndCreateVirtualizationDirectory() {
			var tempDir = new DirectoryInfo(Path.GetTempPath());
			var projfsDir = tempDir.CreateSubdirectory("ProjFS-SFTP");
			var virtDir = projfsDir.CreateSubdirectory(Path.GetFileNameWithoutExtension(Path.GetRandomFileName()));
			return virtDir.FullName;
		}
	}
}
