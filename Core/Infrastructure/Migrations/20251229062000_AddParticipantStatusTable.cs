using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SPSS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddParticipantStatusTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "participantstatuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    institutionbic = table.Column<string>(type: "text", nullable: false),
                    institutionname = table.Column<string>(type: "text", nullable: false),
                    islive = table.Column<bool>(type: "boolean", nullable: false),
                    lastcheckedat = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    laststatuschangeat = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    consecutivefailures = table.Column<int>(type: "integer", nullable: false),
                    consecutivesuccesses = table.Column<int>(type: "integer", nullable: false),
                    lasterror = table.Column<string>(type: "text", nullable: true),
                    updatedat = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_participantstatuses", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_participantstatuses_institutionbic",
                table: "participantstatuses",
                column: "institutionbic",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_participantstatuses_islive",
                table: "participantstatuses",
                column: "islive");

            migrationBuilder.CreateIndex(
                name: "ix_participantstatuses_lastcheckedat",
                table: "participantstatuses",
                column: "lastcheckedat");

            migrationBuilder.CreateIndex(
                name: "ix_participantstatuses_updatedat",
                table: "participantstatuses",
                column: "updatedat");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "participantstatuses");
        }
    }
}
