using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SPSS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlignFailedTransactionContract : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_failedtransactions_ipstransactionid",
                table: "failedtransactions");

            migrationBuilder.DropIndex(
                name: "ix_failedtransactions_snapshotfromdate",
                table: "failedtransactions");

            migrationBuilder.DropIndex(
                name: "ix_failedtransactions_snapshottodate",
                table: "failedtransactions");

            migrationBuilder.Sql("UPDATE failedtransactions SET id = ipstransactionid;");

            migrationBuilder.AlterColumn<long>(
                name: "id",
                table: "failedtransactions",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.CreateTable(
                name: "failedtransactionruleassignments",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    failedtransactionid = table.Column<long>(type: "bigint", nullable: false),
                    ruleids = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_failedtransactionruleassignments", x => x.id);
                    table.ForeignKey(
                        name: "fk_failedtransactionruleassignments_failedtransactions_failedt~",
                        column: x => x.failedtransactionid,
                        principalTable: "failedtransactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_failedtransactionruleassignments_failedtransactionid",
                table: "failedtransactionruleassignments",
                column: "failedtransactionid",
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO failedtransactionruleassignments (failedtransactionid, ruleids)
                SELECT id, ruleids
                FROM failedtransactions
                WHERE cardinality(ruleids) > 0;
                """);

            migrationBuilder.DropColumn(
                name: "active",
                table: "failedtransactions");

            migrationBuilder.DropColumn(
                name: "ipstransactionid",
                table: "failedtransactions");

            migrationBuilder.DropColumn(
                name: "receivedat",
                table: "failedtransactions");

            migrationBuilder.DropColumn(
                name: "ruleids",
                table: "failedtransactions");

            migrationBuilder.DropColumn(
                name: "snapshotfromdate",
                table: "failedtransactions");

            migrationBuilder.DropColumn(
                name: "snapshottimestamp",
                table: "failedtransactions");

            migrationBuilder.DropColumn(
                name: "snapshottodate",
                table: "failedtransactions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "active",
                table: "failedtransactions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "ipstransactionid",
                table: "failedtransactions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "receivedat",
                table: "failedtransactions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<List<string>>(
                name: "ruleids",
                table: "failedtransactions",
                type: "text[]",
                nullable: false,
                defaultValue: new List<string>());

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "snapshotfromdate",
                table: "failedtransactions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "snapshottimestamp",
                table: "failedtransactions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "snapshottodate",
                table: "failedtransactions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.Sql(
                """
                UPDATE failedtransactions
                SET ipstransactionid = id,
                    ruleids = COALESCE(rule_assignments.ruleids, ARRAY[]::text[])
                FROM (
                    SELECT failedtransactionid, ruleids
                    FROM failedtransactionruleassignments
                ) AS rule_assignments
                WHERE failedtransactions.id = rule_assignments.failedtransactionid;
                """);

            migrationBuilder.Sql(
                """
                UPDATE failedtransactions
                SET ipstransactionid = id
                WHERE ipstransactionid = 0;
                """);

            migrationBuilder.DropTable(
                name: "failedtransactionruleassignments");

            migrationBuilder.AlterColumn<long>(
                name: "id",
                table: "failedtransactions",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.CreateIndex(
                name: "ix_failedtransactions_ipstransactionid",
                table: "failedtransactions",
                column: "ipstransactionid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_failedtransactions_snapshotfromdate",
                table: "failedtransactions",
                column: "snapshotfromdate");

            migrationBuilder.CreateIndex(
                name: "ix_failedtransactions_snapshottodate",
                table: "failedtransactions",
                column: "snapshottodate");
        }
    }
}
