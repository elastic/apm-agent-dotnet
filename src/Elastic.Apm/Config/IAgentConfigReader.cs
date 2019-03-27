using System;
using System.Collections.Generic;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Config
{
	public interface IConfigurationReader
	{
		LogLevel LogLevel { get; }
		IReadOnlyList<Uri> ServerUrls { get; }
		string ServiceName { get; }
		string SecretToken { get; }
		bool CaptureHeaders { get; }
	}
}
