// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;

namespace Elastic.Apm.Tests.Extensions
{
	/// <summary>
	/// Helper class that runs Task.Delay and Thread.Sleep
	/// that delays/sleeps according to the frequency of the timer
	/// and also offers asserts based on this frequency.
	/// The motivation is to have asserts that can reliable assert on the
	/// duration of spans/transactions which is only possible
	/// if the sleeps and delays are adjusted to the timer's frequency.
	/// </summary>
	public static class WaitHelpers
	{
		public static int SleepLength
		{
			get
			{
				var frequency = Stopwatch.Frequency;
				return 1000L / frequency < 1 ? 1 : (int)(1000L / frequency);
			}
		}

		public static async Task Delay2XMinimum() => await Task.Delay(2 * SleepLength);

		public static void Sleep2XMinimum() => Thread.Sleep(2 * SleepLength);

		public static async Task DelayMinimum() => await Task.Delay(SleepLength);

		public static void SleepMinimum() => Thread.Sleep(SleepLength);

		public static void Assert3XMinimumSleepLength(double? duration) => duration.Should().BeGreaterOrEqualToMinimumSleepLength(3);
	}
}
