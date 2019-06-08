using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Windows.ProjFS;
using Renci.SshNet;

namespace ProjFS_SFTP {
	public class FileProvider : IDisposable {
		public ConnectionInfo ConnectionInfo { get; }
		public string VirtualizationPath { get; }
		public string SftpRootPath { get; private set; }

		private SftpClient SftpClient { get; }
		private VirtualizationInstance virtualization;
		private RequiredCallbacks requiredCallbacks;

		public FileProvider(ConnectionInfo conInfo, string virtualizationPath) {
			ConnectionInfo = conInfo;
			VirtualizationPath = virtualizationPath;
			SftpClient = new SftpClient(ConnectionInfo);
		}

		public async Task<bool> InitProjectionAsync() {
			Stop();

			var success = await ConnectSftpAsync();
			if(!success)
				return false;

			SftpRootPath = SftpClient.WorkingDirectory;

			try {
				virtualization = new VirtualizationInstance(VirtualizationPath, 0, 0, false, new NotificationMapping[0]);
			} catch(Exception e) {
				MessageBox.Show($"{e.Message}\n{e.StackTrace}", "Failed to create virtualization instance");
				return false;
			}
			return true;
		}

		public bool StartProjecting() {
			requiredCallbacks = new RequiredCallbacks(this, virtualization, SftpClient);
			virtualization.OnQueryFileName = requiredCallbacks.QueryFilenameCallback;
			var hr = virtualization.StartVirtualizing(requiredCallbacks);
			if(hr == HResult.Ok)
				Process.Start(VirtualizationPath);
			return hr == HResult.Ok;
		}

		public void Stop() {
			if(!(SftpClient is null) && SftpClient.IsConnected)
				SftpClient.Disconnect();
			if(!(virtualization is null))
				try { virtualization.StopVirtualizing(); } catch {}
		}

		private async Task<bool> ConnectSftpAsync() {
			var success = false;

			await Task.Run(() => {
				try {
					SftpClient.Connect();
					success = true;
				} catch(Exception e) {
					MessageBox.Show($"{e.Message}\n{e.StackTrace}", "Failed to connect to sftp server");
					success = false;
				}
			});
			
			return success;
		}

		public void Dispose() {
			if(!(SftpClient is null))
				SftpClient.Dispose();
		}
	}
}
