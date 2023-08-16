// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Apm.Azure.Functions;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Elastic.AzureFunctionApp.InProcess.Startup))]

namespace Elastic.AzureFunctionApp.InProcess;

internal class Startup : FunctionsStartup
{
	public override void Configure(IFunctionsHostBuilder builder) => builder.AddElasticApm();
}
