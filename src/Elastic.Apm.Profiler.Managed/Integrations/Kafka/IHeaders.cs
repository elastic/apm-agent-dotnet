// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="IHeaders.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.ComponentModel;

namespace Elastic.Apm.Profiler.Managed.Integrations.Kafka
{
    /// <summary>
    /// Headers interface for duck-typing
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IHeaders
    {
        /// <summary>
        /// Adds a header to the collection
        /// </summary>
        /// <param name="key">The header's key value</param>
        /// <param name="val">The value of the header. May be null. Format strings as UTF8</param>
        public void Add(string key, byte[] val);

        /// <summary>
        ///     Removes all headers for the given key.
        /// </summary>
        /// <param name="key">The key to remove all headers for</param>
        public void Remove(string key);

        /// <summary>
        ///     Try to get the value of the latest header with the specified key.
        /// </summary>
        /// <param name="key">
        ///     The key to get the associated value of.
        /// </param>
        /// <param name="lastHeader">
        ///     The value of the latest element in the collection with the
        ///     specified key, if a header with that key was present in the
        ///     collection.
        /// </param>
        /// <returns>
        ///     true if the a value with the specified key was present in
        ///     the collection, false otherwise.
        /// </returns>
        public bool TryGetLastBytes(string key, out byte[] lastHeader);
    }
}
