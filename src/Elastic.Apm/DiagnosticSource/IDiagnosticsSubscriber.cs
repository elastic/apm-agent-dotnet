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

	/// <summary>
	/// A base diagnostic subscriber that subscribes to diagnostic
	/// listeners only if the agent is enabled
	/// </summary>
	public abstract class DiagnosticsSubscriberBase : IDiagnosticsSubscriber
	{
		/// <summary>
		/// Subscribes to diagnostic listeners only if the agent is enabled
		/// </summary>
		/// <param name="components">The agent components</param>
		/// <returns>A disposable</returns>
		public IDisposable Subscribe(IApmAgent components)
		{
			var disposable = new CompositeDisposable();
			return components.ConfigurationReader.Enabled
				? Subscribe(components, disposable)
				: disposable;
		}

		/// <summary>
		/// Subscribes to diagnostic listeners
		/// </summary>
		/// <param name="components">The agent components</param>
		/// <param name="disposable">A disposable to which other disposables can be added</param>
		/// <returns>A disposable</returns>
		protected abstract IDisposable Subscribe(IApmAgent components, ICompositeDisposable disposable);
	}
}
