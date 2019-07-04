using System;

namespace Elastic.Apm.Helpers
{
	public static class ObjectExtensions
	{
		// ReSharper disable once UnusedMember.Global
		public static TResult Let<T, TResult>(this T x, Func<T, TResult> func) => func(x);

		public static void Let<T>(this T x, Action<T> action) => action(x);
	}
}
