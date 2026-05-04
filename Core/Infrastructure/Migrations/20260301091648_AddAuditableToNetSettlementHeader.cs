using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SPSS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditableToNetSettlementHeader : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "createdby",
                table: "netsettlementheaders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "createdon",
                table: "netsettlementheaders",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "lastmodifiedby",
                table: "netsettlementheaders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "lastmodifiedon",
                table: "netsettlementheaders",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "createdby",
                table: "netsettlementheaders");

            migrationBuilder.DropColumn(
                name: "createdon",
                table: "netsettlementheaders");

            migrationBuilder.DropColumn(
                name: "lastmodifiedby",
                table: "netsettlementheaders");

            migrationBuilder.DropColumn(
                name: "lastmodifiedon",
                table: "netsettlementheaders");
        }
    }
}
