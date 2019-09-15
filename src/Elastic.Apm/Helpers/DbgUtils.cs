using System.Runtime.CompilerServices;

namespace Elastic.Apm.Helpers
{
	internal class DbgUtils
	{
		internal static string GetCurrentMethodName([CallerMemberName] string caller = null) => caller;
	}
}
