// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections;
using System.Collections.Generic;
using Elastic.Apm.Config;

namespace Elastic.Apm.Tests.Utilities.Data
{
	public class TransactionMaxSpansTestData : IEnumerable<object[]>
	{
		public IEnumerator<object[]> GetEnumerator()
		{
			yield return new object[] { null, ConfigConsts.DefaultValues.TransactionMaxSpans };
			yield return new object[] { "1avc", ConfigConsts.DefaultValues.TransactionMaxSpans };

			yield return new object[] { "-2", ConfigConsts.DefaultValues.TransactionMaxSpans };
			yield return new object[] { "-1", -1 };
			yield return new object[] { "0", 0 };
			yield return new object[] { "10", 10 };
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
