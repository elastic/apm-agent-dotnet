using Elastic.Apm.Api;

namespace Elastic.Apm.Helpers
{
	internal static class GrpcHelper
	{
		internal static string GrpcReturnCodeToString(string returnCode)
		{
			if (!int.TryParse(returnCode, out var intValue))
				return null;

			return intValue switch
			{
				0 => "OK",
				1 => "CANCELLED",
				2 => "UNKNOWN",
				3 => "INVALID_ARGUMENT",
				4 => "DEADLINE_EXCEEDED",
				5 => "NOT_FOUND",
				6 => "ALREADY_EXISTS",
				7 => "PERMISSION_DENIED",
				8 => "RESOURCE_EXHAUSTED",
				9 => "FAILED_PRECONDITION",
				10 => "ABORTED",
				11 => "OUT_OF_RANGE",
				12 => "UNIMPLEMENTED",
				13 => "INTERNAL",
				14 => "UNAVAILABLE",
				15 => "DATA_LOSS",
				16 => "UNAUTHENTICATED",
				_ => null
			};
		}

		internal static Outcome GrpcServerReturnCodeToOutcome(string returnCode) => returnCode.ToUpper() switch
		{
			"OK" => Outcome.Success,
			"CANCELLED" => Outcome.Success,
			"UNKNOWN" => Outcome.Failure,
			"INVALID_ARGUMENT" => Outcome.Success,
			"DEADLINE_EXCEEDED" => Outcome.Failure,
			"NOT_FOUND" => Outcome.Success,
			"ALREADY_EXISTS" => Outcome.Success,
			"PERMISSION_DENIED" => Outcome.Success,
			"RESOURCE_EXHAUSTED" => Outcome.Failure,
			"FAILED_PRECONDITION" => Outcome.Failure,
			"ABORTED" => Outcome.Failure,
			"OUT_OF_RANGE" => Outcome.Success,
			"UNIMPLEMENTED" => Outcome.Success,
			"INTERNAL" => Outcome.Failure,
			"UNAVAILABLE" => Outcome.Failure,
			"DATA_LOSS" => Outcome.Failure,
			"UNAUTHENTICATED" => Outcome.Success,
			_ => Outcome.Failure,
		};

		internal static Outcome GrpcClientReturnCodeToOutcome(string returnCode) => returnCode.ToUpper() switch
		{
			"OK" => Outcome.Success,
			"CANCELLED" => Outcome.Failure,
			"UNKNOWN" => Outcome.Failure,
			"INVALID_ARGUMENT" => Outcome.Failure,
			"DEADLINE_EXCEEDED" => Outcome.Failure,
			"NOT_FOUND" => Outcome.Failure,
			"ALREADY_EXISTS" => Outcome.Failure,
			"PERMISSION_DENIED" => Outcome.Failure,
			"RESOURCE_EXHAUSTED" => Outcome.Failure,
			"FAILED_PRECONDITION" => Outcome.Failure,
			"ABORTED" => Outcome.Failure,
			"OUT_OF_RANGE" => Outcome.Failure,
			"UNIMPLEMENTED" => Outcome.Failure,
			"INTERNAL" => Outcome.Failure,
			"UNAVAILABLE" => Outcome.Failure,
			"DATA_LOSS" => Outcome.Failure,
			"UNAUTHENTICATED" => Outcome.Failure,
			_ => Outcome.Failure,
		};
	}
}
