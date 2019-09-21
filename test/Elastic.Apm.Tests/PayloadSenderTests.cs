using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;
using Elastic.Apm.Report;
using Elastic.Apm.Tests.Mocks;
using Elastic.Apm.Tests.TestHelpers;
using FluentAssertions;
using FluentAssertions.Extensions;
using Xunit;
using Xunit.Abstractions;
using static Elastic.Apm.Tests.TestHelpers.FluentAssertionsUtils;

namespace Elastic.Apm.Tests
{
	public class PayloadSenderTests : LoggingTestBase
	{
		private const string ThisClassName = nameof(PayloadSenderTests);

		private static readonly IEnumerable<TimeSpan?> FlushIntervalVariants = new TimeSpan?[]
		{
			null,
			ConfigConsts.DefaultValues.FlushIntervalInMilliseconds.Milliseconds(),
			TimeSpan.Zero,
			10.Milliseconds(),
			100.Milliseconds(),
			1.Seconds(),
			1.Hours(),
			1.Days()
		};

		private static readonly TimeSpan VeryLongFlushInterval = 1.Hours();
		private static readonly TimeSpan VeryShortFlushInterval = 1.Seconds();
		private readonly IApmLogger _logger;

		public PayloadSenderTests(ITestOutputHelper xUnitOutputHelper) : base(xUnitOutputHelper /*, LogLevel.Debug */)
		{
			_logger = LoggerBase.Scoped(ThisClassName);
//			LoggerBase.Level = LogLevel.Debug;
		}

