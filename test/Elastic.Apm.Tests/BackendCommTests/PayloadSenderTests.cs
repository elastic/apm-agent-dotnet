// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.BackendComm;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;
using Elastic.Apm.Report;
using Elastic.Apm.ServerInfo;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using FluentAssertions.Extensions;
using RichardSzalay.MockHttp;
using Xunit;
using Xunit.Abstractions;
using MockHttpMessageHandler = Elastic.Apm.Tests.Utilities.MockHttpMessageHandler;

namespace Elastic.Apm.Tests.BackendCommTests
{
	public class PayloadSenderTests : LoggingTestBase
	{
		private const string ThisClassName = nameof(PayloadSenderTests);

		private static readonly IEnumerable<TimeSpan?> FlushIntervalVariants = new TimeSpan?[]
		{
			null, ConfigConsts.DefaultValues.FlushIntervalInMilliseconds.Milliseconds(), TimeSpan.Zero, 10.Milliseconds(), 100.Milliseconds(),
			1.Seconds(), 1.Hours(), 1.Days()
		};

		private static readonly TimeSpan VeryLongFlushInterval = 1.Hours();
		private static readonly TimeSpan VeryShortFlushInterval = 1.Seconds();
		private readonly IApmLogger _logger;

		public PayloadSenderTests(ITestOutputHelper xUnitOutputHelper) : base(xUnitOutputHelper /*, LogLevel.Debug */) =>
			_logger = LoggerBase.Scoped(ThisClassName);

		public static IEnumerable<object[]> TestArgsVariantsWithVeryLongFlushInterval =>
			TestArgsVariants(args => args.FlushInterval.HasValue && args.FlushInterval >= VeryLongFlushInterval).Select(t => new object[] { t });


		[Fact]
		public void Should_Sanitize_HttpRequestMessage_In_Log()
		{
			var testLogger = new TestLogger(LogLevel.Trace);
			var secretToken = "secretToken";
			var serverUrl = "http://username:password@localhost:8200";

			var config = new MockConfiguration(testLogger, logLevel: "Trace", serverUrl: serverUrl, secretToken: secretToken, flushInterval: "0");
			var service = Service.GetDefaultService(config, testLogger);
			var waitHandle = new ManualResetEvent(false);
			var handler = new RichardSzalay.MockHttp.MockHttpMessageHandler();
			var configUrl = BackendCommUtils.ApmServerEndpoints
				.BuildIntakeV2EventsAbsoluteUrl(config.ServerUrl);

			handler.When(configUrl.AbsoluteUri)
				.Respond(_ =>
				{
					waitHandle.Set();
					return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
				});

			var payloadSender = new PayloadSenderV2(testLogger, config, service, new Api.System(), MockApmServerInfo.Version710, handler);
			using var agent = new ApmAgent(new TestAgentComponents(LoggerBase, config, payloadSender));
			agent.PayloadSender.QueueTransaction(new Transaction(agent, "TestName", "TestType"));

			waitHandle.WaitOne();

			var count = 0;
			while (!testLogger.Log.Contains("Failed sending event.")
				   && count < 10)
			{
				Thread.Sleep(500);
				count++;
			}

			testLogger.Log.Should()
				.NotContain(secretToken)
				.And.Contain("http://[REDACTED]:[REDACTED]@localhost:8200")
				.And.NotContain(serverUrl);
		}

		[Fact]
		public async Task SecretToken_ShouldBeSent_WhenApiKeyIsNotSpecified()
		{
			// Arrange
			const string secretToken = "SecretToken";

			var isRequestFinished = new TaskCompletionSource<object>();

			AuthenticationHeaderValue authHeader = null;
			var handler = new MockHttpMessageHandler((r, _) =>
			{
				authHeader = r.Headers.Authorization;
				isRequestFinished.SetResult(null);
				return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
			});

			var noopLogger = new NoopLogger();
			var mockConfig = new MockConfiguration(_logger, secretToken: secretToken, maxBatchEventCount: "1");
			var payloadSender = new PayloadSenderV2(_logger, mockConfig,
				Service.GetDefaultService(mockConfig, noopLogger), new Api.System(), MockApmServerInfo.Version710, handler, /* dbgName: */
				TestDisplayName);

			// Act
			using (var agent = new ApmAgent(new TestAgentComponents(LoggerBase, mockConfig, payloadSender)))
			{
				agent.PayloadSender.QueueTransaction(new Transaction(agent, "TestName", "TestType"));
				await isRequestFinished.Task;
			}

			// Assert
			authHeader.Should().NotBeNull();
			authHeader.Scheme.Should().Be("Bearer");
			authHeader.Parameter.Should().Be(secretToken);
		}

