// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="RabbitMQConstants.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

namespace Elastic.Apm.Profiler.Managed.Integrations.RabbitMq
{
    internal static class RabbitMqIntegration
    {
        internal const string Name = "RabbitMQ";
		// ReSharper disable once InconsistentNaming
		internal const string IBasicPropertiesTypeName = "RabbitMQ.Client.IBasicProperties";
		internal const string Subtype = "rabbitmq";

		internal static string NormalizeExchangeName(string exchange) =>
			exchange switch
			{
				null => "<unknown>",
				_ when exchange.Length == 0 => "<default>",
				_ => exchange
			};

		internal static string NormalizeQueueName(string queue) =>
			queue switch
			{
				null => "<unknown>",
				_ when queue.StartsWith("amq.gen-") => "amq.gen-*",
				_ => queue
			};
	}
}
