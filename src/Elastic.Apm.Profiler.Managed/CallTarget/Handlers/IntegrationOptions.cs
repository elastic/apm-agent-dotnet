// Licensed to Elasticsearch B.V under the Apache 2.0 License.
// Elasticsearch B.V licenses this file, including any modifications, to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.
//
// <copyright file="IntegrationOptions.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Runtime.CompilerServices;
using Elastic.Apm.Logging;
using Elastic.Apm.Profiler.Managed.DuckTyping;

namespace Elastic.Apm.Profiler.Managed.CallTarget.Handlers
{
    internal static class IntegrationOptions<TIntegration, TTarget>
    {
		private static volatile bool _disableIntegration;

        internal static bool IsIntegrationEnabled => !_disableIntegration;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void DisableIntegration() => _disableIntegration = true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void LogException(Exception exception, string message = null)
        {
            // ReSharper disable twice ExplicitCallerInfoArgument
            Logger.Log(LogLevel.Error, exception, message ?? "exception whilst instrumenting integration <{0}, {1}>",
				typeof(TIntegration).FullName,
				typeof(TTarget).FullName);

             if (exception is DuckTypeException)
             {
                 Logger.Log(LogLevel.Warn, "DuckTypeException has been detected, the integration <{0}, {1}> will be disabled.",
					 typeof(TIntegration).FullName,
					 typeof(TTarget).FullName);
                 _disableIntegration = true;
             }
             else if (exception is CallTargetInvokerException)
			 {
				 Logger.Log(LogLevel.Warn, "CallTargetInvokerException has been detected, the integration <{0}, {1}> will be disabled.",
					 typeof(TIntegration).FullName,
					 typeof(TTarget).FullName);
				 _disableIntegration = true;
			 }
		}
    }
}