		[Fact]
		public async Task ApiKey_ShouldBeSent_WhenApiKeyAndSecretTokenAreSpecified()
		{
			// Arrange
			const string secretToken = "SecretToken";
			const string apiKey = "ApiKey";

			var isRequestFinished = new TaskCompletionSource<object>();

			AuthenticationHeaderValue authHeader = null;
			var handler = new MockHttpMessageHandler((r, _) =>
			{
				authHeader = r.Headers.Authorization;
				isRequestFinished.SetResult(null);
				return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
			});

			var noopLogger = new NoopLogger();
			var mockConfig = new MockConfiguration(_logger, secretToken: secretToken, apiKey: apiKey, maxBatchEventCount: "1");
			var payloadSender = new PayloadSenderV2(_logger, mockConfig,
				Service.GetDefaultService(mockConfig, noopLogger), new Api.System(), MockApmServerInfo.Version710, handler, /* dbgName: */
				TestDisplayName);

			// Act
			using (var agent = new ApmAgent(new TestAgentComponents(LoggerBase, mockConfig, payloadSender)))
			{
				agent.PayloadSender.QueueTransaction(new Transaction(agent, "TestName", "TestType"));
				await isRequestFinished.Task;
			}

			// Assert
			authHeader.Should().NotBeNull();
			authHeader.Scheme.Should().Be("ApiKey");
			authHeader.Parameter.Should().Be(apiKey);
		}

		[Fact]
		public async Task UserAgent_test()
		{
			var isRequestFinished = new TaskCompletionSource<object>();

			HttpHeaderValueCollection<ProductInfoHeaderValue> userAgentHeader = null;
			var handler = new MockHttpMessageHandler((r, _) =>
			{
				userAgentHeader = r.Headers.UserAgent;
				isRequestFinished.SetResult(null);
				return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
			});

			var logger = new NoopLogger();
			var service = Service.GetDefaultService(new MockConfiguration(logger), logger);
			var payloadSender = new PayloadSenderV2(logger, new MockConfiguration(logger, flushInterval: "1s"),
				service, new Api.System(), MockApmServerInfo.Version710, handler, /* dbgName: */ TestDisplayName);

			using (var agent = new ApmAgent(new TestAgentComponents(LoggerBase, payloadSender: payloadSender)))
			{
				agent.PayloadSender.QueueTransaction(new Transaction(agent, "TestName", "TestType"));
				await isRequestFinished.Task;
			}

			var headerValues = userAgentHeader.ToList();

			headerValues
				.Should()
				.NotBeEmpty()
				.And.HaveCount(4);

			headerValues[0].Product.Name.Should().Be($"apm-agent-{Consts.AgentName}");
			headerValues[0].Product.Version.Should().NotBeEmpty();

			// (<service name> <service version>)
			headerValues[1].Comment.Should().StartWith("(").And.EndWith(")");

			headerValues[2].Product.Name.Should().Be("System.Net.Http");
			headerValues[2].Product.Version.Should().NotBeEmpty();

			headerValues[3].Product.Name.Should().NotBeEmpty();
			headerValues[3].Product.Version.Should().NotBeEmpty();
		}

		private static IEnumerable<TestArgs> TestArgsVariantsWithoutIndex()
		{
			yield return new TestArgs();

			var maxQueueEventCountVariants = new int?[] { null, 1, 2, 3, 10, ConfigConsts.DefaultValues.MaxQueueEventCount };
			var batchVsQueueCountDeltas = new[] { -2, -1, 0 };

			foreach (var flushInterval in FlushIntervalVariants)
			{
				foreach (var maxQueueEventCount in maxQueueEventCountVariants)
				{
					if (maxQueueEventCount == null)
						continue;

					foreach (var delta in batchVsQueueCountDeltas)
					{
						var maxBatchEventCount = maxQueueEventCount + delta;
						if (maxBatchEventCount < 1)
							continue;

						yield return new TestArgs
						{
							FlushInterval = flushInterval,
							MaxBatchEventCount = maxBatchEventCount,
							MaxQueueEventCount = maxQueueEventCount
						};
					}
				}
			}
		}

