using System;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Model.Payload;
using Elastic.Apm.Tests.Mock;
using Xunit;

namespace Elastic.Apm.Tests
{
    public class ConvenientApiSpanTests
    {
        private const int TransactionSleepLength = 10;
        private const int SpanSleepLength = 20;
        private const string TransactionName = "ConvenientApiTest";
        private const string TransactionType = "Test";
        private const string ExceptionMessage = "Foo";
        private const string SpanName = "TestSpan";
        private const string SpanType = "TestSpan";

        public ConvenientApiSpanTests()
            => TestHelper.ResetAgentAndEnvVars();

        /// <summary>
        /// Tests the <see cref="Transaction.CaptureSpan(string,string,System.Action,string,string)"/> method.
        /// It wraps a fake span (Thread.Sleep) into the CaptureSpan method
        /// and it makes sure that the span is captured by the agent.
        /// </summary>
        [Fact]
        public void SimpleAction()
            => AssertWith1TransactionAnd1Span((t) =>
            {
             
                        t.CaptureSpan(SpanName, SpanType, () =>
                        {
                            Thread.Sleep(SpanSleepLength);
                        });
                   
            });
        
        [Fact]
        public void SimpleActionWithException()
        => AssertWith1TransactionAnd1SpanAnd1Error((t) =>
            {
                    Assert.Throws<InvalidOperationException>(() =>
                    {
                        t.CaptureSpan(SpanName, SpanType, new Action(() =>
                        {
                            Thread.Sleep(SpanSleepLength);
                            throw new InvalidOperationException(ExceptionMessage);
                        }));
                    });
            });
        
        //TODO: Fix comments from here 
        
        /// <summary>
        /// Tests the <see cref="ElasticApm.CaptureTransaction(string,string,System.Action{ITransaction})"/> method.
        /// It wraps a fake transaction (Thread.Sleep) into the CaptureTransaction method with an <see cref="Action{ITransaction}"/> parameter
        /// and it makes sure that the transaction is captured by the agent and the <see cref="Action{ITransaction}"/> parameter is not null
        /// </summary>
        [Fact]
        public void SimpleActionWithParameter()
        => AssertWith1TransactionAnd1Span((t) =>
        {
                t.CaptureSpan(SpanName, SpanType,
                    (s) =>
                    {
                        Assert.NotNull(s);
                        Thread.Sleep(SpanSleepLength);
                    });
        });

