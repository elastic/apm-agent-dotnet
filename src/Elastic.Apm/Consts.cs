namespace Elastic.Apm
{
	internal static class Consts
	{
		public const string IntakeV1Errors = "v1/errors";
		public const string IntakeV1Transactions = "v1/transactions";

		public const int PropertyMaxLength = 1024;

		public static string AgentName => "dotNet";
	}
}
