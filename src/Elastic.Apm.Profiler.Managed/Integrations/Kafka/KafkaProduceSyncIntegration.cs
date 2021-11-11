// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="KafkaProduceSyncIntegration.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.ComponentModel;
using Elastic.Apm.Api;
using Elastic.Apm.Profiler.Managed.CallTarget;
using Elastic.Apm.Profiler.Managed.Core;
using Elastic.Apm.Profiler.Managed.DuckTyping;

namespace Elastic.Apm.Profiler.Managed.Integrations.Kafka
{
    /// <summary>
    /// Confluent.Kafka Producer.Produce calltarget instrumentation
    /// </summary>
    [Instrument(
        Assembly = "Confluent.Kafka",
        Type = "Confluent.Kafka.Producer`2",
        Method = "Produce",
        ReturnType = ClrTypeNames.Void,
        ParameterTypes = new[] { KafkaIntegration.TopicPartitionTypeName, KafkaIntegration.MessageTypeName, KafkaIntegration.ActionOfDeliveryReportTypeName },
        MinimumVersion = "1.4.0",
        MaximumVersion = "1.*.*",
        Group = KafkaIntegration.Name)]
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class KafkaProduceSyncIntegration
    {
        /// <summary>
        /// OnMethodBegin callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <typeparam name="TTopicPartition">Type of the TopicPartition</typeparam>
        /// <typeparam name="TMessage">Type of the message</typeparam>
        /// <typeparam name="TDeliveryHandler">Type of the delivery handler action</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="topicPartition">TopicPartition instance</param>
        /// <param name="message">Message instance</param>
        /// <param name="deliveryHandler">Delivery Handler instance</param>
        /// <returns>Calltarget state value</returns>
        public static CallTargetState OnMethodBegin<TTarget, TTopicPartition, TMessage, TDeliveryHandler>(TTarget instance, TTopicPartition topicPartition, TMessage message, TDeliveryHandler deliveryHandler)
            where TMessage : IMessage
        {
			var agent = Agent.Instance;

            // manually doing duck cast here so we have access to the _original_ TopicPartition type
            // as a generic parameter, for injecting headers
			var span = KafkaIntegration.CreateProducerSpan(
                agent,
                topicPartition.DuckCast<ITopicPartition>(),
                isTombstone: message.Value is null,
                finishOnClose: deliveryHandler is null);

            if (span is not null)
                KafkaIntegration.TryInjectHeaders<TTopicPartition, TMessage>(agent, span, message);

            return new CallTargetState(span);
        }

        /// <summary>
        /// OnMethodEnd callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="exception">Exception instance in case the original code threw an exception.</param>
        /// <param name="state">Calltarget state value</param>
        /// <returns>A response value, in an async scenario will be T of Task of T</returns>
        public static CallTargetReturn OnMethodEnd<TTarget>(TTarget instance, Exception exception, CallTargetState state)
        {
			var span = state.Segment;
			if (span is not null)
			{
				if (exception is not null)
					span.CaptureException(exception);

				span.End();
			}

			return CallTargetReturn.GetDefault();
        }
    }
}
