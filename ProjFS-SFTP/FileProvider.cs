using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Windows.ProjFS;
using Renci.SshNet;

namespace ProjFS_SFTP {
	public class FileProvider : IDisposable {
		public ConnectionInfo ConnectionInfo { get; }
		public DirectoryInfo VirtualizationDirectory { get; }
		public string SftpRootPath { get; private set; }

		private SftpClient SftpClient { get; }
		private VirtualizationInstance _virtInstance;
		private RequiredCallbacks _requiredCallbacks;

		public FileProvider(ConnectionInfo conInfo, DirectoryInfo virtualizationDir) {
			ConnectionInfo = conInfo;
			VirtualizationDirectory = virtualizationDir;
			SftpClient = new SftpClient(ConnectionInfo);
		}

		public bool InitProjection() {
			Stop();

			try {
				SftpClient.Connect();
			} catch(Exception e) {
				MessageBox.Show($"{e.Message}\n{e.StackTrace}", "Failed to connect to sftp server");
				return false;
			}

			SftpRootPath = SftpClient.WorkingDirectory;

			try {
				_virtInstance = new VirtualizationInstance(VirtualizationDirectory.FullName, 0, 0, false, new NotificationMapping[0]);
				VirtualizationDirectory.Refresh();
			} catch(Exception e) {
				MessageBox.Show($"{e.Message}\n{e.StackTrace}", "Failed to create virtualization instance");
				return false;
			}
			return true;
		}

		public bool StartProjecting() {
			_requiredCallbacks = new RequiredCallbacks(this, _virtInstance, SftpClient);
			_virtInstance.OnQueryFileName = _requiredCallbacks.QueryFilenameCallback;
			var hr = _virtInstance.StartVirtualizing(_requiredCallbacks);
			if(hr == HResult.Ok)
				Process.Start(VirtualizationDirectory.FullName);
			return hr == HResult.Ok;
		}

		public void Stop() {
			if(!(SftpClient is null) && SftpClient.IsConnected)
				SftpClient.Disconnect();
			if(!(_virtInstance is null))
				try { _virtInstance.StopVirtualizing(); } catch { }

			VirtualizationDirectory.Refresh();
			if(VirtualizationDirectory.Exists)
				VirtualizationDirectory.Delete(true);
		}

		public void Dispose() {
			if(!(SftpClient is null))
				SftpClient.Dispose();
		}
	}
}
