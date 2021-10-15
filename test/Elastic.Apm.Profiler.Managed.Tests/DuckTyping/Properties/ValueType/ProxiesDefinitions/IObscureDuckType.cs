// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="IObscureDuckType.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.Threading.Tasks;

namespace Elastic.Apm.Profiler.Managed.Tests.DuckTyping.Properties.ValueType.ProxiesDefinitions
{
    public interface IObscureDuckType
    {
        int PublicStaticGetValueType { get; }

        int InternalStaticGetValueType { get; }

        int ProtectedStaticGetValueType { get; }

        int PrivateStaticGetValueType { get; }

        // *

        int PublicStaticGetSetValueType { get; set; }

        int InternalStaticGetSetValueType { get; set; }

        int ProtectedStaticGetSetValueType { get; set; }

        int PrivateStaticGetSetValueType { get; set; }

        // *

        int PublicGetValueType { get; }

        int InternalGetValueType { get; }

        int ProtectedGetValueType { get; }

        int PrivateGetValueType { get; }

        // *

        int PublicGetSetValueType { get; set; }

        int InternalGetSetValueType { get; set; }

        int ProtectedGetSetValueType { get; set; }

        int PrivateGetSetValueType { get; set; }

        // *

        int? PublicStaticNullableInt { get; set; }

        int? PrivateStaticNullableInt { get; set; }

        int? PublicNullableInt { get; set; }

        int? PrivateNullableInt { get; set; }

        // *

        TaskStatus Status { get; set; }

        // *

        int this[int index] { get; set; }
    }
}
