// Licensed to Elasticsearch B.V under the Apache 2.0 License.
// Elasticsearch B.V licenses this file, including any modifications, to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.
//
// <copyright file="TaskContinuationGeneratorTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Profiler.Managed.CallTarget;
using Elastic.Apm.Profiler.Managed.CallTarget.Handlers.Continuations;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Profiler.Managed.Tests.Continuations
{
	public class TaskContinuationGeneratorTests
    {
        public static TReturn OnAsyncMethodEnd<TTarget, TReturn>(TTarget instance, TReturn returnValue, Exception exception, CallTargetState state) =>
			returnValue;

		[Fact]
        public async Task SuccessTest()
        {
            var tcg = new TaskContinuationGenerator<TaskContinuationGeneratorTests, TaskContinuationGeneratorTests, Task>();
            var cTask = tcg.SetContinuation(this, GetPreviousTask(), null, CallTargetState.GetDefault());

            await cTask;

            async Task GetPreviousTask()
            {
                await Task.Delay(1000).ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task ExceptionTest()
        {
            Exception ex = null;

            // Normal
            ex = await Assert.ThrowsAsync<CustomException>(() => GetPreviousTask());
            Assert.Equal("Internal Test Exception", ex.Message);

            // Using the continuation
            var tcg = new TaskContinuationGenerator<TaskContinuationGeneratorTests, TaskContinuationGeneratorTests, Task>();
            ex = await Assert.ThrowsAsync<CustomException>(() => tcg.SetContinuation(this, GetPreviousTask(), null, CallTargetState.GetDefault()));
            Assert.Equal("Internal Test Exception", ex.Message);

            async Task GetPreviousTask()
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
            await Assert.ThrowsAsync<CustomCancellationException>(() => task);
            Assert.Equal(TaskStatus.Canceled, task.Status);

            // Using the continuation
            var tcg = new TaskContinuationGenerator<TaskContinuationGeneratorTests, TaskContinuationGeneratorTests, Task>();
            task = tcg.SetContinuation(this, GetPreviousTask(), null, CallTargetState.GetDefault());
            await Assert.ThrowsAsync<CustomCancellationException>(() => task);
            Assert.Equal(TaskStatus.Canceled, task.Status);

            static Task GetPreviousTask()
            {
                var cts = new CancellationTokenSource();

                return Task.FromResult(true).ContinueWith(
                    _ =>
                    {
                        cts.Cancel();
                        throw new CustomCancellationException(cts.Token);
                    },
                    cts.Token);
            }
        }

        [Fact]
        public async Task SuccessGenericTest()
        {
            var tcg = new TaskContinuationGenerator<TaskContinuationGeneratorTests, TaskContinuationGeneratorTests, Task<bool>, bool>();
            var cTask = tcg.SetContinuation(this, GetPreviousTask(), null, CallTargetState.GetDefault());

            await cTask;

            async Task<bool> GetPreviousTask()
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
            ex = await Assert.ThrowsAsync<CustomException>(() => GetPreviousTask());
            Assert.Equal("Internal Test Exception", ex.Message);

            // Using the continuation
            var tcg = new TaskContinuationGenerator<TaskContinuationGeneratorTests, TaskContinuationGeneratorTests, Task<bool>, bool>();
            ex = await Assert.ThrowsAsync<CustomException>(() => tcg.SetContinuation(this, GetPreviousTask(), null, CallTargetState.GetDefault()));
            Assert.Equal("Internal Test Exception", ex.Message);

            async Task<bool> GetPreviousTask()
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
            await Assert.ThrowsAsync<CustomCancellationException>(() => task);
            Assert.Equal(TaskStatus.Canceled, task.Status);

            // Using the continuation
            var tcg = new TaskContinuationGenerator<TaskContinuationGeneratorTests, TaskContinuationGeneratorTests, Task<bool>, bool>();
            task = tcg.SetContinuation(this, GetPreviousTask(), null, CallTargetState.GetDefault());
            await Assert.ThrowsAsync<CustomCancellationException>(() => task);

			task.Status.Should().Be(TaskStatus.Canceled);

            static Task<bool> GetPreviousTask()
            {
                var cts = new CancellationTokenSource();

                return Task.FromResult(true).ContinueWith<bool>(
                    _ =>
                    {
                        cts.Cancel();
                        throw new CustomCancellationException(cts.Token);
                    },
                    cts.Token);
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
