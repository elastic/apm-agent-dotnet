using Topshelf;

namespace SampleHttpSelfHostApp
{
	internal class Program
	{
		public static void Main(string[] args) =>
			HostFactory.Run(x =>
			{
				x.Service<ApiService>(s =>
				{
					s.ConstructUsing(name => new ApiService());
					s.WhenStarted(ws => ws.Start());
					s.WhenStopped(ws => ws.Stop());
				});

				x.RunAsLocalService();
				x.StartAutomatically();
			});
	}
}