		private static IEnumerable<TestArgs> TestArgsVariants(Func<TestArgs, bool> predicate = null)
		{
			var counter = 0;
			foreach (var argsVariant in TestArgsVariantsWithoutIndex())
			{
				if (predicate != null && !predicate(argsVariant))
					continue;

				argsVariant.ArgsIndex = counter++;
				yield return argsVariant;
			}
		}

		private static async Task<bool> EnqueueDummyEvent(PayloadSenderV2 payloadSender, ApmAgent agent, int txIndex) =>
			await payloadSender.EnqueueEventInternal(new Transaction(agent, $"Tx #{txIndex}", "TestType"), "Transaction");

		public static IEnumerable<object[]> MaxBatchEventCount_test_variants()
		{
			var numberOfBatchesVariants = new[] { 1, 2, 3, 10 };
			foreach (var args in TestArgsVariantsWithVeryLongFlushInterval)
			{
				foreach (var numberOfBatches in numberOfBatchesVariants)
					yield return new[] { args[0], numberOfBatches };
			}
		}

		[Theory]
		[MemberData(nameof(MaxBatchEventCount_test_variants))]
		internal async Task MaxBatchEventCount_test(TestArgs args, int expectedNumberOfBatches)
		{
			var expectedNumberOfBatchesSentTcs = new TaskCompletionSource<object>();

			var actualNumberOfBatches = 0;
			var handler = new MockHttpMessageHandler((_, _) =>
			{
				if (Interlocked.Increment(ref actualNumberOfBatches) == expectedNumberOfBatches)
					expectedNumberOfBatchesSentTcs.SetResult(null);
				return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
			});

			var configurationReader = args.BuildConfig(_logger);
			var service = Service.GetDefaultService(configurationReader, _logger);
			var payloadSender = new PayloadSenderV2(_logger, configurationReader, service, new Api.System(), MockApmServerInfo.Version710, handler
				, /* dbgName: */ TestDisplayName);

			using (var agent = new ApmAgent(new TestAgentComponents(_logger, payloadSender: payloadSender)))
			{
				var numberOfEventsEnqueuedSuccessfully = 0;
				for (var txIndex = 1; ; ++txIndex)
				{
					if (await EnqueueDummyEvent(payloadSender, agent, txIndex))
						++numberOfEventsEnqueuedSuccessfully;

					if (numberOfEventsEnqueuedSuccessfully == expectedNumberOfBatches * args.MaxBatchEventCount)
						break;
				}

				(await Task.WhenAny(expectedNumberOfBatchesSentTcs.Task, Task.Delay(30.Seconds())))
					.Should()
					.Be(expectedNumberOfBatchesSentTcs.Task
						, $"because numberOfEventsEnqueuedSuccessfully: {numberOfEventsEnqueuedSuccessfully}," +
						$"actualNumberOfBatches: {actualNumberOfBatches} ");
			}
		}

		public static IEnumerable<object[]> FlushInterval_test_variants()
		{
			var argsVariantsCounter = 0;
			var numberOfEventsToSendVariants = new[] { 1, 2, 3, 10 };

			foreach (var flushInterval in FlushIntervalVariants.Where(x => x.HasValue && x.Value <= VeryShortFlushInterval))
			{
				foreach (var numberOfEventsToSend in numberOfEventsToSendVariants)
				{
					yield return new object[]
					{
						new TestArgs { ArgsIndex = argsVariantsCounter++, FlushInterval = flushInterval }, numberOfEventsToSend
					};
				}
			}
		}

		[Theory]
		[MemberData(nameof(FlushInterval_test_variants))]
		internal async Task FlushInterval_test(TestArgs args, int numberOfEventsToSend)
		{
			var batchSentBarrier = new Barrier(2);
			var barrierTimeout = 30.Seconds();

			var handler = new MockHttpMessageHandler((_, _) =>
			{
				batchSentBarrier.SignalAndWait(barrierTimeout).Should().BeTrue();
				return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
			});

			var configurationReader = args.BuildConfig(_logger);
			var service = Service.GetDefaultService(configurationReader, _logger);
			var payloadSender = new PayloadSenderV2(_logger, configurationReader, service, new Api.System(), MockApmServerInfo.Version710,
				handler, /* dbgName: */ TestDisplayName);

			using (var agent = new ApmAgent(new TestAgentComponents(_logger, payloadSender: payloadSender)))
			{
				for (var eventIndex = 1; eventIndex <= numberOfEventsToSend; ++eventIndex)
				{
					(await EnqueueDummyEvent(payloadSender, agent, eventIndex)).Should().BeTrue($"eventIndex: {eventIndex}, args: {args}");
					batchSentBarrier.SignalAndWait(barrierTimeout).Should().BeTrue($"eventIndex: {eventIndex}, args: {args}");
				}
			}
		}

