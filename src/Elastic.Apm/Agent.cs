using System;
using System.Runtime.CompilerServices;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Logging;
using Elastic.Apm.Model.Payload;
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
[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.All, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]
[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.All.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]
[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.PerfTests, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]

namespace Elastic.Apm
{
	public interface IApmAgent
	{
		IConfigurationReader ConfigurationReader { get; }

		IApmLogger Logger { get; }

		IPayloadSender PayloadSender { get; }

		Service Service { get; }

		ITracer Tracer { get; }
	}

	internal class ApmAgent : IApmAgent, IDisposable
	{
		internal readonly CompositeDisposable Disposables = new CompositeDisposable();

		public ApmAgent(AgentComponents agentComponents) => Components = agentComponents ?? new AgentComponents();

		private AgentComponents Components { get; }
		public IConfigurationReader ConfigurationReader => Components.ConfigurationReader;
		public IApmLogger Logger => Components.Logger;
		public IPayloadSender PayloadSender => Components.PayloadSender;
		public ITracer Tracer => Components.Tracer;
		public Service Service => Components.Service;

		internal Tracer TracerInternal => Tracer as Tracer;

		internal TransactionContainer TransactionContainer => Components.TransactionContainer;

		public void Dispose()
		{
			Disposables?.Dispose();
			Components?.Dispose();
		}
	}

	public static class Agent
	{
		private static readonly Lazy<ApmAgent> Lazy = new Lazy<ApmAgent>(() => new ApmAgent(_components));
		private static AgentComponents _components;

		public static IConfigurationReader Config => Lazy.Value.ConfigurationReader;

		internal static ApmAgent Instance => Lazy.Value;

		/// <summary>
		/// The entry point for manual instrumentation. The <see cref="Tracer" /> property returns the tracer,
		/// which you access to the currently active transaction and span and it also enables you to manually start
		/// a transaction.
		/// </summary>
		public static ITracer Tracer => Instance.Tracer;

		internal static TransactionContainer TransactionContainer => Instance.TransactionContainer;

		/// <summary>
		/// Sets up multiple <see cref="IDiagnosticsSubscriber" />'s to start listening to one or more
		/// <see cref="IDiagnosticListener" />'s
		/// </summary>
		/// <param name="subscribers">
		/// An array of <see cref="IDiagnosticsSubscriber" /> that will set up <see cref="IDiagnosticListener" /> subscriptions
		/// </param>
		/// <returns>
		/// A disposable referencing all the subscriptions, disposing this is not necessary for clean up, only to unsubscribe if
		/// desired.
		/// </returns>
		public static IDisposable Subscribe(params IDiagnosticsSubscriber[] subscribers) => Instance.Subscribe(subscribers);

		public static void Setup(AgentComponents agentComponents)
		{
			if (Lazy.IsValueCreated) throw new Exception("The singleton APM agent has already been instantiated and can no longer be configured");

			_components = agentComponents;
		}
	}
}
