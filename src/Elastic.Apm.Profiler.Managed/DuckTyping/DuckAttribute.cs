// <copyright file="DuckAttribute.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>
// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Reflection;

namespace Elastic.Apm.Profiler.Managed.DuckTyping
{
    /// <summary>
    /// Duck kind
    /// </summary>
    public enum DuckKind
    {
        /// <summary>
        /// Property
        /// </summary>
        Property,

        /// <summary>
        /// Field
        /// </summary>
        Field
    }

    /// <summary>
    /// Duck attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Field, AllowMultiple = false)]
    public class DuckAttribute : Attribute
    {
        /// <summary>
        /// Default BindingFlags
        /// </summary>
        public const BindingFlags DefaultFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.FlattenHierarchy;

        /// <summary>
        /// Gets or sets the underlying type member name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets duck kind
        /// </summary>
        public DuckKind Kind { get; set; } = DuckKind.Property;

        /// <summary>
        /// Gets or sets the binding flags
        /// </summary>
        public BindingFlags BindingFlags { get; set; } = DefaultFlags;

        /// <summary>
        /// Gets or sets the generic parameter type names definition for a generic method call (required when calling generic methods and instance type is non public)
        /// </summary>
        public string[] GenericParameterTypeNames { get; set; }

        /// <summary>
        /// Gets or sets the parameter type names of the target method (optional / used to disambiguation)
        /// </summary>
        public string[] ParameterTypeNames { get; set; }
    }
}
