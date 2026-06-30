using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SPSS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFailedTransactionRuleDefinitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "failedtransactionruledefinitions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ruleid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    label = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_failedtransactionruledefinitions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_failedtransactionruledefinitions_active",
                table: "failedtransactionruledefinitions",
                column: "active");

            migrationBuilder.CreateIndex(
                name: "ix_failedtransactionruledefinitions_category",
                table: "failedtransactionruledefinitions",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "ix_failedtransactionruledefinitions_ruleid",
                table: "failedtransactionruledefinitions",
                column: "ruleid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "failedtransactionruledefinitions");
        }
    }
}
