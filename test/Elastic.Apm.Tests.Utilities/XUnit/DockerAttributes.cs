// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.IO;
using ProcNet;
using Xunit;

namespace Elastic.Apm.Tests.Utilities.XUnit;


/// <inheritdoc cref="DockerTheory"/>
public sealed class DockerFact : FactAttribute
{
	public DockerFact() => Skip = DockerTheory.ShouldSkip();
}

/// <summary>
/// <para>Locally we always run TestContainers through docker.</para>
/// <para>On Github Actions windows hosts we use TC cloud to launch linux docker containers</para>
/// <para>
/// When forks run the tests on pull request TestContainers Cloud won't be configured and available.
/// So we opt to skip instead.
/// </para>
/// </summary>
public sealed class DockerTheory : TheoryAttribute
{
	public DockerTheory() => Skip = ShouldSkip();

	public static string ShouldSkip()
	{
		var tcCloudConfigured = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TC_CLOUD_TOKEN"));
		if (TestEnvironment.IsGitHubActions
			&& TestEnvironment.IsWindows
			&& !tcCloudConfigured)
			return "Running on Github Action Windows host with no active TC_CLOUD_TOKEN configuration, skipping.";
		// if running locally skip the test because it needs docker installed.
		else if (!DockerUtils.HasDockerInstalled)
			return "This test requires docker to be installed";

		return null;
	}
}

/// <summary>
/// Marks a test that has to be running inside of a docker container
/// </summary>
public class RunningInDockerFactAttribute : FactAttribute
{
	public RunningInDockerFactAttribute()
	{
		if (!DockerUtils.IsRunningInDocker())
			Skip = "Not running in a docker container";
	}
}

internal static class DockerUtils
{
	public static bool IsRunningInDocker() =>
		File.Exists("/proc/1/cgroup") && File.ReadAllText("/proc/1/cgroup").Contains("docker");

	public static bool HasDockerInstalled { get; }

	static DockerUtils()
	{
		try
		{
			var result = Proc.Start(new StartArguments("docker", "--version") { Timeout = TimeSpan.FromSeconds(30) });
			HasDockerInstalled = result.ExitCode == 0;
		}
		catch (Exception)
		{
			HasDockerInstalled = false;
		}
	}

}
