// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Specification;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.Tests
{
	public class SpecTests
	{
		private readonly ITestOutputHelper _output;
		private List<Type> _specTypes;
		private Validator _validator;

		public SpecTests(ITestOutputHelper output)
		{
			var downloadDir = Directory.GetCurrentDirectory();
			var agentTypes = typeof(Agent).Assembly.GetTypes();

			// classes and interfaces can define the spec...
			var specInterfaces =
				(from type in agentTypes
					where type.GetCustomAttribute<SpecificationAttribute>() != null
					select type).ToList();

			// but the concrete implementations of spec types are what get serialized
			// so they define the constraints such as max length, etc.
			_specTypes =
				(from type in agentTypes
					where type.IsClass && specInterfaces.Any(i => i.IsAssignableFrom(type))
					select type).ToList();

			_validator = new Validator("master", downloadDir);
			_output = output;
		}

		/// <summary>
		/// Validates the specification against the agent type.
		/// </summary>
		/// <returns></returns>
		[Fact]
		public async Task Validate_Spec_To_Type_Should_Be_Valid()
		{
			var results = new List<ValidationResult>(_specTypes.Count);

			foreach (var specType in _specTypes)
			{
				try
				{
					results.Add(await _validator.ValidateAsync(specType, Validation.SpecToType));
				}
				catch (JsonSchemaException e)
				{
					// bug in specs: https://github.com/elastic/apm-server/issues/4326
					// log but continue for now
					_output.WriteLine(e.Message);
				}
			}

			results.Should().OnlyContain(r => r.Success);
		}

		[Fact]
		public async Task Validate_Type_To_Spec_Should_Be_Valid()
		{
			var results = new List<ValidationResult>(_specTypes.Count);
			foreach (var specType in _specTypes)
			{
				try
				{
					results.Add(await _validator.ValidateAsync(specType, Validation.TypeToSpec));
				}
				catch (JsonSchemaException e)
				{
					// bug in specs: https://github.com/elastic/apm-server/issues/4326
					// log but continue for now
					_output.WriteLine(e.Message);
				}
			}

			results.Should().OnlyContain(r => r.Success);
		}
	}
}
