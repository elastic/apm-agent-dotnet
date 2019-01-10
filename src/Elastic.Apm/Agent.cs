using System;
using System.Runtime.CompilerServices;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Report;

//TODO: It'd be nice to move this into the .csproj
[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.AspNetCore, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]
[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.EntityFrameworkCore, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]
[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]
[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.AspNetCore.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]

namespace Elastic.Apm
{
	public static class Agent
	{
		/// <summary>
		/// By default the agent reads configs from environment variables and it uses the <see cref="EnvironmentVariableConfig" />
		/// class.
		/// This behaviour can be overwritten via the <see cref="Config" /> property.
		/// For example in ASP.NET Core "Elastic.Apm.AspNetCore.Config.MicrosoftExtensionsConfig"
		/// with the actual "IConfiguration" instance can be created and passed to the agent.
		/// With that the agent will read configs from the "IConfiguration" instance.
		/// </summary>
		private static AbstractAgentConfig _config = new EnvironmentVariableConfig();

		private static Type _loggerType = typeof(ConsoleLogger);

		private static IPayloadSender _payloadSender;

		/// <summary>
		/// The entry point for manual instrumentation. The <see cref="Tracer" /> property returns the tracer,
		/// which you access to the currently active transaction and span and it also enables you to manually start
		/// a transaction.
		/// </summary>
		private static Tracer _tracer;

		/// <summary>
		/// The current agent config. This property stores all configs.
		/// </summary>
		/// <value>The config.</value>
		public static AbstractAgentConfig Config
		{
			get
			{
				if (_config?.Logger != null) return _config;

				if (_config != null)
					_config.Logger = CreateLogger("Config");

				return _config;
			}
			set
			{
				_config = value;
				_config.Logger = CreateLogger("Config");
			}
		}

		public static IPayloadSender PayloadSender
		{
			get => _payloadSender ?? (_payloadSender = new PayloadSender());
			internal set => _payloadSender = value;
		}

		public static ITracer Tracer => _tracer ?? (_tracer = new Tracer());


		/// <summary>
		/// Returns a logger with a specific prefix. The idea behind this class
		/// is that each component in the agent creates its own agent with a
		/// specific <paramref name="prefix" /> which makes correlating logs easier
		/// </summary>
		/// <returns>The logger.</returns>
		/// <param name="prefix">Prefix.</param>
		public static AbstractLogger CreateLogger(string prefix = "")
		{
			if (!(Activator.CreateInstance(_loggerType) is AbstractLogger logger)) return null;

			logger.Prefix = prefix;
			return logger;
		}

		/// <summary>
		/// Sets the type of the logger.
		/// </summary>
		/// <param name="t">T.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		internal static void SetLoggerType<T>() where T : AbstractLogger, new() => _loggerType = typeof(T);
	}
}
