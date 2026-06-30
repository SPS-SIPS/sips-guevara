using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SPSS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOracleTransactionsReceiver : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "oracletransactioningestionbatches",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    fromid = table.Column<long>(type: "bigint", nullable: false),
                    toid = table.Column<long>(type: "bigint", nullable: false),
                    highwatermarkid = table.Column<long>(type: "bigint", nullable: false),
                    producertimestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    statuses = table.Column<List<string>>(type: "text[]", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    receivedat = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processedat = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    totalcount = table.Column<int>(type: "integer", nullable: false),
                    receivedcount = table.Column<int>(type: "integer", nullable: false),
                    insertedcount = table.Column<int>(type: "integer", nullable: false),
                    updatedcount = table.Column<int>(type: "integer", nullable: false),
                    duplicatecount = table.Column<int>(type: "integer", nullable: false),
                    rejectedcount = table.Column<int>(type: "integer", nullable: false),
                    errormessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    sampleerrors = table.Column<List<string>>(type: "text[]", nullable: false),
                    rawpayload = table.Column<string>(type: "text", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_oracletransactioningestionbatches", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "oracletransactions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    oracleid = table.Column<long>(type: "bigint", nullable: false),
                    parentid = table.Column<long>(type: "bigint", nullable: true),
                    bankfrom = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    bankto = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    createddate = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    createdat = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modifieddate = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    modifiedat = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lastmodifieddate = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    lastmodifiedat = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    acceptancedate = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    acceptanceat = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    txid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    typedescription = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    amount = table.Column<string>(type: "text", nullable: true),
                    amountvalue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    txsts = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    intrbksttlmatm = table.Column<string>(type: "text", nullable: true),
                    intrbksttlmatmvalue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    intrbksttlmcurr = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    dbtracctid = table.Column<string>(type: "text", nullable: true),
                    dbtraccttype = table.Column<string>(type: "text", nullable: true),
                    dbtrname = table.Column<string>(type: "text", nullable: true),
                    dbtraddress = table.Column<string>(type: "text", nullable: true),
                    cdtrname = table.Column<string>(type: "text", nullable: true),
                    cdtraddress = table.Column<string>(type: "text", nullable: true),
                    cdtracctid = table.Column<string>(type: "text", nullable: true),
                    cdtraccttype = table.Column<string>(type: "text", nullable: true),
                    sttlid = table.Column<long>(type: "bigint", nullable: true),
                    error = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    errdesc = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    errorreason = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    requestid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    transcode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    lclinstrm = table.Column<string>(type: "text", nullable: true),
                    ctgypurp = table.Column<string>(type: "text", nullable: true),
                    rtrid = table.Column<string>(type: "text", nullable: true),
                    oldbalance = table.Column<string>(type: "text", nullable: true),
                    newbalance = table.Column<string>(type: "text", nullable: true),
                    bankfromsaposition = table.Column<string>(type: "text", nullable: true),
                    banktosaposition = table.Column<string>(type: "text", nullable: true),
                    bankfromwaposition = table.Column<string>(type: "text", nullable: true),
                    banktowaposition = table.Column<string>(type: "text", nullable: true),
                    endtoendid = table.Column<string>(type: "text", nullable: true),
                    iban = table.Column<string>(type: "text", nullable: true),
                    rtpid = table.Column<string>(type: "text", nullable: true),
                    attributesraw = table.Column<string>(type: "text", nullable: true),
                    orgtrxid = table.Column<string>(type: "text", nullable: true),
                    stsrsninfrsn = table.Column<string>(type: "text", nullable: true),
                    stsrsninfaddtlinf = table.Column<string>(type: "text", nullable: true),
                    mandatereqid = table.Column<string>(type: "text", nullable: true),
                    transactionraw = table.Column<string>(type: "text", nullable: true),
                    transactionsraw = table.Column<string>(type: "text", nullable: true),
                    messagesraw = table.Column<string>(type: "text", nullable: true),
                    rawpayload = table.Column<string>(type: "text", nullable: true),
                    receivedat = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_oracletransactions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_oracletransactioningestionbatches_processedat",
                table: "oracletransactioningestionbatches",
                column: "processedat");

            migrationBuilder.CreateIndex(
                name: "ix_oracletransactioningestionbatches_receivedat",
                table: "oracletransactioningestionbatches",
                column: "receivedat");

            migrationBuilder.CreateIndex(
                name: "ix_oracletransactioningestionbatches_source",
                table: "oracletransactioningestionbatches",
                column: "source");

            migrationBuilder.CreateIndex(
                name: "ix_oracletransactioningestionbatches_source_fromid_toid_highwa~",
                table: "oracletransactioningestionbatches",
                columns: new[] { "source", "fromid", "toid", "highwatermarkid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_oracletransactioningestionbatches_status",
                table: "oracletransactioningestionbatches",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_oracletransactions_bankfrom",
                table: "oracletransactions",
                column: "bankfrom");

            migrationBuilder.CreateIndex(
                name: "ix_oracletransactions_bankto",
                table: "oracletransactions",
                column: "bankto");

            migrationBuilder.CreateIndex(
                name: "ix_oracletransactions_createdat",
                table: "oracletransactions",
                column: "createdat");

            migrationBuilder.CreateIndex(
                name: "ix_oracletransactions_receivedat",
                table: "oracletransactions",
                column: "receivedat");

            migrationBuilder.CreateIndex(
                name: "ix_oracletransactions_requestid",
                table: "oracletransactions",
                column: "requestid");

            migrationBuilder.CreateIndex(
                name: "ix_oracletransactions_source_oracleid",
                table: "oracletransactions",
                columns: new[] { "source", "oracleid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_oracletransactions_status",
                table: "oracletransactions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_oracletransactions_txid",
                table: "oracletransactions",
                column: "txid");

            migrationBuilder.CreateIndex(
                name: "ix_oracletransactions_txsts",
                table: "oracletransactions",
                column: "txsts");

            migrationBuilder.CreateIndex(
                name: "ix_oracletransactions_type",
                table: "oracletransactions",
                column: "type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "oracletransactioningestionbatches");

            migrationBuilder.DropTable(
                name: "oracletransactions");
        }
    }
}
