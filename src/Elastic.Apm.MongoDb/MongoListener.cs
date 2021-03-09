using System.Diagnostics;
using MongoDB.Driver.Core.Events;

// ReSharper disable UnusedMember.Global

namespace Elastic.Apm.MongoDb
{
	internal class MongoListener
	{
		private static readonly System.Diagnostics.DiagnosticSource MongoLogger =
			new DiagnosticListener(Constants.MongoDiagnosticName);

		public void Handle(CommandStartedEvent @event)
		{
			if (MongoLogger.IsEnabled(Constants.Events.CommandStart))
				MongoLogger.Write(Constants.Events.CommandStart, new EventPayload<CommandStartedEvent>(@event));
		}

		public void Handle(CommandSucceededEvent @event)
		{
			if (MongoLogger.IsEnabled(Constants.Events.CommandEnd))
				MongoLogger.Write(Constants.Events.CommandEnd, new EventPayload<CommandSucceededEvent>(@event));
		}

		public void Handle(CommandFailedEvent @event)
		{
			if (MongoLogger.IsEnabled(Constants.Events.CommandFail))
				MongoLogger.Write(Constants.Events.CommandFail, new EventPayload<CommandFailedEvent>(@event));
		}
	}
}
