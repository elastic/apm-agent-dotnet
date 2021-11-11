// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="IBasicGetResult.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.ComponentModel;

namespace Elastic.Apm.Profiler.Managed.Integrations.RabbitMq
{
    /// <summary>
    /// BasicGetResult interface for ducktyping
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IBasicGetResult
    {
        /// <summary>
        /// Gets the message body of the result
        /// </summary>
        IBody Body { get; }

        /// <summary>
        /// Gets the message properties
        /// </summary>
        IBasicProperties BasicProperties { get; }

		/// <summary>
		/// Retrieve the exchange this message was published to.
		/// </summary>
		string Exchange { get; }

		/// <summary>
		/// Retrieve the routing key with which this message was published.
		/// </summary>
		public string RoutingKey { get; }
    }
}
