// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Apm.Tests.Utilities
{
	public class SplittingLineWriter : ILineWriter
	{
		private readonly ILineWriter[] _lineWriters;

		public SplittingLineWriter(params ILineWriter[] lineWriters) => _lineWriters = lineWriters;

		public void WriteLine(string line)
		{
			foreach (var lineWriter in _lineWriters) lineWriter.WriteLine(line);
		}
	}
}
