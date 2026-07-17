using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokeBinder.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CardVariantScrapeStates",
                columns: table => new
                {
                    CardVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastScrapedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ScrapePriority = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardVariantScrapeStates", x => x.CardVariantId);
                    table.ForeignKey(
                        name: "FK_CardVariantScrapeStates_CardVariants_CardVariantId",
                        column: x => x.CardVariantId,
                        principalTable: "CardVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PricePoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CardVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GradedStatus = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Grader = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Grade = table.Column<decimal>(type: "decimal(4,1)", nullable: true),
                    Condition = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    WindowDays = table.Column<int>(type: "int", nullable: false),
                    ItemOnlyMedianGbp = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    DeliveredMedianGbp = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    SampleCount = table.Column<int>(type: "int", nullable: false),
                    MinGbp = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    MaxGbp = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    LastSaleDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    QuarantinedReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AggregatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PricePoints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RawListings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CardVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Query = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ListingId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SourceProvider = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ItemPriceGbp = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PostagePriceGbp = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    SoldDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ListingFormat = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FetchedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RawListings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScrapeRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TriggeredBy = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TriggeredByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CardsProcessed = table.Column<int>(type: "int", nullable: false),
                    ListingsFound = table.Column<int>(type: "int", nullable: false),
                    ListingsAccepted = table.Column<int>(type: "int", nullable: false),
                    ListingsQuarantined = table.Column<int>(type: "int", nullable: false),
                    ListingsRejected = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScrapeRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ListingClassifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RawListingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResolvedCardVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdentityMatchStrong = table.Column<bool>(type: "bit", nullable: false),
                    GradedStatus = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Grader = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Grade = table.Column<decimal>(type: "decimal(4,1)", nullable: true),
                    RawCondition = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    VariantMatch = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Language = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BestOfferAccepted = table.Column<bool>(type: "bit", nullable: false),
                    KillReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ConfidenceScore = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ClassifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListingClassifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ListingClassifications_RawListings_RawListingId",
                        column: x => x.RawListingId,
                        principalTable: "RawListings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClassificationFeedbacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ListingClassificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalGuessJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorrectedValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReviewedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassificationFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassificationFeedbacks_ListingClassifications_ListingClassificationId",
                        column: x => x.ListingClassificationId,
                        principalTable: "ListingClassifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CardVariantScrapeStates_ScrapePriority_LastScrapedAt",
                table: "CardVariantScrapeStates",
                columns: new[] { "ScrapePriority", "LastScrapedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ClassificationFeedbacks_ListingClassificationId",
                table: "ClassificationFeedbacks",
                column: "ListingClassificationId");

            migrationBuilder.CreateIndex(
                name: "IX_ListingClassifications_RawListingId",
                table: "ListingClassifications",
                column: "RawListingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ListingClassifications_ResolvedCardVariantId",
                table: "ListingClassifications",
                column: "ResolvedCardVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_ListingClassifications_Status",
                table: "ListingClassifications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PricePoints_Graded_Bucket",
                table: "PricePoints",
                columns: new[] { "CardVariantId", "Grader", "Grade", "WindowDays" },
                unique: true,
                filter: "[GradedStatus] = 'Graded'");

            migrationBuilder.CreateIndex(
                name: "IX_PricePoints_Raw_Bucket",
                table: "PricePoints",
                columns: new[] { "CardVariantId", "Condition", "WindowDays" },
                unique: true,
                filter: "[GradedStatus] = 'Raw'");

            migrationBuilder.CreateIndex(
                name: "IX_RawListings_CardVariantId",
                table: "RawListings",
                column: "CardVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_RawListings_ListingId_SourceProvider",
                table: "RawListings",
                columns: new[] { "ListingId", "SourceProvider" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScrapeRuns_StartedAt",
                table: "ScrapeRuns",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ScrapeRuns_Status",
                table: "ScrapeRuns",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CardVariantScrapeStates");

            migrationBuilder.DropTable(
                name: "ClassificationFeedbacks");

            migrationBuilder.DropTable(
                name: "PricePoints");

            migrationBuilder.DropTable(
                name: "ScrapeRuns");

            migrationBuilder.DropTable(
                name: "ListingClassifications");

            migrationBuilder.DropTable(
                name: "RawListings");
        }
    }
}
