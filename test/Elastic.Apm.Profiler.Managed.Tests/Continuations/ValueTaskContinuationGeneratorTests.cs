// Licensed to Elasticsearch B.V under the Apache 2.0 License.
// Elasticsearch B.V licenses this file, including any modifications, to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.
//
// <copyright file="ValueTaskContinuationGeneratorTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#if NETCOREAPP3_1_OR_GREATER
using System;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Profiler.Managed.CallTarget;
using Elastic.Apm.Profiler.Managed.CallTarget.Handlers.Continuations;
using Xunit;

namespace Elastic.Apm.Profiler.Managed.Tests.Continuations
{
    public class ValueTaskContinuationGeneratorTests
    {
        public static TReturn OnAsyncMethodEnd<TTarget, TReturn>(TTarget instance, TReturn returnValue, Exception exception, CallTargetState state) =>
			returnValue;

		[Fact]
        public async ValueTask SuccessTest()
        {
            var tcg = new ValueTaskContinuationGenerator<ValueTaskContinuationGeneratorTests, ValueTaskContinuationGeneratorTests, ValueTask>();
            var cTask = tcg.SetContinuation(this, GetPreviousTask(), null, CallTargetState.GetDefault());

            await cTask;

            async ValueTask GetPreviousTask()
            {
                await Task.Delay(1000).ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task ExceptionTest()
        {
            Exception ex = null;

            // Normal
            ex = await Assert.ThrowsAsync<CustomException>(() => GetPreviousTask().AsTask());
            Assert.Equal("Internal Test Exception", ex.Message);

            // Using the continuation
            var tcg = new ValueTaskContinuationGenerator<ValueTaskContinuationGeneratorTests, ValueTaskContinuationGeneratorTests, ValueTask>();
            ex = await Assert.ThrowsAsync<CustomException>(() => tcg.SetContinuation(this, GetPreviousTask(), null, CallTargetState.GetDefault()).AsTask());
            Assert.Equal("Internal Test Exception", ex.Message);

            async ValueTask GetPreviousTask()
            {
                await Task.Delay(1000).ConfigureAwait(false);
                throw new CustomException("Internal Test Exception");
            }
        }

        [Fact]
        public async Task CancelledTest()
        {
            // Normal
            var task = GetPreviousTask();
            await Assert.ThrowsAsync<CustomCancellationException>(() => task.AsTask());
            Assert.True(task.IsCanceled);

            // Using the continuation
            task = GetPreviousTask();
            var tcg = new ValueTaskContinuationGenerator<ValueTaskContinuationGeneratorTests, ValueTaskContinuationGeneratorTests, ValueTask>();
            await Assert.ThrowsAsync<CustomCancellationException>(() => tcg.SetContinuation(this, task, null, CallTargetState.GetDefault()).AsTask());
            Assert.True(task.IsCanceled);

            ValueTask GetPreviousTask()
            {
                var cts = new CancellationTokenSource();

                var task = Task.FromResult(true).ContinueWith(
                    _ =>
                    {
                        cts.Cancel();
                        throw new CustomCancellationException(cts.Token);
                    },
                    cts.Token);

                return new ValueTask(task);
            }
        }

        [Fact]
        public async Task SuccessGenericTest()
        {
            var tcg = new ValueTaskContinuationGenerator<ValueTaskContinuationGeneratorTests, ValueTaskContinuationGeneratorTests, ValueTask<bool>, bool>();
            var cTask = tcg.SetContinuation(this, GetPreviousTask(), null, CallTargetState.GetDefault());

            await cTask;

            async ValueTask<bool> GetPreviousTask()
            {
                await Task.Delay(1000).ConfigureAwait(false);
                return true;
            }
        }

        [Fact]
        public async Task ExceptionGenericTest()
        {
            Exception ex = null;

            // Normal
            ex = await Assert.ThrowsAsync<CustomException>(() => GetPreviousTask().AsTask());
            Assert.Equal("Internal Test Exception", ex.Message);

            // Using the continuation
            var tcg = new ValueTaskContinuationGenerator<ValueTaskContinuationGeneratorTests, ValueTaskContinuationGeneratorTests, ValueTask<bool>, bool>();
            ex = await Assert.ThrowsAsync<CustomException>(() => tcg.SetContinuation(this, GetPreviousTask(), null, CallTargetState.GetDefault()).AsTask());
            Assert.Equal("Internal Test Exception", ex.Message);

            async ValueTask<bool> GetPreviousTask()
            {
                await Task.Delay(1000).ConfigureAwait(false);
                throw new CustomException("Internal Test Exception");
            }
        }

        [Fact]
        public async Task CancelledGenericTest()
        {
            // Normal
            var task = GetPreviousTask();
            await Assert.ThrowsAsync<CustomCancellationException>(() => task.AsTask());
            Assert.True(task.IsCanceled);

            // Using the continuation
            task = GetPreviousTask();
            var tcg = new ValueTaskContinuationGenerator<ValueTaskContinuationGeneratorTests, ValueTaskContinuationGeneratorTests, ValueTask<bool>, bool>();
            await Assert.ThrowsAsync<CustomCancellationException>(() => tcg.SetContinuation(this, task, null, CallTargetState.GetDefault()).AsTask());
            Assert.True(task.IsCanceled);

            ValueTask<bool> GetPreviousTask()
            {
                var cts = new CancellationTokenSource();

                var task = Task.FromResult(true).ContinueWith<bool>(
                    _ =>
                    {
                        cts.Cancel();
                        throw new CustomCancellationException(cts.Token);
                    },
                    cts.Token);

                return new ValueTask<bool>(task);
            }
        }

        internal class CustomException : Exception
        {
            public CustomException(string message)
                : base(message)
            {
            }
        }

        internal class CustomCancellationException : OperationCanceledException
        {
            public CustomCancellationException(CancellationToken token)
                : base(token)
            {
            }
        }
    }
}
#endif
