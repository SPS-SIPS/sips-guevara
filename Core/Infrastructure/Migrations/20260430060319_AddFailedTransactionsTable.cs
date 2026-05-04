using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SPSS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFailedTransactionsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "failedtransactions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ipstransactionid = table.Column<long>(type: "bigint", nullable: false),
                    snapshottimestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    snapshotfromdate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    snapshottodate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    receivedat = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ruleids = table.Column<List<string>>(type: "text[]", nullable: false),
                    bankfrom = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    bankfromname = table.Column<string>(type: "text", nullable: true),
                    bankto = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    banktoname = table.Column<string>(type: "text", nullable: true),
                    createddate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modifieddate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lastmodifieddate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    acceptancedate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    txid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    amount = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    sttlid = table.Column<long>(type: "bigint", nullable: true),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    intrbksttlmcurr = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
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
                    oldbalance = table.Column<string>(type: "text", nullable: true),
                    newbalance = table.Column<string>(type: "text", nullable: true),
                    aliasfrom = table.Column<string>(type: "text", nullable: true),
                    aliasto = table.Column<string>(type: "text", nullable: true),
                    aliastypefrom = table.Column<string>(type: "text", nullable: true),
                    aliastypeto = table.Column<string>(type: "text", nullable: true),
                    error = table.Column<string>(type: "text", nullable: true),
                    errorreason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    rawtransactionjson = table.Column<string>(type: "text", nullable: true),
                    transactionsjson = table.Column<string>(type: "text", nullable: true),
                    messagesjson = table.Column<string>(type: "text", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_failedtransactions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_failedtransactions_bankfrom",
                table: "failedtransactions",
                column: "bankfrom");

            migrationBuilder.CreateIndex(
                name: "ix_failedtransactions_bankto",
                table: "failedtransactions",
                column: "bankto");

            migrationBuilder.CreateIndex(
                name: "ix_failedtransactions_createddate",
                table: "failedtransactions",
                column: "createddate");

            migrationBuilder.CreateIndex(
                name: "ix_failedtransactions_ipstransactionid",
                table: "failedtransactions",
                column: "ipstransactionid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_failedtransactions_requestid",
                table: "failedtransactions",
                column: "requestid");

            migrationBuilder.CreateIndex(
                name: "ix_failedtransactions_snapshotfromdate",
                table: "failedtransactions",
                column: "snapshotfromdate");

            migrationBuilder.CreateIndex(
                name: "ix_failedtransactions_snapshottodate",
                table: "failedtransactions",
                column: "snapshottodate");

            migrationBuilder.CreateIndex(
                name: "ix_failedtransactions_status",
                table: "failedtransactions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_failedtransactions_txid",
                table: "failedtransactions",
                column: "txid");

            migrationBuilder.CreateIndex(
                name: "ix_failedtransactions_type",
                table: "failedtransactions",
                column: "type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "failedtransactions");
        }
    }
}
