// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.AspNetCore.Http.Features;

namespace Elastic.Apm.AspNetCore
{
	internal static class Consts
	{
		public const int RequestBodyMaxLength = 2048;

		internal static class OpenIdClaimTypes
		{
			internal const string Email = "email";
			internal const string UserId = "sub";
		}

		internal static FormOptions FormContentOptions => new FormOptions { BufferBody = true };
	}
}
