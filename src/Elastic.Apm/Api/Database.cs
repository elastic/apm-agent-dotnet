namespace Elastic.Apm.Api
{
	/// <summary>
	/// An object containing contextual data for database spans.
	/// It can be attached to an <see cref="ISpan"/> through <see cref="ISpan.Context"/>
	/// </summary>
	public class Database
	{
		public string Instance { get; set; }
		public string Statement { get; set; }
		public string Type { get; set; }

		public const string TypeSql = "sql";
		public const string TypeElasticsearch = "elasticsearch";
	}
}
