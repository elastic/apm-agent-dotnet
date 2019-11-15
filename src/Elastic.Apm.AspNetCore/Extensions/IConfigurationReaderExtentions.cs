using Elastic.Apm.Config;

namespace Elastic.Apm.AspNetCore.Extensions
{
	/// <summary>
	/// Simplifies the way to determine if we should collect request field's body during transactions, errors etc.
	/// </summary>
	public static class IConfigurationReaderExtentions
	{
		/// <summary>
		/// Returns weather we should collect the request body in case of error (according to the Apm configuration)
		/// </summary>
		/// <returns></returns>
		public static bool ShouldExtractRequestBodyOnError(this IConfigurationReader configurationReader) =>
			configurationReader.CaptureBody.Equals(ConfigConsts.SupportedValues.CaptureBodyAll) ||
			configurationReader.CaptureBody.Equals(ConfigConsts.SupportedValues.CaptureBodyErrors);

		/// <summary>
		/// Returns weather we should collect the request body in transaction logging (according to the Apm configuration)
		/// </summary>
		/// <returns></returns>
		public static bool ShouldExtractRequestBodyOnTransactions(this IConfigurationReader configurationReader) =>
			configurationReader.CaptureBody.Equals(ConfigConsts.SupportedValues.CaptureBodyAll) ||
			configurationReader.CaptureBody.Equals(ConfigConsts.SupportedValues.CaptureBodyTransactions);
	}
}
