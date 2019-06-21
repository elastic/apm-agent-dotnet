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
		internal static IDiagnosticListener New(IApmAgent components)
		{
			var logger = components.Logger.Scoped(nameof(HttpDiagnosticListener));

			if (PlatformDetection.IsDotNetFullFramework)
			{
				logger.Debug()
					?.Log("Current runtime is detected as Full Framework. " +
						"RuntimeInformation.FrameworkDescription: {RuntimeInformation.FrameworkDescription}",
						RuntimeInformation.FrameworkDescription);
				return new HttpDiagnosticListenerFullFrameworkImpl(components);
			}

			logger.Debug()
				?.Log("Current runtime is not detected as Full Framework - returning implementation for Core. " +
					"RuntimeInformation.FrameworkDescription: {RuntimeInformation.FrameworkDescription}",
					RuntimeInformation.FrameworkDescription);
			return new HttpDiagnosticListenerCoreImpl(components);
		}
	}
}
