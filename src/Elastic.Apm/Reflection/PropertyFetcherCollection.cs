// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections;
using System.Collections.Generic;

namespace Elastic.Apm.Reflection
{
	/// <summary>
	/// A collection of property fetchers, used to retrieve property values
	/// from objects at runtime.
	/// </summary>
	internal class PropertyFetcherCollection : IEnumerable<PropertyFetcher>
	{
		private readonly Dictionary<string, PropertyFetcher> _propertyFetchers;

		public PropertyFetcherCollection() =>
			_propertyFetchers = new Dictionary<string, PropertyFetcher>();

		public PropertyFetcherCollection(int capacity) =>
			_propertyFetchers = new Dictionary<string, PropertyFetcher>(capacity);

		public void Add(PropertyFetcher propertyFetcher) =>
			_propertyFetchers.Add(propertyFetcher.PropertyName, propertyFetcher);

		public void Add(string propertyName) =>
			_propertyFetchers.Add(propertyName, new PropertyFetcher(propertyName));

		public object Fetch(object obj, string propertyName) =>
			_propertyFetchers.TryGetValue(propertyName, out var fetcher) ? fetcher.Fetch(obj) : null;

		public IEnumerator<PropertyFetcher> GetEnumerator() => _propertyFetchers.Values.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
