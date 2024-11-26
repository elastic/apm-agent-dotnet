// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Config;
using Elastic.Apm.Tests.MockApmServer;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using static Elastic.Apm.Config.ConfigurationOption;
using Environment = System.Environment;
using LogLevel = Elastic.Apm.Logging.LogLevel;

namespace Elastic.Apm.StartupHook.Tests
{
	public partial class StartupHookTests(ITestOutputHelper output)
	{
		private readonly ITestOutputHelper _output = output;

		// NOTE: We test the two latest supported LTS releases.
		private static IEnumerable<(string TargetFramework, string RuntimeName, string Version, string ShortVersion)> GetDotNetFrameworkVersionInfos()
		{
			yield return ("net8.0", ".NET 8", "8.0.0.0", "80");
		}

		public static IEnumerable<object[]> DotNetFrameworkVersionInfos()
			=> GetDotNetFrameworkVersionInfos().Select(i => new[] { i.TargetFramework, i.RuntimeName, i.Version });

		public static IEnumerable<object[]> DotNetFrameworks()
			=> DotNetFrameworkVersionInfos().Select(o => o[0..1]);

		public static IEnumerable<object[]> WebAppInfos()
		{
			var testData = new List<string[]>();
			foreach (var i in GetDotNetFrameworkVersionInfos())
			{
				testData.Add(["webapi", $"WebApi{i.ShortVersion}", i.TargetFramework, "weatherforecast"]);
				testData.Add(["webapp", $"WebApp{i.ShortVersion}", i.TargetFramework, ""]);
				testData.Add(["mvc", $"Mvc{i.ShortVersion}", i.TargetFramework, ""]);
			}
			return testData;
		}

		/// <summary>
		/// Asserts that startup hooks successfully hook up the APM agent and
		/// send data to mock APM server for the supported framework versions
		/// </summary>
		/// <param name="targetFramework"></param>
		/// <returns></returns>
		[Theory]
		[MemberData(nameof(DotNetFrameworks))]
		public async Task Auto_Instrument_With_StartupHook_Should_Capture_Transaction(string targetFramework)
		{
			var apmLogger = new InMemoryBlockingLogger(LogLevel.Error);
			var apmServer = new MockApmServer(apmLogger, nameof(Auto_Instrument_With_StartupHook_Should_Capture_Transaction));
			var port = apmServer.FindAvailablePortToListen();
			apmServer.RunInBackground(port);
			var waitHandle = new ManualResetEvent(false);

			apmServer.OnReceive += o =>
			{
				if (o is TransactionDto)
					waitHandle.Set();
			};

			using (var sampleApp = new SampleApplication())
			{
				var environmentVariables = new Dictionary<string, string>
				{
					[ServerUrl.ToEnvironmentVariable()] = $"http://localhost:{port}",
					[CloudProvider.ToEnvironmentVariable()] = "none"
				};

				var uri = sampleApp.Start(targetFramework, environmentVariables);
				var client = new HttpClient();
				var response = await client.GetAsync(uri);

				response.IsSuccessStatusCode.Should().BeTrue();

				waitHandle.WaitOne(TimeSpan.FromMinutes(2));
				apmServer.ReceivedData.Transactions.Should().HaveCount(1);

				var transaction = apmServer.ReceivedData.Transactions.First();
				transaction.Name.Should().Be("GET Home/Index");
			}

			await apmServer.StopAsync();
		}

		[Theory]
		[MemberData(nameof(DotNetFrameworks))]
		public async Task Auto_Instrument_With_StartupHook_Should_Capture_Error(string targetFramework)
		{
			var apmLogger = new InMemoryBlockingLogger(LogLevel.Error);
			var apmServer = new MockApmServer(apmLogger, nameof(Auto_Instrument_With_StartupHook_Should_Capture_Error));
			var port = apmServer.FindAvailablePortToListen();
			apmServer.RunInBackground(port);
			var transactionWaitHandle = new ManualResetEvent(false);
			var errorWaitHandle = new ManualResetEvent(false);

			apmServer.OnReceive += o =>
			{
				if (o is TransactionDto)
					transactionWaitHandle.Set();
				if (o is ErrorDto)
					errorWaitHandle.Set();
			};

			using (var sampleApp = new SampleApplication())
			{
				var environmentVariables = new Dictionary<string, string>
				{
					[ServerUrl.ToEnvironmentVariable()] = $"http://localhost:{port}",
					[CloudProvider.ToEnvironmentVariable()] = "none"
				};

				var uri = sampleApp.Start(targetFramework, environmentVariables);
				var builder = new UriBuilder(uri) { Path = "Home/Exception" };
				var client = new HttpClient();
				var response = await client.GetAsync(builder.Uri);

				response.IsSuccessStatusCode.Should().BeFalse();

				transactionWaitHandle.WaitOne(TimeSpan.FromMinutes(2));
				apmServer.ReceivedData.Transactions.Should().HaveCount(1);

				var transaction = apmServer.ReceivedData.Transactions.First();
				transaction.Name.Should().Be("GET Home/Exception");

				errorWaitHandle.WaitOne(TimeSpan.FromMinutes(2));
				apmServer.ReceivedData.Errors.Should().HaveCount(1);

				var error = apmServer.ReceivedData.Errors.First();
				error.Culprit.Should().Be("Elastic.Apm.StartupHook.Sample.Controllers.HomeController");
			}

			await apmServer.StopAsync();
		}

