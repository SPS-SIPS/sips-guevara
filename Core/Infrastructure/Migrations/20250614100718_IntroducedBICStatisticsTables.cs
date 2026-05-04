using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SPSS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IntroducedBICStatisticsTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bicstatistics",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    netsettlementheaderid = table.Column<long>(type: "bigint", nullable: false),
                    bic = table.Column<string>(type: "text", nullable: false),
                    sentcount = table.Column<long>(type: "bigint", nullable: false),
                    totalamountsent = table.Column<decimal>(type: "numeric", nullable: false),
                    receivedcount = table.Column<long>(type: "bigint", nullable: false),
                    totalamountreceived = table.Column<decimal>(type: "numeric", nullable: false),
                    totalfeesreceived = table.Column<decimal>(type: "numeric", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bicstatistics", x => x.id);
                    table.ForeignKey(
                        name: "fk_bicstatistics_netsettlementheaders_netsettlementheaderid",
                        column: x => x.netsettlementheaderid,
                        principalTable: "netsettlementheaders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bicstatistics_topreceivers",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    bic = table.Column<string>(type: "text", nullable: false),
                    transactioncount = table.Column<long>(type: "bigint", nullable: false),
                    totalamount = table.Column<decimal>(type: "numeric", nullable: false),
                    bicstatisticid = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bicstatistics_topreceivers", x => x.id);
                    table.ForeignKey(
                        name: "fk_bicstatistics_topreceivers_bicstatistics_bicstatisticid",
                        column: x => x.bicstatisticid,
                        principalTable: "bicstatistics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bicstatistics_topsenders",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    bic = table.Column<string>(type: "text", nullable: false),
                    transactioncount = table.Column<long>(type: "bigint", nullable: false),
                    totalamount = table.Column<decimal>(type: "numeric", nullable: false),
                    bicstatisticid = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bicstatistics_topsenders", x => x.id);
                    table.ForeignKey(
                        name: "fk_bicstatistics_topsenders_bicstatistics_bicstatisticid",
                        column: x => x.bicstatisticid,
                        principalTable: "bicstatistics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bicstatistics_bic",
                table: "bicstatistics",
                column: "bic");

            migrationBuilder.CreateIndex(
                name: "ix_bicstatistics_netsettlementheaderid",
                table: "bicstatistics",
                column: "netsettlementheaderid");

            migrationBuilder.CreateIndex(
                name: "ix_bicstatistics_topreceivers_bicstatisticid",
                table: "bicstatistics_topreceivers",
                column: "bicstatisticid");

            migrationBuilder.CreateIndex(
                name: "ix_bicstatistics_topsenders_bicstatisticid",
                table: "bicstatistics_topsenders",
                column: "bicstatisticid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bicstatistics_topreceivers");

            migrationBuilder.DropTable(
                name: "bicstatistics_topsenders");

            migrationBuilder.DropTable(
                name: "bicstatistics");
        }
    }
}
