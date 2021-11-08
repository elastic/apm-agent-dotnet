// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="ObscureDuckTypeAbstractClass.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.Threading.Tasks;

namespace Elastic.Apm.Profiler.Managed.Tests.DuckTyping.Properties.ValueType.ProxiesDefinitions
{
    public abstract class ObscureDuckTypeAbstractClass
    {
        public abstract int PublicStaticGetValueType { get; }

        public abstract int InternalStaticGetValueType { get; }

        public abstract int ProtectedStaticGetValueType { get; }

        public abstract int PrivateStaticGetValueType { get; }

        // *

        public abstract int PublicStaticGetSetValueType { get; set; }

        public abstract int InternalStaticGetSetValueType { get; set; }

        public abstract int ProtectedStaticGetSetValueType { get; set; }

        public abstract int PrivateStaticGetSetValueType { get; set; }

        // *

        public abstract int PublicGetValueType { get; }

        public abstract int InternalGetValueType { get; }

        public abstract int ProtectedGetValueType { get; }

        public abstract int PrivateGetValueType { get; }

        // *

        public abstract int PublicGetSetValueType { get; set; }

        public abstract int InternalGetSetValueType { get; set; }

        public abstract int ProtectedGetSetValueType { get; set; }

        public abstract int PrivateGetSetValueType { get; set; }

        // *

        public abstract int? PublicStaticNullableInt { get; set; }

        public abstract int? PrivateStaticNullableInt { get; set; }

        public abstract int? PublicNullableInt { get; set; }

        public abstract int? PrivateNullableInt { get; set; }

        // *

        public abstract TaskStatus Status { get; set; }

        // *

        public abstract int this[int index] { get; set; }
    }
}
