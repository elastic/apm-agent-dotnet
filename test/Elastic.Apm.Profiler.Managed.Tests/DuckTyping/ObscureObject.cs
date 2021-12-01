// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="ObscureObject.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elastic.Apm.Profiler.Managed.DuckTyping;
// ReSharper disable InconsistentNaming
// ReSharper disable FieldCanBeMadeReadOnly.Local

#pragma warning disable 414

namespace Elastic.Apm.Profiler.Managed.Tests.DuckTyping
{
	public class ObscureObject
    {
        private static FieldPublicObject fieldPublicObject = new FieldPublicObject();
        private static FieldInternalObject fieldInternalObject = new FieldInternalObject();
        private static FieldPrivateObject fieldPrivateObject = new FieldPrivateObject();

        private static PropertyPublicObject propertyPublicObject = new PropertyPublicObject();
        private static PropertyInternalObject propertyInternalObject = new PropertyInternalObject();
        private static PropertyPrivateObject propertyPrivateObject = new PropertyPrivateObject();

        public static object GetFieldPublicObject() => fieldPublicObject;

        public static object GetFieldInternalObject() => fieldInternalObject;

        public static object GetFieldPrivateObject() => fieldPrivateObject;

        public static object GetPropertyPublicObject() => propertyPublicObject;

        public static object GetPropertyInternalObject() => propertyInternalObject;

        public static object GetPropertyPrivateObject() => propertyPrivateObject;

        // ***
        public enum TestEnum
        {
            First,
            Second
        }

        // ***

        public class DummyFieldObject
        {
            internal static DummyFieldObject Default = new DummyFieldObject();

            public int MagicNumber = 42;

            internal TypesTuple this[TypesTuple index]
            {
                get => index;
                set { }
            }
        }

        private class PrivateDummyFieldObject
        {
            internal static PrivateDummyFieldObject Default = new PrivateDummyFieldObject();

            public int MagicNumber = 42;

            internal TypesTuple this[TypesTuple index]
            {
                get => index;
                set { }
            }
        }

        // ***

        public class FieldPublicObject
        {
            public static readonly int _publicStaticReadonlyValueTypeField = 10;
            internal static readonly int _internalStaticReadonlyValueTypeField = 11;
            protected static readonly int _protectedStaticReadonlyValueTypeField = 12;
            private static readonly int _privateStaticReadonlyValueTypeField = 13;

            public static int _publicStaticValueTypeField = 20;
            internal static int _internalStaticValueTypeField = 21;
            protected static int _protectedStaticValueTypeField = 22;
            private static int _privateStaticValueTypeField = 23;

            public readonly int _publicReadonlyValueTypeField = 30;
            internal readonly int _internalReadonlyValueTypeField = 31;
            protected readonly int _protectedReadonlyValueTypeField = 32;
            private readonly int _privateReadonlyValueTypeField = 33;

            public int _publicValueTypeField = 40;
            internal int _internalValueTypeField = 41;
            protected int _protectedValueTypeField = 42;
            private int _privateValueTypeField = 43;

            // ***

            public static readonly string _publicStaticReadonlyReferenceTypeField = "10";
            internal static readonly string _internalStaticReadonlyReferenceTypeField = "11";
            protected static readonly string _protectedStaticReadonlyReferenceTypeField = "12";
            private static readonly string _privateStaticReadonlyReferenceTypeField = "13";

            public static string _publicStaticReferenceTypeField = "20";
            internal static string _internalStaticReferenceTypeField = "21";
            protected static string _protectedStaticReferenceTypeField = "22";
            private static string _privateStaticReferenceTypeField = "23";

            public readonly string _publicReadonlyReferenceTypeField = "30";
            internal readonly string _internalReadonlyReferenceTypeField = "31";
            protected readonly string _protectedReadonlyReferenceTypeField = "32";
            private readonly string _privateReadonlyReferenceTypeField = "33";

            public string _publicReferenceTypeField = "40";
            internal string _internalReferenceTypeField = "41";
            protected string _protectedReferenceTypeField = "42";
            private string _privateReferenceTypeField = "43";

            // ***

