// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="ObscureDuckTypeStruct.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using Elastic.Apm.Profiler.Managed.DuckTyping;

namespace Elastic.Apm.Profiler.Managed.Tests.DuckTyping.Properties.ValueType.ProxiesDefinitions
{
#pragma warning disable 649

    [DuckCopy]
    internal struct ObscureDuckTypeStruct
    {
        public int PublicStaticGetValueType;
        public int InternalStaticGetValueType;
        public int ProtectedStaticGetValueType;
        public int PrivateStaticGetValueType;

        public int PublicStaticGetSetValueType;
        public int InternalStaticGetSetValueType;
        public int ProtectedStaticGetSetValueType;
        public int PrivateStaticGetSetValueType;

        public int PublicGetValueType;
        public int InternalGetValueType;
        public int ProtectedGetValueType;
        public int PrivateGetValueType;

        public int PublicGetSetValueType;
        public int InternalGetSetValueType;
        public int ProtectedGetSetValueType;
        public int PrivateGetSetValueType;
    }
}
