// <copyright file="TypesTuple.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>
// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;

namespace Elastic.Apm.Profiler.Managed.DuckTyping
{
    internal readonly struct TypesTuple : IEquatable<TypesTuple>
    {
        /// <summary>
        /// The proxy definition type
        /// </summary>
        public readonly Type ProxyDefinitionType;

        /// <summary>
        /// The target type
        /// </summary>
        public readonly Type TargetType;

        /// <summary>
        /// Initializes a new instance of the <see cref="TypesTuple"/> struct.
        /// </summary>
        /// <param name="proxyDefinitionType">The proxy definition type</param>
        /// <param name="targetType">The target type</param>
        public TypesTuple(Type proxyDefinitionType, Type targetType)
        {
            ProxyDefinitionType = proxyDefinitionType;
            TargetType = targetType;
        }

        /// <summary>
        /// Gets the struct hashcode
        /// </summary>
        /// <returns>Hashcode</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)2166136261;
                hash = (hash ^ ProxyDefinitionType.GetHashCode()) * 16777619;
                hash = (hash ^ TargetType.GetHashCode()) * 16777619;
                return hash;
            }
        }

        /// <summary>
        /// Gets if the struct is equal to other object or struct
        /// </summary>
        /// <param name="obj">Object to compare</param>
        /// <returns>True if both are equals; otherwise, false.</returns>
        public override bool Equals(object obj) =>
			obj is TypesTuple vTuple &&
			ProxyDefinitionType == vTuple.ProxyDefinitionType &&
			TargetType == vTuple.TargetType;

		/// <inheritdoc />
        public bool Equals(TypesTuple other) =>
			ProxyDefinitionType == other.ProxyDefinitionType &&
			TargetType == other.TargetType;
	}
}
