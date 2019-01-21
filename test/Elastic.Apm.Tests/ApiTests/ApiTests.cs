using System;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Model.Payload;
using Elastic.Apm.Tests.Mocks;
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
		/// <summary>
		/// Starts and ends a transaction with the public API
		/// and makes sure that the transaction is captured by the agent
		/// </summary>
		[Fact]
		public void StartEndTransaction()
		{
			var transactionName = "TestTransaction";
			var transactionType = "UnitTest";
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			var transaction = agent.Tracer.StartTransaction(transactionName, transactionType);

			Thread.Sleep(5); //Make sure we have duration > 0

			transaction.End();
			Assert.Single(payloadSender.Payloads);
			Assert.Equal(transactionName, payloadSender.Payloads[0].Transactions[0].Name);
			Assert.Equal(transactionType, payloadSender.Payloads[0].Transactions[0].Type);
			Assert.True(payloadSender.Payloads[0].Transactions[0].Duration >= 5);
			Assert.True(payloadSender.Payloads[0].Transactions[0].Id != Guid.Empty);

			Assert.NotNull(payloadSender.Payloads[0].Service);
		}

		/// <summary>
		/// Starts a transaction, but does not call the End() method.
		/// Makes sure that the agent does not report the transaction.
		/// </summary>
		[Fact]
		public void StartNoEndTransaction()
		{
			var transactionName = "TestTransaction";
			var transactionType = "UnitTest";
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			var unused = agent.Tracer.StartTransaction(transactionName, transactionType);
			Assert.Empty(payloadSender.Payloads);
		}

		/// <summary>
		/// Starts a transaction, sets its result to 'success' and
		/// makes sure the result is captured by the agent.
		/// </summary>
		[Fact]
		public void TransactionResultTest()
		{
			var transactionType = "TestTransaction";
			var transacitonType = "UnitTest";
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			var transaction = agent.Tracer.StartTransaction(transactionType, transacitonType);
			var result = "success";
			transaction.Result = result;
			transaction.End();

			Assert.Equal(result, payloadSender.Payloads[0].Transactions[0].Result);
		}

		/// <summary>
		/// Calls ElasticApm.CurrentTransaction without starting any transaction.
		/// Makes sure the returned CurrentTransaction is null and nothing else happens.
		/// </summary>
		[Fact]
		public void GetCurrentTransactionWithNoTransaction()
		{
			var agent = new ApmAgent(new TestAgentComponents());
			var currentTransaction = agent.Tracer.CurrentTransaction;
			Assert.Null(currentTransaction);
		}

		/// <summary>
		/// Starts a transaction in a Task, does some work in a subtask, and after that it calls ElasticApm.CurrentTransaction.
		/// Makes sure the current transaction is not null - we assert on multiple points
		/// </summary>
		[Fact]
		public async Task GetCurrentTransactionWithNotNull()
		{
			var transactionName = "TestTransaction";
			var agent = new ApmAgent(new TestAgentComponents());

			StartTransaction(); //Start transaction on the current task
			await DoAsyncWork(); //Do work in subtask

			var currentTransaction = agent.Tracer.CurrentTransaction; //Get transaction in the current task

			Assert.NotNull(currentTransaction);
			Assert.Equal(transactionName, currentTransaction.Name);
			Assert.Equal(ApiConstants.TypeRequest, currentTransaction.Type);

			void StartTransaction()
			{
				Agent.TransactionContainer.Transactions.Value =
					new Transaction(agent, transactionName, ApiConstants.TypeRequest);
			}

			async Task DoAsyncWork()
			{
				//Make sure we have a transaction in the subtask before the async work
				Assert.NotNull(agent.Tracer.CurrentTransaction);
				Assert.Equal(transactionName, agent.Tracer.CurrentTransaction.Name);
				Assert.Equal(ApiConstants.TypeRequest, agent.Tracer.CurrentTransaction.Type);

				await Task.Delay(50);

				//and after the async work
				Assert.NotNull(agent.Tracer.CurrentTransaction);
				Assert.Equal(transactionName, agent.Tracer.CurrentTransaction.Name);
				Assert.Equal(ApiConstants.TypeRequest, agent.Tracer.CurrentTransaction.Type);
			}
		}

		/// <summary>
		/// Starts a transaction and then starts a span, ends both of them probably.
		/// Makes sure that the transaction and the span are captured.
		/// </summary>
		[Fact]
		public void TransactionWithSpan()
		{
			var transactionName = "TestTransaction";
			var transactionType = "UnitTest";
			var spanName = "TestSpan";
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			var transaction = agent.Tracer.StartTransaction(transactionName, transactionType);

			var span = transaction.StartSpan(spanName, ApiConstants.TypeExternal);

			Thread.Sleep(5); //Make sure we have duration > 0

			span.End();
			transaction.End();
			Assert.NotEmpty(payloadSender.Payloads);
			Assert.NotEmpty(payloadSender.Payloads[0].Transactions[0].Spans);

			Assert.Equal(spanName, payloadSender.Payloads[0].Transactions[0].Spans[0].Name);
			Assert.True(payloadSender.Payloads[0].Transactions[0].Spans[0].Duration >= 5);
			Assert.True(payloadSender.Payloads[0].Transactions[0].Spans[0].Id >= 5);
			Assert.NotNull(payloadSender.Payloads[0].Service);
		}

		/// <summary>
		/// Starts a transaction and then calls transaction.StartSpan, but doesn't call Span.End().
		/// Makes sure that the transaction is recorded, but the span isn't.
		/// </summary>
		[Fact]
		public void TransactionWithSpanWithoutEnd()
		{
			var transactionName = "TestTransaction";
			var transactionType = "UnitTest";
			var spanName = "TestSpan";
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			var transaction = agent.Tracer.StartTransaction(transactionName, transactionType);

			var unused = transaction.StartSpan(spanName, ApiConstants.TypeExternal);

			Thread.Sleep(5); //Make sure we have duration > 0

			transaction.End(); //Ends transaction, but doesn't end span.
			Assert.NotEmpty(payloadSender.Payloads);
			Assert.Empty(payloadSender.Payloads[0].Transactions[0].Spans);

			Assert.NotNull(payloadSender.Payloads[0].Service);
		}

		/// <summary>
		/// Starts a transaction and a span with subtype and action and ends them properly.
		/// Makes sure the subtype and the action are recorded
		/// </summary>
		[Fact]
		public void TransactionWithSpanWithSubTypeAndAction()
		{
			var transactionName = "TestTransaction";
			var transactionType = "UnitTest";
			var spanName = "TestSpan";
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			var transaction = agent.Tracer.StartTransaction(transactionName, transactionType);
			var span = transaction.StartSpan(spanName, ApiConstants.TypeDb, ApiConstants.SubtypeMssql, ApiConstants.ActionQuery);
			span.End();
			transaction.End();

			Assert.NotEmpty(payloadSender.Payloads);
			Assert.NotEmpty(payloadSender.Payloads[0].Transactions[0].Spans);

			Assert.Equal(ApiConstants.TypeDb, payloadSender.Payloads[0].Transactions[0].Spans[0].Type);
			Assert.Equal(ApiConstants.SubtypeMssql, payloadSender.Payloads[0].Transactions[0].Spans[0].Subtype);
			Assert.Equal(ApiConstants.ActionQuery, payloadSender.Payloads[0].Transactions[0].Spans[0].Action);

			Assert.NotNull(payloadSender.Payloads[0].Service);
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
		private void ErrorOnTransactionCommon(string culprit = null)
		{
			var transactionName = "TestTransaction";
			var transacitonType = "UnitTest";
			var exceptionMessage = "Foo!";
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			var transaction = agent.Tracer.StartTransaction(transactionName, transacitonType);

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

			Assert.Single(payloadSender.Payloads);
			Assert.Single(payloadSender.Errors);
			Assert.Equal(exceptionMessage, payloadSender.Errors[0].Errors[0].Exception.Message);
			Assert.Equal(exceptionMessage, payloadSender.Errors[0].Errors[0].Exception.Message);

			Assert.Equal(!string.IsNullOrEmpty(culprit) ? culprit : "PublicAPI-CaptureException", payloadSender.Errors[0].Errors[0].Culprit);
		}

		/// <summary>
		/// Starts a transaction with a span, ends the transaction and the span properly and call CaptureException() on the span.
		/// Makes sure the exception is captured.
		/// </summary>
		[Fact]
		public void ErrorOnSpan()
		{
			var transactionName = "TestTransaction";
			var transactionType = "UnitTest";
			var spanName = "TestSpan";
			var exceptionMessage = "Foo!";
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

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

			Assert.Single(payloadSender.Payloads);
			Assert.Single(payloadSender.Errors);
			Assert.Equal(exceptionMessage, payloadSender.Errors[0].Errors[0].Exception.Message);
			Assert.Equal(exceptionMessage, payloadSender.Errors[0].Errors[0].Exception.Message);
		}

		/// <summary>
		/// Creates 1 transaction and 1 span with 2 tags on both of them.
		/// Makes sure that the tags are captured.
		/// </summary>
		[Fact]
		public void TagsOnTransactionAndSpan()
		{
			var transactionName = "TestTransaction";
			var transactionType = "UnitTest";
			var spanName = "TestSpan";
			var exceptionMessage = "Foo!";
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

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

			Assert.Single(payloadSender.Payloads);
			Assert.Single(payloadSender.Errors);
			Assert.Equal(exceptionMessage, payloadSender.Errors[0].Errors[0].Exception.Message);
			Assert.Equal(exceptionMessage, payloadSender.Errors[0].Errors[0].Exception.Message);

			Assert.Equal("barTransaction1", payloadSender.Payloads[0].Transactions[0].Tags["fooTransaction1"]);
			Assert.Equal("barTransaction1", payloadSender.Payloads[0].Transactions[0].Context.Tags["fooTransaction1"]);

			Assert.Equal("barTransaction2", payloadSender.Payloads[0].Transactions[0].Tags["fooTransaction2"]);
			Assert.Equal("barTransaction2", payloadSender.Payloads[0].Transactions[0].Context.Tags["fooTransaction2"]);

			Assert.Equal("barSpan1", payloadSender.Payloads[0].Transactions[0].Spans[0].Tags["fooSpan1"]);
			Assert.Equal("barSpan1", payloadSender.Payloads[0].Transactions[0].Spans[0].Context.Tags["fooSpan1"]);

			Assert.Equal("barSpan2", payloadSender.Payloads[0].Transactions[0].Spans[0].Tags["fooSpan2"]);
			Assert.Equal("barSpan2", payloadSender.Payloads[0].Transactions[0].Spans[0].Context.Tags["fooSpan2"]);
		}
	}
}
