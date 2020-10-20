// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using Elastic.Apm.Api;
using Elastic.Apm.Api.Kubernetes;
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

		internal Api.System ParseSystemInfo(string hostName)
		{
			var containerInfo = ParseContainerInfo();
			var detectedHostName = GetHostName();
			var kubernetesInfo = ParseKubernetesInfo(containerInfo, detectedHostName);

			return new Api.System
			{
				Container = containerInfo,
				DetectedHostName = detectedHostName,
				HostName = hostName,
				Kubernetes = kubernetesInfo
			};
		}

		internal string GetHostName()
		{
			try
			{
				return Dns.GetHostName();
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

			_logger.Warning()?.Log("Failed parsing container id - the agent will not report container id");
			return null;
		}

		protected virtual StreamReader GetCGroupAsStream()
			=> File.Exists("/proc/self/cgroup") ? new StreamReader("/proc/self/cgroup") : null;

		internal const string Namespace = "KUBERNETES_NAMESPACE";
		internal const string PodName = "KUBERNETES_POD_NAME";
		internal const string PodUid = "KUBERNETES_POD_UID";
		internal const string NodeName = "KUBERNETES_NODE_NAME";

		internal KubernetesMetadata ParseKubernetesInfo(Container containerInfo, string hostName)
		{
			var @namespace = Environment.GetEnvironmentVariable(Namespace);
			var podName = Environment.GetEnvironmentVariable(PodName);
			var podUid = Environment.GetEnvironmentVariable(PodUid);
			var nodeName = Environment.GetEnvironmentVariable(NodeName);

			if (@namespace == null && podName == null && podUid == null && nodeName == null)
			{
				// By default, Kubernetes will set the hostname of the pod containers to the pod name.
				// Users that override the name should use the Downward API to override the pod name.
				return containerInfo != null
					? new KubernetesMetadata { Pod = new Pod { Uid = containerInfo.Id, Name = hostName } }
					: null;
			}

			var kubernetesMetadata = new KubernetesMetadata { Namespace = @namespace };
			if (podName != null || podUid != null) kubernetesMetadata.Pod = new Pod { Name = podName, Uid = podUid };
			if (nodeName != null) kubernetesMetadata.Node = new Api.Kubernetes.Node { Name = nodeName };

			return kubernetesMetadata;
		}
	}
}
