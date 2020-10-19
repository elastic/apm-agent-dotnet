// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Elastic.Apm.Specification
{
	public class ValidationResult
	{
		private readonly List<ValidationError> _errors = new List<ValidationError>();
		private readonly List<ValidationIgnore> _ignores = new List<ValidationIgnore>();

		public Type Type { get; }
		public string SpecificationId { get; }
		public Validation Validation { get; }
		public IEnumerable<ValidationError> Errors => _errors;

		public IEnumerable<ValidationIgnore> Ignores => _ignores;
		public bool Success => !Errors.Any();

		internal void AddError(ValidationError error) => _errors.Add(error);

		internal void AddIgnore(ValidationIgnore error) => _ignores.Add(error);

		public ValidationResult(Type type, string specificationId, Validation validation)
		{
			Type = type;
			SpecificationId = specificationId;
			Validation = validation;
		}

		public override string ToString()
		{
			if (Success) return "success";

			var builder = new StringBuilder("failure").AppendLine();

			foreach (var error in Errors) builder.AppendLine(error.ToString());

			return builder.ToString();
		}
	}
}
