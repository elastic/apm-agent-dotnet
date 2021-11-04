// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="ObscureDuckTypeVirtualClass.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using Elastic.Apm.Profiler.Managed.DuckTyping;

namespace Elastic.Apm.Profiler.Managed.Tests.DuckTyping.Fields.ReferenceType.ProxiesDefinitions
{
    public class ObscureDuckTypeVirtualClass
    {
        [Duck(Name = "_publicStaticReadonlyReferenceTypeField", Kind = DuckKind.Field)]
        public virtual string PublicStaticReadonlyReferenceTypeField { get; }

        [Duck(Name = "_internalStaticReadonlyReferenceTypeField", Kind = DuckKind.Field)]
        public virtual string InternalStaticReadonlyReferenceTypeField { get; }

        [Duck(Name = "_protectedStaticReadonlyReferenceTypeField", Kind = DuckKind.Field)]
        public virtual string ProtectedStaticReadonlyReferenceTypeField { get; }

        [Duck(Name = "_privateStaticReadonlyReferenceTypeField", Kind = DuckKind.Field)]
        public virtual string PrivateStaticReadonlyReferenceTypeField { get; }

        // *

        [Duck(Name = "_publicStaticReferenceTypeField", Kind = DuckKind.Field)]
        public virtual string PublicStaticReferenceTypeField { get; set; }

        [Duck(Name = "_internalStaticReferenceTypeField", Kind = DuckKind.Field)]
        public virtual string InternalStaticReferenceTypeField { get; set; }

        [Duck(Name = "_protectedStaticReferenceTypeField", Kind = DuckKind.Field)]
        public virtual string ProtectedStaticReferenceTypeField { get; set; }

        [Duck(Name = "_privateStaticReferenceTypeField", Kind = DuckKind.Field)]
        public virtual string PrivateStaticReferenceTypeField { get; set; }

        // *

        [Duck(Name = "_publicReadonlyReferenceTypeField", Kind = DuckKind.Field)]
        public virtual string PublicReadonlyReferenceTypeField { get; }

        [Duck(Name = "_internalReadonlyReferenceTypeField", Kind = DuckKind.Field)]
        public virtual string InternalReadonlyReferenceTypeField { get; }

        [Duck(Name = "_protectedReadonlyReferenceTypeField", Kind = DuckKind.Field)]
        public virtual string ProtectedReadonlyReferenceTypeField { get; }

        [Duck(Name = "_privateReadonlyReferenceTypeField", Kind = DuckKind.Field)]
        public virtual string PrivateReadonlyReferenceTypeField { get; }

        // *

        [Duck(Name = "_publicReferenceTypeField", Kind = DuckKind.Field)]
        public virtual string PublicReferenceTypeField { get; set; }

        [Duck(Name = "_internalReferenceTypeField", Kind = DuckKind.Field)]
        public virtual string InternalReferenceTypeField { get; set; }

        [Duck(Name = "_protectedReferenceTypeField", Kind = DuckKind.Field)]
        public virtual string ProtectedReferenceTypeField { get; set; }

        [Duck(Name = "_privateReferenceTypeField", Kind = DuckKind.Field)]
        public virtual string PrivateReferenceTypeField { get; set; }
    }
}
