using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Microsoft.Windows.ProjFS;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using System.IO;
using System.Linq;

namespace ProjFS_SFTP {
	public class RequiredCallbacks : IRequiredCallbacks {
		private readonly FileProvider fileProvider;
		private readonly VirtualizationInstance virtualization;
		private readonly SftpClient sftpClient;
		private ConcurrentDictionary<Guid, IEnumerator<SftpFile>> _activeEnumerations = new ConcurrentDictionary<Guid, IEnumerator<SftpFile>>();

		public RequiredCallbacks(FileProvider provider, VirtualizationInstance virtualization, SftpClient client) {
			fileProvider = provider;
			this.virtualization = virtualization;
			sftpClient = client;
		}

		public HResult StartDirectoryEnumerationCallback(int commandId, Guid enumerationId, string relativePath, uint triggeringProcessId, string triggeringProcessImageFileName) {
			if(_activeEnumerations.ContainsKey(enumerationId))
				return HResult.InternalError;
			if(!sftpClient.Exists(GetFullPath(relativePath)))
				return HResult.PathNotFound;

			var fileEnumerator = sftpClient.ListDirectory(GetFullPath(relativePath)).OrderBy(s => Path.GetFileName(s.FullName)).ToList().GetEnumerator();
			_activeEnumerations.TryAdd(enumerationId, fileEnumerator);
			return HResult.Ok;
		}

		public HResult EndDirectoryEnumerationCallback(Guid enumerationId)
			=> _activeEnumerations.TryRemove(enumerationId, out var _) ? HResult.Ok : HResult.InternalError;

		public HResult GetDirectoryEnumerationCallback(int commandId, Guid enumerationId, string filterFileName, bool restartScan, IDirectoryEnumerationResults result) {
			if(!_activeEnumerations.TryGetValue(enumerationId, out var enumerator))
				return HResult.InternalError;

			if(restartScan)
				enumerator.Reset();

			var entryAdded = false;
			var hr = HResult.Ok;

			while(enumerator.MoveNext()) {
				var file = enumerator.Current;
				var filename = Path.GetFileName(file.FullName);
				if(FilenameMatchesFilter(filename, filterFileName) && result.Add(filename, file.Length, file.IsDirectory, file.IsDirectory ? FileAttributes.Directory : FileAttributes.Normal, DateTime.MinValue, file.LastAccessTime, file.LastWriteTime, file.LastWriteTime)) {
					entryAdded = true;
				} else {
					hr = entryAdded ? HResult.Ok : HResult.InsufficientBuffer;
					break;
				}
			}

			return hr;
		}

		public HResult GetFileDataCallback(int commandId, string relativePath, ulong byteOffset, uint length, Guid dataStreamId, byte[] contentId, byte[] providerId, uint triggeringProcessId, string triggeringProcessImageFileName) {
			if(string.IsNullOrWhiteSpace(relativePath)) {
				return HResult.Ok;
			} else {
				var fullPath = GetFullPath(relativePath);
				if(sftpClient.Exists(fullPath)) {
					var file = sftpClient.Get(fullPath);
					uint desiredBufferSize = (uint)Math.Min(64*1024, file.Length);

					using(var reader = sftpClient.OpenRead(fullPath)) {
						using(var writeBuffer = virtualization.CreateWriteBuffer(byteOffset, desiredBufferSize, out var alignedWriteOffset, out var alignedBufferSize)) {
							while(alignedWriteOffset < (ulong)file.Length) {
								writeBuffer.Stream.Seek(0, SeekOrigin.Begin);
								var currentBufferSize = Math.Min(desiredBufferSize, (ulong)file.Length - alignedWriteOffset);
								var buffer = new byte[currentBufferSize];
								reader.Read(buffer, 0, (int)currentBufferSize);
								writeBuffer.Stream.Write(buffer, 0, (int)currentBufferSize);

								var hr = virtualization.WriteFileData(dataStreamId, writeBuffer, alignedWriteOffset, (uint)currentBufferSize);
								if(hr != HResult.Ok)
									return HResult.InternalError;

								alignedWriteOffset += (ulong)currentBufferSize;
							}
						}
					}

					return HResult.Ok;
				} else {
					return HResult.FileNotFound;
				}
			}
		}

		public HResult GetPlaceholderInfoCallback(int commandId, string relativePath, uint triggeringProcessId, string triggeringProcessImageFileName) {
			var fullPath = GetFullPath(relativePath);
			if(!string.IsNullOrWhiteSpace(relativePath) && sftpClient.Exists(fullPath)) {
				var file = sftpClient.Get(fullPath);
				virtualization.WritePlaceholderInfo(relativePath, file.LastAccessTime, file.LastAccessTime, file.LastWriteTime, file.LastWriteTime, file.IsDirectory ? FileAttributes.Directory : FileAttributes.Normal, file.Length, file.IsDirectory, new byte[] { 0 }, new byte[] { 1 });
			}
			return HResult.Ok;
		}


		public HResult QueryFilenameCallback(string relativePath) {
			var fullPath = GetFullPath(relativePath);
			var hr = sftpClient.Exists(fullPath) && sftpClient.Get(fullPath).IsRegularFile ? HResult.Ok : HResult.FileNotFound;
			return hr;
		}


		private string GetFullPath(string relativePath) {
			var combined = Path.Combine(sftpClient.WorkingDirectory, relativePath);
			var combinedReplaced = combined.Replace('\\', '/');
			return combinedReplaced;
		}

		private bool FilenameMatchesFilter(string filename, string filter) {
			return string.IsNullOrEmpty(filter) ? true : filter == "*" ? true : Utils.IsFileNameMatch(filename, filter);
		}
	}
}
