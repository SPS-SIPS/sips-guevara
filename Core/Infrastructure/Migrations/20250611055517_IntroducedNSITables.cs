using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SPSS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IntroducedNSITables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "netsettlementheaders",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    msgid = table.Column<string>(type: "text", nullable: false),
                    originalname = table.Column<string>(type: "text", nullable: false),
                    creddttm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    numberoftrans = table.Column<int>(type: "integer", nullable: false),
                    totalamount = table.Column<decimal>(type: "numeric", nullable: false),
                    totalfees = table.Column<decimal>(type: "numeric", nullable: false),
                    totaltransactions = table.Column<decimal>(type: "numeric", nullable: false),
                    from = table.Column<string>(type: "text", nullable: false),
                    to = table.Column<string>(type: "text", nullable: false),
                    originaluserreference = table.Column<string>(type: "text", nullable: false),
                    bizmsgid = table.Column<string>(type: "text", nullable: false),
                    interbanksettlementdate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    originalfile = table.Column<string>(type: "text", nullable: false),
                    processed = table.Column<bool>(type: "boolean", nullable: false),
                    processedon = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    count = table.Column<int>(type: "integer", nullable: false),
                    error = table.Column<string>(type: "text", nullable: true),
                    filepath = table.Column<string>(type: "text", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_netsettlementheaders", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "netsettlementdetails",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    netsettlementheaderid = table.Column<long>(type: "bigint", nullable: false),
                    endtoendid = table.Column<string>(type: "text", nullable: false),
                    txid = table.Column<string>(type: "text", nullable: false),
                    uetr = table.Column<string>(type: "text", nullable: false),
                    instructionpriority = table.Column<string>(type: "text", nullable: false),
                    svclvl = table.Column<string>(type: "text", nullable: false),
                    localinstrument = table.Column<string>(type: "text", nullable: false),
                    date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    debtorbic = table.Column<string>(type: "text", nullable: false),
                    creditorbic = table.Column<string>(type: "text", nullable: false),
                    categorypurpose = table.Column<string>(type: "text", nullable: false),
                    isfee = table.Column<bool>(type: "boolean", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_netsettlementdetails", x => x.id);
                    table.ForeignKey(
                        name: "fk_netsettlementdetails_netsettlementheaders_netsettlementhead~",
                        column: x => x.netsettlementheaderid,
                        principalTable: "netsettlementheaders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_netsettlementdetails_creditorbic",
                table: "netsettlementdetails",
                column: "creditorbic");

            migrationBuilder.CreateIndex(
                name: "ix_netsettlementdetails_date",
                table: "netsettlementdetails",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "ix_netsettlementdetails_debtorbic",
                table: "netsettlementdetails",
                column: "debtorbic");

            migrationBuilder.CreateIndex(
                name: "ix_netsettlementdetails_isfee",
                table: "netsettlementdetails",
                column: "isfee");

            migrationBuilder.CreateIndex(
                name: "ix_netsettlementdetails_netsettlementheaderid",
                table: "netsettlementdetails",
                column: "netsettlementheaderid");

            migrationBuilder.CreateIndex(
                name: "ix_netsettlementheaders_creddttm",
                table: "netsettlementheaders",
                column: "creddttm");

            migrationBuilder.CreateIndex(
                name: "ix_netsettlementheaders_msgid",
                table: "netsettlementheaders",
                column: "msgid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "netsettlementdetails");

            migrationBuilder.DropTable(
                name: "netsettlementheaders");
        }
    }
}
