// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="KafkaConsumerCloseIntegration.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.ComponentModel;
using Elastic.Apm.Api;
using Elastic.Apm.Profiler.Managed.CallTarget;
using Elastic.Apm.Profiler.Managed.Core;

namespace Elastic.Apm.Profiler.Managed.Integrations.Kafka
{
    /// <summary>
    /// Confluent.Kafka Consumer.Consume calltarget instrumentation
    /// </summary>
    [Instrument(
        Assembly = "Confluent.Kafka",
        Type = "Confluent.Kafka.Consumer`2",
        Method = "Close",
        ReturnType = ClrTypeNames.Void,
        ParameterTypes = new string[0],
        MinimumVersion = "1.4.0",
        MaximumVersion = "1.*.*",
        Group = "Kafka")]
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class KafkaConsumerCloseIntegration
    {
        /// <summary>
        /// OnMethodBegin callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <returns>Calltarget state value</returns>
        public static CallTargetState OnMethodBegin<TTarget>(TTarget instance)
        {
            // If we are already in a consumer scope, close it.
            KafkaHelper.CloseConsumerTransaction(Agent.Instance);
            return CallTargetState.GetDefault();
        }

        /// <summary>
        /// OnMethodEnd callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="exception">Exception instance in case the original code threw an exception.</param>
        /// <param name="state">Calltarget state value</param>
        /// <returns>A response value, in an async scenario will be T of Task of T</returns>
        public static CallTargetReturn OnMethodEnd<TTarget>(TTarget instance, Exception exception, CallTargetState state) =>
			CallTargetReturn.GetDefault();
	}
}
