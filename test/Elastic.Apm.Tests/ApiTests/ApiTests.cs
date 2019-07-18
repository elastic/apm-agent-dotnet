using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Model;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Xunit;

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

				payloadSender.Transactions[0].Result.Should().Be(result);
			}
		}

		/// <summary>
		/// Calls ElasticApm.CurrentTransaction without starting any transaction.
		/// Makes sure the returned CurrentTransaction is null and nothing else happens.
		/// </summary>
		[Fact]
		public void GetCurrentTransactionWithNoTransaction()
		{
			using (var agent = new ApmAgent(new TestAgentComponents()))
			{
				var currentTransaction = agent.Tracer.CurrentTransaction;
				currentTransaction.Should().BeNull();
			}
		}

		/// <summary>
		/// Starts a transaction in a Task, does some work in a subtask, and after that it calls ElasticApm.CurrentTransaction.
		/// Makes sure the current transaction is not null - we assert on multiple points
		/// </summary>
		[Fact]
		public async Task GetCurrentTransactionWithNotNull()
		{
			const string transactionName = TestTransaction;
			using (var agent = new ApmAgent(new TestAgentComponents()))
			{
				StartTransaction(agent); //Start transaction on the current task
				await DoAsyncWork(agent); //Do work in subtask

				var currentTransaction = agent.Tracer.CurrentTransaction; //Get transaction in the current task

				currentTransaction.Should().NotBeNull();
				currentTransaction.Name.Should().Be(transactionName);
				currentTransaction.Type.Should().Be(ApiConstants.TypeRequest);
			}

			void StartTransaction(ApmAgent agent)
			{
				Agent.TransactionContainer.Transactions.Value =
					new Transaction(agent, transactionName, ApiConstants.TypeRequest, new TestAgentConfigurationReader(new NoopLogger()));
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
				payloadSender.Transactions.Should().NotBeEmpty();
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
				payloadSender.Transactions.Should().NotBeEmpty();
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

				payloadSender.Transactions.Should().NotBeEmpty();
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

			payloadSender.Transactions.Should().ContainSingle();
			payloadSender.Errors.Should().ContainSingle();
			payloadSender.FirstError.Exception.Message.Should().Be(exceptionMessage);
			payloadSender.FirstError.Exception.Message.Should().Be(exceptionMessage);

			payloadSender.FirstError.Culprit.Should().Be(!string.IsNullOrEmpty(culprit) ? culprit : "PublicAPI-CaptureException");
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

			payloadSender.Transactions.Should().ContainSingle();
			payloadSender.Errors.Should().ContainSingle();
			payloadSender.FirstError.Exception.Message.Should().Be(exceptionMessage);
		}

		/// <summary>
		/// Creates 1 transaction and 1 span with 2 tags on both of them.
		/// Makes sure that the tags are captured.
		/// </summary>
		[Fact]
		public void TagsOnTransactionAndSpan()
		{
			const string transactionName = TestTransaction;
			const string transactionType = UnitTest;
			const string spanName = "TestSpan";
			const string exceptionMessage = "Foo!";
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
			{
				var transaction = agent.Tracer.StartTransaction(transactionName, transactionType);
				transaction.Tags["fooTransaction1"] = "barTransaction1";
				transaction.Tags["fooTransaction2"] = "barTransaction2";

				var span = transaction.StartSpan(spanName, ApiConstants.TypeExternal);
				span.Tags["fooSpan1"] = "barSpan1";
				span.Tags["fooSpan2"] = "barSpan2";

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

			payloadSender.Transactions.Should().ContainSingle();
			payloadSender.Errors.Should().ContainSingle();
			payloadSender.FirstError.Exception.Message.Should().Be(exceptionMessage);

			payloadSender.FirstTransaction.Tags.Should().Contain("fooTransaction1", "barTransaction1");
			payloadSender.FirstTransaction.Context.Tags.Should().Contain("fooTransaction1", "barTransaction1");

			payloadSender.FirstTransaction.Tags.Should().Contain("fooTransaction2", "barTransaction2");
			payloadSender.FirstTransaction.Context.Tags.Should().Contain("fooTransaction2", "barTransaction2");

			payloadSender.SpansOnFirstTransaction[0].Tags.Should().Contain("fooSpan1", "barSpan1");
			payloadSender.SpansOnFirstTransaction[0].Context.Tags.Should().Contain("fooSpan1", "barSpan1");

			payloadSender.SpansOnFirstTransaction[0].Tags.Should().Contain("fooSpan2", "barSpan2");
			payloadSender.SpansOnFirstTransaction[0].Context.Tags.Should().Contain("fooSpan2", "barSpan2");
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
		/// Creates a transaction, then a span and then a sub span, which captures a tag.
		/// Makes sure that the tag is captured by the agent.
		/// </summary>
		[Fact]
		public void SubSpanWithTags()
		{
			var payloadSender = new MockPayloadSender();
			StartTransactionAndSpanWithSubSpan(payloadSender, span2 => { span2.Tags["foo"] = "bar"; });

			payloadSender.FirstSpan.Context.Tags.Should().NotBeEmpty();
			payloadSender.FirstSpan.Context.Tags.Should().ContainKey("foo");
			payloadSender.FirstSpan.Context.Tags["foo"].Should().Be("bar");
		}

		/// <summary>
		/// Creates a transaction, then a span then calls <see cref="ITransaction.End()"/> and <see cref="ISpan.End()"/> twice.
		/// Makes sure that transaction and span sent on the first respective call to End() and the second call is no-op.
		/// </summary>
		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void CallingEndMoreThanOnceIsNoop(bool isSampled)
		{
			var payloadSender = new MockPayloadSender();
			var expectedSpansCount = isSampled ? 1 : 0;

			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, transactionSampleRate: isSampled ? "1" : "0")))
			{
				var transaction = agent.Tracer.StartTransaction(TestTransaction, UnitTest);
				var span = transaction.StartSpan(TestSpan1, ApiConstants.TypeExternal);

				payloadSender.Spans.Should().HaveCount(0);
				span.End();
				payloadSender.Spans.Should().HaveCount(expectedSpansCount);
				if (isSampled) payloadSender.FirstSpan.Name.Should().Be(TestSpan1);
				span.End();
				payloadSender.Spans.Should().HaveCount(expectedSpansCount);

				payloadSender.Transactions.Should().HaveCount(0);
				transaction.End();
				payloadSender.Transactions.Should().HaveCount(1);
				payloadSender.FirstTransaction.Name.Should().Be(TestTransaction);
				transaction.End();
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

			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, transactionSampleRate: isSampled ? "1" : "0")))
			{
				var transaction = agent.Tracer.StartTransaction(TestTransaction, UnitTest);
				var span = transaction.StartSpan(TestSpan1, ApiConstants.TypeExternal);

				payloadSender.Spans.Should().HaveCount(0);
				span.Duration = 123456.789;
				span.End();
				payloadSender.Spans.Should().HaveCount(expectedSpansCount);
				if (isSampled)
				{
					payloadSender.FirstSpan.Duration.Should().Be(123456.789);
				}

				payloadSender.Transactions.Should().HaveCount(0);
				transaction.Duration = 987654.321;
				transaction.End();
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
			expectedErrorContext.Tags["one"] = "1";
			expectedErrorContext.Tags["twenty two"] = "22";

			ITransaction capturedTransaction = null;
			IExecutionSegment errorCapturingExecutionSegment = null;
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
			{
				agent.TracerInternal.Sampler = new Sampler(isSampled ? 1 : 0);
				agent.Tracer.CaptureTransaction(TestTransaction, CustomTransactionTypeForTests, transaction =>
				{
					capturedTransaction = transaction;
					foreach (var keyValue in expectedErrorContext.Tags)
						transaction.Context.Tags[keyValue.Key] = keyValue.Value;
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
					// transaction.Context.Tags["three hundred thirty three"] = "333";

					span?.End();
				});
			}

			payloadSender.Errors.Count.Should().Be(1);
			payloadSender.FirstError.Transaction.IsSampled.Should().Be(isSampled);
			payloadSender.FirstError.Transaction.Type.Should().Be(CustomTransactionTypeForTests);
			payloadSender.FirstError.TransactionId.Should().Be(capturedTransaction.Id);
			payloadSender.FirstError.TraceId.Should().Be(capturedTransaction.TraceId);
			payloadSender.FirstError.ParentId.Should().Be(errorCapturingExecutionSegment.Id);
			payloadSender.FirstError.Context.Should().BeEquivalentTo(expectedErrorContext);
		}

		private class TestException : Exception
		{
			public TestException(string message) : base(message) { }
		}
	}
}
