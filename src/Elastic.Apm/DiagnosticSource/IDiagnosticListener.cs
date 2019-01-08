using System;
using System.Collections.Generic;

namespace Elastic.Apm.DiagnosticSource
{
	/// <summary>
	/// Common interface for every diagnostic listener
	/// The DiagnisticInitializer works through this interface with the different listeners
	/// </summary>
	public interface IDiagnosticListener : IObserver<KeyValuePair<string, object>>
	{
		/// <summary>
		/// Represents the component associated with the event.
		/// </summary>
		/// <value>The name.</value>
		String Name { get; }
	}
}
