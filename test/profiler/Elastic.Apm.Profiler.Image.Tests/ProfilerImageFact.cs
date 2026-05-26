// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO;
using System.Linq;
using Elastic.Apm.Tests.Utilities;
using Elastic.Apm.Tests.Utilities.XUnit;
using Xunit;

namespace Elastic.Apm.Profiler.Image.Tests;

/// <summary>
/// Skips the test if Docker is unavailable or if the profiler zip has not been built yet.
/// Run 'build.bat profiler-zip' (Windows) or './build.sh profiler-zip' (Linux) to produce it.
/// </summary>
public sealed class ProfilerImageFact : FactAttribute
{
	public ProfilerImageFact()
	{
		Skip = DockerTheory.ShouldSkip();
		if (Skip is not null)
			return;

		var buildOutput = Path.Combine(SolutionPaths.Root, "build", "output");
		var hasZip = Directory.Exists(buildOutput)
			&& Directory.GetFiles(buildOutput, "elastic_apm_profiler_*-linux-x64.zip").Length != 0;

		if (!hasZip)
			Skip = "Profiler zip not found in build/output. Run './build.sh profiler-zip' (Linux) first";
	}
}
