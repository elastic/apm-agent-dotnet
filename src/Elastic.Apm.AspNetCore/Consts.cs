namespace Elastic.Apm.AspNetCore
{
	internal static class Consts
	{
		public const int RequestBodyMaxLength = 2048;

		internal static class OpenIdClaimTypes
		{
			internal const string Email = "email";
			internal const string UserId = "sub";
		}
	}
}
