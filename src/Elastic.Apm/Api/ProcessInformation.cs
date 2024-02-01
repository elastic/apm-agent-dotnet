// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using Elastic.Apm.Helpers;

namespace Elastic.Apm.Api;

internal class ProcessInformation
{
	public int Pid { get; set; }

	public string Title { get; set; }

	public static ProcessInformation Create()
	{
		var p = Process.GetCurrentProcess();
		return new ProcessInformation { Pid = p.Id, Title = p.ProcessName };
	}

	public override string ToString() => new ToStringBuilder(nameof(Service))
	{
		{ nameof(Pid), Pid },
		{ nameof(Title), Title }
	}.ToString();
}