            public static readonly DummyFieldObject _publicStaticReadonlySelfTypeField = DummyFieldObject.Default;
            internal static readonly DummyFieldObject _internalStaticReadonlySelfTypeField = DummyFieldObject.Default;
            protected static readonly DummyFieldObject _protectedStaticReadonlySelfTypeField = DummyFieldObject.Default;
            private static readonly DummyFieldObject _privateStaticReadonlySelfTypeField = DummyFieldObject.Default;

            public static DummyFieldObject _publicStaticSelfTypeField = DummyFieldObject.Default;
            internal static DummyFieldObject _internalStaticSelfTypeField = DummyFieldObject.Default;
            protected static DummyFieldObject _protectedStaticSelfTypeField = DummyFieldObject.Default;
            private static DummyFieldObject _privateStaticSelfTypeField = DummyFieldObject.Default;

            public readonly DummyFieldObject _publicReadonlySelfTypeField = DummyFieldObject.Default;
            internal readonly DummyFieldObject _internalReadonlySelfTypeField = DummyFieldObject.Default;
            protected readonly DummyFieldObject _protectedReadonlySelfTypeField = DummyFieldObject.Default;
            private readonly DummyFieldObject _privateReadonlySelfTypeField = DummyFieldObject.Default;

            public DummyFieldObject _publicSelfTypeField = DummyFieldObject.Default;
            internal DummyFieldObject _internalSelfTypeField = DummyFieldObject.Default;
            protected DummyFieldObject _protectedSelfTypeField = DummyFieldObject.Default;
            private DummyFieldObject _privateSelfTypeField = DummyFieldObject.Default;

            // ***

            public static int? _publicStaticNullableIntField = null;
            private static int? _privateStaticNullableIntField = null;
            public int? _publicNullableIntField = null;
            private int? _privateNullableIntField = null;
        }

        internal class FieldInternalObject
        {
            public static readonly int _publicStaticReadonlyValueTypeField = 10;
            internal static readonly int _internalStaticReadonlyValueTypeField = 11;
            protected static readonly int _protectedStaticReadonlyValueTypeField = 12;
            private static readonly int _privateStaticReadonlyValueTypeField = 13;

            public static int _publicStaticValueTypeField = 20;
            internal static int _internalStaticValueTypeField = 21;
            protected static int _protectedStaticValueTypeField = 22;
            private static int _privateStaticValueTypeField = 23;

            public readonly int _publicReadonlyValueTypeField = 30;
            internal readonly int _internalReadonlyValueTypeField = 31;
            protected readonly int _protectedReadonlyValueTypeField = 32;
            private readonly int _privateReadonlyValueTypeField = 33;

            public int _publicValueTypeField = 40;
            internal int _internalValueTypeField = 41;
            protected int _protectedValueTypeField = 42;
            private int _privateValueTypeField = 43;

            // ***

            public static readonly string _publicStaticReadonlyReferenceTypeField = "10";
            internal static readonly string _internalStaticReadonlyReferenceTypeField = "11";
            protected static readonly string _protectedStaticReadonlyReferenceTypeField = "12";
            private static readonly string _privateStaticReadonlyReferenceTypeField = "13";

            public static string _publicStaticReferenceTypeField = "20";
            internal static string _internalStaticReferenceTypeField = "21";
            protected static string _protectedStaticReferenceTypeField = "22";
            private static string _privateStaticReferenceTypeField = "23";

            public readonly string _publicReadonlyReferenceTypeField = "30";
            internal readonly string _internalReadonlyReferenceTypeField = "31";
            protected readonly string _protectedReadonlyReferenceTypeField = "32";
            private readonly string _privateReadonlyReferenceTypeField = "33";

            public string _publicReferenceTypeField = "40";
            internal string _internalReferenceTypeField = "41";
            protected string _protectedReferenceTypeField = "42";
            private string _privateReferenceTypeField = "43";

            // ***

            public static readonly DummyFieldObject _publicStaticReadonlySelfTypeField = DummyFieldObject.Default;
            internal static readonly DummyFieldObject _internalStaticReadonlySelfTypeField = DummyFieldObject.Default;
            protected static readonly DummyFieldObject _protectedStaticReadonlySelfTypeField = DummyFieldObject.Default;
            private static readonly DummyFieldObject _privateStaticReadonlySelfTypeField = DummyFieldObject.Default;

