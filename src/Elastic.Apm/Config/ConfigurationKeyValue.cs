namespace Elastic.Apm.Config
{
	public class ConfigurationKeyValue
	{
		public ConfigurationKeyValue(string key, string value, string readFrom) =>
			(Key, Value, ReadFrom) = (key, value, readFrom);

		public string Key { get; }
		public string ReadFrom { get; }
		public string Value { get; }
	}
}
