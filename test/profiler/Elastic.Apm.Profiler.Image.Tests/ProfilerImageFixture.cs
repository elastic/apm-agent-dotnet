// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Elastic.Apm.Tests.Utilities;
using ProcNet;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Elastic.Apm.Profiler.Image.Tests;

public sealed class ProfilerImageFixture(IMessageSink sink) : IAsyncLifetime
{
	public string? ImageTag { get; private set; }
	public string? SkipReason { get; private set; }

	public Task InitializeAsync()
	{
		var buildOutput = Path.Combine(SolutionPaths.Root, "build", "output");
		var zipFile = Directory.Exists(buildOutput)
			? Directory.GetFiles(buildOutput, "elastic_apm_profiler_*-linux-x64.zip")
				.OrderByDescending(f => new FileInfo(f).LastWriteTimeUtc)
				.FirstOrDefault()
			: null;

		if (zipFile is null)
		{
			SkipReason = "Profiler zip not found in build/output — run 'build.bat profiler-zip' (Windows) or './build.sh profiler-zip' (Linux) first";
			return Task.CompletedTask;
		}

		var zipFileName = Path.GetFileName(zipFile);
		var version = zipFileName
			.Replace("elastic_apm_profiler_", string.Empty)
			.Replace("-linux-x64.zip", string.Empty);

		ImageTag = $"docker.elastic.co/observability/apm-agent-dotnet:{version}-imagetest";

		Log($"Building image {ImageTag} from {zipFileName}");

		var result = Proc.Start(new StartArguments("docker",
			"build", SolutionPaths.Root,
			"-t", ImageTag,
			"--build-arg", $"AGENT_ZIP_FILE=build/output/{zipFileName}")
		{
			Timeout = TimeSpan.FromMinutes(10)
		});

		foreach (var line in result.ConsoleOut)
			Log(line.Line);

		if (result.ExitCode != 0)
		{
			SkipReason = $"docker build exited with code {result.ExitCode}";
			ImageTag = null;
		}

		return Task.CompletedTask;
	}

	public Task DisposeAsync()
	{
		if (ImageTag is not null)
		{
			Log($"Removing test image {ImageTag}");
			Proc.Start(new StartArguments("docker", "rmi", "--force", ImageTag)
			{
				Timeout = TimeSpan.FromMinutes(2)
			});
		}
		return Task.CompletedTask;
	}

	private void Log(string message) =>
		sink.OnMessage(new DiagnosticMessage(message));
}
