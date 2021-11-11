// <copyright file="IBasicProperties.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.Collections.Generic;
using System.ComponentModel;

namespace Elastic.Apm.Profiler.Managed.Integrations.RabbitMq
{
    /// <summary>
    /// BasicProperties interface for ducktyping
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IBasicProperties
    {
        /// <summary>
        /// Gets or sets the headers of the message
        /// </summary>
        /// <returns>Message headers</returns>
        IDictionary<string, object> Headers { get; set; }

        /// <summary>
        /// Gets the delivery mode of the message
        /// </summary>
        byte DeliveryMode { get; }

        /// <summary>
        /// Returns true if the DeliveryMode property is present
        /// </summary>
        /// <returns>true if the DeliveryMode property is present</returns>
        bool IsDeliveryModePresent();
    }
}
