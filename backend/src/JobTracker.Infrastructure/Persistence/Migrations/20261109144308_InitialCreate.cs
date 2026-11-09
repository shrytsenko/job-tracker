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
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Company = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    RoleTitle = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    JobUrl = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Track = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Channel = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    EmploymentType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    RemoteRegion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    HybridCity = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CompensationAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    CompensationCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    CompensationPeriod = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    CompensationIsNet = table.Column<bool>(type: "INTEGER", nullable: true),
                    AppliedOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    LastContactOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    NextActionDescription = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    NextActionDueOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Contacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Company = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    LinkedInUrl = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 320, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    LastContactOn = table.Column<DateOnly>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Activities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Text = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false)
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
                    ApplicationsId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContactsId = table.Column<Guid>(type: "TEXT", nullable: false)
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
