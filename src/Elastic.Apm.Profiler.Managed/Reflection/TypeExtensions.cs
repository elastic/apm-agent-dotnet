// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="TypeExtensions.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

namespace Elastic.Apm.Profiler.Managed.Reflection
{
	public static class TypeExtensions
	{
		public static System.Type GetInstrumentedType(
			this object runtimeObject,
			string instrumentedNamespace,
			string instrumentedTypeName)
		{
			if (runtimeObject == null)
			{
				return null;
			}

			var currentType = runtimeObject.GetType();

			while (currentType != null)
			{
				if (currentType.Name == instrumentedTypeName && currentType.Namespace == instrumentedNamespace)
				{
					return currentType;
				}

				currentType = currentType.BaseType;
			}

			return null;
		}

	}
}
