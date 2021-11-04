// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="PreserveContextAttribute.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;

namespace Elastic.Apm.Profiler.Managed.CallTarget
{
	/// <summary>
	/// Apply on a calltarget async callback to indicate that the method
	/// should execute under the current synchronization context/task scheduler.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
	internal class PreserveContextAttribute : Attribute
	{
	}
}
