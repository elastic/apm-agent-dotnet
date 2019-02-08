using System;

namespace Elastic.Apm.DiagnosticSource
{
	public interface IDiagnosticsSubscriber
	{
		IDisposable Subscribe(IApmAgent components);
	}
}
