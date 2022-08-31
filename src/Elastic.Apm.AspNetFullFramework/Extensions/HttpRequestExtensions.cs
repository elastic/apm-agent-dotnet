// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Web;

namespace Elastic.Apm.AspNetFullFramework.Extensions
{
	internal static class HttpRequestExtensions
	{
		//
		// Implementation of "HasFormContentType" and its helper methods
		// "HasApplicationFormContentType" and "HasMultipartFormContentType" took inspiration from
		// https://source.dot.net/#Microsoft.AspNetCore.Http/Features/FormFeature.cs
		//
		internal static bool HasFormContentType(this HttpRequest httpRequest)
		{
			var contentType = httpRequest?.ContentType;
			return HasApplicationFormContentType(contentType) || HasMultipartFormContentType(contentType);
		}

		private static bool HasApplicationFormContentType(string contentType) =>
			// Content-Type: application/x-www-form-urlencoded; charset=utf-8
			contentType != null && contentType.Equals("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase);

		private static bool HasMultipartFormContentType(string contentType) =>
			// Content-Type: multipart/form-data; boundary=----WebKitFormBoundarymx2fSWqWSd0OxQqq
			contentType != null && contentType.Equals("multipart/form-data", StringComparison.OrdinalIgnoreCase);
	}
}
