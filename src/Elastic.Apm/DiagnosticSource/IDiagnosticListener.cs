using System;
using System.Collections.Generic;

namespace Elastic.Apm.DiagnosticSource
{
	/// <summary>
	/// Common interface for every diagnostic listener
	/// The DiagnosticInitializer works through this interface with the different listeners
	/// </summary>
	public interface IDiagnosticListener : IObserver<KeyValuePair<string, object>>
	{
		/// <summary>
		/// Represents the component associated with the event.
		/// </summary>
		/// <value>The name.</value>
		string Name { get; }

		/// <summary>
		/// Reference to the source subscription.
		/// This is set by <see cref="DiagnosticInitializer"/>
		/// </summary>
		IDisposable SourceSubscription { get; set; }
	}
}
