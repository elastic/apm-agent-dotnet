// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;

namespace Elastic.Apm.Azure.ServiceBus.Tests
{
	/// <summary>
	/// An exception from interacting with terraform resources.
	/// </summary>
	public class TerraformResourceException : Exception
	{
		public TerraformResourceException(string message, int exitCode, List<string> output)
			: base(string.Join(Environment.NewLine, new [] { message, $"exit code: {exitCode}", "output:" }.Concat(output)))
		{
		}

		public TerraformResourceException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}
