using System;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Model.Payload;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.ApiTests
{
	/// <summary>
	/// Tests the API for manual instrumentation.
	/// Only tests scenarios when using the convenient API and only test transactions.
	/// Spans are covered by <see cref="ConvenientApiSpanTests" />.
	/// Scenarios with manually calling <see cref="Tracer.StartTransaction" />,
	/// <see cref="Transaction.StartSpan" />, <see cref="Transaction.End" />
	/// are covered by <see cref="ApiTests" />
	/// Very similar to <see cref="ConvenientApiSpanTests" />. The test cases are the same,
	/// but this one tests the CaptureTransaction method - including every single overload.
	/// </summary>
	public class ConvenientApiTransactionTests
	{
		private const string ExceptionMessage = "Foo";

		private const string TransactionName = "ConvenientApiTest";
		private const string TransactionType = "Test";

		/// <summary>
		/// Tests the <see cref="Tracer.CaptureTransaction(string,string,System.Action)" /> method.
		/// It wraps a fake transaction (Thread.Sleep) into the CaptureTransaction method
		/// and it makes sure that the transaction is captured by the agent.
		/// </summary>
		[Fact]
		public void SimpleAction() => AssertWith1Transaction(agent =>
		{
			agent.Tracer.CaptureTransaction(TransactionName, TransactionType,
				() => { WaitHelpers.SleepMinimum(); });
		});

		/// <summary>
		/// Tests the <see cref="Tracer.CaptureTransaction(string,string,System.Action)" /> method with an exception.
		/// It wraps a fake transaction (Thread.Sleep) that throws an exception into the CaptureTransaction method
		/// and it makes sure that the transaction and the exception are captured by the agent.
		/// </summary>
		[Fact]
		public void SimpleActionWithException() => AssertWith1TransactionAnd1Error(agent =>
		{
			Action act = () =>
			{
				agent.Tracer.CaptureTransaction(TransactionName, TransactionType, new Action(() =>
				{
					WaitHelpers.SleepMinimum();
					throw new InvalidOperationException(ExceptionMessage);
				}));
			};
			act.Should().Throw<InvalidOperationException>();
		});

		/// <summary>
		/// Tests the <see cref="Tracer.CaptureTransaction(string,string,System.Action{ITransaction})" /> method.
		/// It wraps a fake transaction (Thread.Sleep) into the CaptureTransaction method with an <see cref="Action{T}" />
		/// parameter
		/// and it makes sure that the transaction is captured by the agent and the <see cref="Action{ITransaction}" /> parameter
		/// is not null
		/// </summary>
		[Fact]
		public void SimpleActionWithParameter() => AssertWith1Transaction(agent =>
		{
			agent.Tracer.CaptureTransaction(TransactionName, TransactionType,
				t =>
				{
					t.Should().NotBeNull();
					WaitHelpers.SleepMinimum();
				});
		});

		/// <summary>
		/// Tests the <see cref="Tracer.CaptureTransaction(string,string,System.Action{ITransaction})" /> method with an exception.
		/// It wraps a fake transaction (Thread.Sleep) that throws an exception into the CaptureTransaction method with an
		/// <see cref="Action{ITransaction}" /> parameter
		/// and it makes sure that the transaction and the error are captured by the agent and the
		/// <see cref="Action{ITransaction}" /> parameter is not null
		/// </summary>
		[Fact]
		public void SimpleActionWithExceptionAndParameter() => AssertWith1TransactionAnd1Error(agent =>
		{
			Assert.Throws<InvalidOperationException>(() =>
			{
				agent.Tracer.CaptureTransaction(TransactionName, TransactionType, new Action<ITransaction>(t =>
				{
					t.Should().NotBeNull();
					WaitHelpers.SleepMinimum();
					throw new InvalidOperationException(ExceptionMessage);
				}));
			});
		});

		/// <summary>
		/// Tests the <see cref="Tracer.CaptureTransaction{T}(string,string,System.Func{T})" /> method.
		/// It wraps a fake transaction (Thread.Sleep) with a return value into the CaptureTransaction method
		/// and it makes sure that the transaction is captured by the agent and the return value is correct.
		/// </summary>
		[Fact]
		public void SimpleActionWithReturnType() => AssertWith1Transaction(agent =>
		{
			var res = agent.Tracer.CaptureTransaction(TransactionName, TransactionType, () =>
			{
				WaitHelpers.SleepMinimum();
				return 42;
			});

			res.Should().Be(42);
		});

		/// <summary>
		/// Tests the <see cref="Tracer.CaptureTransaction{T}(string,string,System.Func{ITransaction,T})" /> method.
		/// It wraps a fake transaction (Thread.Sleep) with a return value into the CaptureTransaction method with an
		/// <see cref="Action{ITransaction}" /> parameter
		/// and it makes sure that the transaction is captured by the agent and the return value is correct and the
		/// <see cref="Action{ITransaction}" /> is not null.
		/// </summary>
		[Fact]
		public void SimpleActionWithReturnTypeAndParameter() => AssertWith1Transaction(agent =>
		{
			var res = agent.Tracer.CaptureTransaction(TransactionName, TransactionType,
				t =>
				{
					t.Should().NotBeNull();
					WaitHelpers.SleepMinimum();
					return 42;
				});

			res.Should().Be(42);
		});

		/// <summary>
		/// Tests the <see cref="Tracer.CaptureTransaction{T}(string,string,System.Func{ITransaction,T})" /> method with an
		/// exception.
		/// It wraps a fake transaction (Thread.Sleep) with a return value that throws an exception into the CaptureTransaction
		/// method with an <see cref="Action{ITransaction}" /> parameter
		/// and it makes sure that the transaction and the error are captured by the agent and the return value is correct and the
		/// <see cref="Action{ITransaction}" /> is not null.
		/// </summary>
		[Fact]
		public void SimpleActionWithReturnTypeAndExceptionAndParameter() => AssertWith1TransactionAnd1Error(agent =>
		{
			Action act = () =>
			{
				var result = agent.Tracer.CaptureTransaction(TransactionName, TransactionType, t =>
				{
					t.Should().NotBeNull();
					WaitHelpers.SleepMinimum();

					if (new Random().Next(1) == 0) //avoid unreachable code warning.
						throw new InvalidOperationException(ExceptionMessage);

					return 42;
				});
				throw new Exception("CaptureTransaction should not eat exception and continue");
			};
			act.Should().Throw<InvalidOperationException>();
		});

		/// <summary>
		/// Tests the <see cref="Tracer.CaptureTransaction{T}(string,string,System.Func{T})" /> method with an exception.
		/// It wraps a fake transaction (Thread.Sleep) with a return value that throws an exception into the CaptureTransaction
		/// method
		/// and it makes sure that the transaction and the error are captured by the agent.
		/// </summary>
		[Fact]
		public void SimpleActionWithReturnTypeAndException() => AssertWith1TransactionAnd1Error(agent =>
		{
			Action act = () =>
			{
				var result = agent.Tracer.CaptureTransaction(TransactionName, TransactionType, () =>
				{
					WaitHelpers.SleepMinimum();

					if (new Random().Next(1) == 0) //avoid unreachable code warning.
						throw new InvalidOperationException(ExceptionMessage);

					return 42;
				});
				throw new Exception("CaptureTransaction should not eat exception and continue");
			};
			act.Should().Throw<InvalidOperationException>();
		});

		/// <summary>
		/// Tests the <see cref="Tracer.CaptureTransaction(string,string,System.Func{Task})" /> method.
		/// It wraps a fake async transaction (Task.Delay) into the CaptureTransaction method
		/// and it makes sure that the transaction is captured.
		/// </summary>
		[Fact]
		public async Task AsyncTask() => await AssertWith1TransactionAsync(async agent =>
		{
			await agent.Tracer.CaptureTransaction(TransactionName, TransactionType,
				async () => { await WaitHelpers.DelayMinimum(); });
		});

		/// <summary>
		/// Tests the <see cref="Tracer.CaptureTransaction(string,string,System.Func{Task})" /> method with an exception
		/// It wraps a fake async transaction (Task.Delay) that throws an exception into the CaptureTransaction method
		/// and it makes sure that the transaction and the error are captured.
		/// </summary>
		[Fact]
		public async Task AsyncTaskWithException() => await AssertWith1TransactionAnd1ErrorAsync(async agent =>
		{
			Func<Task> act = async () =>
			{
				await agent.Tracer.CaptureTransaction(TransactionName, TransactionType, async () =>
				{
					await WaitHelpers.DelayMinimum();
					throw new InvalidOperationException(ExceptionMessage);
				});
			};
			await act.Should().ThrowAsync<InvalidOperationException>();
		});

		/// <summary>
		/// Tests the <see cref="Tracer.CaptureTransaction(string,string,System.Func{ITransaction, Task})" /> method.
		/// It wraps a fake async transaction (Task.Delay) into the CaptureTransaction method with an
		/// <see cref="Action{ITransaction}" /> parameter
		/// and it makes sure that the transaction is captured and the <see cref="Action{ITransaction}" /> parameter is not null.
		/// </summary>
		[Fact]
		public async Task AsyncTaskWithParameter() => await AssertWith1TransactionAsync(async agent =>
		{
			await agent.Tracer.CaptureTransaction(TransactionName, TransactionType,
				async t =>
				{
					t.Should().NotBeNull();
					await WaitHelpers.DelayMinimum();
				});
		});

		/// <summary>
		/// Tests the <see cref="Tracer.CaptureTransaction(string,string,System.Func{ITransaction, Task})" /> method with an
		/// exception.
		/// It wraps a fake async transaction (Task.Delay) that throws an exception into the CaptureTransaction method with an
		/// <see cref="Action{ITransaction}" /> parameter
		/// and it makes sure that the transaction and the error are captured and the <see cref="Action{ITransaction}" /> parameter
		/// is not null.
		/// </summary>
		[Fact]
		public async Task AsyncTaskWithExceptionAndParameter() => await AssertWith1TransactionAnd1ErrorAsync(async agent =>
		{
			Func<Task> act = async () =>
			{
				await agent.Tracer.CaptureTransaction(TransactionName, TransactionType, async t =>
				{
					t.Should().NotBeNull();
					await WaitHelpers.DelayMinimum();
					throw new InvalidOperationException(ExceptionMessage);
				});
			};
			await act.Should().ThrowAsync<InvalidOperationException>();
		});

		/// <summary>
		/// Tests the <see cref="Tracer.CaptureTransaction{T}(string,string,System.Func{Task{T}})" /> method.
		/// It wraps a fake async transaction (Task.Delay) with a return value into the CaptureTransaction method
		/// and it makes sure that the transaction is captured by the agent and the return value is correct.
		/// </summary>
		[Fact]
		public async Task AsyncTaskWithReturnType() => await AssertWith1TransactionAsync(async agent =>
		{
			var res = await agent.Tracer.CaptureTransaction(TransactionName, TransactionType, async () =>
			{
				await WaitHelpers.DelayMinimum();
				return 42;
			});
			res.Should().Be(42);
		});

		/// <summary>
		/// Tests the <see cref="Tracer.CaptureTransaction{T}(string,string,System.Func{ITransaction,Task{T}})" /> method.
		/// It wraps a fake async transaction (Task.Delay) with a return value into the CaptureTransaction method with an
		/// <see cref="Action{ITransaction}" /> parameter
		/// and it makes sure that the transaction is captured by the agent and the return value is correct and the
		/// <see cref="ITransaction" /> is not null.
		/// </summary>
		[Fact]
		public async Task AsyncTaskWithReturnTypeAndParameter() => await AssertWith1TransactionAsync(async agent =>
		{
			var res = await agent.Tracer.CaptureTransaction(TransactionName, TransactionType,
				async t =>
				{
					t.Should().NotBeNull();
					await WaitHelpers.DelayMinimum();
					return 42;
				});

			res.Should().Be(42);
		});

		/// <summary>
		/// Tests the <see cref="Tracer.CaptureTransaction{T}(string,string,System.Func{ITransaction,Task{T}})" /> method with an
		/// exception.
		/// It wraps a fake async transaction (Task.Delay) with a return value that throws an exception into the CaptureTransaction
		/// method with an <see cref="Action{ITransaction}" /> parameter
		/// and it makes sure that the transaction and the error are captured by the agent and the return value is correct and the
		/// <see cref="Action{ITransaction}" /> is not null.
		/// </summary>
		[Fact]
		public async Task AsyncTaskWithReturnTypeAndExceptionAndParameter() => await AssertWith1TransactionAnd1ErrorAsync(async agent =>
		{
			Func<Task> act = async () =>
			{
				var result = await agent.Tracer.CaptureTransaction(TransactionName, TransactionType, async t =>
				{
					t.Should().NotBeNull();
					await WaitHelpers.DelayMinimum();

					if (new Random().Next(1) == 0) //avoid unreachable code warning.
						throw new InvalidOperationException(ExceptionMessage);

					return 42;
				});

				Assert.True(false); //Should not be executed because the agent isn't allowed to catch an exception.
				result.Should().Be(42); //But if it'd not throw it'd be 42.
			};
			await act.Should().ThrowAsync<InvalidOperationException>();
		});

		/// <summary>
		/// Tests the <see cref="Tracer.CaptureTransaction{T}(string,string,System.Func{Task{T}})" /> method with an exception.
		/// It wraps a fake async transaction (Task.Delay) with a return value that throws an exception into the CaptureTransaction
		/// method
		/// and it makes sure that the transaction and the error are captured by the agent.
		/// </summary>
		[Fact]
		public async Task AsyncTaskWithReturnTypeAndException() => await AssertWith1TransactionAnd1ErrorAsync(async agent =>
		{
			Func<Task> act = async () =>
			{
				var result = await agent.Tracer.CaptureTransaction(TransactionName, TransactionType, async () =>
				{
					await WaitHelpers.DelayMinimum();

					if (new Random().Next(1) == 0) //avoid unreachable code warning.
						throw new InvalidOperationException(ExceptionMessage);

					return 42;
				});

				Assert.True(false); //Should not be executed because the agent isn't allowed to catch an exception.
				result.Should().Be(42); //But if it'd not throw it'd be 42.
			};
			await act.Should().ThrowAsync<InvalidOperationException>();
		});

		/// <summary>
		/// Wraps a cancelled task into the CaptureTransaction method and
		/// makes sure that the cancelled task is captured by the agent.
		/// </summary>
		[Fact]
		public async Task CancelledAsyncTask()
		{
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			var cancellationTokenSource = new CancellationTokenSource();
			var token = cancellationTokenSource.Token;
			cancellationTokenSource.Cancel();

			Func<Task> act = async () =>
			{
				await agent.Tracer.CaptureTransaction(TransactionName, TransactionType,
					async () =>
					{
						// ReSharper disable once MethodSupportsCancellation, we want to delay before we throw the exception
						await WaitHelpers.DelayMinimum();
						token.ThrowIfCancellationRequested();
					});
			};
			await act.Should().ThrowAsync<OperationCanceledException>();

			payloadSender.Payloads.Should().NotBeEmpty();
			payloadSender.Payloads[0].Transactions.Should().NotBeEmpty();

			payloadSender.Payloads[0].Transactions[0].Name.Should().Be(TransactionName);
			payloadSender.Payloads[0].Transactions[0].Type.Should().Be(TransactionType);

			var duration = payloadSender.Payloads[0].Transactions[0].Duration;
			Assert.True(duration >= WaitHelpers.SleepLength, $"Expected {duration} to be greater or equal to: {WaitHelpers.SleepLength}");

			payloadSender.Errors.Should().NotBeEmpty();
			payloadSender.Errors[0].Errors.Should().NotBeEmpty();

			payloadSender.Errors[0].Errors[0].Culprit.Should().Be("A task was canceled");
			payloadSender.Errors[0].Errors[0].Exception.Message.Should().Be("Task canceled");
		}

		/// <summary>
		/// Creates a custom transaction and adds a tag to it.
		/// Makes sure that the tag is stored on Transaction.Context.
		/// </summary>
		[Fact]
		public void TagsOnTransaction()
		{
			var payloadSender = AssertWith1Transaction(
				t =>
				{
					t.Tracer.CaptureTransaction(TransactionName, TransactionType, transaction =>
					{
						WaitHelpers.SleepMinimum();
						transaction.Tags["foo"] = "bar";
					});
				});

			//According to the Intake API tags are stored on the Context (and not on Transaction.Tags directly).
			payloadSender.FirstTransaction.Context.Tags.Should().Contain("foo", "bar");

			//Also make sure the tag is visible directly on Transaction.Tags.
			payloadSender.Payloads[0].Transactions[0].Tags.Should().Contain("foo", "bar");
		}

		/// <summary>
		/// Creates a custom async transaction and adds a tag to it.
		/// Makes sure that the tag is stored on Transaction.Context.
		/// </summary>
		[Fact]
		public async Task TagsOnTransactionAsync()
		{
			var payloadSender = await AssertWith1TransactionAsync(
				async t =>
				{
					await t.Tracer.CaptureTransaction(TransactionName, TransactionType, async transaction =>
					{
						await WaitHelpers.DelayMinimum();
						transaction.Tags["foo"] = "bar";
					});
				});

			//According to the Intake API tags are stored on the Context (and not on Transaction.Tags directly).
			payloadSender.FirstTransaction.Context.Tags.Should().Contain("foo", "bar");

			//Also make sure the tag is visible directly on Transaction.Tags.
			payloadSender.Payloads[0].Transactions[0].Tags.Should().Contain("foo", "bar");
		}

		/// <summary>
		/// Creates a custom async Transaction that ends with an error and adds a tag to it.
		/// Makes sure that the tag is stored on Transaction.Context.
		/// </summary>
		[Fact]
		public async Task TagsOnTransactionAsyncError()
		{
			var payloadSender = await AssertWith1TransactionAnd1ErrorAsync(
				async t =>
				{
					Func<Task> act = async () =>
					{
						await t.Tracer.CaptureTransaction(TransactionName, TransactionType, async transaction =>
						{
							await WaitHelpers.DelayMinimum();
							transaction.Tags["foo"] = "bar";

							if (new Random().Next(1) == 0) //avoid unreachable code warning.
								throw new InvalidOperationException(ExceptionMessage);
						});
					};
					await act.Should().ThrowAsync<InvalidOperationException>();
				});

			//According to the Intake API tags are stored on the Context (and not on Transaction.Tags directly).
			payloadSender.FirstTransaction.Context.Tags.Should().Contain("foo", "bar");

			//Also make sure the tag is visible directly on Transaction.Tags.
			payloadSender.Payloads[0].Transactions[0].Tags.Should().Contain("foo", "bar");
		}

		/// <summary>
		/// Asserts on 1 transaction with async code
		/// </summary>
		private async Task<MockPayloadSender> AssertWith1TransactionAsync(Func<ApmAgent, Task> func)
		{
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			await func(agent);

			payloadSender.Payloads.Should().NotBeEmpty();
			payloadSender.Payloads[0].Transactions.Should().NotBeEmpty();

			payloadSender.Payloads[0].Transactions[0].Name.Should().Be(TransactionName);
			payloadSender.Payloads[0].Transactions[0].Type.Should().Be(TransactionType);

			var duration = payloadSender.Payloads[0].Transactions[0].Duration;
			WaitHelpers.AssertMinimumSleepLength(duration);

			return payloadSender;
		}

		/// <summary>
		/// Asserts on 1 async transaction and 1 error
		/// </summary>
		private async Task<MockPayloadSender> AssertWith1TransactionAnd1ErrorAsync(Func<ApmAgent, Task> func)
		{
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			await func(agent);

			payloadSender.Payloads.Should().NotBeEmpty();
			payloadSender.Payloads[0].Transactions.Should().NotBeEmpty();

			payloadSender.Payloads[0].Transactions[0].Name.Should().Be(TransactionName);
			payloadSender.Payloads[0].Transactions[0].Type.Should().Be(TransactionType);

			var duration = payloadSender.Payloads[0].Transactions[0].Duration;
			WaitHelpers.AssertMinimumSleepLength(duration);

			payloadSender.Errors.Should().NotBeEmpty();
			payloadSender.Errors[0].Errors.Should().NotBeEmpty();

			payloadSender.Errors[0].Errors[0].Exception.Type.Should().Be(typeof(InvalidOperationException).FullName);
			payloadSender.Errors[0].Errors[0].Exception.Message.Should().Be(ExceptionMessage);
			return payloadSender;
		}

		/// <summary>
		/// Asserts on 1 transaction
		/// </summary>
		private MockPayloadSender AssertWith1Transaction(Action<ApmAgent> action)
		{
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			action(agent);

			payloadSender.Payloads.Should().NotBeEmpty();
			payloadSender.Payloads[0].Transactions.Should().NotBeEmpty();

			payloadSender.Payloads[0].Transactions[0].Name.Should().Be(TransactionName);
			payloadSender.Payloads[0].Transactions[0].Type.Should().Be(TransactionType);

			var duration = payloadSender.Payloads[0].Transactions[0].Duration;
			WaitHelpers.AssertMinimumSleepLength(duration);

			return payloadSender;
		}

		/// <summary>
		/// Asserts on 1 transaction and 1 error
		/// </summary>
		private void AssertWith1TransactionAnd1Error(Action<ApmAgent> action)
		{
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			action(agent);

			payloadSender.Payloads.Should().NotBeEmpty();
			payloadSender.Payloads[0].Transactions.Should().NotBeEmpty();

			payloadSender.Payloads[0].Transactions[0].Name.Should().Be(TransactionName);
			payloadSender.Payloads[0].Transactions[0].Type.Should().Be(TransactionType);

			var duration = payloadSender.Payloads[0].Transactions[0].Duration;
			WaitHelpers.AssertMinimumSleepLength(duration);

			payloadSender.Errors.Should().NotBeEmpty();
			payloadSender.Errors[0].Errors.Should().NotBeEmpty();

			payloadSender.Errors[0].Errors[0].Exception.Type.Should().Be(typeof(InvalidOperationException).FullName);
			payloadSender.Errors[0].Errors[0].Exception.Message.Should().Be(ExceptionMessage);
		}
	}
}
