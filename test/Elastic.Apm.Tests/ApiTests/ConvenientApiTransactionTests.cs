﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Model.Payload;
using Elastic.Apm.Tests.Mocks;
using Xunit;
using Request = Elastic.Apm.Api.Request;

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
		private const int SleepLength = 450;
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
				() => { Thread.Sleep(SleepLength); });
		});

		/// <summary>
		/// Tests the <see cref="Tracer.CaptureTransaction(string,string,System.Action)" /> method with an exception.
		/// It wraps a fake transaction (Thread.Sleep) that throws an exception into the CaptureTransaction method
		/// and it makes sure that the transaction and the exception are captured by the agent.
		/// </summary>
		[Fact]
		public void SimpleActionWithException() => AssertWith1TransactionAnd1Error(agent =>
		{
			Assert.Throws<InvalidOperationException>(() =>
			{
				agent.Tracer.CaptureTransaction(TransactionName, TransactionType, new Action(() =>
				{
					Thread.Sleep(SleepLength);
					throw new InvalidOperationException(ExceptionMessage);
				}));
			});
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
					Assert.NotNull(t);
					Thread.Sleep(SleepLength);
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
					Assert.NotNull(t);
					Thread.Sleep(SleepLength);
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
				Thread.Sleep(SleepLength);
				return 42;
			});

			Assert.Equal(42, res);
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
					Assert.NotNull(t);
					Thread.Sleep(SleepLength);
					return 42;
				});

			Assert.Equal(42, res);
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
			Assert.Throws<InvalidOperationException>(() =>
			{
				var result = agent.Tracer.CaptureTransaction(TransactionName, TransactionType, t =>
				{
					Assert.NotNull(t);
					Thread.Sleep(SleepLength);

					if (new Random().Next(1) == 0) //avoid unreachable code warning.
						throw new InvalidOperationException(ExceptionMessage);

					return 42;
				});

				Assert.True(false); //Should not be executed because the agent isn't allowed to catch an exception.
				Assert.Equal(42, result); //But if it'd not throw it'd be 42.
			});
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
			Assert.Throws<InvalidOperationException>(() =>
			{
				var result = agent.Tracer.CaptureTransaction(TransactionName, TransactionType, () =>
				{
					Thread.Sleep(SleepLength);

					if (new Random().Next(1) == 0) //avoid unreachable code warning.
						throw new InvalidOperationException(ExceptionMessage);

					return 42;
				});

				Assert.True(false); //Should not be executed because the agent isn't allowed to catch an exception.
				Assert.Equal(42, result); //But if it'd not throw it'd be 42.
			});
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
				async () => { await Task.Delay(SleepLength); });
		});

		/// <summary>
		/// Tests the <see cref="Tracer.CaptureTransaction(string,string,System.Func{Task})" /> method with an exception
		/// It wraps a fake async transaction (Task.Delay) that throws an exception into the CaptureTransaction method
		/// and it makes sure that the transaction and the error are captured.
		/// </summary>
		[Fact]
		public async Task AsyncTaskWithException() => await AssertWith1TransactionAnd1ErrorAsync(async agent =>
		{
			await Assert.ThrowsAsync<InvalidOperationException>(async () =>
			{
				await agent.Tracer.CaptureTransaction(TransactionName, TransactionType, async () =>
				{
					await Task.Delay(SleepLength);
					throw new InvalidOperationException(ExceptionMessage);
				});
			});
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
					Assert.NotNull(t);
					await Task.Delay(SleepLength);
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
			await Assert.ThrowsAsync<InvalidOperationException>(async () =>
			{
				await agent.Tracer.CaptureTransaction(TransactionName, TransactionType, async t =>
				{
					Assert.NotNull(t);
					await Task.Delay(SleepLength);
					throw new InvalidOperationException(ExceptionMessage);
				});
			});
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
				await Task.Delay(SleepLength);
				return 42;
			});
			Assert.Equal(42, res);
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
					Assert.NotNull(t);
					await Task.Delay(SleepLength);
					return 42;
				});

			Assert.Equal(42, res);
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
			await Assert.ThrowsAsync<InvalidOperationException>(async () =>
			{
				var result = await agent.Tracer.CaptureTransaction(TransactionName, TransactionType, async t =>
				{
					Assert.NotNull(t);
					await Task.Delay(SleepLength);

					if (new Random().Next(1) == 0) //avoid unreachable code warning.
						throw new InvalidOperationException(ExceptionMessage);

					return 42;
				});

				Assert.True(false); //Should not be executed because the agent isn't allowed to catch an exception.
				Assert.Equal(42, result); //But if it'd not throw it'd be 42.
			});
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
			await Assert.ThrowsAsync<InvalidOperationException>(async () =>
			{
				var result = await agent.Tracer.CaptureTransaction(TransactionName, TransactionType, async () =>
				{
					await Task.Delay(SleepLength);

					if (new Random().Next(1) == 0) //avoid unreachable code warning.
						throw new InvalidOperationException(ExceptionMessage);

					return 42;
				});

				Assert.True(false); //Should not be executed because the agent isn't allowed to catch an exception.
				Assert.Equal(42, result); //But if it'd not throw it'd be 42.
			});
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

			await Assert.ThrowsAsync<OperationCanceledException>(async () =>
			{
				await agent.Tracer.CaptureTransaction(TransactionName, TransactionType,
					async () =>
					{
						// ReSharper disable once MethodSupportsCancellation, we want to delay before we throw the exception
						await Task.Delay(SleepLength);
						token.ThrowIfCancellationRequested();
					});
			});

			Assert.NotEmpty(payloadSender.Payloads);
			Assert.NotEmpty(payloadSender.Payloads[0].Transactions);

			Assert.Equal(TransactionName, payloadSender.Payloads[0].Transactions[0].Name);
			Assert.Equal(TransactionType, payloadSender.Payloads[0].Transactions[0].Type);

			//`await Task.Delay` is inaccurate, so we enable 10% error in the test
			var expectedTransactionLength = SleepLength - SleepLength * 0.1;
			var duration = payloadSender.Payloads[0].Transactions[0].Duration;
			Assert.True(duration >= expectedTransactionLength, $"Expected {duration} to be greater or equal to: {expectedTransactionLength}");

			Assert.NotEmpty(payloadSender.Errors);
			Assert.NotEmpty(payloadSender.Errors[0].Errors);

			Assert.Equal("A task was canceled", payloadSender.Errors[0].Errors[0].Culprit);
			Assert.Equal("Task canceled", payloadSender.Errors[0].Errors[0].Exception.Message);
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
						Thread.Sleep(SleepLength);
						transaction.Tags["foo"] = "bar";
					});
				});

			//According to the Intake API tags are stored on the Context (and not on Transaction.Tags directly).
			Assert.Equal("bar", payloadSender.FirstTransaction.Context.Tags["foo"]);

			//Also make sure the tag is visible directly on Transaction.Tags.
			Assert.Equal("bar", payloadSender.Payloads[0].Transactions[0].Tags["foo"]);
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
						await Task.Delay(SleepLength);
						transaction.Tags["foo"] = "bar";
					});
				});

			//According to the Intake API tags are stored on the Context (and not on Transaction.Tags directly).
			Assert.Equal("bar", payloadSender.FirstTransaction.Context.Tags["foo"]);

			//Also make sure the tag is visible directly on Transaction.Tags.
			Assert.Equal("bar", payloadSender.Payloads[0].Transactions[0].Tags["foo"]);
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
					await Assert.ThrowsAsync<InvalidOperationException>(async () =>
					{
						await t.Tracer.CaptureTransaction(TransactionName, TransactionType, async transaction =>
						{
							await Task.Delay(SleepLength);
							transaction.Tags["foo"] = "bar";

							if (new Random().Next(1) == 0) //avoid unreachable code warning.
								throw new InvalidOperationException(ExceptionMessage);
						});
					});
				});

			//According to the Intake API tags are stored on the Context (and not on Transaction.Tags directly).
			Assert.Equal("bar", payloadSender.FirstTransaction.Context.Tags["foo"]);

			//Also make sure the tag is visible directly on Transaction.Tags.
			Assert.Equal("bar", payloadSender.Payloads[0].Transactions[0].Tags["foo"]);
		}

		/// <summary>
		/// Creates a transaction and attaches a Request to this transaction.
		/// Makes sure that the transaction details are captured.
		/// </summary>
		[Fact]
		public void TransactionWithRequest()
		{
			var payloadSender = AssertWith1Transaction(
				n =>
				{
					n.Tracer.CaptureTransaction(TransactionName, TransactionType, transaction =>
					{
						Thread.Sleep(SleepLength);
						transaction.Request = new Request("GET", "HTTP");
					});
				});

			Assert.Equal("GET", payloadSender.FirstTransaction.Context.Request.Method);
			Assert.Equal("HTTP", payloadSender.FirstTransaction.Context.Request.Url.Protocol);
		}

		/// <summary>
		/// Creates a transaction and attaches a Request to this transaction. It fills all the fields on the Request.
		/// Makes sure that all the transaction details are captured.
		/// </summary>
		[Fact]
		public void TransactionWithRequestDetailed()
		{
			var payloadSender = AssertWith1Transaction(
				n =>
				{
					n.Tracer.CaptureTransaction(TransactionName, TransactionType, transaction =>
					{
						Thread.Sleep(SleepLength);
						transaction.Request = new Request("GET", "HTTP");
						transaction.Request.HttpVersion = "2.0";
						transaction.Request.SocketEncrypted = true;
						transaction.Request.UrlFull = "https://elastic.co";
						transaction.Request.UrlRaw = "https://elastic.co";
						transaction.Request.SocketRemoteAddress = "127.0.0.1";
						transaction.Request.UrlHostName = "elastic";
					});
				});

			Assert.Equal("GET", payloadSender.FirstTransaction.Context.Request.Method);
			Assert.Equal("HTTP", payloadSender.FirstTransaction.Context.Request.Url.Protocol);

			Assert.Equal("2.0", payloadSender.FirstTransaction.Context.Request.HttpVersion);
			Assert.True(payloadSender.FirstTransaction.Context.Request.Socket.Encrypted);
			Assert.Equal("https://elastic.co", payloadSender.FirstTransaction.Context.Request.Url.Full);
			Assert.Equal("https://elastic.co", payloadSender.FirstTransaction.Context.Request.Url.Raw);
			Assert.Equal("127.0.0.1", payloadSender.FirstTransaction.Context.Request.Socket.RemoteAddress);
			Assert.Equal("elastic", payloadSender.FirstTransaction.Context.Request.Url.HostName);
		}

		/// <summary>
		/// Asserts on 1 transaction with async code
		/// </summary>
		private async Task<MockPayloadSender> AssertWith1TransactionAsync(Func<ApmAgent, Task> func)
		{
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			await func(agent);

			Assert.NotEmpty(payloadSender.Payloads);
			Assert.NotEmpty(payloadSender.Payloads[0].Transactions);

			Assert.Equal(TransactionName, payloadSender.Payloads[0].Transactions[0].Name);
			Assert.Equal(TransactionType, payloadSender.Payloads[0].Transactions[0].Type);

			//`await Task.Delay` is inaccurate, so we enable 10% error in the test
			var expectedTransactionLength = SleepLength - SleepLength * 0.1;
			var duration = payloadSender.Payloads[0].Transactions[0].Duration;
			Assert.True(duration >= expectedTransactionLength, $"Expected {duration} to be greater or equal to: {expectedTransactionLength}");

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

			Assert.NotEmpty(payloadSender.Payloads);
			Assert.NotEmpty(payloadSender.Payloads[0].Transactions);

			Assert.Equal(TransactionName, payloadSender.Payloads[0].Transactions[0].Name);
			Assert.Equal(TransactionType, payloadSender.Payloads[0].Transactions[0].Type);

			//`await Task.Delay` is inaccurate, so we enable 10% error in the test
			var expectedTransactionLength = SleepLength - SleepLength * 0.1;
			var duration = payloadSender.Payloads[0].Transactions[0].Duration;
			Assert.True(duration >= expectedTransactionLength, $"Expected {duration} to be greater or equal to: {expectedTransactionLength}");

			Assert.NotEmpty(payloadSender.Errors);
			Assert.NotEmpty(payloadSender.Errors[0].Errors);

			Assert.Equal(typeof(InvalidOperationException).FullName, payloadSender.Errors[0].Errors[0].Exception.Type);
			Assert.Equal(ExceptionMessage, payloadSender.Errors[0].Errors[0].Exception.Message);
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

			Assert.NotEmpty(payloadSender.Payloads);
			Assert.NotEmpty(payloadSender.Payloads[0].Transactions);

			Assert.Equal(TransactionName, payloadSender.Payloads[0].Transactions[0].Name);
			Assert.Equal(TransactionType, payloadSender.Payloads[0].Transactions[0].Type);

			//`await Task.Delay` is inaccurate, so we enable 10% error in the test
			var expectedTransactionLength = SleepLength - SleepLength * 0.1;
			var duration = payloadSender.Payloads[0].Transactions[0].Duration;
			Assert.True(duration >= expectedTransactionLength, $"Expected {duration} to be greater or equal to: {expectedTransactionLength}");

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

			Assert.NotEmpty(payloadSender.Payloads);
			Assert.NotEmpty(payloadSender.Payloads[0].Transactions);

			Assert.Equal(TransactionName, payloadSender.Payloads[0].Transactions[0].Name);
			Assert.Equal(TransactionType, payloadSender.Payloads[0].Transactions[0].Type);


			//`await Task.Delay` is inaccurate, so we enable 10% error in the test
			var expectedTransactionLength = SleepLength - SleepLength * 0.1;
			var duration = payloadSender.Payloads[0].Transactions[0].Duration;
			Assert.True(duration >= expectedTransactionLength, $"Expected {duration} to be greater or equal to: {expectedTransactionLength}");

			Assert.NotEmpty(payloadSender.Errors);
			Assert.NotEmpty(payloadSender.Errors[0].Errors);

			Assert.Equal(typeof(InvalidOperationException).FullName, payloadSender.Errors[0].Errors[0].Exception.Type);
			Assert.Equal(ExceptionMessage, payloadSender.Errors[0].Errors[0].Exception.Message);
		}
	}
}
