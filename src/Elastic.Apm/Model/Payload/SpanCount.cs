namespace Elastic.Apm.Model.Payload
{
	internal class SpanCount
	{
		public int Started { get; set; }
		public int Dropped { get; set; }
	}
}
