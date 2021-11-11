// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="KafkaHeadersCollectionAdapter.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Profiler.Managed.Integrations.Kafka
{
	/// <summary>
	/// A collection of headers.
	/// </summary>
	internal interface IHeadersCollection
	{
		/// <summary>
		/// Returns all header values for a specified header stored in the collection.
		/// </summary>
		/// <param name="name">The specified header to return values for.</param>
		/// <returns>Zero or more header strings.</returns>
		IEnumerable<string> GetValues(string name);

		/// <summary>
		/// Sets the value of an entry in the collection, replacing any previous values.
		/// </summary>
		/// <param name="name">The header to add to the collection.</param>
		/// <param name="value">The content of the header.</param>
		void Set(string name, string value);

		/// <summary>
		/// Adds the specified header and its value into the collection.
		/// </summary>
		/// <param name="name">The header to add to the collection.</param>
		/// <param name="value">The content of the header.</param>
		void Add(string name, string value);

		/// <summary>
		/// Removes the specified header from the collection.
		/// </summary>
		/// <param name="name">The name of the header to remove from the collection.</param>
		void Remove(string name);
	}

    internal struct KafkaHeadersCollection : IHeadersCollection
    {
		private readonly IHeaders _headers;
		private readonly IApmLogger _logger;

		public KafkaHeadersCollection(IHeaders headers, IApmLogger logger)
		{
			_headers = headers;
			_logger = logger.Scoped(nameof(KafkaHeadersCollection));
		}

        public IEnumerable<string> GetValues(string name)
        {
            // This only returns the _last_ bytes. Accessing other values is more expensive and should generally be unnecessary
            if (_headers.TryGetLastBytes(name, out var bytes))
            {
                try
                {
                    return new[] { Encoding.UTF8.GetString(bytes) };
                }
                catch (Exception ex)
                {
                    _logger.Info()?.LogException(ex, "Could not deserialize Kafka header {headerName}", name);
                }
            }

            return Enumerable.Empty<string>();
        }

        public void Set(string name, string value)
        {
            Remove(name);
            Add(name, value);
        }

        public void Add(string name, string value) => _headers.Add(name, Encoding.UTF8.GetBytes(value));

		public void Remove(string name) => _headers.Remove(name);
	}
}
