using System;
using System.Threading.Tasks;
using Elastic.Apm.Model.Payload;
using Elastic.Apm.Tests.Mock;
using Xunit;

namespace Elastic.Apm.Tests
{
    /// <summary>
    /// Tests the API for manual instrumentation.
    /// Only tests scenarios with manually calling StartTransaction, StartSpan, and End.
    /// The convenient API is covered by <see cref="ConvenientApiTests"/>
    /// </summary>
    public class ApiTests
    {
        public ApiTests() => TestHelper.ResetAgentAndEnvVars();

        /// <summary>
        /// Starts and ends a transaction with the public API
        /// and makes sure that the transaction is captured by the agent
        /// </summary>
        [Fact]
        public void StartEndTransaction()
        {
            var transactionName = "TestTransaction";
            var transacitonType = "UnitTest";
            var payloadSender = new MockPayloadSender();
            Agent.PayloadSender = payloadSender;

            var transaction = Agent.GetApi().StartTransaction(transactionName, transacitonType);

            System.Threading.Thread.Sleep(5); //Make sure we have duration > 0

            transaction.End();
            Assert.Single(payloadSender.Payloads);
            Assert.Equal(transactionName, payloadSender.Payloads[0].Transactions[0].Name);
            Assert.Equal(transacitonType, payloadSender.Payloads[0].Transactions[0].Type);
            Assert.True(payloadSender.Payloads[0].Transactions[0].Duration > 0);
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
            var transacitonType = "UnitTest";
            var payloadSender = new MockPayloadSender();
            Agent.PayloadSender = payloadSender;

            var transaction = Agent.GetApi().StartTransaction(transactionName, transacitonType);
            Assert.Empty(payloadSender.Payloads);
        }

