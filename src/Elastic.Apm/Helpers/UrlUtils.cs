// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Apm.Api;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Helpers
{
	internal static class UrlUtils
	{
		private const string ThisClassName = nameof(UrlUtils);

		/// <returns><c>Destination</c> if successful and <c>null</c> otherwise</returns>
		internal static Destination ExtractDestination(Uri url, IApmLogger logger)
		{
			if (!url.IsAbsoluteUri || url.HostNameType == UriHostNameType.Basic || url.HostNameType == UriHostNameType.Unknown)
			{
				logger.Scoped($"{ThisClassName}.{DbgUtils.CurrentMethodName()}")
					.Debug()
					?.Log(
						"Cannot extract destination info (URL is not absolute or doesn't point to an external resource)."
						+ " url: IsAbsoluteUri: {IsAbsoluteUri}, HostNameType: {HostNameType}."
						, url.IsAbsoluteUri, url.HostNameType);
				return null;
			}

			var host = url.Host;
			if (url.HostNameType == UriHostNameType.IPv6 && host.Length > 2 && host[0] == '[' && host[host.Length - 1] == ']')
				host = host.Substring(1, host.Length - 2);

			return new Destination { Address = host, Port = url.Port == -1 ? (int?)null : url.Port };
		}

		internal static Destination.DestinationService ExtractService(Uri url, ISpan span)
		{
			var port = url.IsDefaultPort ? string.Empty : $":{url.Port}";
			var scheme = $"{url.Scheme}://";
			return new Destination.DestinationService { Type = span.Type, Name = scheme + url.Host + port, Resource = $"{url.Host}:{url.Port}" };
		}
	}
}
