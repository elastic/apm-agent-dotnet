// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="HttpModule_Integration.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>


using System.Threading;
#if NETFRAMEWORK
using System.Web;
using Elastic.Apm.AspNetFullFramework;
#endif
using Elastic.Apm.Profiler.Managed.CallTarget;
using Elastic.Apm.Profiler.Managed.Core;

namespace Elastic.Apm.Profiler.Managed.Integrations.AspNet
{
    /// <summary>
    /// System.Web.Compilation.BuildManager.InvokePreStartInitMethodsCore calltarget instrumentation
    /// </summary>
    [Instrument(
		Nuget = "part of .NET Framework",
        Assembly = "System.Web",
        Type = "System.Web.Compilation.BuildManager",
        Method = "InvokePreStartInitMethodsCore",
        ReturnType = ClrTypeNames.Void,
        ParameterTypes = new[] { "System.Collections.Generic.ICollection`1[System.Reflection.MethodInfo]", "System.Func`1[System.IDisposable]" },
        MinimumVersion = "4.0.0",
        MaximumVersion = "4.*.*",
        Group = "AspNet")]
    public class ElasticApmModuleIntegration
    {
		/// <summary>
        /// Indicates whether we're initializing the HttpModule for the first time
        /// </summary>
        private static int FirstInitialization = 1;

        /// <summary>
        /// OnMethodBegin callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <typeparam name="TCollection">Type of the collection</typeparam>
        /// <typeparam name="TFunc">Type of the </typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method. This method is static so this parameter will always be null</param>
        /// <param name="methods">The methods to be invoked</param>
        /// <param name="setHostingEnvironmentCultures">The function to set the environment culture</param>
        /// <returns>Calltarget state value</returns>
        public static CallTargetState OnMethodBegin<TTarget, TCollection, TFunc>(TTarget instance, TCollection methods, TFunc setHostingEnvironmentCultures)
        {
            if (Interlocked.Exchange(ref FirstInitialization, 0) != 1)
            {
                // The HttpModule was already registered
                return CallTargetState.GetDefault();
            }

            try
            {
// directive applied just to here and to .NET framework specific using directives, to allow
// the integrations file generator to pick this integration up, irrespective of version.
#if NETFRAMEWORK
                HttpApplication.RegisterModule(typeof(ElasticApmModule));
#endif
            }
            catch
            {
                // Unable to dynamically register module
                // Not sure if we can technically log yet or not, so do nothing
            }

            return CallTargetState.GetDefault();
        }
    }
}
