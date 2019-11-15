using System;
using System.Text;

namespace Elastic.Apm
{
	/// <summary>
	/// A sampler is responsible for determining whether a transaction should be sampled.
	/// In contrast to other tracing systems, in Elastic APM,  non-sampled transactions do get reported to the APM server.
	/// However, to keep the size at a minimum, the reported transaction only contains the transaction name, the duration and
	/// the id.
	/// Also, spans of non-sampled transactions are not reported.
	/// This implementation samples based on a sampling probability (AKA sampling rate) between 0.0 and 1.0.
	/// A sampling rate of 0.5 means that 50% of all transactions should be sampled.
	/// </summary>
	internal readonly struct Sampler
	{
		private readonly ulong _maxSampledUInt64;
		private readonly double _rate;

		/// <summary>
		/// Constructs a new Sampler
		/// </summary>
		/// <param name="rate">Value of the rate - must be between 0 and 1 (including both)</param>
		/// <exception cref="System.ArgumentOutOfRangeException">Thrown when rate is not between 0 and 1 (including both)</exception>
		/// <returns>The same value as the given rate</returns>
		internal Sampler(double rate)
		{
			if (!IsValidRate(rate)) throw new ArgumentOutOfRangeException($"Invalid rate: {rate} - it must be between 0 and 1 (including both)");

			_rate = rate;
			switch (_rate)
			{
				case 0:
					Constant = false;
					_maxSampledUInt64 = 0;
					break;
				case 1:
					Constant = true;
					_maxSampledUInt64 = ulong.MaxValue;
					break;
				default:
					_maxSampledUInt64 = Convert.ToUInt64(ulong.MaxValue * rate);
					Constant = null;
					break;
			}
		}

		internal bool? Constant { get; }

		/// <summary>
		/// Decides if to sample or not based on the given randomBytes.
		/// </summary>
		/// <param name="randomBytes">Should contain at least 8 random bytes.</param>
		/// <exception cref="System.ArgumentException">
		/// When length of <paramref name="randomBytes">randomBytes</paramref> is less
		/// than 8.
		/// </exception>
		/// <returns>True if and only if the decision is to sample</returns>
		internal bool DecideIfToSample(byte[] randomBytes) => Constant ?? BitConverter.ToUInt64(randomBytes, 0) <= _maxSampledUInt64;

		internal static bool IsValidRate(double rate) => 0 <= rate && rate <= 1.0;

		public override string ToString()
		{
			var retVal = new StringBuilder();
			retVal.Append(nameof(Sampler));
			retVal.Append("{ ");
			if (Constant.HasValue)
				retVal.Append($"constant: {Constant}");
			else
				retVal.Append($"rate: {_rate}");
			retVal.Append(" }");
			return retVal.ToString();
		}
	}
}
