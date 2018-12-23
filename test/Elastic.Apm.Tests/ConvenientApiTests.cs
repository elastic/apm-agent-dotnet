using System;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Model.Payload;
using Elastic.Apm.Tests.Mock;
using Xunit;

namespace Elastic.Apm.Tests
{
    /// <summary>
    /// Tests the API for manual instrumentation.
    /// Only tests scenarios with when using the convenient API
    /// Scenarios with manually calling <see cref="ElasticApm.StartTransaction"/>,
    /// <see cref="Transaction.StartSpan"/>, <see cref="Transaction.End"/>
    /// are covered by <see cref="ApiTests"/>
    /// </summary>
    public class ConvenientApiTests
    {
        private const int SleepLength = 10;
        private const string TransactionName = "ConvenientApiTest";
        private const string TransactionType = "Test";
        private const string ExceptionMessage = "Foo";

        public ConvenientApiTests()
            => TestHelper.ResetAgentAndEnvVars();

        /// <summary>
        /// Tests the <see cref="ElasticApm.CaptureTransaction(string,string,System.Action)"/> method.
        /// It wraps a fake transaction (Thread.Sleep) into the CaptureTransaction method
        /// and it makes sure that the transaction is captured by the agent.
        /// </summary>
        [Fact]
        public void SimpleAction()
        => AssertWith1Transaction(() =>
           {
            ElasticApm.CaptureTransaction(TransactionName, TransactionType,
                   () =>
                   {
                       Thread.Sleep(SleepLength);
                   });
           });

        /// <summary>
        /// Tests the <see cref="ElasticApm.CaptureTransaction(string,string,System.Action)"/> method with an exception.
        /// It wraps a fake transaction (Thread.Sleep) that throws an exception into the CaptureTransaction method
        /// and it makes sure that the transaction and the exception are captured by the agent.
        /// </summary>
        [Fact]
        public void SimpleActionWithException()
        => AssertWith1TransactionAnd1Error(() =>
           {
               Assert.Throws<InvalidOperationException>(() =>
               {
                ElasticApm.CaptureTransaction(TransactionName, TransactionType, new Action(() =>
                        {
                            Thread.Sleep(SleepLength);
                            throw new InvalidOperationException(ExceptionMessage);
                        }));
               });
           });

        /// <summary>
        /// Tests the <see cref="ElasticApm.CaptureTransaction(string,string,System.Action{Elastic.Apm.Model.Payload.Transaction})"/> method.
        /// It wraps a fake transaction (Thread.Sleep) into the CaptureTransaction method with an <see cref="Action{Transaction}"/> parameter
        /// and it makes sure that the transaction is captured by the agent and the <see cref="Action{Transaction}"/> parameter is not null
        /// </summary>
        [Fact]
        public void SimpleActionWithParameter()
        => AssertWith1Transaction(() =>
        {
            ElasticApm.CaptureTransaction(TransactionName, TransactionType,
                   (t) =>
                   {
                       Assert.NotNull(t);
                       Thread.Sleep(SleepLength);
                   });
        });

        /// <summary>
        /// Tests the <see cref="ElasticApm.CaptureTransaction(string,string,System.Action{Elastic.Apm.Model.Payload.Transaction})"/> method with an exception.
        /// It wraps a fake transaction (Thread.Sleep) that throws an exception into the CaptureTransaction method with an <see cref="Action{Transaction}"/> parameter
        /// and it makes sure that the transaction and the error are captured by the agent and the <see cref="Action{Transaction}"/> parameter is not null
        /// </summary>
        [Fact]
        public void SimpleActionWithExceptionAndParameter()
        => AssertWith1TransactionAnd1Error(() =>
        {
           Assert.Throws<InvalidOperationException>(() =>
           {
                ElasticApm.CaptureTransaction(TransactionName, TransactionType, new Action<Transaction>((t) =>
               {
                    Assert.NotNull(t);
                    Thread.Sleep(SleepLength);
                    throw new InvalidOperationException(ExceptionMessage);
               }));
           });

       });

        /// <summary>
        /// Tests the <see cref="ElasticApm.CaptureTransaction{T}(string,string,System.Func{T})"/> method.
        /// It wraps a fake transaction (Thread.Sleep) with a return value into the CaptureTransaction method
        /// and it makes sure that the transaction is captured by the agent and the return value is correct.
        /// </summary>
        [Fact]
        public void SimpleActionWithReturnType()
        => AssertWith1Transaction(() =>
        {
            var res = ElasticApm.CaptureTransaction(TransactionName, TransactionType, () =>
              {
                  Thread.Sleep(SleepLength);
                  return 42;
              });

           Assert.Equal(42, res);
        });

