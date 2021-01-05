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
	public class TypeValidationResult
	{
		private readonly List<TypeValidationError> _errors = new List<TypeValidationError>();
		private readonly List<TypeValidationIgnore> _ignores = new List<TypeValidationIgnore>();
		public Type Type { get; }
		public string SpecificationId { get; }
		public Validation Validation { get; }
		public IEnumerable<TypeValidationError> Errors => _errors;

		public IEnumerable<TypeValidationIgnore> Ignores => _ignores;

		public bool Success => !Errors.Any();

		internal void AddError(TypeValidationError error) => _errors.Add(error);

		internal void AddIgnore(TypeValidationIgnore error) => _ignores.Add(error);

		public TypeValidationResult(Type type, string specificationId, Validation validation)
		{
			Type = type;
			SpecificationId = specificationId;
			Validation = validation;
		}

		public override string ToString()
		{
			var builder = new StringBuilder(Type.FullName)
				.Append(": ")
				.AppendLine(Success ? "success" : "failure");

			foreach (var error in Errors)
				builder.AppendLine(error.ToString());

			foreach (var ignore in Ignores)
				builder.AppendLine(ignore.ToString());

			return builder.ToString();
		}
	}
}
