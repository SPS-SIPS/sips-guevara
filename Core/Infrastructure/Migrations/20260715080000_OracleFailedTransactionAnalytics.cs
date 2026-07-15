using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SPSS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OracleFailedTransactionAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "oraclefailedtransactionanalyticsruns",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    windowstart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    windowend = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    generatedat = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    totaltransactions = table.Column<int>(type: "integer", nullable: false),
                    totalamount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    uniquesendinginstitutions = table.Column<int>(type: "integer", nullable: false),
                    uniquereceivinginstitutions = table.Column<int>(type: "integer", nullable: false),
                    uniqueaccounts = table.Column<int>(type: "integer", nullable: false),
                    errormessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_oraclefailedtransactionanalyticsruns", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "oraclefailedinstitutionanalytics",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    runid = table.Column<long>(type: "bigint", nullable: false),
                    sendinginstitution = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    receivinginstitution = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    transactioncount = table.Column<int>(type: "integer", nullable: false),
                    totalamount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    distinctdebtoraccounts = table.Column<int>(type: "integer", nullable: false),
                    topfailurecode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    topfailurereason = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    firsttransactionat = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lasttransactionat = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_oraclefailedinstitutionanalytics", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "oraclefailedaccountanalytics",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    runid = table.Column<long>(type: "bigint", nullable: false),
                    institutionbic = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    receivinginstitution = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    accountid = table.Column<string>(type: "text", nullable: false),
                    transactioncount = table.Column<int>(type: "integer", nullable: false),
                    totalamount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    topfailurecode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    topfailurereason = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    firsttransactionat = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lasttransactionat = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_oraclefailedaccountanalytics", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "oraclefailedrejectiontypeanalytics",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    runid = table.Column<long>(type: "bigint", nullable: false),
                    sendinginstitution = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    receivinginstitution = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    failurecode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    failurereason = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    transactioncount = table.Column<int>(type: "integer", nullable: false),
                    totalamount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_oraclefailedrejectiontypeanalytics", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "oraclefailedtrendanalytics",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    runid = table.Column<long>(type: "bigint", nullable: false),
                    bucketstart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    buckettype = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    sendinginstitution = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    receivinginstitution = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    failurecode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    transactioncount = table.Column<int>(type: "integer", nullable: false),
                    totalamount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_oraclefailedtrendanalytics", x => x.id);
                });

            migrationBuilder.CreateIndex(name: "ix_oraclefailedtransactionanalyticsruns_generatedat", table: "oraclefailedtransactionanalyticsruns", column: "generatedat");
            migrationBuilder.CreateIndex(name: "ix_oraclefailedtransactionanalyticsruns_status", table: "oraclefailedtransactionanalyticsruns", column: "status");
            migrationBuilder.CreateIndex(name: "ix_oraclefailedtransactionanalyticsruns_windowstart_windowend", table: "oraclefailedtransactionanalyticsruns", columns: new[] { "windowstart", "windowend" });

            migrationBuilder.CreateIndex(name: "ix_oraclefailedinstitutionanalytics_runid", table: "oraclefailedinstitutionanalytics", column: "runid");
            migrationBuilder.CreateIndex(name: "ix_oraclefailedinstitutionanalytics_sendinginstitution", table: "oraclefailedinstitutionanalytics", column: "sendinginstitution");
            migrationBuilder.CreateIndex(name: "ix_oraclefailedinstitutionanalytics_receivinginstitution", table: "oraclefailedinstitutionanalytics", column: "receivinginstitution");

            migrationBuilder.CreateIndex(name: "ix_oraclefailedaccountanalytics_runid", table: "oraclefailedaccountanalytics", column: "runid");
            migrationBuilder.CreateIndex(name: "ix_oraclefailedaccountanalytics_institutionbic", table: "oraclefailedaccountanalytics", column: "institutionbic");
            migrationBuilder.CreateIndex(name: "ix_oraclefailedaccountanalytics_receivinginstitution", table: "oraclefailedaccountanalytics", column: "receivinginstitution");
            migrationBuilder.CreateIndex(name: "ix_oraclefailedaccountanalytics_accountid", table: "oraclefailedaccountanalytics", column: "accountid");

            migrationBuilder.CreateIndex(name: "ix_oraclefailedrejectiontypeanalytics_runid", table: "oraclefailedrejectiontypeanalytics", column: "runid");
            migrationBuilder.CreateIndex(name: "ix_oraclefailedrejectiontypeanalytics_sendinginstitution", table: "oraclefailedrejectiontypeanalytics", column: "sendinginstitution");
            migrationBuilder.CreateIndex(name: "ix_oraclefailedrejectiontypeanalytics_receivinginstitution", table: "oraclefailedrejectiontypeanalytics", column: "receivinginstitution");
            migrationBuilder.CreateIndex(name: "ix_oraclefailedrejectiontypeanalytics_failurecode", table: "oraclefailedrejectiontypeanalytics", column: "failurecode");

            migrationBuilder.CreateIndex(name: "ix_oraclefailedtrendanalytics_runid", table: "oraclefailedtrendanalytics", column: "runid");
            migrationBuilder.CreateIndex(name: "ix_oraclefailedtrendanalytics_bucketstart", table: "oraclefailedtrendanalytics", column: "bucketstart");
            migrationBuilder.CreateIndex(name: "ix_oraclefailedtrendanalytics_sendinginstitution", table: "oraclefailedtrendanalytics", column: "sendinginstitution");
            migrationBuilder.CreateIndex(name: "ix_oraclefailedtrendanalytics_receivinginstitution", table: "oraclefailedtrendanalytics", column: "receivinginstitution");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "oraclefailedtrendanalytics");
            migrationBuilder.DropTable(name: "oraclefailedrejectiontypeanalytics");
            migrationBuilder.DropTable(name: "oraclefailedaccountanalytics");
            migrationBuilder.DropTable(name: "oraclefailedinstitutionanalytics");
            migrationBuilder.DropTable(name: "oraclefailedtransactionanalyticsruns");
        }
    }
}
