// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Apm.Api
{
	public class Label
	{
		public Label(string value) => Value = value;

		public Label(bool value) => Value = value;

		public Label(double value) => Value = value;

		public Label(int value) => Value = value;

		public Label(long value) => Value = value;

		public Label(decimal value) => Value = value;

		public object Value { get; }

		public static implicit operator Label(string value) => new Label(value);

		public static implicit operator Label(bool value) => new Label(value);

		public static implicit operator Label(double value) => new Label(value);

		public static implicit operator Label(int value) => new Label(value);

		public static implicit operator Label(long value) => new Label(value);

		public static implicit operator Label(decimal value) => new Label(value);
	}
}
