// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Web.Mvc;

namespace AspNetFullFrameworkSampleApp.Extensions
{
	public static class TempDataExtensions
	{
		public static T Get<T>(this TempDataDictionary tempData, string key)
		{
			tempData.TryGetValue(key, out var item);
			return item == null ? default : (T)item;
		}
		public static void Put<T>(this TempDataDictionary tempData, string key, T value) => tempData[key] = value;
	}
}
