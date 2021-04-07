// Based on the elastic-apm-mongo project by Vadim Hatsura (@vhatsura)
// https://github.com/vhatsura/elastic-apm-mongo
// Licensed to Elasticsearch B.V under the Apache 2.0 License.
// Elasticsearch B.V licenses this file, including any modifications, to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

			var initializer = new DiagnosticInitializer(components.Logger,
				new[] { new MongoDiagnosticListener(components) });

			retVal.Add(initializer);

			retVal.Add(DiagnosticListener
				.AllListeners
				.Subscribe(initializer));

			return retVal;
		}
	}
}
