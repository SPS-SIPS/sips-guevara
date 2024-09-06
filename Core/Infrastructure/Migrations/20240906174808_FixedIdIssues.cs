using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SPSS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixedIdIssues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "normalizedname",
                table: "participants");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "normalizedname",
                table: "participants",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
