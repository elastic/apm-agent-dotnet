// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Threading;

namespace Elastic.Apm.Profiler.Managed
{
	public static class AutoInstrumentation
	{
		private static int _firstInitialization = 1;

		public static void Initialize()
		{
			// check if already called
			if (Interlocked.Exchange(ref _firstInitialization, 0) != 1)
				return;

			try
			{
				// ensure global instance is created if it's not already
				_ = Agent.Instance;
			}
			catch
			{
				// ignore
			}
		}
	}
}
