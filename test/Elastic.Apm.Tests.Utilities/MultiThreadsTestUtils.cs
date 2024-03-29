// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FluentAssertions;

namespace Elastic.Apm.Tests.Utilities
{
	internal static class MultiThreadsTestUtils
	{
		internal static int NumberOfThreadsForTest => Math.Max(Environment.ProcessorCount, 2);

		internal static IList<TResult> TestOnThreads<TResult>(Func<int, TResult> threadAction) =>
			TestOnThreads<TResult>(NumberOfThreadsForTest, threadAction);

		internal static TResult[] TestOnThreads<TResult>(int numberOfThreads, Func<int, TResult> threadAction)
		{
			numberOfThreads.Should().BeGreaterThan(1);

			var startBarrier = new Barrier(numberOfThreads);
			var results = new TResult[numberOfThreads];
			var exceptions = new Exception[numberOfThreads];
			var threads = Enumerable.Range(0, numberOfThreads).Select(i => new Thread(() => EachThreadDo(i))).ToList();

			foreach (var thread in threads)
				thread.Start();
			foreach (var thread in threads)
				thread.Join();

			// ReSharper disable once ImplicitlyCapturedClosure
			exceptions.ForEachIndexed((ex, threadIndex) =>
			{
				if (ex != null)
					throw new AggregateException($"Exception was thrown out of thread's (threadIndex: {threadIndex}) action", ex);
			});

			return results;

			void EachThreadDo(int threadIndex)
			{
				startBarrier.SignalAndWait();

				try
				{
					results[threadIndex] = threadAction(threadIndex);
				}
				catch (Exception ex)
				{
					exceptions[threadIndex] = ex;
				}
			}
		}
	}
}
