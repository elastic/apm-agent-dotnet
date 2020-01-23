using System.Threading.Tasks;


namespace Elastic.Apm.Tests.Extensions
{
	internal static class TaskExtensions
	{
		public static bool IsCompletedSuccessfully(this Task task) =>
#if NET461
			task.Status == TaskStatus.RanToCompletion && !task.IsFaulted && !task.IsCanceled;
#else
			task.IsCompletedSuccessfully;
#endif
	}
}
