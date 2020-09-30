// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Runtime.CompilerServices;
using Elastic.Apm.Api;
using Elastic.Apm.BackendComm;
using Elastic.Apm.BackendComm.CentralConfig;
using Elastic.Apm.Config;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Logging;
using Elastic.Apm.Report;

//TODO: It'd be nice to move this into the .csproj
[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.Extensions.Hosting, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]
[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.AspNetCore, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]
[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.EntityFrameworkCore, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]
[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.EntityFrameworkCore.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]
[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.EntityFramework6, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]
[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.SqlClient, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]
[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.SqlClient.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]
[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]
[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.AspNetCore.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]
[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.NetCoreAll, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]
[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.NetCoreAll.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]
[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.PerfTests, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]
[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.AspNetFullFramework, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]
[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.DockerTests, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]
[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.AspNetFullFramework.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]
[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.Tests.MockApmServer, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]
[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.Elasticsearch, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]
[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.Elasticsearch.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]
// Moq library uses CastleProxy to build mocks, which generates temporary assembly DynamicProxyGenAssembly2.
[assembly:
	InternalsVisibleTo(
		"DynamicProxyGenAssembly2, PublicKey=0024000004800000940000000602000000240000525341310004000001000100c547cac37abd99c8db225ef2f6c8a3602f3b3606cc9891605d02baa56104f4cfc0734aa39b93bf7852f7d9266654753cc297e7d2edfe0bac1cdcf9f717241550e0a7b191195b7667bb4f64bcb8e2121380fd1d9d46ad2d92d2d15605093924cceaf74c4861eff62abf69b9291ed0a340e113be11e6a7d3113e92484cf7045cc7")]
