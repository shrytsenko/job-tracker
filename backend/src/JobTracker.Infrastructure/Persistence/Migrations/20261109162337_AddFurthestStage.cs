using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFurthestStage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FurthestStage",
                table: "Applications",
                type: "TEXT",
                maxLength: 30,
                nullable: true);

            // Backfill from the current status. For applications already closed (Rejected, …) the stage
            // they ended at was never recorded, so it stays unknown (null).
            migrationBuilder.Sql(
                """
                UPDATE "Applications" SET "FurthestStage" = "Status"
                WHERE "Status" IN ('Applied', 'RecruiterScreen', 'TechInterview', 'FinalInterview', 'Offer');
                UPDATE "Applications" SET "FurthestStage" = 'Offer' WHERE "Status" = 'Accepted';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FurthestStage",
                table: "Applications");
        }
    }
}
