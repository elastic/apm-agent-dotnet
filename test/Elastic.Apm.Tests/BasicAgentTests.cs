using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
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

[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.AspNetCore.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]
[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.PerfTests, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]
[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.DockerTests, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]
[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.AspNetFullFramework.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]
[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.Tests.MockApmServer, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]


namespace Elastic.Apm.Tests
{
	/// <summary>
	/// Very basic agent related tests
	/// </summary>
	public class BasicAgentTests
	{
		private readonly IApmLogger _logger;

		public BasicAgentTests(ITestOutputHelper xUnitOutputHelper) =>
			_logger = new XunitOutputLogger(xUnitOutputHelper).Scoped(nameof(BasicAgentTests));

		/// <summary>
		/// Creates a simple transaction.
		/// Makes sure that the agent reports the transaction with the correct agent version,
		/// which is the version of the Elastic.Apm assembly.
		/// </summary>
		[Fact]
		public void AgentVersion()
		{
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
			{
				agent.Tracer.CaptureTransaction("TestName", "TestType", () => { Thread.Sleep(5); });
				agent.Service.Agent.Version.Should()
					.Be(typeof(Agent).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);
			}
		}

		[Fact]
		public async Task PayloadSentWithBearerToken()
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
			var configReader = new TestAgentConfigurationReader(_logger, secretToken: secretToken, flushInterval: "1s");
			var payloadSender = new PayloadSenderV2(_logger, configReader,
				Service.GetDefaultService(configReader, noopLogger), new Api.System(), handler);

			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, configurationReader: configReader)))
			{
				agent.PayloadSender.QueueTransaction(new Transaction(agent, "TestName", "TestType"));
				var completedTask = await Task.WhenAny(isRequestFinished.Task, Task.Delay(10_000)); // 10 seconds time out
				completedTask.Should().Be(isRequestFinished.Task);
			}

			authHeader.Should().NotBeNull();
			authHeader.Scheme.Should().Be("Bearer");
			authHeader.Parameter.Should().Be(secretToken);
		}

		[Fact]
		public async Task PayloadSentWithProperUserAgent()
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
		public void PayloadSender_enforces_MaxQueueEventCount()
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
		public async Task PayloadSender_enforces_MaxQueueEventCount_after_first_send()
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

		/// <summary>
		/// Creates 1 span and 1 transaction.
		/// Makes sure that the ids have the correct lengths.
		/// </summary>
		[Fact]
		public void SpanAndTransactionIdsLength()
		{
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
			{
				agent.Tracer.CaptureTransaction("TestTransaction", "TestTransactionType",
					t => { t.CaptureSpan("TestSpan", "TestSpanType", () => { }); });
			}

			StringToByteArray(payloadSender.FirstTransaction.Id).Should().HaveCount(8);
			StringToByteArray(payloadSender.FirstTransaction.TraceId).Should().HaveCount(16);
			StringToByteArray(payloadSender.FirstSpan.TraceId).Should().HaveCount(16);
			StringToByteArray(payloadSender.FirstSpan.Id).Should().HaveCount(8);
			StringToByteArray(payloadSender.FirstSpan.TransactionId).Should().HaveCount(8);
		}

		/// <summary>
		/// Captures 1 error.
		/// Makes sure that the ids on the error have the correct length.
		/// </summary>
		[Fact]
		public void ErrorIdsLength()
		{
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
			{
				agent.Tracer.CaptureTransaction("TestTransaction", "TestTransactionType",
					t => { t.CaptureException(new Exception("TestMst")); });
			}

			StringToByteArray(payloadSender.FirstError.Id).Should().HaveCount(16);
			StringToByteArray(payloadSender.FirstError.TraceId).Should().HaveCount(16);
			StringToByteArray(payloadSender.FirstError.ParentId).Should().HaveCount(8);
			StringToByteArray(payloadSender.FirstError.TransactionId).Should().HaveCount(8);
		}

		private static IEnumerable<byte> StringToByteArray(string hex)
		{
			var numberChars = hex.Length;
			var bytes = new byte[numberChars / 2];
			for (var i = 0; i < numberChars; i += 2)
				bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
			return bytes;
		}
	}
}
