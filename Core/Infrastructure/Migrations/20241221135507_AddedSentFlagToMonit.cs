using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SPSS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedSentFlagToMonit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "gateway",
                table: "tunnels");

            migrationBuilder.DropColumn(
                name: "gateway",
                table: "endpoints");

            migrationBuilder.AddColumn<bool>(
                name: "sent",
                table: "monithistories",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "sent",
                table: "monithistories");

            migrationBuilder.AddColumn<string>(
                name: "gateway",
                table: "tunnels",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "gateway",
                table: "endpoints",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
