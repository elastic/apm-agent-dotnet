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
using Elastic.Apm.Tests.MockApmServer;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;
using static Elastic.Apm.Config.ConfigConsts;
using LogLevel = Elastic.Apm.Logging.LogLevel;

namespace Elastic.Apm.StartupHook.Tests
{
	public class StartupHookTests
	{
		/// <summary>
		/// Asserts that startup hooks successfully hook up the APM agent and
		/// send data to mock APM server for the supported framework versions
		/// </summary>
		/// <param name="targetFramework"></param>
		/// <returns></returns>
		[Theory]
		[InlineData("netcoreapp3.0")]
		[InlineData("netcoreapp3.1")]
		[InlineData("net5.0")]
		public async Task Auto_Instrument_With_StartupHook_Should_Capture_Transaction(string targetFramework)
		{
			var apmLogger = new InMemoryBlockingLogger(LogLevel.Error);
			var apmServer = new MockApmServer(apmLogger, nameof(Auto_Instrument_With_StartupHook_Should_Capture_Transaction));
			var port = apmServer.FindAvailablePortToListen();
			apmServer.RunInBackground(port);

			using (var sampleApp = new SampleApplication())
			{
				var environmentVariables = new Dictionary<string, string>
				{
					[EnvVarNames.ServerUrl] = $"http://localhost:{port}",
					[EnvVarNames.CloudProvider] = "none"
				};

				var uri = sampleApp.Start(targetFramework, environmentVariables);
				var client = new HttpClient();
				var response = await client.GetAsync(uri);

				response.IsSuccessStatusCode.Should().BeTrue();

				var waitHandle = new ManualResetEvent(false);

				apmServer.OnReceive += o =>
				{
					if (o is TransactionDto)
						waitHandle.Set();
				};

				waitHandle.WaitOne(TimeSpan.FromMinutes(2));
				apmServer.ReceivedData.Transactions.Should().HaveCount(1);

				var transaction = apmServer.ReceivedData.Transactions.First();
				transaction.Name.Should().Be("GET Home/Index");
			}

			await apmServer.StopAsync();
		}

		[Theory]
		[InlineData("netcoreapp3.0", ".NET Core", "3.0.0.0")]
		[InlineData("netcoreapp3.1", ".NET Core", "3.1.0.0")]
		[InlineData("net5.0", ".NET 5", "5.0.0.0")]
		public async Task Auto_Instrument_With_StartupHook_Should_Capture_Metadata(
			string targetFramework,
			string expectedRuntimeName,
			string expectedFrameworkVersion)
		{
			var apmLogger = new InMemoryBlockingLogger(LogLevel.Error);
			var apmServer = new MockApmServer(apmLogger, nameof(Auto_Instrument_With_StartupHook_Should_Capture_Metadata));
			var port = apmServer.FindAvailablePortToListen();
			apmServer.RunInBackground(port);

			using (var sampleApp = new SampleApplication())
			{
				var environmentVariables = new Dictionary<string, string>
				{
					[EnvVarNames.ServerUrl] = $"http://localhost:{port}",
					[EnvVarNames.CloudProvider] = "none"
				};

				var uri = sampleApp.Start(targetFramework, environmentVariables);
				var client = new HttpClient();
				var response = await client.GetAsync(uri);

				response.IsSuccessStatusCode.Should().BeTrue();

				var waitHandle = new ManualResetEvent(false);

				apmServer.OnReceive += o =>
				{
					if (o is MetadataDto)
						waitHandle.Set();
				};

				waitHandle.WaitOne(TimeSpan.FromMinutes(2));
				apmServer.ReceivedData.Metadata.Should().HaveCount(1);

				var metadata = apmServer.ReceivedData.Metadata.First();
				metadata.Service.Runtime.Name.Should().Be(expectedRuntimeName);
				metadata.Service.Framework.Name.Should().Be("ASP.NET Core");
				metadata.Service.Framework.Version.Should().Be(expectedFrameworkVersion);
			}

			await apmServer.StopAsync();
		}

		[Theory]
		[InlineData("webapi", "WebApi30", "netcoreapp3.0", "weatherforecast")]
		[InlineData("webapi", "WebApi31", "netcoreapp3.1", "weatherforecast")]
		[InlineData("webapi", "WebApi50", "net5.0", "weatherforecast")]
		[InlineData("webapp", "WebApp30", "netcoreapp3.0", "")]
		[InlineData("webapp", "WebApp31", "netcoreapp3.1", "")]
		[InlineData("webapp", "WebApp50", "net5.0", "")]
		[InlineData("mvc", "Mvc30", "netcoreapp3.0", "")]
		[InlineData("mvc", "Mvc31", "netcoreapp3.1", "")]
		[InlineData("mvc", "Mvc50", "net5.0", "")]
		public async Task Auto_Instrument_With_StartupHook(string template, string name, string targetFramework, string path)
		{
			var apmLogger = new InMemoryBlockingLogger(LogLevel.Trace);
			var apmServer = new MockApmServer(apmLogger, nameof(Auto_Instrument_With_StartupHook));
			var port = apmServer.FindAvailablePortToListen();
			apmServer.RunInBackground(port);

			using var project = DotnetProject.Create(name, template, "--framework", targetFramework, "--no-https");
			var environmentVariables = new Dictionary<string, string>
			{
				[EnvVarNames.ServerUrl] = $"http://localhost:{port}",
				[EnvVarNames.CloudProvider] = "none"
			};

			using (var process = project.CreateProcess(SolutionPaths.AgentZip, environmentVariables))
			{
				var startHandle = new ManualResetEvent(false);
				Uri uri = null;
				ExceptionDispatchInfo e = null;
				var capturedLines = new List<string>();
				var endpointRegex = new Regex(@"\s*Now listening on:\s*(?<endpoint>[^\s]*)");

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

				var timeout = TimeSpan.FromMinutes(2);
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

				var transactionWaitHandle = new ManualResetEvent(false);
				var metadataWaitHandle = new ManualResetEvent(false);

				apmServer.OnReceive += o =>
				{
					if (o is TransactionDto)
						transactionWaitHandle.Set();
					else if (o is MetadataDto)
						metadataWaitHandle.Set();
				};

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

				apmServer.ReceivedData.Transactions.Should().HaveCount(1);
				var metadata = apmServer.ReceivedData.Metadata.First();
				metadata.Service.Runtime.Name.Should().NotBeNullOrEmpty();
				metadata.Service.Framework.Name.Should().Be("ASP.NET Core");
				metadata.Service.Framework.Version.Should().NotBeNullOrEmpty();
			}

			await apmServer.StopAsync();
		}
	}
}
