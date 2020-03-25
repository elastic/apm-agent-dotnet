using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Model;
using Elastic.Apm.Report;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace Elastic.Apm.Tests
{
	public class FilterTests
	{
		[Fact]
		public void RenameTransactionNameAndTypeIn2Filters() =>
			RegisterFilterRunCodeAndAssert(
				payloadSender =>
				{
					payloadSender.TransactionFilters.Add(t =>
					{
						t.Name = "NewTransactionName";
						return true;
					});

					payloadSender.TransactionFilters.Add(t =>
					{
						t.Type = "NewTransactionType";
						return true;
					});
				},
				agent => { agent.Tracer.CaptureTransaction("Test123", "TestTransaction", t => { Thread.Sleep(10); }); },
				(transactions, spans, errors) =>
				{
					transactions.First().Name.Should().Be("NewTransactionName");
					transactions.First().Type.Should().Be("NewTransactionType");
				});

		[Fact]
		public void MultipleHandlerOneOfThemThrowingOnTransactions() =>
			RegisterFilterRunCodeAndAssert(
				payloadSender =>
				{
					// Rename transaction name
					payloadSender.TransactionFilters.Add(t =>
					{
						t.Name = "NewTransactionName";
						return true;
					});

					// Throw an exception
					payloadSender.TransactionFilters.Add(t => throw new Exception("This is a test exception"));

					// Rename transaction type
					payloadSender.TransactionFilters.Add(t =>
					{
						t.Type = "NewTransactionType";
						return true;
					});
				},
				agent => { agent.Tracer.CaptureTransaction("Test123", "TestTransaction", t => { Thread.Sleep(10); }); },
				(transactions, spans, errors) =>
				{
					transactions.First().Name.Should().Be("NewTransactionName");
					transactions.First().Type.Should().Be("NewTransactionType");
				});

		private void RegisterFilterRunCodeAndAssert(Action<PayloadSenderV2> registerFilters, Action<ApmAgent> executeCodeThatGeneratesData,
			Action<List<Transaction>, List<Span>, List<Error>> assert
		)
		{
			var handlerCompleted = false;
			var mockConfig = new MockConfigSnapshot(maxBatchEventCount: "1", flushInterval: "0");

			// The handler is executed in PayloadSenderV2, where all exceptions are handled - no exception bubbles up from PayloadSenderV2 (reason is that in prod if the HTTP Request fails, agent handles it and doesn't let it bubble up
			// From this reason, failing asserts are also handled - a failing assert would not make the test fail, because the exception thrown be XUnit is caught by PayloadSenderV2
			// So we simply store the exceptions in the variable below and assert on that after the code from the handler with the asserts were executed.
			Exception exception = null;

			var handler = RegisterHandlerAndAssert((transactions, spans, errors) =>
			{
				handlerCompleted = true;
				try
				{
					assert(transactions, spans, errors);
				}
				catch (Exception e)
				{
					exception = e;
					throw;
				}
			});

			var noopLogger = new NoopLogger();

			var payloadSender = new PayloadSenderV2(noopLogger, mockConfig,
				Service.GetDefaultService(mockConfig, noopLogger), new Api.System(), handler);

			registerFilters(payloadSender);

			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender))) executeCodeThatGeneratesData(agent);

			exception.Should().BeNull();
			handlerCompleted.Should().BeTrue();
		}

		private static MockHttpMessageHandler RegisterHandlerAndAssert(Action<List<Transaction>, List<Span>, List<Error>> assert) =>
			new MockHttpMessageHandler((r, c) =>
			{
				var content = r.Content.ReadAsStringAsync().Result;
				var payloadStrings = content.Split('\n');

				var transactions = new List<Transaction>();
				var spans = new List<Span>();
				var errors = new List<Error>();

				foreach (var receivedEvent in payloadStrings)
				{
					switch (receivedEvent)
					{
						case { } s when s.StartsWith("{\"transaction\":"):
							var str = receivedEvent.Substring(16);
							str = str.Remove(str.Length - 2);
							var transaction = JsonConvert.DeserializeObject<Transaction>(str);
							transactions.Add(transaction);
							break;
						case { } s when s.StartsWith("{\"span\":"):
							str = receivedEvent.Substring(9);
							str = str.Remove(str.Length - 2);
							var span = JsonConvert.DeserializeObject<Span>(str);
							spans.Add(span);
							break;
						case { } s when s.StartsWith("{\"error\":"):
							str = receivedEvent.Substring(10);
							str = str.Remove(str.Length - 2);
							var error = JsonConvert.DeserializeObject<Error>(str);
							errors.Add(error);
							break;
					}
				}
				assert(transactions, spans, errors);
				return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
			});
	}
}
