using System;

namespace Elastic.Apm.Helpers
{
	public static class ObjectExtensions
	{
		// ReSharper disable once UnusedMember.Global
		public static Result Let<T, Result>(this T x, Func<T, Result> func)
		{
			return func(x);
		}

		public static void Let<T>(this T x, Action<T> action)
		{
			action(x);
		}
	}
}
