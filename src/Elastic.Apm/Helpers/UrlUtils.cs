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
				logger.Scoped($"{ThisClassName}.{DbgUtils.CurrentMethodName()}").Debug()?.Log(
					"Cannot extract destination info (URL is not absolute or doesn't point to an external resource)."
					+ " url: IsAbsoluteUri: {IsAbsoluteUri}, HostNameType: {HostNameType}."
					, url.IsAbsoluteUri, url.HostNameType);
				return null;
			}

			var host = url.Host;
			if (url.HostNameType == UriHostNameType.IPv6 && host.Length > 2 && host[0] == '[' && host[host.Length - 1] == ']')
				host = host.Substring(1, host.Length - 2);

			return new Destination{ Address = host, Port = url.Port == -1 ? (int?)null : url.Port };
		}
	}
}
