using System;
using Elastic.Apm.Helpers;

namespace Elastic.Apm.Api
{
	internal class Sampler
	{
		private readonly bool _constantValue;
		private readonly bool _isConstant;
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
					_isConstant = true;
					_constantValue = false;
					break;
				case 1:
					_isConstant = true;
					_constantValue = true;
					break;
				default:
					_isConstant = false;
					break;
			}
		}

		/// <summary>
		/// Determines if to sample or not this time
		/// </summary>
		/// <returns>True if and only if the decision is to sample</returns>
		internal bool DecideIfToSample()
		{
			if (_isConstant) return _constantValue;

			var randomDoubleBetween0And1 = RandomGenerator.GenerateRandomDoubleBetween0And1();
			return randomDoubleBetween0And1 <= _rate;
		}

		internal static bool IsValidRate(double rate) => 0 <= rate && rate <= 1.0;
	}
}
