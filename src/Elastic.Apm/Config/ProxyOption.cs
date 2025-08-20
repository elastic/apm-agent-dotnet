// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;

namespace Elastic.Apm.Config;

public sealed class ProxyOption
{
	private ProxyOption(bool isEnabled, Uri proxyUrl, string proxyUserName, string proxyPassword)
	{
		IsEnabled = isEnabled;
		Url = proxyUrl;
		UserName = proxyUserName;
		Password = proxyPassword;
	}

	public bool IsEnabled { get; }

	public Uri Url { get; }

	public string UserName { get; }

	public string Password { get; }

	public static ProxyOption Create(Uri proxyUrl, string proxyUserName = null, string proxyPassword = null)
	{
		if (proxyUrl == null)
			return new ProxyOption(false, null, null, null);

		return new ProxyOption(true, proxyUrl, proxyUserName, proxyPassword);
	}
}
