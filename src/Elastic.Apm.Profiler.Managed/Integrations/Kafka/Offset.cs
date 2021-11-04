// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="Offset.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.ComponentModel;
using Elastic.Apm.Profiler.Managed.DuckTyping;

// ReSharper disable SA1310

namespace Elastic.Apm.Profiler.Managed.Integrations.Kafka
{
    /// <summary>
    /// Partition for duck-typing
    /// </summary>
    [DuckCopy]
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct Offset
    {
        private const long RdKafkaOffsetBeginning = -2;
        private const long RdKafkaOffsetEnd = -1;
        private const long RdKafkaOffsetStored = -1000;
        private const long RdKafkaOffsetInvalid = -1001;

        /// <summary>
        /// Gets the long value corresponding to this offset
        /// </summary>
        public long Value;

        /// <summary>
        /// Based on the original implementation
        /// https://github.com/confluentinc/confluent-kafka-dotnet/blob/643c8fdc90f54f4d82d5135ae7e91a995f0efdee/src/Confluent.Kafka/Offset.cs#L274
        /// </summary>
        /// <returns>A string that represents the Offset object</returns>
        public override string ToString() =>
			Value switch
			{
				RdKafkaOffsetBeginning => $"Beginning [{RdKafkaOffsetBeginning}]",
				RdKafkaOffsetEnd => $"End [{RdKafkaOffsetEnd}]",
				RdKafkaOffsetStored => $"Stored [{RdKafkaOffsetStored}]",
				RdKafkaOffsetInvalid => $"Unset [{RdKafkaOffsetInvalid}]",
				_ => Value.ToString()
			};
	}
}