            public static DummyFieldObject _publicStaticSelfTypeField = DummyFieldObject.Default;
            internal static DummyFieldObject _internalStaticSelfTypeField = DummyFieldObject.Default;
            protected static DummyFieldObject _protectedStaticSelfTypeField = DummyFieldObject.Default;
            private static DummyFieldObject _privateStaticSelfTypeField = DummyFieldObject.Default;

            public readonly DummyFieldObject _publicReadonlySelfTypeField = DummyFieldObject.Default;
            internal readonly DummyFieldObject _internalReadonlySelfTypeField = DummyFieldObject.Default;
            protected readonly DummyFieldObject _protectedReadonlySelfTypeField = DummyFieldObject.Default;
            private readonly DummyFieldObject _privateReadonlySelfTypeField = DummyFieldObject.Default;

            public DummyFieldObject _publicSelfTypeField = DummyFieldObject.Default;
            internal DummyFieldObject _internalSelfTypeField = DummyFieldObject.Default;
            protected DummyFieldObject _protectedSelfTypeField = DummyFieldObject.Default;
            private DummyFieldObject _privateSelfTypeField = DummyFieldObject.Default;

            // ***

            public static int? _publicStaticNullableIntField = null;
            private static int? _privateStaticNullableIntField = null;
            public int? _publicNullableIntField = null;
            private int? _privateNullableIntField = null;
        }

        private class FieldPrivateObject
        {
            public static readonly int _publicStaticReadonlyValueTypeField = 10;
            internal static readonly int _internalStaticReadonlyValueTypeField = 11;
            protected static readonly int _protectedStaticReadonlyValueTypeField = 12;
            private static readonly int _privateStaticReadonlyValueTypeField = 13;

            public static int _publicStaticValueTypeField = 20;
            internal static int _internalStaticValueTypeField = 21;
            protected static int _protectedStaticValueTypeField = 22;
            private static int _privateStaticValueTypeField = 23;

            public readonly int _publicReadonlyValueTypeField = 30;
            internal readonly int _internalReadonlyValueTypeField = 31;
            protected readonly int _protectedReadonlyValueTypeField = 32;
            private readonly int _privateReadonlyValueTypeField = 33;

            public int _publicValueTypeField = 40;
            internal int _internalValueTypeField = 41;
            protected int _protectedValueTypeField = 42;
            private int _privateValueTypeField = 43;

            // ***

            public static readonly string _publicStaticReadonlyReferenceTypeField = "10";
            internal static readonly string _internalStaticReadonlyReferenceTypeField = "11";
            protected static readonly string _protectedStaticReadonlyReferenceTypeField = "12";
            private static readonly string _privateStaticReadonlyReferenceTypeField = "13";

            public static string _publicStaticReferenceTypeField = "20";
            internal static string _internalStaticReferenceTypeField = "21";
            protected static string _protectedStaticReferenceTypeField = "22";
            private static string _privateStaticReferenceTypeField = "23";

            public readonly string _publicReadonlyReferenceTypeField = "30";
            internal readonly string _internalReadonlyReferenceTypeField = "31";
            protected readonly string _protectedReadonlyReferenceTypeField = "32";
            private readonly string _privateReadonlyReferenceTypeField = "33";

            public string _publicReferenceTypeField = "40";
            internal string _internalReferenceTypeField = "41";
            protected string _protectedReferenceTypeField = "42";
            private string _privateReferenceTypeField = "43";

            // ***

            public static readonly DummyFieldObject _publicStaticReadonlySelfTypeField = DummyFieldObject.Default;
            internal static readonly DummyFieldObject _internalStaticReadonlySelfTypeField = DummyFieldObject.Default;
            protected static readonly DummyFieldObject _protectedStaticReadonlySelfTypeField = DummyFieldObject.Default;
            private static readonly DummyFieldObject _privateStaticReadonlySelfTypeField = DummyFieldObject.Default;

            public static DummyFieldObject _publicStaticSelfTypeField = DummyFieldObject.Default;
            internal static DummyFieldObject _internalStaticSelfTypeField = DummyFieldObject.Default;
            protected static DummyFieldObject _protectedStaticSelfTypeField = DummyFieldObject.Default;
            private static DummyFieldObject _privateStaticSelfTypeField = DummyFieldObject.Default;

