// Licensed to Elasticsearch B.V under the Apache 2.0 License.
// Elasticsearch B.V licenses this file, including any modifications, to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.
//
// <copyright file="CallTargetInvoker.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Runtime.CompilerServices;
using Elastic.Apm.Profiler.Managed.CallTarget.Handlers;

namespace Elastic.Apm.Profiler.Managed.CallTarget
{
    /// <summary>
    /// CallTarget Invoker
    /// </summary>
    public static class CallTargetInvoker
    {
        /// <summary>
        /// Begin Method Invoker
        /// </summary>
        /// <typeparam name="TIntegration">Integration type</typeparam>
        /// <typeparam name="TTarget">Target type</typeparam>
        /// <param name="instance">Instance value</param>
        /// <returns>Call target state</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CallTargetState BeginMethod<TIntegration, TTarget>(TTarget instance)
        {
            if (IntegrationOptions<TIntegration, TTarget>.IsIntegrationEnabled)
            {
                return BeginMethodHandler<TIntegration, TTarget>.Invoke(instance);
            }

            return CallTargetState.GetDefault();
        }

        /// <summary>
        /// Begin Method Invoker
        /// </summary>
        /// <typeparam name="TIntegration">Integration type</typeparam>
        /// <typeparam name="TTarget">Target type</typeparam>
        /// <typeparam name="TArg1">First argument type</typeparam>
        /// <param name="instance">Instance value</param>
        /// <param name="arg1">First argument value</param>
        /// <returns>Call target state</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CallTargetState BeginMethod<TIntegration, TTarget, TArg1>(TTarget instance, TArg1 arg1)
        {
            if (IntegrationOptions<TIntegration, TTarget>.IsIntegrationEnabled)
            {
                return BeginMethodHandler<TIntegration, TTarget, TArg1>.Invoke(instance, arg1);
            }

            return CallTargetState.GetDefault();
        }

        /// <summary>
        /// Begin Method Invoker
        /// </summary>
        /// <typeparam name="TIntegration">Integration type</typeparam>
        /// <typeparam name="TTarget">Target type</typeparam>
        /// <typeparam name="TArg1">First argument type</typeparam>
        /// <typeparam name="TArg2">Second argument type</typeparam>
        /// <param name="instance">Instance value</param>
        /// <param name="arg1">First argument value</param>
        /// <param name="arg2">Second argument value</param>
        /// <returns>Call target state</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CallTargetState BeginMethod<TIntegration, TTarget, TArg1, TArg2>(TTarget instance, TArg1 arg1, TArg2 arg2)
        {
            if (IntegrationOptions<TIntegration, TTarget>.IsIntegrationEnabled)
            {
                return BeginMethodHandler<TIntegration, TTarget, TArg1, TArg2>.Invoke(instance, arg1, arg2);
            }

            return CallTargetState.GetDefault();
        }

        /// <summary>
        /// Begin Method Invoker
        /// </summary>
        /// <typeparam name="TIntegration">Integration type</typeparam>
        /// <typeparam name="TTarget">Target type</typeparam>
        /// <typeparam name="TArg1">First argument type</typeparam>
        /// <typeparam name="TArg2">Second argument type</typeparam>
        /// <typeparam name="TArg3">Third argument type</typeparam>
        /// <param name="instance">Instance value</param>
        /// <param name="arg1">First argument value</param>
        /// <param name="arg2">Second argument value</param>
        /// <param name="arg3">Third argument value</param>
        /// <returns>Call target state</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CallTargetState BeginMethod<TIntegration, TTarget, TArg1, TArg2, TArg3>(TTarget instance, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            if (IntegrationOptions<TIntegration, TTarget>.IsIntegrationEnabled)
            {
                return BeginMethodHandler<TIntegration, TTarget, TArg1, TArg2, TArg3>.Invoke(instance, arg1, arg2, arg3);
            }

            return CallTargetState.GetDefault();
        }

        /// <summary>
        /// Begin Method Invoker
        /// </summary>
        /// <typeparam name="TIntegration">Integration type</typeparam>
        /// <typeparam name="TTarget">Target type</typeparam>
        /// <typeparam name="TArg1">First argument type</typeparam>
        /// <typeparam name="TArg2">Second argument type</typeparam>
        /// <typeparam name="TArg3">Third argument type</typeparam>
        /// <typeparam name="TArg4">Fourth argument type</typeparam>
        /// <param name="instance">Instance value</param>
        /// <param name="arg1">First argument value</param>
        /// <param name="arg2">Second argument value</param>
        /// <param name="arg3">Third argument value</param>
        /// <param name="arg4">Fourth argument value</param>
        /// <returns>Call target state</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CallTargetState BeginMethod<TIntegration, TTarget, TArg1, TArg2, TArg3, TArg4>(TTarget instance, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            if (IntegrationOptions<TIntegration, TTarget>.IsIntegrationEnabled)
            {
                return BeginMethodHandler<TIntegration, TTarget, TArg1, TArg2, TArg3, TArg4>.Invoke(instance, arg1, arg2, arg3, arg4);
            }

