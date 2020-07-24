// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

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