            public readonly DummyFieldObject _publicReadonlySelfTypeField = DummyFieldObject.Default;
            internal readonly DummyFieldObject _internalReadonlySelfTypeField = DummyFieldObject.Default;
            protected readonly DummyFieldObject _protectedReadonlySelfTypeField = DummyFieldObject.Default;
            private readonly DummyFieldObject _privateReadonlySelfTypeField = DummyFieldObject.Default;

            public DummyFieldObject _publicSelfTypeField = DummyFieldObject.Default;
            internal DummyFieldObject _internalSelfTypeField = DummyFieldObject.Default;
            protected DummyFieldObject _protectedSelfTypeField = DummyFieldObject.Default;
            private DummyFieldObject _privateSelfTypeField = DummyFieldObject.Default;

            // ***

            public static int? _publicStaticNullableIntField = null;
            private static int? _privateStaticNullableIntField = null;
            public int? _publicNullableIntField = null;
            private int? _privateNullableIntField = null;
        }

        // ***

        public class PropertyPublicObject
        {
            private Dictionary<int, int> _dictioInt = new Dictionary<int, int>();
            private Dictionary<string, string> _dictioString = new Dictionary<string, string>();

            // *********

            public static int PublicStaticGetValueType { get; } = 10;

            internal static int InternalStaticGetValueType { get; } = 11;

            protected static int ProtectedStaticGetValueType { get; } = 12;

            private static int PrivateStaticGetValueType { get; } = 13;

            // *

            public static int PublicStaticGetSetValueType { get; set; } = 20;

            internal static int InternalStaticGetSetValueType { get; set; } = 21;

            protected static int ProtectedStaticGetSetValueType { get; set; } = 22;

            private static int PrivateStaticGetSetValueType { get; set; } = 23;

            // *

            public int PublicGetValueType { get; } = 30;

            internal int InternalGetValueType { get; } = 31;

            protected int ProtectedGetValueType { get; } = 32;

            private int PrivateGetValueType { get; } = 33;

            // *

            public int PublicGetSetValueType { get; set; } = 40;

            internal int InternalGetSetValueType { get; set; } = 41;

            protected int ProtectedGetSetValueType { get; set; } = 42;

            private int PrivateGetSetValueType { get; set; } = 43;

            // *********

            public static string PublicStaticGetReferenceType { get; } = "10";

            internal static string InternalStaticGetReferenceType { get; } = "11";

            protected static string ProtectedStaticGetReferenceType { get; } = "12";

            private static string PrivateStaticGetReferenceType { get; } = "13";

            // *

            public static string PublicStaticGetSetReferenceType { get; set; } = "20";

            internal static string InternalStaticGetSetReferenceType { get; set; } = "21";

            protected static string ProtectedStaticGetSetReferenceType { get; set; } = "22";

            private static string PrivateStaticGetSetReferenceType { get; set; } = "23";

            // *

            public string PublicGetReferenceType { get; } = "30";

            internal string InternalGetReferenceType { get; } = "31";

            protected string ProtectedGetReferenceType { get; } = "32";

            private string PrivateGetReferenceType { get; } = "33";

            // *

            public string PublicGetSetReferenceType { get; set; } = "40";

            internal string InternalGetSetReferenceType { get; set; } = "41";

            protected string ProtectedGetSetReferenceType { get; set; } = "42";

            private string PrivateGetSetReferenceType { get; set; } = "43";

            // *********

            public static DummyFieldObject PublicStaticGetSelfType { get; } = DummyFieldObject.Default;

            internal static DummyFieldObject InternalStaticGetSelfType { get; } = DummyFieldObject.Default;

            protected static DummyFieldObject ProtectedStaticGetSelfType { get; } = DummyFieldObject.Default;

            private static DummyFieldObject PrivateStaticGetSelfType { get; } = DummyFieldObject.Default;

            // *

            public static DummyFieldObject PublicStaticGetSetSelfType { get; set; } = DummyFieldObject.Default;

            internal static DummyFieldObject InternalStaticGetSetSelfType { get; set; } = DummyFieldObject.Default;

            protected static DummyFieldObject ProtectedStaticGetSetSelfType { get; set; } = DummyFieldObject.Default;

