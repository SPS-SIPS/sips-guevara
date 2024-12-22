using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SPSS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedMonitoringSolution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "endpoints",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    participantid = table.Column<string>(type: "text", nullable: false),
                    date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    gateway = table.Column<string>(type: "text", nullable: false),
                    createdby = table.Column<string>(type: "text", nullable: false),
                    createdon = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    lastmodifiedby = table.Column<string>(type: "text", nullable: true),
                    lastmodifiedon = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false),
                    address = table.Column<string>(type: "text", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false)
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
                    date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    gateway = table.Column<string>(type: "text", nullable: false),
                    createdby = table.Column<string>(type: "text", nullable: false),
                    createdon = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    lastmodifiedby = table.Column<string>(type: "text", nullable: true),
                    lastmodifiedon = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false)
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
                    date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    gateway = table.Column<string>(type: "text", nullable: false),
                    createdby = table.Column<string>(type: "text", nullable: false),
                    createdon = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    lastmodifiedby = table.Column<string>(type: "text", nullable: true),
                    lastmodifiedon = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    endpoint = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "monithistories",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    participantid = table.Column<string>(type: "text", nullable: false),
                    date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    address = table.Column<string>(type: "text", nullable: false),
                    roundtriptime = table.Column<long>(type: "bigint", nullable: false),
                    packetloss = table.Column<int>(type: "integer", nullable: false),
                    additionalinfo = table.Column<string>(type: "text", nullable: true),
                    endpointid = table.Column<int>(type: "integer", nullable: true),
                    tunnelid = table.Column<int>(type: "integer", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_monithistories", x => x.id);
                    table.ForeignKey(
                        name: "fk_monithistories_endpoints_endpointid",
                        column: x => x.endpointid,
                        principalTable: "endpoints",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_monithistories_participants_participantid",
                        column: x => x.participantid,
                        principalTable: "participants",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_monithistories_tunnels_tunnelid",
                        column: x => x.tunnelid,
                        principalTable: "tunnels",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_endpoints_participantid",
                table: "endpoints",
                column: "participantid");

            migrationBuilder.CreateIndex(
                name: "ix_monithistories_endpointid",
                table: "monithistories",
                column: "endpointid");

            migrationBuilder.CreateIndex(
                name: "ix_monithistories_participantid",
                table: "monithistories",
                column: "participantid");

            migrationBuilder.CreateIndex(
                name: "ix_monithistories_tunnelid",
                table: "monithistories",
                column: "tunnelid");

            migrationBuilder.CreateIndex(
                name: "ix_tunnels_vpnid",
                table: "tunnels",
                column: "vpnid");

            migrationBuilder.CreateIndex(
                name: "ix_vpns_participantid",
                table: "vpns",
                column: "participantid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "monithistories");

            migrationBuilder.DropTable(
                name: "endpoints");

            migrationBuilder.DropTable(
                name: "tunnels");

            migrationBuilder.DropTable(
                name: "vpns");
        }
    }
}
