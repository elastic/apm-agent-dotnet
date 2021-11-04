// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="ObscureDuckTypeVirtualClass.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.Threading.Tasks;

namespace Elastic.Apm.Profiler.Managed.Tests.DuckTyping.Properties.ValueType.ProxiesDefinitions
{
    public class ObscureDuckTypeVirtualClass
    {
        public virtual int PublicStaticGetValueType { get; }

        public virtual int InternalStaticGetValueType { get; }

        public virtual int ProtectedStaticGetValueType { get; }

        public virtual int PrivateStaticGetValueType { get; }

        // *

        public virtual int PublicStaticGetSetValueType { get; set; }

        public virtual int InternalStaticGetSetValueType { get; set; }

        public virtual int ProtectedStaticGetSetValueType { get; set; }

        public virtual int PrivateStaticGetSetValueType { get; set; }

        // *

        public virtual int PublicGetValueType { get; }

        public virtual int InternalGetValueType { get; }

        public virtual int ProtectedGetValueType { get; }

        public virtual int PrivateGetValueType { get; }

        // *

        public virtual int PublicGetSetValueType { get; set; }

        public virtual int InternalGetSetValueType { get; set; }

        public virtual int ProtectedGetSetValueType { get; set; }

        public virtual int PrivateGetSetValueType { get; set; }

        // *

        public virtual int? PublicStaticNullableInt { get; set; }

        public virtual int? PrivateStaticNullableInt { get; set; }

        public virtual int? PublicNullableInt { get; set; }

        public virtual int? PrivateNullableInt { get; set; }

        // *

        public virtual TaskStatus Status
        {
            get => default;
            set { }
        }

        // *

        public virtual int this[int index]
        {
            get => default;
            set { }
        }
    }
}
