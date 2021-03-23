// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

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
				agent.Service.Agent.Version.Should()
					.Be(typeof(Agent).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);
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
