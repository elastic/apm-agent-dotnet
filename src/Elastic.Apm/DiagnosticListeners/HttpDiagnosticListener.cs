using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;

namespace Elastic.Apm.DiagnosticListeners
{
	/// <inheritdoc />
	/// <summary>
	/// Captures web requests initiated by <see cref="T:System.Net.Http.HttpClient" />
	/// </summary>
	internal static class HttpDiagnosticListener
	{
		internal static IDiagnosticListener New(IApmAgent components)
		{
			var logger = components.Logger.Scoped(nameof(HttpDiagnosticListener));

			if (PlatformDetection.IsFullFramework)
			{
				logger.Debug()
					?.Log("Current runtime is detected as Full Framework. " +
						"RuntimeInformation.FrameworkDescription: {RuntimeInformation.FrameworkDescription}",
						RuntimeInformation.FrameworkDescription);
				return new HttpDiagnosticListenerFullFrameworkImpl(components);
			}

			logger.Debug()
				?.Log("Current runtime is not detected as Full Framework - assuming that it's Core. " +
					"RuntimeInformation.FrameworkDescription: {RuntimeInformation.FrameworkDescription}",
					RuntimeInformation.FrameworkDescription);
			return new HttpDiagnosticListenerCoreImpl(components);
		}

		internal static string ExceptionEventKey(IDiagnosticListener listener) =>
			PlatformDetection.IsFullFramework
				? ((HttpDiagnosticListenerFullFrameworkImpl)listener).ExceptionEventKey
				: ((HttpDiagnosticListenerCoreImpl)listener).ExceptionEventKey;

		internal static string StartEventKey(IDiagnosticListener listener) =>
			PlatformDetection.IsFullFramework
				? ((HttpDiagnosticListenerFullFrameworkImpl)listener).StartEventKey
				: ((HttpDiagnosticListenerCoreImpl)listener).StartEventKey;

		internal static string StopEventKey(IDiagnosticListener listener) =>
			PlatformDetection.IsFullFramework
				? ((HttpDiagnosticListenerFullFrameworkImpl)listener).StopEventKey
				: ((HttpDiagnosticListenerCoreImpl)listener).StopEventKey;

		internal static int ProcessingRequestsCount(IDiagnosticListener listener) =>
			PlatformDetection.IsFullFramework
				? ((HttpDiagnosticListenerFullFrameworkImpl)listener).ProcessingRequests.Count
				: ((HttpDiagnosticListenerCoreImpl)listener).ProcessingRequests.Count;

		internal static Span GetSpanForRequest(IDiagnosticListener listener, object request) =>
			PlatformDetection.IsFullFramework
				? ((HttpDiagnosticListenerFullFrameworkImpl)listener).ProcessingRequests[(HttpWebRequest)request]
				: ((HttpDiagnosticListenerCoreImpl)listener).ProcessingRequests[(HttpRequestMessage)request];
	}
}
