// Licensed to Elasticsearch B.V under the Apache 2.0 License.
// Elasticsearch B.V licenses this file, including any modifications, to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.
//
// <copyright file="EndMethodHandler.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Elastic.Apm.Profiler.Managed.CallTarget.Handlers
{
    internal static class EndMethodHandler<TIntegration, TTarget>
    {
        private static readonly InvokeDelegate _invokeDelegate;

        static EndMethodHandler()
        {
            try
            {
                var dynMethod = IntegrationMapper.CreateEndMethodDelegate(typeof(TIntegration), typeof(TTarget));
                if (dynMethod != null)
					_invokeDelegate = (InvokeDelegate)dynMethod.CreateDelegate(typeof(InvokeDelegate));
			}
            catch (Exception ex)
            {
                throw new CallTargetInvokerException(ex);
            }
            finally
            {
                if (_invokeDelegate is null)
					_invokeDelegate = (instance, exception, state) => CallTargetReturn.GetDefault();
			}
        }

        internal delegate CallTargetReturn InvokeDelegate(TTarget instance, Exception exception, CallTargetState state);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static CallTargetReturn Invoke(TTarget instance, Exception exception, CallTargetState state) =>
			_invokeDelegate(instance, exception, state);
	}
}
