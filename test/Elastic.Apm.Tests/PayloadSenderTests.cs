using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;
using Elastic.Apm.Report;
using Elastic.Apm.Tests.Mocks;
using Elastic.Apm.Tests.TestHelpers;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using static Elastic.Apm.Config.ConfigConsts.DefaultValues;
using static Elastic.Apm.Tests.TestHelpers.FluentAssertionsUtils;

namespace Elastic.Apm.Tests
{
	public class PayloadSenderTests
	{
		private readonly IApmLogger _logger;

		public PayloadSenderTests(ITestOutputHelper xUnitOutputHelper) =>
			_logger = new XunitOutputLogger(xUnitOutputHelper).Scoped(nameof(BasicAgentTests));

		[Fact]
		public async Task SecretToken_test()
		{
			var isRequestFinished = new TaskCompletionSource<object>();

			AuthenticationHeaderValue authHeader = null;
			var handler = new MockHttpMessageHandler((r, c) =>
			{
				authHeader = r.Headers.Authorization;
				isRequestFinished.SetResult(null);
				return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
			});

			const string secretToken = "SecretToken";
			var noopLogger = new NoopLogger();
			var configReader = new TestAgentConfigurationReader(_logger, secretToken: secretToken, maxBatchEventCount: "1");
			var payloadSender = new PayloadSenderV2(_logger, configReader,
				Service.GetDefaultService(configReader, noopLogger), new Api.System(), handler);

			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, configurationReader: configReader)))
			{
				agent.PayloadSender.QueueTransaction(new Transaction(agent, "TestName", "TestType"));
				await isRequestFinished.Task;
			}

			authHeader.Should().NotBeNull();
			authHeader.Scheme.Should().Be("Bearer");
			authHeader.Parameter.Should().Be(secretToken);
		}

		[Fact]
		public async Task UserAgent_test()
		{
			var isRequestFinished = new TaskCompletionSource<object>();

			HttpHeaderValueCollection<ProductInfoHeaderValue> userAgentHeader = null;
			var handler = new MockHttpMessageHandler((r, c) =>
			{
				userAgentHeader = r.Headers.UserAgent;
				isRequestFinished.SetResult(null);
				return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
			});

			var logger = new NoopLogger();
			var service = Service.GetDefaultService(new TestAgentConfigurationReader(logger), logger);
			var payloadSender = new PayloadSenderV2(logger, new TestAgentConfigurationReader(logger, flushInterval: "1s"),
				service, new Api.System(), handler);

			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
			{
				agent.PayloadSender.QueueTransaction(new Transaction(agent, "TestName", "TestType"));
				await isRequestFinished.Task;
			}

			userAgentHeader
				.Should()
				.NotBeEmpty()
				.And.HaveCount(3);

			userAgentHeader.First().Product.Name.Should().Be($"elasticapm-{Consts.AgentName}");
			userAgentHeader.First().Product.Version.Should().NotBeEmpty();

			userAgentHeader.Skip(1).First().Product.Name.Should().Be("System.Net.Http");
			userAgentHeader.Skip(1).First().Product.Version.Should().NotBeEmpty();

			userAgentHeader.Skip(2).First().Product.Name.Should().NotBeEmpty();
			userAgentHeader.Skip(2).First().Product.Version.Should().NotBeEmpty();
		}

		[Fact]
		public void MaxQueueEventCount_should_be_enforced_before_send()
		{
			var sendTcs = new TaskCompletionSource<object>();

			var handler = new MockHttpMessageHandler(async (r, c) =>
			{
				await sendTcs.Task;
				return new HttpResponseMessage(HttpStatusCode.OK);
			});

			var logger = new NoopLogger();
			var service = Service.GetDefaultService(new TestAgentConfigurationReader(logger), logger);
			var payloadSender = new PayloadSenderV2(logger, new TestAgentConfigurationReader(logger), service, new Api.System(), handler);

			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
			{
				for (var i = 1; i <= MaxQueueEventCount + MaxBatchEventCount + 10; ++i)
				{
					var enqueuedSuccessfully = payloadSender.EnqueueEvent(new Transaction(agent, "TestName", "TestType"), "Transaction");

					// It's possible that the events for the first batch have already been dequeued
					// so we can be sure that queue doesn't have any free space left only after MaxQueueEventCount + MaxBatchEventCount events
					if (i <= MaxQueueEventCount)
					{
						enqueuedSuccessfully.Should()
							.BeTrue($"i: {i}, MaxQueueEventCount: {MaxQueueEventCount}, MaxBatchEventCount: {MaxBatchEventCount}");
					}
					else if (i > MaxQueueEventCount + MaxBatchEventCount)
					{
						enqueuedSuccessfully.Should()
							.BeFalse($"i: {i}, MaxQueueEventCount: {MaxQueueEventCount}, MaxBatchEventCount: {MaxBatchEventCount}");
					}
				}

				sendTcs.SetResult(null);
			}
		}

		[Fact]
		public async Task MaxQueueEventCount_should_be_enforced_after_send()
		{
			var sendTcs = new TaskCompletionSource<object>();
			var firstBatchDequeuedTcs = new TaskCompletionSource<object>();

			var handler = new MockHttpMessageHandler(async (r, c) =>
			{
				firstBatchDequeuedTcs.SetResult(null);
				await sendTcs.Task;
				return new HttpResponseMessage(HttpStatusCode.OK);
			});

			var logger = new NoopLogger();
			var configReader = new TestAgentConfigurationReader(logger, flushInterval: $"{24 * 60}m");
			var service = Service.GetDefaultService(configReader, logger);
			var payloadSender = new PayloadSenderV2(logger, configReader, service, new Api.System(), handler);

			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
			{
				for (var i = 1; i <= MaxQueueEventCount; ++i)
					payloadSender.EnqueueEvent(new Transaction(agent, "TestName", "TestType"), "Transaction");

				await firstBatchDequeuedTcs.Task;

				for (var i = 1; i < MaxBatchEventCount + 10; ++i)
				{
					var enqueuedSuccessfully = payloadSender.EnqueueEvent(new Transaction(agent, "TestName", "TestType"), "Transaction");
					enqueuedSuccessfully.Should()
						.Be(i <= MaxBatchEventCount, $"i: {i}, MaxQueueEventCount: {MaxQueueEventCount}, MaxBatchEventCount: {MaxBatchEventCount}");
				}

				sendTcs.SetResult(null);
			}
		}

		[Fact]
		public void Dispose_stops_the_thread()
		{
			PayloadSenderV2 lastPayloadSender = null;
			CreateSutEnvAndTest((agent, payloadSender) =>
			{
				lastPayloadSender = payloadSender;
				lastPayloadSender.Thread.IsAlive.Should().BeTrue();
			});
			lastPayloadSender.Thread.IsAlive.Should().BeFalse();

			CreateSutEnvAndTest((agent, payloadSender) =>
			{
				lastPayloadSender = payloadSender;
				lastPayloadSender.Thread.IsAlive.Should().BeTrue();

				payloadSender.QueueTransaction(new Transaction(agent, "TestName", "TestType"));
			});
			lastPayloadSender.Thread.IsAlive.Should().BeFalse();
		}

		[Fact]
		public void calling_after_Dispose_throws()
		{
			PayloadSenderV2 payloadSender = null;
			Transaction dummyTx = null;
			CreateSutEnvAndTest((agent, payloadSenderArg) =>
			{
				payloadSender = payloadSenderArg;
				dummyTx = new Transaction(agent, "TestName", "TestType");
				payloadSender.QueueTransaction(dummyTx);
			});

			AsAction(() => payloadSender.QueueTransaction(dummyTx))
				.Should()
				.ThrowExactly<ObjectDisposedException>()
				.WithMessage($"*{nameof(PayloadSenderV2)}*");
		}

		private void CreateSutEnvAndTest(Action<ApmAgent, PayloadSenderV2> doAction)
		{
			var logger = new NoopLogger();
			var configReader = new TestAgentConfigurationReader(logger);
			var mockHttpMessageHandler = new MockHttpMessageHandler((r, c) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
			var service = Service.GetDefaultService(configReader, logger);
			var payloadSender = new PayloadSenderV2(logger, configReader, service, new Api.System(), mockHttpMessageHandler);

			payloadSender.Thread.IsAlive.Should().BeTrue();

			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender))) doAction(agent, payloadSender);

			payloadSender.Thread.IsAlive.Should().BeFalse();
		}

	}
}
