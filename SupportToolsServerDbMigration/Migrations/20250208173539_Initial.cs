using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupportToolsServerDbMigration.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gitIgnoreFileTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", maxLength: 16384, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gitIgnoreFileTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gitDatas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GitAddress = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GitIgnoreFileTypeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gitDatas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_gitDatas_gitIgnoreFileTypes_GitIgnoreFileTypeId",
                        column: x => x.GitIgnoreFileTypeId,
                        principalTable: "gitIgnoreFileTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gitDatas_GitIgnoreFileTypeId",
                table: "gitDatas",
                column: "GitIgnoreFileTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_GitDatas_name_Unique",
                table: "gitDatas",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GitIgnoreFileTypes_name_Unique",
                table: "gitIgnoreFileTypes",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gitDatas");

            migrationBuilder.DropTable(
                name: "gitIgnoreFileTypes");
        }
    }
}
