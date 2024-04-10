// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit.Abstractions;

namespace Elastic.Apm.Extensions.Tests.Shared;

public class ExtensionsTestHelper
{
	private readonly string _workingDirectory = Path.Combine(SolutionPaths.Root, "test", "integrations", "applications", "HostingTestApp");
	private readonly ITestOutputHelper _testOutput;

	public ExtensionsTestHelper(ITestOutputHelper testOutput) => _testOutput = testOutput;

	public void TestSetup()
	{
		// Build the app once before running tests

		var startInfo = new ProcessStartInfo
		{
			FileName = "dotnet",
			UseShellExecute = false,
			RedirectStandardOutput = false,
			RedirectStandardError = false,
			CreateNoWindow = true,
			WorkingDirectory = _workingDirectory,
		};

		startInfo.ArgumentList.Add("build");
		startInfo.ArgumentList.Add("-c");
		startInfo.ArgumentList.Add("Release");

		using var proc = new Process { StartInfo = startInfo };

		proc.Start();
		proc.WaitForExit();

		if (proc.ExitCode != 0)
			throw new Exception("Unable to build test app project required for tests!");
	}

	public async Task ExecuteTestProcessAsync(bool? enabled, bool registerTwice, bool legacyIHostBuilder, bool disabledViaEnvVar, bool loggingMode)
	{
		var startInfo = new ProcessStartInfo
		{
			FileName = "dotnet",
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			CreateNoWindow = true,
			WorkingDirectory = _workingDirectory,
		};

		if (disabledViaEnvVar)
			startInfo.Environment["ELASTIC_APM_ENABLED"] = "false";

		startInfo.ArgumentList.Add("run");
		startInfo.ArgumentList.Add("-c");
		startInfo.ArgumentList.Add("Release");
		startInfo.ArgumentList.Add("--no-build");
		startInfo.ArgumentList.Add("--no-restore");
		startInfo.ArgumentList.Add("--");
		startInfo.ArgumentList.Add(enabled.HasValue ? enabled.Value.ToString() : "unset");
		startInfo.ArgumentList.Add(registerTwice.ToString());
		startInfo.ArgumentList.Add(legacyIHostBuilder.ToString());
		startInfo.ArgumentList.Add(loggingMode.ToString());

		using var proc = new Process { StartInfo = startInfo };

		proc.OutputDataReceived += new DataReceivedEventHandler((_, e) =>
		{
			if (e.Data is null)
				return;

			_testOutput.WriteLine(e.Data);
		});

		proc.ErrorDataReceived += new DataReceivedEventHandler((_, e) =>
		{
			if (e.Data is null)
				return;

			_testOutput.WriteLine($"ERROR: {e.Data}");
		});

		proc.Start();
		proc.BeginOutputReadLine();
		proc.BeginErrorReadLine();

		await proc.WaitForExitAsync();

		proc.ExitCode.Should().Be(0);
	}
}
