using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WithDatabase.Migrations
{
    public partial class AddedNutStoreData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Nuts",
                columns: table => new
                {
                    Nut = table.Column<string>(nullable: false),
                    CreatedDate = table.Column<DateTime>(nullable: false),
                    IpAddress = table.Column<string>(nullable: true),
                    FirstNut = table.Column<string>(nullable: true),
                    Idk = table.Column<string>(nullable: true),
                    Authorized = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nuts", x => x.Nut);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Nuts");
        }
    }
}
