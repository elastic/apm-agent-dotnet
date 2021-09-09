// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="ClrNames.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

namespace Elastic.Apm.Profiler.Managed.Core
{
	public static class ClrTypeNames
	{
		public const string Ignore = "_";

		public const string Void = "System.Void";
		public const string Object = "System.Object";
		public const string Bool = "System.Boolean";
		public const string String = "System.String";

		public const string SByte = "System.SByte";
		public const string Byte = "System.Byte";

		public const string Int16 = "System.Int16";
		public const string Int32 = "System.Int32";
		public const string Int64 = "System.Int64";

		public const string UInt16 = "System.UInt16";
		public const string UInt32 = "System.UInt32";
		public const string UInt64 = "System.UInt64";

		public const string Stream = "System.IO.Stream";

		public const string Task = "System.Threading.Tasks.Task";
		public const string CancellationToken = "System.Threading.CancellationToken";

		// ReSharper disable once InconsistentNaming
		public const string IAsyncResult = "System.IAsyncResult";
		public const string AsyncCallback = "System.AsyncCallback";

		public const string HttpRequestMessage = "System.Net.Http.HttpRequestMessage";
		public const string HttpResponseMessage = "System.Net.Http.HttpResponseMessage";
		public const string HttpResponseMessageTask = "System.Threading.Tasks.Task`1<System.Net.Http.HttpResponseMessage>";

		public const string GenericTask = "System.Threading.Tasks.Task`1";
		public const string GenericParameterTask = "System.Threading.Tasks.Task`1<T>";
	}
}
