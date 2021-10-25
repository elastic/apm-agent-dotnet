// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="ObscureDuckTypeAbstractClass.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using Elastic.Apm.Profiler.Managed.DuckTyping;

namespace Elastic.Apm.Profiler.Managed.Tests.DuckTyping.Methods.ProxiesDefinitions
{
    public abstract class ObscureDuckTypeAbstractClass
    {
        public abstract int Sum(int a, int b);

        public abstract float Sum(float a, float b);

        public abstract double Sum(double a, double b);

        public abstract short Sum(short a, short b);

        public abstract TestEnum2 ShowEnum(TestEnum2 val);

        public abstract object InternalSum(int a, int b);

        [Duck(ParameterTypeNames = new string[] { "System.String", "Elastic.Apm.Profiler.Managed.Tests.DuckTyping.ObscureObject+DummyFieldObject, Elastic.Apm.Profiler.Managed.Tests" })]
        public abstract void Add(string name, object obj);

        public abstract void Add(string name, int obj);

        public abstract void Add(string name, string obj = "none");

        public abstract void Pow2(ref int value);

        public abstract void GetOutput(out int value);

        [Duck(Name = "GetOutput")]
        public abstract void GetOutputObject(out object value);

        public abstract bool TryGetObscure(out IDummyFieldObject obj);

        [Duck(Name = "TryGetObscure")]
        public abstract bool TryGetObscureObject(out object obj);

        public abstract void GetReference(ref int value);

        [Duck(Name = "GetReference")]
        public abstract void GetReferenceObject(ref object value);

        public abstract bool TryGetReference(ref IDummyFieldObject obj);

        [Duck(Name = "TryGetReference")]
        public abstract bool TryGetReferenceObject(ref object obj);

        public abstract bool TryGetPrivateObscure(out IDummyFieldObject obj);

        [Duck(Name = "TryGetPrivateObscure")]
        public abstract bool TryGetPrivateObscureObject(out object obj);

        public abstract bool TryGetPrivateReference(ref IDummyFieldObject obj);

        [Duck(Name = "TryGetPrivateReference")]
        public abstract bool TryGetPrivateReferenceObject(ref object obj);

        public void NormalMethod()
        {
            // .
        }

        public abstract IDummyFieldObject Bypass(IDummyFieldObject obj);
    }
}
