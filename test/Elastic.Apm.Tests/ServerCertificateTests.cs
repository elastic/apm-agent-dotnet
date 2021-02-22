// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

// depends on Mock APM server project TargetFramework
#if NETCOREAPP2_1

using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Utilities;
using Xunit;
using Elastic.Apm.Tests.MockApmServer;
using FluentAssertions;

namespace Elastic.Apm.Tests
{
	public class ServerCertificateTests : IAsyncLifetime
	{
		private readonly MockApmServer.MockApmServer _server;
		private readonly int _port;
		private readonly ManualResetEvent _waitHandle;
		private readonly TestLogger _logger;

		public ServerCertificateTests()
		{
			_logger = new TestLogger(LogLevel.Trace);
			_server = new MockApmServer.MockApmServer(_logger, nameof(ServerCert_Should_Allow_Https_To_Apm_Server), true);
			_port = _server.FindAvailablePortToListen();
			_waitHandle = new ManualResetEvent(false);

			_server.OnReceive += o =>
			{
				if (o is TransactionDto)
					_waitHandle.Set();
			};

			_server.RunInBackground(_port);
		}

		[Fact]
		public void ServerCert_Should_Allow_Https_To_Apm_Server()
		{
			using var tempFile = new TempFile();
			var certPath = Path.Combine(SolutionPaths.Root, "test", "Elastic.Apm.Tests.MockApmServer", "cert.pfx");
			var serverCert = new X509Certificate2(certPath, "password");
			File.WriteAllBytes(tempFile.Path, serverCert.Export(X509ContentType.Cert));

			var configuration = new MockConfigSnapshot(
				serverUrl: $"https://localhost:{_port}",
				serverCert: tempFile.Path,
				disableMetrics: "*",
				cloudProvider: "none");

			using var agent = new ApmAgent(new AgentComponents(_logger, configuration));
			agent.Tracer.CaptureTransaction("TestTransaction", "TestType", t =>
			{
				t.SetLabel("self_signed_cert", true);
			});

			var signalled = _waitHandle.WaitOne(TimeSpan.FromMinutes(2));
			signalled.Should().BeTrue("timed out waiting to receive transaction");

			_server.ReceivedData.Transactions.Should().HaveCount(1);
			var transaction = _server.ReceivedData.Transactions.First();
			transaction.Context.Labels.MergedDictionary.Should().ContainKey("self_signed_cert");
		}

		[Fact]
		public void VerifyServerCert_Should_Allow_Https_To_Apm_Server()
		{
			var configuration = new MockConfigSnapshot(
				serverUrl: $"https://localhost:{_port}",
				verifyServerCert: "false",
				disableMetrics: "*",
				cloudProvider: "none");

			using var agent = new ApmAgent(new AgentComponents(_logger, configuration));
			agent.Tracer.CaptureTransaction("TestTransaction", "TestType", t =>
			{
				t.SetLabel("verify_server_cert", false);
			});

			var signalled = _waitHandle.WaitOne(TimeSpan.FromMinutes(2));
			signalled.Should().BeTrue("timed out waiting to receive transaction");

			_server.ReceivedData.Transactions.Should().HaveCount(1);
			var transaction = _server.ReceivedData.Transactions.First();
			transaction.Context.Labels.MergedDictionary.Should().ContainKey("verify_server_cert");
		}

		public Task InitializeAsync() => Task.CompletedTask;

		public async Task DisposeAsync()
		{
			_waitHandle?.Dispose();
			await _server.StopAsync();

		}
	}
}

#endif
