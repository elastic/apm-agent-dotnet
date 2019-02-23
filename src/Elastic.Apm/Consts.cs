namespace Elastic.Apm
{
	internal static class Consts
	{
		public static string IntakeV1Errors = "v1/errors";
		public static string IntakeV1Transactions = "v1/transactions";

		public static readonly int PropertyMaxLength = 1024;

		public static string AgentName => "dotNet";
	}
}
