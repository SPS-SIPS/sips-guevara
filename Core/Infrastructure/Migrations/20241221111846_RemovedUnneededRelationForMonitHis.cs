using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SPSS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemovedUnneededRelationForMonitHis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_monithistories_endpoints_endpointid",
                table: "monithistories");

            migrationBuilder.DropForeignKey(
                name: "fk_monithistories_tunnels_tunnelid",
                table: "monithistories");

            migrationBuilder.DropIndex(
                name: "ix_monithistories_endpointid",
                table: "monithistories");

            migrationBuilder.DropIndex(
                name: "ix_monithistories_tunnelid",
                table: "monithistories");

            migrationBuilder.DropColumn(
                name: "endpointid",
                table: "monithistories");

            migrationBuilder.DropColumn(
                name: "tunnelid",
                table: "monithistories");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "endpointid",
                table: "monithistories",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "tunnelid",
                table: "monithistories",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_monithistories_endpointid",
                table: "monithistories",
                column: "endpointid");

            migrationBuilder.CreateIndex(
                name: "ix_monithistories_tunnelid",
                table: "monithistories",
                column: "tunnelid");

            migrationBuilder.AddForeignKey(
                name: "fk_monithistories_endpoints_endpointid",
                table: "monithistories",
                column: "endpointid",
                principalTable: "endpoints",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_monithistories_tunnels_tunnelid",
                table: "monithistories",
                column: "tunnelid",
                principalTable: "tunnels",
                principalColumn: "id");
        }
    }
}
