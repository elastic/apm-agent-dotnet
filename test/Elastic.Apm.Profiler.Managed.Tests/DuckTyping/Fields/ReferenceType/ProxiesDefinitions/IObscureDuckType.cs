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

namespace Elastic.Apm.Profiler.Managed.Tests.DuckTyping.Fields.ReferenceType.ProxiesDefinitions
{
    public interface IObscureDuckType
    {
        [Duck(Name = "_publicStaticReadonlyReferenceTypeField", Kind = DuckKind.Field)]
        string PublicStaticReadonlyReferenceTypeField { get; }

        [Duck(Name = "_internalStaticReadonlyReferenceTypeField", Kind = DuckKind.Field)]
        string InternalStaticReadonlyReferenceTypeField { get; }

        [Duck(Name = "_protectedStaticReadonlyReferenceTypeField", Kind = DuckKind.Field)]
        string ProtectedStaticReadonlyReferenceTypeField { get; }

        [Duck(Name = "_privateStaticReadonlyReferenceTypeField", Kind = DuckKind.Field)]
        string PrivateStaticReadonlyReferenceTypeField { get; }

        // *

        [Duck(Name = "_publicStaticReferenceTypeField", Kind = DuckKind.Field)]
        string PublicStaticReferenceTypeField { get; set; }

        [Duck(Name = "_internalStaticReferenceTypeField", Kind = DuckKind.Field)]
        string InternalStaticReferenceTypeField { get; set; }

        [Duck(Name = "_protectedStaticReferenceTypeField", Kind = DuckKind.Field)]
        string ProtectedStaticReferenceTypeField { get; set; }

        [Duck(Name = "_privateStaticReferenceTypeField", Kind = DuckKind.Field)]
        string PrivateStaticReferenceTypeField { get; set; }

        // *

        [Duck(Name = "_publicReadonlyReferenceTypeField", Kind = DuckKind.Field)]
        string PublicReadonlyReferenceTypeField { get; }

        [Duck(Name = "_internalReadonlyReferenceTypeField", Kind = DuckKind.Field)]
        string InternalReadonlyReferenceTypeField { get; }

        [Duck(Name = "_protectedReadonlyReferenceTypeField", Kind = DuckKind.Field)]
        string ProtectedReadonlyReferenceTypeField { get; }

        [Duck(Name = "_privateReadonlyReferenceTypeField", Kind = DuckKind.Field)]
        string PrivateReadonlyReferenceTypeField { get; }

        // *

        [Duck(Name = "_publicReferenceTypeField", Kind = DuckKind.Field)]
        string PublicReferenceTypeField { get; set; }

        [Duck(Name = "_internalReferenceTypeField", Kind = DuckKind.Field)]
        string InternalReferenceTypeField { get; set; }

        [Duck(Name = "_protectedReferenceTypeField", Kind = DuckKind.Field)]
        string ProtectedReferenceTypeField { get; set; }

        [Duck(Name = "_privateReferenceTypeField", Kind = DuckKind.Field)]
        string PrivateReferenceTypeField { get; set; }
    }
}
