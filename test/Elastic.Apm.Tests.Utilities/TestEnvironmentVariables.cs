// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections;
using Elastic.Apm.Helpers;

namespace Elastic.Apm.Tests.Utilities
{
	public class TestEnvironmentVariables: IEnvironmentVariables
	{
		private readonly Hashtable _hashTable;

		public TestEnvironmentVariables() => _hashTable = new Hashtable();

		public string this[string key]
		{
			get => _hashTable[key] as string;
			set => _hashTable[key] = value;
		}

		public IDictionary GetEnvironmentVariables() => _hashTable;
	}
}
