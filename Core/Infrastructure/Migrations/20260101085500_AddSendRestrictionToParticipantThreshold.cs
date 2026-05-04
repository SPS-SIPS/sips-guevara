using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SPSS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSendRestrictionToParticipantThreshold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "enforcesendrestriction",
                table: "participantthresholds",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "minimumsendbalance",
                table: "participantthresholds",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "cansend",
                table: "participantthresholds",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "ix_participantthresholds_cansend",
                table: "participantthresholds",
                column: "cansend");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_participantthresholds_cansend",
                table: "participantthresholds");

            migrationBuilder.DropColumn(
                name: "enforcesendrestriction",
                table: "participantthresholds");

            migrationBuilder.DropColumn(
                name: "minimumsendbalance",
                table: "participantthresholds");

            migrationBuilder.DropColumn(
                name: "cansend",
                table: "participantthresholds");
        }
    }
}
