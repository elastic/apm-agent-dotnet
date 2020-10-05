namespace Elastic.Apm.Helpers
{
	internal static class GrpcHelper
	{
		internal static string GrpcReturnCodeToString(string returnCode)
		{
			if (!int.TryParse(returnCode, out var intValue)) return null;

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
	}
}