        /// <summary>
        /// Tests the <see cref="ElasticApm.CaptureTransaction{T}(string,string,System.Func{Transaction,T})"/> method.
        /// It wraps a fake transaction (Thread.Sleep) with a return value into the CaptureTransaction method with an <see cref="Action{Transaction}"/> parameter
        /// and it makes sure that the transaction is captured by the agent and the return value is correct and the <see cref="Action{Transaction}"/> is not null. 
        /// </summary>
        [Fact]
        public void SimpleActionWithReturnTypeAndParameter()
        => AssertWith1Transaction(() =>
            {
                var res = ElasticApm.CaptureTransaction(TransactionName, TransactionType,
                    (t) =>
                    {
                        Assert.NotNull(t);
                        Thread.Sleep(SleepLength);
                        return 42;
                    });

                Assert.Equal(42, res);
            });

        /// <summary>
        /// Tests the <see cref="ElasticApm.CaptureTransaction{T}(string,string,System.Func{Transaction,T})"/> method with an exception.
        /// It wraps a fake transaction (Thread.Sleep) with a return value that throws an exception into the CaptureTransaction method with an <see cref="Action{Transaction}"/> parameter
        /// and it makes sure that the transaction and the error are captured by the agent and the return value is correct and the <see cref="Action{Transaction}"/> is not null. 
        /// </summary>
        [Fact]
        public void SimpleActionWithReturnTypeAndExceptionAndParameter()
        => AssertWith1TransactionAnd1Error(() =>
         {
             Assert.Throws<InvalidOperationException>(() =>
             {
                 var result = ElasticApm.CaptureTransaction(TransactionName, TransactionType, (t) =>
                 {
                     Assert.NotNull(t);
                     Thread.Sleep(SleepLength);

                     if (new Random().Next(1) == 0)//avoid unreachable code warning.
                     {
                         throw new InvalidOperationException(ExceptionMessage);
                     }

                     return 42;
                 });

                 Assert.True(false); //Should not be executed because the agent isn't allowed to catch an exception.
                 Assert.Equal(42, result); //But if it'd not throw it'd be 42.
             });
         });

        /// <summary>
        /// Tests the <see cref="ElasticApm.CaptureTransaction{T}(string,string,System.Func{T})"/> method with an exception.
        /// It wraps a fake transaction (Thread.Sleep) with a return value that throws an exception into the CaptureTransaction method
        /// and it makes sure that the transaction and the error are captured by the agent. 
        /// </summary>
        [Fact]
        public void SimpleActionWithReturnTypeAndException()
         =>  AssertWith1TransactionAnd1Error(() =>
           {
               Assert.Throws<InvalidOperationException>(() =>
               {
                   var result = ElasticApm.CaptureTransaction(TransactionName, TransactionType, () =>
                   {
                       Thread.Sleep(SleepLength);

                       if (new Random().Next(1) == 0)//avoid unreachable code warning.
                       {
                           throw new InvalidOperationException(ExceptionMessage);
                       }

                       return 42;
                   });

                   Assert.True(false); //Should not be executed because the agent isn't allowed to catch an exception.
                   Assert.Equal(42, result); //But if it'd not throw it'd be 42.
               });
           });

        /// <summary>
        /// Tests the <see cref="ElasticApm.CaptureTransaction(string,string,System.Func{Task})"/> method.
        /// It wraps a fake async transaction (Task.Delay) into the CaptureTransaction method
        /// and it makes sure that the transaction is captured.
        /// </summary>
        [Fact]
        public async Task AsyncTask()
        {
            await AssertWith1TransactionAsync(async () =>
            {
                await ElasticApm.CaptureTransaction(TransactionName, TransactionType,
                    async () => { await Task.Delay(SleepLength); });
            });
        }
        
        /// <summary>
        /// Tests the <see cref="ElasticApm.CaptureTransaction(string,string,System.Func{Task})"/> method with an exception
        /// It wraps a fake async transaction (Task.Delay) that throws an exception into the CaptureTransaction method
        /// and it makes sure that the transaction and the error are captured.
        /// </summary>
        [Fact]
        public async Task AsyncTaskWithException()
            => await AssertWith1TransactionAnd1ErrorAsync(async() =>
            {
                await Assert.ThrowsAsync<InvalidOperationException>(async() =>
                {
                    await ElasticApm.CaptureTransaction(TransactionName, TransactionType, async () =>
                    {
                        await Task.Delay(SleepLength);
                        throw new InvalidOperationException(ExceptionMessage);
                    });
                });
            });

