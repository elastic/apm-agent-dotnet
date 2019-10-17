using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.CodeFirst;

namespace AspNetFullFrameworkSampleApp.Data
{
	[Table("SampleDataTable")]
	public class SampleData
	{
		[Autoincrement]
		public int Id { get; set; }

		[Required]
		public string Name { get; set; }
	}
}
