// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="ObscureDuckTypeVirtualClass.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

namespace Elastic.Apm.Profiler.Managed.Tests.DuckTyping.Properties.ReferenceType.ProxiesDefinitions
{
    public class ObscureDuckTypeVirtualClass
    {
        public virtual string PublicStaticGetReferenceType { get; }

        public virtual string InternalStaticGetReferenceType { get; }

        public virtual string ProtectedStaticGetReferenceType { get; }

        public virtual string PrivateStaticGetReferenceType { get; }

        // *

        public virtual string PublicStaticGetSetReferenceType { get; set; }

        public virtual string InternalStaticGetSetReferenceType { get; set; }

        public virtual string ProtectedStaticGetSetReferenceType { get; set; }

        public virtual string PrivateStaticGetSetReferenceType { get; set; }

        // *

        public virtual string PublicGetReferenceType { get; }

        public virtual string InternalGetReferenceType { get; }

        public virtual string ProtectedGetReferenceType { get; }

        public virtual string PrivateGetReferenceType { get; }

        // *

        public virtual string PublicGetSetReferenceType { get; set; }

        public virtual string InternalGetSetReferenceType { get; set; }

        public virtual string ProtectedGetSetReferenceType { get; set; }

        public virtual string PrivateGetSetReferenceType { get; set; }

        // *

        public virtual string this[string index]
        {
            get => default;
            set { }
        }
    }
}
