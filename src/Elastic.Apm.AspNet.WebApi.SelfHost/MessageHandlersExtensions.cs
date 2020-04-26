using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Logging;

namespace Elastic.Apm.AspNet.WebApi.SelfHost
{
	public static class MessageHandlersExtensions
	{
		// Extension method on System.Web.Http.HttpConfiguration is needed to access Routes property in the future
		/// <summary>
		///
		/// </summary>
		/// <param name="configuration"></param>
		/// <param name="logger"></param>
		/// <param name="subscribers"></param>
		public static void AddElasticApm(this System.Web.Http.HttpConfiguration configuration, IApmLogger logger = null,
			params IDiagnosticsSubscriber[] subscribers
		)
		{
			// ConsoleLogger.Instance has Error as a default log level. By this reason, we need to read LogLevel from configuration reader, however,
			// configuration reader needs logger to instantiate.
			// By this reason, we pass ConsoleLogger.Instance to configuration reader and instantiate new logger for the rest of agent components
			var configurationReader = new FullFrameworkConfigReader(logger ?? ConsoleLogger.Instance);
			var rootLogger = logger ?? ConsoleLogger.LoggerOrDefault(configurationReader.LogLevel);
			var config = new AgentComponents(rootLogger, configurationReader: configurationReader);

			config.Service.Language = new Language { Name = "C#" }; //TODO

			Agent.Setup(config);

			var subs = new List<IDiagnosticsSubscriber>(subscribers ?? Array.Empty<IDiagnosticsSubscriber>()) { new HttpDiagnosticsSubscriber() };

			Agent.Subscribe(subs.ToArray());

			configuration.MessageHandlers.Add(new ApmMessageHandler(Agent.Instance));
		}
	}
}
