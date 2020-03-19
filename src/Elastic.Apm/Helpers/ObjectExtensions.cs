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
