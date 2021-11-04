// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="ExceptionsTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.Threading.Tasks;
using Elastic.Apm.Profiler.Managed.DuckTyping;
using Xunit;

namespace Elastic.Apm.Profiler.Managed.Tests.DuckTyping
{
    public class ExceptionsTests
    {
        [Fact]
        public void PropertyCantBeReadException()
        {
            object target = new PropertyCantBeReadExceptionClass();

            Assert.Throws<DuckTypePropertyCantBeReadException>(() =>
            {
                target.DuckCast<IPropertyCantBeReadException>();
            });

            Assert.Throws<DuckTypePropertyCantBeReadException>(() =>
            {
                target.DuckCast<StructPropertyCantBeReadException>();
            });
        }

        public interface IPropertyCantBeReadException
        {
            string OnlySetter { get; set; }
        }

        public struct StructPropertyCantBeReadException
        {
            public string OnlySetter;
        }

        internal class PropertyCantBeReadExceptionClass
        {
            public string OnlySetter
            {
                set { }
            }
        }

        // *

        [Fact]
        public void PropertyCantBeWrittenException()
        {
            object target = new PropertyCantBeWrittenExceptionClass();

            Assert.Throws<DuckTypePropertyCantBeWrittenException>(() =>
            {
                target.DuckCast<IPropertyCantBeWrittenException>();
            });
        }

        public interface IPropertyCantBeWrittenException
        {
            string OnlyGetter { get; set; }
        }

        internal class PropertyCantBeWrittenExceptionClass
        {
            public string OnlyGetter { get; }
        }

        // *

        [Fact]
        public void PropertyArgumentsLengthException()
        {
            object target = new PropertyArgumentsLengthExceptionClass();

            Assert.Throws<DuckTypePropertyArgumentsLengthException>(() =>
            {
                target.DuckCast<IPropertyArgumentsLengthException>();
            });

            Assert.Throws<DuckTypePropertyArgumentsLengthException>(() =>
            {
                target.DuckCast<StructPropertyArgumentsLengthException>();
            });

            Assert.Throws<DuckTypePropertyArgumentsLengthException>(() =>
            {
                target.DuckCast<ISetPropertyArgumentsLengthException>();
            });
        }

        public interface IPropertyArgumentsLengthException
        {
            string Item { get; }
        }

        [DuckCopy]
        public struct StructPropertyArgumentsLengthException
        {
            public string Item;
        }

        public interface ISetPropertyArgumentsLengthException
        {
            string Item { set; }
        }

        internal class PropertyArgumentsLengthExceptionClass
        {
            public string this[string key]
            {
                get => null;
                set { }
            }
        }

        // *

        [Fact]
        public void FieldIsReadonlyException()
        {
            object target = new FieldIsReadonlyExceptionClass();

            Assert.Throws<DuckTypeFieldIsReadonlyException>(() =>
            {
                target.DuckCast<IFieldIsReadonlyException>();
            });
        }

        public interface IFieldIsReadonlyException
        {
            [Duck(Name = "_name", Kind = DuckKind.Field)]
            string Name { get; set; }
        }

        internal class FieldIsReadonlyExceptionClass
        {
            private readonly string _name = string.Empty;

            public string AvoidCompileError => _name;
        }

        // *

        [Fact]
        public void PropertyOrFieldNotFoundException()
        {
            object[] targets = new object[]
            {
                new PropertyOrFieldNotFoundExceptionClass(),
                (PropertyOrFieldNotFoundExceptionTargetStruct)default
            };

            foreach (object target in targets)
            {
                Assert.Throws<DuckTypePropertyOrFieldNotFoundException>(() =>
                {
                    target.DuckCast<IPropertyOrFieldNotFoundException>();
                });

                Assert.Throws<DuckTypePropertyOrFieldNotFoundException>(() =>
                {
                    target.DuckCast<IPropertyOrFieldNotFound2Exception>();
                });

                Assert.Throws<DuckTypePropertyOrFieldNotFoundException>(() =>
                {
                    target.DuckCast<IPropertyOrFieldNotFound3Exception>();
                });

                Assert.Throws<DuckTypePropertyOrFieldNotFoundException>(() =>
                {
                    target.DuckCast<PropertyOrFieldNotFoundExceptionStruct>();
                });

                Assert.Throws<DuckTypePropertyOrFieldNotFoundException>(() =>
                {
                    target.DuckCast<PropertyOrFieldNotFound2ExceptionStruct>();
                });
            }
        }

        public interface IPropertyOrFieldNotFoundException
        {
            string Name { get; set; }
        }

        public interface IPropertyOrFieldNotFound2Exception
        {
            [Duck(Kind = DuckKind.Field)]
            string Name { get; set; }
        }

        public interface IPropertyOrFieldNotFound3Exception
        {
            string Name { set; }
        }

        public struct PropertyOrFieldNotFoundExceptionStruct
        {
            public string Name;
        }

