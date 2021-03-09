using System;
using System.Diagnostics;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.MongoDb.DiagnosticSource;

// ReSharper disable UnusedMember.Global

namespace Elastic.Apm.MongoDb
{
	/// <summary>
	///     A subscriber to events from mongoDB driver diagnostic source.
	/// </summary>
	public class MongoDbDiagnosticsSubscriber : IDiagnosticsSubscriber
	{
		/// <summary>
		/// Starts listening for mongoDB driver diagnostic source events
		/// </summary>
		public IDisposable Subscribe(IApmAgent components)
		{
			var retVal = new CompositeDisposable();

			if (!components.ConfigurationReader.Enabled)
				return retVal;

			var initializer = new MongoDbDiagnosticInitializer(components);

			retVal.Add(initializer);

			retVal.Add(DiagnosticListener
				.AllListeners
				.Subscribe(initializer));

			return retVal;
		}
	}
}
