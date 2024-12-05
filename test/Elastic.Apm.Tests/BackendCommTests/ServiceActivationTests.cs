// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using Elastic.Apm.Api;
using Elastic.Apm.BackendComm;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;
using Elastic.Apm.Report;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using RichardSzalay.MockHttp;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.Tests.BackendCommTests
{
	public class ServiceActivationTests : LoggingTestBase
	{
		private const string ThisClassName = nameof(ServiceActivationTests);

		private readonly IApmLogger _logger;

		public ServiceActivationTests(ITestOutputHelper xUnitOutputHelper) : base(xUnitOutputHelper /*, LogLevel.Debug */) =>
			_logger = LoggerBase.Scoped(ThisClassName);

		[Fact]
		public void ShouldNotSendActivationMethodOnceBadVersionIsDiscovered()
		{
			var requests = FakeServerInformationCallAndEnqueue("8.7.0");
			requests.Should().HaveCount(3);
			requests.Last().Should().NotContain("activation_method");
		}

		[Fact]
		public void ShouldSendActivationMethodOtherVersions()
		{
			var requests = FakeServerInformationCallAndEnqueue("8.7.1");
			requests.Should().HaveCount(3);
			requests.Last().Should().Contain("activation_method");
		}

		private List<string> FakeServerInformationCallAndEnqueue(string version)
		{
			var secretToken = "secretToken";
			var serverUrl = "http://username:password@localhost:8200";

			var config = new MockConfiguration(_logger, logLevel: "Trace", serverUrl: serverUrl, secretToken: secretToken, flushInterval: "0",
				maxBatchEventCount: "1");
			var service = Service.GetDefaultService(config, _logger);
			service.Agent.ActivationMethod.Should().NotBeNullOrEmpty();

			var waitHandle = new CountdownEvent(3);
			var handler = new RichardSzalay.MockHttp.MockHttpMessageHandler();
			var serverInformationUrl = BackendCommUtils.ApmServerEndpoints
				.BuildApmServerInformationUrl(config.ServerUrl);

			var eventsAbsoluteUrl = BackendCommUtils.ApmServerEndpoints
				.BuildIntakeV2EventsAbsoluteUrl(config.ServerUrl);

			var requests = new List<string>();
			handler
				.When(eventsAbsoluteUrl.AbsoluteUri)
				.Respond(async c =>
				{
					if (c.Content != null)
					{
						var request = await c.Content.ReadAsStringAsync();
						requests.Add(request);
					}
					waitHandle.Signal();
					var response = new HttpResponseMessage(HttpStatusCode.OK);
					var json = "{}";
					response.Content = new StringContent(json, Encoding.UTF8, "application/json");
					return new HttpResponseMessage(HttpStatusCode.OK);
				});

			// Agent blocks first event queue worker loop to get information ONCE
			handler
				.When(serverInformationUrl.AbsoluteUri)
				.Respond(_ =>
				{
					var response = new HttpResponseMessage(HttpStatusCode.OK);
					var json = $@"{{
	""build_date"": ""2021-12-18T19:59:06Z"",
	""build_sha"": ""24fe620eeff5a19e2133c940c7e5ce1ceddb1445"",
	""publish_ready"": true,
	""version"": ""{version}""
}}";
					response.Content = new StringContent(json, Encoding.UTF8, "application/json");
					return response;
				});

			var payloadSender = new PayloadSenderV2(_logger, config, service, new Api.System(), null, handler);
			using var agent = new ApmAgent(new TestAgentComponents(LoggerBase, config, payloadSender));
			agent.PayloadSender.QueueTransaction(new Transaction(agent, "TestName", "TestType"));
			agent.PayloadSender.QueueTransaction(new Transaction(agent, "TestName", "TestType"));
			agent.PayloadSender.QueueTransaction(new Transaction(agent, "TestName", "TestType"));

			waitHandle.WaitHandle.WaitOne(TimeSpan.FromSeconds(5));

			foreach (var request in requests)
				LoggerBase?.Info()?.Log("Request: {Request}", request);

			return requests;
		}
	}
}
