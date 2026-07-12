using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokeBinder.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCardFilterIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HpValue",
                table: "Cards",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CardResistanceTypes",
                columns: table => new
                {
                    CardId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardResistanceTypes", x => new { x.CardId, x.Type });
                    table.ForeignKey(
                        name: "FK_CardResistanceTypes_Cards_CardId",
                        column: x => x.CardId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CardSubtypes",
                columns: table => new
                {
                    CardId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Subtype = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardSubtypes", x => new { x.CardId, x.Subtype });
                    table.ForeignKey(
                        name: "FK_CardSubtypes_Cards_CardId",
                        column: x => x.CardId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CardTypes",
                columns: table => new
                {
                    CardId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardTypes", x => new { x.CardId, x.Type });
                    table.ForeignKey(
                        name: "FK_CardTypes_Cards_CardId",
                        column: x => x.CardId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CardWeaknessTypes",
                columns: table => new
                {
                    CardId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardWeaknessTypes", x => new { x.CardId, x.Type });
                    table.ForeignKey(
                        name: "FK_CardWeaknessTypes_Cards_CardId",
                        column: x => x.CardId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cards_ConvertedRetreatCost",
                table: "Cards",
                column: "ConvertedRetreatCost");

            migrationBuilder.CreateIndex(
                name: "IX_Cards_HpValue",
                table: "Cards",
                column: "HpValue");

            migrationBuilder.CreateIndex(
                name: "IX_CardResistanceTypes_Type",
                table: "CardResistanceTypes",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_CardSubtypes_Subtype",
                table: "CardSubtypes",
                column: "Subtype");

            migrationBuilder.CreateIndex(
                name: "IX_CardTypes_Type",
                table: "CardTypes",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_CardWeaknessTypes_Type",
                table: "CardWeaknessTypes",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CardResistanceTypes");

            migrationBuilder.DropTable(
                name: "CardSubtypes");

            migrationBuilder.DropTable(
                name: "CardTypes");

            migrationBuilder.DropTable(
                name: "CardWeaknessTypes");

            migrationBuilder.DropIndex(
                name: "IX_Cards_ConvertedRetreatCost",
                table: "Cards");

            migrationBuilder.DropIndex(
                name: "IX_Cards_HpValue",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "HpValue",
                table: "Cards");
        }
    }
}
