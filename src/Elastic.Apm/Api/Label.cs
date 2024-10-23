// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;

namespace Elastic.Apm.Api
{
	/// <summary>
	/// Represents the value of a label (see <see cref="IExecutionSegment.SetLabel(string,string)"/> with all its overloads).
	/// It contains implicit operators to convert all supported types to a new instance of <see cref="Label"/>
	/// </summary>
	public class Label
	{
		public Label(string value) => Value = value;

		public Label(bool value) => Value = value;

		public Label(double value) => Value = value;

		public Label(int value) => Value = value;

		public Label(long value) => Value = value;

		public Label(decimal value) => Value = value;

		public object Value { get; }

		public static implicit operator Label(string value) => new(value);

		public static implicit operator Label(bool value) => new(value);

		public static implicit operator Label(double value) => new(value);

		public static implicit operator Label(int value) => new(value);

		public static implicit operator Label(long value) => new(value);

		public static implicit operator Label(decimal value) => new(value);

		public override string ToString() => Value switch
		{
			string s => s,
			bool s => s.ToString(CultureInfo.InvariantCulture),
			double s => s.ToString(CultureInfo.InvariantCulture),
			int s => s.ToString(CultureInfo.InvariantCulture),
			long s => s.ToString(CultureInfo.InvariantCulture),
			decimal s => s.ToString(CultureInfo.InvariantCulture),
			_ => Value.ToString()
		};
	}
}
