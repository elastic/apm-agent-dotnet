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

namespace Elastic.Apm.Profiler.Managed.Tests.DuckTyping.Properties.ReferenceType.ProxiesDefinitions
{
#pragma warning disable 649

    [DuckCopy]
    internal struct ObscureDuckTypeStruct
    {
        public readonly string ReadonlyFieldIgnored;

        public string PublicStaticGetReferenceType;
        public string InternalStaticGetReferenceType;
        public string ProtectedStaticGetReferenceType;
        public string PrivateStaticGetReferenceType;

        public string PublicStaticGetSetReferenceType;
        public string InternalStaticGetSetReferenceType;
        public string ProtectedStaticGetSetReferenceType;
        public string PrivateStaticGetSetReferenceType;

        public string PublicGetReferenceType;
        public string InternalGetReferenceType;
        public string ProtectedGetReferenceType;
        public string PrivateGetReferenceType;

        public string PublicGetSetReferenceType;
        public string InternalGetSetReferenceType;
        public string ProtectedGetSetReferenceType;
        public string PrivateGetSetReferenceType;
    }
}
