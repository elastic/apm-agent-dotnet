// Licensed to Elasticsearch B.V under the Apache 2.0 License.
// Elasticsearch B.V licenses this file, including any modifications, to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.
//
// <copyright file="DelegateMetadata.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;

namespace Elastic.Apm.Profiler.Managed.Reflection
{
    internal class DelegateMetadata
    {
        public Type Type { get; set; }

        public Type ReturnType { get; set; }

        public Type[] Generics { get; set; }

        public Type[] Parameters { get; set; }

        public static DelegateMetadata Create<TDelegate>()
            where TDelegate : Delegate
        {
            var delegateType = typeof(TDelegate);
            var genericTypeArguments = delegateType.GenericTypeArguments;

            Type[] parameterTypes;
            Type returnType;

            if (delegateType.Name.StartsWith("Func`"))
            {
                // last generic type argument is the return type
                var parameterCount = genericTypeArguments.Length - 1;
                parameterTypes = new Type[parameterCount];
                Array.Copy(genericTypeArguments, parameterTypes, parameterCount);

                returnType = genericTypeArguments[parameterCount];
            }
            else if (delegateType.Name.StartsWith("Action`"))
            {
                parameterTypes = genericTypeArguments;
                returnType = typeof(void);
            }
            else
            {
                throw new Exception($"Only Func<> or Action<> are supported in {nameof(DelegateMetadata)}.");
            }

            return new DelegateMetadata
            {
                Generics = genericTypeArguments,
                Parameters = parameterTypes,
                ReturnType = returnType,
                Type = delegateType
            };
        }
    }
}
