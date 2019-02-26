using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Elastic.Apm.Tests
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

		public static int A(this string str) => 24;

		public static void AssertMinimumSleepLength(double? duration)
			=> Assert.True(duration >= SleepLength, $"Expected {duration} to be greater or equal to: {SleepLength}");

		public static void Assert3XMinimumSleepLength(double? duration)
		{
			var expectedTransactionLength = SleepLength + 2 *  SleepLength;
			Assert.True(duration >= expectedTransactionLength, $"Expected {duration} to be greater or equal to: {expectedTransactionLength}");
		}
	}
}
