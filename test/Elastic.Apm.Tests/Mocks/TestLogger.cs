using System;
using System.Collections.Generic;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Tests.Mocks
{
	public class TestLogger : AbstractLogger
	{
		public List<string> Lines { get; } = new List<string>();

		protected override void PrintLogline(string logline) => Lines.Add(logline);
	}
}
