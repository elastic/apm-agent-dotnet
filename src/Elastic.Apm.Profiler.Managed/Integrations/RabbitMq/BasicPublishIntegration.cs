// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="BasicPublishIntegration.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Elastic.Apm.Api;
using Elastic.Apm.DistributedTracing;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Profiler.Managed.CallTarget;
using Elastic.Apm.Profiler.Managed.Core;
using Elastic.Apm.Profiler.Managed.DuckTyping;

namespace Elastic.Apm.Profiler.Managed.Integrations.RabbitMq
{
    /// <summary>
    /// RabbitMQ.Client BasicPublish calltarget instrumentation
    /// </summary>
    [Instrument(
        Assembly = "RabbitMQ.Client",
        Type = "RabbitMQ.Client.Framing.Impl.Model",
        Method = "_Private_BasicPublish",
        ReturnType = ClrTypeNames.Void,
        ParameterTypes = new[] { ClrTypeNames.String, ClrTypeNames.String, ClrTypeNames.Bool, RabbitMqIntegration.IBasicPropertiesTypeName, ClrTypeNames.Ignore },
        MinimumVersion = "3.6.9",
        MaximumVersion = "6.*.*",
        Group = RabbitMqIntegration.Name)]
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class BasicPublishIntegration
    {
		private static readonly string[] DeliveryModeStrings = { null, "1", "2" };

        /// <summary>
        /// OnMethodBegin callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <typeparam name="TBasicProperties">Type of the message properties</typeparam>
        /// <typeparam name="TBody">Type of the message body</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="exchange">Name of the exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="mandatory">The mandatory routing flag.</param>
        /// <param name="basicProperties">The message properties.</param>
        /// <param name="body">The message body.</param>
        /// <returns>Calltarget state value</returns>
        public static CallTargetState OnMethodBegin<TTarget, TBasicProperties, TBody>(TTarget instance, string exchange, string routingKey, bool mandatory, TBasicProperties basicProperties, TBody body)
            where TBasicProperties : IBasicProperties, IDuckType
            where TBody : IBody, IDuckType // Versions < 6.0.0: TBody is byte[] // Versions >= 6.0.0: TBody is ReadOnlyMemory<byte>
		{
			var agent = Agent.Instance;
			var transaction = agent.Tracer.CurrentTransaction;
			if (transaction is null)
				return default;

			var matcher = WildcardMatcher.AnyMatch(transaction.Configuration.IgnoreMessageQueues, exchange);
			if (matcher != null)
			{
				agent.Logger.Trace()
					?.Log(
						"Not tracing message to {Queue} because it matched IgnoreMessageQueues pattern {Matcher}",
						exchange,
						matcher.GetMatcher());

				return default;
			}

			var normalizedExchange = RabbitMqIntegration.NormalizeExchangeName(exchange);
			var span = agent.Tracer.CurrentExecutionSegment()
				.StartSpan($"{RabbitMqIntegration.Name} SEND to {normalizedExchange}", ApiConstants.TypeMessaging, RabbitMqIntegration.Subtype, isExitSpan: true);

			span.Context.Message = new Message { Queue = new Queue { Name = exchange } };

			if (!string.IsNullOrEmpty(routingKey))
				span.Context.Message.RoutingKey = routingKey;

			span.SetLabel("message_size", body.Instance != null ? body.Length : 0);

			if (basicProperties.Instance != null)
            {
				if (basicProperties.IsDeliveryModePresent())
				{
					var deliveryMode = DeliveryModeStrings[0x3 & basicProperties.DeliveryMode];
					if (deliveryMode != null)
						span.SetLabel("delivery_mode", deliveryMode);
				}

				// add distributed tracing headers to the message
                basicProperties.Headers ??= new Dictionary<string, object>();
				var distributedTracingData = span.OutgoingDistributedTracingData;
				ContextPropagation.HeadersSetter(basicProperties.Headers, TraceContext.TraceParentHeaderName,
					distributedTracingData.SerializeToString());
				ContextPropagation.HeadersSetter(basicProperties.Headers, TraceContext.TraceStateHeaderName,
					distributedTracingData.TraceState.ToTextHeader());
			}

			return new CallTargetState(span);
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
