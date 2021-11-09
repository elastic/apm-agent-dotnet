// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="ObscureDuckTypeVirtualClass.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

namespace Elastic.Apm.Profiler.Managed.Tests.DuckTyping.Properties.TypeChaining.ProxiesDefinitions
{
    public class ObscureDuckTypeVirtualClass
    {
        public virtual IDummyFieldObject PublicStaticGetSelfType { get; }

        public virtual IDummyFieldObject InternalStaticGetSelfType { get; }

        public virtual IDummyFieldObject ProtectedStaticGetSelfType { get; }

        public virtual IDummyFieldObject PrivateStaticGetSelfType { get; }

        // *

        public virtual IDummyFieldObject PublicStaticGetSetSelfType { get; set; }

        public virtual IDummyFieldObject InternalStaticGetSetSelfType { get; set; }

        public virtual IDummyFieldObject ProtectedStaticGetSetSelfType { get; set; }

        public virtual IDummyFieldObject PrivateStaticGetSetSelfType { get; set; }

        // *

        public virtual IDummyFieldObject PublicGetSelfType { get; }

        public virtual IDummyFieldObject InternalGetSelfType { get; }

        public virtual IDummyFieldObject ProtectedGetSelfType { get; }

        public virtual IDummyFieldObject PrivateGetSelfType { get; }

        // *

        public virtual IDummyFieldObject PublicGetSetSelfType { get; set; }

        public virtual IDummyFieldObject InternalGetSetSelfType { get; set; }

        public virtual IDummyFieldObject ProtectedGetSetSelfType { get; set; }

        public virtual IDummyFieldObject PrivateGetSetSelfType { get; set; }
    }
}
