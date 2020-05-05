// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using System.Threading;

namespace Elastic.Apm.Helpers
{
	internal struct DbgInstanceNameGenerator
	{
		private long _lastId;

		// ReSharper disable once UnusedMember.Global
		internal DbgInstanceNameGenerator(long startId = 1) => _lastId = startId - 1;

		internal string Generate(string prefix = null)
		{
			var result = new StringBuilder();

			if (prefix != null) result.Append(prefix);

			var currentId = Interlocked.Increment(ref _lastId);
			result.Append(currentId);

			return result.ToString();
		}
	}
}
