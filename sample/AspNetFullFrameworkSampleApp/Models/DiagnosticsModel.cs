using System;

namespace AspNetFullFrameworkSampleApp.Models
{
	public class DiagnosticsModel
	{
		public string EnvironmentVariables { get; set; }
		public string InMemoryLogContent { get; set; }
		public string LogFileContent { get; set; }
		public string LogFilePath { get; set; }
		public Exception LogFileReadException { get; set; }
		public string LoggingSubsystemInternalLogContent { get; set; }
	}
}
