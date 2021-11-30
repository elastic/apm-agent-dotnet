// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="KafkaConsumerConsumeIntegration.cs" company="Datadog">
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
    /// Confluent.Kafka Consumer.Consume calltarget instrumentation
    /// </summary>
    [Instrument(
        Assembly = "Confluent.Kafka",
        Type = "Confluent.Kafka.Consumer`2",
        Method = "Consume",
        ReturnType = KafkaIntegration.ConsumeResultTypeName,
        ParameterTypes = new[] { ClrTypeNames.Int32 },
        MinimumVersion = "1.4.0",
        MaximumVersion = "1.*.*",
        Group = KafkaIntegration.Name)]
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class KafkaConsumerConsumeIntegration
    {
        /// <summary>
        /// OnMethodBegin callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="millisecondsTimeout">The maximum period of time the call may block.</param>
        /// <returns>Calltarget state value</returns>
        public static CallTargetState OnMethodBegin<TTarget>(TTarget instance, int millisecondsTimeout)
        {
            // If we are already in a consumer scope, close it, and start a new one on method exit.
            KafkaIntegration.CloseConsumerTransaction(Agent.Instance);
            return CallTargetState.GetDefault();
        }

        /// <summary>
        /// OnMethodEnd callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <typeparam name="TResponse">Type of the response</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="response">Response instance</param>
        /// <param name="exception">Exception instance in case the original code threw an exception.</param>
        /// <param name="state">Calltarget state value</param>
        /// <returns>A response value, in an async scenario will be T of Task of T</returns>
        public static CallTargetReturn<TResponse> OnMethodEnd<TTarget, TResponse>(TTarget instance, TResponse response, Exception exception, CallTargetState state)
            where TResponse : IConsumeResult, IDuckType
        {
            IConsumeResult consumeResult = response.Instance is not null
				? response
				: null;

            if (exception is not null && exception.TryDuckCast<IConsumeException>(out var consumeException))
				consumeResult = consumeException.ConsumerRecord;

			if (consumeResult is not null)
			{
				// creates a transaction and ends it on the next call to any of
				// Consumer.Consume(), Consumer.Close() Consumer.Dispose() or Consumer.Unsubscribe() call
                var transaction = KafkaIntegration.CreateConsumerTransaction(
                    Agent.Instance,
                    consumeResult.Topic,
                    consumeResult.Partition,
                    consumeResult.Offset,
                    consumeResult.Message);

				if (exception is not null)
					transaction.CaptureException(exception);
			}

            return new CallTargetReturn<TResponse>(response);
        }
    }
}
