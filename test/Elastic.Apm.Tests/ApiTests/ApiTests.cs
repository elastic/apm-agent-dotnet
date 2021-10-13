// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.HelpersTests;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

// ReSharper disable AccessToDisposedClosure

namespace Elastic.Apm.Tests.ApiTests
{
	/// <summary>
	/// Tests the API for manual instrumentation.
	/// Only tests scenarios with manually calling StartTransaction, StartSpan, and End.
	/// The convenient API is covered by <see cref="ConvenientApiSpanTests" /> and <see cref="ConvenientApiTransactionTests" />
	/// </summary>
	public class ApiTests
	{
		private const string CustomTransactionTypeForTests = "custom transaction type for tests";
		private const string TestSpan1 = "TestSpan1";
		private const string TestSpan2 = "TestSpan1";
		private const string TestTransaction = "TestTransaction";
		private const string UnitTest = "UnitTest";

		/// <summary>
		/// Starts and ends a transaction with the public API
		/// and makes sure that the transaction is captured by the agent
		/// </summary>
		[Fact]
		public void StartEndTransaction()
		{
			const string transactionName = TestTransaction;
			const string transactionType = UnitTest;
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
			{
				var transaction = agent.Tracer.StartTransaction(transactionName, transactionType);

				Thread.Sleep(5); //Make sure we have duration > 0

				transaction.End();

				payloadSender.WaitForTransactions();
				payloadSender.Transactions.Should().ContainSingle();

				var capturedTransaction = payloadSender.Transactions[0];

				capturedTransaction.Name.Should().Be(transactionName);
				capturedTransaction.Type.Should().Be(transactionType);
				capturedTransaction.Duration.Should().BeGreaterOrEqualTo(5);
				capturedTransaction.Id.Should().NotBeEmpty();

				agent.Service.Should().NotBeNull();
			}
		}

		/// <summary>
		/// Starts a transaction, but does not call the End() method.
		/// Makes sure that the agent does not report the transaction.
		/// </summary>
		[Fact]
		public void StartNoEndTransaction()
		{
			const string transactionName = TestTransaction;
			const string transactionType = UnitTest;
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
			{
				var unused = agent.Tracer.StartTransaction(transactionName, transactionType);
			}

			payloadSender.SignalEndTransactions();
			payloadSender.WaitForTransactions();
			payloadSender.Transactions.Should().BeEmpty();
		}

		/// <summary>
		/// Starts a transaction, sets its result to 'success' and
		/// makes sure the result is captured by the agent.
		/// </summary>
		[Fact]
		public void TransactionResultTest()
		{
			const string transactionName = TestTransaction;
			const string transactionType = UnitTest;
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
			{
				var transaction = agent.Tracer.StartTransaction(transactionName, transactionType);

				const string result = "success";
				transaction.Result = result;
				transaction.End();

				payloadSender.WaitForTransactions();
				payloadSender.Transactions[0].Result.Should().Be(result);
			}
		}

		[Fact]
		public void GetCurrentTransaction()
		{
			using (var agent = new ApmAgent(new TestAgentComponents()))
			{
				agent.Tracer.CurrentTransaction.Should().BeNull();

				agent.Tracer.CaptureTransaction("dummy_transaction_name", "dummy_transaction_type", transaction1 =>
				{
					transaction1.Should().NotBeNull();
					agent.Tracer.CurrentTransaction.Should().Be(transaction1);
				});

				agent.Tracer.CurrentTransaction.Should().BeNull();

				var transaction2 = agent.Tracer.StartTransaction("dummy_transaction_name", "dummy_transaction_type");
				agent.Tracer.CurrentTransaction.Should().NotBeNull();
				agent.Tracer.CurrentTransaction.Should().Be(transaction2);
				transaction2.End();

				agent.Tracer.CurrentTransaction.Should().BeNull();
			}
		}

		[Fact]
		public void GetCurrentSpan()
		{
			using (var agent = new ApmAgent(new TestAgentComponents()))
			{
				agent.Tracer.CaptureTransaction("dummy_transaction_name", "dummy_transaction_type", transaction =>
				{
					agent.Tracer.CurrentSpan.Should().BeNull();

					transaction.CaptureSpan("dummy_span_name", "dummy_span_type", span1 =>
					{
						span1.Should().NotBeNull();
						agent.Tracer.CurrentSpan.Should().Be(span1);
					});

					var span2 = transaction.StartSpan("dummy_span_name", "dummy_span_type");
					agent.Tracer.CurrentSpan.Should().NotBeNull();
					agent.Tracer.CurrentSpan.Should().Be(span2);
					span2.End();

					agent.Tracer.CurrentSpan.Should().BeNull();
				});
			}
		}

