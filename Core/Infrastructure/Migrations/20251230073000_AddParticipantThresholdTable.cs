using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SPSS.Infrastructure.Migrations
{
    public partial class AddParticipantThresholdTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "participantthresholds",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    bic = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    goldenamount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    amberpercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    redpercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    lastknownzone = table.Column<int>(type: "integer", nullable: false),
                    lastknownbalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    lastzonechangeat = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lastalertsentat = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    alertrecipients = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_participantthresholds", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_participantthresholds_bic",
                table: "participantthresholds",
                column: "bic",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_participantthresholds_lastknownzone",
                table: "participantthresholds",
                column: "lastknownzone");

            migrationBuilder.CreateIndex(
                name: "ix_participantthresholds_lastzonechangeat",
                table: "participantthresholds",
                column: "lastzonechangeat");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "participantthresholds");
        }
    }
}
