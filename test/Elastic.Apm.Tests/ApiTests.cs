using System;
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
    }
}
