// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="KafkaConstants.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

namespace Elastic.Apm.Profiler.Managed.Integrations.Kafka
{
    internal static class KafkaConstants
    {
		internal const string ConsumeOperationName = "kafka.consume";
        internal const string ProduceOperationName = "kafka.produce";
        internal const string TopicPartitionTypeName = "Confluent.Kafka.TopicPartition";
        internal const string MessageTypeName = "Confluent.Kafka.Message`2[!0,!1]";
        internal const string ConsumeResultTypeName = "Confluent.Kafka.ConsumeResult`2[!0,!1]";
        internal const string ActionOfDeliveryReportTypeName = "System.Action`1[Confluent.Kafka.DeliveryReport`2[!0,!1]]";
        internal const string TaskDeliveryReportTypeName = "System.Threading.Tasks.Task`1[Confluent.Kafka.DeliveryReport`2[!0,!1]]";
        internal const string Subtype = "kafka";
		internal const string IntegrationName = "Kafka";
	}
}
