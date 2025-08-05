using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SPSS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExtendedHistoryWithIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_monithistories_checkedat",
                table: "monithistories",
                column: "checkedat");

            migrationBuilder.CreateIndex(
                name: "ix_monithistories_institutionbic",
                table: "monithistories",
                column: "institutionbic");

            migrationBuilder.CreateIndex(
                name: "ix_monithistories_institutionname",
                table: "monithistories",
                column: "institutionname");

            migrationBuilder.CreateIndex(
                name: "ix_monithistories_success",
                table: "monithistories",
                column: "success");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_monithistories_checkedat",
                table: "monithistories");

            migrationBuilder.DropIndex(
                name: "ix_monithistories_institutionbic",
                table: "monithistories");

            migrationBuilder.DropIndex(
                name: "ix_monithistories_institutionname",
                table: "monithistories");

            migrationBuilder.DropIndex(
                name: "ix_monithistories_success",
                table: "monithistories");
        }
    }
}
