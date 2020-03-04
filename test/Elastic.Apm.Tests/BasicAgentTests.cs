using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Xunit;

[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.AspNetCore.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]
[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.EntityFrameworkCore.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]
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
[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.SqlClient.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]

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

		[Fact]
		public void GetCulpritTest()
		{
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
			{
				try
				{
					// Throw the exception to generate a stacktrace
					throw new Exception("TestMst");
				}
				catch(Exception e)
				{
					agent.Tracer.CaptureTransaction("TestTransaction", "TestTransactionType",
						t => { t.CaptureException(e); });
				}
			}

			payloadSender.FirstError.Culprit.Should().Be("Elastic.Apm.Tests.BasicAgentTests");
		}
		

		[Fact]
		public void GetCulprit_ShouldNotReturnNotIncludedNamespaces()
		{
			var payloadSender = new MockPayloadSender();
			var config = new MockConfigSnapshot(applicationNamespaces: "System.");
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, config:config)))
			{
				try
				{
					// Throw the exception to generate a stacktrace
					throw new Exception("TestMst");
				}
				catch(Exception e)
				{
					agent.Tracer.CaptureTransaction("TestTransaction", "TestTransactionType",
						t => { t.CaptureException(e); });
				}
			}

			payloadSender.FirstError.Culprit.Should().NotBe("Elastic.Apm.Tests.BasicAgentTests");
		}

		[Fact]
		public void GetCulprit_ShouldReturnIncludedNamespaces()
		{
			var payloadSender = new MockPayloadSender();
			var config = new MockConfigSnapshot(applicationNamespaces: "Elastic.Apm.Tests.");
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, config:config)))
			{
				try
				{
					// Throw the exception to generate a stacktrace
					throw new Exception("TestMst");
				}
				catch(Exception e)
				{
					agent.Tracer.CaptureTransaction("TestTransaction", "TestTransactionType",
						t => { t.CaptureException(e); });
				}
			}

			payloadSender.FirstError.Culprit.Should().Be("Elastic.Apm.Tests.BasicAgentTests");
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
