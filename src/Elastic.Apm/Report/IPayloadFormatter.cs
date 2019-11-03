namespace Elastic.Apm.Report
{
	internal interface IPayloadFormatter
	{
		string FormatPayload(object[] items);
	}
}
