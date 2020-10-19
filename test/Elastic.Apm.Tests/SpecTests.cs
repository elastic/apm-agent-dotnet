// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Specification;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests
{
	public class SpecTests
	{
		[Fact]
		public async Task Agent_Should_Conform_To_ApmServer_Specs()
		{
			var downloadDir = Directory.GetCurrentDirectory();

			// schema in master branch has a bug: https://github.com/elastic/apm-server/issues/4326
			// use latest version that works for now.
			var specification = new SpecificationValidator("v7.2.0", downloadDir);

			var agentTypes = typeof(Agent).Assembly.GetTypes();

			// classes and interfaces can define the spec...
			var specInterfaces =
				(from type in agentTypes
				where type.GetCustomAttribute<SpecificationAttribute>() != null
				select type).ToList();

			// but the concrete implementations of spec types are what get serialized
			// so they define the constraints such as max length, etc.
			var specTypes =
				(from type in agentTypes
				where type.IsClass && specInterfaces.Any(i => i.IsAssignableFrom(type))
				select type).ToList();

			var results = new List<ValidationResult>(specTypes.Count);

			foreach (var specType in specTypes)
				results.Add(await specification.ValidateSpecAgainstTypeAsync(specType));

			results.Should().OnlyContain(r => r.Success);
		}
	}
}
