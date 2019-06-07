using Renci.SshNet;

namespace ProjFS_SFTP {
	public class FileProvider {
		private readonly ConnectionInfo connectionInfo;
		private readonly string virtualizationPath;

		public FileProvider(ConnectionInfo conInfo, string virtualizationPath) {
			connectionInfo = conInfo;
			this.virtualizationPath = virtualizationPath;
		}
	}
}
