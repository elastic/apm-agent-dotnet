// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;

namespace Elastic.Apm.Azure.Storage
{
	internal class BlobUrl : StorageUrl
	{
		public static bool TryCreate(string url, out BlobUrl blobUrl)
		{
			if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
			{
				blobUrl = new BlobUrl(uri);
				return true;
			}

			blobUrl = null;
			return false;
		}

		public BlobUrl(Uri url) : base(url) => ResourceName = url.AbsolutePath.TrimStart('/');

		public string ResourceName { get; }
	}

	internal abstract class StorageUrl
	{
		private static char[] SplitDomain = { '.' };

		protected StorageUrl(Uri url)
		{
			StorageAccountName = url.Host.Split(SplitDomain, 2)[0];
			FullyQualifiedNamespace = url.Host;
		}

		public string StorageAccountName { get; }
		public string FullyQualifiedNamespace { get; }
	}
}
