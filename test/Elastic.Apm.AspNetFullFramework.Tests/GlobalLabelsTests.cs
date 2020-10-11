// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elastic.Apm.Config;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	[Collection(Consts.AspNetFullFrameworkTestsCollection)]
	public class GlobalLabelsTests : TestsBase
	{
		private static readonly Dictionary<string, string> CustomGlobalLabels = new Dictionary<string, string>
		{
			{ "k", "v" },
			{ "key_B", "value_B" },
			{ "", "" },
			{ "key_with_empty_string_value", "" }
		};

		public GlobalLabelsTests(ITestOutputHelper xUnitOutputHelper)
			: base(xUnitOutputHelper,
				envVarsToSetForSampleAppPool: new Dictionary<string, string>
				{
					{ ConfigConsts.EnvVarNames.GlobalLabels, GlobalLabelsToRawOptionValue(CustomGlobalLabels) }
				}) =>
			AgentConfig.GlobalLabels = CustomGlobalLabels;

		/// <returns>key=value[,key=value[,...]]</returns>
		private static string GlobalLabelsToRawOptionValue(IReadOnlyDictionary<string, string> stringToStringMap) =>
			string.Join(",", stringToStringMap.Select(kv => $"{kv.Key}={kv.Value}"));

		[AspNetFullFrameworkFact]
		public async Task Test()
		{
			var pageData = SampleAppUrlPaths.HomePage;
			await SendGetRequestToSampleAppAndVerifyResponse(pageData.Uri, pageData.StatusCode);
			await WaitAndVerifyReceivedDataSharedConstraints(pageData);
		}
	}
}
