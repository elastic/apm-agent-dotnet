// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="BasicGetIntegration.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.ComponentModel;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;
using Elastic.Apm.Profiler.Managed.CallTarget;
using Elastic.Apm.Profiler.Managed.Core;
using Elastic.Apm.Profiler.Managed.DuckTyping;

namespace Elastic.Apm.Profiler.Managed.Integrations.RabbitMq
{
    /// <summary>
    /// RabbitMQ.Client BasicGet calltarget instrumentation
    /// </summary>
    [Instrument(
        Assembly = "RabbitMQ.Client",
        Type = "RabbitMQ.Client.Impl.ModelBase",
        Method = "BasicGet",
        ReturnType = "RabbitMQ.Client.BasicGetResult",
        ParameterTypes = new[] { ClrTypeNames.String, ClrTypeNames.Bool },
        MinimumVersion = "3.6.9",
        MaximumVersion = "6.*.*",
        Group = RabbitMqIntegration.Name)]
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class BasicGetIntegration
    {
		/// <summary>
        /// OnMethodBegin callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="queue">The queue name of the message</param>
        /// <param name="autoAck">The original autoAck argument</param>
        /// <returns>Calltarget state value</returns>
        public static CallTargetState OnMethodBegin<TTarget>(TTarget instance, string queue, bool autoAck) =>
			new CallTargetState(segment: null, state: queue, startTime: DateTimeOffset.UtcNow);

		/// <summary>
        /// OnMethodEnd callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <typeparam name="TResult">Type of the BasicGetResult</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="basicGetResult">BasicGetResult instance</param>
        /// <param name="exception">Exception instance in case the original code threw an exception.</param>
        /// <param name="state">Calltarget state value</param>
        /// <returns>A default CallTargetReturn to satisfy the CallTarget contract</returns>
        public static CallTargetReturn<TResult> OnMethodEnd<TTarget, TResult>(TTarget instance, TResult basicGetResult, Exception exception, CallTargetState state)
            where TResult : IBasicGetResult, IDuckType
        {
            var queue = (string)state.State;
            var startTime = state.StartTime;
			var agent = Agent.Instance;
			var transaction = agent.Tracer.CurrentTransaction;
			if (transaction is null)
				return new CallTargetReturn<TResult>(basicGetResult);

			var matcher = WildcardMatcher.AnyMatch(transaction.Configuration.IgnoreMessageQueues, queue);
			if (matcher != null)
			{
				agent.Logger.Trace()
					?.Log(
						"Not tracing message from {Queue} because it matched IgnoreMessageQueues pattern {Matcher}",
						queue,
						matcher.GetMatcher());

				return new CallTargetReturn<TResult>(basicGetResult);
			}

			// check if there is an actual instance of the duck-typed type. RabbitMQ client can return null when the server
			// answers that there are no messages available
			var instanceNotNull = basicGetResult.Instance != null;
			if (instanceNotNull)
			{
				var normalizedExchange = RabbitMqIntegration.NormalizeExchangeName(basicGetResult.Exchange);
				matcher = WildcardMatcher.AnyMatch(transaction.Configuration.IgnoreMessageQueues, normalizedExchange);
				if (matcher != null)
				{
					agent.Logger.Trace()
						?.Log(
							"Not tracing message from {Queue} because it matched IgnoreMessageQueues pattern {Matcher}",
							normalizedExchange,
							matcher.GetMatcher());

					return new CallTargetReturn<TResult>(basicGetResult);
				}
			}

			var normalizedQueue = RabbitMqIntegration.NormalizeQueueName(queue);
			var span = agent.Tracer.CurrentExecutionSegment().StartSpan(
				$"{RabbitMqIntegration.Name} POLL from {normalizedQueue}",
				ApiConstants.TypeMessaging,
				RabbitMqIntegration.Subtype);

			if (startTime.HasValue && span is Span realSpan)
				realSpan.Timestamp = TimeUtils.ToTimestamp(startTime.Value);

			span.Context.Message = new Message { Queue = new Queue { Name = queue } };

			if (instanceNotNull)
			{
				span.SetLabel("message_size", basicGetResult.Body?.Length ?? 0);

				if (!string.IsNullOrEmpty(basicGetResult.RoutingKey))
					span.Context.Message.RoutingKey = basicGetResult.RoutingKey;
			}

			span.EndCapturingException(exception);
			return new CallTargetReturn<TResult>(basicGetResult);
        }
	}
}
