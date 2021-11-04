// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="IObscureStaticReadonlyErrorDuckType.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using Elastic.Apm.Profiler.Managed.DuckTyping;

namespace Elastic.Apm.Profiler.Managed.Tests.DuckTyping.Fields.ReferenceType.ProxiesDefinitions
{
    public interface IObscureStaticReadonlyErrorDuckType
    {
        [Duck(Name = "_publicStaticReadonlyReferenceTypeField", Kind = DuckKind.Field)]
        string PublicStaticReadonlyReferenceTypeField { get; set; }
    }
}
