using System.Threading.Tasks;

namespace Elastic.Apm.Tests.TestHelpers
{
	internal static class TaskTestExtensions
	{
		/// <summary>
		/// Task.IsCompletedSuccessfully is defined only for .NET Core 3.0 2.2 2.1 2.0 (.NET Standard 2.1 Preview)
		/// and it's not defined for .NET Framework at all
		/// https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.iscompletedsuccessfully
		/// So we need to implement IsCompletedSuccessfully ourselves when targeting .NET Framework.
		/// C#, as of versions 7.2, doesn't support extension properties
		/// so the only option is to implement IsCompletedSuccessfully as an extension method.
		/// </summary>
		internal static bool IsCompletedSuccessfully(this Task thisObj) =>
#if NETFRAMEWORK
			thisObj.IsCompleted && !(thisObj.IsFaulted || thisObj.IsCanceled);
#else
			thisObj.IsCompletedSuccessfully;
#endif
	}
}
