using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SPSS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedAtToMonitHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "createdat",
                table: "monithistories",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: DateTimeOffset.UtcNow);

            migrationBuilder.CreateIndex(
                name: "ix_monithistories_createdat",
                table: "monithistories",
                column: "createdat");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_monithistories_createdat",
                table: "monithistories");

            migrationBuilder.DropColumn(
                name: "createdat",
                table: "monithistories");
        }
    }
}
