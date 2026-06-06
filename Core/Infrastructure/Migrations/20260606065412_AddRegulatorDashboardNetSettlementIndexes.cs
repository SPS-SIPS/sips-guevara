using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SPSS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRegulatorDashboardNetSettlementIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_netsettlementdetails_creditorbic_date",
                table: "netsettlementdetails",
                columns: new[] { "creditorbic", "date" });

            migrationBuilder.CreateIndex(
                name: "ix_netsettlementdetails_debtorbic_date",
                table: "netsettlementdetails",
                columns: new[] { "debtorbic", "date" });

            migrationBuilder.CreateIndex(
                name: "ix_netsettlementdetails_isfee_date",
                table: "netsettlementdetails",
                columns: new[] { "isfee", "date" });

            migrationBuilder.CreateIndex(
                name: "ix_netsettlementdetails_txid",
                table: "netsettlementdetails",
                column: "txid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_netsettlementdetails_creditorbic_date",
                table: "netsettlementdetails");

            migrationBuilder.DropIndex(
                name: "ix_netsettlementdetails_debtorbic_date",
                table: "netsettlementdetails");

            migrationBuilder.DropIndex(
                name: "ix_netsettlementdetails_isfee_date",
                table: "netsettlementdetails");

            migrationBuilder.DropIndex(
                name: "ix_netsettlementdetails_txid",
                table: "netsettlementdetails");
        }
    }
}
