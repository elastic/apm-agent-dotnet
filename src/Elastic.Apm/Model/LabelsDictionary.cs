// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using Elastic.Apm.Api;
using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Model
{
	/// <summary>
	/// The instance of this type holds the <see cref="Dictionary{TKey,TValue}" /> that contains all the labels on a given
	/// event.
	/// The public interface of this mimics a Dictionary with <see cref="string" /> as both the key and the value.
	/// The reason for this is backwards compatibility - this type makes sure that we don't break user that rely on the old
	/// interface.
	/// </summary>
	[JsonConverter(typeof(LabelsJsonConverter))]
	internal class LabelsDictionary : Dictionary<string, string>
	{
		internal Dictionary<string, Label> InnerDictionary { get; } = new Dictionary<string, Label>();

		/// <summary>
		/// Merges the string dictionary with the InnerDictionary
		/// </summary>
		internal Dictionary<string, Label> MergedDictionary
		{
			get
			{
				// merge
				foreach (var key in Keys) InnerDictionary[key] = base[key];
				return InnerDictionary;
			}
		}
	}
}
