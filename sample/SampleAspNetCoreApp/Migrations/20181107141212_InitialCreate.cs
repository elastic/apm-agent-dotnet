using Microsoft.EntityFrameworkCore.Migrations;

namespace SampleAspNetCoreApp.Migrations
{
	public partial class InitialCreate : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder) =>
			migrationBuilder.CreateTable(
				"Users",
				table => new
				{
					Id = table.Column<int>(nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					Name = table.Column<string>(nullable: true)
				},
				constraints: table => { table.PrimaryKey("PK_Users", x => x.Id); });

		protected override void Down(MigrationBuilder migrationBuilder) =>
			migrationBuilder.DropTable(
				"Users");
	}
}
