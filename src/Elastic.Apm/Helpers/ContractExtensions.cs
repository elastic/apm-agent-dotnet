// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;

namespace Elastic.Apm.Helpers
{
	internal static class ContractExtensions
	{
		public static T ThrowIfArgumentNull<T>(this T arg, string argName) where T : class =>
			arg ?? throw new ArgumentNullException(argName);

		public static T? ThrowIfNullableValueArgumentNull<T>(this T? arg, string argName) where T : struct =>
			arg ?? throw new ArgumentNullException(argName);

		public static int ThrowIfArgumentNegative(this int arg, string argName) =>
			arg < 0 ? throw new ArgumentException($"Argument {argName} should not be negative but its value is {arg}", argName) : arg;
	}
}
