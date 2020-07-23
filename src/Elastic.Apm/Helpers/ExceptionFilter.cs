// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Apm.Api;

namespace Elastic.Apm.Helpers
{
	/// <summary>
	/// Offers methods that can be used as exception filters.
	/// Within the filter we return false and capture the exception.
	/// Advantage: it avoid stack unwinding.
	/// </summary>
	internal static class ExceptionFilter
	{
		internal static bool Capture(Exception e, ITransaction transaction)
		{
			transaction.CaptureException(e);
			return false;
		}

		internal static bool Capture(Exception e, ISpan span)
		{
			span.CaptureException(e);
			return false;
		}
	}
}
