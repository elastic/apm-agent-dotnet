#region License

// Copyright (c) 2007 James Newton-King
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.

#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Elastic.Apm.Libraries.Newtonsoft.Json.Serialization;
#if !HAVE_LINQ
using Elastic.Apm.Libraries.Newtonsoft.Json.Utilities.LinqBridge;

#else
using System.Linq;

#endif

#nullable enable
namespace Elastic.Apm.Libraries.Newtonsoft.Json.Utilities
{
	internal class ReflectionMember
	{
		public Func<object, object?>? Getter { get; set; }
		public Type? MemberType { get; set; }
		public Action<object, object?>? Setter { get; set; }
	}

	internal class ReflectionObject
	{
		private ReflectionObject(ObjectConstructor<object>? creator)
		{
			Members = new Dictionary<string, ReflectionMember>();
			Creator = creator;
		}

		public ObjectConstructor<object>? Creator { get; }
		public IDictionary<string, ReflectionMember> Members { get; }

		public object? GetValue(object target, string member)
		{
			var getter = Members[member].Getter!;
			return getter(target);
		}

		public void SetValue(object target, string member, object? value)
		{
			var setter = Members[member].Setter!;
			setter(target, value);
		}

		public Type GetType(string member) => Members[member].MemberType!;

		public static ReflectionObject Create(Type t, params string[] memberNames) => Create(t, null, memberNames);

		public static ReflectionObject Create(Type t, MethodBase? creator, params string[] memberNames)
		{
			var delegateFactory = JsonTypeReflector.ReflectionDelegateFactory;

			ObjectConstructor<object>? creatorConstructor = null;
			if (creator != null)
				creatorConstructor = delegateFactory.CreateParameterizedConstructor(creator);
			else
			{
				if (ReflectionUtils.HasDefaultConstructor(t, false))
				{
					var ctor = delegateFactory.CreateDefaultConstructor<object>(t);

					creatorConstructor = args => ctor();
				}
			}

			var d = new ReflectionObject(creatorConstructor);

			foreach (var memberName in memberNames)
			{
				var members = t.GetMember(memberName, BindingFlags.Instance | BindingFlags.Public);
				if (members.Length != 1)
					throw new ArgumentException("Expected a single member with the name '{0}'.".FormatWith(CultureInfo.InvariantCulture, memberName));

				var member = members.Single();

				var reflectionMember = new ReflectionMember();

				switch (member.MemberType())
				{
					case MemberTypes.Field:
					case MemberTypes.Property:
						if (ReflectionUtils.CanReadMemberValue(member, false)) reflectionMember.Getter = delegateFactory.CreateGet<object>(member);

						if (ReflectionUtils.CanSetMemberValue(member, false, false))
							reflectionMember.Setter = delegateFactory.CreateSet<object>(member);
						break;
					case MemberTypes.Method:
						var method = (MethodInfo)member;
						if (method.IsPublic)
						{
							var parameters = method.GetParameters();
							if (parameters.Length == 0 && method.ReturnType != typeof(void))
							{
								var call = delegateFactory.CreateMethodCall<object>(method);
								reflectionMember.Getter = target => call(target);
							}
							else if (parameters.Length == 1 && method.ReturnType == typeof(void))
							{
								var call = delegateFactory.CreateMethodCall<object>(method);
								reflectionMember.Setter = (target, arg) => call(target, arg);
							}
						}
						break;
					default:
						throw new ArgumentException(
							"Unexpected member type '{0}' for member '{1}'.".FormatWith(CultureInfo.InvariantCulture, member.MemberType(),
								member.Name));
				}

				reflectionMember.MemberType = ReflectionUtils.GetMemberUnderlyingType(member);

				d.Members[memberName] = reflectionMember;
			}

			return d;
		}
	}
}