            private static DummyFieldObject PrivateStaticGetSetSelfType { get; set; } = DummyFieldObject.Default;

            // *

            public DummyFieldObject PublicGetSelfType { get; } = DummyFieldObject.Default;

            internal DummyFieldObject InternalGetSelfType { get; } = DummyFieldObject.Default;

            protected DummyFieldObject ProtectedGetSelfType { get; } = DummyFieldObject.Default;

            private DummyFieldObject PrivateGetSelfType { get; } = DummyFieldObject.Default;

            // *

            public DummyFieldObject PublicGetSetSelfType { get; set; } = DummyFieldObject.Default;

            internal DummyFieldObject InternalGetSetSelfType { get; set; } = DummyFieldObject.Default;

            protected DummyFieldObject ProtectedGetSetSelfType { get; set; } = DummyFieldObject.Default;

            private DummyFieldObject PrivateGetSetSelfType { get; set; } = DummyFieldObject.Default;

            // ***

            private PrivateDummyFieldObject PrivateDummyGetSetSelfType { get; set; } = PrivateDummyFieldObject.Default;

            // ***

            public static int? PublicStaticNullableInt { get; set; }

            private static int? PrivateStaticNullableInt { get; set; }

            public int? PublicNullableInt { get; set; }

            private int? PrivateNullableInt { get; set; }

            // ***

            public int this[int index]
            {
                get => _dictioInt[index];
                set => _dictioInt[index] = value;
            }

            public string this[string index]
            {
                get => _dictioString[index];
                set => _dictioString[index] = value;
            }

            // ***

            public TaskStatus Status { get; set; } = TaskStatus.RanToCompletion;

            // *** Methods
            public int Sum(int a, int b) => a + b;

            public float Sum(float a, float b) => a + b;

            public double Sum(double a, double b) => a + b;

            public short Sum(short a, short b) => (short)(a + b);

            private int InternalSum(int a, int b) => a + b;

            public TestEnum ShowEnum(TestEnum val)
            {
                return val;
            }

            public T GetDefault<T>() => default;

            public Tuple<T1, T2> Wrap<T1, T2>(T1 a, T2 b)
            {
                return new Tuple<T1, T2>(a, b);
            }

            public void Add(string name, DummyFieldObject obj)
            {
            }

            public void Add(string name, int obj)
            {
            }

            public void Add(string name, string obj = "none")
            {
            }

            public void Pow2(ref int value)
            {
                value *= value;
            }

            public void GetOutput(out int value)
            {
                value = 42;
            }

            private bool TryGetObscure(out DummyFieldObject obj)
            {
                obj = new DummyFieldObject { MagicNumber = 99 };
                return true;
            }

            public void GetReference(ref int value)
            {
                value *= value;
            }

            private bool TryGetReference(ref DummyFieldObject obj)
            {
                obj = new DummyFieldObject { MagicNumber = (obj?.MagicNumber ?? 99) + 1 };
                return true;
            }

            private bool TryGetPrivateObscure(out PrivateDummyFieldObject obj)
            {
                obj = new PrivateDummyFieldObject { MagicNumber = 99 };
                return true;
            }

            private bool TryGetPrivateReference(ref PrivateDummyFieldObject obj)
            {
                obj = new PrivateDummyFieldObject { MagicNumber = (obj?.MagicNumber ?? 99) + 1 };
                return true;
            }

            private DummyFieldObject Bypass(DummyFieldObject obj)
            {
                return obj;
            }
        }

        internal class PropertyInternalObject
        {
            private Dictionary<int, int> _dictioInt = new Dictionary<int, int>();
            private Dictionary<string, string> _dictioString = new Dictionary<string, string>();

            public static int PublicStaticGetValueType { get; } = 10;

            internal static int InternalStaticGetValueType { get; } = 11;

            protected static int ProtectedStaticGetValueType { get; } = 12;

            private static int PrivateStaticGetValueType { get; } = 13;

            // *

            public static int PublicStaticGetSetValueType { get; set; } = 20;

            internal static int InternalStaticGetSetValueType { get; set; } = 21;

            protected static int ProtectedStaticGetSetValueType { get; set; } = 22;

            private static int PrivateStaticGetSetValueType { get; set; } = 23;

            // *

