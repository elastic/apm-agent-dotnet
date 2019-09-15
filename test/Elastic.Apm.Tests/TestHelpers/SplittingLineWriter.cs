namespace Elastic.Apm.Tests.TestHelpers
{
	public class SplittingLineWriter : ILineWriter
	{
		public SplittingLineWriter(params ILineWriter[] lineWriters) => _lineWriters = lineWriters;

		private readonly ILineWriter[] _lineWriters;

		public void WriteLine(string line)
		{
			foreach (var lineWriter in _lineWriters) lineWriter.WriteLine(line);
		}
	}
}
