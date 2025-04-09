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
                name: "ApiKeysByRemoteIpAddresses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApiKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RemoteIpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeysByRemoteIpAddresses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GitIgnoreFileTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", maxLength: 16384, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GitIgnoreFileTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GitData",
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
                    table.PrimaryKey("PK_GitData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GitData_GitIgnoreFileTypes_GitIgnoreFileTypeId",
                        column: x => x.GitIgnoreFileTypeId,
                        principalTable: "GitIgnoreFileTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeysByRemoteIpAddresses_ApiKey_RemoteIpAddress",
                table: "ApiKeysByRemoteIpAddresses",
                columns: new[] { "ApiKey", "RemoteIpAddress" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GitData_GitIgnoreFileTypeId",
                table: "GitData",
                column: "GitIgnoreFileTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_GitData_Name",
                table: "GitData",
                column: "Name",
                unique: true);

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
                name: "ApiKeysByRemoteIpAddresses");

            migrationBuilder.DropTable(
                name: "GitData");

            migrationBuilder.DropTable(
                name: "GitIgnoreFileTypes");
        }
    }
}
