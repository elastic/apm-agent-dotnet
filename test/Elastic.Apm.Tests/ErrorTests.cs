// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Diagnostics;
using Elastic.Apm.Api;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests
{
	public class ErrorTests
	{
		/// <summary>
		/// Makes sure that the error.context contains a deep copy of transaction.context and
		/// all changes on transaction.context after the error is captured are not reflected on error.context.
		/// </summary>
		[Fact]
		public void ChangeTransactionContextAfterError()
		{
			var mockPayloadSender = new MockPayloadSender();
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: mockPayloadSender));

			agent.Tracer.CaptureTransaction("Test", "Test", t =>
			{
				t.Context.Request = new Request("GET", new Url { Full = "http://localhost", Protocol = "http", Search = "abc" })
				{
					Body = "abc", Headers = new Dictionary<string, string> { { "header1", "headerValue" } }
				};
				t.Context.Response = new Response { StatusCode = 404, Finished = false };

				t.SetLabel("foo", "bar");
				// Let's capture an error
				t.CaptureError("Test Error", "Test", new StackTrace().GetFrames());

				// Let's change CurrentTransaction.Context after the error is captured
				t.Context.Request.Method = "PUT";
				t.Context.Request.Body = "cde";
				t.Context.Request.Headers["header2"] = "headerValue";
				t.Context.Request.Url.Full = "http://elastic.co";
				t.Context.Request.Url.Protocol = "tcp";
				t.Context.Request.Url.Search = "cde";
				t.Context.Response.StatusCode = 500;
				t.Context.Response.Finished = true;
				t.Context.InternalLabels.Value.InnerDictionary["foo"].Value.Should().Be("bar");

				// Asserts on the captured error
				mockPayloadSender.WaitForErrors();
				mockPayloadSender.FirstError.Should().NotBeNull("first error should not be null");
				mockPayloadSender.FirstError.Context.Should().NotBeNull("context should not be null");
				mockPayloadSender.FirstError.Context.Request.Method.Should().Be("GET");
				mockPayloadSender.FirstError.Context.Request.Body = "abc";
				mockPayloadSender.FirstError.Context.Request.Headers.Count.Should().Be(1);
				mockPayloadSender.FirstError.Context.Request.Headers["header1"] = "headerValue";
				mockPayloadSender.FirstError.Context.Request.Url.Full = "http://localhost";
				mockPayloadSender.FirstError.Context.Request.Url.Protocol = "http";
				mockPayloadSender.FirstError.Context.Request.Url.Search = "abc";
				mockPayloadSender.FirstError.Context.Response.StatusCode = 404;
				mockPayloadSender.FirstError.Context.Response.Finished = false;
				mockPayloadSender.FirstError.Context.InternalLabels.Value.InnerDictionary["foo"].Value.Should().Be("bar");
				mockPayloadSender.FirstError.Context.Response.Headers.Should().BeNull();
			});

			// Asserts on the captured transaction
			mockPayloadSender.WaitForTransactions();
			mockPayloadSender.FirstTransaction.Context.Request.Method.Should().Be("PUT");
			mockPayloadSender.FirstTransaction.Context.Request.Body = "cde";
			mockPayloadSender.FirstTransaction.Context.Request.Headers.Count.Should().Be(2);
			mockPayloadSender.FirstTransaction.Context.Request.Headers["header1"] = "headerValue";
			mockPayloadSender.FirstTransaction.Context.Request.Headers["header2"] = "headerValue";
			mockPayloadSender.FirstTransaction.Context.Request.Url.Full = "http://elastic.co";
			mockPayloadSender.FirstTransaction.Context.Request.Url.Protocol = "tcp";
			mockPayloadSender.FirstTransaction.Context.Request.Url.Search = "cde";
			mockPayloadSender.FirstTransaction.Context.Response.StatusCode = 500;
			mockPayloadSender.FirstTransaction.Context.Response.Finished = true;
			mockPayloadSender.FirstTransaction.Context.InternalLabels.Value.InnerDictionary["foo"].Value.Should().Be("bar");
			mockPayloadSender.FirstTransaction.Context.Response.Headers.Should().BeNull();
		}

		/// <summary>
		/// Makes sure that in case of empty transaction.context, error.context is also empty
		/// </summary>
		[Fact]
		public void ErrorOnEmptyTransaction()
		{
			var mockPayloadSender = new MockPayloadSender();
			using var agent = new ApmAgent(new AgentComponents(payloadSender: mockPayloadSender));

			agent.Tracer.CaptureTransaction("Test", "Test", t =>
			{
				t.CaptureError("Test Error", "Test", new StackTrace().GetFrames());

				mockPayloadSender.WaitForErrors();
				mockPayloadSender.FirstError.Should().NotBeNull("error should not be null");
				mockPayloadSender.FirstError.Context.Should().NotBeNull("error context should not be null");
				mockPayloadSender.FirstError.Context.Request.Should().BeNull();
				mockPayloadSender.FirstError.Context.Response.Should().BeNull();
			});
		}

		/// <summary>
		/// Makes sure that in case header dictionaries on transaction.context are empty then they are also empty on
		/// error.context.
		/// </summary>
		[Fact]
		public void ErrorOnTransactionWithEmptyHeaders()
		{
			var mockPayloadSender = new MockPayloadSender();
			using var agent = new ApmAgent(new AgentComponents(payloadSender: mockPayloadSender));

			agent.Tracer.CaptureTransaction("Test", "Test", t =>
			{
				t.Context.Request = new Request("GET", new Url { Full = "http://localhost" });
				t.Context.Response = new Response();

				t.CaptureError("Test Error", "Test", new StackTrace().GetFrames());
			});

			mockPayloadSender.WaitForErrors();
			mockPayloadSender.FirstError.Context.Request.Should().NotBeNull();
			mockPayloadSender.FirstError.Context.Request.Headers.Should().BeNull();
			mockPayloadSender.FirstError.Context.Response.Should().NotBeNull();
			mockPayloadSender.FirstError.Context.Response.Headers.Should().BeNull();
			mockPayloadSender.FirstError.Context.InternalLabels.Value.InnerDictionary.Should().BeEmpty();
		}
	}
}