        /// <summary>
        /// Tests the <see cref="ElasticApm.CaptureTransaction(string,string,System.Func{Transaction, Task})"/> method.
        /// It wraps a fake async transaction (Task.Delay) into the CaptureTransaction method with an <see cref="Action{Transaction}"/> parameter
        /// and it makes sure that the transaction is captured and the <see cref="Action{Transaction}"/> parameter is not null.
        /// </summary>
        [Fact]
        public async Task AsyncTaskWithParameter()
            => await AssertWith1TransactionAsync(async () =>
            {
                await ElasticApm.CaptureTransaction(TransactionName, TransactionType,
                    async(t) =>
                    {
                        Assert.NotNull(t);
                        await Task.Delay(SleepLength);
                    });
            });
        
        /// <summary>
        /// Tests the <see cref="ElasticApm.CaptureTransaction(string,string,System.Func{Transaction, Task})"/> method with an exception.
        /// It wraps a fake async transaction (Task.Delay) that throws an exception into the CaptureTransaction method with an <see cref="Action{Transaction}"/> parameter
        /// and it makes sure that the transaction and the error are captured and the <see cref="Action{Transaction}"/> parameter is not null.
        /// </summary>
        [Fact]
        public async Task AsyncTaskWithExceptionAndParameter()
            => await AssertWith1TransactionAnd1ErrorAsync(async () =>
            {
                await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                {
                   await ElasticApm.CaptureTransaction(TransactionName, TransactionType, async (t) =>
                    {
                        Assert.NotNull(t);
                        await Task.Delay(SleepLength);
                        throw new InvalidOperationException(ExceptionMessage);
                    });
                });

            });
        
        /// <summary>
        /// Tests the <see cref="ElasticApm.CaptureTransaction{T}(string,string,System.Func{Task{T}})"/> method.
        /// It wraps a fake async transaction (Task.Delay) with a return value into the CaptureTransaction method
        /// and it makes sure that the transaction is captured by the agent and the return value is correct.
        /// </summary>
        [Fact]
        public async Task AsyncTaskWithReturnType()
            => await AssertWith1TransactionAsync(async () =>
            {
                var res = await ElasticApm.CaptureTransaction(TransactionName, TransactionType, async () =>
                {
                    await Task.Delay(SleepLength);
                    return 42;
                });
                Assert.Equal(42, res);
            });
        
        /// <summary>
        /// Tests the <see cref="ElasticApm.CaptureTransaction{T}(string,string,System.Func{Transaction,Task{T}})"/> method.
        /// It wraps a fake async transaction (Task.Delay) with a return value into the CaptureTransaction method with an <see cref="Action{Transaction}"/> parameter
        /// and it makes sure that the transaction is captured by the agent and the return value is correct and the <see cref="Action{Transaction}"/> is not null. 
        /// </summary>
        [Fact]
        public async Task AsyncTaskWithReturnTypeAndParameter()
        => await AssertWith1TransactionAsync(async () =>
            {
                var res =  await ElasticApm.CaptureTransaction(TransactionName, TransactionType,
                    async (t) =>
                    {
                        Assert.NotNull(t);
                        await Task.Delay(SleepLength);
                        return 42;
                    });

                Assert.Equal(42, res);
            });

        /// <summary>
        /// Tests the <see cref="ElasticApm.CaptureTransaction{T}(string,string,System.Func{Transaction,Task{T}})"/> method with an exception.
        /// It wraps a fake async transaction (Task.Delay) with a return value that throws an exception into the CaptureTransaction method with an <see cref="Action{Transaction}"/> parameter
        /// and it makes sure that the transaction and the error are captured by the agent and the return value is correct and the <see cref="Action{Transaction}"/> is not null. 
        /// </summary>
        [Fact]
        public async Task AsyncTaskWithReturnTypeAndExceptionAndParameter()
        => await AssertWith1TransactionAnd1ErrorAsync(async () =>
         {
             await Assert.ThrowsAsync<InvalidOperationException>(async () =>
             {
                 var result = await ElasticApm.CaptureTransaction(TransactionName, TransactionType, async (t) =>
                 {
                     Assert.NotNull(t);
                     await Task.Delay(SleepLength);

                     if (new Random().Next(1) == 0)//avoid unreachable code warning.
                     {
                         throw new InvalidOperationException(ExceptionMessage);
                     }

                     return 42;
                 });

                 Assert.True(false); //Should not be executed because the agent isn't allowed to catch an exception.
                 Assert.Equal(42, result); //But if it'd not throw it'd be 42.
             });
         });

