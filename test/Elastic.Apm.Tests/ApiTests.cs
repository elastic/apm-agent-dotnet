using System;
using System.Threading.Tasks;
using Elastic.Apm.Model.Payload;
using Elastic.Apm.Tests.Mock;
using Xunit;

namespace Elastic.Apm.Tests
{
    /// <summary>
    /// Tests the API for manual instrumentation
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

            var transaction = Api.ElasticApm.StartTransaction(transactionName, transacitonType);

            transaction.End();
            Assert.Single(payloadSender.Payloads);
            Assert.Equal(transactionName, payloadSender.Payloads[0].Transactions[0].Name);
            Assert.Equal(transacitonType, payloadSender.Payloads[0].Transactions[0].Type);

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

            var transaction = Api.ElasticApm.StartTransaction(transactionName, transacitonType);
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

            var transaction = Api.ElasticApm.StartTransaction(transactionName, transacitonType);
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
            var currentTransaction = Api.ElasticApm.CurrentTransaction;
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
            var currentTransaction = Api.ElasticApm.CurrentTransaction; //Get transaction in the current task

            Assert.NotNull(currentTransaction);
            Assert.Equal(transactionName, currentTransaction.Name);
            Assert.Equal(Transaction.TYPE_REQUEST, currentTransaction.Type);

            void StartTransaction()
              =>  TransactionContainer.Transactions.Value =
                                  new Transaction(transactionName,
                                  Transaction.TYPE_REQUEST)
                  {
                      Id = Guid.NewGuid(),
                      StartDate = DateTime.UtcNow,
                  };

            async Task DoAsynWork()
            {
                //Make sure we have a transaction in the subtask before the async work
                Assert.NotNull(Api.ElasticApm.CurrentTransaction);
                Assert.Equal(transactionName, Api.ElasticApm.CurrentTransaction.Name);
                Assert.Equal(Transaction.TYPE_REQUEST, Api.ElasticApm.CurrentTransaction.Type);

                await Task.Delay(50);

                //and after the async work
                Assert.NotNull(Api.ElasticApm.CurrentTransaction);
                Assert.Equal(transactionName, Api.ElasticApm.CurrentTransaction.Name);
                Assert.Equal(Transaction.TYPE_REQUEST, Api.ElasticApm.CurrentTransaction.Type);
            }
        }
    }
}
