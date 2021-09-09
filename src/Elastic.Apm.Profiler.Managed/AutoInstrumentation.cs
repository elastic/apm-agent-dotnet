// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="Instrumentation.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.Threading;

namespace Elastic.Apm.Profiler.Managed
{
	public static class AutoInstrumentation
	{
		private static int FirstInitialization = 1;

		public static void Initialize()
		{
			// check if already called
			if (Interlocked.Exchange(ref FirstInitialization, 0) != 1)
				return;

			try
			{
				// ReSharper disable once ReplaceWithSingleAssignment.False
				var skipInstantiation = false;

#if NETFRAMEWORK
				// if this is a .NET Framework application running in IIS, don't instantiate the agent here, but let the
				// ElasticApmModule do so in its Init(). This assumes that the application:
				// either
				// 1. references Elastic.Apm.AspNetFullFramework and has configured ElasticApmModule in web.config
				// or
				// 2. is relying on profiler auto instrumentation to register ElasticApmModule
				//
				// We allow instantiation to happen in ElasticApmModule because in the case point 1, a user may have
				// configured a logging adaptor for the agent running in ASP.NET, which would be ignored if the agent
				// was instantiated here.
				// ReSharper disable once ConvertIfToOrExpression
				if (System.Web.Hosting.HostingEnvironment.IsHosted)
					skipInstantiation = true;
#endif
				// ensure global instance is created if it's not already
				if (!skipInstantiation)
					_ = Agent.Instance;
			}
			catch
			{
				// ignore
			}
		}
	}
}
