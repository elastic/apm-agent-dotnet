using System;
using System.Threading.Tasks;

namespace Elastic.Apm.Helpers
{
	public static class IntExtensions
	{
		public static void Repeat(this int repeatCount, Action action)
		{
			for (var i = 0; i < repeatCount; ++i)
				action();
		}

		public static void Repeat(this int repeatCount, Action<int> action)
		{
			for (var i = 0; i < repeatCount; ++i)
				action(i);
		}

		public static async Task Repeat(this int repeatCount, Func<int, Task> action)
		{
			for (var i = 0; i < repeatCount; ++i)
				await action(i);
		}
	}
}
