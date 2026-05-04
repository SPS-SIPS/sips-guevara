using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SPSS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FailedTransactionComplianceReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "failedtransactioncompliancereports",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    windowstart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    windowend = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    baselinestart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    baselineend = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    generatedat = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    totalfailed = table.Column<int>(type: "integer", nullable: false),
                    baselinetotalfailed = table.Column<int>(type: "integer", nullable: false),
                    baselineaveragehourly = table.Column<decimal>(type: "numeric", nullable: false),
                    uniquedebtors = table.Column<int>(type: "integer", nullable: false),
                    uniquecreditors = table.Column<int>(type: "integer", nullable: false),
                    highriskfindings = table.Column<int>(type: "integer", nullable: false),
                    mediumriskfindings = table.Column<int>(type: "integer", nullable: false),
                    reportjson = table.Column<string>(type: "text", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_failedtransactioncompliancereports", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_failedtransactioncompliancereports_generatedat",
                table: "failedtransactioncompliancereports",
                column: "generatedat");

            migrationBuilder.CreateIndex(
                name: "ix_failedtransactioncompliancereports_highriskfindings",
                table: "failedtransactioncompliancereports",
                column: "highriskfindings");

            migrationBuilder.CreateIndex(
                name: "ix_failedtransactioncompliancereports_totalfailed",
                table: "failedtransactioncompliancereports",
                column: "totalfailed");

            migrationBuilder.CreateIndex(
                name: "ix_failedtransactioncompliancereports_windowstart_windowend",
                table: "failedtransactioncompliancereports",
                columns: new[] { "windowstart", "windowend" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "failedtransactioncompliancereports");
        }
    }
}
