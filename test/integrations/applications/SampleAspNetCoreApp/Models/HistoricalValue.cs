// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;

namespace SampleAspNetCoreApp.Models
{
	/// <summary>
	/// Represents a historical value with date. e.g. can be a historical price from a stock or from an index.
	/// </summary>
	public class HistoricalValue
	{
		private decimal _close;

		private decimal _high;

		private decimal _low;

		private decimal _open;

		public decimal Close
		{
			get => Math.Round(_close, 2);
			set => _close = value;
		}

		public DateTime Date { get; set; }

		public decimal High
		{
			get => Math.Round(_high, 2);
			set => _high = value;
		}

		public decimal Low
		{
			get => Math.Round(_low, 2);
			set => _low = value;
		}

		public decimal Open
		{
			get => Math.Round(_open, 2);
			set => _open = value;
		}
	}
}
