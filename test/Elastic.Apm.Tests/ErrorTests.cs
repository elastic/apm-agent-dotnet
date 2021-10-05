// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Elastic.Apm.Api;
using Elastic.Apm.Logging;
using Elastic.Apm.Report;
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
				mockPayloadSender.FirstError.Context.Request.Body.Should().Be("abc");
				mockPayloadSender.FirstError.Context.Request.Headers.Count.Should().Be(1);
				mockPayloadSender.FirstError.Context.Request.Headers["header1"].Should().Be("headerValue");
				mockPayloadSender.FirstError.Context.Request.Url.Full.Should().Be("http://localhost");
				mockPayloadSender.FirstError.Context.Request.Url.Protocol.Should().Be("http");
				mockPayloadSender.FirstError.Context.Request.Url.Search.Should().Be("abc");
				mockPayloadSender.FirstError.Context.Response.StatusCode.Should().Be(404);
				mockPayloadSender.FirstError.Context.Response.Finished.Should().BeFalse();
				mockPayloadSender.FirstError.Context.InternalLabels.Value.InnerDictionary["foo"].Value.Should().Be("bar");
				mockPayloadSender.FirstError.Context.Response.Headers.Should().BeNull();
			});

			// Asserts on the captured transaction
			mockPayloadSender.WaitForTransactions();
			mockPayloadSender.FirstTransaction.Context.Request.Method.Should().Be("PUT");
			mockPayloadSender.FirstTransaction.Context.Request.Body.Should().Be("cde");
			mockPayloadSender.FirstTransaction.Context.Request.Headers.Count.Should().Be(2);
			mockPayloadSender.FirstTransaction.Context.Request.Headers["header1"].Should().Be("headerValue");
			mockPayloadSender.FirstTransaction.Context.Request.Headers["header2"].Should().Be("headerValue");
			mockPayloadSender.FirstTransaction.Context.Request.Url.Full.Should().Be("http://elastic.co");
			mockPayloadSender.FirstTransaction.Context.Request.Url.Protocol.Should().Be("tcp");
			mockPayloadSender.FirstTransaction.Context.Request.Url.Search.Should().Be("cde");
			mockPayloadSender.FirstTransaction.Context.Response.StatusCode.Should().Be(500);
			mockPayloadSender.FirstTransaction.Context.Response.Finished.Should().BeTrue();
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
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: mockPayloadSender));

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
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: mockPayloadSender));

			agent.Tracer.CaptureTransaction("Test", "Test", t =>
			{
				t.Context.Request = new Request("GET", new Url { Full = "http://localhost" });
				t.Context.Response = new Response();

				t.CaptureError("Test Error", "Test", new StackTrace().GetFrames());
			});

			mockPayloadSender.WaitForErrors();
			mockPayloadSender.FirstError.Should().NotBeNull();
			mockPayloadSender.FirstError.Context.Should().NotBeNull();
			mockPayloadSender.FirstError.Context.Request.Should().NotBeNull();
			mockPayloadSender.FirstError.Context.Request.Headers.Should().BeNull();
			mockPayloadSender.FirstError.Context.Response.Should().NotBeNull();
			mockPayloadSender.FirstError.Context.Response.Headers.Should().BeNull();
			mockPayloadSender.FirstError.Context.InternalLabels.Value.InnerDictionary.Should().BeEmpty();
		}

		[Fact]
		public void Includes_Cause_When_Exception_Has_InnerException()
		{
			var mockPayloadSender = new MockPayloadSender();
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: mockPayloadSender));

			agent.Tracer.CaptureTransaction("Test", "Test", t =>
			{
				var exception = new Exception(
					"Outer exception",
					new Exception("Inner exception", new Exception("Inner inner exception")));

				t.CaptureException(exception);
			});

			mockPayloadSender.WaitForErrors();
			var error = mockPayloadSender.FirstError;

			error.Should().NotBeNull();
			var capturedException = error.Exception;

			capturedException.Should().NotBeNull();
			capturedException.Message.Should().Be("Outer exception");
			capturedException.Cause.Should().NotBeNull().And.HaveCount(2);

			var firstCause = capturedException.Cause[0];
			firstCause.Message.Should().Be("Inner exception");
			firstCause.Type.Should().Be("System.Exception");

			var secondCause = capturedException.Cause[1];
			secondCause.Message.Should().Be("Inner inner exception");
			secondCause.Type.Should().Be("System.Exception");
		}

		[Fact]
		public void ErrorContextSanitizerFilterDoesNotThrowWhenTransactionNotSampled()
		{
			var waitHandle = new ManualResetEventSlim();
			using var localServer = LocalServer.Create(context =>
			{
				context.Response.StatusCode = 200;
				waitHandle.Set();
			});

			var config = new MockConfiguration(transactionSampleRate: "0", serverUrl: localServer.Uri, flushInterval: "0");
			var logger = new InMemoryBlockingLogger(LogLevel.Warning);
			var payloadSender = new PayloadSenderV2(logger, config,
				Service.GetDefaultService(config, logger), new Api.System(),MockApmServerInfo.Version710);

			using var agent = new ApmAgent(new AgentComponents(payloadSender: payloadSender, configurationReader: config));
			agent.Tracer.CaptureTransaction("Test", "Test", t =>
			{
				t.CaptureException(new Exception("boom!"));
			});

			waitHandle.Wait();

			logger.Lines.Should().NotContain(line => line.Contains("Exception during execution of the filter on transaction"));
		}
	}
}
