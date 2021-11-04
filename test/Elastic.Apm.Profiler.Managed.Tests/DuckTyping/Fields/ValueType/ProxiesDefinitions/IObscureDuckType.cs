// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="IObscureDuckType.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using Elastic.Apm.Profiler.Managed.DuckTyping;

namespace Elastic.Apm.Profiler.Managed.Tests.DuckTyping.Fields.ValueType.ProxiesDefinitions
{
    public interface IObscureDuckType
    {
        [Duck(Name = "_publicStaticReadonlyValueTypeField", Kind = DuckKind.Field)]
        int PublicStaticReadonlyValueTypeField { get; }

        [Duck(Name = "_internalStaticReadonlyValueTypeField", Kind = DuckKind.Field)]
        int InternalStaticReadonlyValueTypeField { get; }

        [Duck(Name = "_protectedStaticReadonlyValueTypeField", Kind = DuckKind.Field)]
        int ProtectedStaticReadonlyValueTypeField { get; }

        [Duck(Name = "_privateStaticReadonlyValueTypeField", Kind = DuckKind.Field)]
        int PrivateStaticReadonlyValueTypeField { get; }

        // *

        [Duck(Name = "_publicStaticValueTypeField", Kind = DuckKind.Field)]
        int PublicStaticValueTypeField { get; set; }

        [Duck(Name = "_internalStaticValueTypeField", Kind = DuckKind.Field)]
        int InternalStaticValueTypeField { get; set; }

        [Duck(Name = "_protectedStaticValueTypeField", Kind = DuckKind.Field)]
        int ProtectedStaticValueTypeField { get; set; }

        [Duck(Name = "_privateStaticValueTypeField", Kind = DuckKind.Field)]
        int PrivateStaticValueTypeField { get; set; }

        // *

        [Duck(Name = "_publicReadonlyValueTypeField", Kind = DuckKind.Field)]
        int PublicReadonlyValueTypeField { get; }

        [Duck(Name = "_internalReadonlyValueTypeField", Kind = DuckKind.Field)]
        int InternalReadonlyValueTypeField { get; }

        [Duck(Name = "_protectedReadonlyValueTypeField", Kind = DuckKind.Field)]
        int ProtectedReadonlyValueTypeField { get; }

        [Duck(Name = "_privateReadonlyValueTypeField", Kind = DuckKind.Field)]
        int PrivateReadonlyValueTypeField { get; }

        // *

        [Duck(Name = "_publicValueTypeField", Kind = DuckKind.Field)]
        int PublicValueTypeField { get; set; }

        [Duck(Name = "_internalValueTypeField", Kind = DuckKind.Field)]
        int InternalValueTypeField { get; set; }

        [Duck(Name = "_protectedValueTypeField", Kind = DuckKind.Field)]
        int ProtectedValueTypeField { get; set; }

        [Duck(Name = "_privateValueTypeField", Kind = DuckKind.Field)]
        int PrivateValueTypeField { get; set; }

        // *

        [Duck(Name = "_publicStaticNullableIntField", Kind = DuckKind.Field)]
        int? PublicStaticNullableIntField { get; set; }

        [Duck(Name = "_privateStaticNullableIntField", Kind = DuckKind.Field)]
        int? PrivateStaticNullableIntField { get; set; }

        [Duck(Name = "_publicNullableIntField", Kind = DuckKind.Field)]
        int? PublicNullableIntField { get; set; }

        [Duck(Name = "_privateNullableIntField", Kind = DuckKind.Field)]
        int? PrivateNullableIntField { get; set; }
    }
}
