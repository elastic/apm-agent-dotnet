// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.Libraries.Newtonsoft.Json;
using Elastic.Apm.Libraries.Newtonsoft.Json.Linq;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;
using Elastic.Apm.Report;
using Elastic.Apm.Report.Serialization;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using Timer = System.Timers.Timer;

namespace Elastic.Apm.Tests
{
	public class FilterTests
	{
		private readonly IApmLogger _logger;

		public FilterTests(ITestOutputHelper testOutputHelper)
		{
			var adapter = new XunitOutputToLineWriterAdaptor(testOutputHelper, nameof(FilterTests));
			_logger = new LineWriterToLoggerAdaptor(new SplittingLineWriter(adapter), LogLevel.Trace);
		}

		/// <summary>
		/// Registers 2 transaction filters - one changes the transaction name, one changes the transaction type.
		/// Makes sure changes are applied to the serialized transaction.
		/// </summary>
		[Fact]
		public void RenameTransactionNameAndTypeIn2Filters() =>
			RegisterFilterRunCodeAndAssert(
				payloadSender =>
				{
					payloadSender.TransactionFilters.Add(t =>
					{
						t.Name = "NewTransactionName";
						return t;
					});

					payloadSender.TransactionFilters.Add(t =>
					{
						t.Type = "NewTransactionType";
						return t;
					});
				},
				agent => { agent.Tracer.CaptureTransaction("Test123", "TestTransaction", t => { Thread.Sleep(10); }); },
				(transactions, spans, errors) =>
				{
					transactions.First().Name.Should().Be("NewTransactionName");
					transactions.First().Type.Should().Be("NewTransactionType");
				});

		/// <summary>
		/// Registers 3 handlers - one of them throws, 2 other just change the transactions.
		/// Makes sure that the changes from the 2 filters that don't throw get applied and also makes sure the transaction is
		/// serialized with the changes.
		/// </summary>
		[Fact]
		public void MultipleHandlerOneOfThemThrowingOnTransactions() =>
			RegisterFilterRunCodeAndAssert(
				payloadSender =>
				{
					// Rename transaction name
					payloadSender.TransactionFilters.Add(t =>
					{
						t.Name = "NewTransactionName";
						return t;
					});

					// Throw an exception
					payloadSender.TransactionFilters.Add(t => throw new Exception("This is a test exception"));

					// Rename transaction type
					payloadSender.TransactionFilters.Add(t =>
					{
						t.Type = "NewTransactionType";
						return t;
					});
				},
				agent => { agent.Tracer.CaptureTransaction("Test123", "TestTransaction", t => { Thread.Sleep(10); }); },
				(transactions, spans, errors) =>
				{
					transactions.First().Name.Should().Be("NewTransactionName");
					transactions.First().Type.Should().Be("NewTransactionType");
				});

		/// <summary>
		/// Registers 3 span-filters, 1 throws, 1 renames the span, 1 changes the span type.
		/// Makes sure that changes from the 2 filters that don't throw get applied and the span is serialized accordingly.
		/// </summary>
		[Fact]
		public void FilterSpanWith3Filters()
			=>
				RegisterFilterRunCodeAndAssert(
					payloadSender =>
					{
						// Rename span name
						payloadSender.SpanFilters.Add(span =>
						{
							span.Name = "NewSpanName";
							return span;
						});

						// Throw an exception
						payloadSender.SpanFilters.Add(span => throw new Exception("This is a test exception"));

						// Rename span type
						payloadSender.SpanFilters.Add(span =>
						{
							span.Type = "NewSpanType";
							return span;
						});
					},
					agent =>
					{
						agent.Tracer.CaptureTransaction("Test123", "TestTransaction",
							t => { t.CaptureSpan("SampleSpan", "TestSpan", () => Thread.Sleep(10)); });
					},
					(transactions, spans, errors) =>
					{
						spans.First().Name.Should().Be("NewSpanName");
						spans.First().Type.Should().Be("NewSpanType");
					});

