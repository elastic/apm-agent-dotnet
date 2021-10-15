// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="MethodTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using Elastic.Apm.Profiler.Managed.DuckTyping;
using Elastic.Apm.Profiler.Managed.Tests.DuckTyping.Methods.ProxiesDefinitions;
using Xunit;

namespace Elastic.Apm.Profiler.Managed.Tests.DuckTyping.Methods
{
    public class MethodTests
    {
        public static IEnumerable<object[]> Data()
        {
            return new[]
            {
                new object[] { ObscureObject.GetPropertyPublicObject() },
                new object[] { ObscureObject.GetPropertyInternalObject() },
                new object[] { ObscureObject.GetPropertyPrivateObject() },
            };
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void ReturnMethods(object obscureObject)
        {
            var duckInterface = obscureObject.DuckCast<IObscureDuckType>();
            var duckAbstract = obscureObject.DuckCast<ObscureDuckTypeAbstractClass>();
            var duckVirtual = obscureObject.DuckCast<ObscureDuckTypeVirtualClass>();

            // Integers
            Assert.Equal(20, duckInterface.Sum(10, 10));
            Assert.Equal(20, duckAbstract.Sum(10, 10));
            Assert.Equal(20, duckVirtual.Sum(10, 10));

            // Float
            Assert.Equal(20f, duckInterface.Sum(10f, 10f));
            Assert.Equal(20f, duckAbstract.Sum(10f, 10f));
            Assert.Equal(20f, duckVirtual.Sum(10f, 10f));

            // Double
            Assert.Equal(20d, duckInterface.Sum(10d, 10d));
            Assert.Equal(20d, duckAbstract.Sum(10d, 10d));
            Assert.Equal(20d, duckVirtual.Sum(10d, 10d));

            // Short
            Assert.Equal((short)20, duckInterface.Sum((short)10, (short)10));
            Assert.Equal((short)20, duckAbstract.Sum((short)10, (short)10));
            Assert.Equal((short)20, duckVirtual.Sum((short)10, (short)10));

            // Enum
            Assert.Equal(TestEnum2.Segundo, duckInterface.ShowEnum(TestEnum2.Segundo));
            Assert.Equal(TestEnum2.Segundo, duckAbstract.ShowEnum(TestEnum2.Segundo));
            Assert.Equal(TestEnum2.Segundo, duckVirtual.ShowEnum(TestEnum2.Segundo));

            // Internal Sum
            Assert.Equal(20, duckInterface.InternalSum(10, 10));
            Assert.Equal(20, duckAbstract.InternalSum(10, 10));
            Assert.Equal(20, duckVirtual.InternalSum(10, 10));

            var dummy = new ObscureObject.DummyFieldObject { MagicNumber = 987654 };
            var dummyInt = dummy.DuckCast<IDummyFieldObject>();
            Assert.Equal(dummy.MagicNumber, duckInterface.Bypass(dummyInt).MagicNumber);
            Assert.Equal(dummy.MagicNumber, duckAbstract.Bypass(dummyInt).MagicNumber);
            Assert.Equal(dummy.MagicNumber, duckVirtual.Bypass(dummyInt).MagicNumber);
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void VoidMethods(object obscureObject)
        {
            var duckInterface = obscureObject.DuckCast<IObscureDuckType>();
            var duckAbstract = obscureObject.DuckCast<ObscureDuckTypeAbstractClass>();
            var duckVirtual = obscureObject.DuckCast<ObscureDuckTypeVirtualClass>();

            // Void with object
            duckInterface.Add("Key01", new ObscureObject.DummyFieldObject());
            duckAbstract.Add("Key02", new ObscureObject.DummyFieldObject());
            duckVirtual.Add("Key03", new ObscureObject.DummyFieldObject());

            // Void with int
            duckInterface.Add("KeyInt01", 42);
            duckAbstract.Add("KeyInt02", 42);
            duckVirtual.Add("KeyInt03", 42);

            // Void with string
            duckInterface.Add("KeyString01", "Value01");
            duckAbstract.Add("KeyString02", "Value02");
            duckVirtual.Add("KeyString03", "Value03");
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void RefParametersMethods(object obscureObject)
        {
            var duckInterface = obscureObject.DuckCast<IObscureDuckType>();
            var duckAbstract = obscureObject.DuckCast<ObscureDuckTypeAbstractClass>();
            var duckVirtual = obscureObject.DuckCast<ObscureDuckTypeVirtualClass>();

            // Ref parameter
            int value = 4;
            duckInterface.Pow2(ref value);
            duckAbstract.Pow2(ref value);
            duckVirtual.Pow2(ref value);
            Assert.Equal(65536, value);

            value = 4;
            duckInterface.GetReference(ref value);
            duckAbstract.GetReference(ref value);
            duckVirtual.GetReference(ref value);
            Assert.Equal(65536, value);

            // Ref object parameter
            object objValue = 4;
            object objValue2 = objValue;
            duckInterface.GetReferenceObject(ref objValue);
            duckAbstract.GetReferenceObject(ref objValue);
            duckVirtual.GetReferenceObject(ref objValue);
            Assert.Equal(65536, (int)objValue);

            // Ref DuckType
            IDummyFieldObject refDuckType;
            refDuckType = null;
            Assert.True(duckInterface.TryGetReference(ref refDuckType));
            Assert.Equal(100, refDuckType.MagicNumber);
            Assert.True(duckAbstract.TryGetReference(ref refDuckType));
            Assert.Equal(101, refDuckType.MagicNumber);
            Assert.True(duckVirtual.TryGetReference(ref refDuckType));
            Assert.Equal(102, refDuckType.MagicNumber);

            // Ref object
            object refObject;
            refObject = null;
            Assert.True(duckInterface.TryGetReferenceObject(ref refObject));
            Assert.Equal(100, refObject.DuckCast<IDummyFieldObject>().MagicNumber);
            Assert.True(duckAbstract.TryGetReferenceObject(ref refObject));
            Assert.Equal(101, refObject.DuckCast<IDummyFieldObject>().MagicNumber);
            Assert.True(duckVirtual.TryGetReferenceObject(ref refObject));
            Assert.Equal(102, refObject.DuckCast<IDummyFieldObject>().MagicNumber);

            // Private internal parameter type with duck type output
            refDuckType = null;
            Assert.True(duckInterface.TryGetPrivateReference(ref refDuckType));
            Assert.Equal(100, refDuckType.MagicNumber);
            Assert.True(duckAbstract.TryGetPrivateReference(ref refDuckType));
            Assert.Equal(101, refDuckType.MagicNumber);
            Assert.True(duckVirtual.TryGetPrivateReference(ref refDuckType));
            Assert.Equal(102, refDuckType.MagicNumber);

            // Private internal parameter type object output
            refObject = null;
            Assert.True(duckInterface.TryGetPrivateReferenceObject(ref refObject));
            Assert.Equal(100, refObject.DuckCast<IDummyFieldObject>().MagicNumber);
            Assert.True(duckAbstract.TryGetPrivateReferenceObject(ref refObject));
            Assert.Equal(101, refObject.DuckCast<IDummyFieldObject>().MagicNumber);
            Assert.True(duckVirtual.TryGetPrivateReferenceObject(ref refObject));
            Assert.Equal(102, refObject.DuckCast<IDummyFieldObject>().MagicNumber);
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void OutParametersMethods(object obscureObject)
        {
            var duckInterface = obscureObject.DuckCast<IObscureDuckType>();
            var duckAbstract = obscureObject.DuckCast<ObscureDuckTypeAbstractClass>();
            var duckVirtual = obscureObject.DuckCast<ObscureDuckTypeVirtualClass>();

            // Out parameter
            int outValue;
            duckInterface.GetOutput(out outValue);
            Assert.Equal(42, outValue);
            duckAbstract.GetOutput(out outValue);
            Assert.Equal(42, outValue);
            duckVirtual.GetOutput(out outValue);
            Assert.Equal(42, outValue);

            // Out object parameter
            object outObjectValue;
            duckInterface.GetOutputObject(out outObjectValue);
            Assert.Equal(42, (int)outObjectValue);
            duckAbstract.GetOutputObject(out outObjectValue);
            Assert.Equal(42, (int)outObjectValue);
            duckVirtual.GetOutputObject(out outObjectValue);
            Assert.Equal(42, (int)outObjectValue);

            // Duck type output
            IDummyFieldObject outDuckType;
            Assert.True(duckInterface.TryGetObscure(out outDuckType));
            Assert.NotNull(outDuckType);
            Assert.Equal(99, outDuckType.MagicNumber);

            Assert.True(duckAbstract.TryGetObscure(out outDuckType));
            Assert.NotNull(outDuckType);
            Assert.Equal(99, outDuckType.MagicNumber);

            Assert.True(duckVirtual.TryGetObscure(out outDuckType));
            Assert.NotNull(outDuckType);
            Assert.Equal(99, outDuckType.MagicNumber);

            // Object output
            object outObject;
            Assert.True(duckInterface.TryGetObscureObject(out outObject));
            Assert.NotNull(outObject);
            Assert.Equal(99, outObject.DuckCast<IDummyFieldObject>().MagicNumber);

            Assert.True(duckAbstract.TryGetObscureObject(out outObject));
            Assert.NotNull(outObject);
            Assert.Equal(99, outObject.DuckCast<IDummyFieldObject>().MagicNumber);

            Assert.True(duckVirtual.TryGetObscureObject(out outObject));
            Assert.NotNull(outObject);
            Assert.Equal(99, outObject.DuckCast<IDummyFieldObject>().MagicNumber);

            // Private internal parameter type with duck type output
            Assert.True(duckInterface.TryGetPrivateObscure(out outDuckType));
            Assert.NotNull(outDuckType);
            Assert.Equal(99, outDuckType.MagicNumber);

            Assert.True(duckAbstract.TryGetPrivateObscure(out outDuckType));
            Assert.NotNull(outDuckType);
            Assert.Equal(99, outDuckType.MagicNumber);

            Assert.True(duckVirtual.TryGetPrivateObscure(out outDuckType));
            Assert.NotNull(outDuckType);
            Assert.Equal(99, outDuckType.MagicNumber);

            // Private internal parameter type object output
            Assert.True(duckInterface.TryGetPrivateObscureObject(out outObject));
            Assert.NotNull(outObject);
            Assert.Equal(99, outObject.DuckCast<IDummyFieldObject>().MagicNumber);

            Assert.True(duckAbstract.TryGetPrivateObscureObject(out outObject));
            Assert.NotNull(outObject);
            Assert.Equal(99, outObject.DuckCast<IDummyFieldObject>().MagicNumber);

            Assert.True(duckVirtual.TryGetPrivateObscureObject(out outObject));
            Assert.NotNull(outObject);
            Assert.Equal(99, outObject.DuckCast<IDummyFieldObject>().MagicNumber);
        }

        [Fact]
        public void DictionaryDuckTypeExample()
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();

            var duckInterface = dictionary.DuckCast<IDictioDuckType>();

            duckInterface.Add("Key01", "Value01");
            duckInterface.Add("Key02", "Value02");
            duckInterface.Add("K", "V");

            Assert.True(duckInterface.ContainsKey("K"));
            if (duckInterface.ContainsKey("K"))
            {
                Assert.True(duckInterface.Remove("K"));
            }

            if (duckInterface.TryGetValue("Key01", out string value))
            {
                Assert.Equal("Value01", value);
            }

            Assert.Equal("Value02", duckInterface["Key02"]);

            Assert.Equal(2, duckInterface.Count);

            foreach (KeyValuePair<string, string> val in duckInterface)
            {
                Assert.NotNull(val.Key);
            }

            if (duckInterface.TryGetValueInObject("Key02", out object objValue))
            {
                Assert.NotNull(objValue);
            }

            if (duckInterface.TryGetValueInDuckChaining("Key02", out IDictioValue dictioValue))
            {
                Assert.NotNull(dictioValue);
            }
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void DefaultGenericsMethods(object obscureObject)
        {
#if NET452
            if (!obscureObject.GetType().IsPublic && !obscureObject.GetType().IsNestedPublic)
            {
                Assert.Throws<DuckTypeProxyMethodsWithGenericParametersNotSupportedInNonPublicInstancesException>(
                    () =>
                    {
                        obscureObject.DuckCast<IDefaultGenericMethodDuckType>();
                    });
                Assert.Throws<DuckTypeProxyMethodsWithGenericParametersNotSupportedInNonPublicInstancesException>(
                    () =>
                    {
                        obscureObject.DuckCast<DefaultGenericMethodDuckTypeAbstractClass>();
                    });
                Assert.Throws<DuckTypeProxyMethodsWithGenericParametersNotSupportedInNonPublicInstancesException>(
                    () =>
                    {
                        obscureObject.DuckCast<DefaultGenericMethodDuckTypeVirtualClass>();
                    });
                return;
            }
#endif

            var duckInterface = obscureObject.DuckCast<IDefaultGenericMethodDuckType>();
            var duckAbstract = obscureObject.DuckCast<DefaultGenericMethodDuckTypeAbstractClass>();
            var duckVirtual = obscureObject.DuckCast<DefaultGenericMethodDuckTypeVirtualClass>();

            // GetDefault int
            Assert.Equal(0, duckInterface.GetDefault<int>());
            Assert.Equal(0, duckAbstract.GetDefault<int>());
            Assert.Equal(0, duckVirtual.GetDefault<int>());

            // GetDefault double
            Assert.Equal(0d, duckInterface.GetDefault<double>());
            Assert.Equal(0d, duckAbstract.GetDefault<double>());
            Assert.Equal(0d, duckVirtual.GetDefault<double>());

            // GetDefault string
            Assert.Null(duckInterface.GetDefault<string>());
            Assert.Null(duckAbstract.GetDefault<string>());
            Assert.Null(duckVirtual.GetDefault<string>());

            // Wrap ints
            Tuple<int, int> wrapper = duckInterface.Wrap(10, 20);
            Assert.Equal(10, wrapper.Item1);
            Assert.Equal(20, wrapper.Item2);

            // Wrap string
            Tuple<string, string> wrapper2 = duckAbstract.Wrap("Hello", "World");
            Assert.Equal("Hello", wrapper2.Item1);
            Assert.Equal("World", wrapper2.Item2);

            // Wrap object
            Tuple<object, string> wrapper3 = duckAbstract.Wrap<object, string>(null, "World");
            Assert.Null(wrapper3.Item1);
            Assert.Equal("World", wrapper3.Item2);
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void GenericsWithAttributeResolution(object obscureObject)
        {
            var duckInterface = obscureObject.DuckCast<IGenericsWithAttribute>();
            var duckAttribute = obscureObject.DuckCast<AbstractGenericsWithAttribute>();
            var duckVirtual = obscureObject.DuckCast<VirtualGenericsWithAttribute>();

            Tuple<int, string> result;

            Assert.Equal(0, duckInterface.GetDefaultInt());
            Assert.Null(duckInterface.GetDefaultString());

            result = duckInterface.WrapIntString(42, "All");
            Assert.Equal(42, result.Item1);
            Assert.Equal("All", result.Item2);

            // ...

            Assert.Equal(0, duckAttribute.GetDefaultInt());
            Assert.Null(duckAttribute.GetDefaultString());

            result = duckAttribute.WrapIntString(42, "All");
            Assert.Equal(42, result.Item1);
            Assert.Equal("All", result.Item2);

            // ...

            Assert.Equal(0, duckVirtual.GetDefaultInt());
            Assert.Null(duckVirtual.GetDefaultString());

            result = duckVirtual.WrapIntString(42, "All");
            Assert.Equal(42, result.Item1);
            Assert.Equal("All", result.Item2);
        }
    }
}
