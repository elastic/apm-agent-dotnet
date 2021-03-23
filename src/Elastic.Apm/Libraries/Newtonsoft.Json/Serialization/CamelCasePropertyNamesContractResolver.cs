#region License

// Copyright (c) 2007 James Newton-King
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.

#endregion

using System;
using System.Collections.Generic;
using Elastic.Apm.Libraries.Newtonsoft.Json.Utilities;

namespace Elastic.Apm.Libraries.Newtonsoft.Json.Serialization
{
	/// <summary>
	/// Resolves member mappings for a type, camel casing property names.
	/// </summary>
	public class CamelCasePropertyNamesContractResolver : DefaultContractResolver
	{
		private static Dictionary<StructMultiKey<Type, Type>, JsonContract>? _contractCache;
		private static readonly DefaultJsonNameTable NameTable = new();
		private static readonly object TypeContractCacheLock = new();

		/// <summary>
		/// Initializes a new instance of the <see cref="CamelCasePropertyNamesContractResolver" /> class.
		/// </summary>
		public CamelCasePropertyNamesContractResolver() =>
			NamingStrategy = new CamelCaseNamingStrategy { ProcessDictionaryKeys = true, OverrideSpecifiedNames = true };

		/// <summary>
		/// Resolves the contract for a given type.
		/// </summary>
		/// <param name="type">The type to resolve a contract for.</param>
		/// <returns>The contract for a given type.</returns>
		public override JsonContract ResolveContract(Type type)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			// for backwards compadibility the CamelCasePropertyNamesContractResolver shares contracts between instances
			var key = new StructMultiKey<Type, Type>(GetType(), type);
			var cache = _contractCache;
			if (cache == null || !cache.TryGetValue(key, out var contract))
			{
				contract = CreateContract(type);

				// avoid the possibility of modifying the cache dictionary while another thread is accessing it
				lock (TypeContractCacheLock)
				{
					cache = _contractCache;
					var updatedCache = cache != null
						? new Dictionary<StructMultiKey<Type, Type>, JsonContract>(cache)
						: new Dictionary<StructMultiKey<Type, Type>, JsonContract>();
					updatedCache[key] = contract;

					_contractCache = updatedCache;
				}
			}

			return contract;
		}

		internal override DefaultJsonNameTable GetNameTable() => NameTable;
	}
}
