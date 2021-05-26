// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;

namespace Elastic.Apm.Azure.Storage
{
	internal class BlobUrl
	{
		public BlobUrl(Uri url)
		{
			var builder = new UriBuilder(url);

			FullyQualifiedNamespace = builder.Uri.GetLeftPart(UriPartial.Authority) + "/";
			ResourceName = builder.Uri.AbsolutePath.TrimStart('/');
		}

		public BlobUrl(string url) : this(new Uri(url))
		{
		}

		public string ResourceName { get; }

		public string FullyQualifiedNamespace { get; }
	}
}
