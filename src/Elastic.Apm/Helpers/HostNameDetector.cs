// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Net;
using System.Net.NetworkInformation;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Helpers;

internal interface IHostNameDetector
{
	string GetDetectedHostName(IApmLogger logger);
}
public class HostNameDetector : IHostNameDetector
{
	public string GetDetectedHostName(IApmLogger logger)
	{
		var fqdn = string.Empty;

		try
		{
			fqdn = Dns.GetHostEntry(string.Empty).HostName;
		}
		catch (Exception e)
		{
			logger.Warning()?.LogException(e, "Failed to get hostname via Dns.GetHostEntry(string.Empty).HostName.");
		}

		if (!string.IsNullOrEmpty(fqdn))
			return NormalizeHostName(fqdn);

		try
		{
			var hostName = IPGlobalProperties.GetIPGlobalProperties().HostName;
			var domainName = IPGlobalProperties.GetIPGlobalProperties().DomainName;

			if (!string.IsNullOrEmpty(domainName))
			{
				hostName = $"{hostName}.{domainName}";
			}

			fqdn = hostName;

		}
		catch (Exception e)
		{
			logger.Warning()?.LogException(e, "Failed to get hostname via IPGlobalProperties.GetIPGlobalProperties().");
		}

		if (!string.IsNullOrEmpty(fqdn))
			return NormalizeHostName(fqdn);

		try
		{
			fqdn = Environment.MachineName;
		}
		catch (Exception e)
		{
			logger.Warning()?.LogException(e, "Failed to get hostname via Environment.MachineName.");
		}

		if (!string.IsNullOrEmpty(fqdn))
			return NormalizeHostName(fqdn);

		logger.Debug()?.Log("Falling back to environment variables to get hostname.");

		try
		{
			fqdn = (Environment.GetEnvironmentVariable("COMPUTERNAME")
					?? Environment.GetEnvironmentVariable("HOSTNAME"))
				?? Environment.GetEnvironmentVariable("HOST");

			if (string.IsNullOrEmpty(fqdn))
				logger.Error()?.Log("Failed to get hostname via environment variables.");

			return NormalizeHostName(fqdn);
		}
		catch (Exception e)
		{
			logger.Error()?.LogException(e, "Failed to get hostname.");
		}

		return null;

		static string NormalizeHostName(string hostName) =>
			string.IsNullOrEmpty(hostName) ? null : hostName.Trim().ToLower();
	}
}
