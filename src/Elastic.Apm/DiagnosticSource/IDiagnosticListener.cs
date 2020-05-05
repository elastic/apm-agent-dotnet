// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;

namespace Elastic.Apm.DiagnosticSource
{
	/// <summary>
	/// Common interface for every diagnostic listener
	/// The DiagnosticInitializer works through this interface with the different listeners
	/// </summary>
	internal interface IDiagnosticListener : IObserver<KeyValuePair<string, object>>
	{
		/// <summary>
		/// Represents the component associated with the event.
		/// </summary>
		/// <value>The name.</value>
		string Name { get; }
	}
}
