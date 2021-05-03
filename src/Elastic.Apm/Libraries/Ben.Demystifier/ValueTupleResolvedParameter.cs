// Copyright (c) Ben A Adams. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Elastic.Apm.Libraries.Ben.Demystifier.Internal;

#nullable enable
namespace Elastic.Apm.Libraries.Ben.Demystifier
{
	internal class ValueTupleResolvedParameter : ResolvedParameter
	{
		public ValueTupleResolvedParameter(Type resolvedType, IList<string> tupleNames)
			: base(resolvedType)
			=> TupleNames = tupleNames;

		public IList<string> TupleNames { get; }

		protected override void AppendTypeName(StringBuilder sb)
		{
			if (ResolvedType is not null)
			{
				if (ResolvedType.IsValueTuple())
					AppendValueTupleParameterName(sb, ResolvedType);
				else
				{
					// Need to unwrap the first generic argument first.
					sb.Append(TypeNameHelper.GetTypeNameForGenericType(ResolvedType));
					sb.Append("<");
					AppendValueTupleParameterName(sb, ResolvedType.GetGenericArguments()[0]);
					sb.Append(">");
				}
			}
		}

		private void AppendValueTupleParameterName(StringBuilder sb, Type parameterType)
		{
			sb.Append("(");
			var args = parameterType.GetGenericArguments();
			for (var i = 0; i < args.Length; i++)
			{
				if (i > 0) sb.Append(", ");

				sb.AppendTypeDisplayName(args[i], false, true);

				if (i >= TupleNames.Count) continue;

				var argName = TupleNames[i];
				if (argName == null) continue;

				sb.Append(" ");
				sb.Append(argName);
			}

			sb.Append(")");
		}
	}
}