		[Fact]
		public void Dispose_stops_the_thread()
		{
			PayloadSenderV2 lastPayloadSender = null;
			CreateSutEnvAndTest((_, payloadSender) =>
			{
				lastPayloadSender = payloadSender;
				lastPayloadSender.IsRunning.Should().BeTrue();
			});
			lastPayloadSender.IsRunning.Should().BeFalse();

			CreateSutEnvAndTest((agent, payloadSender) =>
			{
				lastPayloadSender = payloadSender;
				lastPayloadSender.IsRunning.Should().BeTrue();

				payloadSender.QueueTransaction(new Transaction(agent, "TestName", "TestType"));
			});
			lastPayloadSender.IsRunning.Should().BeFalse();
		}

		/// <summary>
		/// Makes sure <see cref="BackendCommComponentBase.Dispose"/> finishes without exception and doesn't cause deadlock.
		/// </summary>
		[Fact]
		public void PayloadSenderV2DisposeTest()
		{
			using (var payloadSenderV2 = new PayloadSenderV2(new NoopLogger(), new MockConfiguration(),
					   Service.GetDefaultService(new MockConfiguration(), new NoopLogger()), new Api.System(),
					   new ApmServerInfo()))
				Thread.Sleep(1000);
		}

		private void CreateSutEnvAndTest(Action<ApmAgent, PayloadSenderV2> doAction)
		{
			var configReader = new MockConfiguration(_logger);
			var mockHttpMessageHandler = new MockHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
			var service = Service.GetDefaultService(configReader, _logger);
			var payloadSender = new PayloadSenderV2(_logger, configReader, service, new Api.System(), MockApmServerInfo.Version710,
				mockHttpMessageHandler
				, /* dbgName: */ TestDisplayName);

			payloadSender.IsRunning.Should().BeTrue();

			using (var agent = new ApmAgent(new TestAgentComponents(LoggerBase, payloadSender: payloadSender)))
				doAction(agent, payloadSender);

			payloadSender.IsRunning.Should().BeFalse();
		}

		/// <summary>
		/// Regression test for https://github.com/elastic/apm-agent-dotnet/issues/288.
		/// Events queued just before Dispose must not be silently dropped.
		/// </summary>
		[Fact]
		public async Task Dispose_sends_queued_events_before_stopping()
		{
			var received = new List<string>();
			var handler = new MockHttpMessageHandler((request, _) =>
			{
				var body = request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
				lock (received)
					received.Add(body);
				return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
			});

			var config = new MockConfiguration(_logger, flushInterval: "3600s");
			var service = Service.GetDefaultService(config, _logger);
			var payloadSender = new PayloadSenderV2(_logger, config, service, new Api.System(),
				MockApmServerInfo.Version710, handler, TestDisplayName);

			using var agent = new ApmAgent(new TestAgentComponents(_logger, payloadSender: payloadSender));

			// Use EnqueueEventInternal directly (awaitable) so the item is guaranteed to be
			// in the BatchBlock before Dispose is called. EnqueueEvent (fire-and-forget) has
			// a race with Dispose where the item may not be counted yet.
			await payloadSender.EnqueueEventInternal(
				new Transaction(agent, "TestTransaction", "TestType"), "Transaction");

			agent.Dispose();

			lock (received)
			{
				received.Should().NotBeEmpty("Dispose must flush queued events before cancelling");
				received.Any(r => r.Contains("\"transaction\"")).Should().BeTrue();
			}
		}

