using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokeBinder.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCardDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Sets",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Series = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PrintedTotal = table.Column<int>(type: "int", nullable: false),
                    Total = table.Column<int>(type: "int", nullable: false),
                    ReleaseDate = table.Column<DateOnly>(type: "date", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PtcgoCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SymbolImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LogoImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Legalities = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VariantTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VariantTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cards",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SetId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Supertype = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Subtypes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Level = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Hp = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Types = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EvolvesFrom = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Abilities = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Attacks = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Weaknesses = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Resistances = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RetreatCost = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConvertedRetreatCost = table.Column<int>(type: "int", nullable: true),
                    Number = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NumberSortGroup = table.Column<byte>(type: "tinyint", nullable: false),
                    NumberSortPrefix = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    NumberSortValue = table.Column<int>(type: "int", nullable: false),
                    NumberSortSuffix = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Artist = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Rarity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FlavorText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RegulationMark = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Legalities = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageSmallUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImageLargeUrl = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cards_Sets_SetId",
                        column: x => x.SetId,
                        principalTable: "Sets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CardPokedexNumbers",
                columns: table => new
                {
                    CardId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardPokedexNumbers", x => new { x.CardId, x.Number });
                    table.ForeignKey(
                        name: "FK_CardPokedexNumbers_Cards_CardId",
                        column: x => x.CardId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CardVariants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CardId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VariantTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CardVariants_Cards_CardId",
                        column: x => x.CardId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CardVariants_VariantTypes_VariantTypeId",
                        column: x => x.VariantTypeId,
                        principalTable: "VariantTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CardPokedexNumbers_Number",
                table: "CardPokedexNumbers",
                column: "Number");

            migrationBuilder.CreateIndex(
                name: "IX_Card_Set_NumberSortKey",
                table: "Cards",
                columns: new[] { "SetId", "NumberSortGroup", "NumberSortPrefix", "NumberSortValue", "NumberSortSuffix" });

            migrationBuilder.CreateIndex(
                name: "IX_Cards_Artist",
                table: "Cards",
                column: "Artist");

            migrationBuilder.CreateIndex(
                name: "IX_Cards_Name",
                table: "Cards",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Cards_Rarity",
                table: "Cards",
                column: "Rarity");

            migrationBuilder.CreateIndex(
                name: "IX_Cards_RegulationMark",
                table: "Cards",
                column: "RegulationMark");

            migrationBuilder.CreateIndex(
                name: "IX_Cards_SetId",
                table: "Cards",
                column: "SetId");

            migrationBuilder.CreateIndex(
                name: "IX_Cards_Supertype",
                table: "Cards",
                column: "Supertype");

            migrationBuilder.CreateIndex(
                name: "IX_CardVariants_CardId_VariantTypeId",
                table: "CardVariants",
                columns: new[] { "CardId", "VariantTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CardVariants_VariantTypeId",
                table: "CardVariants",
                column: "VariantTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Sets_ReleaseDate",
                table: "Sets",
                column: "ReleaseDate");

            migrationBuilder.CreateIndex(
                name: "IX_VariantTypes_Name",
                table: "VariantTypes",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CardPokedexNumbers");

            migrationBuilder.DropTable(
                name: "CardVariants");

            migrationBuilder.DropTable(
                name: "Cards");

            migrationBuilder.DropTable(
                name: "VariantTypes");

            migrationBuilder.DropTable(
                name: "Sets");
        }
    }
}
