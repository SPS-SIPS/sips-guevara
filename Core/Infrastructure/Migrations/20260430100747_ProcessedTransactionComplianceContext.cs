using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SPSS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProcessedTransactionComplianceContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "processedtransactions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    bankfrom = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    bankfromname = table.Column<string>(type: "text", nullable: true),
                    bankto = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    banktoname = table.Column<string>(type: "text", nullable: true),
                    createddate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modifieddate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    acceptancedate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    txid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    amount = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    sttlid = table.Column<long>(type: "bigint", nullable: true),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    intrbksttlmatm = table.Column<string>(type: "text", nullable: true),
                    dbtracctid = table.Column<string>(type: "text", nullable: true),
                    dbtraccttype = table.Column<string>(type: "text", nullable: true),
                    dbtrname = table.Column<string>(type: "text", nullable: true),
                    dbtraddress = table.Column<string>(type: "text", nullable: true),
                    cdtracctid = table.Column<string>(type: "text", nullable: true),
                    cdtrname = table.Column<string>(type: "text", nullable: true),
                    cdtraddress = table.Column<string>(type: "text", nullable: true),
                    requestid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    transcode = table.Column<string>(type: "text", nullable: true),
                    receivedat = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_processedtransactions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_processedtransactions_bankfrom",
                table: "processedtransactions",
                column: "bankfrom");

            migrationBuilder.CreateIndex(
                name: "ix_processedtransactions_bankto",
                table: "processedtransactions",
                column: "bankto");

            migrationBuilder.CreateIndex(
                name: "ix_processedtransactions_createddate",
                table: "processedtransactions",
                column: "createddate");

            migrationBuilder.CreateIndex(
                name: "ix_processedtransactions_receivedat",
                table: "processedtransactions",
                column: "receivedat");

            migrationBuilder.CreateIndex(
                name: "ix_processedtransactions_requestid",
                table: "processedtransactions",
                column: "requestid");

            migrationBuilder.CreateIndex(
                name: "ix_processedtransactions_status",
                table: "processedtransactions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_processedtransactions_txid",
                table: "processedtransactions",
                column: "txid");

            migrationBuilder.CreateIndex(
                name: "ix_processedtransactions_type",
                table: "processedtransactions",
                column: "type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "processedtransactions");
        }
    }
}
