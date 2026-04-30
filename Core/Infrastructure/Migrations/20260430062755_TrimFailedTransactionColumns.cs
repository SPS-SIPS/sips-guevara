using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SPSS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TrimFailedTransactionColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "aliasfrom",
                table: "failedtransactions");

            migrationBuilder.DropColumn(
                name: "aliasto",
                table: "failedtransactions");

            migrationBuilder.DropColumn(
                name: "aliastypefrom",
                table: "failedtransactions");

            migrationBuilder.DropColumn(
                name: "aliastypeto",
                table: "failedtransactions");

            migrationBuilder.DropColumn(
                name: "intrbksttlmcurr",
                table: "failedtransactions");

            migrationBuilder.DropColumn(
                name: "lastmodifieddate",
                table: "failedtransactions");

            migrationBuilder.DropColumn(
                name: "messagesjson",
                table: "failedtransactions");

            migrationBuilder.DropColumn(
                name: "newbalance",
                table: "failedtransactions");

            migrationBuilder.DropColumn(
                name: "oldbalance",
                table: "failedtransactions");

            migrationBuilder.DropColumn(
                name: "rawtransactionjson",
                table: "failedtransactions");

            migrationBuilder.DropColumn(
                name: "transactionsjson",
                table: "failedtransactions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "aliasfrom",
                table: "failedtransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "aliasto",
                table: "failedtransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "aliastypefrom",
                table: "failedtransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "aliastypeto",
                table: "failedtransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "intrbksttlmcurr",
                table: "failedtransactions",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "lastmodifieddate",
                table: "failedtransactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "messagesjson",
                table: "failedtransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "newbalance",
                table: "failedtransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "oldbalance",
                table: "failedtransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rawtransactionjson",
                table: "failedtransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "transactionsjson",
                table: "failedtransactions",
                type: "text",
                nullable: true);
        }
    }
}
