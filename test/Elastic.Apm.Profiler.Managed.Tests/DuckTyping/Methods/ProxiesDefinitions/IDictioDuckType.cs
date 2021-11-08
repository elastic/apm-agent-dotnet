// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="IDictioDuckType.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.Collections.Generic;
using Elastic.Apm.Profiler.Managed.DuckTyping;

namespace Elastic.Apm.Profiler.Managed.Tests.DuckTyping.Methods.ProxiesDefinitions
{
    public interface IDictioDuckType
    {
        public ICollection<string> Keys { get; }

        public ICollection<string> Values { get; }

        int Count { get; }

        public string this[string key] { get; set; }

        void Add(string key, string value);

        bool ContainsKey(string key);

        bool Remove(string key);

        bool TryGetValue(string key, out string value);

        [Duck(Name = "TryGetValue")]
        bool TryGetValueInObject(string key, out object value);

        [Duck(Name = "TryGetValue")]
        bool TryGetValueInDuckChaining(string key, out IDictioValue value);

        IEnumerator<KeyValuePair<string, string>> GetEnumerator();
    }
}
