using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SPSS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TransactionIngestionBatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "transactioningestionbatches",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    batchtype = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    fromdate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    todate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    receivedat = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processedat = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    snapshottype = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    snapshotstatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    totalcount = table.Column<int>(type: "integer", nullable: false),
                    receivedcount = table.Column<int>(type: "integer", nullable: false),
                    uniquecount = table.Column<int>(type: "integer", nullable: false),
                    insertedcount = table.Column<int>(type: "integer", nullable: false),
                    updatedcount = table.Column<int>(type: "integer", nullable: false),
                    duplicatecount = table.Column<int>(type: "integer", nullable: false),
                    rejectedcount = table.Column<int>(type: "integer", nullable: false),
                    errormessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    sampleerrors = table.Column<List<string>>(type: "text[]", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_transactioningestionbatches", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_transactioningestionbatches_batchtype",
                table: "transactioningestionbatches",
                column: "batchtype");

            migrationBuilder.CreateIndex(
                name: "ix_transactioningestionbatches_fromdate",
                table: "transactioningestionbatches",
                column: "fromdate");

            migrationBuilder.CreateIndex(
                name: "ix_transactioningestionbatches_receivedat",
                table: "transactioningestionbatches",
                column: "receivedat");

            migrationBuilder.CreateIndex(
                name: "ix_transactioningestionbatches_status",
                table: "transactioningestionbatches",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_transactioningestionbatches_todate",
                table: "transactioningestionbatches",
                column: "todate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "transactioningestionbatches");
        }
    }
}
