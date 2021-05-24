// Based on .NET Tracer for Datadog APM by Datadog
// https://github.com/DataDog/dd-trace-dotnet
// Licensed to Elasticsearch B.V under the Apache 2.0 License.
// Elasticsearch B.V licenses this file, including any modifications, to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Elastic.Apm.Profiler.Managed.Reflection
{
    internal enum OpCodeValue : short
    {
        /// <seealso cref="System.Reflection.Emit.OpCodes.Call"/>
        Call = 40,

        /// <seealso cref="System.Reflection.Emit.OpCodes.Callvirt"/>
        Callvirt = 111
    }
}
