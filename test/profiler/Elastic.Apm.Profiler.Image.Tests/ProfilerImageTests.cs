// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Linq;
using FluentAssertions;
using ProcNet;
using Xunit;

namespace Elastic.Apm.Profiler.Image.Tests;

public class ProfilerImageTests(ProfilerImageFixture fixture) : IClassFixture<ProfilerImageFixture>
{
	// Key files that must be present and readable inside the image after extraction.
	// The native .so and integrations.yml are the minimum required for the profiler
	// to attach; the managed DLL confirms the zip contents landed correctly.
	private static readonly string[] ExpectedFiles =
	[
		"libelastic_apm_profiler.so",
		"integrations.yml",
		"net8.0/Elastic.Apm.Profiler.Managed.dll",
	];

	[ProfilerImageFact]
	public void Container_RunsAs_NonRoot_User()
	{
		if (fixture.SkipReason is not null)
			Assert.Fail(fixture.SkipReason);

		var (exitCode, stdout) = Docker("run", "--rm", fixture.ImageTag!, "id", "-u");

		exitCode.Should().Be(0);
		stdout.Should().Be("65532", "the container should run as the wolfi nonroot user (uid 65532)");
	}

	[ProfilerImageFact]
	public void Agent_Files_Are_Present_And_Readable()
	{
		if (fixture.SkipReason is not null)
			Assert.Fail(fixture.SkipReason);

		foreach (var file in ExpectedFiles)
		{
			var (exitCode, _) = Docker("run", "--rm", fixture.ImageTag!,
				"sh", "-c", $"test -r /usr/agent/apm-dotnet-agent/{file}");
			exitCode.Should().Be(0, $"'{file}' should be present and readable at /usr/agent/apm-dotnet-agent/");
		}

		var (soExit, _) = Docker("run", "--rm", fixture.ImageTag!,
			"sh", "-c", "test -x /usr/agent/apm-dotnet-agent/libelastic_apm_profiler.so");
		soExit.Should().Be(0, "libelastic_apm_profiler.so must have execute permission for the CLR to load it");
	}

	/// <summary>
	/// Simulates the Kubernetes init container pattern: the operator mounts an emptyDir
	/// volume into this container and runs 'cp -r' to copy the agent files across before
	/// the application container starts.
	/// </summary>
	[ProfilerImageFact]
	public void InitContainer_CopiesFiles_ToSharedVolume()
	{
		if (fixture.SkipReason is not null)
			Assert.Fail(fixture.SkipReason);

		var volume = $"apm-agent-dotnet-imagetest-{Guid.NewGuid():N}";
		Docker("volume", "create", volume).ExitCode.Should().Be(0);
		try
		{
			// Docker named volumes are root:root 0755 by default; Kubernetes emptyDir volumes
			// are 0777. Run a one-shot root container to replicate k8s emptyDir permissions
			// so the non-root user (65532) can write to the volume.
			Docker("run", "--rm", "-v", $"{volume}:/target", "--user", "0",
				fixture.ImageTag!, "sh", "-c", "chmod 777 /target")
				.ExitCode.Should().Be(0, "volume must be world-writable to simulate k8s emptyDir");

			var (cpExit, _) = Docker("run", "--rm",
				"-v", $"{volume}:/target",
				fixture.ImageTag!,
				"sh", "-c", "cp -r /usr/agent/apm-dotnet-agent/. /target/");
			cpExit.Should().Be(0, "init container cp to shared volume should succeed");

			foreach (var file in ExpectedFiles)
			{
				var (testExit, _) = Docker("run", "--rm",
					"-v", $"{volume}:/target",
					fixture.ImageTag!,
					"sh", "-c", $"test -f /target/{file}");
				testExit.Should().Be(0, $"'{file}' should be present in shared volume after cp");
			}
		}
		finally
		{
			Docker("volume", "rm", volume);
		}
	}

	private static (int ExitCode, string Stdout) Docker(params string[] args)
	{
		var result = Proc.Start(new StartArguments("docker", args) { Timeout = TimeSpan.FromMinutes(2) });
		var stdout = string.Join(Environment.NewLine, result.ConsoleOut.Where(l => !l.Error).Select(l => l.Line)).Trim();
		return (result.ExitCode ?? 1, stdout);
	}
}
