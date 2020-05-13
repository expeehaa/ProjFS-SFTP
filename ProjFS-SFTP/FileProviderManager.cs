using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Renci.SshNet;

namespace ProjFS_SFTP {
	public class FileProviderManager {
		private static readonly HashAlgorithm _hashAlgorithm = SHA256.Create();
		private static readonly ConcurrentDictionary<string, FileProvider> _openConnections = new ConcurrentDictionary<string, FileProvider>();
		private static readonly List<string> _openInitiations = new List<string>();

		public static bool CreateFileProvider(string hostname, string username, string password, int port = 22) {
			var hash = CreateStringHash(hostname, username);
			if(_openInitiations.Contains(hash)) {
				MessageBox.Show($"Connection to {username}@{hostname} is already initializing!");
				return false;
			}
			if(_openConnections.ContainsKey(hash)) {
				MessageBox.Show($"Connection to {username}@{hostname} already exists!");
				return false;
			}
			_openInitiations.Add(hash);

			var rootDir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "ProjFS-SFTP", Path.GetFileNameWithoutExtension(Path.GetRandomFileName())));

			SftpClient sftpClient;
			try {
				sftpClient = new SftpClient(new ConnectionInfo(hostname, port, username, new PasswordAuthenticationMethod(username, password)));
			} catch(Exception ex) {
				MessageBox.Show($"{ex.Message}\n{ex.StackTrace}", "Failed to create connection");
				return false;
			}

			var fileProvider = new FileProvider(sftpClient, rootDir);
			Task.Run(() => {
				if(fileProvider.InitProjection() && fileProvider.StartProjecting()) {
					_openConnections.TryAdd(hash, fileProvider);
				}
				_openInitiations.Remove(hash);
			});

			return true;
		}

		public static void StopAllFileProviders() {
			foreach(var provider in _openConnections.Values) {
				provider.Stop();
			}
		}

		private static string CreateStringHash(params string[] strings)
			=> Encoding.UTF8.GetString(_hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(string.Join("", strings))));
	}
}
