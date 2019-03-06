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
			const string transactionName = "TestTransaction";
			const string transactionType = "UnitTest";
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			var transaction = agent.Tracer.StartTransaction(transactionName, transactionType);

			Thread.Sleep(5); //Make sure we have duration > 0

			transaction.End();
      
			payloadSender.Payloads.Should().ContainSingle();
			var capturedTransaction = payloadSender.Payloads[0].Transactions[0];
			capturedTransaction.Name.Should().Be(transactionName);
			capturedTransaction.Type.Should().Be(transactionType);
			capturedTransaction.Duration.Should().BeGreaterOrEqualTo(5);
			capturedTransaction.Id.Should().NotBeEmpty();

			payloadSender.Payloads[0].Service.Should().NotBeNull();
		}

		/// <summary>
		/// Starts a transaction, but does not call the End() method.
		/// Makes sure that the agent does not report the transaction.
		/// </summary>
		[Fact]
		public void StartNoEndTransaction()
		{
			const string transactionName = "TestTransaction";
			const string transactionType = "UnitTest";
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			var unused = agent.Tracer.StartTransaction(transactionName, transactionType);
			payloadSender.Payloads.Should().BeEmpty();
		}

		/// <summary>
		/// Starts a transaction, sets its result to 'success' and
		/// makes sure the result is captured by the agent.
		/// </summary>
		[Fact]
		public void TransactionResultTest()
		{
			const string transactionName = "TestTransaction";
			const string transactionType = "UnitTest";
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			var transaction = agent.Tracer.StartTransaction(transactionName, transactionType);
			const string result = "success";
			transaction.Result = result;
			transaction.End();

			payloadSender.Payloads[0].Transactions[0].Result.Should().Be(result);
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
			currentTransaction.Should().BeNull();
		}

		/// <summary>
		/// Starts a transaction in a Task, does some work in a subtask, and after that it calls ElasticApm.CurrentTransaction.
		/// Makes sure the current transaction is not null - we assert on multiple points
		/// </summary>
		[Fact]
		public async Task GetCurrentTransactionWithNotNull()
		{
			const string transactionName = "TestTransaction";
			var agent = new ApmAgent(new TestAgentComponents());

			StartTransaction(); //Start transaction on the current task
			await DoAsyncWork(); //Do work in subtask

			var currentTransaction = agent.Tracer.CurrentTransaction; //Get transaction in the current task

			currentTransaction.Should().NotBeNull();
			currentTransaction.Name.Should().Be(transactionName);
			currentTransaction.Type.Should().Be(ApiConstants.TypeRequest);

			void StartTransaction()
			{
				Agent.TransactionContainer.Transactions.Value =
					new Transaction(agent, transactionName, ApiConstants.TypeRequest);
			}

			async Task DoAsyncWork()
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
			const string transactionName = "TestTransaction";
			const string transactionType = "UnitTest";
			const string spanName = "TestSpan";
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			var transaction = agent.Tracer.StartTransaction(transactionName, transactionType);

			var span = transaction.StartSpan(spanName, ApiConstants.TypeExternal);

			Thread.Sleep(5); //Make sure we have duration > 0

			span.End();
			transaction.End();
			payloadSender.Payloads.Should().NotBeEmpty();
			payloadSender.SpansOnFirstTransaction.Should().NotBeEmpty();

			payloadSender.SpansOnFirstTransaction[0].Name.Should().Be(spanName);
			payloadSender.SpansOnFirstTransaction[0].Duration.Should().BeGreaterOrEqualTo(5);
			payloadSender.Payloads[0].Service.Should().NotBeNull();
		}

		/// <summary>
		/// Starts a transaction and then calls transaction.StartSpan, but doesn't call Span.End().
		/// Makes sure that the transaction is recorded, but the span isn't.
		/// </summary>
		[Fact]
		public void TransactionWithSpanWithoutEnd()
		{
			const string transactionName = "TestTransaction";
			const string transactionType = "UnitTest";
			const string spanName = "TestSpan";
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			var transaction = agent.Tracer.StartTransaction(transactionName, transactionType);

			var unused = transaction.StartSpan(spanName, ApiConstants.TypeExternal);

			Thread.Sleep(5); //Make sure we have duration > 0

			transaction.End(); //Ends transaction, but doesn't end span.
			payloadSender.Payloads.Should().NotBeEmpty();
			payloadSender.SpansOnFirstTransaction.Should().BeEmpty();

			payloadSender.Payloads[0].Service.Should().NotBeNull();
		}

		/// <summary>
		/// Starts a transaction and a span with subtype and action and ends them properly.
		/// Makes sure the subtype and the action are recorded
		/// </summary>
		[Fact]
		public void TransactionWithSpanWithSubTypeAndAction()
		{
			const string transactionName = "TestTransaction";
			const string transactionType = "UnitTest";
			const string spanName = "TestSpan";
			var payloadSender = new MockPayloadSender();
			var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			var transaction = agent.Tracer.StartTransaction(transactionName, transactionType);
			var span = transaction.StartSpan(spanName, ApiConstants.TypeDb, ApiConstants.SubtypeMssql, ApiConstants.ActionQuery);
			span.End();
			transaction.End();

			payloadSender.Payloads.Should().NotBeEmpty();
			payloadSender.SpansOnFirstTransaction.Should().NotBeEmpty();

			payloadSender.SpansOnFirstTransaction[0].Type.Should().Be(ApiConstants.TypeDb);
			payloadSender.SpansOnFirstTransaction[0].Subtype.Should().Be(ApiConstants.SubtypeMssql);
			payloadSender.SpansOnFirstTransaction[0].Action.Should().Be(ApiConstants.ActionQuery);

			payloadSender.Payloads[0].Service.Should().NotBeNull();
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
			const string transactionName = "TestTransaction";
			const string transacitonType = "UnitTest";
			const string exceptionMessage = "Foo!";
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

			payloadSender.Payloads.Should().ContainSingle();
			payloadSender.Errors.Should().ContainSingle();
			payloadSender.Errors[0].Errors[0].Exception.Message.Should().Be(exceptionMessage);
			payloadSender.Errors[0].Errors[0].Exception.Message.Should().Be(exceptionMessage);

			payloadSender.Errors[0].Errors[0].Culprit.Should().Be(!string.IsNullOrEmpty(culprit) ? culprit : "PublicAPI-CaptureException");
		}

		/// <summary>
		/// Starts a transaction with a span, ends the transaction and the span properly and call CaptureException() on the span.
		/// Makes sure the exception is captured.
		/// </summary>
		[Fact]
		public void ErrorOnSpan()
		{
			const string transactionName = "TestTransaction";
			const string transactionType = "UnitTest";
			const string spanName = "TestSpan";
			const string exceptionMessage = "Foo!";
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

			payloadSender.Payloads.Should().ContainSingle();
			payloadSender.Errors.Should().ContainSingle();
			payloadSender.Errors[0].Errors[0].Exception.Message.Should().Be(exceptionMessage);
		}

		/// <summary>
		/// Creates 1 transaction and 1 span with 2 tags on both of them.
		/// Makes sure that the tags are captured.
		/// </summary>
		[Fact]
		public void TagsOnTransactionAndSpan()
		{
			const string transactionName = "TestTransaction";
			const string transactionType = "UnitTest";
			const string spanName = "TestSpan";
			const string exceptionMessage = "Foo!";
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

			payloadSender.Payloads.Should().ContainSingle();
			payloadSender.Errors.Should().ContainSingle();
			payloadSender.Errors[0].Errors[0].Exception.Message.Should().Be(exceptionMessage);

			payloadSender.Payloads[0].Transactions[0].Tags.Should().Contain("fooTransaction1", "barTransaction1");
			payloadSender.FirstTransaction.Context.Tags.Should().Contain("fooTransaction1", "barTransaction1");

			payloadSender.Payloads[0].Transactions[0].Tags.Should().Contain("fooTransaction2", "barTransaction2");
			payloadSender.FirstTransaction.Context.Tags.Should().Contain("fooTransaction2", "barTransaction2");

			payloadSender.SpansOnFirstTransaction[0].Tags.Should().Contain("fooSpan1", "barSpan1");
			payloadSender.SpansOnFirstTransaction[0].Context.Tags.Should().Contain("fooSpan1", "barSpan1");

			payloadSender.SpansOnFirstTransaction[0].Tags.Should().Contain("fooSpan2", "barSpan2");
			payloadSender.SpansOnFirstTransaction[0].Context.Tags.Should().Contain("fooSpan2", "barSpan2");
		}
	}
}
