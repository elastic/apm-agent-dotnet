namespace Elastic.Apm.Config
{
	internal interface IConfigSnapshot : IConfigSnapshotOptions
	{
		string DbgDescription { get; }
	}
}
