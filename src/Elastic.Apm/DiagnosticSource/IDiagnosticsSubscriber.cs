// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;

namespace Elastic.Apm.DiagnosticSource
{
	public interface IDiagnosticsSubscriber
	{
		/// <summary>
		/// Subscribes to diagnostic listeners
		/// </summary>
		/// <param name="components">The agent components</param>
		/// <returns>A disposable</returns>
		IDisposable Subscribe(IApmAgent components);
	}
}
