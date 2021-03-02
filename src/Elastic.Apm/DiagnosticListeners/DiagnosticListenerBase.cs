// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Logging;

namespace Elastic.Apm.DiagnosticListeners
{
	/// <summary>
	/// A base class for DiagnosticSource listeners which encapsulates common functionality - e.g. exception handling.
	/// </summary>
	public abstract class DiagnosticListenerBase : IDiagnosticListener
	{
		/// <summary>
		/// A logger scoped to the child class.
		/// </summary>
		protected IApmLogger Logger { get; }

		/// <summary>
		/// Current Agent instance.
		/// </summary>
		protected IApmAgent ApmAgent { get; }

		protected DiagnosticListenerBase(IApmAgent apmAgent)
		{
			ApmAgent = apmAgent;
			Logger = apmAgent.Logger.Scoped(GetType().Name);
		}

		/// <summary>
		/// Fires each time when <see cref="OnNext"/> is called.
		/// Code within this method is guarded by a try-catch.
		/// </summary>
		/// <param name="kv"></param>
		protected abstract void HandleOnNext(KeyValuePair<string, object> kv);

		public abstract string Name { get; }

		public virtual void OnCompleted() { }

		public virtual void OnError(Exception error) { }

		public virtual void OnNext(KeyValuePair<string, object> kv)
		{
			try
			{
				HandleOnNext(kv);
			}
			catch (Exception e)
			{
				Logger.Error()?.LogException(e, "Failed capturing DiagnosticSource event");
			}
		}
	}
}
