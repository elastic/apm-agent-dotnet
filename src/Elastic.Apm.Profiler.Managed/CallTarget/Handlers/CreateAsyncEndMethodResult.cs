// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="CreateAsyncEndMethodResult.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.Reflection.Emit;

namespace Elastic.Apm.Profiler.Managed.CallTarget.Handlers
{
	internal readonly struct CreateAsyncEndMethodResult
	{
		public readonly DynamicMethod Method;
		public readonly bool PreserveContext;

		public CreateAsyncEndMethodResult(DynamicMethod method, bool preserveContext)
		{
			Method = method;
			PreserveContext = preserveContext;
		}
	}
}
