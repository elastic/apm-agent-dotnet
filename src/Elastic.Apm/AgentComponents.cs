using System;
using System.IO;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Report;

namespace Elastic.Apm
{
	public class AgentComponents : IApmAgent, IDisposable
	{
		public AgentComponents(
			IApmLogger logger = null,
			IConfigurationReader configurationReader = null,
			IPayloadSender payloadSender = null
		)
		{
			Logger = logger ?? ConsoleLogger.LoggerOrDefault(configurationReader?.LogLevel);
			ConfigurationReader = configurationReader ?? new EnvironmentConfigurationReader(Logger);

			Service = Service.GetDefaultService(ConfigurationReader);

			var system = new Api.System();

			try
			{
				if (File.Exists("/proc/self/cgroup"))
				{
					using (var sr = new StreamReader("/proc/self/cgroup"))
					{
						var line = sr.ReadLine();

						while (line != null)
						{
							var fields = line.Split(':');
							if (fields.Length == 3)
							{
								var dirAndId = fields[2].Split('/');

								if (dirAndId.Length == 2)
								{
									var id = dirAndId[1].ToLower().EndsWith(".scope") ? dirAndId[1].Substring(".scope".Length) : dirAndId[1];
									system.Container = new Container { Id = id };
								}
							}
							line = sr.ReadLine();
						}
					}
				}
			}
			catch (Exception e)
			{
				Logger.Error()?.LogException(e, "Failed reading container id");
			}

			PayloadSender = payloadSender ?? new PayloadSenderV2(Logger, ConfigurationReader, Service, system);
			TracerInternal = new Tracer(Logger, Service, PayloadSender, ConfigurationReader);
			TransactionContainer = new TransactionContainer();
		}

		public IConfigurationReader ConfigurationReader { get; }

		public IApmLogger Logger { get; }

		public IPayloadSender PayloadSender { get; }

		/// <summary>
		/// Identifies the monitored service. If this remains unset the agent
		/// automatically populates it based on the entry assembly.
		/// </summary>
		/// <value>The service.</value>
		public Service Service { get; }

		public ITracer Tracer => TracerInternal;

		private Tracer TracerInternal { get; }

		internal TransactionContainer TransactionContainer { get; }

		public void Dispose()
		{
			if (PayloadSender is IDisposable disposable) disposable.Dispose();
		}
	}
}
