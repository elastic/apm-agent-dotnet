namespace Elastic.Apm.Config
{
	internal interface IConfigSnapshotProvider
	{
		IConfigSnapshot CurrentSnapshot { get; }
	}
}
