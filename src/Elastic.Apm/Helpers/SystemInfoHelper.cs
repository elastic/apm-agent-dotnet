// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using Elastic.Apm.Api;
using Elastic.Apm.Api.Kubernetes;
using Elastic.Apm.Features;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Helpers
{
	internal class SystemInfoHelper
	{
		private readonly Regex _containerUidRegex = new Regex("^[0-9a-fA-F]{64}$");
		private readonly Regex _shortenedUuidRegex = new Regex("^[0-9a-fA-F]{8}\\-[0-9a-fA-F]{4}\\-[0-9a-fA-F]{4}\\-[0-9a-fA-F]{4}\\-[0-9a-fA-F]{4,}");
		private readonly Regex _ecsContainerIdRegex = new Regex("^[a-z0-9]{32}-[0-9]{10}$");
		private readonly Regex _podRegex = new Regex(
			@"(?:^/kubepods[\S]*/pod([^/]+)$)|(?:^/kubepods\.slice/(kubepods-[^/]+\.slice/)?kubepods[^/]*-pod([^/]+)\.slice$)");

		private readonly IApmLogger _logger;

		public SystemInfoHelper(IApmLogger logger)
			=> _logger = logger.Scoped(nameof(SystemInfoHelper));


        //3997 3984 253:1 /var/lib/docker/containers/6548c6863fb748e72d1e2a4f824fde92f720952d062dede1318c2d6219a672d6/hostname /etc/hostname rw,relatime shared:1877 - ext4 /dev/mapper/vgubuntu-root rw,errors=remount-ro
		internal void ParseMountInfo(Api.System system, string reportedHostName, string line)
		{

			var fields = line.Split(' ');
			if (fields.Length <= 3)
				return;

			var path = fields[3];
			foreach (var folder in path.Split('/'))
			{
				//naive implementation to check for guid.
				if (folder.Length != 64) continue;
				system.Container = new Container { Id = folder };
			}

		}

		// "1:name=systemd:/ecs/03752a671e744971a862edcee6195646/03752a671e744971a862edcee6195646-4015103728"
		// "0::/kubepods.slice/kubepods-burstable.slice/kubepods-burstable-pod121157b5_c67d_4c3e_9052_cb27bbb711fb.slice/cri-containerd-1cd3449e930b8a28c7595240fa32ba20c84f36d059e5fbe63104ad40057992d1.scope"
		internal void ParseContainerId(Api.System system, string reportedHostName, string line)
		{
			var fields = line.Split(':');
			if (fields.Length != 3)
				return;

			var cGroupPath = fields[2];
			var lastIndexSlash = cGroupPath.LastIndexOf('/');
			if (lastIndexSlash == -1)
				return;

			var idPart = cGroupPath.Substring(lastIndexSlash + 1);
			if (string.IsNullOrWhiteSpace(idPart))
				return;

			// Legacy, e.g.: /system.slice/docker-<CID>.scope or cri-containerd-<CID>.scope
			if (idPart.EndsWith(".scope"))
			{
				var idParts = idPart.Split(new[] { '-'}, StringSplitOptions.RemoveEmptyEntries);
				var containerIdWithScope = idParts.Last();

				idPart = containerIdWithScope.Substring(0, containerIdWithScope.Length - ".scope".Length);
			}

			// Looking for kubernetes info
			var dir = cGroupPath.Substring(0, lastIndexSlash);
			if (dir.Length > 0)
			{
				var match = _podRegex.Match(dir);
				if (match.Success)
				{
					for (var i = 1; i <= match.Groups.Count; i++)
					{
						var podUid = match.Groups[i].Value;
						if (!string.IsNullOrWhiteSpace(podUid))
						{
							if (i == 2)
								continue;

							if (i == 3)
								podUid = podUid.Replace('_', '-');

							_logger.Debug()?.Log("Found Kubernetes pod UID: {podUid}", podUid);
							system.Kubernetes = new KubernetesMetadata { Pod = new Pod { Name = reportedHostName, Uid = podUid } };
							break;
						}
					}
				}
			}

			// If the line matched the one of the kubernetes patterns, we assume that the last part is always the container ID.
			// Otherwise we validate that it is a 64-length hex string
			if (system.Kubernetes != null || _containerUidRegex.IsMatch(idPart) || _shortenedUuidRegex.IsMatch(idPart) || _ecsContainerIdRegex.IsMatch(idPart))
				system.Container = new Container { Id = idPart };
			else
				_logger.Info()?.Log("Could not parse container ID from '/proc/self/cgroup' line: {line}", line);
		}

		internal Api.System GetSystemInfo(string hostName)
		{
			var detectedHostName = GetHostName();
			var system = new Api.System { DetectedHostName = detectedHostName, ConfiguredHostName = hostName };

			if (AgentFeaturesProvider.Get(_logger).Check(AgentFeature.ContainerInfo))
			{
				ParseContainerInfo(system, string.IsNullOrEmpty(hostName) ? detectedHostName : hostName);
				ParseKubernetesInfo(system);
			}

			return system;
		}

		internal string GetHostName()
		{
			var fqdn = string.Empty;

			try
			{
				fqdn = Dns.GetHostEntry(string.Empty).HostName;
			}
			catch (Exception e)
			{
				_logger.Warning()?.LogException(e, "Failed to get hostname via Dns.GetHostEntry(string.Empty).HostName.");
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
				_logger.Warning()?.LogException(e, "Failed to get hostname via IPGlobalProperties.GetIPGlobalProperties().");
			}

			if (!string.IsNullOrEmpty(fqdn))
				return NormalizeHostName(fqdn);

			try
			{
				fqdn = Environment.MachineName;
			}
			catch (Exception e)
			{
				_logger.Warning()?.LogException(e, "Failed to get hostname via Environment.MachineName.");
			}

			if (!string.IsNullOrEmpty(fqdn))
				return NormalizeHostName(fqdn);

			_logger.Debug()?.Log("Falling back to environment variables to get hostname.");

			try
			{
				fqdn = (Environment.GetEnvironmentVariable("COMPUTERNAME")
					?? Environment.GetEnvironmentVariable("HOSTNAME"))
					?? Environment.GetEnvironmentVariable("HOST");

				if (string.IsNullOrEmpty(fqdn))
					_logger.Error()?.Log("Failed to get hostname via environment variables.");

				return NormalizeHostName(fqdn);
			}
			catch (Exception e)
			{
				_logger.Error()?.LogException(e, "Failed to get hostname.");
			}

			return null;

			static string NormalizeHostName(string hostName) =>
				string.IsNullOrEmpty(hostName) ? null : hostName.Trim().ToLower();
		}

		private void ParseContainerInfo(Api.System system, string reportedHostName)
		{
			//0::/
			try
			{
				var fallBackToMountInfo = false;
				using var sr = GetCGroupAsStream();
				if (sr is null)
				{
					//just debug log, since this is normal on non-docker environments
					_logger.Debug()?.Log("No /proc/self/cgroup found - the agent will not report container id");
					return;
				}

				var i = 0;
				string line;
				while ((line = sr.ReadLine()) != null)
				{
					if (line == "0::/" && i == 0)
						fallBackToMountInfo = true;
					ParseContainerId(system, reportedHostName, line);
					if (system.Container != null)
						return;
					i++;
				}
				if (!fallBackToMountInfo)
					return;

				using var mi = GetMountInfoAsStream();
				if (mi is null)
				{
					_logger.Debug()?.Log("No /proc/self/mountinfo found - no information to fallback to");
					return;
				}

				while ((line = mi.ReadLine()) != null)
				{
					if (!line.Contains("/etc/hostname")) continue;
					ParseMountInfo(system, reportedHostName, line);
					if (system.Container != null)
						return;
				}
			}
			catch (Exception e)
			{
				_logger.Error()?.LogException(e, "Exception while parsing container id");
			}

			_logger.Info()
				?.Log(
					"Failed parsing container id - the agent will not report container id. Likely the application is not running within a container");
		}

		protected virtual StreamReader GetCGroupAsStream() =>
			File.Exists("/proc/self/cgroup") ? new StreamReader("/proc/self/cgroup") : null;

		protected virtual StreamReader GetMountInfoAsStream() =>
			File.Exists("/proc/self/mountinfo") ? new StreamReader("/proc/self/mountinfo") : null;


		internal const string Namespace = "KUBERNETES_NAMESPACE";
		internal const string PodName = "KUBERNETES_POD_NAME";
		internal const string PodUid = "KUBERNETES_POD_UID";
		internal const string NodeName = "KUBERNETES_NODE_NAME";

		internal void ParseKubernetesInfo(Api.System system)
		{
			try
			{
				var @namespace = Environment.GetEnvironmentVariable(Namespace);
				var podName = Environment.GetEnvironmentVariable(PodName);
				var podUid = Environment.GetEnvironmentVariable(PodUid);
				var nodeName = Environment.GetEnvironmentVariable(NodeName);

				var podUidNotNullOrEmpty = !string.IsNullOrEmpty(podUid);
				var podNameNotNullOrEmpty = !string.IsNullOrEmpty(podName);

				if (podUidNotNullOrEmpty || podNameNotNullOrEmpty || !string.IsNullOrEmpty(@namespace) || !string.IsNullOrEmpty(nodeName))
				{
					system.Kubernetes ??= new KubernetesMetadata();
					system.Kubernetes.Namespace = @namespace;

					if (!string.IsNullOrEmpty(nodeName))
						system.Kubernetes.Node = new Api.Kubernetes.Node { Name = nodeName };

					if (podUidNotNullOrEmpty || podNameNotNullOrEmpty)
					{
						// retain any existing pod values, and overwrite with environment variables values only if not null or empty
						system.Kubernetes.Pod ??= new Pod();
						if (podUidNotNullOrEmpty)
							system.Kubernetes.Pod.Uid = podUid;

						if (podNameNotNullOrEmpty)
							system.Kubernetes.Pod.Name = podName;
					}
				}
			}
			catch (Exception e)
			{
				_logger.Warning()?.LogException(e, "Failed to read environment variables for Kubernetes Downward API discovery");
			}
		}
	}
}
