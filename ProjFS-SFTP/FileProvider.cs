using System;
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

		public FileProvider(ConnectionInfo conInfo, string virtualizationPath) {
			ConnectionInfo = conInfo;
			VirtualizationPath = virtualizationPath;
			SftpClient = new SftpClient(ConnectionInfo);
		}

		public bool InitProjection() {
			Stop();

			try {
				SftpClient.Connect();
			} catch(Exception e) {
				MessageBox.Show($"Failed to connect:\n{e.Message}\n{e.StackTrace}");
				return false;
			}

			SftpRootPath = SftpClient.WorkingDirectory;

			try {
				virtualization = new VirtualizationInstance(VirtualizationPath, 0, 0, false, new NotificationMapping[0]);
			} catch(Exception e) {
				MessageBox.Show($"Failed to create virtualization instance:\n{e.Message}\n{e.StackTrace}");
				return false;
			}
			return true;
		}

		public void Stop() {
			if(!(SftpClient is null) && SftpClient.IsConnected)
				SftpClient.Disconnect();
			if(!(virtualization is null))
				try { virtualization.StopVirtualizing(); } catch {}
		}

		public void Dispose() {
			if(!(SftpClient is null))
				SftpClient.Dispose();
		}
	}
}
