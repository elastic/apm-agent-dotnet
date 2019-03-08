using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Logging;
using Elastic.Apm.Model.Payload;
using Elastic.Apm.Report;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Xunit;

[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.AspNetCore.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]


namespace Elastic.Apm.Tests
{
	/// <summary>
	/// Very basic agent related tests
	/// </summary>
	public class BasicAgentTests
	{
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
				agent.Service.Agent.Version.Should().Be(Assembly.Load("Elastic.Apm").GetName().Version.ToString());
			}
		}

		/// <summary>
		/// Starts a custom span with name length > 1024.
		/// Makes sure that the name is truncated.
		/// Reason: server rejects spans with name length > 1024.
		/// </summary>
		[Fact]
		public void SpanNameLengthTest()
		{
			var spanName = new StringBuilder();

			for (var i = 0; i < 1030; i++) spanName.Append('a');

			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			agent.Tracer.CaptureTransaction("TestTransaction", "Test", (t) => { t.CaptureSpan(spanName.ToString(), "test", () => { }); });

			payloadSender.FirstSpan.Should().NotBeNull();
			payloadSender.FirstSpan.Name.Length.Should().Be(1024);
			spanName.ToString(0, 1021).Should().Be(payloadSender.FirstSpan.Name.Substring(0, 1021));
			payloadSender.FirstSpan.Name.Substring(1021, 3).Should().Be("...");
		}

		[Fact]
		public void PayloadSentWithBearerToken()
		{
			AuthenticationHeaderValue authHeader = null;
			var handler = new MockHttpMessageHandler((r, c) =>
			{
				authHeader = r.Headers.Authorization;
				return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
			});

			var secretToken = "SecretToken";
			var logger = ConsoleLogger.Instance;
			var payloadSender = new PayloadSenderV2(logger, new TestAgentConfigurationReader(logger, secretToken: secretToken),
				Service.GetDefaultService(new TestAgentConfigurationReader(logger)), handler);

			using (var agent = new ApmAgent(new TestAgentComponents(secretToken: secretToken, payloadSender: payloadSender)))
				agent.PayloadSender.QueueTransaction(new Transaction(agent, "TestName", "TestType"));

			// ideally, introduce a mechanism to flush payloads
			Thread.Sleep(TimeSpan.FromSeconds(2));

			authHeader.Should().NotBeNull();
			authHeader.Scheme.Should().Be("Bearer");
			authHeader.Parameter.Should().Be(secretToken);
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
				agent.Tracer.CaptureTransaction("TestTransaction", "TestTransactionType",
					(t) => { t.CaptureSpan("TestSpan", "TestSpanType", () => { }); });

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
				agent.Tracer.CaptureTransaction("TestTransaction", "TestTransactionType",
					(t) => { t.CaptureException(new Exception("TestMst")); });

			StringToByteArray(payloadSender.FirstError.Id).Should().HaveCount(8);
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
