// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;

namespace Elastic.Apm.Azure.Storage
{
	internal class BlobUrl : StorageUrl
	{
		public BlobUrl(Uri url) : base(url) => ResourceName = url.AbsolutePath.TrimStart('/');

		public BlobUrl(string url) : this(new Uri(url))
		{
		}

		public string ResourceName { get; }
	}

	internal abstract class StorageUrl
	{
		private static char[] SplitDomain = { '.' };

		protected StorageUrl(Uri url)
		{
			StorageAccountName = url.Host.Split(SplitDomain, 2)[0];
			FullyQualifiedNamespace = url.GetLeftPart(UriPartial.Authority) + "/";
		}

		public string StorageAccountName { get; }
		public string FullyQualifiedNamespace { get; }
	}
}
