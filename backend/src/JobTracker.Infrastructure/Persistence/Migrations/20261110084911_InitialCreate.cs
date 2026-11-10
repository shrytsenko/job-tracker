using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Applications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Company = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RoleTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    JobUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CvVersion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Track = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Channel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    EmploymentType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    FurthestStage = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    RemoteRegion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    HybridCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CompensationAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    CompensationCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    CompensationPeriod = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    CompensationIsNet = table.Column<bool>(type: "boolean", nullable: true),
                    AppliedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    LastContactOn = table.Column<DateOnly>(type: "date", nullable: true),
                    NextActionDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    NextActionDueOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Contacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Role = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Company = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LinkedInUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    LastContactOn = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Activities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Activities_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationContacts",
                columns: table => new
                {
                    ApplicationsId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactsId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationContacts", x => new { x.ApplicationsId, x.ContactsId });
                    table.ForeignKey(
                        name: "FK_ApplicationContacts_Applications_ApplicationsId",
                        column: x => x.ApplicationsId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApplicationContacts_Contacts_ContactsId",
                        column: x => x.ContactsId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_ApplicationId",
                table: "Activities",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationContacts_ContactsId",
                table: "ApplicationContacts",
                column: "ContactsId");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_Status",
                table: "Applications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_Track",
                table: "Applications",
                column: "Track");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Activities");

            migrationBuilder.DropTable(
                name: "ApplicationContacts");

            migrationBuilder.DropTable(
                name: "Applications");

            migrationBuilder.DropTable(
                name: "Contacts");
        }
    }
}
