using System;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Model.Payload;
using Elastic.Apm.Tests.Extensions;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.ApiTests
{
	/// <summary>
	/// Tests the API for manual instrumentation.
	/// Only tests scenarios when using the convenient API and only test spans.
	/// Transactions are covered by <see cref="ConvenientApiTransactionTests" />.
	/// Scenarios with manually calling <see cref="Tracer.StartTransaction" />,
	/// <see cref="Transaction.StartSpan" />, <see cref="Transaction.End" />
	/// are covered by <see cref="ApiTests" />
	/// Very similar to <see cref="ConvenientApiTransactionTests" />. The test cases are the same,
	/// but this one tests the CaptureSpan method - including every single overload.
	/// </summary>
	public class ConvenientApiSpanTests
	{
		private const string ExceptionMessage = "Foo";
		private const string SpanName = "TestSpan";

		private const string SpanType = "TestSpan";
		private const string TransactionName = "ConvenientApiTest";

		private const string TransactionType = "Test";

		/// <summary>
		/// Tests the <see cref="Transaction.CaptureSpan(string,string,System.Action,string,string)" /> method.
		/// It wraps a fake span (Thread.Sleep) into the CaptureSpan method
		/// and it makes sure that the span is captured by the agent.
		/// </summary>
		[Fact]
		public void SimpleAction()
			=> AssertWith1TransactionAnd1Span(t => { t.CaptureSpan(SpanName, SpanType, () => { WaitHelpers.Sleep2XMinimum(); }); });

		/// <summary>
		/// Tests the <see cref="Transaction.CaptureSpan(string,string,System.Action,string,string)" /> method with an exception.
		/// It wraps a fake span (Thread.Sleep) that throws an exception into the CaptureSpan method
		/// and it makes sure that the span and the exception are captured by the agent.
		/// </summary>
		[Fact]
		public void SimpleActionWithException()
			=> AssertWith1TransactionAnd1SpanAnd1Error(t =>
			{
				Action act = () =>
				{
					t.CaptureSpan(SpanName, SpanType, new Action(() =>
					{
						WaitHelpers.Sleep2XMinimum();
						throw new InvalidOperationException(ExceptionMessage);
					}));
				};
				act.Should().Throw<InvalidOperationException>();
			});

		/// <summary>
		/// Tests the <see cref="Transaction.CaptureSpan(string,string,System.Action{ISpan},string,string)" /> method.
		/// It wraps a fake span (Thread.Sleep) into the CaptureSpan method with an <see cref="Action{T}" /> parameter
		/// and it makes sure that the span is captured by the agent and the <see cref="Action{ISpan}" /> parameter is not null
		/// </summary>
		[Fact]
		public void SimpleActionWithParameter()
			=> AssertWith1TransactionAnd1Span(t =>
			{
				t.CaptureSpan(SpanName, SpanType,
					s =>
					{
						s.Should().NotBeNull();
						WaitHelpers.Sleep2XMinimum();
					});
			});

		/// <summary>
		/// Tests the <see cref="Transaction.CaptureSpan(string,string,System.Action{ISpan},string,string)" /> method with an
		/// exception.
		/// It wraps a fake span (Thread.Sleep) that throws an exception into the CaptureSpan method with an
		/// <see cref="Action{ISpan}" /> parameter
		/// and it makes sure that the span and the error are captured by the agent and the <see cref="ISpan" /> parameter is not
		/// null
		/// </summary>
		[Fact]
		public void SimpleActionWithExceptionAndParameter()
			=> AssertWith1TransactionAnd1SpanAnd1Error(t =>
			{
				Action act = () =>
				{
					t.CaptureSpan(SpanName, SpanType, new Action<ISpan>(s =>
					{
						s.Should().NotBeNull();
						WaitHelpers.Sleep2XMinimum();
						throw new InvalidOperationException(ExceptionMessage);
					}));
				};
				act.Should().Throw<InvalidOperationException>().WithMessage(ExceptionMessage);
			});

		/// <summary>
		/// Tests the <see cref="Transaction.CaptureSpan{T}(string,string,System.Func{T},string,string)" /> method.
		/// It wraps a fake span (Thread.Sleep) with a return value into the CaptureSpan method
		/// and it makes sure that the span is captured by the agent and the return value is correct.
		/// </summary>
		[Fact]
		public void SimpleActionWithReturnType()
			=> AssertWith1TransactionAnd1Span(t =>
			{
				var res = t.CaptureSpan(SpanName, SpanType, () =>
				{
					WaitHelpers.Sleep2XMinimum();
					return 42;
				});

				res.Should().Be(42);
			});

		/// <summary>
		/// Tests the <see cref="Transaction.CaptureSpan{T}(string,string,System.Func{ISpan,T},string,string)" /> method.
		/// It wraps a fake span (Thread.Sleep) with a return value into the CaptureSpan method with an
		/// <see cref="Action{ISpan}" /> parameter
		/// and it makes sure that the span is captured by the agent and the return value is correct and the
		/// <see cref="Action{ISpan}" /> is not null.
		/// </summary>
		[Fact]
		public void SimpleActionWithReturnTypeAndParameter()
			=> AssertWith1TransactionAnd1Span(t =>
			{
				var res = t.CaptureSpan(SpanName, SpanType, s =>
				{
					t.Should().NotBeNull();
					WaitHelpers.Sleep2XMinimum();
					return 42;
				});

				res.Should().Be(42);
			});

		/// <summary>
		/// Tests the <see cref="Transaction.CaptureSpan{T}(string,string,System.Func{ISpan,T},string,string)" /> method with an
		/// exception.
		/// It wraps a fake span (Thread.Sleep) with a return value that throws an exception into the CaptureSpan method with an
		/// <see cref="Action{ISpan}" /> parameter
		/// and it makes sure that the span and the error are captured by the agent and the return value is correct and the
		/// <see cref="Action{ISpan}" /> is not null.
		/// </summary>
		[Fact]
		public void SimpleActionWithReturnTypeAndExceptionAndParameter()
			=> AssertWith1TransactionAnd1SpanAnd1Error(t =>
			{
				Action act = () =>
				{
					var result = t.CaptureSpan(SpanName, SpanType, s =>
					{
						s.Should().NotBeNull();
						WaitHelpers.Sleep2XMinimum();

						if (new Random().Next(1) == 0) //avoid unreachable code warning.
							throw new InvalidOperationException(ExceptionMessage);

						return 42;
					});
					throw new Exception("CaptureSpan should not eat exception and continue");
				};
				act.Should().Throw<InvalidOperationException>().WithMessage(ExceptionMessage);
			});

		/// <summary>
		/// Tests the <see cref="Transaction.CaptureSpan{T}(string,string,System.Func{ISpan,T},string,string)" /> method with an
		/// exception.
		/// It wraps a fake span (Thread.Sleep) with a return value that throws an exception into the CaptureSpan method with an
		/// <see cref="Action{ISpan}" /> parameter
		/// and it makes sure that the span and the error are captured by the agent and the return value is correct and the
		/// <see cref="Action{ISpan}" /> is not null.
		/// </summary>
		[Fact]
		public void SimpleActionWithReturnTypeAndException()
			=> AssertWith1TransactionAnd1SpanAnd1Error(t =>
			{
				var alwaysThrow = new Random().Next(1) == 0;
				Func<int> act = () => t.CaptureSpan(SpanName, SpanType, () =>
				{
					WaitHelpers.Sleep2XMinimum();

					if (alwaysThrow) //avoid unreachable code warning.
						throw new InvalidOperationException(ExceptionMessage);

					return 42;
				});
				act.Should().Throw<InvalidOperationException>().WithMessage(ExceptionMessage);
			});

		/// <summary>
		/// Tests the <see cref="Transaction.CaptureSpan(string,string,System.Func{Task},string,string)" /> method.
		/// It wraps a fake async span (Task.Delay) into the CaptureSpan method
		/// and it makes sure that the span is captured.
		/// </summary>
		[Fact]
		public async Task AsyncTask()
			=> await AssertWith1TransactionAnd1SpanAsync(async t =>
			{
				await t.CaptureSpan(SpanName, SpanType, async () => { await WaitHelpers.Delay2XMinimum(); });
			});

		/// <summary>
		/// Tests the <see cref="Transaction.CaptureSpan(string,string,System.Func{Task},string,string)" /> method with an
		/// exception
		/// It wraps a fake async span (Task.Delay) that throws an exception into the CaptureSpan method
		/// and it makes sure that the span and the error are captured.
		/// </summary>
		[Fact]
		public async Task AsyncTaskWithException()
			=> await AssertWith1TransactionAnd1ErrorAnd1SpanAsync(async t =>
			{
				Func<Task> act = async () =>
				{
					await t.CaptureSpan(SpanName, SpanType, async () =>
					{
						await WaitHelpers.Delay2XMinimum();
						throw new InvalidOperationException(ExceptionMessage);
					});
				};
				var should = await act.Should().ThrowAsync<InvalidOperationException>();
				should.WithMessage(ExceptionMessage);
			});

		/// <summary>
		/// Tests the <see cref="Transaction.CaptureSpan(string,string,System.Func{ISpan, Task},string,string)" /> method.
		/// It wraps a fake async span (Task.Delay) into the CaptureSpan method with an <see cref="Action{ISpan}" /> parameter
		/// and it makes sure that the span is captured and the <see cref="Action{ISpan}" /> parameter is not null.
		/// </summary>
		[Fact]
		public async Task AsyncTaskWithParameter()
			=> await AssertWith1TransactionAnd1SpanAsync(async t =>
			{
				await t.CaptureSpan(SpanName, SpanType,
					async s =>
					{
						s.Should().NotBeNull();
						await WaitHelpers.Delay2XMinimum();
					});
			});

		/// <summary>
		/// Tests the <see cref="Transaction.CaptureSpan(string,string,System.Func{ISpan, Task},string,string)" /> method with an
		/// exception.
		/// It wraps a fake async span (Task.Delay) that throws an exception into the CaptureSpan method with an
		/// <see cref="Action{ISpan}" /> parameter
		/// and it makes sure that the span and the error are captured and the <see cref="Action{ISpan}" /> parameter is not null.
		/// </summary>
		[Fact]
		public async Task AsyncTaskWithExceptionAndParameter()
			=> await AssertWith1TransactionAnd1ErrorAnd1SpanAsync(async t =>
			{
				Func<Task> act = async () =>
				{
					await t.CaptureSpan(SpanName, SpanType, async s =>
					{
						s.Should().NotBeNull();
						await WaitHelpers.Delay2XMinimum();
						throw new InvalidOperationException(ExceptionMessage);
					});
				};
				await act.Should().ThrowAsync<InvalidOperationException>();
			});

		/// <summary>
		/// Tests the <see cref="Transaction.CaptureSpan{T}(string,string,System.Func{Task{T}},string,string)" /> method.
		/// It wraps a fake async span (Task.Delay) with a return value into the CaptureSpan method
		/// and it makes sure that the span is captured by the agent and the return value is correct.
		/// </summary>
		[Fact]
		public async Task AsyncTaskWithReturnType()
			=> await AssertWith1TransactionAnd1SpanAsync(async t =>
			{
				var res = await t.CaptureSpan(SpanName, SpanType, async () =>
				{
					await WaitHelpers.Delay2XMinimum();
					return 42;
				});
				res.Should().Be(42);
			});

		/// <summary>
		/// Tests the <see cref="Transaction.CaptureSpan{T}(string,string,System.Func{ISpan,Task{T}},string,string)" /> method.
		/// It wraps a fake async span (Task.Delay) with a return value into the CaptureSpan method with an
		/// <see cref="Action{ISpan}" /> parameter
		/// and it makes sure that the span is captured by the agent and the return value is correct and the <see cref="ISpan" />
		/// is not null.
		/// </summary>
		[Fact]
		public async Task AsyncTaskWithReturnTypeAndParameter()
			=> await AssertWith1TransactionAnd1SpanAsync(async t =>
			{
				var res = await t.CaptureSpan(SpanName, SpanType,
					async s =>
					{
						s.Should().NotBeNull();
						await WaitHelpers.Delay2XMinimum();
						return 42;
					});

				res.Should().Be(42);
			});

		/// <summary>
		/// Tests the <see cref="Transaction.CaptureSpan{T}(string,string,System.Func{ISpan,Task{T}},string,string)" /> method with
		/// an exception.
		/// It wraps a fake async span (Task.Delay) with a return value that throws an exception into the CaptureSpan method with
		/// an <see cref="Action{ISpan}" /> parameter
		/// and it makes sure that the span and the error are captured by the agent and the return value is correct and the
		/// <see cref="Action{ISpan}" /> is not null.
		/// </summary>
		[Fact]
		public async Task AsyncTaskWithReturnTypeAndExceptionAndParameter()
			=> await AssertWith1TransactionAnd1ErrorAnd1SpanAsync(async t =>
			{
				Func<Task> act = async () =>
				{
					var result = await t.CaptureSpan(SpanName, SpanType, async s =>
					{
						s.Should().NotBeNull();
						await WaitHelpers.Delay2XMinimum();

						if (new Random().Next(1) == 0) //avoid unreachable code warning.
							throw new InvalidOperationException(ExceptionMessage);

						return 42;
					});
				};
				await act.Should().ThrowAsync<InvalidOperationException>();
			});

		/// <summary>
		/// Tests the <see cref="Transaction.CaptureSpan{T}(string,string,System.Func{Task{T}},string,string)" /> method with an
		/// exception.
		/// It wraps a fake async span (Task.Delay) with a return value that throws an exception into the CaptureSpan method
		/// and it makes sure that the span and the error are captured by the agent.
		/// </summary>
		[Fact]
		public async Task AsyncTaskWithReturnTypeAndException()
			=> await AssertWith1TransactionAnd1ErrorAnd1SpanAsync(async t =>
			{
				Func<Task> act = async () =>
				{
					var result = await t.CaptureSpan(SpanName, SpanType, async () =>
					{
						await WaitHelpers.Delay2XMinimum();

						if (new Random().Next(1) == 0) //avoid unreachable code warning.
							throw new InvalidOperationException(ExceptionMessage);

						return 42;
					});
				};
				await act.Should().ThrowAsync<InvalidOperationException>();
			});

		/// <summary>
		/// Wraps a cancelled task into the CaptureSpan method and
		/// makes sure that the cancelled task is captured by the agent.
		/// </summary>
		[Fact]
		public async Task CancelledAsyncTask()
		{
			var agent = new ApmAgent(new TestAgentComponents());

			var cancellationTokenSource = new CancellationTokenSource();
			var token = cancellationTokenSource.Token;
			cancellationTokenSource.Cancel();

			await agent.Tracer.CaptureTransaction(TransactionName, TransactionType, async t =>
			{
				Func<Task> act = async () =>
				{
					await t.CaptureSpan(SpanName, SpanType, async () =>
					{
						// ReSharper disable once MethodSupportsCancellation, we want to delay before we throw the exception
						await WaitHelpers.Delay2XMinimum();
						token.ThrowIfCancellationRequested();
					});
				};
				await act.Should().ThrowAsync<OperationCanceledException>();
			});
		}

		/// <summary>
		/// Creates a custom span and adds a tag to it.
		/// Makes sure that the tag is stored on Span.Context.
		/// </summary>
		[Fact]
		public void TagsOnSpan()
		{
			var payloadSender = AssertWith1TransactionAnd1Span(
				t =>
				{
					t.CaptureSpan(SpanName, SpanType, span =>
					{
						WaitHelpers.Sleep2XMinimum();
						span.Tags["foo"] = "bar";
					});
				});

			//According to the Intake API tags are stored on the Context (and not on Spans.Tags directly).
			payloadSender.SpansOnFirstTransaction[0].Context.Tags.Should().Contain("foo","bar");

			//Also make sure the tag is visible directly on Span.Tags.
			payloadSender.SpansOnFirstTransaction[0].Tags.Should().Contain("foo","bar");
		}

		/// <summary>
		/// Creates a custom async span and adds a tag to it.
		/// Makes sure that the tag is stored on Span.Context.
		/// </summary>
		[Fact]
		public async Task TagsOnSpanAsync()
		{
			var payloadSender = await AssertWith1TransactionAnd1SpanAsync(
				async t =>
				{
					await t.CaptureSpan(SpanName, SpanType, async span =>
					{
						await WaitHelpers.Delay2XMinimum();
						span.Tags["foo"] = "bar";
					});
				});

			//According to the Intake API tags are stored on the Context (and not on Spans.Tags directly).
			payloadSender.SpansOnFirstTransaction[0].Context.Tags.Should().Contain("foo","bar");

			//Also make sure the tag is visible directly on Span.Tags.
			payloadSender.SpansOnFirstTransaction[0].Tags.Should().Contain("foo","bar");
		}

		/// <summary>
		/// Creates a custom async span that ends with an error and adds a tag to it.
		/// Makes sure that the tag is stored on Span.Context.
		/// </summary>
		[Fact]
		public async Task TagsOnSpanAsyncError()
		{
			var payloadSender = await AssertWith1TransactionAnd1ErrorAnd1SpanAsync(async t =>
			{
				Func<Task> act = async () =>
				{
					await t.CaptureSpan(SpanName, SpanType, async span =>
					{
						await WaitHelpers.Delay2XMinimum();
						span.Tags["foo"] = "bar";

						if (new Random().Next(1) == 0) //avoid unreachable code warning.
							throw new InvalidOperationException(ExceptionMessage);
					});
				};
				await act.Should().ThrowAsync<InvalidOperationException>();
			});

			//According to the Intake API tags are stored on the Context (and not on Spans.Tags directly).
			payloadSender.SpansOnFirstTransaction[0].Context.Tags.Should().Contain("foo","bar");

			//Also make sure the tag is visible directly on Span.Tags.
			payloadSender.SpansOnFirstTransaction[0].Tags.Should().Contain("foo","bar");
		}

		/// <summary>
		/// Asserts on 1 transaction with 1 async span and 1 error
		/// </summary>
		private async Task<MockPayloadSender> AssertWith1TransactionAnd1ErrorAnd1SpanAsync(Func<ITransaction, Task> func)
		{
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			await agent.Tracer.CaptureTransaction(TransactionName, TransactionType, async t =>
			{
				await WaitHelpers.DelayMinimum();
				await func(t);
			});

			payloadSender.Transactions.Should().NotBeEmpty();

			payloadSender.FirstTransaction.Name.Should().Be(TransactionName);
			payloadSender.FirstTransaction.Type.Should().Be(TransactionType);

			var duration = payloadSender.FirstTransaction.Duration;
			duration.Should().BeGreaterOrEqualToMinimumSleepLength(numberOfSleeps: 3);

			payloadSender.SpansOnFirstTransaction.Should().NotBeEmpty();

			payloadSender.SpansOnFirstTransaction[0].Name.Should().Be(SpanName);
			payloadSender.SpansOnFirstTransaction[0].Type.Should().Be(SpanType);


			payloadSender.Errors.Should().NotBeEmpty();

			payloadSender.FirstError.CapturedException.Type.Should().Be(typeof(InvalidOperationException).FullName);
			payloadSender.FirstError.CapturedException.Message.Should().Be(ExceptionMessage);

			return payloadSender;
		}


		/// <summary>
		/// Asserts on 1 transaction with 1 async Span
		/// </summary>
		private async Task<MockPayloadSender> AssertWith1TransactionAnd1SpanAsync(Func<ITransaction, Task> func)
		{
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			await agent.Tracer.CaptureTransaction(TransactionName, TransactionType, async t =>
			{
				await WaitHelpers.DelayMinimum();
				await func(t);
			});

			payloadSender.Transactions.Should().NotBeEmpty();

			payloadSender.FirstTransaction.Name.Should().Be(TransactionName);
			payloadSender.FirstTransaction.Type.Should().Be(TransactionType);

			var duration = payloadSender.FirstTransaction.Duration;
			duration.Should().BeGreaterOrEqualToMinimumSleepLength(numberOfSleeps: 3);

			payloadSender.SpansOnFirstTransaction.Should().NotBeEmpty();

			payloadSender.SpansOnFirstTransaction[0].Name.Should().Be(SpanName);
			payloadSender.SpansOnFirstTransaction[0].Type.Should().Be(SpanType);

			return payloadSender;
		}

		/// <summary>
		/// Asserts on 1 transaction with 1 span
		/// </summary>
		private MockPayloadSender AssertWith1TransactionAnd1Span(Action<ITransaction> action)
		{
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			agent.Tracer.CaptureTransaction(TransactionName, TransactionType, t =>
			{
				WaitHelpers.SleepMinimum();
				action(t);
			});

			payloadSender.Transactions.Should().NotBeEmpty();

			payloadSender.FirstTransaction.Name.Should().Be(TransactionName);
			payloadSender.FirstTransaction.Type.Should().Be(TransactionType);

			payloadSender.SpansOnFirstTransaction.Should().NotBeEmpty();

			payloadSender.SpansOnFirstTransaction[0].Name.Should().Be(SpanName);
			payloadSender.SpansOnFirstTransaction[0].Type.Should().Be(SpanType);

			var duration = payloadSender.FirstTransaction.Duration;
			duration.Should().BeGreaterOrEqualToMinimumSleepLength(3);

			return payloadSender;
		}

		/// <summary>
		/// Asserts on 1 transaction with 1 span and 1 error
		/// </summary>
		private void AssertWith1TransactionAnd1SpanAnd1Error(Action<ITransaction> action)
		{
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			agent.Tracer.CaptureTransaction(TransactionName, TransactionType, t =>
			{
				WaitHelpers.SleepMinimum();
				action(t);
			});

			payloadSender.Transactions.Should().NotBeEmpty();

			payloadSender.FirstTransaction.Name.Should().Be(TransactionName);
			payloadSender.FirstTransaction.Type.Should().Be(TransactionType);

			var duration = payloadSender.FirstTransaction.Duration;
			duration.Should().BeGreaterOrEqualToMinimumSleepLength(3);

			payloadSender.SpansOnFirstTransaction.Should().NotBeEmpty();

			payloadSender.SpansOnFirstTransaction[0].Name.Should().Be(SpanName);
			payloadSender.SpansOnFirstTransaction[0].Type.Should().Be(SpanType);

			payloadSender.Errors.Should().NotBeEmpty();
			payloadSender.Errors.Should().NotBeEmpty();

			payloadSender.FirstError.CapturedException.Type.Should().Be(typeof(InvalidOperationException).FullName);
			payloadSender.FirstError.CapturedException.Message.Should().Be(ExceptionMessage);
		}
	}
}