		/// <summary>
		/// Agent.FlushAsync() and the IApmAgent extension must complete without deadlock and
		/// signal only after all queued events have been sent.
		/// </summary>
		[Fact]
		public async Task FlushAsync_waits_for_events_to_be_sent()
		{
			var sendCount = 0;
			var handler = new MockHttpMessageHandler((_, _) =>
			{
				Interlocked.Increment(ref sendCount);
				return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
			});

			var config = new MockConfiguration(_logger, flushInterval: "3600s");
			var service = Service.GetDefaultService(config, _logger);
			using var payloadSender = new PayloadSenderV2(_logger, config, service, new Api.System(),
				MockApmServerInfo.Version710, handler, TestDisplayName);

			using var agent = new ApmAgent(new TestAgentComponents(_logger, payloadSender: payloadSender));

			// Use EnqueueEventInternal directly so the item is in the BatchBlock before FlushAsync,
			// avoiding a race with the fire-and-forget Task.Run in EnqueueEvent.
			await payloadSender.EnqueueEventInternal(
				new Transaction(agent, "TestTransaction", "TestType"), "Transaction");

			await agent.FlushAsync(new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);

			sendCount.Should().Be(1, "FlushAsync must return only after the HTTP POST completes");
		}

		/// <summary>
		/// FlushAsync on an empty queue must return immediately without deadlock.
		/// </summary>
		[Fact]
		public async Task FlushAsync_on_empty_queue_returns_immediately()
		{
			var config = new MockConfiguration(_logger);
			var service = Service.GetDefaultService(config, _logger);
			using var payloadSender = new PayloadSenderV2(_logger, config, service, new Api.System(),
				MockApmServerInfo.Version710, dbgName: TestDisplayName);

			using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
			await payloadSender.FlushAsync(cts.Token);
		}

		/// <summary>
		/// FlushAsync via the public QueueTransaction path (EnqueueEvent → Task.Run) must wait
		/// for the HTTP send to complete. Regression for the synchronous count-increment fix:
		/// _eventQueueCount is incremented before Task.Run so FlushAsync cannot miss the event.
		/// </summary>
		[Fact]
		public async Task FlushAsync_via_public_QueueTransaction_waits_for_send()
		{
			var sendCount = 0;
			var handler = new MockHttpMessageHandler((_, _) =>
			{
				Interlocked.Increment(ref sendCount);
				return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
			});

			// Long flushInterval: the only batch trigger comes from FlushAsync's TriggerBatch calls.
			var config = new MockConfiguration(_logger, flushInterval: "3600s", maxBatchEventCount: "1");
			var service = Service.GetDefaultService(config, _logger);
			using var payloadSender = new PayloadSenderV2(_logger, config, service, new Api.System(),
				MockApmServerInfo.Version710, handler, TestDisplayName);
			using var agent = new ApmAgent(new TestAgentComponents(_logger, payloadSender: payloadSender));

			// Public path: QueueTransaction calls EnqueueEvent which now increments _eventQueueCount
			// synchronously, so FlushAsync will see the event even before Task.Run executes.
			agent.PayloadSender.QueueTransaction(new Transaction(agent, "TestTransaction", "TestType"));

			await agent.FlushAsync(new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);

			sendCount.Should().Be(1, "FlushAsync must not return before the HTTP POST completes");
		}

		/// <summary>
		/// FlushAsync called while a batch has already been received by the work loop (so
		/// _eventQueueCount is already 0) but ProcessQueueItems has not yet finished must still
		/// wait for the HTTP send to complete. Regression for the _inFlightSends-in-ReceiveBatch fix.
		/// </summary>
		[Fact]
		public async Task FlushAsync_waits_when_called_while_batch_is_in_flight()
		{
			var sendStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			var sendCanComplete = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			var sendCount = 0;

			var handler = new MockHttpMessageHandler(async (_, _) =>
			{
				sendStarted.TrySetResult(true);
				await sendCanComplete.Task;
				Interlocked.Increment(ref sendCount);
				return new HttpResponseMessage(HttpStatusCode.Accepted);
			});

			var config = new MockConfiguration(_logger, flushInterval: "100ms", maxBatchEventCount: "1");
			var service = Service.GetDefaultService(config, _logger);
			using var payloadSender = new PayloadSenderV2(_logger, config, service, new Api.System(),
				MockApmServerInfo.Version710, handler, TestDisplayName);
			using var agent = new ApmAgent(new TestAgentComponents(_logger, payloadSender: payloadSender));

			await payloadSender.EnqueueEventInternal(
				new Transaction(agent, "TestTransaction", "TestType"), "Transaction");

			// Wait until the HTTP handler has started: at this point _eventQueueCount is already 0
			// (decremented by ReceiveBatch) but _inFlightSends is 1 (incremented in ReceiveBatch
			// before the decrement), so FlushAsync must not take the fast path.
			await sendStarted.Task;

			var flushTask = agent.FlushAsync(new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);

			// Yield so FlushAsync evaluates the counters; it must not have completed yet.
			// 500 ms gives generous headroom on loaded CI runners (Windows timer resolution is ~15 ms).
			await Task.Delay(500);
			flushTask.IsCompleted.Should().BeFalse("FlushAsync must wait while the HTTP send is still in progress");

			sendCanComplete.TrySetResult(true);
			await flushTask;

			sendCount.Should().Be(1, "FlushAsync must return only after the HTTP POST completes");
		}