[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.Feature.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]
[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.EntityFramework6.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]
[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.GrpcClient, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]
[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.Grpc.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]



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

		internal ApmAgent(AgentComponents agentComponents) => Components = agentComponents ?? new AgentComponents();

		internal ICentralConfigFetcher CentralConfigFetcher => Components.CentralConfigFetcher;

		private AgentComponents Components { get; }
		internal IConfigStore ConfigStore => Components.ConfigStore;
		public IConfigurationReader ConfigurationReader => Components.ConfigurationReader;
		public IApmLogger Logger => Components.Logger;
		public IPayloadSender PayloadSender => Components.PayloadSender;
		public Service Service => Components.Service;
		public ITracer Tracer => Components.Tracer;

		internal Tracer TracerInternal => Components.TracerInternal;

		public void Dispose()
		{
			Disposables?.Dispose();
			Components?.Dispose();
		}
	}

	public static class Agent
	{
		private static readonly Lazy<ApmAgent> LazyApmAgent = new Lazy<ApmAgent>(() => new ApmAgent(_components));
		private static AgentComponents _components;
		private static volatile bool _isConfigured;

		public static IConfigurationReader Config => Instance.ConfigurationReader;

		internal static ApmAgent Instance => LazyApmAgent.Value;

		public static bool IsConfigured => _isConfigured;

		/// <summary>
		/// The entry point for manual instrumentation. The <see cref="Tracer" /> property returns the tracer,
		/// which you access to the currently active transaction and span and it also enables you to manually start
		/// a transaction.
		/// </summary>
		public static ITracer Tracer => Instance.Tracer;

		/// <summary>
		/// Adds a filter which gets called before each transaction gets sent to APM Server.
		/// In the
		/// <param name="filter"></param>
		/// you have access to the <see cref="ITransaction" /> instance which gets sent to APM Server
		/// and you can modify it. With the return value of the
		/// <param name="filter"></param>
		/// you can also control if the <see cref="ITransaction" /> should be sent to the server or not.
		/// If the
		/// <param name="filter"></param>
		/// returns a non-null <see cref="ITransaction"/> instance then it will be sent to the APM Server -
		/// if it returns <code>null</code>, the event will be dropped and won't be sent to the APM server.
		/// </summary>
		/// <param name="filter">The filter that can process the <see cref="ITransaction"/> and decide if it should be sent to APM Server or not.</param>
		/// <returns><code>true</code> if the filter was added successfully, <code>false</code> otherwise. In case the method returns <code>false</code> the filter won't be called.</returns>
		public static bool AddFilter(Func<ITransaction, ITransaction> filter) => CheckAndAddFilter(p => p.TransactionFilters.Add(filter));

		/// <summary>
		/// Adds a filter which gets called before each span gets sent to APM Server.
		/// In the
		/// <param name="filter"></param>
		/// you have access to the <see cref="ISpan" /> instance which gets sent to APM Server
		/// and you can modify it. With the return value of the
		/// <param name="filter"></param>
		/// you can also control if the <see cref="ISpan" /> should be sent to the server or not.
		/// If the
		/// <param name="filter"></param>
		/// returns a non-null <see cref="ISpan"/> instance then it will be sent to the APM Server -
		/// if it returns <code>null</code>, the event will be dropped and won't be sent to the APM server.
		/// </summary>
		/// <param name="filter">The filter that can process the <see cref="ISpan"/> and decide if it should be sent to APM Server or not.</param>
		/// <returns><code>true</code> if the filter was added successfully, <code>false</code> otherwise. In case the method returns <code>false</code> the filter won't be called.</returns>
		public static bool AddFilter(Func<ISpan, ISpan> filter) => CheckAndAddFilter(p => p.SpanFilters.Add(filter));

		/// <summary>
		/// Adds a filter which gets called before each error gets sent to APM Server.
		/// In the
		/// <param name="filter"></param>
		/// you have access to the <see cref="IError" /> instance which gets sent to APM Server
		/// and you can modify it. With the return value of the
		/// <param name="filter"></param>
		/// you can also control if the <see cref="IError" /> should be sent to the server or not.
		/// If the
		/// <param name="filter"></param>
		/// returns a non-null <see cref="IError"/> instance then it will be sent to the APM Server -
		/// if it returns <code>null</code>, the event will be dropped and won't be sent to the APM server.
		/// </summary>
		/// <param name="filter">The filter that can process the <see cref="IError"/> and decide if it should be sent to APM Server or not.</param>
		/// <returns><code>true</code> if the filter was added successfully, <code>false</code> otherwise. In case the method returns <code>false</code> the filter won't be called.</returns>
		public static bool AddFilter(Func<IError, IError> filter) => CheckAndAddFilter(p => p.ErrorFilters.Add(filter));

		private static bool CheckAndAddFilter(Action<PayloadSenderV2> action)
		{
			if (!(Instance.PayloadSender is PayloadSenderV2 payloadSenderV2)) return false;

			action(payloadSenderV2);
			return true;
		}

		/// <summary>
		/// Sets up multiple <see cref="IDiagnosticsSubscriber" />s to start listening to one or more
		/// <see cref="IDiagnosticListener" />s.
		/// </summary>
		/// <param name="subscribers">
		/// An array of <see cref="IDiagnosticsSubscriber" /> that will set up <see cref="IDiagnosticListener" /> subscriptions.
		/// </param>
		/// <returns>
		/// A disposable referencing all the subscriptions. Disposing this is not necessary for clean up, only to unsubscribe if
		/// desired.
		/// </returns>
		public static IDisposable Subscribe(params IDiagnosticsSubscriber[] subscribers) => Instance.Subscribe(subscribers);

		public static void Setup(AgentComponents agentComponents)
		{
			if (LazyApmAgent.IsValueCreated)
				throw new InstanceAlreadyCreatedException("The singleton APM agent has already been instantiated and can no longer be configured.");

			_components = agentComponents;
			_isConfigured = true;
		}

		internal class InstanceAlreadyCreatedException : Exception
		{
			internal InstanceAlreadyCreatedException(string message) : base(message) { }
		}
	}
}
