// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;

namespace Elastic.Apm.Model;

internal class ProcessInformation
{
	public int Pid { get; set; }

	public string Title { get; set; }

	public static ProcessInformation Create()
	{
		var p = Process.GetCurrentProcess();
		return new ProcessInformation { Pid = p.Id, Title = p.ProcessName };
	}

}
