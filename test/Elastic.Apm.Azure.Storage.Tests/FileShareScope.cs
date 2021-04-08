// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Threading.Tasks;
using Azure.Storage.Files.Shares;

namespace Elastic.Apm.Azure.Storage.Tests
{
	public class FileShareScope : IAsyncDisposable
	{
		public string ShareName { get; }
		public ShareClient ShareClient { get; }

		private FileShareScope(ShareClient adminClient, string shareName)
		{
			ShareClient = adminClient;
			ShareName = shareName;
		}

		public static async Task<FileShareScope> CreateShare(string connectionString)
		{
			var shareName = Guid.NewGuid().ToString("D");
			var shareClient = new ShareClient(connectionString, shareName);
			await shareClient.CreateAsync().ConfigureAwait(false);
			return new FileShareScope(shareClient, shareName);
		}

		public async ValueTask DisposeAsync() =>
			await ShareClient.DeleteIfExistsAsync().ConfigureAwait(false);

		public static async Task<FileShareDirectoryScope> CreateDirectory(string connectionString)
		{
			var shareName = Guid.NewGuid().ToString("D");
			var shareClient = new ShareClient(connectionString, shareName);
			await shareClient.CreateAsync().ConfigureAwait(false);

			var directoryName = Guid.NewGuid().ToString("D");
			var directoryClient = shareClient.GetDirectoryClient(directoryName);
			await directoryClient.CreateAsync().ConfigureAwait(false);

			return new FileShareDirectoryScope(shareClient, directoryClient, shareName, directoryName);
		}

		public class FileShareDirectoryScope : FileShareScope
		{
			public string DirectoryName { get; }
			public ShareDirectoryClient DirectoryClient { get; }

			public FileShareDirectoryScope(ShareClient shareClient, ShareDirectoryClient directoryClient, string shareName, string directoryName)
				: base(shareClient, shareName)
			{
				DirectoryClient = directoryClient;
				DirectoryName = directoryName;
			}
		}
	}


}
