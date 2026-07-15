using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokeBinder.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCardOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CardOwnerships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CardVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Condition = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardOwnerships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CardOwnerships_CardVariants_CardVariantId",
                        column: x => x.CardVariantId,
                        principalTable: "CardVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CardOwnerships_CardVariantId",
                table: "CardOwnerships",
                column: "CardVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_CardOwnerships_UserId_CardVariantId",
                table: "CardOwnerships",
                columns: new[] { "UserId", "CardVariantId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CardOwnerships");
        }
    }
}
