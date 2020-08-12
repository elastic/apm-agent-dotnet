// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;

namespace Elastic.Apm.Helpers
{
	internal static class ObjectExtensions
	{
		public const string NullAsString = "<null>";

		// ReSharper disable once UnusedMember.Global
		public static TResult Let<T, TResult>(this T x, Func<T, TResult> func) => func(x);

		public static void Let<T>(this T x, Action<T> action) => action(x);

		public static string AsNullableToString(this object value) => value?.ToString() ?? NullAsString;

		public static string AsNullableToString<T>(this T? value) where T : struct => value?.ToString() ?? NullAsString;
	}
}
