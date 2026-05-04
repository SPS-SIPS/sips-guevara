using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SPSS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemovedUnneededMonitoringConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_monithistories_participants_participantid",
                table: "monithistories");

            migrationBuilder.DropTable(
                name: "endpoints");

            migrationBuilder.DropTable(
                name: "tunnels");

            migrationBuilder.DropTable(
                name: "vpns");

            migrationBuilder.DropIndex(
                name: "ix_monithistories_participantid",
                table: "monithistories");

            migrationBuilder.DropColumn(
                name: "packetloss",
                table: "monithistories");

            migrationBuilder.DropColumn(
                name: "roundtriptime",
                table: "monithistories");

            migrationBuilder.DropColumn(
                name: "status",
                table: "monithistories");

            migrationBuilder.RenameColumn(
                name: "type",
                table: "monithistories",
                newName: "httpstatus");

            migrationBuilder.RenameColumn(
                name: "sent",
                table: "monithistories",
                newName: "success");

            migrationBuilder.RenameColumn(
                name: "participantid",
                table: "monithistories",
                newName: "institutionname");

            migrationBuilder.RenameColumn(
                name: "date",
                table: "monithistories",
                newName: "checkedat");

            migrationBuilder.RenameColumn(
                name: "address",
                table: "monithistories",
                newName: "institutionbic");

            migrationBuilder.RenameColumn(
                name: "additionalinfo",
                table: "monithistories",
                newName: "response");

            migrationBuilder.AddColumn<string>(
                name: "error",
                table: "monithistories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "errortype",
                table: "monithistories",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "error",
                table: "monithistories");

            migrationBuilder.DropColumn(
                name: "errortype",
                table: "monithistories");

            migrationBuilder.RenameColumn(
                name: "success",
                table: "monithistories",
                newName: "sent");

            migrationBuilder.RenameColumn(
                name: "response",
                table: "monithistories",
                newName: "additionalinfo");

            migrationBuilder.RenameColumn(
                name: "institutionname",
                table: "monithistories",
                newName: "participantid");

            migrationBuilder.RenameColumn(
                name: "institutionbic",
                table: "monithistories",
                newName: "address");

            migrationBuilder.RenameColumn(
                name: "httpstatus",
                table: "monithistories",
                newName: "type");

            migrationBuilder.RenameColumn(
                name: "checkedat",
                table: "monithistories",
                newName: "date");

            migrationBuilder.AddColumn<int>(
                name: "packetloss",
                table: "monithistories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "roundtriptime",
                table: "monithistories",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "monithistories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "endpoints",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    participantid = table.Column<string>(type: "text", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    address = table.Column<string>(type: "text", nullable: false),
                    createdby = table.Column<string>(type: "text", nullable: false),
                    createdon = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    lastmodifiedby = table.Column<string>(type: "text", nullable: true),
                    lastmodifiedon = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_endpoints", x => x.id);
                    table.ForeignKey(
                        name: "fk_endpoints_participants_participantid",
                        column: x => x.participantid,
                        principalTable: "participants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vpns",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    participantid = table.Column<string>(type: "text", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    createdby = table.Column<string>(type: "text", nullable: false),
                    createdon = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    gateway = table.Column<string>(type: "text", nullable: false),
                    lastmodifiedby = table.Column<string>(type: "text", nullable: true),
                    lastmodifiedon = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vpns", x => x.id);
                    table.ForeignKey(
                        name: "fk_vpns_participants_participantid",
                        column: x => x.participantid,
                        principalTable: "participants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tunnels",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    vpnid = table.Column<int>(type: "integer", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    createdby = table.Column<string>(type: "text", nullable: false),
                    createdon = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    endpoint = table.Column<string>(type: "text", nullable: false),
                    lastmodifiedby = table.Column<string>(type: "text", nullable: true),
                    lastmodifiedon = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tunnels", x => x.id);
                    table.ForeignKey(
                        name: "fk_tunnels_vpns_vpnid",
                        column: x => x.vpnid,
                        principalTable: "vpns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_monithistories_participantid",
                table: "monithistories",
                column: "participantid");

            migrationBuilder.CreateIndex(
                name: "ix_endpoints_participantid",
                table: "endpoints",
                column: "participantid");

            migrationBuilder.CreateIndex(
                name: "ix_tunnels_vpnid",
                table: "tunnels",
                column: "vpnid");

            migrationBuilder.CreateIndex(
                name: "ix_vpns_participantid",
                table: "vpns",
                column: "participantid");

            migrationBuilder.AddForeignKey(
                name: "fk_monithistories_participants_participantid",
                table: "monithistories",
                column: "participantid",
                principalTable: "participants",
                principalColumn: "id");
        }
    }
}
