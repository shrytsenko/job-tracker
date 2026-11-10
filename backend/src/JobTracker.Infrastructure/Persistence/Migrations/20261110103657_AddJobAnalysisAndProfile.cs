using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddJobAnalysisAndProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JobDescription",
                table: "Applications",
                type: "character varying(30000)",
                maxLength: 30000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "JobAnalyses",
                columns: table => new
                {
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Stack = table.Column<string[]>(type: "text[]", nullable: false),
                    Seniority = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    LocationRestrictions = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    EmploymentType = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Salary = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    MatchScore = table.Column<int>(type: "integer", nullable: false),
                    Strengths = table.Column<string[]>(type: "text[]", nullable: false),
                    Gaps = table.Column<string[]>(type: "text[]", nullable: false),
                    RedFlags = table.Column<string[]>(type: "text[]", nullable: false),
                    Model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AnalyzedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobAnalyses", x => x.ApplicationId);
                    table.ForeignKey(
                        name: "FK_JobAnalyses_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Profiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    CvSummary = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profiles", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobAnalyses");

            migrationBuilder.DropTable(
                name: "Profiles");

            migrationBuilder.DropColumn(
                name: "JobDescription",
                table: "Applications");
        }
    }
}