        ///// <summary>
        ///// Tests the <see cref="ElasticApm.CaptureTransaction(string,string,System.Action{ITransaction})"/> method with an exception.
        ///// It wraps a fake transaction (Thread.Sleep) that throws an exception into the CaptureTransaction method with an <see cref="Action{ITransaction}"/> parameter
        ///// and it makes sure that the transaction and the error are captured by the agent and the <see cref="Action{ITransaction}"/> parameter is not null
        ///// </summary>
        [Fact]
        public void SimpleActionWithExceptionAndParameter()
        => AssertWith1TransactionAnd1SpanAnd1Error((t) =>
        {
                Assert.Throws<InvalidOperationException>(() =>
                {
                    t.CaptureSpan(SpanName, SpanType, new Action<ISpan>((s) =>
                    {
                        Assert.NotNull(s);
                        Thread.Sleep(SpanSleepLength);
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
        => AssertWith1TransactionAnd1Span((t) =>
        {        
            var res = t.CaptureSpan(SpanName, SpanType, () =>
            {
                Thread.Sleep(SpanSleepLength);
                return 42;
            });

           Assert.Equal(42, res);
        });

        /// <summary>
        /// Tests the <see cref="ElasticApm.CaptureTransaction{T}(string,string,System.Func{ITransaction,T})"/> method.
        /// It wraps a fake transaction (Thread.Sleep) with a return value into the CaptureTransaction method with an <see cref="Action{ITransaction}"/> parameter
        /// and it makes sure that the transaction is captured by the agent and the return value is correct and the <see cref="Action{ITransaction}"/> is not null. 
        /// </summary>
        [Fact]
        public void SimpleActionWithReturnTypeAndParameter()
        => AssertWith1TransactionAnd1Span((t) =>
            {
                var res = t.CaptureSpan(SpanName, SpanType, (s) =>
                    {
                        Assert.NotNull(t);
                        Thread.Sleep(SpanSleepLength);
                        return 42;
                    });

                Assert.Equal(42, res);
            });

        /// <summary>
        /// Tests the <see cref="ElasticApm.CaptureTransaction{T}(string,string,System.Func{ITransaction,T})"/> method with an exception.
        /// It wraps a fake transaction (Thread.Sleep) with a return value that throws an exception into the CaptureTransaction method with an <see cref="Action{ITransaction}"/> parameter
        /// and it makes sure that the transaction and the error are captured by the agent and the return value is correct and the <see cref="Action{ITransaction}"/> is not null. 
        /// </summary>
        [Fact]
        public void SimpleActionWithReturnTypeAndExceptionAndParameter()
        => AssertWith1TransactionAnd1SpanAnd1Error((t) =>
         {
             Assert.Throws<InvalidOperationException>(() =>
             {
                var result = t.CaptureSpan(SpanName, SpanType, (s) =>
                {
                    Assert.NotNull(s);
                    Thread.Sleep(SpanSleepLength);

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

        ///// <summary>
        ///// Tests the <see cref="ElasticApm.CaptureTransaction{T}(string,string,System.Func{T})"/> method with an exception.
        ///// It wraps a fake transaction (Thread.Sleep) with a return value that throws an exception into the CaptureTransaction method
        ///// and it makes sure that the transaction and the error are captured by the agent. 
        ///// </summary>
        [Fact]
        public void SimpleActionWithReturnTypeAndException()
         =>  AssertWith1TransactionAnd1SpanAnd1Error((t) =>
           {
               Assert.Throws<InvalidOperationException>(() =>
               {
                   var result = t.CaptureSpan(SpanName, SpanType, () =>
                   {
                       Thread.Sleep(SpanSleepLength);

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

        ///// <summary>
        ///// Tests the <see cref="ElasticApm.CaptureTransaction(string,string,System.Func{Task})"/> method.
        ///// It wraps a fake async transaction (Task.Delay) into the CaptureTransaction method
        ///// and it makes sure that the transaction is captured.
        ///// </summary>
        [Fact]
        public async Task AsyncTask()
         => await AssertWith1TransactionAnd1SpanAsync(async (t) =>
            {
                await t.CaptureSpan(SpanName, SpanType, async () => 
                { 
                    await Task.Delay(SpanSleepLength); 
                });
            });
        //}

        ///// <summary>
        ///// Tests the <see cref="ElasticApm.CaptureTransaction(string,string,System.Func{Task})"/> method with an exception
        ///// It wraps a fake async transaction (Task.Delay) that throws an exception into the CaptureTransaction method
        ///// and it makes sure that the transaction and the error are captured.
        ///// </summary>
        [Fact]
        public async Task AsyncTaskWithException()
        => await AssertWith1TransactionAnd1ErrorAnd1SpanAsync(async(t) =>
            {
                await Assert.ThrowsAsync<InvalidOperationException>(async() =>
                {
                    await t.CaptureSpan(SpanName, SpanType, async () =>
                    {
                    await Task.Delay(SpanSleepLength);
                        throw new InvalidOperationException(ExceptionMessage);
                    });
                });
            });

        ///// <summary>
        ///// Tests the <see cref="ElasticApm.CaptureTransaction(string,string,System.Func{ITransaction, Task})"/> method.
        ///// It wraps a fake async transaction (Task.Delay) into the CaptureTransaction method with an <see cref="Action{ITransaction}"/> parameter
        ///// and it makes sure that the transaction is captured and the <see cref="Action{ITransaction}"/> parameter is not null.
        ///// </summary>
        [Fact]
        public async Task AsyncTaskWithParameter()
        => await AssertWith1TransactionAnd1SpanAsync(async (t) =>
            {
                await t.CaptureSpan(SpanName, SpanType,
                    async(s) =>
                    {
                        Assert.NotNull(s);
                        await Task.Delay(SpanSleepLength);
                    });
            });

        ///// <summary>
        ///// Tests the <see cref="ElasticApm.CaptureTransaction(string,string,System.Func{ITransaction, Task})"/> method with an exception.
        ///// It wraps a fake async transaction (Task.Delay) that throws an exception into the CaptureTransaction method with an <see cref="Action{ITransaction}"/> parameter
        ///// and it makes sure that the transaction and the error are captured and the <see cref="Action{ITransaction}"/> parameter is not null.
        ///// </summary>
        [Fact]
        public async Task AsyncTaskWithExceptionAndParameter()
            => await AssertWith1TransactionAnd1ErrorAnd1SpanAsync(async (t) =>
            {
                await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                {
                   await t.CaptureSpan(SpanName, SpanType, async (s) =>
                    {
                        Assert.NotNull(s);
                    await Task.Delay(SpanSleepLength);
                        throw new InvalidOperationException(ExceptionMessage);
                    });
                });

            });

        ///// <summary>
        ///// Tests the <see cref="ElasticApm.CaptureTransaction{T}(string,string,System.Func{Task{T}})"/> method.
        ///// It wraps a fake async transaction (Task.Delay) with a return value into the CaptureTransaction method
        ///// and it makes sure that the transaction is captured by the agent and the return value is correct.
        ///// </summary>
        [Fact]
        public async Task AsyncTaskWithReturnType()
        => await AssertWith1TransactionAnd1SpanAsync(async (t) =>
            {
                var res = await t.CaptureSpan(SpanName, SpanType, async () =>
                {
                    await Task.Delay(SpanSleepLength);
                    return 42;
                });
                Assert.Equal(42, res);
            });

        ///// <summary>
        ///// Tests the <see cref="ElasticApm.CaptureTransaction{T}(string,string,System.Func{ITransaction,Task{T}})"/> method.
        ///// It wraps a fake async transaction (Task.Delay) with a return value into the CaptureTransaction method with an <see cref="Action{ITransaction}"/> parameter
        ///// and it makes sure that the transaction is captured by the agent and the return value is correct and the <see cref="Action{IElasticApmTransaction}"/> is not null. 
        ///// </summary>
        [Fact]
        public async Task AsyncTaskWithReturnTypeAndParameter()
        => await AssertWith1TransactionAnd1SpanAsync(async (t) =>
            {
                var res =  await t.CaptureSpan(SpanName, SpanType,
                    async (s) =>
                    {
                        Assert.NotNull(s);
                        await Task.Delay(SpanSleepLength);
                        return 42;
                    });

                Assert.Equal(42, res);
            });

        ///// <summary>
        ///// Tests the <see cref="ElasticApm.CaptureTransaction{T}(string,string,System.Func{ITransaction,Task{T}})"/> method with an exception.
        ///// It wraps a fake async transaction (Task.Delay) with a return value that throws an exception into the CaptureTransaction method with an <see cref="Action{ITransaction}"/> parameter
        ///// and it makes sure that the transaction and the error are captured by the agent and the return value is correct and the <see cref="Action{ITransaction}"/> is not null. 
        ///// </summary>
        [Fact]
        public async Task AsyncTaskWithReturnTypeAndExceptionAndParameter()
        => await AssertWith1TransactionAnd1ErrorAnd1SpanAsync(async (t) =>
         {
             await Assert.ThrowsAsync<InvalidOperationException>(async () =>
             {
                 var result = await t.CaptureSpan(SpanName, SpanType, async (s) =>
                 {
                    Assert.NotNull(s);
                    await Task.Delay(SpanSleepLength);

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

        ///// <summary>
        ///// Tests the <see cref="ElasticApm.CaptureTransaction{T}(string,string,System.Func{Task{T}})"/> method with an exception.
        ///// It wraps a fake async transaction (Task.Delay) with a return value that throws an exception into the CaptureTransaction method
        ///// and it makes sure that the transaction and the error are captured by the agent. 
        ///// </summary>
        [Fact]
        public async Task AsyncTaskWithReturnTypeAndException()
        => await AssertWith1TransactionAnd1ErrorAnd1SpanAsync(async (t) =>
           {
               await Assert.ThrowsAsync<InvalidOperationException>(async () =>
               {
                   var result = await t.CaptureSpan(SpanName, SpanType, async () =>
                   {
                       await Task.Delay(SpanSleepLength);

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
            var payloadSender = new MockPayloadSender();
            Agent.PayloadSender = payloadSender;

            var cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;
            cancellationTokenSource.Cancel();

            await Agent.GetApi().CaptureTransaction(TransactionName, TransactionType, async (t) =>
            {
                await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                {
                    await t.CaptureSpan(SpanName, SpanType, async () =>
                    {
                        // ReSharper disable once MethodSupportsCancellation, we want to delay before we throw the exception
                        await Task.Delay(SpanSleepLength);
                        token.ThrowIfCancellationRequested();
                    });
                });
            });
        }

        /// <summary>
        /// Asserts on 1 async transaction and 1 error
        /// </summary>
        private async Task AssertWith1TransactionAnd1ErrorAnd1SpanAsync(Func<ITransaction, Task> func)
        {

            var payloadSender = new MockPayloadSender();
            Agent.PayloadSender = payloadSender;

            await Agent.GetApi().CaptureTransaction(TransactionName, TransactionType, async (t) =>
            {
                await Task.Delay(TransactionSleepLength);
                await func(t);
            });

            Assert.NotEmpty(payloadSender.Payloads);
            Assert.NotEmpty(payloadSender.Payloads[0].Transactions);

            Assert.Equal(TransactionName, payloadSender.Payloads[0].Transactions[0].Name);
            Assert.Equal(TransactionType, payloadSender.Payloads[0].Transactions[0].Type);

            Assert.True(payloadSender.Payloads[0].Transactions[0].Duration >= TransactionSleepLength + SpanSleepLength);

            Assert.NotEmpty(payloadSender.Payloads[0].Transactions[0].Spans);

            Assert.Equal(SpanName, payloadSender.Payloads[0].Transactions[0].Spans[0].Name);
            Assert.Equal(SpanType, payloadSender.Payloads[0].Transactions[0].Spans[0].Type);


            Assert.NotEmpty(payloadSender.Errors);
            Assert.NotEmpty(payloadSender.Errors[0].Errors);

            Assert.Equal(typeof(InvalidOperationException).FullName, payloadSender.Errors[0].Errors[0].Exception.Type);
            Assert.Equal(ExceptionMessage, payloadSender.Errors[0].Errors[0].Exception.Message);
        }


        /// <summary>
        /// Asserts on 1 transaction with 1 async Span 
        /// </summary>
        private async Task AssertWith1TransactionAnd1SpanAsync(Func<ITransaction, Task> func)
            {
                var payloadSender = new MockPayloadSender();
                Agent.PayloadSender = payloadSender;

                await Agent.GetApi().CaptureTransaction(TransactionName, TransactionType, async(t) =>
                {
                    await Task.Delay(TransactionSleepLength);
                    await func(t);
                });

                Assert.NotEmpty(payloadSender.Payloads);
                Assert.NotEmpty(payloadSender.Payloads[0].Transactions);

                Assert.Equal(TransactionName, payloadSender.Payloads[0].Transactions[0].Name);
                Assert.Equal(TransactionType, payloadSender.Payloads[0].Transactions[0].Type);

                Assert.True(payloadSender.Payloads[0].Transactions[0].Duration >= TransactionSleepLength + SpanSleepLength);

                Assert.NotEmpty(payloadSender.Payloads[0].Transactions[0].Spans);

                Assert.Equal(SpanName, payloadSender.Payloads[0].Transactions[0].Spans[0].Name);
                Assert.Equal(SpanType, payloadSender.Payloads[0].Transactions[0].Spans[0].Type);
        }

        /// <summary>
        /// Asserts on 1 transaction with 1 span
        /// </summary>
        private void AssertWith1TransactionAnd1Span(Action<ITransaction> action)
        {
            var payloadSender = new MockPayloadSender();
            Agent.PayloadSender = payloadSender;

            Agent.GetApi().CaptureTransaction(TransactionName, TransactionType, (t) =>
            {
                Thread.Sleep(SpanSleepLength);
                action(t);
            });

            Assert.NotEmpty(payloadSender.Payloads);
            Assert.NotEmpty(payloadSender.Payloads[0].Transactions);

            Assert.Equal(TransactionName, payloadSender.Payloads[0].Transactions[0].Name);
            Assert.Equal(TransactionType, payloadSender.Payloads[0].Transactions[0].Type);
            
            Assert.NotEmpty(payloadSender.Payloads[0].Transactions[0].Spans);
            
            Assert.Equal(SpanName, payloadSender.Payloads[0].Transactions[0].Spans[0].Name);
            Assert.Equal(SpanType, payloadSender.Payloads[0].Transactions[0].Spans[0].Type);

            Assert.True(payloadSender.Payloads[0].Transactions[0].Duration >= TransactionSleepLength + SpanSleepLength);
        }


        /// <summary>
        /// Asserts on 1 transaction and 1 error
        /// </summary>
        private void AssertWith1TransactionAnd1SpanAnd1Error(Action<ITransaction> action)
        {
            var payloadSender = new MockPayloadSender();
            Agent.PayloadSender = payloadSender;

            Agent.GetApi().CaptureTransaction(TransactionName, TransactionType, (t) =>
            {

                Thread.Sleep(SpanSleepLength);
                action(t);
            });

            Assert.NotEmpty(payloadSender.Payloads);
            Assert.NotEmpty(payloadSender.Payloads[0].Transactions);

            Assert.Equal(TransactionName, payloadSender.Payloads[0].Transactions[0].Name);
            Assert.Equal(TransactionType, payloadSender.Payloads[0].Transactions[0].Type);

            Assert.True(payloadSender.Payloads[0].Transactions[0].Duration >= TransactionSleepLength + SpanSleepLength);
            
            Assert.NotEmpty(payloadSender.Payloads[0].Transactions[0].Spans);

            Assert.Equal(SpanName, payloadSender.Payloads[0].Transactions[0].Spans[0].Name);
            Assert.Equal(SpanType, payloadSender.Payloads[0].Transactions[0].Spans[0].Type);

            Assert.NotEmpty(payloadSender.Errors);
            Assert.NotEmpty(payloadSender.Errors[0].Errors);

            Assert.Equal(typeof(InvalidOperationException).FullName, payloadSender.Errors[0].Errors[0].Exception.Type);
            Assert.Equal(ExceptionMessage, payloadSender.Errors[0].Errors[0].Exception.Message);
        }
    }
}