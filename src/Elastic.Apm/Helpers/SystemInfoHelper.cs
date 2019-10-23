using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using Elastic.Apm.Api;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Helpers
{
	internal class SystemInfoHelper
	{
		private const string ContainerUidRegexString = "^[0-9a-fA-F]{64}$";

		private const string PodRegexString = @"(?:^/kubepods/[^/]+/pod([^/]+)$)|"
			+ @"(?:^/kubepods\.slice/kubepods-[^/]+\.slice/kubepods-[^/]+-pod([^/]+)\.slice$)";

		private readonly Regex _containerUidRegex = new Regex(ContainerUidRegexString);
		private readonly IApmLogger _logger;
		private readonly Regex _podRegex = new Regex(PodRegexString);

		public SystemInfoHelper(IApmLogger logger)
			=> _logger = logger.Scoped(nameof(SystemInfoHelper));

		private Container ParseContainerId(string line)
		{
			//Copied from the Java agent, and C#-ified

			string kubernetesPodUid = null;

			var fields = line.Split(':');
			if (fields.Length != 3) return null;

			var cGroupPath = fields[2];
			var idPart = Path.GetFileName(cGroupPath);

			if (string.IsNullOrWhiteSpace(idPart)) return null;

			// Legacy, e.g.: /system.slice/docker-<CID>.scope
			if (idPart.EndsWith(".scope"))
				idPart = idPart.Substring(0, idPart.Length - ".scope".Length).Substring(idPart.IndexOf("-", StringComparison.Ordinal) + 1);

			// Looking for kubernetes info
			var dirPathPart = Path.GetDirectoryName(cGroupPath);
			if (dirPathPart != null)
			{
				var dir = dirPathPart;

				var matcher = _podRegex.Match(dir);

				if (matcher.Success)
				{
					for (var i = 1; i <= matcher.Groups.Count; i++)
					{
						var podUid = matcher.Groups[i].Value;
						if (string.IsNullOrWhiteSpace(podUid)) continue;

						_logger.Debug()?.Log("Found Kubernetes pod UID: {podUid}", podUid);
						// By default, Kubernetes will set the hostname of the pod containers to the pod name. Users that override
						// the name should use the Downward API to override the pod name.
						kubernetesPodUid = podUid;
						break;
					}
				}
			}

			// If the line matched the one of the kubernetes patterns, we assume that the last part is always the container ID.
			// Otherwise we validate that it is a 64-length hex string
			if (!string.IsNullOrWhiteSpace(kubernetesPodUid) || _containerUidRegex.Match(idPart).Success)
				return new Container { Id = idPart };

			_logger.Debug()?.Log("Could not parse container ID from '/proc/self/cgroup' line: {line}", line);
			return null;
		}

		internal Api.System ParseSystemInfo() =>
			new Api.System { Container = ParseContainerInfo(), DetectedHostName = GetHostName() };

		internal string GetHostName()
		{
			try
			{
				// gets fully qualified domain name (FQDN)
				return Dns.GetHostEntry(Environment.MachineName).HostName;
			}
			catch (Exception e)
			{
				_logger.Error()?.LogException(e, "Failed to get hostname");
			}

			return null;
		}

		private Container ParseContainerInfo()
		{
			try
			{
				using (var sr = GetCGroupAsStream())
				{
					if (sr == null)
					{
						//just debug log, since this is normal on non-docker environments
						_logger.Debug()?.Log("No /proc/self/cgroup found - the agent will not report container id");
						return null;
					}

					var line = sr.ReadLine();

					while (line != null)
					{
						var res = ParseContainerId(line);
						if (res != null)
							return res;

						line = sr.ReadLine();
					}
				}
			}
			catch (Exception e)
			{
				_logger.Error()?.LogException(e, "Exception while parsing container id");
			}

			_logger.Error()?.Log("Failed parsing container id");
			return null;
		}

		protected virtual StreamReader GetCGroupAsStream()
			=> File.Exists("/proc/self/cgroup") ? new StreamReader("/proc/self/cgroup") : null;
	}
}
