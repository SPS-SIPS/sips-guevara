using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SPSS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OracleFailedTransactionAnalyticsClassification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "rejectioncategory",
                table: "oraclefailedtrendanalytics",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "rejectioncategory",
                table: "oraclefailedrejectiontypeanalytics",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_oraclefailedtrendanalytics_rejectioncategory",
                table: "oraclefailedtrendanalytics",
                column: "rejectioncategory");

            migrationBuilder.CreateIndex(
                name: "ix_oraclefailedrejectiontypeanalytics_rejectioncategory",
                table: "oraclefailedrejectiontypeanalytics",
                column: "rejectioncategory");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_oraclefailedtrendanalytics_rejectioncategory",
                table: "oraclefailedtrendanalytics");

            migrationBuilder.DropIndex(
                name: "ix_oraclefailedrejectiontypeanalytics_rejectioncategory",
                table: "oraclefailedrejectiontypeanalytics");

            migrationBuilder.DropColumn(
                name: "rejectioncategory",
                table: "oraclefailedtrendanalytics");

            migrationBuilder.DropColumn(
                name: "rejectioncategory",
                table: "oraclefailedrejectiontypeanalytics");
        }
    }
}
