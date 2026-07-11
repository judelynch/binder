using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokeBinder.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBinderDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Binders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ColourHex = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    Rows = table.Column<int>(type: "int", nullable: false),
                    Columns = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastAccessedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Binders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OverlayTags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ColourHex = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OverlayTags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BinderPages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BinderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PageNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BinderPages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BinderPages_Binders_BinderId",
                        column: x => x.BinderId,
                        principalTable: "Binders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BinderSlots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    CardVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Owned = table.Column<bool>(type: "bit", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: true),
                    Condition = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    OverlayTagId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BinderSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BinderSlots_BinderPages_PageId",
                        column: x => x.PageId,
                        principalTable: "BinderPages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BinderSlots_CardVariants_CardVariantId",
                        column: x => x.CardVariantId,
                        principalTable: "CardVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BinderSlots_OverlayTags_OverlayTagId",
                        column: x => x.OverlayTagId,
                        principalTable: "OverlayTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BinderPages_BinderId_PageNumber",
                table: "BinderPages",
                columns: new[] { "BinderId", "PageNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Binders_OwnerId",
                table: "Binders",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Binders_OwnerId_LastAccessedAt",
                table: "Binders",
                columns: new[] { "OwnerId", "LastAccessedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BinderSlots_CardVariantId",
                table: "BinderSlots",
                column: "CardVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_BinderSlots_OverlayTagId",
                table: "BinderSlots",
                column: "OverlayTagId");

            migrationBuilder.CreateIndex(
                name: "IX_BinderSlots_PageId_Position",
                table: "BinderSlots",
                columns: new[] { "PageId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OverlayTags_OwnerId_Name",
                table: "OverlayTags",
                columns: new[] { "OwnerId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BinderSlots");

            migrationBuilder.DropTable(
                name: "BinderPages");

            migrationBuilder.DropTable(
                name: "OverlayTags");

            migrationBuilder.DropTable(
                name: "Binders");
        }
    }
}