		[Fact]
		public void GetCurrentSpanNested()
		{
			using (var agent = new ApmAgent(new TestAgentComponents()))
			{
				agent.Tracer.CaptureTransaction("dummy_transaction_name", "dummy_transaction_type", transaction =>
				{
					agent.Tracer.CurrentSpan.Should().BeNull();

					transaction.CaptureSpan("dummy_span_name", "dummy_span_type", span1 =>
					{
						span1.Should().NotBeNull();
						agent.Tracer.CurrentSpan.Should().Be(span1);

						span1.CaptureSpan("dummy_nested_span_name", "dummy_nested_span_type", span11 =>
						{
							span11.Should().NotBeNull();
							agent.Tracer.CurrentSpan.Should().Be(span11);
						});

						agent.Tracer.CurrentSpan.Should().Be(span1);
					});

					var span2 = transaction.StartSpan("dummy_span_name", "dummy_span_type");
					agent.Tracer.CurrentSpan.Should().NotBeNull();
					agent.Tracer.CurrentSpan.Should().Be(span2);

					var span21 = span2.StartSpan("dummy_span_name", "dummy_span_type");
					agent.Tracer.CurrentSpan.Should().NotBeNull();
					agent.Tracer.CurrentSpan.Should().Be(span21);
					span21.End();

					agent.Tracer.CurrentSpan.Should().Be(span2);
					span2.End();

					agent.Tracer.CurrentSpan.Should().BeNull();
				});
			}
		}

		/// <summary>
		/// Starts a transaction in a Task, does some work in a subtask, and after that it calls ElasticApm.CurrentTransaction.
		/// Makes sure the current transaction is not null - we assert on multiple points
		/// </summary>
		[Fact]
		public async Task GetCurrentTransactionAsyncContext()
		{
			const string transactionName = TestTransaction;
			using (var agent = new ApmAgent(new TestAgentComponents()))
			{
				var transaction = agent.Tracer.StartTransaction(transactionName, ApiConstants.TypeRequest); //Start transaction on the current task
				transaction.Should().NotBeNull();
				await DoAsyncWork(agent); //Do work in subtask

				var currentTransaction = agent.Tracer.CurrentTransaction; //Get transaction in the current task

				currentTransaction.Should().Be(transaction);
				currentTransaction.Name.Should().Be(transactionName);
				currentTransaction.Type.Should().Be(ApiConstants.TypeRequest);

				transaction.End();
			}

			async Task DoAsyncWork(ApmAgent agent)
			{
				//Make sure we have a transaction in the subtask before the async work
				agent.Tracer.CurrentTransaction.Should().NotBeNull();
				agent.Tracer.CurrentTransaction.Name.Should().Be(transactionName);
				agent.Tracer.CurrentTransaction.Type.Should().Be(ApiConstants.TypeRequest);

				await Task.Delay(50);

				//and after the async work
				agent.Tracer.CurrentTransaction.Should().NotBeNull();
				agent.Tracer.CurrentTransaction.Name.Should().Be(transactionName);
				agent.Tracer.CurrentTransaction.Type.Should().Be(ApiConstants.TypeRequest);
			}
		}

		[Fact]
		public async Task GetCurrentSpanAsyncContext()
		{
			const string transactionName = TestTransaction;
			const string spanName = "test_span_name";
			using (var agent = new ApmAgent(new TestAgentComponents()))
			{
				var transaction = agent.Tracer.StartTransaction(transactionName, ApiConstants.TypeRequest); //Start transaction on the current task
				var span = transaction.StartSpan(spanName, ApiConstants.TypeExternal); //Start span on the current task
				await DoAsyncWork(agent); //Do work in subtask

				var currentSpan = agent.Tracer.CurrentSpan; //Get span for the current task

				currentSpan.Should().Be(span);
				currentSpan.Name.Should().Be(spanName);
				currentSpan.Type.Should().Be(ApiConstants.TypeExternal);

				span.End();
				transaction.End();
			}

			async Task DoAsyncWork(ApmAgent agent)
			{
				//Make sure we have a span in the subtask before the async work
				agent.Tracer.CurrentSpan.Should().NotBeNull();
				agent.Tracer.CurrentSpan.Name.Should().Be(spanName);
				agent.Tracer.CurrentSpan.Type.Should().Be(ApiConstants.TypeExternal);

				await Task.Delay(50);

				//and after the async work
				agent.Tracer.CurrentSpan.Should().NotBeNull();
				agent.Tracer.CurrentSpan.Name.Should().Be(spanName);
				agent.Tracer.CurrentSpan.Type.Should().Be(ApiConstants.TypeExternal);
			}
		}

		/// <summary>
		/// Starts a transaction and then starts a span, ends both of them probably.
		/// Makes sure that the transaction and the span are captured.
		/// </summary>
		[Fact]
		public void TransactionWithSpan()
		{
			const string transactionName = TestTransaction;
			const string transactionType = UnitTest;
			const string spanName = "TestSpan";
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
			{
				var transaction = agent.Tracer.StartTransaction(transactionName, transactionType);

				var span = transaction.StartSpan(spanName, ApiConstants.TypeExternal);

				Thread.Sleep(5); //Make sure we have duration > 0

				span.End();
				transaction.End();

				payloadSender.WaitForTransactions();
				payloadSender.Transactions.Should().NotBeEmpty();

				payloadSender.WaitForSpans();
				payloadSender.SpansOnFirstTransaction.Should().NotBeEmpty();

				payloadSender.SpansOnFirstTransaction[0].Name.Should().Be(spanName);
				payloadSender.SpansOnFirstTransaction[0].Duration.Should().BeGreaterOrEqualTo(5);

				agent.Service.Should().NotBeNull();
			}
		}

