// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Profiler.Managed.Core;
using Elastic.Apm.Profiler.Managed.Reflection;

namespace Elastic.Apm.Profiler.Managed.Integrations
{
	public static class ElasticsearchNet6Integration
	{
		private const string Version6 = "6";
		private const string ElasticsearchAssemblyName = "Elasticsearch.Net";
		private const string RequestPipelineInterfaceTypeName = "Elasticsearch.Net.IRequestPipeline";

		public static object CallElasticsearch<TResponse>(
			object pipeline,
			object requestData,
			int opCode,
			int mdToken,
			long moduleVersionPtr
		)
		{
			if (pipeline == null)
			{
				throw new ArgumentNullException(nameof(pipeline));
			}

			const string methodName = nameof(CallElasticsearch);
			Func<object, object, TResponse> callElasticSearch;
			var pipelineType = pipeline.GetType();
			var genericArgument = typeof(TResponse);

			try
			{
				callElasticSearch =
					MethodBuilder<Func<object, object, TResponse>>
						.Start(moduleVersionPtr, mdToken, opCode, methodName)
						.WithConcreteType(pipelineType)
						.WithMethodGenerics(genericArgument)
						.WithParameters(requestData)
						.WithNamespaceAndNameFilters(ClrTypeNames.Ignore, "Elasticsearch.Net.RequestData")
						.Build();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				throw;
			}

			return callElasticSearch(pipeline, requestData);
		}

		public static object CallElasticsearchAsync<TResponse>(
			object pipeline,
			object requestData,
			object boxedCancellationToken,
			int opCode,
			int mdToken,
			long moduleVersionPtr)
		{
			var cancellationToken = (CancellationToken)boxedCancellationToken;
			return CallElasticsearchAsyncInternal<TResponse>(pipeline, requestData, cancellationToken, opCode, mdToken, moduleVersionPtr);
		}

		private static Task<TResponse> CallElasticsearchAsyncInternal<TResponse>(
			object pipeline,
			object requestData,
			CancellationToken cancellationToken,
			int opCode,
			int mdToken,
			long moduleVersionPtr)
		{
			const string methodName = "CallElasticsearchAsync";
			Func<object, object, CancellationToken, Task<TResponse>> callElasticSearchAsync;
			var pipelineType = pipeline.GetType();
			var genericArgument = typeof(TResponse);

			try
			{
				callElasticSearchAsync =
					MethodBuilder<Func<object, object, CancellationToken, Task<TResponse>>>
						.Start(moduleVersionPtr, mdToken, opCode, methodName)
						.WithConcreteType(pipelineType)
						.WithMethodGenerics(genericArgument)
						.WithParameters(requestData, cancellationToken)
						.WithNamespaceAndNameFilters(ClrTypeNames.GenericParameterTask, "Elasticsearch.Net.RequestData", ClrTypeNames.CancellationToken)
						.Build();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				throw;
			}

			return callElasticSearchAsync(pipeline, requestData, cancellationToken);
		}
	}
}