            public int PublicGetValueType { get; } = 30;

            internal int InternalGetValueType { get; } = 31;

            protected int ProtectedGetValueType { get; } = 32;

            private int PrivateGetValueType { get; } = 33;

            // *

            public int PublicGetSetValueType { get; set; } = 40;

            internal int InternalGetSetValueType { get; set; } = 41;

            protected int ProtectedGetSetValueType { get; set; } = 42;

            private int PrivateGetSetValueType { get; set; } = 43;

            // *********

            public static string PublicStaticGetReferenceType { get; } = "10";

            internal static string InternalStaticGetReferenceType { get; } = "11";

            protected static string ProtectedStaticGetReferenceType { get; } = "12";

            private static string PrivateStaticGetReferenceType { get; } = "13";

            // *

            public static string PublicStaticGetSetReferenceType { get; set; } = "20";

            internal static string InternalStaticGetSetReferenceType { get; set; } = "21";

            protected static string ProtectedStaticGetSetReferenceType { get; set; } = "22";

            private static string PrivateStaticGetSetReferenceType { get; set; } = "23";

            // *

            public string PublicGetReferenceType { get; } = "30";

            internal string InternalGetReferenceType { get; } = "31";

            protected string ProtectedGetReferenceType { get; } = "32";

            private string PrivateGetReferenceType { get; } = "33";

            // *

            public string PublicGetSetReferenceType { get; set; } = "40";

            internal string InternalGetSetReferenceType { get; set; } = "41";

            protected string ProtectedGetSetReferenceType { get; set; } = "42";

            private string PrivateGetSetReferenceType { get; set; } = "43";

            // *********

            public static DummyFieldObject PublicStaticGetSelfType { get; } = DummyFieldObject.Default;

            internal static DummyFieldObject InternalStaticGetSelfType { get; } = DummyFieldObject.Default;

            protected static DummyFieldObject ProtectedStaticGetSelfType { get; } = DummyFieldObject.Default;

            private static DummyFieldObject PrivateStaticGetSelfType { get; } = DummyFieldObject.Default;

            // *

            public static DummyFieldObject PublicStaticGetSetSelfType { get; set; } = DummyFieldObject.Default;

            internal static DummyFieldObject InternalStaticGetSetSelfType { get; set; } = DummyFieldObject.Default;

            protected static DummyFieldObject ProtectedStaticGetSetSelfType { get; set; } = DummyFieldObject.Default;

            private static DummyFieldObject PrivateStaticGetSetSelfType { get; set; } = DummyFieldObject.Default;

            // *

            public DummyFieldObject PublicGetSelfType { get; } = DummyFieldObject.Default;

            internal DummyFieldObject InternalGetSelfType { get; } = DummyFieldObject.Default;

            protected DummyFieldObject ProtectedGetSelfType { get; } = DummyFieldObject.Default;

            private DummyFieldObject PrivateGetSelfType { get; } = DummyFieldObject.Default;

            // *

            public DummyFieldObject PublicGetSetSelfType { get; set; } = DummyFieldObject.Default;

            internal DummyFieldObject InternalGetSetSelfType { get; set; } = DummyFieldObject.Default;

            protected DummyFieldObject ProtectedGetSetSelfType { get; set; } = DummyFieldObject.Default;

            private DummyFieldObject PrivateGetSetSelfType { get; set; } = DummyFieldObject.Default;

            // ***

            private PrivateDummyFieldObject PrivateDummyGetSetSelfType { get; set; } = PrivateDummyFieldObject.Default;

            // ***

            public static int? PublicStaticNullableInt { get; set; }

            private static int? PrivateStaticNullableInt { get; set; }

            public int? PublicNullableInt { get; set; }

            private int? PrivateNullableInt { get; set; }

            // ***

            public int this[int index]
            {
                get => _dictioInt[index];
                set => _dictioInt[index] = value;
            }

            public string this[string index]
            {
                get => _dictioString[index];
                set => _dictioString[index] = value;
            }

            // ***

            public TaskStatus Status { get; set; } = TaskStatus.RanToCompletion;

            // *** Methods
            public int Sum(int a, int b) => a + b;

            public float Sum(float a, float b) => a + b;

            public double Sum(double a, double b) => a + b;

