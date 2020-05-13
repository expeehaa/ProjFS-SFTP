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
		private static readonly HashAlgorithm hashAlgorithm = SHA256.Create();
		private static readonly ConcurrentDictionary<string, FileProvider> openConnections = new ConcurrentDictionary<string, FileProvider>();
		private static readonly List<string> openInitiations = new List<string>();

		public static bool CreateFileProvider(string hostname, string username, string password, int port = 22) {
			var hash = CreateStringHash(hostname, username);
			if(openInitiations.Contains(hash)) {
				MessageBox.Show($"Connection to {username}@{hostname} is already initializing!");
				return false;
			}
			if(openConnections.ContainsKey(hash)) {
				MessageBox.Show($"Connection to {username}@{hostname} already exists!");
				return false;
			}
			openInitiations.Add(hash);

			var rootDir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "ProjFS-SFTP", Path.GetFileNameWithoutExtension(Path.GetRandomFileName())));

			ConnectionInfo conInfo;
			try {
				conInfo = new ConnectionInfo(hostname, port, username, new PasswordAuthenticationMethod(username, password));
			} catch(Exception ex) {
				MessageBox.Show($"{ex.Message}\n{ex.StackTrace}", "Failed to create connection");
				return false;
			}

			var fileProvider = new FileProvider(conInfo, rootDir);
			Task.Run(() => {
				if(fileProvider.InitProjection() && fileProvider.StartProjecting()) {
					openConnections.TryAdd(hash, fileProvider);
				}
				openInitiations.Remove(hash);
			});

			return true;
		}

		public static void StopAllFileProviders() {
			foreach(var provider in openConnections.Values) {
				provider.Stop();
			}
		}

		private static string CreateStringHash(params string[] strings)
			=> Encoding.UTF8.GetString(hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(string.Join("", strings))));
	}
}
