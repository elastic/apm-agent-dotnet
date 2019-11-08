namespace Elastic.Apm.Tests.TestHelpers
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