            public short Sum(short a, short b) => (short)(a + b);

            private int InternalSum(int a, int b) => a + b;

            public TestEnum ShowEnum(TestEnum val)
            {
                return val;
            }

            public T GetDefault<T>() => default;

            public Tuple<T1, T2> Wrap<T1, T2>(T1 a, T2 b)
            {
                return new Tuple<T1, T2>(a, b);
            }

            public void Add(string name, DummyFieldObject obj)
            {
            }

            public void Add(string name, int obj)
            {
            }

            public void Add(string name, string obj = "none")
            {
            }

            public void Pow2(ref int value)
            {
                value *= value;
            }

            public void GetOutput(out int value)
            {
                value = 42;
            }

            private bool TryGetObscure(out DummyFieldObject obj)
            {
                obj = new DummyFieldObject { MagicNumber = 99 };
                return true;
            }

            public void GetReference(ref int value)
            {
                value *= value;
            }

            private bool TryGetReference(ref DummyFieldObject obj)
            {
                obj = new DummyFieldObject { MagicNumber = (obj?.MagicNumber ?? 99) + 1 };
                return true;
            }

            private bool TryGetPrivateObscure(out PrivateDummyFieldObject obj)
            {
                obj = new PrivateDummyFieldObject { MagicNumber = 99 };
                return true;
            }

            private bool TryGetPrivateReference(ref PrivateDummyFieldObject obj)
            {
                obj = new PrivateDummyFieldObject { MagicNumber = (obj?.MagicNumber ?? 99) + 1 };
                return true;
            }

            private DummyFieldObject Bypass(DummyFieldObject obj)
            {
                return obj;
            }
        }

        private class PropertyPrivateObject
        {
            private Dictionary<int, int> _dictioInt = new Dictionary<int, int>();
            private Dictionary<string, string> _dictioString = new Dictionary<string, string>();

            public static int PublicStaticGetValueType { get; } = 10;

            internal static int InternalStaticGetValueType { get; } = 11;

            protected static int ProtectedStaticGetValueType { get; } = 12;

            private static int PrivateStaticGetValueType { get; } = 13;

            // *

            public static int PublicStaticGetSetValueType { get; set; } = 20;

            internal static int InternalStaticGetSetValueType { get; set; } = 21;

            protected static int ProtectedStaticGetSetValueType { get; set; } = 22;

            private static int PrivateStaticGetSetValueType { get; set; } = 23;

            // *

            public int PublicGetValueType { get; } = 30;

            internal int InternalGetValueType { get; } = 31;

            protected int ProtectedGetValueType { get; } = 32;

            private int PrivateGetValueType { get; } = 33;

            // *

            public int PublicGetSetValueType { get; set; } = 40;

            internal int InternalGetSetValueType { get; set; } = 41;

            protected int ProtectedGetSetValueType { get; set; } = 42;

            private int PrivateGetSetValueType { get; set; } = 43;

            // *********

            public static string PublicStaticGetReferenceType { get; } = "10";

            internal static string InternalStaticGetReferenceType { get; } = "11";

            protected static string ProtectedStaticGetReferenceType { get; } = "12";

            private static string PrivateStaticGetReferenceType { get; } = "13";

            // *

            public static string PublicStaticGetSetReferenceType { get; set; } = "20";

            internal static string InternalStaticGetSetReferenceType { get; set; } = "21";

            protected static string ProtectedStaticGetSetReferenceType { get; set; } = "22";

            private static string PrivateStaticGetSetReferenceType { get; set; } = "23";

            // *

            public string PublicGetReferenceType { get; } = "30";

            internal string InternalGetReferenceType { get; } = "31";

            protected string ProtectedGetReferenceType { get; } = "32";

            private string PrivateGetReferenceType { get; } = "33";

            // *

            public string PublicGetSetReferenceType { get; set; } = "40";

            internal string InternalGetSetReferenceType { get; set; } = "41";

            protected string ProtectedGetSetReferenceType { get; set; } = "42";

            private string PrivateGetSetReferenceType { get; set; } = "43";

            // *********

            public static DummyFieldObject PublicStaticGetSelfType { get; } = DummyFieldObject.Default;

            internal static DummyFieldObject InternalStaticGetSelfType { get; } = DummyFieldObject.Default;

