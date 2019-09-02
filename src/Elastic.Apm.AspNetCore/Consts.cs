namespace Elastic.Apm.AspNetCore
{
	internal static class Consts
	{
		public const int RequestBodyMaxLength = 2048;
		public const string BodyRedacted = "REDACTED";
		internal static class OpenIdClaimTypes
		{
			internal const string UserId = "sub";
			internal const string Email = "email";
		}
	}
}
