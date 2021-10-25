// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="ITimestamp.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.ComponentModel;

namespace Elastic.Apm.Profiler.Managed.Integrations.Kafka
{
    /// <summary>
    /// Timestamp struct for duck-typing
    /// Requires boxing, but necessary as we need to duck-type <see cref="Type"/> too
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ITimestamp
    {
        /// <summary>
        /// Gets the timestamp type
        /// </summary>
        public int Type { get; }

        /// <summary>
        /// Gets the UTC DateTime for the timestamp
        /// </summary>
        public DateTime UtcDateTime { get; }
    }
}
