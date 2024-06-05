// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Elastic.Apm.Libraries.Newtonsoft.Json.Linq;
using Xunit.Sdk;

namespace Elastic.Apm.Tests.Utilities;

public struct CgroupFiles
{
		public string ProcSelfCgroup;
		public string[] MountInfo;
}

public struct CGroupTestData
{
	public CgroupFiles Files;
	public string ContainerId;
	public string PodId;
}


public class CGroupTestCasesAttribute : DataAttribute
{
	private readonly string _fileName = "./TestResources/json-specs//container_metadata_discovery.json";

	public override IEnumerable<object[]> GetData(MethodInfo testMethod)
	{
		if (!File.Exists(_fileName))
			throw new ArgumentException($"JSON input file {_fileName} does not exist");

		var jToken = JToken.Parse(File.ReadAllText(_fileName), new JsonLoadSettings
		{
			CommentHandling = CommentHandling.Ignore
		});

		foreach (var kvp in (JObject)jToken)
		{
			var name = kvp.Key;
			var data = ParseTestData(kvp.Value as JObject);
			yield return [name, data];
		}
	}

	private static CGroupTestData ParseTestData(JObject jToken)
	{
		var testData = new CGroupTestData { Files = new CgroupFiles() };

		foreach (var kvp in jToken)
		{
			switch (kvp.Key)
			{
				case "containerId":
					testData.ContainerId = kvp.Value?.Value<string>();
					break;
				case "podId":
					testData.PodId = kvp.Value?.Value<string>();
					break;
				case "files":
					var o = (JObject)kvp.Value;
					var cgroupA = o.Property("/proc/self/cgroup")?.Value as JArray;
					testData.Files.ProcSelfCgroup = cgroupA?.Values<string>().FirstOrDefault();

					var mountInfoA = o.Property("/proc/self/mountinfo")?.Value as JArray;
					testData.Files.MountInfo = mountInfoA?.Values<string>().ToArray();
					break;
			}
		}
		return testData;
	}
}
