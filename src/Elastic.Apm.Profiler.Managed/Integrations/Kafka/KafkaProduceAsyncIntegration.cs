// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="KafkaProduceAsyncIntegration.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.ComponentModel;
using System.Threading;
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
        Method= "ProduceAsync",
        ReturnType = KafkaIntegration.TaskDeliveryReportTypeName,
        ParameterTypes = new[] { KafkaIntegration.TopicPartitionTypeName, KafkaIntegration.MessageTypeName, ClrTypeNames.CancellationToken },
        MinimumVersion = "1.4.0",
        MaximumVersion = "1.*.*",
        Group = KafkaIntegration.Name)]
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class KafkaProduceAsyncIntegration
    {
        /// <summary>
        /// OnMethodBegin callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <typeparam name="TTopicPartition">Type of the TopicPartition</typeparam>
        /// <typeparam name="TMessage">Type of the message</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="topicPartition">TopicPartition instance</param>
        /// <param name="message">Message instance</param>
        /// <param name="cancellationToken">CancellationToken instance</param>
        /// <returns>Calltarget state value</returns>
        public static CallTargetState OnMethodBegin<TTarget, TTopicPartition, TMessage>(TTarget instance, TTopicPartition topicPartition, TMessage message, CancellationToken cancellationToken)
            where TMessage : IMessage
        {
			var agent = Agent.Instance;

			var span = KafkaIntegration.CreateProducerSpan(
                agent,
                topicPartition.DuckCast<ITopicPartition>(),
                isTombstone: message.Value is null,
                finishOnClose: true);

            if (span is not null)
				KafkaIntegration.TryInjectHeaders<TTopicPartition, TMessage>(agent, span, message);

			return new CallTargetState(span);
        }

        /// <summary>
        /// OnAsyncMethodEnd callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <typeparam name="TResponse">Type of the response, in an async scenario will be T of Task of T</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="response">Response instance</param>
        /// <param name="exception">Exception instance in case the original code threw an exception.</param>
        /// <param name="state">Calltarget state value</param>
        /// <returns>A response value, in an async scenario will be T of Task of T</returns>
        public static TResponse OnAsyncMethodEnd<TTarget, TResponse>(TTarget instance, TResponse response, Exception exception, CallTargetState state)
        where TResponse : IDeliveryResult
        {
			var span = state.Segment;
			if (span is not null)
			{
				IDeliveryResult deliveryResult = null;
				if (exception is not null)
				{
					span.CaptureException(exception);

					var produceException = exception.DuckAs<IProduceException>();
					if (produceException is not null)
						deliveryResult = produceException.DeliveryResult;
				}
				else if (response is not null)
					deliveryResult = response;

				if (deliveryResult is not null)
				{
					span.SetLabel("partition", deliveryResult.Partition.ToString());
					span.SetLabel("offset", deliveryResult.Offset.ToString());
				}

				span.End();
			}
			return response;
        }
    }
}
