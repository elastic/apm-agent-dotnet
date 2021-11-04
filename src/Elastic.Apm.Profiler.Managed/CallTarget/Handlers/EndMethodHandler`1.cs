// Licensed to Elasticsearch B.V under the Apache 2.0 License.
// Elasticsearch B.V licenses this file, including any modifications, to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.
//
// <copyright file="EndMethodHandler`1.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Profiler.Managed.CallTarget.Handlers.Continuations;

#pragma warning disable SA1649 // File name must match first type name

namespace Elastic.Apm.Profiler.Managed.CallTarget.Handlers
{
    internal static class EndMethodHandler<TIntegration, TTarget, TReturn>
    {
        private static readonly InvokeDelegate _invokeDelegate = null;
        private static readonly ContinuationGenerator<TTarget, TReturn> _continuationGenerator = null;

        static EndMethodHandler()
        {
            var returnType = typeof(TReturn);
            try
            {
                var dynMethod = IntegrationMapper.CreateEndMethodDelegate(typeof(TIntegration), typeof(TTarget), returnType);
                if (dynMethod != null)
					_invokeDelegate = (InvokeDelegate)dynMethod.CreateDelegate(typeof(InvokeDelegate));
			}
            catch (Exception ex)
            {
                throw new CallTargetInvokerException(ex);
            }

            if (returnType.IsGenericType)
            {
                var genericReturnType = returnType.GetGenericTypeDefinition();
                if (typeof(Task).IsAssignableFrom(returnType))
                {
                    // The type is a Task<>
                    _continuationGenerator = (ContinuationGenerator<TTarget, TReturn>)Activator.CreateInstance(typeof(TaskContinuationGenerator<,,,>).MakeGenericType(typeof(TIntegration), typeof(TTarget), returnType, ContinuationsHelper.GetResultType(returnType)));
                }
#if NETCOREAPP3_1
                else if (genericReturnType == typeof(ValueTask<>))
                {
                    // The type is a ValueTask<>
                    _continuationGenerator = (ContinuationGenerator<TTarget, TReturn>)Activator.CreateInstance(typeof(ValueTaskContinuationGenerator<,,,>).MakeGenericType(typeof(TIntegration), typeof(TTarget), returnType, ContinuationsHelper.GetResultType(returnType)));
                }
#endif
            }
            else
            {
                if (returnType == typeof(Task))
                {
                    // The type is a Task
                    _continuationGenerator = new TaskContinuationGenerator<TIntegration, TTarget, TReturn>();
                }
#if NETCOREAPP3_1
                else if (returnType == typeof(ValueTask))
                {
                    // The type is a ValueTask
                    _continuationGenerator = new ValueTaskContinuationGenerator<TIntegration, TTarget, TReturn>();
                }
#endif
            }
        }

        internal delegate CallTargetReturn<TReturn> InvokeDelegate(TTarget instance, TReturn returnValue, Exception exception, CallTargetState state);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static CallTargetReturn<TReturn> Invoke(TTarget instance, TReturn returnValue, Exception exception, CallTargetState state)
        {
            if (_continuationGenerator != null)
            {
                returnValue = _continuationGenerator.SetContinuation(instance, returnValue, exception, state);

                // Restore previous scope if there is a continuation
                // This is used to mimic the ExecutionContext copy from the StateMachine
                if (Agent.IsConfigured)
				{
					switch (state.PreviousSegment)
					{
						case ITransaction transaction:
							Agent.Components.TracerInternal.CurrentExecutionSegmentsContainer.CurrentTransaction = transaction;
							break;
						case ISpan span:
							Agent.Components.TracerInternal.CurrentExecutionSegmentsContainer.CurrentSpan = span;
							break;
					}
				}
            }

            if (_invokeDelegate != null)
            {
                var returnWrap = _invokeDelegate(instance, returnValue, exception, state);
                returnValue = returnWrap.GetReturnValue();
            }

            return new CallTargetReturn<TReturn>(returnValue);
        }
    }
}
