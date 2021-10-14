// Licensed to Elasticsearch B.V under the Apache 2.0 License.
// Elasticsearch B.V licenses this file, including any modifications, to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.
//
// <copyright file="TaskContinuationGenerator`1.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Reflection.Emit;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable SA1649 // File name must match first type name

namespace Elastic.Apm.Profiler.Managed.CallTarget.Handlers.Continuations
{
    internal class TaskContinuationGenerator<TIntegration, TTarget, TReturn, TResult> : ContinuationGenerator<TTarget, TReturn>
    {
        private static readonly Func<TTarget, TResult, Exception, CallTargetState, TResult> _continuation;

        static TaskContinuationGenerator()
        {
            var continuationMethod = IntegrationMapper.CreateAsyncEndMethodDelegate(typeof(TIntegration), typeof(TTarget), typeof(TResult));
            if (continuationMethod != null)
				_continuation = (Func<TTarget, TResult, Exception, CallTargetState, TResult>)continuationMethod.CreateDelegate(typeof(Func<TTarget, TResult, Exception, CallTargetState, TResult>));
		}

        public override TReturn SetContinuation(TTarget instance, TReturn returnValue, Exception exception, CallTargetState state)
        {
            if (_continuation is null)
				return returnValue;

			if (exception != null || returnValue == null)
            {
                _continuation(instance, default, exception, state);
                return returnValue;
            }

            var previousTask = FromTReturn<Task<TResult>>(returnValue);

            if (previousTask.Status == TaskStatus.RanToCompletion)
				return ToTReturn(Task.FromResult(_continuation(instance, previousTask.Result, default, state)));

			return ToTReturn(ContinuationAction(previousTask, instance, state));
        }

        private static async Task<TResult> ContinuationAction(Task<TResult> previousTask, TTarget target, CallTargetState state)
        {
            if (!previousTask.IsCompleted)
				await new NoThrowAwaiter(previousTask);

			TResult taskResult = default;
            Exception exception = null;
            TResult continuationResult = default;

            if (previousTask.Status == TaskStatus.RanToCompletion)
            {
                taskResult = previousTask.Result;
            }
            else if (previousTask.Status == TaskStatus.Faulted)
            {
                exception = previousTask.Exception.GetBaseException();
            }
            else if (previousTask.Status == TaskStatus.Canceled)
            {
                try
                {
                    // The only supported way to extract the cancellation exception is to await the task
                    await previousTask;
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            }

            try
            {
                // *
                // Calls the CallTarget integration continuation, exceptions here should never bubble up to the application
                // *
                continuationResult = _continuation(target, taskResult, exception, state);
            }
            catch (Exception ex)
            {
                IntegrationOptions<TIntegration, TTarget>.LogException(ex, "Exception occurred when calling the CallTarget integration continuation.");
            }

            // *
            // If the original task throws an exception we rethrow it here.
            // *
            if (exception != null)
				ExceptionDispatchInfo.Capture(exception).Throw();

			return continuationResult;
        }
    }
}
