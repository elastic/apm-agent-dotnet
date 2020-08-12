// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
		private readonly string _folderPath;

		public CsvDataReader(string folderPath) => _folderPath = folderPath;

		public async Task<IEnumerable<HistoricalValue>> GetHistoricalQuotes(string symbol)
		{
			var retVal = new List<HistoricalValue>();
			symbol = symbol.Replace('.', '_');
			var logPath = _folderPath + Path.DirectorySeparatorChar + symbol + ".csv";
			var logFile = File.OpenRead(logPath);
			using (var logReader = new StreamReader(logFile))
			{
				string line;
				while ((line = await logReader.ReadLineAsync()) != null)
				{
					var items = line.Split(',');
					var date = items[0].Split('-');

					retVal.Add(new HistoricalValue
					{
						Date = new DateTime(int.Parse(date[0]), int.Parse(date[1]), int.Parse(date[2])),
						Close = decimal.Parse(items[1].Replace(',', '.'), CultureInfo.InvariantCulture),
						High = decimal.Parse(items[2].Replace(',', '.'), CultureInfo.InvariantCulture),
						Low = decimal.Parse(items[3].Replace(',', '.'), CultureInfo.InvariantCulture),
						Open = decimal.Parse(items[4].Replace(',', '.'), CultureInfo.InvariantCulture)
					});
				}
			}

			return retVal;
		}
	}
}
