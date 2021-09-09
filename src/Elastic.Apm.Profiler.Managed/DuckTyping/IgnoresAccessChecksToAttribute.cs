// Licensed to Elasticsearch B.V under the Apache 2.0 License.
// Elasticsearch B.V licenses this file, including any modifications, to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.
//
// <copyright file="IgnoresAccessChecksToAttribute.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;

namespace Elastic.Apm.Profiler.Managed.DuckTyping
{
    /// <summary>
    /// This attribute is recognized by the CLR and allow us to disable visibility checks for certain assemblies (only from 4.6+)
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class IgnoresAccessChecksToAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IgnoresAccessChecksToAttribute"/> class.
        /// </summary>
        /// <param name="assemblyName">Assembly name</param>
        public IgnoresAccessChecksToAttribute(string assemblyName) => AssemblyName = assemblyName;

		/// <summary>
        /// Gets the assembly name
        /// </summary>
        public string AssemblyName { get; }
    }
}
