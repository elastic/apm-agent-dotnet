// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.IO;
using System.Net;
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

			// Legacy, e.g.: /system.slice/docker-<CID>.scope
			if (idPart.EndsWith(".scope"))
			{
				idPart = idPart.Substring(0, idPart.Length - ".scope".Length)
					.Substring(idPart.IndexOf("-", StringComparison.Ordinal) + 1);
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
			try
			{
				return Environment.MachineName;
			}
			catch (Exception e)
			{
				_logger.Warning()?.LogException(e, "Failed to get hostname via Dns.GetHostName - revert to environment variables");

				try
				{
					// try environment variables
					var host = (Environment.GetEnvironmentVariable("COMPUTERNAME")
						?? Environment.GetEnvironmentVariable("HOSTNAME"))
						?? Environment.GetEnvironmentVariable("HOST");

					if (host == null)
						_logger.Error()?.Log("Failed to get hostname via environment variables.");
					return host;
				}
				catch (Exception exception)
				{
					_logger.Error()?.LogException(exception, "Failed to get hostname.");
				}
			}

			return null;
		}

		private void ParseContainerInfo(Api.System system, string reportedHostName)
		{
			try
			{
				using var sr = GetCGroupAsStream();
				if (sr is null)
				{
					//just debug log, since this is normal on non-docker environments
					_logger.Debug()?.Log("No /proc/self/cgroup found - the agent will not report container id");
					return;
				}

				string line;
				while ((line = sr.ReadLine()) != null)
				{
					ParseContainerId(system, reportedHostName, line);
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

		protected virtual StreamReader GetCGroupAsStream()
			=> File.Exists("/proc/self/cgroup") ? new StreamReader("/proc/self/cgroup") : null;

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