        public struct PropertyOrFieldNotFound2ExceptionStruct
        {
            [Duck(Kind = DuckKind.Field)]
            public string Name;
        }

        internal class PropertyOrFieldNotFoundExceptionClass
        {
        }

        internal struct PropertyOrFieldNotFoundExceptionTargetStruct
        {
        }

#if NET452
        // *
        [Fact]
        public void TypeIsNotPublicException()
        {
            object target = new TypeIsNotPublicExceptionClass();

            Assert.Throws<DuckTypeTypeIsNotPublicException>(() =>
            {
                target.DuckCast<ITypeIsNotPublicException>();
            });

            Assert.Throws<DuckTypeTypeIsNotPublicException>(() =>
            {
                target.DuckCast(typeof(ITypeIsNotPublicException));
            });
        }

        internal interface ITypeIsNotPublicException
        {
            string Name { get; set; }
        }

        internal class TypeIsNotPublicExceptionClass
        {
            public string Name { get; set; }
        }
#endif
        // *

        [Fact]
        public void StructMembersCannotBeChangedException()
        {
            StructMembersCannotBeChangedExceptionStruct targetStruct = default;
            object target = (object)targetStruct;

            Assert.Throws<DuckTypeStructMembersCannotBeChangedException>(() =>
            {
                target.DuckCast<IStructMembersCannotBeChangedException>();
            });
        }

        public interface IStructMembersCannotBeChangedException
        {
            string Name { get; set; }
        }

        internal struct StructMembersCannotBeChangedExceptionStruct
        {
            public string Name { get; set; }
        }

        // *

        [Fact]
        public void StructMembersCannotBeChanged2Exception()
        {
            StructMembersCannotBeChanged2ExceptionStruct targetStruct = default;
            object target = (object)targetStruct;

            Assert.Throws<DuckTypeStructMembersCannotBeChangedException>(() =>
            {
                target.DuckCast<IStructMembersCannotBeChanged2Exception>();
            });
        }

        public interface IStructMembersCannotBeChanged2Exception
        {
            [Duck(Kind = DuckKind.Field)]
            string Name { get; set; }
        }

        internal struct StructMembersCannotBeChanged2ExceptionStruct
        {
#pragma warning disable 649
            public string Name;
#pragma warning restore 649
        }

        // *

        [Fact]
        public void TargetMethodNotFoundException()
        {
            object target = new TargetMethodNotFoundExceptionClass();

            Assert.Throws<DuckTypeTargetMethodNotFoundException>(() =>
            {
                target.DuckCast<ITargetMethodNotFoundException>();
            });

            Assert.Throws<DuckTypeTargetMethodNotFoundException>(() =>
            {
                target.DuckCast<ITargetMethodNotFound2Exception>();
            });

            Assert.Throws<DuckTypeTargetMethodNotFoundException>(() =>
            {
                target.DuckCast<ITargetMethodNotFound3Exception>();
            });
        }

        public interface ITargetMethodNotFoundException
        {
            public void AddTypo(string key, string value);
        }

        public interface ITargetMethodNotFound2Exception
        {
            public void AddGeneric(object value);
        }

        public interface ITargetMethodNotFound3Exception
        {
            [Duck(GenericParameterTypeNames = new string[] { "P1", "P2" })]
            public void AddGeneric(object value);
        }

        internal class TargetMethodNotFoundExceptionClass
        {
            public void Add(string key, string value)
            {
            }

            public void AddGeneric<T>(T value)
            {
            }
        }

        // *

        [Fact]
        public void ProxyMethodParameterIsMissingException()
        {
            object target = new ProxyMethodParameterIsMissingExceptionClass();

            Assert.Throws<DuckTypeProxyMethodParameterIsMissingException>(() =>
            {
                target.DuckCast<IProxyMethodParameterIsMissingException>();
            });
        }

        public interface IProxyMethodParameterIsMissingException
        {
            [Duck(ParameterTypeNames = new string[] { "System.String", "System.String" })]
            public void Add(string key);
        }

        internal class ProxyMethodParameterIsMissingExceptionClass
        {
            public void Add(string key, string value)
            {
            }
        }

        // *

        [Fact]
        public void ProxyAndTargetMethodParameterSignatureMismatchException()
        {
            object target = new ProxyAndTargetMethodParameterSignatureMismatchExceptionClass();

            Assert.Throws<DuckTypeProxyAndTargetMethodParameterSignatureMismatchException>(() =>
            {
                target.DuckCast<IProxyAndTargetMethodParameterSignatureMismatchException>();
            });

            Assert.Throws<DuckTypeProxyAndTargetMethodParameterSignatureMismatchException>(() =>
            {
                target.DuckCast<IProxyAndTargetMethodParameterSignatureMismatch2Exception>();
            });
        }

        public interface IProxyAndTargetMethodParameterSignatureMismatchException
        {
            [Duck(ParameterTypeNames = new string[] { "System.String", "System.String" })]
            public void Add(string key, ref string value);
        }

