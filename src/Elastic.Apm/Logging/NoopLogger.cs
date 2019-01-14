namespace Elastic.Apm.Logging
{
	internal class NoopLogger : AbstractLogger
	{
		protected NoopLogger() : base(LogLevelDefault) { }

		protected override void PrintLogline(string logline) { }

		public static NoopLogger Instance { get; } = new NoopLogger();
	}
}
