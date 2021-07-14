// Based on .NET Tracer for Datadog APM by Datadog
// https://github.com/DataDog/dd-trace-dotnet
// Licensed to Elasticsearch B.V under the Apache 2.0 License.
// Elasticsearch B.V licenses this file, including any modifications, to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Elastic.Apm.Profiler.Managed.Reflection
{
    internal static class ClrNames
    {
        public const string Ignore = "_";

        public const string Void = "System.Void";
        public const string Object = "System.Object";
        public const string Bool = "System.Boolean";
        public const string String = "System.String";

        public const string SByte = "System.SByte";
        public const string Byte = "System.Byte";

        public const string Int16 = "System.Int16";
        public const string Int32 = "System.Int32";
        public const string Int64 = "System.Int64";

        public const string UInt16 = "System.UInt16";
        public const string UInt32 = "System.UInt32";
        public const string UInt64 = "System.UInt64";

        public const string Stream = "System.IO.Stream";

        public const string Task = "System.Threading.Tasks.Task";
        public const string CancellationToken = "System.Threading.CancellationToken";

        // ReSharper disable once InconsistentNaming
        public const string IAsyncResult = "System.IAsyncResult";
        public const string AsyncCallback = "System.AsyncCallback";

        public const string HttpRequestMessage = "System.Net.Http.HttpRequestMessage";
        public const string HttpResponseMessage = "System.Net.Http.HttpResponseMessage";
        public const string HttpResponseMessageTask = "System.Threading.Tasks.Task`1<System.Net.Http.HttpResponseMessage>";

        public const string GenericTask = "System.Threading.Tasks.Task`1";
		public const string GenericParameterTask = "System.Threading.Tasks.Task`1<T>";
    }

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
