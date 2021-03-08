using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Elastic.Apm.Mongo.DiagnosticSource
{
	internal sealed class MongoDbDiagnosticInitializer : IObserver<DiagnosticListener>, IDisposable
	{
		private readonly IApmAgent _apmAgent;

		private IDisposable _sourceSubscription;

		internal MongoDbDiagnosticInitializer(IApmAgent apmAgent) => _apmAgent = apmAgent;

		public void Dispose() => _sourceSubscription?.Dispose();

		[ExcludeFromCodeCoverage]
		public void OnCompleted()
		{
			// do nothing because it's not necessary to handle such event from provider
		}

		[ExcludeFromCodeCoverage]
		public void OnError(Exception error)
		{
			// do nothing because it's not necessary to handle such event from provider
		}

		public void OnNext(DiagnosticListener value)
		{
			if (value.Name == Constants.MongoDiagnosticName)
				_sourceSubscription = value.Subscribe(new MongoDiagnosticListener(_apmAgent));
		}
	}
}
