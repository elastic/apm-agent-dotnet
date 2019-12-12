using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Api
{
	public class Destination
	{
		private Optional<string> _address;
		private Optional<int?> _port;

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Address
		{
			get => _address.Value;
			set => _address = new Optional<string>(value);
		}

		public int? Port
		{
			get => _port.Value;
			set => _port = new Optional<int?>(value);
		}

		internal void CopyMissingPropertiesFrom(Destination src)
		{
			if (!_address.HasValue) _address = src._address;
			if (!_port.HasValue) _port = src._port;
		}

		private readonly struct Optional<T>
		{
			internal readonly bool HasValue;
			internal readonly T Value;

			internal Optional(T value)
			{
				Value = value;
				HasValue = true;
			}
		}
	}
}
