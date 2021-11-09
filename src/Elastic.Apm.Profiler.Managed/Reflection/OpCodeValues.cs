// Licensed to Elasticsearch B.V under the Apache 2.0 License.
// Elasticsearch B.V licenses this file, including any modifications, to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.
//
// <copyright file="OpCodeValue.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

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