		/// <summary>
		/// Regression guard for the serialization-buffer reuse optimisation.
		///
		/// <para>
		/// Before the fix, <see cref="PayloadSenderV2"/> allocated a new
		/// <c>MemoryStream</c> for every outgoing batch, which caused LOH pressure
		/// under sustained load.  After the fix a single buffer is reused via
		/// <c>SetLength(0)</c>.
		/// </para>
		///
		/// <para>
		/// This test verifies that the reset is complete: data written in batch A
		/// must not bleed into the body received by the server for batch B.
		/// </para>
		/// </summary>
		[Fact]
		public async Task SequentialBatches_SerializationBufferIsIsolated()
		{
			var receivedBodies = new List<string>();
			var handler = new MockHttpMessageHandler((request, _) =>
			{
				var body = request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
				lock (receivedBodies)
					receivedBodies.Add(body);
				return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
			});

			// flushInterval: "0" forces the work loop to send immediately after each item.
			var config = new MockConfiguration(_logger, flushInterval: "0", maxBatchEventCount: "3");
			var service = Service.GetDefaultService(config, _logger);
			using var payloadSender = new PayloadSenderV2(_logger, config, service, new Api.System(),
				MockApmServerInfo.Version710, handler, TestDisplayName);
			using var agent = new ApmAgent(new TestAgentComponents(_logger, payloadSender: payloadSender));

			// Batch A: three transactions with a distinguishable name prefix.
			for (var i = 0; i < 3; i++)
				await payloadSender.EnqueueEventInternal(
					new Transaction(agent, $"BatchA-Tx{i}", "test"), "Transaction");

			await agent.FlushAsync(new CancellationTokenSource(10.Seconds()).Token);

			// Batch B: three transactions with a different name prefix.
			for (var i = 0; i < 3; i++)
				await payloadSender.EnqueueEventInternal(
					new Transaction(agent, $"BatchB-Tx{i}", "test"), "Transaction");

			await agent.FlushAsync(new CancellationTokenSource(10.Seconds()).Token);

			string batchABody, batchBBody;
			lock (receivedBodies)
			{
				receivedBodies.Should().HaveCountGreaterOrEqualTo(2,
					"two distinct flushes must produce at least two separate HTTP POSTs");

				batchABody = receivedBodies[0];
				batchBBody = receivedBodies[receivedBodies.Count - 1];
			}

			// Batch A's body must contain only A-prefixed names.
			batchABody.Should().Contain("BatchA-Tx", "first batch must include BatchA transactions");
			batchABody.Should().NotContain("BatchB-Tx", "BatchB data must not bleed into BatchA's HTTP body");

			// Batch B's body must contain only B-prefixed names.
			batchBBody.Should().Contain("BatchB-Tx", "second batch must include BatchB transactions");
			batchBBody.Should().NotContain("BatchA-Tx", "BatchA data must not bleed into BatchB's HTTP body");
		}

		internal class TestArgs
		{
			internal int ArgsIndex { get; set; }
			internal TimeSpan? FlushInterval { get; set; }
			internal int? MaxBatchEventCount { get; set; }
			internal int? MaxQueueEventCount { get; set; }

			internal MockConfiguration BuildConfig(IApmLogger logger) =>
				new MockConfiguration(logger
					, flushInterval: FlushInterval.HasValue ? $"{FlushInterval.Value.TotalMilliseconds}ms" : null
					, maxBatchEventCount: MaxBatchEventCount?.ToString()
					, maxQueueEventCount: MaxQueueEventCount?.ToString());

			public override string ToString() => new ToStringBuilder("")
			{
				{ nameof(ArgsIndex), ArgsIndex },
				{ nameof(MaxQueueEventCount), MaxQueueEventCount },
				{ nameof(MaxBatchEventCount), MaxBatchEventCount },
				{ nameof(FlushInterval), (FlushInterval?.ToHms()).AsNullableToString() }
			}.ToString();
		}
	}
}