		/// <summary>
		/// Starts a transaction and then calls transaction.StartSpan, but doesn't call Span.End().
		/// Makes sure that the transaction is recorded, but the span isn't.
		/// </summary>
		[Fact]
		public void TransactionWithSpanWithoutEnd()
		{
			const string transactionName = TestTransaction;
			const string transactionType = UnitTest;
			const string spanName = "TestSpan";
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
			{
				var transaction = agent.Tracer.StartTransaction(transactionName, transactionType);

				var unused = transaction.StartSpan(spanName, ApiConstants.TypeExternal);

				Thread.Sleep(5); //Make sure we have duration > 0

				transaction.End(); //Ends transaction, but doesn't end span.

				payloadSender.WaitForTransactions();
				payloadSender.Transactions.Should().NotBeEmpty();

				payloadSender.SignalEndSpans();
				payloadSender.WaitForSpans();
				payloadSender.SpansOnFirstTransaction.Should().BeEmpty();

				agent.Service.Should().NotBeNull();
			}
		}

		/// <summary>
		/// Starts a transaction and a span with subtype and action and ends them properly.
		/// Makes sure the subtype and the action are recorded
		/// </summary>
		[Fact]
		public void TransactionWithSpanWithSubTypeAndAction()
		{
			const string transactionName = TestTransaction;
			const string transactionType = UnitTest;
			const string spanName = "TestSpan";
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
			{
				var transaction = agent.Tracer.StartTransaction(transactionName, transactionType);
				var span = transaction.StartSpan(spanName, ApiConstants.TypeDb, ApiConstants.SubtypeMssql, ApiConstants.ActionQuery);
				span.End();
				transaction.End();

				payloadSender.WaitForTransactions();
				payloadSender.Transactions.Should().NotBeEmpty();
				payloadSender.WaitForSpans();
				payloadSender.SpansOnFirstTransaction.Should().NotBeEmpty();

				payloadSender.SpansOnFirstTransaction[0].Type.Should().Be(ApiConstants.TypeDb);
				payloadSender.SpansOnFirstTransaction[0].Subtype.Should().Be(ApiConstants.SubtypeMssql);
				payloadSender.SpansOnFirstTransaction[0].Action.Should().Be(ApiConstants.ActionQuery);

				agent.Service.Should().NotBeNull();
			}
		}

		/// <summary>
		/// Starts and ends a transaction properly and captures an error in between.
		/// Makes sure the error is captured.
		/// </summary>
		[Fact]
		public void ErrorOnTransaction() => ErrorOnTransactionCommon();

		/// <summary>
		/// Same as <see cref="ErrorOnTransaction" />, but this time a Culprit is also provided
		/// </summary>
		[Fact]
		public void ErrorOnTransactionWithCulprit() => ErrorOnTransactionCommon("TestCulprit");

		/// <summary>
		/// Shared between ErrorOnTransaction and ErrorOnTransactionWithCulprit
		/// </summary>
		/// <param name="culprit">Culprit.</param>
		private static void ErrorOnTransactionCommon(string culprit = null)
		{
			const string transactionName = TestTransaction;
			const string transactionType = UnitTest;
			const string exceptionMessage = "Foo!";
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
			{
				var transaction = agent.Tracer.StartTransaction(transactionName, transactionType);

				Thread.Sleep(5); //Make sure we have duration > 0
				try
				{
					throw new InvalidOperationException(exceptionMessage);
				}
				catch (Exception e)
				{
					if (string.IsNullOrEmpty(culprit))
						transaction.CaptureException(e);
					else
						transaction.CaptureException(e, culprit);
				}

				transaction.End();
			}

			payloadSender.WaitForTransactions();
			payloadSender.Transactions.Should().ContainSingle();
			payloadSender.WaitForErrors();
			payloadSender.Errors.Should().ContainSingle();
			payloadSender.FirstError.Exception.Message.Should().Be(exceptionMessage);
			payloadSender.FirstError.Exception.Message.Should().Be(exceptionMessage);

			payloadSender.FirstError.Culprit.Should().Be(!string.IsNullOrEmpty(culprit) ? culprit : "Elastic.Apm.Tests.ApiTests.ApiTests");
		}

		/// <summary>
		/// Starts a transaction with a span, ends the transaction and the span properly and call CaptureException() on the span.
		/// Makes sure the exception is captured.
		/// </summary>
		[Fact]
		public void ErrorOnSpan()
		{
			const string transactionName = TestTransaction;
			const string transactionType = UnitTest;
			const string spanName = "TestSpan";
			const string exceptionMessage = "Foo!";
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
			{
				var transaction = agent.Tracer.StartTransaction(transactionName, transactionType);

				var span = transaction.StartSpan(spanName, ApiConstants.TypeExternal);

				Thread.Sleep(5); //Make sure we have duration > 0

				try
				{
					throw new InvalidOperationException(exceptionMessage);
				}
				catch (Exception e)
				{
					span.CaptureException(e);
				}

				span.End();
				transaction.End();
			}

			payloadSender.WaitForTransactions();
			payloadSender.Transactions.Should().ContainSingle();
			payloadSender.WaitForErrors();
			payloadSender.Errors.Should().ContainSingle();
			payloadSender.FirstError.Exception.Message.Should().Be(exceptionMessage);
		}

