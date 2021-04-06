// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Apm.Reflection
{
	internal class CascadePropertyFetcher : PropertyFetcher
	{
		private readonly PropertyFetcher _innerFetcher;

		public CascadePropertyFetcher(PropertyFetcher innerFetcher, string propertyName) : base(propertyName) => _innerFetcher = innerFetcher;

		public override object Fetch(object obj)
		{
			var fetchedObject = _innerFetcher.Fetch(obj);

			return base.Fetch(fetchedObject);
		}
	}
}