		public static IEnumerable<object[]> TestArgsVariantsWithVeryLongFlushInterval =>
			TestArgsVariants(args => args.FlushInterval.HasValue && args.FlushInterval >= VeryLongFlushInterval).Select(t => new object[] { t });

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
			var mockConfig = new MockConfigSnapshot(_logger, secretToken: secretToken, maxBatchEventCount: "1");
			var payloadSender = new PayloadSenderV2(_logger, mockConfig,
				Service.GetDefaultService(mockConfig, noopLogger), new Api.System(), handler, /* dbgName: */ TestDisplayName);

			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, config: mockConfig)))
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
			var service = Service.GetDefaultService(new MockConfigSnapshot(logger), logger);
			var payloadSender = new PayloadSenderV2(logger, new MockConfigSnapshot(logger, flushInterval: "1s"),
				service, new Api.System(), handler, /* dbgName: */ TestDisplayName);

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

		private static IEnumerable<TestArgs> TestArgsVariantsWithoutIndex()
		{
			yield return new TestArgs();

			var maxQueueEventCountVariants = new int?[] { null, 1, 2, 3, 10, ConfigConsts.DefaultValues.MaxQueueEventCount };
			var batchVsQueueCountDeltas = new[] { -2, -1, 0 };

			foreach (var flushInterval in FlushIntervalVariants)
			{
				foreach (var maxQueueEventCount in maxQueueEventCountVariants)
				{
					if (maxQueueEventCount == null) continue;

					foreach (var delta in batchVsQueueCountDeltas)
					{
						var maxBatchEventCount = maxQueueEventCount + delta;
						if (maxBatchEventCount < 1) continue;

						yield return new TestArgs
						{
							FlushInterval = flushInterval, MaxBatchEventCount = maxBatchEventCount, MaxQueueEventCount = maxQueueEventCount
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
				if (predicate != null && !predicate(argsVariant)) continue;

				argsVariant.ArgsIndex = counter++;
				yield return argsVariant;
			}
		}

		private static bool EnqueueDummyEvent(PayloadSenderV2 payloadSender, ApmAgent agent, int txIndex) =>
			payloadSender.EnqueueEvent(new Transaction(agent, $"Tx #{txIndex}", "TestType"), "Transaction");

		[Theory]
		[MemberData(nameof(TestArgsVariantsWithVeryLongFlushInterval))]
		internal void MaxQueueEventCount_should_be_enforced_before_send(TestArgs args)
		{
			var sendTcs = new TaskCompletionSource<object>();

			var handler = new MockHttpMessageHandler(async (r, c) =>
			{
				await sendTcs.Task;
				return new HttpResponseMessage(HttpStatusCode.OK);
			});

			var configurationReader = args.BuildConfig(_logger);
			var service = Service.GetDefaultService(configurationReader, _logger);
			var payloadSender = new PayloadSenderV2(_logger, configurationReader, service, new Api.System(), handler, /* dbgName: */ TestDisplayName);

			using (var agent = new ApmAgent(new TestAgentComponents(_logger, payloadSender: payloadSender)))
			{
				int? txIndexResumedEnqueuing = null;
				for (var txIndex = 1; txIndex <= args.MaxQueueEventCount + args.MaxBatchEventCount + 10; ++txIndex)
				{
					var enqueuedSuccessfully = EnqueueDummyEvent(payloadSender, agent, txIndex);

					if (txIndex <= args.MaxQueueEventCount)
					{
						enqueuedSuccessfully.Should().BeTrue($"txIndex: {txIndex}, args: {args}");
						continue;
					}

					// It's possible that the events for the first batch have already been dequeued
					// so we can be sure that queue doesn't have any free space left only after MaxQueueEventCount + MaxBatchEventCount events

					if (enqueuedSuccessfully && !txIndexResumedEnqueuing.HasValue) txIndexResumedEnqueuing = txIndex;

					enqueuedSuccessfully.Should()
						.Be(txIndex - txIndexResumedEnqueuing < args.MaxBatchEventCount
							, $"txIndex: {txIndex}, txIndexResumedEnqueuing: {txIndexResumedEnqueuing}, args: {args}");
				}

				sendTcs.SetResult(null);
			}
		}

		[Theory]
		[MemberData(nameof(TestArgsVariantsWithVeryLongFlushInterval))]
		internal async Task MaxQueueEventCount_should_be_enforced_after_send(TestArgs args)
		{
			var sendTcs = new TaskCompletionSource<object>();
			var firstBatchDequeuedTcs = new TaskCompletionSource<object>();

			var handler = new MockHttpMessageHandler(async (r, c) =>
			{
				firstBatchDequeuedTcs.SetResult(null);
				await sendTcs.Task;
				return new HttpResponseMessage(HttpStatusCode.OK);
			});

			var configurationReader = args.BuildConfig(_logger);
			var service = Service.GetDefaultService(configurationReader, _logger);
			var payloadSender = new PayloadSenderV2(_logger, configurationReader, service, new Api.System(), handler, /* dbgName: */ TestDisplayName);

			using (var agent = new ApmAgent(new TestAgentComponents(_logger, payloadSender: payloadSender /*, useRealCentralConfigFetcher: true */
				, dbgName: TestDisplayName)))
			{
				var txIndex = 1;
				for (; txIndex <= args.MaxQueueEventCount; ++txIndex)
					EnqueueDummyEvent(payloadSender, agent, txIndex).Should().BeTrue($"txIndex: {txIndex}, args: {args}");

				await firstBatchDequeuedTcs.Task;

				for (; txIndex <= args.MaxQueueEventCount + args.MaxBatchEventCount + 10; ++txIndex)
				{
					EnqueueDummyEvent(payloadSender, agent, txIndex)
						.Should()
						.Be(txIndex <= args.MaxQueueEventCount + args.MaxBatchEventCount
							, $"txIndex: {txIndex}, args: {args}");
				}

				sendTcs.SetResult(null);
			}
		}

		[Theory]
		[MemberData(nameof(TestArgsVariantsWithVeryLongFlushInterval))]
		internal async Task MaxBatchEventCount_test(TestArgs args)
		{
			foreach (var expectedNumberOfBatches in new[] { 1, 2, 3, 10 })
			{
				_logger.Debug()?.Log("Starting sub-test... args: {args}", args);

				var expectedNumberOfBatchesSentTcs = new TaskCompletionSource<object>();

				var actualNumberOfBatches = 0;
				var handler = new MockHttpMessageHandler((r, c) =>
				{
					if (Interlocked.Increment(ref actualNumberOfBatches) == expectedNumberOfBatches)
						expectedNumberOfBatchesSentTcs.SetResult(null);
					return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
				});

				var configurationReader = args.BuildConfig(_logger);
				var service = Service.GetDefaultService(configurationReader, _logger);
				var payloadSender = new PayloadSenderV2(_logger, configurationReader, service, new Api.System(), handler
					, /* dbgName: */ TestDisplayName);

				using (var agent = new ApmAgent(new TestAgentComponents(_logger, payloadSender: payloadSender)))
				{
					var numberOfEventsEnqueuedSuccessfully = 0;
					for (var txIndex = 1;; ++txIndex)
					{
						if (EnqueueDummyEvent(payloadSender, agent, txIndex))
							++numberOfEventsEnqueuedSuccessfully;
						else
							Thread.Yield();

						if (numberOfEventsEnqueuedSuccessfully == expectedNumberOfBatches * args.MaxBatchEventCount)
							break;
					}

					(await Task.WhenAny(expectedNumberOfBatchesSentTcs.Task, Task.Delay(30.Seconds())))
						.Should()
						.Be(expectedNumberOfBatchesSentTcs.Task
							, $"because numberOfEventsEnqueuedSuccessfully: {numberOfEventsEnqueuedSuccessfully}");
				}
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
						{ new TestArgs { ArgsIndex = argsVariantsCounter++, FlushInterval = flushInterval }, numberOfEventsToSend };
				}
			}
		}

		[Theory]
		[MemberData(nameof(FlushInterval_test_variants))]
		internal void FlushInterval_test(TestArgs args, int numberOfEventsToSend)
		{
			var batchSentBarrier = new Barrier(2);
			var barrierTimeout = 30.Seconds();

			var handler = new MockHttpMessageHandler((r, c) =>
			{
				batchSentBarrier.SignalAndWait(barrierTimeout).Should().BeTrue();
				return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
			});

			var configurationReader = args.BuildConfig(_logger);
			var service = Service.GetDefaultService(configurationReader, _logger);
			var payloadSender = new PayloadSenderV2(_logger, configurationReader, service, new Api.System(), handler, /* dbgName: */ TestDisplayName);

			using (var agent = new ApmAgent(new TestAgentComponents(_logger, payloadSender: payloadSender)))
			{
				for (var txIndex = 1; txIndex <= numberOfEventsToSend; ++txIndex)
				{
					EnqueueDummyEvent(payloadSender, agent, txIndex).Should().BeTrue($"txIndex: {txIndex}, args: {args}");
					batchSentBarrier.SignalAndWait(barrierTimeout).Should().BeTrue($"txIndex: {txIndex}, args: {args}");
				}
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
			var configReader = new MockConfigSnapshot(_logger);
			var mockHttpMessageHandler = new MockHttpMessageHandler((r, c) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
			var service = Service.GetDefaultService(configReader, _logger);
			var payloadSender = new PayloadSenderV2(_logger, configReader, service, new Api.System(), mockHttpMessageHandler
				, /* dbgName: */ TestDisplayName);

			payloadSender.Thread.IsAlive.Should().BeTrue();

			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender))) doAction(agent, payloadSender);

			payloadSender.Thread.IsAlive.Should().BeFalse();
		}

		internal class TestArgs
		{
			internal int ArgsIndex { get; set; }
			internal TimeSpan? FlushInterval { get; set; }
			internal int? MaxBatchEventCount { get; set; }
			internal int? MaxQueueEventCount { get; set; }

			internal MockConfigSnapshot BuildConfig(IApmLogger logger) =>
				new MockConfigSnapshot(logger
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