		/// <summary>
		/// Creates 1 transaction and 1 span with 2 labels on both of them.
		/// Makes sure that the labels are captured.
		/// </summary>
		[Fact]
		public void LabelsOnTransactionAndSpan()
		{
			const string transactionName = TestTransaction;
			const string transactionType = UnitTest;
			const string spanName = "TestSpan";
			const string exceptionMessage = "Foo!";
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
			{
				var transaction = agent.Tracer.StartTransaction(transactionName, transactionType);
				transaction.SetLabel("fooTransaction1", "barTransaction1");
				transaction.SetLabel("fooTransaction2", "barTransaction2");

				var span = transaction.StartSpan(spanName, ApiConstants.TypeExternal);
				span.SetLabel("fooSpan1", "barSpan1");
				span.SetLabel("fooSpan2", "barSpan2");

				Thread.Sleep(5); //Make sure we have duration > 0

				try
				{
					throw new InvalidOperationException(exceptionMessage);
				}
				catch (Exception e)
				{
					span.CaptureException(e);
				}

				span.End();
				transaction.End();
			}

			payloadSender.WaitForTransactions();
			payloadSender.Transactions.Should().ContainSingle();
			payloadSender.WaitForErrors();
			payloadSender.Errors.Should().ContainSingle();
			payloadSender.FirstError.Exception.Message.Should().Be(exceptionMessage);

			payloadSender.FirstTransaction.Context.InternalLabels.Value.MergedDictionary["fooTransaction1"].Value.Should().Be("barTransaction1");
			payloadSender.FirstTransaction.Context.InternalLabels.Value.MergedDictionary["fooTransaction2"].Value.Should().Be("barTransaction2");

			payloadSender.WaitForSpans();
			payloadSender.SpansOnFirstTransaction[0].Context.InternalLabels.Value.MergedDictionary["fooSpan1"].Value.Should().Be("barSpan1");
			payloadSender.SpansOnFirstTransaction[0].Context.InternalLabels.Value.MergedDictionary["fooSpan2"].Value.Should().Be("barSpan2");
		}

		/// <summary>
		/// Creates a transaction and then a span inside this transaction and then a 2. span within the 1. span (aka sub span)
		/// Makes sure the relationship between the transaction and the spans are captured correctly.
		/// </summary>
		[Fact]
		public void CreateSubSpan()
		{
			var payloadSender = new MockPayloadSender();
			StartTransactionAndSpanWithSubSpan(payloadSender, s => { });
		}

		/// <summary>
		/// Helper method. It creates a transaction, and a span on it, and then a sub span on that span.
		/// Then it runs <paramref name="action" /> by passing the subspan as parameter
		/// </summary>
		/// <param name="payloadSender"></param>
		/// <param name="action"></param>
		private void StartTransactionAndSpanWithSubSpan(MockPayloadSender payloadSender, Action<ISpan> action)
		{
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
			{
				var transaction = agent.Tracer.StartTransaction(TestTransaction, UnitTest);
				Thread.Sleep(5);
				var span1 = transaction.StartSpan(TestSpan1, ApiConstants.TypeExternal);
				Thread.Sleep(5);
				var span2 = span1.StartSpan(TestSpan2, ApiConstants.TypeExternal);
				Thread.Sleep(5);

				action(span2);

				span2.End();
				span1.End();
				transaction.End();
			}

			var orderedSpans = payloadSender.Spans.OrderBy(n => n.Timestamp).ToList();

			var firstSpan = orderedSpans.First();
			var innerSpan = orderedSpans.Last();

			firstSpan.ParentId.Should().Be(payloadSender.FirstTransaction.Id);
			innerSpan.ParentId.Should().Be(firstSpan.Id);

			firstSpan.TransactionId.Should().Be(payloadSender.FirstTransaction.Id);
			innerSpan.TransactionId.Should().Be(payloadSender.FirstTransaction.Id);
		}

		/// <summary>
		/// Creates a transaction, then a span and then a sub span, which captures an exception.
		/// Makes sure that the exception is captured by the agent.
		/// </summary>
		[Fact]
		public void SubSpanWithError()
		{
			var payloadSender = new MockPayloadSender();
			StartTransactionAndSpanWithSubSpan(payloadSender, span2 =>
			{
				try
				{
					throw new TestException("test exception");
				}
				catch (Exception e)
				{
					span2.CaptureException(e);
				}
			});

			payloadSender.Errors.Should().NotBeEmpty();
			payloadSender.FirstError.Exception.Type.Should().Be(typeof(TestException).ToString());
			payloadSender.FirstError.Exception.Message.Should().Be("test exception");
		}

		/// <summary>
		/// Creates a transaction, then a span and then a sub span, which captures a label.
		/// Makes sure that the label is captured by the agent.
		/// </summary>
		[Fact]
		public void SubSpanWithLabels()
		{
			var payloadSender = new MockPayloadSender();
			StartTransactionAndSpanWithSubSpan(payloadSender, span2 => { span2.SetLabel("foo", 42); });

			payloadSender.FirstSpan.Context.InternalLabels.Value.MergedDictionary.Should().NotBeEmpty();
			payloadSender.FirstSpan.Context.InternalLabels.Value.MergedDictionary.Should().ContainKey("foo");
			payloadSender.FirstSpan.Context.InternalLabels.Value.MergedDictionary["foo"].Value.Should().Be(42);
		}

