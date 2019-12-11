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
			get => _address?.Value;
			set
			{
				if (_address == null) _address = new Optional<string>();
				_address.Value = value;
			}
		}

		public int? Port
		{
			get => _port?.Value;
			set
			{
				if (_port == null) _port = new Optional<int?>();
				_port.Value = value;
			}
		}

		internal void CopyMissingPropertiesFrom(Destination src)
		{
			if (_address == null) _address = src._address;
			if (_port == null) _port = src._port;
		}

		private class Optional<T>
		{
			internal T Value;
		}
	}
}
