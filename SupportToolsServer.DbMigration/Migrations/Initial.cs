#nullable disable

using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SupportToolsServer.DbMigration.Migrations;

/// <inheritdoc />
public partial class Initial : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("GitIgnoreFileTypes",
            table => new
            {
                Id = table.Column<Guid>("uniqueidentifier", nullable: false),
                Name = table.Column<string>("nvarchar(50)", maxLength: 50, nullable: false),
                Content = table.Column<string>("nvarchar(max)", maxLength: 16384, nullable: false)
            }, constraints: table =>
            {
                table.PrimaryKey("PK_GitIgnoreFileTypes", x => x.Id);
            });

        migrationBuilder.CreateIndex("IX_GitIgnoreFileTypes_Name", "GitIgnoreFileTypes", "Name", unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("GitIgnoreFileTypes");
    }
}
