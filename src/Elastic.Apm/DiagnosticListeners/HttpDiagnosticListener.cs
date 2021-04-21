// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Runtime.InteropServices;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;

namespace Elastic.Apm.DiagnosticListeners
{
	/// <summary>
	/// Captures web requests initiated by <see cref="T:System.Net.Http.HttpClient" />
	/// </summary>
	internal static class HttpDiagnosticListener
	{
		internal static TraceableHttpDiagnosticListener New(IApmAgent components, bool startHttpSpan = true)
		{
			var logger = components.Logger.Scoped(nameof(HttpDiagnosticListener));

			if (PlatformDetection.IsDotNetFullFramework)
			{
				logger.Debug()
					?.Log("Current runtime is detected as Full Framework. " +
						"RuntimeInformation.FrameworkDescription: {RuntimeInformation.FrameworkDescription}",
						RuntimeInformation.FrameworkDescription);
				return new HttpDiagnosticListenerFullFrameworkImpl(components, startHttpSpan);
			}

			logger.Debug()
				?.Log("Current runtime is not detected as Full Framework - returning implementation for Core. " +
					"RuntimeInformation.FrameworkDescription: {RuntimeInformation.FrameworkDescription}",
					RuntimeInformation.FrameworkDescription);
			return new HttpDiagnosticListenerCoreImpl(components, startHttpSpan);
		}
	}
}
