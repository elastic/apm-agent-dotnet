// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Text;
using Elastic.Apm.Libraries.Ben.Demystifier.Enumerable;

#nullable enable
namespace Elastic.Apm.Libraries.Ben.Demystifier
{
	internal class ResolvedMethod
	{
		public Type? DeclaringType { get; set; }

		public string? GenericArguments { get; set; }

		public bool IsAsync { get; set; }

		public bool IsLambda { get; set; }
		public MethodBase? MethodBase { get; set; }

		public string? Name { get; set; }

		public int? Ordinal { get; set; }

		public EnumerableIList<ResolvedParameter> Parameters { get; set; }
		public int RecurseCount { get; internal set; }

		public Type[]? ResolvedGenericArguments { get; set; }

		public ResolvedParameter? ReturnParameter { get; set; }

		public string? SubMethod { get; set; }

		public MethodBase? SubMethodBase { get; set; }

		public EnumerableIList<ResolvedParameter> SubMethodParameters { get; set; }

		internal bool IsSequentialEquivalent(ResolvedMethod obj) =>
			IsAsync == obj.IsAsync &&
			DeclaringType == obj.DeclaringType &&
			Name == obj.Name &&
			IsLambda == obj.IsLambda &&
			Ordinal == obj.Ordinal &&
			GenericArguments == obj.GenericArguments &&
			SubMethod == obj.SubMethod;

		public override string ToString() => Append(new StringBuilder()).ToString();

		public StringBuilder Append(StringBuilder builder)
			=> Append(builder, true);

		public StringBuilder Append(StringBuilder builder, bool fullName)
		{
			if (IsAsync) builder.Append("async ");

			if (ReturnParameter != null)
			{
				ReturnParameter.Append(builder);
				builder.Append(" ");
			}

			if (DeclaringType != null)
			{
				if (Name == ".ctor")
				{
					if (string.IsNullOrEmpty(SubMethod) && !IsLambda)
						builder.Append("new ");

					AppendDeclaringTypeName(builder, fullName);
				}
				else if (Name == ".cctor")
				{
					builder.Append("static ");
					AppendDeclaringTypeName(builder, fullName);
				}
				else
				{
					AppendDeclaringTypeName(builder, fullName)
						.Append(".")
						.Append(Name);
				}
			}
			else
				builder.Append(Name);
			builder.Append(GenericArguments);

			builder.Append("(");
			if (MethodBase != null)
			{
				var isFirst = true;
				foreach (var param in Parameters)
				{
					if (isFirst)
						isFirst = false;
					else
						builder.Append(", ");
					param.Append(builder);
				}
			}
			else
				builder.Append("?");
			builder.Append(")");

			if (!string.IsNullOrEmpty(SubMethod) || IsLambda)
			{
				builder.Append("+");
				builder.Append(SubMethod);
				builder.Append("(");
				if (SubMethodBase != null)
				{
					var isFirst = true;
					foreach (var param in SubMethodParameters)
					{
						if (isFirst)
							isFirst = false;
						else
							builder.Append(", ");
						param.Append(builder);
					}
				}
				else
					builder.Append("?");
				builder.Append(")");
				if (IsLambda)
				{
					builder.Append(" => { }");

					if (Ordinal.HasValue)
					{
						builder.Append(" [");
						builder.Append(Ordinal);
						builder.Append("]");
					}
				}
			}

			if (RecurseCount > 0) builder.Append($" x {RecurseCount + 1:0}");

			return builder;
		}

		private StringBuilder AppendDeclaringTypeName(StringBuilder builder, bool fullName = true) =>
			DeclaringType != null
				? builder.AppendTypeDisplayName(DeclaringType, fullName, true)
				: builder;
	}
}
