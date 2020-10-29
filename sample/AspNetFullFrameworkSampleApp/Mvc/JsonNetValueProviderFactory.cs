// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

// https://gist.github.com/rorymurphy/db0b02e8267960a0881a
// This is a slightly modified ValueProviderFactory based almost entirely on Microsoft's JsonValueProviderFactory.
// That file is licensed under the Apache 2.0 license, but I am leaving the copyright statement below nonetheless.
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Web.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AspNetFullFrameworkSampleApp.Mvc
{
	/// <summary>
	/// A value provider factory for application/json requests that uses
	/// Json.NET to deserialize the request stream and provide the
	/// value to subsequent steps.
	/// </summary>
	public sealed class JsonNetValueProviderFactory : ValueProviderFactory
    {
		private static readonly JsonSerializer Serializer = new JsonSerializer
		{
			Converters = { new ExpandoObjectConverter() }
		};

        private static void AddToBackingStore(EntryLimitedDictionary backingStore, string prefix, object value)
        {
			switch (value)
			{
				case IDictionary<string, object> d:
				{
					foreach (var entry in d)
						AddToBackingStore(backingStore, MakePropertyKey(prefix, entry.Key), entry.Value);
					return;
				}
				case IList l:
				{
					for (var i = 0; i < l.Count; i++)
						AddToBackingStore(backingStore, MakeArrayKey(prefix, i), l[i]);
					return;
				}
				default:
					backingStore.Add(prefix, value);
					break;
			}
		}

        private static object GetDeserializedObject(ControllerContext controllerContext)
        {
			var request = controllerContext.HttpContext.Request;
			if (!request.ContentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
				return null;

			string bodyText;
			using (var reader = new StreamReader(request.InputStream))
				bodyText = reader.ReadToEnd();

			if (string.IsNullOrEmpty(bodyText))
				return null;

			object jsonData;
            using (var reader = new StringReader(bodyText))
			using (var jsonTextReader = new JsonTextReader(reader))
			{
				jsonTextReader.Read();
				if (jsonTextReader.TokenType == JsonToken.StartArray)
					jsonData = Serializer.Deserialize<List<ExpandoObject>>(jsonTextReader);
				else
					jsonData = Serializer.Deserialize<ExpandoObject>(jsonTextReader);
			}

			return jsonData;
        }

        public override IValueProvider GetValueProvider(ControllerContext controllerContext)
        {
            if (controllerContext == null)
				throw new ArgumentNullException(nameof(controllerContext));

			var jsonData = GetDeserializedObject(controllerContext);
            if (jsonData == null) return null;

			var backingStore = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var backingStoreWrapper = new EntryLimitedDictionary(backingStore);
            AddToBackingStore(backingStoreWrapper, string.Empty, jsonData);
            return new DictionaryValueProvider<object>(backingStore, CultureInfo.CurrentCulture);
        }

        private static string MakeArrayKey(string prefix, int index) =>
			prefix + "[" + index.ToString(CultureInfo.InvariantCulture) + "]";

		private static string MakePropertyKey(string prefix, string propertyName) =>
			string.IsNullOrEmpty(prefix) ? propertyName : prefix + "." + propertyName;

		private class EntryLimitedDictionary
        {
            private static readonly int MaximumDepth = GetMaximumDepth();
            private readonly IDictionary<string, object> _innerDictionary;
            private int _itemCount;

            public EntryLimitedDictionary(IDictionary<string, object> innerDictionary) =>
				_innerDictionary = innerDictionary;

			public void Add(string key, object value)
            {
                if (++_itemCount > MaximumDepth)
					throw new InvalidOperationException("Request too large");

				_innerDictionary.Add(key, value);
            }

            private static int GetMaximumDepth()
            {
                var appSettings = System.Configuration.ConfigurationManager.AppSettings;
				var valueArray = appSettings?.GetValues("aspnet:MaxJsonDeserializerMembers");
				if (valueArray == null || valueArray.Length <= 0) return 1000;
				return int.TryParse(valueArray[0], out var result) ? result : 1000;
			}
        }
    }
}
