using System;

namespace AspNetCoreSampleApp.Models
{
	/// <summary>
	/// Represents a historical value with date. e.g. can be a historical price from a stock or from an index.
	/// </summary>
	public class HistoricalValue
	{
		private decimal _close;

		public decimal Close
		{
			get => Math.Round(_close, 2);
			set => _close = value;
		}

		public DateTime Date { get; set; }

		private decimal _high;

		public decimal High
		{
			get => Math.Round(_high, 2);
			set => _high = value;
		}

		private decimal _low;

		public decimal Low
		{
			get => Math.Round(_low, 2);
			set => _low = value;
		}

		private decimal _open;

		public decimal Open
		{
			get => Math.Round(_open, 2);
			set => _open = value;
		}
	}
}