        /// <summary>
        /// Tests the <see cref="ElasticApm.CaptureTransaction{T}(string,string,System.Func{Task{T}})"/> method with an exception.
        /// It wraps a fake async transaction (Task.Delay) with a return value that throws an exception into the CaptureTransaction method
        /// and it makes sure that the transaction and the error are captured by the agent. 
        /// </summary>
        [Fact]
        public async Task AsyncTaskWithReturnTypeAndException()
         => await AssertWith1TransactionAnd1ErrorAsync(async () =>
           {
               await Assert.ThrowsAsync<InvalidOperationException>(async () =>
               {
                   var result = await ElasticApm.CaptureTransaction(TransactionName, TransactionType, async () =>
                   {
                       await Task.Delay(SleepLength);

                       if (new Random().Next(1) == 0)//avoid unreachable code warning.
                       {
                           throw new InvalidOperationException(ExceptionMessage);
                       }

                       return 42;
                   });

                   Assert.True(false); //Should not be executed because the agent isn't allowed to catch an exception.
                   Assert.Equal(42, result); //But if it'd not throw it'd be 42.
               });
           });

        [Fact]
        public async Task CancelledAsyncTask()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = cancellationTokenSource.Token;
            cancellationTokenSource.Cancel();
            
            await ElasticApm.CaptureTransaction(TransactionName, TransactionType,
                    async () =>
                    {
                        await Task.Delay(SleepLength, token);
                        token.ThrowIfCancellationRequested();
                    });
        }
        
        /// <summary>
        /// Asserts on 1 transaction with async code 
        /// </summary>
        private async Task AssertWith1TransactionAsync(Func<Task> func)
        {
            var payloadSender = new MockPayloadSender();
            Agent.PayloadSender = payloadSender;

            await func();

            Assert.NotEmpty(payloadSender.Payloads);
            Assert.NotEmpty(payloadSender.Payloads[0].Transactions);

            Assert.Equal(TransactionName, payloadSender.Payloads[0].Transactions[0].Name);
            Assert.Equal(TransactionType, payloadSender.Payloads[0].Transactions[0].Type);

            Assert.True(payloadSender.Payloads[0].Transactions[0].Duration >= SleepLength);
        }

        /// <summary>
        /// Asserts on 1 async transaction and 1 error
        /// </summary>
        private async Task AssertWith1TransactionAnd1ErrorAsync(Func<Task> func)
        {

            var payloadSender = new MockPayloadSender();
            Agent.PayloadSender = payloadSender;

            await func();

            Assert.NotEmpty(payloadSender.Payloads);
            Assert.NotEmpty(payloadSender.Payloads[0].Transactions);

            Assert.Equal(TransactionName, payloadSender.Payloads[0].Transactions[0].Name);
            Assert.Equal(TransactionType, payloadSender.Payloads[0].Transactions[0].Type);

            Assert.True(payloadSender.Payloads[0].Transactions[0].Duration >= SleepLength);


            Assert.NotEmpty(payloadSender.Errors);
            Assert.NotEmpty(payloadSender.Errors[0].Errors);

            Assert.Equal(typeof(InvalidOperationException).FullName, payloadSender.Errors[0].Errors[0].Exception.Type);
            Assert.Equal(ExceptionMessage, payloadSender.Errors[0].Errors[0].Exception.Message);
        }

        /// <summary>
        /// Asserts on 1 transaction
        /// </summary>
        private void AssertWith1Transaction(Action action)
        {
            var payloadSender = new MockPayloadSender();
            Agent.PayloadSender = payloadSender;

            action();

            Assert.NotEmpty(payloadSender.Payloads);
            Assert.NotEmpty(payloadSender.Payloads[0].Transactions);

            Assert.Equal(TransactionName, payloadSender.Payloads[0].Transactions[0].Name);
            Assert.Equal(TransactionType, payloadSender.Payloads[0].Transactions[0].Type);

            Assert.True(payloadSender.Payloads[0].Transactions[0].Duration >= SleepLength);
        }

        /// <summary>
        /// Asserts on 1 transaction and 1 error
        /// </summary>
        private void AssertWith1TransactionAnd1Error(Action action)
        {
            var payloadSender = new MockPayloadSender();
            Agent.PayloadSender = payloadSender;

            action();

            Assert.NotEmpty(payloadSender.Payloads);
            Assert.NotEmpty(payloadSender.Payloads[0].Transactions);

            Assert.Equal(TransactionName, payloadSender.Payloads[0].Transactions[0].Name);
            Assert.Equal(TransactionType, payloadSender.Payloads[0].Transactions[0].Type);

            Assert.True(payloadSender.Payloads[0].Transactions[0].Duration >= SleepLength);


            Assert.NotEmpty(payloadSender.Errors);
            Assert.NotEmpty(payloadSender.Errors[0].Errors);

            Assert.Equal(typeof(InvalidOperationException).FullName, payloadSender.Errors[0].Errors[0].Exception.Type);
            Assert.Equal(ExceptionMessage, payloadSender.Errors[0].Errors[0].Exception.Message);
        }
    }
}