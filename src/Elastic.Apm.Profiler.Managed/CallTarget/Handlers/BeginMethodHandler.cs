// Licensed to Elasticsearch B.V under the Apache 2.0 License.
// Elasticsearch B.V licenses this file, including any modifications, to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.
//
// <copyright file="BeginMethodHandler.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Elastic.Apm.Api;

#pragma warning disable SA1649 // File name must match first type name

namespace Elastic.Apm.Profiler.Managed.CallTarget.Handlers
{
    internal static class BeginMethodHandler<TIntegration, TTarget>
    {
        private static readonly InvokeDelegate _invokeDelegate;

        static BeginMethodHandler()
        {
            try
            {
                var dynMethod = IntegrationMapper.CreateBeginMethodDelegate(typeof(TIntegration), typeof(TTarget), Array.Empty<Type>());
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
					_invokeDelegate = instance => CallTargetState.GetDefault();
			}
        }

        internal delegate CallTargetState InvokeDelegate(TTarget instance);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static CallTargetState Invoke(TTarget instance) =>
			new CallTargetState(Agent.IsConfigured ? Agent.Tracer.CurrentExecutionSegment() : null, _invokeDelegate(instance));
	}
}
