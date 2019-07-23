using System;
using System.IO;
using System.Text;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;

namespace Elastic.Apm.AspNetCore.Helpers
{
	/// <summary>
	/// Simplifies the way to determine if we should collect request field's body during trx, errors etc.
	/// </summary>
    public static class AgentConfigHelper
    {
		/// <summary>
		/// Returns weather we should collect the request body in case of error (accoridng to the Apm configuration)
		/// </summary>
		/// <returns></returns>
		public static bool ShouldExtractRequestBodyOnError() =>
			(Agent.Config.CaptureBody.Equals(ConfigConsts.SupportedValues.CaptureBodyAll) ||
				Agent.Config.CaptureBody.Equals(ConfigConsts.SupportedValues.CaptureBodyErrors));

		/// <summary>
		/// Returns weather we should collect the request body in transaction logging (accoridng to the Apm configuration)
		/// </summary>
		/// <returns></returns>
		public static bool ShouldExtractRequestBodyOnTransactios() =>
			(Agent.Config.CaptureBody.Equals(ConfigConsts.SupportedValues.CaptureBodyAll) ||
				Agent.Config.CaptureBody.Equals(ConfigConsts.SupportedValues.CaptureBodyTransactions));



	}
}
