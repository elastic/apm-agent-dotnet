using System;

namespace Elastic.Apm.Helpers
{
	internal static class UrlUtils
	{
		internal static bool TryExtractDestinationInfo(Uri url, out string host, out int? port)
		{
			if (!url.IsAbsoluteUri || url.HostNameType == UriHostNameType.Basic || url.HostNameType == UriHostNameType.Unknown)
			{
				host = null;
				port = null;
				return false;
			}

			host = url.Host;
			if (url.HostNameType == UriHostNameType.IPv6 && host.Length > 2 && host[0] == '[' && host[host.Length - 1] == ']')
				host = host.Substring(1, host.Length - 2);

			port = url.Port == -1 ? (int?)null : url.Port;
			return true;
		}
	}
}
