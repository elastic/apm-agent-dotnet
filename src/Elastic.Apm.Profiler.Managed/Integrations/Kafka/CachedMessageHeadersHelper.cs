// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="CachedMessageHeadersHelper.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Reflection.Emit;
using Elastic.Apm.Profiler.Managed.DuckTyping;

namespace Elastic.Apm.Profiler.Managed.Integrations.Kafka
{
    internal static class CachedMessageHeadersHelper<TMarkerType>
    {
        private static readonly Func<object> _activator;

        static CachedMessageHeadersHelper()
        {
            var headersType = typeof(TMarkerType).Assembly.GetType("Confluent.Kafka.Headers");

            var ctor = headersType.GetConstructor(System.Type.EmptyTypes);

            var createHeadersMethod = new DynamicMethod(
                $"KafkaCachedMessageHeadersHelpers",
                headersType,
                null,
                typeof(DuckType).Module,
                true);

            var il = createHeadersMethod.GetILGenerator();
            il.Emit(OpCodes.Newobj, ctor);
            il.Emit(OpCodes.Ret);

            _activator = (Func<object>)createHeadersMethod.CreateDelegate(typeof(Func<object>));
        }

        /// <summary>
        /// Creates a Confluent.Kafka.Headers object and assigns it to an `IMessage` proxy
        /// </summary>
        /// <returns>A proxy for the new Headers object</returns>
        public static IHeaders CreateHeaders()
        {
            var headers = _activator();
            return headers.DuckCast<IHeaders>();
        }
    }
}