            return CallTargetState.GetDefault();
        }

        /// <summary>
        /// Begin Method Invoker
        /// </summary>
        /// <typeparam name="TIntegration">Integration type</typeparam>
        /// <typeparam name="TTarget">Target type</typeparam>
        /// <typeparam name="TArg1">First argument type</typeparam>
        /// <typeparam name="TArg2">Second argument type</typeparam>
        /// <typeparam name="TArg3">Third argument type</typeparam>
        /// <typeparam name="TArg4">Fourth argument type</typeparam>
        /// <typeparam name="TArg5">Fifth argument type</typeparam>
        /// <param name="instance">Instance value</param>
        /// <param name="arg1">First argument value</param>
        /// <param name="arg2">Second argument value</param>
        /// <param name="arg3">Third argument value</param>
        /// <param name="arg4">Fourth argument value</param>
        /// <param name="arg5">Fifth argument value</param>
        /// <returns>Call target state</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CallTargetState BeginMethod<TIntegration, TTarget, TArg1, TArg2, TArg3, TArg4, TArg5>(TTarget instance, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
        {
            if (IntegrationOptions<TIntegration, TTarget>.IsIntegrationEnabled)
            {
                return BeginMethodHandler<TIntegration, TTarget, TArg1, TArg2, TArg3, TArg4, TArg5>.Invoke(instance, arg1, arg2, arg3, arg4, arg5);
            }

            return CallTargetState.GetDefault();
        }

        /// <summary>
        /// Begin Method Invoker
        /// </summary>
        /// <typeparam name="TIntegration">Integration type</typeparam>
        /// <typeparam name="TTarget">Target type</typeparam>
        /// <typeparam name="TArg1">First argument type</typeparam>
        /// <typeparam name="TArg2">Second argument type</typeparam>
        /// <typeparam name="TArg3">Third argument type</typeparam>
        /// <typeparam name="TArg4">Fourth argument type</typeparam>
        /// <typeparam name="TArg5">Fifth argument type</typeparam>
        /// <typeparam name="TArg6">Sixth argument type</typeparam>
        /// <param name="instance">Instance value</param>
        /// <param name="arg1">First argument value</param>
        /// <param name="arg2">Second argument value</param>
        /// <param name="arg3">Third argument value</param>
        /// <param name="arg4">Fourth argument value</param>
        /// <param name="arg5">Fifth argument value</param>
        /// <param name="arg6">Sixth argument value</param>
        /// <returns>Call target state</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CallTargetState BeginMethod<TIntegration, TTarget, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(TTarget instance, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
        {
            if (IntegrationOptions<TIntegration, TTarget>.IsIntegrationEnabled)
            {
                return BeginMethodHandler<TIntegration, TTarget, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>.Invoke(instance, arg1, arg2, arg3, arg4, arg5, arg6);
            }

            return CallTargetState.GetDefault();
        }

        /// <summary>
        /// Begin Method Invoker
        /// </summary>
        /// <typeparam name="TIntegration">Integration type</typeparam>
        /// <typeparam name="TTarget">Target type</typeparam>
        /// <typeparam name="TArg1">First argument type</typeparam>
        /// <typeparam name="TArg2">Second argument type</typeparam>
        /// <typeparam name="TArg3">Third argument type</typeparam>
        /// <typeparam name="TArg4">Fourth argument type</typeparam>
        /// <typeparam name="TArg5">Fifth argument type</typeparam>
        /// <typeparam name="TArg6">Sixth argument type</typeparam>
        /// <typeparam name="TArg7">Seventh argument type</typeparam>
        /// <param name="instance">Instance value</param>
        /// <param name="arg1">First argument value</param>
        /// <param name="arg2">Second argument value</param>
        /// <param name="arg3">Third argument value</param>
        /// <param name="arg4">Fourth argument value</param>
        /// <param name="arg5">Fifth argument value</param>
        /// <param name="arg6">Sixth argument value</param>
        /// <param name="arg7">Seventh argument value</param>
        /// <returns>Call target state</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CallTargetState BeginMethod<TIntegration, TTarget, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(TTarget instance, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7)
        {
            if (IntegrationOptions<TIntegration, TTarget>.IsIntegrationEnabled)
            {
                return BeginMethodHandler<TIntegration, TTarget, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>.Invoke(instance, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            }

            return CallTargetState.GetDefault();
        }

        /// <summary>
        /// Begin Method Invoker
        /// </summary>
        /// <typeparam name="TIntegration">Integration type</typeparam>
        /// <typeparam name="TTarget">Target type</typeparam>
        /// <typeparam name="TArg1">First argument type</typeparam>
        /// <typeparam name="TArg2">Second argument type</typeparam>
        /// <typeparam name="TArg3">Third argument type</typeparam>
        /// <typeparam name="TArg4">Fourth argument type</typeparam>
        /// <typeparam name="TArg5">Fifth argument type</typeparam>
        /// <typeparam name="TArg6">Sixth argument type</typeparam>
        /// <typeparam name="TArg7">Seventh argument type</typeparam>
        /// <typeparam name="TArg8">Eighth argument type</typeparam>
        /// <param name="instance">Instance value</param>
        /// <param name="arg1">First argument value</param>
        /// <param name="arg2">Second argument value</param>
        /// <param name="arg3">Third argument value</param>
        /// <param name="arg4">Fourth argument value</param>
        /// <param name="arg5">Fifth argument value</param>
        /// <param name="arg6">Sixth argument value</param>
        /// <param name="arg7">Seventh argument value</param>
        /// <param name="arg8">Eighth argument value</param>
        /// <returns>Call target state</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CallTargetState BeginMethod<TIntegration, TTarget, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8>(TTarget instance, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8)
        {
            if (IntegrationOptions<TIntegration, TTarget>.IsIntegrationEnabled)
            {
                return BeginMethodHandler<TIntegration, TTarget, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8>.Invoke(instance, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
            }

            return CallTargetState.GetDefault();
        }

        /// <summary>
        /// Begin Method Invoker Slow Path
        /// </summary>
        /// <typeparam name="TIntegration">Integration type</typeparam>
        /// <typeparam name="TTarget">Target type</typeparam>
        /// <param name="instance">Instance value</param>
        /// <param name="arguments">Object arguments array</param>
        /// <returns>Call target state</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CallTargetState BeginMethod<TIntegration, TTarget>(TTarget instance, object[] arguments)
        {
            if (IntegrationOptions<TIntegration, TTarget>.IsIntegrationEnabled)
            {
                return BeginMethodSlowHandler<TIntegration, TTarget>.Invoke(instance, arguments);
            }

            return CallTargetState.GetDefault();
        }

        /// <summary>
        /// End Method with Void return value invoker
        /// </summary>
        /// <typeparam name="TIntegration">Integration type</typeparam>
        /// <typeparam name="TTarget">Target type</typeparam>
        /// <param name="instance">Instance value</param>
        /// <param name="exception">Exception value</param>
        /// <param name="state">CallTarget state</param>
        /// <returns>CallTarget return structure</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CallTargetReturn EndMethod<TIntegration, TTarget>(TTarget instance, Exception exception, CallTargetState state)
        {
            if (IntegrationOptions<TIntegration, TTarget>.IsIntegrationEnabled)
            {
                return EndMethodHandler<TIntegration, TTarget>.Invoke(instance, exception, state);
            }

            return CallTargetReturn.GetDefault();
        }

        /// <summary>
        /// End Method with Return value invoker
        /// </summary>
        /// <typeparam name="TIntegration">Integration type</typeparam>
        /// <typeparam name="TTarget">Target type</typeparam>
        /// <typeparam name="TReturn">Return type</typeparam>
        /// <param name="instance">Instance value</param>
        /// <param name="returnValue">Return value</param>
        /// <param name="exception">Exception value</param>
        /// <param name="state">CallTarget state</param>
        /// <returns>CallTarget return structure</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CallTargetReturn<TReturn> EndMethod<TIntegration, TTarget, TReturn>(TTarget instance, TReturn returnValue, Exception exception, CallTargetState state)
        {
            if (IntegrationOptions<TIntegration, TTarget>.IsIntegrationEnabled)
            {
                return EndMethodHandler<TIntegration, TTarget, TReturn>.Invoke(instance, returnValue, exception, state);
            }

            return new CallTargetReturn<TReturn>(returnValue);
        }

        /// <summary>
        /// Log integration exception
        /// </summary>
        /// <typeparam name="TIntegration">Integration type</typeparam>
        /// <typeparam name="TTarget">Target type</typeparam>
        /// <param name="exception">Integration exception instance</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogException<TIntegration, TTarget>(Exception exception) => IntegrationOptions<TIntegration, TTarget>.LogException(exception);

		/// <summary>
        /// Gets the default value of a type
        /// </summary>
        /// <typeparam name="T">Type to get the default value</typeparam>
        /// <returns>Default value of T</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetDefaultValue<T>() => default;
    }
}