            protected static DummyFieldObject ProtectedStaticGetSelfType { get; } = DummyFieldObject.Default;

            private static DummyFieldObject PrivateStaticGetSelfType { get; } = DummyFieldObject.Default;

            // *

            public static DummyFieldObject PublicStaticGetSetSelfType { get; set; } = DummyFieldObject.Default;

            internal static DummyFieldObject InternalStaticGetSetSelfType { get; set; } = DummyFieldObject.Default;

            protected static DummyFieldObject ProtectedStaticGetSetSelfType { get; set; } = DummyFieldObject.Default;

            private static DummyFieldObject PrivateStaticGetSetSelfType { get; set; } = DummyFieldObject.Default;

            // *

            public DummyFieldObject PublicGetSelfType { get; } = DummyFieldObject.Default;

            internal DummyFieldObject InternalGetSelfType { get; } = DummyFieldObject.Default;

            protected DummyFieldObject ProtectedGetSelfType { get; } = DummyFieldObject.Default;

            private DummyFieldObject PrivateGetSelfType { get; } = DummyFieldObject.Default;

            // *

            public DummyFieldObject PublicGetSetSelfType { get; set; } = DummyFieldObject.Default;

            internal DummyFieldObject InternalGetSetSelfType { get; set; } = DummyFieldObject.Default;

            protected DummyFieldObject ProtectedGetSetSelfType { get; set; } = DummyFieldObject.Default;

            private DummyFieldObject PrivateGetSetSelfType { get; set; } = DummyFieldObject.Default;

            // ***

            private PrivateDummyFieldObject PrivateDummyGetSetSelfType { get; set; } = PrivateDummyFieldObject.Default;

            // ***

            public static int? PublicStaticNullableInt { get; set; }

            private static int? PrivateStaticNullableInt { get; set; }

            public int? PublicNullableInt { get; set; }

            private int? PrivateNullableInt { get; set; }

            // ***
            public int this[int index]
            {
                get => _dictioInt[index];
                set => _dictioInt[index] = value;
            }

            public string this[string index]
            {
                get => _dictioString[index];
                set => _dictioString[index] = value;
            }

            // ***

            public TaskStatus Status { get; set; } = TaskStatus.RanToCompletion;

            // *** Methods
            public int Sum(int a, int b) => a + b;

            public float Sum(float a, float b) => a + b;

            public double Sum(double a, double b) => a + b;

            public short Sum(short a, short b) => (short)(a + b);

            private int InternalSum(int a, int b) => a + b;

            public TestEnum ShowEnum(TestEnum val)
            {
                return val;
            }

            public T GetDefault<T>() => default;

            public Tuple<T1, T2> Wrap<T1, T2>(T1 a, T2 b)
            {
                return new Tuple<T1, T2>(a, b);
            }

            public void Add(string name, DummyFieldObject obj)
            {
            }

            public void Add(string name, int obj)
            {
            }

            public void Add(string name, string obj = "none")
            {
            }

            public void Pow2(ref int value)
            {
                value *= value;
            }

            public void GetOutput(out int value)
            {
                value = 42;
            }

            private bool TryGetObscure(out DummyFieldObject obj)
            {
                obj = new DummyFieldObject { MagicNumber = 99 };
                return true;
            }

            public void GetReference(ref int value)
            {
                value *= value;
            }

            private bool TryGetReference(ref DummyFieldObject obj)
            {
                obj = new DummyFieldObject { MagicNumber = (obj?.MagicNumber ?? 99) + 1 };
                return true;
            }

            private bool TryGetPrivateObscure(out PrivateDummyFieldObject obj)
            {
                obj = new PrivateDummyFieldObject { MagicNumber = 99 };
                return true;
            }

            private bool TryGetPrivateReference(ref PrivateDummyFieldObject obj)
            {
                obj = new PrivateDummyFieldObject { MagicNumber = (obj?.MagicNumber ?? 99) + 1 };
                return true;
            }

            private DummyFieldObject Bypass(DummyFieldObject obj)
            {
                return obj;
            }
        }

        // ***

        public struct PublicStruct
        {
            public int PublicGetSetValueType { get; set; }

            private int PrivateGetSetValueType { get; set; }
        }
    }
}
