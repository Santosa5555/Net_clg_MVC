using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusIssueReporting.Migrations
{
    /// <inheritdoc />
    public partial class AddFileUrlToFileRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FileUrl",
                table: "FileRecords",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileUrl",
                table: "FileRecords");
        }
    }
}
