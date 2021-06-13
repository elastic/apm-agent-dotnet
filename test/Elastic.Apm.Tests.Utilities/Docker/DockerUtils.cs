// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO;
using Xunit;

namespace Elastic.Apm.Tests.Utilities.Docker
{
	public class DockerUtils
	{
		public static bool IsRunningInDocker() =>
			File.Exists("/proc/1/cgroup") && File.ReadAllText("/proc/1/cgroup").Contains("docker");
	}

	public class RunningInDockerFactAttribute : FactAttribute
	{
		public RunningInDockerFactAttribute()
		{
			if (!DockerUtils.IsRunningInDocker())
				Skip = "Not running in a docker container";
		}
	}
}
