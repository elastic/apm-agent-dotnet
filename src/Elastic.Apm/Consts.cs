// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Apm
{
	internal static class Consts
	{
		internal const int PropertyMaxLength = 1024;

		internal const string AgentName = "dotnet";

		internal const string Redacted = "[REDACTED]";
		internal const string NotProvided = "[NOT_PROVIDED]";


		internal const string ActivationK8SAttach = "k8s-attach";
		internal const string ActivationMethodNuGet = "nuget";
		internal const string ActivationMethodProfiler = "profiler";
		internal const string ActivationMethodStartupHook = "startup-hook";
	}
}
