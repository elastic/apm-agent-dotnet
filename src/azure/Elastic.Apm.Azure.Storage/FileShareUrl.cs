// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;

namespace Elastic.Apm.Azure.Storage
{
	internal class FileShareUrl : StorageUrl
	{
		public static bool TryCreate(string url, out FileShareUrl fileShareUrl)
		{
			if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
			{
				fileShareUrl = new FileShareUrl(uri);
				return true;
			}

			fileShareUrl = null;
			return false;
		}

		public FileShareUrl(Uri url) : base(url) => ResourceName = url.AbsolutePath.TrimStart('/');

		public string ResourceName { get; }
	}
}
