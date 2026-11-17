using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SPSS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPersistentLoginThrottling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove retired token rows before the application drops their enum/storage support.
            migrationBuilder.Sql("DELETE FROM tokens WHERE tokentype = 2;");

            migrationBuilder.DropColumn(
                name: "value",
                table: "tokens");

            migrationBuilder.AddColumn<int>(
                name: "loginattemptcount",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "loginwindowstart",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "loginattemptcount",
                table: "users");

            migrationBuilder.DropColumn(
                name: "loginwindowstart",
                table: "users");

            migrationBuilder.AddColumn<int>(
                name: "value",
                table: "tokens",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
