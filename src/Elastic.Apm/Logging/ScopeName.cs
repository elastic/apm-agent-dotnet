// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

#nullable enable
using System;

namespace Elastic.Apm.Logging;

internal readonly struct ScopeName(string value)
{
	public bool IsEmpty => Value.Equals(string.Empty, StringComparison.Ordinal);

	public static explicit operator ScopeName(string value) => new(value);

	public static implicit operator string(ScopeName value) => value.Value;

	public static ScopeName From(string value) => string.IsNullOrEmpty(value) ? Empty : new(value);

	public static ScopeName Empty { get; } = new(string.Empty);

	public string Value { get; } = value;

	/// <inheritdoc/>
	public override string ToString() => Value;
}
#nullable restore
