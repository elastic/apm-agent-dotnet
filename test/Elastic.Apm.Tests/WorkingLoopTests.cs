// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Logging;
using Elastic.Apm.Report;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests;

public class WorkingLoopTests
{
	/// <summary>
	/// See https://github.com/elastic/apm-agent-dotnet/pull/1630
	/// Makes sure that the worker loop does not end when an OperationCanceledException happens during the HTTP request to APM Server.
	/// Assert happens based on logging - we assume the PayloadSender prints a log line in the format of the current assert.
	/// </summary>
	[Fact]
	public void RequestTimeoutTest()
	{
		var waitHandle = new ManualResetEvent(false);

		using var localServer = LocalServer.Create(context =>
		{
			Thread.Sleep(500000);
			context.Response.StatusCode = 200;
		});

		var iterCount = 0;
		var handler = new MockHttpMessageHandler((r, c) =>
		{
			// 1. request times out
			if (iterCount == 0)
				throw new OperationCanceledException();

			// 2. request returns OK
			iterCount++;
			waitHandle.Set();
			return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
		});


		var config = new MockConfiguration(serverUrl: localServer.Uri, flushInterval: "1ms");
		var logger = new InMemoryBlockingLogger(LogLevel.Trace);
		var payloadSender = new PayloadSenderV2(logger, config,
			Service.GetDefaultService(config, logger), new Api.System(), MockApmServerInfo.Version710, handler);

		using var agent = new ApmAgent(new AgentComponents(payloadSender: payloadSender, configurationReader: config));

		// This won't be sent due to timeout
		agent.Tracer.CaptureTransaction("Test", "Test", t => { });

		Thread.Sleep(50);

		// This will be sent
		agent.Tracer.CaptureTransaction("Test2", "Test", t => { });

		Thread.Sleep(50);

		waitHandle.WaitOne(TimeSpan.FromMilliseconds(1000));

		logger.Lines.Should().Contain(l => l.Contains("{PayloadSenderV2} Serialized item to send: Transaction") && l.Contains("Name: Test2"));
		logger.Lines.Should().NotContain(l => l.Contains("WorkLoop is about to exit because it was cancelled"));
	}
}
