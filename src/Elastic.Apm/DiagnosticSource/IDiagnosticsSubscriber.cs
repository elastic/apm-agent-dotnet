using System;
using Elastic.Apm.Config;

namespace Elastic.Apm.DiagnosticSource
{
	public interface IDiagnosticsSubscriber
	{
		IDisposable Subscribe(IApmAgent components);
	}
}