		/// <summary>
		/// Creates a transaction, then a span then calls <see cref="ITransaction.End()" /> and <see cref="ISpan.End()" /> twice.
		/// Makes sure that transaction and span sent on the first respective call to End() and the second call is no-op.
		/// </summary>
		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void CallingEndMoreThanOnceIsNoop(bool isSampled)
		{
			var payloadSender = new MockPayloadSender();
			var expectedSpansCount = isSampled ? 1 : 0;

			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender,
				configuration: new MockConfiguration(transactionSampleRate: isSampled ? "1" : "0"))))
			{
				var transaction = agent.Tracer.StartTransaction(TestTransaction, UnitTest);
				var span = transaction.StartSpan(TestSpan1, ApiConstants.TypeExternal);

				payloadSender.Spans.Should().HaveCount(0);
				span.End();

				payloadSender.SignalEndSpans();
				payloadSender.WaitForSpans();
				payloadSender.Spans.Should().HaveCount(expectedSpansCount);
				if (isSampled) payloadSender.FirstSpan.Name.Should().Be(TestSpan1);
				span.End();

				payloadSender.SignalEndSpans();
				payloadSender.WaitForSpans();
				payloadSender.Spans.Should().HaveCount(expectedSpansCount);

				payloadSender.Transactions.Should().HaveCount(0);
				transaction.End();
				payloadSender.WaitForTransactions();
				payloadSender.Transactions.Should().HaveCount(1);
				payloadSender.FirstTransaction.Name.Should().Be(TestTransaction);
				transaction.End();
				payloadSender.SignalEndTransactions();
				payloadSender.WaitForTransactions();
				payloadSender.Transactions.Should().HaveCount(1);
			}
		}

		/// <summary>
		/// Creates a transaction, then a span then set duration for both, then  calls End() for both.
		/// Makes sure that call to End() does not change already set duration.
		/// </summary>
		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void CallToEndDoesNotChangeAlreadySetDuration(bool isSampled)
		{
			var payloadSender = new MockPayloadSender();
			var expectedSpansCount = isSampled ? 1 : 0;

			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender,
				configuration: new MockConfiguration(transactionSampleRate: isSampled ? "1" : "0"))))
			{
				var transaction = agent.Tracer.StartTransaction(TestTransaction, UnitTest);
				var span = transaction.StartSpan(TestSpan1, ApiConstants.TypeExternal);

				payloadSender.Spans.Should().HaveCount(0);
				span.Duration = 123456.789;
				span.End();

				payloadSender.SignalEndSpans();
				payloadSender.WaitForSpans();
				payloadSender.Spans.Should().HaveCount(expectedSpansCount);
				if (isSampled) payloadSender.FirstSpan.Duration.Should().Be(123456.789);

				payloadSender.Transactions.Should().HaveCount(0);
				transaction.Duration = 987654.321;
				transaction.End();
				payloadSender.SignalEndTransactions();
				payloadSender.WaitForTransactions();
				payloadSender.Transactions.Should().HaveCount(1);
				payloadSender.FirstTransaction.Duration.Should().Be(987654.321);
			}
		}

		// ReSharper disable once MemberCanBePrivate.Global
		public static IEnumerable<object[]> ErrorShouldContainTransactionDataParamVariants()
		{
			var boolValues = new[] { false, true };
			foreach (var isSampled in boolValues)
			{
				foreach (var captureOnSpan in boolValues)
				{
					foreach (var captureAsError in boolValues)
						yield return new object[] { isSampled, captureOnSpan, captureAsError };
				}
			}
		}

		/// <summary>
		/// Creates a sampled or non-sampled transaction (depending on isSampled argument),
		/// then a child span (depending on isOnSpan argument)
		/// which captures an error or exception (depending on isError argument).
		/// Makes sure that the sent error data contains transaction data.
		/// </summary>
		[Theory]
		[MemberData(nameof(ErrorShouldContainTransactionDataParamVariants))]
		public void ErrorShouldContainTransactionData(bool isSampled, bool captureOnSpan, bool captureAsError)
		{
			var payloadSender = new MockPayloadSender();
			var expectedErrorContext = new Context();
			expectedErrorContext.InternalLabels.Value.InnerDictionary["one"] = 1;
			expectedErrorContext.InternalLabels.Value.InnerDictionary["twenty two"] = "22";
			expectedErrorContext.InternalLabels.Value.InnerDictionary["true"] = true;

			ITransaction capturedTransaction = null;
			IExecutionSegment errorCapturingExecutionSegment = null;
			var mockConfig = new MockConfiguration(transactionSampleRate: isSampled ? "1" : "0");
			using (var agent = new ApmAgent(new TestAgentComponents(configuration: mockConfig, payloadSender: payloadSender)))
			{
				agent.Tracer.CaptureTransaction(TestTransaction, CustomTransactionTypeForTests, transaction =>
				{
					capturedTransaction = transaction;
					foreach (var item in expectedErrorContext.InternalLabels.Value.MergedDictionary)
						transaction.Context.InternalLabels.Value.MergedDictionary[item.Key] = item.Value;
					ISpan span = null;
					if (captureOnSpan)
					{
						span = transaction.StartSpan(TestSpan1, ApiConstants.TypeExternal);
						errorCapturingExecutionSegment = span;
					}
					else
						errorCapturingExecutionSegment = transaction;

					if (captureAsError)
						errorCapturingExecutionSegment.CaptureError("Test error message", "Test error culprit", new StackTrace(true).GetFrames());
					else
						errorCapturingExecutionSegment.CaptureException(new TestException("test exception"));

					// Immutable snapshot of the context should be captured instead of reference to a mutable object
					// transaction.Context.Labels["three hundred thirty three"] = "333";

					span?.End();
				});
			}

			payloadSender.WaitForErrors();
			payloadSender.Errors.Count.Should().Be(1);
			payloadSender.FirstError.Transaction.IsSampled.Should().Be(isSampled);
			payloadSender.FirstError.Transaction.Type.Should().Be(CustomTransactionTypeForTests);
			payloadSender.FirstError.TransactionId.Should().Be(capturedTransaction.Id);
			payloadSender.FirstError.TraceId.Should().Be(capturedTransaction.TraceId);
			payloadSender.FirstError.ParentId.Should()
				.Be(errorCapturingExecutionSegment.IsSampled ? errorCapturingExecutionSegment.Id : capturedTransaction.Id);

			payloadSender.FirstError.Transaction.IsSampled.Should().Be(isSampled);
			if (isSampled)
				payloadSender.FirstError.Context.Should().NotBeNull().And.BeEquivalentTo(expectedErrorContext);
			else
				payloadSender.FirstError.Context.Should().BeNull();
		}

		/// <summary>
		/// Makes sure Transaction.Custom is captured and it is not truncated.
		/// </summary>
		[Fact]
		public void CaptureCustom()
		{
			var payloadSender = new MockPayloadSender();
			var customValue = "b".Repeat(10_000);
			var customKey = "a";

			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
			{
				var transaction = agent.Tracer.StartTransaction(TestTransaction, CustomTransactionTypeForTests);
				transaction.Custom.Add(customKey, customValue);
				transaction.End();
			}

			payloadSender.WaitForTransactions();
			payloadSender.FirstTransaction.Should().NotBeNull();
			payloadSender.FirstTransaction.Custom[customKey].Should().Be(customValue);
		}

		[Fact]
		public void destination_properties_set_manually_have_precedence_over_automatically_deduced()
		{
			var url = new Uri("http://elastic.co");
			const string manualAddress = "manual.address";
			const int manualPort = 1234;
			var payloadSender = new MockPayloadSender();

			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
			{
				agent.Tracer.CaptureTransaction("test TX name", "test TX type", tx =>
				{
					tx.CaptureSpan("manually set destination address", "test_span_type", span =>
					{
						span.Context.Destination = new Destination { Address = manualAddress };
						span.Context.Http = new Http { Method = "PUT", Url = url.ToString() };
					});
					tx.CaptureSpan("manually set destination port", "test_span_type", span =>
					{
						span.Context.Destination = new Destination { Port = manualPort };
						span.Context.Http = new Http { Method = "PUT", Url = url.ToString() };
					});
					tx.CaptureSpan("manually set destination address to null", "test_span_type", span =>
					{
						span.Context.Destination = new Destination { Address = null };
						span.Context.Http = new Http { Method = "PUT", Url = url.ToString() };
					});
					tx.CaptureSpan("manually set destination port to null", "test_span_type", span =>
					{
						span.Context.Destination = new Destination { Port = null };
						span.Context.Http = new Http { Method = "PUT", Url = url.ToString() };
					});
				});
			}

			payloadSender.WaitForSpans(count: 4);
			var manualAddressSpan = payloadSender.Spans.Single(s => s.Name == "manually set destination address");
			manualAddressSpan.Context.Destination.Address.Should().Be(manualAddress);
			manualAddressSpan.Context.Destination.Port.Should().Be(url.Port);

			var manualPortSpan = payloadSender.Spans.Single(s => s.Name == "manually set destination port");
			manualPortSpan.Context.Destination.Address.Should().Be(url.Host);
			manualPortSpan.Context.Destination.Port.Should().Be(manualPort);

			var nullAddressSpan = payloadSender.Spans.Single(s => s.Name == "manually set destination address to null");
			nullAddressSpan.Context.Destination.Address.Should().BeNull();
			nullAddressSpan.Context.Destination.Port.Should().Be(url.Port);

			var nullPortSpan = payloadSender.Spans.Single(s => s.Name == "manually set destination port to null");
			nullPortSpan.Context.Destination.Address.Should().Be(url.Host);
			nullPortSpan.Context.Destination.Port.Should().BeNull();
		}

		[Fact]
		public void span_that_is_not_external_service_call_should_not_have_destination()
		{
			var payloadSender = new MockPayloadSender();

			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
			{
				agent.Tracer.CaptureTransaction("test TX name", "test TX type",
					tx => { tx.CaptureSpan("test span name", "test_span_subtype", () => { }); });
			}

			payloadSender.WaitForSpans();
			payloadSender.Spans.Single().Context.Destination.Should().BeNull();
		}

		[Fact]
		public void span_with_invalid_Context_Http_Url_should_not_have_destination()
		{
			var mockLogger = new TestLogger(LogLevel.Trace);
			var payloadSender = new MockPayloadSender();

			using (var agent = new ApmAgent(new TestAgentComponents(mockLogger, payloadSender: payloadSender)))
			{
				agent.Tracer.CaptureTransaction("test TX name", "test TX type",
					tx =>
					{
						tx.CaptureSpan("test span name", "test_span_type", span => { span.Context.Http = new Http { Method = "PUT", Url = "://" }; });
					});
			}

			payloadSender.WaitForSpans();
			payloadSender.Spans.Single().Context.Destination.Should().BeNull();
			mockLogger.Lines.Should()
				.Contain(line => line.Contains("destination")
					&& line.Contains("URL"));
		}

		/// <summary>
		/// Makes sure that <see cref="ITransaction.EnsureParentId" /> creates a new parent id, sets it to the transaction and
		/// returns it.
		/// </summary>
		[Fact]
		public void EnsureParentIdWithNoParentId()
		{
			var mockLogger = new TestLogger(LogLevel.Trace);
			var payloadSender = new MockPayloadSender();

			using var agent = new ApmAgent(new TestAgentComponents(mockLogger, payloadSender: payloadSender));
			agent.Tracer.CaptureTransaction("test TX name", "test TX type", tx =>
			{
				var newParentId = tx.EnsureParentId();
				newParentId.Should().NotBeNullOrWhiteSpace();
				newParentId.Should().Be(tx.ParentId);
			});
		}

		/// <summary>
		/// Makes sure that in case a ParentId already exists, the <see cref="ITransaction.EnsureParentId" /> returns it and does
		/// not change the
		/// existing ParentId.
		/// </summary>
		[Fact]
		public void EnsureParentIdWithExistingParentId()
		{
			var mockLogger = new TestLogger(LogLevel.Trace);
			var payloadSender = new MockPayloadSender();

			using var agent = new ApmAgent(new TestAgentComponents(mockLogger, payloadSender: payloadSender));
			agent.Tracer.CaptureTransaction("test TX name", "test TX type", tx =>
				{
					var newParentId = tx.EnsureParentId();
					newParentId.Should().NotBeNullOrWhiteSpace();
					newParentId.Should().Be(tx.ParentId);
					newParentId.Should().Be(DistributedTracingDataHelper.ValidParentId);
				},
				DistributedTracingDataHelper.BuildDistributedTracingData(DistributedTracingDataHelper.ValidTraceId,
					DistributedTracingDataHelper.ValidParentId, DistributedTracingDataHelper.ValidTraceFlags));
		}

		/// <summary>
		/// Creates 3 transactions and overwrites the service name and version of 2 of them.
		/// Makes sure that the service name and version is set for the 2 changed transaction.
		/// </summary>
		[Fact]
		public void CustomServiceTest()
		{
			var payloadSender = new MockPayloadSender();
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			var transaction1 = agent.Tracer.StartTransaction("Transaction1", "test");
			transaction1.SetService("Service1", "1.0-beta1");
			transaction1.End();

			var transaction2 = agent.Tracer.StartTransaction("Transaction2", "test");
			transaction2.SetService("Service2", "1.0-beta2");
			transaction2.End();

			var transaction3 = agent.Tracer.StartTransaction("Transaction3", "test");
			transaction3.End();

			payloadSender.WaitForTransactions();
			payloadSender.Transactions.Count.Should().Be(3);

			var recordedTransaction1 = payloadSender.Transactions.FirstOrDefault(t => t.Name == "Transaction1");
			recordedTransaction1.Should().NotBeNull();
			recordedTransaction1?.Context.Service.Name.Should().Be("Service1");
			recordedTransaction1?.Context.Service.Version.Should().Be("1.0-beta1");

			var recordedTransaction2 = payloadSender.Transactions.FirstOrDefault(t => t.Name == "Transaction2");
			recordedTransaction2.Should().NotBeNull();
			recordedTransaction2?.Context.Service.Name.Should().Be("Service2");
			recordedTransaction2?.Context.Service.Version.Should().Be("1.0-beta2");

			var recordedTransaction3 = payloadSender.Transactions.FirstOrDefault(t => t.Name == "Transaction3");
			recordedTransaction3.Should().NotBeNull();
			recordedTransaction3?.Context.Service.Should().BeNull();
		}

		/// <summary>
		/// Calls <exception cref="ITransaction.SetService"></exception> twice and makes sure the last value is reflected on the
		/// transaction
		/// </summary>
		[Fact]
		public void CustomServiceSetTwice()
		{
			var payloadSender = new MockPayloadSender();
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			var transaction1 = agent.Tracer.StartTransaction("Transaction1", "test");
			transaction1.SetService("Service1", "1.0-beta1");

			transaction1.SetService("Service2", "1.0-beta2");
			transaction1.End();

			payloadSender.WaitForTransactions();
			payloadSender.FirstTransaction.Should().NotBeNull();
			payloadSender.FirstTransaction?.Context.Service.Name.Should().Be("Service2");
			payloadSender.FirstTransaction?.Context.Service.Version.Should().Be("1.0-beta2");
		}

		[Fact]
		public void CaptureErrorOnTracer()
		{
			var payloadSender = new MockPayloadSender();
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			agent.Tracer.CaptureError("foo", "bar");

			payloadSender.WaitForAny();

			payloadSender.Transactions.Should().BeNullOrEmpty();
			payloadSender.Spans.Should().BeNullOrEmpty();
			payloadSender.Errors.Should().HaveCount(1);
			payloadSender.FirstError.Culprit.Should().Be("bar");
			payloadSender.FirstError.Exception.Message.Should().Be("foo");
		}

		[Fact]
		public void CaptureErrorOnTracerWithActiveTransaction()
		{
			var payloadSender = new MockPayloadSender();
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));
			var transaction = agent.Tracer.StartTransaction("Transaction1", "test");

			agent.Tracer.CaptureError("foo", "bar");

			transaction.End();

			payloadSender.WaitForErrors();
			payloadSender.WaitForTransactions();

			payloadSender.FirstError.TransactionId.Should().Be(payloadSender.FirstTransaction.Id);
			payloadSender.FirstError.ParentId.Should().Be(payloadSender.FirstTransaction.Id);
		}

		[Fact]
		public void CaptureErrorOnTracerWithActiveSpan()
		{
			var payloadSender = new MockPayloadSender();
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));
			var transaction = agent.Tracer.StartTransaction("Transaction1", "test");
			var span = transaction.StartSpan("Span1", "test");

			agent.Tracer.CaptureError("foo", "bar");

			transaction.End();
			span.End();

			payloadSender.WaitForErrors();
			payloadSender.WaitForTransactions();
			payloadSender.WaitForSpans();

			payloadSender.FirstError.TransactionId.Should().Be(payloadSender.FirstTransaction.Id);
			payloadSender.FirstError.ParentId.Should().Be(payloadSender.FirstSpan.Id);
		}

		[Fact]
		public void CaptureExceptionOnTracerWithActiveTransaction()
		{
			var payloadSender = new MockPayloadSender();
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));
			var transaction = agent.Tracer.StartTransaction("Transaction1", "test");

			agent.Tracer.CaptureException(new Exception("foo"), "test");

			transaction.End();

			payloadSender.WaitForErrors();
			payloadSender.WaitForTransactions();

			payloadSender.FirstError.TransactionId.Should().Be(payloadSender.FirstTransaction.Id);
			payloadSender.FirstError.ParentId.Should().Be(payloadSender.FirstTransaction.Id);
		}

		[Fact]
		public void CaptureExceptionOnTracerWithActiveSpan()
		{
			var payloadSender = new MockPayloadSender();
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));
			var transaction = agent.Tracer.StartTransaction("Transaction1", "test");
			var span = transaction.StartSpan("Span1", "test");

			agent.Tracer.CaptureException(new Exception("foo"), "test");

			transaction.End();
			span.End();

			payloadSender.WaitForErrors();
			payloadSender.WaitForTransactions();
			payloadSender.WaitForSpans();

			payloadSender.FirstError.TransactionId.Should().Be(payloadSender.FirstTransaction.Id);
			payloadSender.FirstError.ParentId.Should().Be(payloadSender.FirstSpan.Id);
		}

		[Fact]
		public void CaptureErrorLogOnTracerWithActiveTransaction()
		{
			var payloadSender = new MockPayloadSender();
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));
			var transaction = agent.Tracer.StartTransaction("Transaction1", "test");

			var errorLog = new ErrorLog("foo");
			agent.Tracer.CaptureErrorLog(errorLog);

			transaction.End();

			payloadSender.WaitForErrors();
			payloadSender.WaitForTransactions();

			payloadSender.FirstError.TransactionId.Should().Be(payloadSender.FirstTransaction.Id);
			payloadSender.FirstError.ParentId.Should().Be(payloadSender.FirstTransaction.Id);
			payloadSender.FirstError.Log.Message.Should().Be("foo");
		}

		[Fact]
		public void CaptureErrorLogOnTracerWithActiveSpan()
		{
			var payloadSender = new MockPayloadSender();
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));
			var transaction = agent.Tracer.StartTransaction("Transaction1", "test");
			var span = transaction.StartSpan("Span1", "test");

			var errorLog = new ErrorLog("foo");
			agent.Tracer.CaptureErrorLog(errorLog);

			transaction.End();
			span.End();

			payloadSender.WaitForErrors();
			payloadSender.WaitForTransactions();
			payloadSender.WaitForSpans();

			payloadSender.FirstError.TransactionId.Should().Be(payloadSender.FirstTransaction.Id);
			payloadSender.FirstError.ParentId.Should().Be(payloadSender.FirstSpan.Id);
			payloadSender.FirstError.Log.Message.Should().Be("foo");
		}

		[Fact]
		public void CaptureExceptionOnTracer()
		{
			var payloadSender = new MockPayloadSender();
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			try
			{
				throw new Exception("foo");
			}
			catch (Exception e)
			{
				agent.Tracer.CaptureException(e);
			}

			payloadSender.WaitForAny();

			payloadSender.Transactions.Should().BeNullOrEmpty();
			payloadSender.Spans.Should().BeNullOrEmpty();
			payloadSender.Errors.Should().HaveCount(1);
			payloadSender.FirstError.Exception.Message.Should().Be("foo");
		}

		[Fact]
		public void CaptureErrorLogOnTracer()
		{
			var payloadSender = new MockPayloadSender();
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			var errorLog = new ErrorLog("foo") { Level = "error", ParamMessage = "42" };

			agent.Tracer.CaptureErrorLog(errorLog);

			payloadSender.WaitForAny();

			payloadSender.Transactions.Should().BeNullOrEmpty();
			payloadSender.Spans.Should().BeNullOrEmpty();
			payloadSender.Errors.Should().HaveCount(1);
			payloadSender.FirstError.Log.Message.Should().Be("foo");
			payloadSender.FirstError.Log.Level.Should().Be("error");
			payloadSender.FirstError.Log.ParamMessage.Should().Be("42");
		}

		private class TestException : Exception
		{
			public TestException(string message) : base(message) { }
		}
	}
}
