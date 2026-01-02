using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupportToolsServer.DbMigration.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GitIgnoreFileTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", maxLength: 16384, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GitIgnoreFileTypes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GitIgnoreFileTypes_Name",
                table: "GitIgnoreFileTypes",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GitIgnoreFileTypes");
        }
    }
}
