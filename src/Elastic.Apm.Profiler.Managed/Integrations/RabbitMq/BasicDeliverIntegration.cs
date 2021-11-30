// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="BasicDeliverIntegration.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.ComponentModel;
using System.Linq;
using Elastic.Apm.Api;
using Elastic.Apm.DistributedTracing;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Profiler.Managed.CallTarget;
using Elastic.Apm.Profiler.Managed.Core;

namespace Elastic.Apm.Profiler.Managed.Integrations.RabbitMq
{
    /// <summary>
    /// RabbitMQ.Client BasicDeliver calltarget instrumentation
    /// </summary>
    [Instrument(
        Assembly = "RabbitMQ.Client",
        Type = "RabbitMQ.Client.Events.EventingBasicConsumer",
        Method = "HandleBasicDeliver",
        ReturnType = ClrTypeNames.Void,
        ParameterTypes = new[] { ClrTypeNames.String, ClrTypeNames.UInt64, ClrTypeNames.Bool, ClrTypeNames.String, ClrTypeNames.String, RabbitMqIntegration.IBasicPropertiesTypeName, ClrTypeNames.Ignore },
        MinimumVersion = "3.6.9",
        MaximumVersion = "6.*.*",
        Group = RabbitMqIntegration.Name)]
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class BasicDeliverIntegration
    {
		/// <summary>
        /// OnMethodBegin callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <typeparam name="TBasicProperties">Type of the message properties</typeparam>
        /// <typeparam name="TBody">Type of the message body</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="consumerTag">The original consumerTag argument</param>
        /// <param name="deliveryTag">The original deliveryTag argument</param>
        /// <param name="redelivered">The original redelivered argument</param>
        /// <param name="exchange">Name of the exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="basicProperties">The message properties.</param>
        /// <param name="body">The message body.</param>
        /// <returns>Calltarget state value</returns>
        public static CallTargetState OnMethodBegin<TTarget, TBasicProperties, TBody>(TTarget instance, string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey, TBasicProperties basicProperties, TBody body)
            where TBasicProperties : IBasicProperties
            where TBody : IBody // ReadOnlyMemory<byte> body in 6.0.0
		{
			var agent = Agent.Instance;
			if (agent.Tracer.CurrentTransaction is not null)
				return default;

			var matcher = WildcardMatcher.AnyMatch(agent.ConfigurationStore.CurrentSnapshot.IgnoreMessageQueues, exchange);
			if (matcher != null)
			{
				agent.Logger.Trace()
					?.Log(
						"Not tracing message from {Queue} because it matched IgnoreMessageQueues pattern {Matcher}",
						exchange,
						matcher.GetMatcher());

				return default;
			}

            // try to extract propagated context values from headers
			DistributedTracingData distributedTracingData = null;
            if (basicProperties?.Headers != null)
            {
                try
                {
					var traceParent = string.Join(",", ContextPropagation.HeadersGetter(basicProperties.Headers, TraceContext.TraceParentHeaderName));
					var traceState = ContextPropagation.HeadersGetter(basicProperties.Headers, TraceContext.TraceStateHeaderName).FirstOrDefault();
					distributedTracingData = TraceContext.TryExtractTracingData(traceParent, traceState);
				}
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error extracting propagated RabbitMQ headers.");
                }
            }

			var normalizedExchange = RabbitMqIntegration.NormalizeExchangeName(exchange);

			var transaction = agent.Tracer.StartTransaction(
				$"{RabbitMqIntegration.Name} RECEIVE from {normalizedExchange}",
				ApiConstants.TypeMessaging,
				distributedTracingData);

			transaction.Context.Message = new Message { Queue = new Queue { Name = exchange } };

			if (!string.IsNullOrEmpty(routingKey))
				transaction.Context.Message.RoutingKey = routingKey;

			transaction.SetLabel("message_size", body?.Length ?? 0);
			return new CallTargetState(transaction);
        }

        /// <summary>
        /// OnMethodEnd callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="exception">Exception instance in case the original code threw an exception.</param>
        /// <param name="state">Calltarget state value</param>
        /// <returns>A default CallTargetReturn to satisfy the CallTarget contract</returns>
        public static CallTargetReturn OnMethodEnd<TTarget>(TTarget instance, Exception exception, CallTargetState state)
        {
            state.Segment.EndCapturingException(exception);
            return CallTargetReturn.GetDefault();
        }
    }
}
