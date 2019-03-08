using System;

namespace Elastic.Apm.Helpers
{
	public static class RandomGenerator
	{
		private static Random Random => new Random();

		private static readonly object LockObj = new object();

		public static void GetRandomBytes(byte[] bytes)
		{
			lock (LockObj)
			{
				Random.NextBytes(bytes);
			}
		}
	}
}
