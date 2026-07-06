using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SPSS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOracleFraudTransactionsReceiver : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "oraclefraudtransactioningestionbatches",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    fromutrnno = table.Column<long>(type: "bigint", nullable: false),
                    toutrnno = table.Column<long>(type: "bigint", nullable: false),
                    highwatermarkutrnno = table.Column<long>(type: "bigint", nullable: false),
                    producertimestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
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
                    active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_oraclefraudtransactioningestionbatches", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "oraclefraudtransactions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    utrnno = table.Column<long>(type: "bigint", nullable: false),
                    respcode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    fraudzone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ruleslist = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    rules = table.Column<List<string>>(type: "text[]", nullable: false),
                    refnum = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    riskvalue = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    receivedat = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_oraclefraudtransactions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_oraclefraudtransactioningestionbatches_processedat",
                table: "oraclefraudtransactioningestionbatches",
                column: "processedat");

            migrationBuilder.CreateIndex(
                name: "ix_oraclefraudtransactioningestionbatches_receivedat",
                table: "oraclefraudtransactioningestionbatches",
                column: "receivedat");

            migrationBuilder.CreateIndex(
                name: "ix_oraclefraudtransactioningestionbatches_source",
                table: "oraclefraudtransactioningestionbatches",
                column: "source");

            migrationBuilder.CreateIndex(
                name: "ix_oraclefraudtransactioningestionbatches_source_fromutrnno_to~",
                table: "oraclefraudtransactioningestionbatches",
                columns: new[] { "source", "fromutrnno", "toutrnno", "highwatermarkutrnno" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_oraclefraudtransactioningestionbatches_status",
                table: "oraclefraudtransactioningestionbatches",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_oraclefraudtransactions_fraudzone",
                table: "oraclefraudtransactions",
                column: "fraudzone");

            migrationBuilder.CreateIndex(
                name: "ix_oraclefraudtransactions_receivedat",
                table: "oraclefraudtransactions",
                column: "receivedat");

            migrationBuilder.CreateIndex(
                name: "ix_oraclefraudtransactions_refnum",
                table: "oraclefraudtransactions",
                column: "refnum");

            migrationBuilder.CreateIndex(
                name: "ix_oraclefraudtransactions_respcode",
                table: "oraclefraudtransactions",
                column: "respcode");

            migrationBuilder.CreateIndex(
                name: "ix_oraclefraudtransactions_source_utrnno",
                table: "oraclefraudtransactions",
                columns: new[] { "source", "utrnno" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_oraclefraudtransactions_utrnno",
                table: "oraclefraudtransactions",
                column: "utrnno");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "oraclefraudtransactioningestionbatches");

            migrationBuilder.DropTable(
                name: "oraclefraudtransactions");
        }
    }
}
