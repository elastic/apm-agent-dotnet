// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Diagnostics;
using System.Threading;

namespace SampleAspNetCoreApp.Utils;

/// <summary>
/// From: https://github.com/elastic/apm-agent-dotnet/issues/1571#issuecomment-984520076
/// </summary>
public class CpuBurner
{
	public static void ConsumeMultipleCores(int threads, int percentage, CancellationToken cancellationToken)
	{
		for (var i = 0; i < threads; i++)
		{
			ThreadPool.QueueUserWorkItem<object>(
				callBack: s => ConsumeSingleCore(percentage, cancellationToken),
				state: null,
				preferLocal: false);
		}
	}

	private static long ConsumeSingleCore(int percentage, CancellationToken cancellationToken)
	{
		if (percentage < 0 || percentage > 100)
		{
			throw new ArgumentException(nameof(percentage));
		}

		var iterations = 0L;
		var watch = new Stopwatch();
		watch.Start();
		while (!cancellationToken.IsCancellationRequested)
		{
			// Make the loop go on for "percentage" milliseconds then sleep the
			// remaining percentage milliseconds. So 40% utilization means work 40ms and sleep 60ms
			if (watch.ElapsedMilliseconds > percentage)
			{
				if (percentage != 100)
				{
					Thread.Sleep(100 - percentage);
				}

				watch.Reset();
				watch.Start();
			}

			iterations++;
		}

		return iterations;
	}
}
