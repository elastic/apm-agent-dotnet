// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using ElasticApmStartupHook;

namespace Elastic.Apm.StartupHook.Loader
{
	/// <summary>
	/// An <see cref="AssemblyLoadContext" /> implementation which loads agent dlls and dependencies of agent dlls.
	/// </summary>
	internal class AgentLoadContext : AssemblyLoadContext
	{
		public static HashSet<string> AgentDependencyLibsToLoad = new HashSet<string>
		{
			"System.Diagnostics.PerformanceCounter", "Microsoft.Diagnostics.Tracing.TraceEvent", "Newtonsoft.Json", "Elasticsearch.Net"
		};

		public static HashSet<string> AgentLibsToLoad = new HashSet<string>
		{
			"Elastic.Apm",
			"Elastic.Apm.Extensions.Hosting",
			"Elastic.Apm.AspNetCore",
			"Elastic.Apm.EntityFrameworkCore",
			"Elastic.Apm.SqlClient",
			"Elastic.Apm.GrpcClient",
			"Elastic.Apm.Elasticsearch"
		};

		private readonly string _assemblyDir;

		private readonly StartupHookLogger _logger;

		public AgentLoadContext(string assemblyDir, StartupHookLogger logger) => (_assemblyDir, _logger) = (assemblyDir, logger);

		protected override Assembly Load(AssemblyName assemblyName)
		{
			try
			{
				if (AgentLibsToLoad.Contains(assemblyName.Name) || AgentDependencyLibsToLoad.Contains(assemblyName.Name))
				{
					var path = Path.Combine(_assemblyDir, assemblyName.Name + ".dll");
					return LoadFromAssemblyPath(path);
				}
				// If it's not an agent assembly or an agent dependency, let's just reuse it from the default load context
				return Default.LoadFromAssemblyName(assemblyName);
			}
			catch (Exception e)
			{
				_logger.WriteLine($"{nameof(AgentLoadContext)} Failed loading {assemblyName}, exception: {e}");
			}

			return null;
		}
	}
}
