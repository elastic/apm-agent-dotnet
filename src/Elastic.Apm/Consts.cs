using System;

namespace Elastic.Apm
{
	internal static class Consts
	{
		public static string IntakeV1Errors = "v1/errors";
		public static string IntakeV1Transactions = "/v1/transactions";

		public static string AgentName => "dotNet";
		public static string AgentVersion => "0.1"; //TODO: read assembly version
	}
}
