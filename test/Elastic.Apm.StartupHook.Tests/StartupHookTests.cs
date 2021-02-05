// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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

			using var sampleApp = new SampleApplication();

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

			// block until a transaction is received, or 2 minute timeout
			waitHandle.WaitOne(TimeSpan.FromMinutes(2));
			apmServer.ReceivedData.Transactions.Should().HaveCount(1);

			var transaction = apmServer.ReceivedData.Transactions.First();
			transaction.Name.Should().Be("GET Home/Index");

			sampleApp.Stop();
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
			var apmServer = new MockApmServer(apmLogger, nameof(Auto_Instrument_With_StartupHook_Should_Capture_Transaction));
			var port = apmServer.FindAvailablePortToListen();
			apmServer.RunInBackground(port);

			using var sampleApp = new SampleApplication();

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

			// block until a transaction is received, or 2 minute timeout
			waitHandle.WaitOne(TimeSpan.FromMinutes(2));
			apmServer.ReceivedData.Metadata.Should().HaveCount(1);

			var metadata = apmServer.ReceivedData.Metadata.First();
			metadata.Service.Runtime.Name.Should().Be(expectedRuntimeName);
			metadata.Service.Framework.Name.Should().Be("ASP.NET Core");
			metadata.Service.Framework.Version.Should().Be(expectedFrameworkVersion);

			sampleApp.Stop();
			await apmServer.StopAsync();
		}
	}
}
