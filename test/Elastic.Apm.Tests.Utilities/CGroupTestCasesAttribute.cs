// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
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

		var jToken = JsonNode.Parse(File.ReadAllText(_fileName), new JsonNodeOptions(), new JsonDocumentOptions
		{
			CommentHandling = JsonCommentHandling.Skip
		});
		if (jToken is JsonObject jObject)
		{
			foreach (var kvp in jObject)
			{
				var name = kvp.Key;
				var data = ParseTestData(kvp.Value as JsonObject);
				yield return [name, data];
			}
		}

	}

	private static CGroupTestData ParseTestData(JsonObject jToken)
	{
		var testData = new CGroupTestData { Files = new CgroupFiles() };

		foreach (var kvp in jToken)
		{
			switch (kvp.Key)
			{
				case "containerId":
					testData.ContainerId = kvp.Value?.GetValue<string>();
					break;
				case "podId":
					testData.PodId = kvp.Value?.GetValue<string>();
					break;
				case "files":
					var o = (JsonObject)kvp.Value;
					var cgroupA = o.TryGetPropertyValue("/proc/self/cgroup", out var cgroup) ? cgroup as JsonArray : null;
					testData.Files.ProcSelfCgroup = cgroupA?.GetValues<string>().FirstOrDefault();

					var mountInfoA  = o.TryGetPropertyValue("/proc/self/mountinfo", out var mountinfo) ? mountinfo as JsonArray : null;
					testData.Files.MountInfo = mountInfoA?.GetValues<string>().ToArray();
					break;
			}
		}
		return testData;
	}
}