		[Theory]
		[MemberData(nameof(DotNetFrameworkVersionInfos))]
		public async Task Auto_Instrument_With_StartupHook_Should_Capture_Metadata(
			string targetFramework,
			string expectedRuntimeName,
			string expectedFrameworkVersion)
		{
			var apmLogger = new InMemoryBlockingLogger(LogLevel.Error);
			var apmServer = new MockApmServer(apmLogger, nameof(Auto_Instrument_With_StartupHook_Should_Capture_Metadata));
			var port = apmServer.FindAvailablePortToListen();
			apmServer.RunInBackground(port);

			var waitHandle = new ManualResetEvent(false);
			apmServer.OnReceive += o =>
			{
				if (o is MetadataDto)
					waitHandle.Set();
			};

			using (var sampleApp = new SampleApplication())
			{
				var environmentVariables = new Dictionary<string, string>
				{
					[ServerUrl.ToEnvironmentVariable()] = $"http://localhost:{port}",
					[CloudProvider.ToEnvironmentVariable()] = "none"
				};

				var uri = sampleApp.Start(targetFramework, environmentVariables);
				var client = new HttpClient();
				var response = await client.GetAsync(uri);

				response.IsSuccessStatusCode.Should().BeTrue();

				waitHandle.WaitOne(TimeSpan.FromMinutes(2));
				apmServer.ReceivedData.Metadata.Should().HaveCountGreaterOrEqualTo(1);

				var metadata = apmServer.ReceivedData.Metadata.First();
				metadata.Service.Agent.ActivationMethod.Should().Be(Consts.ActivationMethodStartupHook);
				metadata.Service.Runtime.Name.Should().Be(expectedRuntimeName);
				metadata.Service.Framework.Name.Should().Be("ASP.NET Core");
				metadata.Service.Framework.Version.Should().Be(expectedFrameworkVersion);
			}

			await apmServer.StopAsync();
		}

		[Theory]
		[MemberData(nameof(WebAppInfos))]
		public async Task Auto_Instrument_With_StartupHook(string template, string name, string targetFramework, string path)
		{
			var apmLogger = new InMemoryBlockingLogger(LogLevel.Trace);
			var apmServer = new MockApmServer(apmLogger, nameof(Auto_Instrument_With_StartupHook));
			var port = apmServer.FindAvailablePortToListen();
			apmServer.RunInBackground(port);

			var transactionWaitHandle = new ManualResetEvent(false);
			var metadataWaitHandle = new ManualResetEvent(false);

			apmServer.OnReceive += o =>
			{
				if (o is TransactionDto)
					transactionWaitHandle.Set();
				else if (o is MetadataDto)
					metadataWaitHandle.Set();
			};

			using var project = DotnetProject.Create(_output, name, template, targetFramework, "--no-https");

			var environmentVariables = new Dictionary<string, string>
			{
				[ServerUrl.ToEnvironmentVariable()] = $"http://localhost:{port}",
				[CloudProvider.ToEnvironmentVariable()] = "none"
			};

			using var process = project.CreateProcess(SolutionPaths.AgentZip, environmentVariables);

			if (process.ExitCode.HasValue && process.ExitCode.Value != 0)
				throw new Exception($"Unable to start instrumented .NET application {name}");

			var startHandle = new ManualResetEvent(false);
			Uri uri = null;
			ExceptionDispatchInfo e = null;
			var capturedLines = new List<string>();
			var endpointRegex = NowListeningOnRegex();

			process.SubscribeLines(
				line =>
				{
					capturedLines.Add(line.Line);
					var match = endpointRegex.Match(line.Line);
					if (match.Success)
					{
						try
						{
							var endpoint = match.Groups["endpoint"].Value.Trim();
							uri = new UriBuilder(endpoint) { Path = path }.Uri;
						}
						catch (Exception exception)
						{
							e = ExceptionDispatchInfo.Capture(exception);
						}

						startHandle.Set();
					}
				},
				exception => e = ExceptionDispatchInfo.Capture(exception));

			var timeout = TimeSpan.FromMinutes(1);
			var signalled = startHandle.WaitOne(timeout);

			if (!signalled)
			{
				throw new Exception($"Could not start dotnet project within timeout {timeout}: "
					+ string.Join(Environment.NewLine, capturedLines));
			}

			e?.Throw();

			var client = new HttpClient();
			var response = await client.GetAsync(uri);

			response.IsSuccessStatusCode.Should().BeTrue();

			signalled = transactionWaitHandle.WaitOne(timeout);
			if (!signalled)
			{
				throw new Exception($"Did not receive transaction within timeout {timeout}: "
					+ string.Join(Environment.NewLine, capturedLines)
					+ Environment.NewLine
					+ string.Join(Environment.NewLine, apmLogger.Lines));
			}

			apmServer.ReceivedData.Transactions.Should().HaveCount(1);

			var transaction = apmServer.ReceivedData.Transactions.First();
			transaction.Name.Should().NotBeNullOrEmpty();

			signalled = metadataWaitHandle.WaitOne(timeout);
			if (!signalled)
			{
				throw new Exception($"Did not receive metadata within timeout {timeout}: "
					+ string.Join(Environment.NewLine, capturedLines)
					+ Environment.NewLine
					+ string.Join(Environment.NewLine, apmLogger.Lines));
			}

			apmServer.ReceivedData.Metadata.Should().HaveCountGreaterOrEqualTo(1);
			var metadata = apmServer.ReceivedData.Metadata.First();
			metadata.Service.Agent.ActivationMethod.Should().Be(Consts.ActivationMethodStartupHook);
			metadata.Service.Runtime.Name.Should().NotBeNullOrEmpty();
			metadata.Service.Framework.Name.Should().Be("ASP.NET Core");
			metadata.Service.Framework.Version.Should().NotBeNullOrEmpty();

			await apmServer.StopAsync();
		}

		[GeneratedRegex(@"\s*Now listening on:\s*(?<endpoint>http\:[^\s]*)")]
		private static partial Regex NowListeningOnRegex();
	}
}
