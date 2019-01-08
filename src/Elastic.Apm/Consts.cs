using System;

namespace Elastic.Apm
{
	internal static class Consts
	{
		public static String IntakeV1Errors = "v1/errors";
		public static String IntakeV1Transactions = "/v1/transactions";

		public static String AgentName => "dotNet";
		public static String AgentVersion => "0.1"; //TODO: read assembly version
	}
}
