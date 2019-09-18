namespace Elastic.Apm.Config
{
	internal interface IConfigStore: IConfigSnapshotProvider
	{
		new IConfigSnapshot CurrentSnapshot { get; set; }
	}
}
