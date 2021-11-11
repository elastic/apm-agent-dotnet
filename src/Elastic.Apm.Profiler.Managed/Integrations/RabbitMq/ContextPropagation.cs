// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="ContextPropagation.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Elastic.Apm.Profiler.Managed.Integrations.RabbitMq
{
    internal static class ContextPropagation
    {
		public static Action<IDictionary<string, object>, string, string> HeadersSetter = (carrier, key, value) =>
        {
            carrier[key] = Encoding.UTF8.GetBytes(value);
        };

        public static Func<IDictionary<string, object>, string, IEnumerable<string>> HeadersGetter = ((carrier, key) =>
		{
			return carrier.TryGetValue(key, out var value) && value is byte[] bytes
				? new[] { Encoding.UTF8.GetString(bytes) }
				: Enumerable.Empty<string>();
		});
    }
}
