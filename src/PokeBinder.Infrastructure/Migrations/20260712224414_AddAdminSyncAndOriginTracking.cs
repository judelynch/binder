using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokeBinder.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminSyncAndOriginTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Origin",
                table: "Sets",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "Synced");

            migrationBuilder.AddColumn<string>(
                name: "Origin",
                table: "Cards",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "Synced");

            migrationBuilder.CreateTable(
                name: "CardEditAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CardId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EditedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    EditedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ChangedFields = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardEditAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CardEditAudits_Cards_CardId",
                        column: x => x.CardId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SyncRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RunByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    RunByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SetsProcessed = table.Column<int>(type: "int", nullable: false),
                    TotalSets = table.Column<int>(type: "int", nullable: false),
                    CardsProcessed = table.Column<int>(type: "int", nullable: false),
                    SetsAdded = table.Column<int>(type: "int", nullable: false),
                    SetsUpdated = table.Column<int>(type: "int", nullable: false),
                    CardsAdded = table.Column<int>(type: "int", nullable: false),
                    CardsUpdated = table.Column<int>(type: "int", nullable: false),
                    ChangedFieldCounts = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RemainingManualConflicts = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncRuns", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CardEditAudits_CardId",
                table: "CardEditAudits",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_CardEditAudits_EditedAt",
                table: "CardEditAudits",
                column: "EditedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SyncRuns_StartedAt",
                table: "SyncRuns",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SyncRuns_Status",
                table: "SyncRuns",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CardEditAudits");

            migrationBuilder.DropTable(
                name: "SyncRuns");

            migrationBuilder.DropColumn(
                name: "Origin",
                table: "Sets");

            migrationBuilder.DropColumn(
                name: "Origin",
                table: "Cards");
        }
    }
}
