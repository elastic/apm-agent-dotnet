// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SampleAspNetCoreApp.Migrations
{
	public partial class InitDbWithAuthentication : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.CreateTable(
				"AspNetRoles",
				table => new
				{
					Id = table.Column<string>(),
					Name = table.Column<string>(maxLength: 256, nullable: true),
					NormalizedName = table.Column<string>(maxLength: 256, nullable: true),
					ConcurrencyStamp = table.Column<string>(nullable: true)
				},
				constraints: table => { table.PrimaryKey("PK_AspNetRoles", x => x.Id); });

			migrationBuilder.CreateTable(
				"AspNetUsers",
				table => new
				{
					Id = table.Column<string>(),
					UserName = table.Column<string>(maxLength: 256, nullable: true),
					NormalizedUserName = table.Column<string>(maxLength: 256, nullable: true),
					Email = table.Column<string>(maxLength: 256, nullable: true),
					NormalizedEmail = table.Column<string>(maxLength: 256, nullable: true),
					EmailConfirmed = table.Column<bool>(),
					PasswordHash = table.Column<string>(nullable: true),
					SecurityStamp = table.Column<string>(nullable: true),
					ConcurrencyStamp = table.Column<string>(nullable: true),
					PhoneNumber = table.Column<string>(nullable: true),
					PhoneNumberConfirmed = table.Column<bool>(),
					TwoFactorEnabled = table.Column<bool>(),
					LockoutEnd = table.Column<DateTimeOffset>(nullable: true),
					LockoutEnabled = table.Column<bool>(),
					AccessFailedCount = table.Column<int>()
				},
				constraints: table => { table.PrimaryKey("PK_AspNetUsers", x => x.Id); });

			migrationBuilder.CreateTable(
				"SampleTable",
				table => new
				{
					Id = table.Column<int>()
						.Annotation("Sqlite:Autoincrement", true),
					Name = table.Column<string>(nullable: true)
				},
				constraints: table => { table.PrimaryKey("PK_SampleTable", x => x.Id); });

			migrationBuilder.CreateTable(
				"AspNetRoleClaims",
				table => new
				{
					Id = table.Column<int>()
						.Annotation("Sqlite:Autoincrement", true),
					RoleId = table.Column<string>(),
					ClaimType = table.Column<string>(nullable: true),
					ClaimValue = table.Column<string>(nullable: true)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
					table.ForeignKey(
						"FK_AspNetRoleClaims_AspNetRoles_RoleId",
						x => x.RoleId,
						"AspNetRoles",
						"Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				"AspNetUserClaims",
				table => new
				{
					Id = table.Column<int>()
						.Annotation("Sqlite:Autoincrement", true),
					UserId = table.Column<string>(),
					ClaimType = table.Column<string>(nullable: true),
					ClaimValue = table.Column<string>(nullable: true)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
					table.ForeignKey(
						"FK_AspNetUserClaims_AspNetUsers_UserId",
						x => x.UserId,
						"AspNetUsers",
						"Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				"AspNetUserLogins",
				table => new
				{
					LoginProvider = table.Column<string>(maxLength: 128),
					ProviderKey = table.Column<string>(maxLength: 128),
					ProviderDisplayName = table.Column<string>(nullable: true),
					UserId = table.Column<string>()
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
					table.ForeignKey(
						"FK_AspNetUserLogins_AspNetUsers_UserId",
						x => x.UserId,
						"AspNetUsers",
						"Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				"AspNetUserRoles",
				table => new { UserId = table.Column<string>(), RoleId = table.Column<string>() },
				constraints: table =>
				{
					table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
					table.ForeignKey(
						"FK_AspNetUserRoles_AspNetRoles_RoleId",
						x => x.RoleId,
						"AspNetRoles",
						"Id",
						onDelete: ReferentialAction.Cascade);
					table.ForeignKey(
						"FK_AspNetUserRoles_AspNetUsers_UserId",
						x => x.UserId,
						"AspNetUsers",
						"Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				"AspNetUserTokens",
				table => new
				{
					UserId = table.Column<string>(),
					LoginProvider = table.Column<string>(maxLength: 128),
					Name = table.Column<string>(maxLength: 128),
					Value = table.Column<string>(nullable: true)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
					table.ForeignKey(
						"FK_AspNetUserTokens_AspNetUsers_UserId",
						x => x.UserId,
						"AspNetUsers",
						"Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateIndex(
				"IX_AspNetRoleClaims_RoleId",
				"AspNetRoleClaims",
				"RoleId");

			migrationBuilder.CreateIndex(
				"RoleNameIndex",
				"AspNetRoles",
				"NormalizedName",
				unique: true);

			migrationBuilder.CreateIndex(
				"IX_AspNetUserClaims_UserId",
				"AspNetUserClaims",
				"UserId");

			migrationBuilder.CreateIndex(
				"IX_AspNetUserLogins_UserId",
				"AspNetUserLogins",
				"UserId");

			migrationBuilder.CreateIndex(
				"IX_AspNetUserRoles_RoleId",
				"AspNetUserRoles",
				"RoleId");

			migrationBuilder.CreateIndex(
				"EmailIndex",
				"AspNetUsers",
				"NormalizedEmail");

			migrationBuilder.CreateIndex(
				"UserNameIndex",
				"AspNetUsers",
				"NormalizedUserName",
				unique: true);
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				"AspNetRoleClaims");

			migrationBuilder.DropTable(
				"AspNetUserClaims");

			migrationBuilder.DropTable(
				"AspNetUserLogins");

			migrationBuilder.DropTable(
				"AspNetUserRoles");

			migrationBuilder.DropTable(
				"AspNetUserTokens");

			migrationBuilder.DropTable(
				"SampleTable");

			migrationBuilder.DropTable(
				"AspNetRoles");

			migrationBuilder.DropTable(
				"AspNetUsers");
		}
	}
}
