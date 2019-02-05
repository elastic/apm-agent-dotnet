using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SampleAspNetCoreApp.Models;

namespace SampleAspNetCoreApp.Data
{
	/// <summary>
	/// This is a very simple custom class that reads CSV files.
	/// By default this is not captured by the agent, since it's an
	/// unknown class.
	/// In the samples we use this to create a custom span
	/// </summary>
	public class CsvDataReader
	{
		private string _folderPath;

		public CsvDataReader(String folderPath) => _folderPath = folderPath;

		public async Task<IEnumerable<HistoricalValue>> GetHistoricalQuotes(String symbol)
		{
			var retVal = new List<HistoricalValue>();
			symbol = symbol.Replace('.', '_');
			var logPath = _folderPath + System.IO.Path.DirectorySeparatorChar + symbol + ".csv";
			var logFile = System.IO.File.OpenRead(logPath);
			using (var logReader = new System.IO.StreamReader(logFile))
			{
				string line;
				while ((line = await logReader.ReadLineAsync()) != null)
				{
					var items = line.Split(',');
					var date = items[0].Split('-');

					retVal.Add(new HistoricalValue
					{
						Date = new DateTime(int.Parse(date[0]), int.Parse(date[1]), int.Parse(date[2])),
						Close = decimal.Parse(items[1].Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture),
						High = decimal.Parse(items[2].Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture),
						Low = decimal.Parse(items[3].Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture),
						Open = decimal.Parse(items[4].Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture),
						Volume = long.Parse(items[6].Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture)
					});
				}
			}

			return retVal;
		}
	}
}
