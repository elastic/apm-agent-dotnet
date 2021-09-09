// Licensed to Elasticsearch B.V under the Apache 2.0 License.
// Elasticsearch B.V licenses this file, including any modifications, to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.
//
// <copyright file="ContinuationsHelper.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Elastic.Apm.Profiler.Managed.CallTarget.Handlers.Continuations
{
    internal static class ContinuationsHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Type GetResultType(Type parentType)
        {
            var currentType = parentType;
            while (currentType != null)
            {
                var typeArguments = currentType.GenericTypeArguments ?? Type.EmptyTypes;
                switch (typeArguments.Length)
                {
                    case 0:
                        return typeof(object);
                    case 1:
                        return typeArguments[0];
                    default:
                        currentType = currentType.BaseType;
                        break;
                }
            }

            return typeof(object);
        }

#if !NETCOREAPP3_1
		internal static TTo Convert<TFrom, TTo>(TFrom value) => Converter<TFrom, TTo>.Convert(value);

		private static class Converter<TFrom, TTo>
        {
            private static readonly ConvertDelegate _converter;

            static Converter()
            {
                var dMethod = new DynamicMethod($"Converter<{typeof(TFrom).Name},{typeof(TTo).Name}>", typeof(TTo), new[] { typeof(TFrom) }, typeof(ConvertDelegate).Module, true);
                var il = dMethod.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ret);
                _converter = (ConvertDelegate)dMethod.CreateDelegate(typeof(ConvertDelegate));
            }

            private delegate TTo ConvertDelegate(TFrom value);

            public static TTo Convert(TFrom value) => _converter(value);
		}
#endif
    }
}
