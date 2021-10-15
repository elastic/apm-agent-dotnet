// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="AbstractGenericsWithAttribute.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using Elastic.Apm.Profiler.Managed.DuckTyping;

namespace Elastic.Apm.Profiler.Managed.Tests.DuckTyping.Methods.ProxiesDefinitions
{
    public abstract class AbstractGenericsWithAttribute
    {
        [Duck(Name = "GetDefault", GenericParameterTypeNames = new[] { "System.Int32" })]
        public abstract int GetDefaultInt();

        [Duck(Name = "GetDefault", GenericParameterTypeNames = new[] { "System.String" })]
        public abstract string GetDefaultString();

        [Duck(Name = "Wrap", GenericParameterTypeNames = new[] { "System.Int32", "System.String" })]
        public abstract Tuple<int, string> WrapIntString(int a, string b);
    }
}