        public interface IProxyAndTargetMethodParameterSignatureMismatch2Exception
        {
            [Duck(ParameterTypeNames = new string[] { "System.String", "System.String" })]
            public void Add(string key, out string value);
        }

        internal class ProxyAndTargetMethodParameterSignatureMismatchExceptionClass
        {
            public void Add(string key, string value)
            {
            }
        }

#if NET452
        // *
        [Fact]
        public void ProxyMethodsWithGenericParametersNotSupportedInNonPublicInstancesException()
        {
            object target = new ProxyMethodsWithGenericParametersNotSupportedInNonPublicInstancesExceptionClass();

            Assert.Throws<DuckTypeProxyMethodsWithGenericParametersNotSupportedInNonPublicInstancesException>(() =>
            {
                target.DuckCast<IProxyMethodsWithGenericParametersNotSupportedInNonPublicInstancesException>();
            });
        }

        public interface IProxyMethodsWithGenericParametersNotSupportedInNonPublicInstancesException
        {
            public void Add<TKey, TValue>(TKey key, TValue value);
        }

        internal class ProxyMethodsWithGenericParametersNotSupportedInNonPublicInstancesExceptionClass
        {
            public void Add<TKey, TValue>(TKey key, TValue value)
            {
            }
        }
#endif
        // *

        [Fact]
        public void TargetMethodAmbiguousMatchException()
        {
            object target = new TargetMethodAmbiguousMatchExceptionClass();

            Assert.Throws<DuckTypeTargetMethodAmbiguousMatchException>(() =>
            {
                target.DuckCast<ITargetMethodAmbiguousMatchException>();
            });
        }

        public interface ITargetMethodAmbiguousMatchException
        {
            public void Add(string key, object value);

            public void Add(string key, string value);
        }

        internal class TargetMethodAmbiguousMatchExceptionClass
        {
            public void Add(string key, Task value)
            {
            }

            public void Add(string key, string value)
            {
            }
        }

        // *

        [Fact]
        public void ProxyTypeDefinitionIsNull()
        {
            Assert.Throws<DuckTypeProxyTypeDefinitionIsNull>(() =>
            {
                DuckType.Create(null, new object());
            });
        }

        // *

        [Fact]
        public void TargetObjectInstanceIsNull()
        {
            Assert.Throws<DuckTypeTargetObjectInstanceIsNull>(() =>
            {
                DuckType.Create(typeof(ITargetObjectInstanceIsNull), null);
            });
        }

        public interface ITargetObjectInstanceIsNull
        {
        }

        // *

        [Fact]
        public void InvalidTypeConversionException()
        {
            object target = new InvalidTypeConversionExceptionClass();

            Assert.Throws<DuckTypeInvalidTypeConversionException>(() =>
            {
                target.DuckCast<IInvalidTypeConversionException>();
            });
        }

        public interface IInvalidTypeConversionException
        {
            float Sum(int a, int b);
        }

        public class InvalidTypeConversionExceptionClass
        {
            public int Sum(int a, int b)
            {
                return a + b;
            }
        }

        // *

        [Fact]
        public void ObjectInvalidTypeConversionException()
        {
            object target = new ObjectInvalidTypeConversionExceptionClass();

            Assert.Throws<DuckTypeInvalidTypeConversionException>(() =>
            {
                target.DuckCast<IObjectInvalidTypeConversionException>();
            });
        }

        public interface IObjectInvalidTypeConversionException
        {
            string Value { get; }
        }

        public class ObjectInvalidTypeConversionExceptionClass
        {
            public int Value => 42;
        }

        // *

        [Fact]
        public void ObjectInvalidTypeConversion2Exception()
        {
            object target = new ObjectInvalidTypeConversion2ExceptionClass();

            Assert.Throws<DuckTypeInvalidTypeConversionException>(() =>
            {
                target.DuckCast<IObjectInvalidTypeConversion2Exception>();
            });
        }

        public interface IObjectInvalidTypeConversion2Exception
        {
            int Value { get; }
        }

        public class ObjectInvalidTypeConversion2ExceptionClass
        {
            public string Value => "Hello world";
        }

        // *

        [Fact]
        public void ObjectInvalidTypeConversion3Exception()
        {
            object target = new ObjectInvalidTypeConversion3ExceptionClass();

            Assert.Throws<DuckTypeInvalidTypeConversionException>(() =>
            {
                target.DuckCast<IObjectInvalidTypeConversion3Exception>();
            });
        }

        public interface IObjectInvalidTypeConversion3Exception
        {
            [Duck(Kind = DuckKind.Field)]
            int Value { get; }
        }

        public class ObjectInvalidTypeConversion3ExceptionClass
        {
#pragma warning disable 414
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable SA1306 // Field names must begin with lower-case letter
            private readonly string Value = "Hello world";
#pragma warning restore SA1306 // Field names must begin with lower-case letter
#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore 414
        }
    }
}