        /// <summary>
        /// Starts a transaction, sets its result to 'success' and
        /// makes sure the result is captured by the agent.
        /// </summary>
        [Fact]
        public void TransactionResultTest()
        {
            var transactionName = "TestTransaction";
            var transacitonType = "UnitTest";
            var payloadSender = new MockPayloadSender();
            Agent.PayloadSender = payloadSender;

            var transaction = Agent.GetApi().StartTransaction(transactionName, transacitonType);
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
            var currentTransaction = Agent.GetApi().CurrentTransaction;
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

            StartTransaction(); //Start transaction on the current task
            await DoAsynWork(); //Do work in subtask
            var currentTransaction = Agent.GetApi().CurrentTransaction; //Get transaction in the current task

            Assert.NotNull(currentTransaction);
            Assert.Equal(transactionName, currentTransaction.Name);
            Assert.Equal(Transaction.TYPE_REQUEST, currentTransaction.Type);

            void StartTransaction()
              => TransactionContainer.Transactions.Value =
                        new Transaction(transactionName, Transaction.TYPE_REQUEST);

            async Task DoAsynWork()
            {
                //Make sure we have a transaction in the subtask before the async work
                Assert.NotNull(Agent.GetApi().CurrentTransaction);
                Assert.Equal(transactionName, Agent.GetApi().CurrentTransaction.Name);
                Assert.Equal(Transaction.TYPE_REQUEST, Agent.GetApi().CurrentTransaction.Type);

                await Task.Delay(50);

                //and after the async work
                Assert.NotNull(Agent.GetApi().CurrentTransaction);
                Assert.Equal(transactionName, Agent.GetApi().CurrentTransaction.Name);
                Assert.Equal(Transaction.TYPE_REQUEST, Agent.GetApi().CurrentTransaction.Type);
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
            var transacitonType = "UnitTest";
            var spanName = "TestSpan";
            var payloadSender = new MockPayloadSender();
            Agent.PayloadSender = payloadSender;

            var transaction = Agent.GetApi().StartTransaction(transactionName, transacitonType);

            var span = transaction.StartSpan(spanName, Span.TYPE_EXTERNAL);

            System.Threading.Thread.Sleep(5); //Make sure we have duration > 0

            span.End();
            transaction.End();
            Assert.NotEmpty(payloadSender.Payloads);
            Assert.NotEmpty(payloadSender.Payloads[0].Transactions[0].Spans);

            Assert.Equal(spanName, payloadSender.Payloads[0].Transactions[0].Spans[0].Name);
            Assert.True(payloadSender.Payloads[0].Transactions[0].Spans[0].Duration > 0);
            Assert.True(payloadSender.Payloads[0].Transactions[0].Spans[0].Id > 0);
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
            var transacitonType = "UnitTest";
            var spanName = "TestSpan";
            var payloadSender = new MockPayloadSender();
            Agent.PayloadSender = payloadSender;

            var transaction = Agent.GetApi().StartTransaction(transactionName, transacitonType);

            var span = transaction.StartSpan(spanName, Span.TYPE_EXTERNAL);

            System.Threading.Thread.Sleep(5); //Make sure we have duration > 0

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
            var transacitonType = "UnitTest";
            var spanName = "TestSpan";
            var payloadSender = new MockPayloadSender();
            Agent.PayloadSender = payloadSender;

            var transaction = Agent.GetApi().StartTransaction(transactionName, transacitonType);
            var span = transaction.StartSpan(spanName, Span.TYPE_DB, Span.SUBTYPE_MSSQL, Span.ACTION_QUERY);
            span.End();
            transaction.End();

            Assert.NotEmpty(payloadSender.Payloads);
            Assert.NotEmpty(payloadSender.Payloads[0].Transactions[0].Spans);

            Assert.Equal(Span.TYPE_DB, payloadSender.Payloads[0].Transactions[0].Spans[0].Type);
            Assert.Equal(Span.SUBTYPE_MSSQL, payloadSender.Payloads[0].Transactions[0].Spans[0].Subtype);
            Assert.Equal(Span.ACTION_QUERY, payloadSender.Payloads[0].Transactions[0].Spans[0].Action);

            Assert.NotNull(payloadSender.Payloads[0].Service);
        }

        /// <summary>
        /// Starts and ends a transaction properly and captures an error in between.
        /// Makes sure the error is captured.
        /// </summary>
        [Fact]
        public void ErrorOnTransaction()
        {
            ErrorOnTransactionCommon();
        }

        /// <summary>
        /// Same as <see cref="ErrorOnTransaction"/>, but this time a Culprit is also provided
        /// </summary>
        [Fact]
        public void ErrorOnTransactionWithCulprit()
        {
            ErrorOnTransactionCommon("TestCulprit");
        }

        /// <summary>
        /// Shared between ErrorOnTransaction and ErrorOnTransactionWithCulprit
        /// </summary>
        /// <param name="culprit">Culprit.</param>
        private void ErrorOnTransactionCommon(String culprit = null)
        {
            var transactionName = "TestTransaction";
            var transacitonType = "UnitTest";
            var exceptionMessage = "Foo!";
            var payloadSender = new MockPayloadSender();
            Agent.PayloadSender = payloadSender;

            var transaction = Agent.GetApi().StartTransaction(transactionName, transacitonType);

            System.Threading.Thread.Sleep(5); //Make sure we have duration > 0
            try
            {
                throw new InvalidOperationException(exceptionMessage);
            }
            catch (Exception e)
            {
                if (String.IsNullOrEmpty(culprit))
                {
                    transaction.CaptureException(e);
                }
                else
                {
                    transaction.CaptureException(e, culprit);
                }
            }

            transaction.End();

            Assert.Single(payloadSender.Payloads);
            Assert.Single(payloadSender.Errors);
            Assert.Equal(exceptionMessage, payloadSender.Errors[0].Errors[0].Exception.Message);
            Assert.Equal(exceptionMessage, payloadSender.Errors[0].Errors[0].Exception.Message);

            if (!String.IsNullOrEmpty(culprit))
            {
                Assert.Equal(culprit, payloadSender.Errors[0].Errors[0].Culprit);
            }
            else
            {
                Assert.Equal("PublicAPI-CaptureException", payloadSender.Errors[0].Errors[0].Culprit); //this is the default Culprit if the customer doesn't provide us anything.
            }
        }

        /// <summary>
        /// Starts a transaction with a span, ends the transaction and the span properly and call CaptureException() on the span.
        /// Makes sure the exception is captured.
        /// </summary>
        [Fact]
        public void ErrorOnSpan()
        {
            var transactionName = "TestTransaction";
            var transacitonType = "UnitTest";
            var spanName = "TestSpan";
            var exceptionMessage = "Foo!";
            var payloadSender = new MockPayloadSender();
            Agent.PayloadSender = payloadSender;

            var transaction = Agent.GetApi().StartTransaction(transactionName, transacitonType);

            var span = transaction.StartSpan(spanName, Span.TYPE_EXTERNAL);

            System.Threading.Thread.Sleep(5); //Make sure we have duration > 0

            try
            {
                throw new InvalidOperationException(exceptionMessage);
            }
            catch(Exception e)
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
    }
}
