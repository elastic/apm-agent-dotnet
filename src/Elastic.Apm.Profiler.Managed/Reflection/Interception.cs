// Based on .NET Tracer for Datadog APM by Datadog
// https://github.com/DataDog/dd-trace-dotnet
// Licensed to Elasticsearch B.V under the Apache 2.0 License.
// Elasticsearch B.V licenses this file, including any modifications, to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Elastic.Apm.Profiler.Managed.Reflection
{
	internal static class Interception
    {
        internal const Type[] NullTypeArray = null;
        internal static readonly object[] NoArgObjects = Array.Empty<object>();
        internal static readonly Type[] NoArgTypes = Type.EmptyTypes;
        internal static readonly Type VoidType = typeof(void);

        internal static Type[] ParamsToTypes(params object[] objectsToCheck)
        {
            var types = new Type[objectsToCheck.Length];

            for (var i = 0; i < objectsToCheck.Length; i++)
            {
                types[i] = objectsToCheck[i]?.GetType();
            }

            return types;
        }

        internal static string MethodKey(
            Type owningType,
            Type returnType,
            Type[] genericTypes,
            Type[] parameterTypes)
        {
            var key = $"{owningType?.AssemblyQualifiedName}_m_r{returnType?.AssemblyQualifiedName}";

            for (ushort i = 0; i < (genericTypes?.Length ?? 0); i++)
            {
                Debug.Assert(genericTypes != null, nameof(genericTypes) + " != null");
                key = string.Concat(key, $"_g{genericTypes[i].AssemblyQualifiedName}");
            }

            for (ushort i = 0; i < (parameterTypes?.Length ?? 0); i++)
            {
                Debug.Assert(parameterTypes != null, nameof(parameterTypes) + " != null");
                key = string.Concat(key, $"_p{parameterTypes[i].AssemblyQualifiedName}");
            }

            return key;
        }
    }
}