		/// <summary>
		/// Registers a span-filter that returns false for specific span names and sends a span with that specific name.
		/// Makes sure the span is not sent and serialized.
		/// </summary>
		[Fact]
		public void DropSpanWithSpecificName()
			=> RegisterFilterRunCodeAndAssert(
				payloadSender =>
				{
					// Rename span name
					payloadSender.SpanFilters.Add(span =>
					{
						if (span.Name == "SpanToDrop") return null;

						return span;
					});
				},
				agent =>
				{
					agent.Tracer.CaptureTransaction("Test123", "TestTransaction",
						t => { t.CaptureSpan("SpanToDrop", "TestSpan", () => Thread.Sleep(10)); });
				},
				(transactions, spans, errors) => { spans.Should().BeEmpty(); });

		private void RegisterFilterRunCodeAndAssert(Action<PayloadSenderV2> registerFilters, Action<ApmAgent> executeCodeThatGeneratesData,
			Action<List<Transaction>, List<Span>, List<Error>> assert
		)
		{
			var mockConfig = new MockConfigSnapshot(maxBatchEventCount: "1", flushInterval: "0");

			// The handler is executed in PayloadSenderV2, in a separate thread where all exceptions are handled.
			// No exception bubbles up from PayloadSenderV2 (reason is that in prod if the HTTP Request fails, agent handles it and doesn't let it bubble up)
			// To hold up the test we use TaskCompletionSource and control its result within the PayloadSender's thread
			var taskCompletionSource = new TaskCompletionSource<object>();

			// If from some reason the handler is not executed a timer is here defined that
			// sets the task to cancelled, so the test is guaranteed to end
			var timer = new Timer(20000);
			timer.Elapsed += (o, args) => { taskCompletionSource.SetCanceled(); };
			timer.Start();


			var handler = RegisterHandlerAndAssert(mockConfig, (transactions, spans, errors) =>
			{
				try
				{
					assert(transactions, spans, errors);
					taskCompletionSource.SetResult(null);
				}
				catch (Exception e)
				{
					taskCompletionSource.SetException(e);
					throw;
				}
			});

			var payloadSender = new PayloadSenderV2(_logger, mockConfig,
				Service.GetDefaultService(mockConfig, _logger), new Api.System(),MockApmServerInfo.Version710, handler);

			registerFilters(payloadSender);

			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, logger: _logger));
			executeCodeThatGeneratesData(agent);
			// hold the test run until the event is processed within PayloadSender's thread - also makes sure that PayloadSender is not disposed
			var _ = taskCompletionSource.Task.Result;
		}

		private static MockHttpMessageHandler RegisterHandlerAndAssert(IConfigurationReader config, Action<List<Transaction>, List<Span>, List<Error>> assert) =>
			new MockHttpMessageHandler((r, c) =>
			{
				using var reader = new StreamReader(r.Content.ReadAsStreamAsync().Result);
				var serializer = new PayloadItemSerializer();
				var content = reader.ReadToEnd();
				var payloadStrings = content.Split(new [] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

				var transactions = new List<Transaction>();
				var spans = new List<Span>();
				var errors = new List<Error>();

				foreach (var receivedEvent in payloadStrings)
				{
					var jObject = JObject.Parse(receivedEvent);
					var property = jObject.Properties().First();

					switch (property.Name)
					{
						case "transaction":
							var transaction = serializer.Deserialize<Transaction>(property.Value.ToString(Formatting.None));
							transactions.Add(transaction);
							break;
						case "span":
							var span = serializer.Deserialize<Span>(property.Value.ToString(Formatting.None));
							spans.Add(span);
							break;
						case "error":
							var error = serializer.Deserialize<Error>(property.Value.ToString(Formatting.None));
							errors.Add(error);
							break;
					}
				}

				assert(transactions, spans, errors);
				return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
			});
	}
}
