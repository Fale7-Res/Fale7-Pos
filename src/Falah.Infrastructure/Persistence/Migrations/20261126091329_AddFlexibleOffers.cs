using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Falah.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFlexibleOffers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "AmountOffPiastres",
                table: "PriceRules",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateOnly>(
                name: "EndDate",
                table: "PriceRules",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "GiftAutoAdd",
                table: "PriceRules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "GiftIncludesDelivery",
                table: "PriceRules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "PriceRules",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "ItemFixedPrice");

            migrationBuilder.AddColumn<int>(
                name: "MaxPerDay",
                table: "PriceRules",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxPerOrder",
                table: "PriceRules",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxTotal",
                table: "PriceRules",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrderTypes",
                table: "PriceRules",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PercentBasisPoints",
                table: "PriceRules",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "StartDate",
                table: "PriceRules",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Stations",
                table: "PriceRules",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PriceRuleCategories",
                columns: table => new
                {
                    PriceRuleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CategoryId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceRuleCategories", x => new { x.PriceRuleId, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_PriceRuleCategories_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PriceRuleCategories_PriceRules_PriceRuleId",
                        column: x => x.PriceRuleId,
                        principalTable: "PriceRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PriceRuleGiftTiers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PriceRuleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ThresholdPiastres = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    VariantId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceRuleGiftTiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceRuleGiftTiers_PriceRules_PriceRuleId",
                        column: x => x.PriceRuleId,
                        principalTable: "PriceRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PriceRuleGiftTiers_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PriceRuleCategories_CategoryId",
                table: "PriceRuleCategories",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceRuleGiftTiers_PriceRuleId",
                table: "PriceRuleGiftTiers",
                column: "PriceRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceRuleGiftTiers_ProductId",
                table: "PriceRuleGiftTiers",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PriceRuleCategories");

            migrationBuilder.DropTable(
                name: "PriceRuleGiftTiers");

            migrationBuilder.DropColumn(
                name: "AmountOffPiastres",
                table: "PriceRules");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "PriceRules");

            migrationBuilder.DropColumn(
                name: "GiftAutoAdd",
                table: "PriceRules");

            migrationBuilder.DropColumn(
                name: "GiftIncludesDelivery",
                table: "PriceRules");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "PriceRules");

            migrationBuilder.DropColumn(
                name: "MaxPerDay",
                table: "PriceRules");

            migrationBuilder.DropColumn(
                name: "MaxPerOrder",
                table: "PriceRules");

            migrationBuilder.DropColumn(
                name: "MaxTotal",
                table: "PriceRules");

            migrationBuilder.DropColumn(
                name: "OrderTypes",
                table: "PriceRules");

            migrationBuilder.DropColumn(
                name: "PercentBasisPoints",
                table: "PriceRules");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "PriceRules");

            migrationBuilder.DropColumn(
                name: "Stations",
                table: "PriceRules");
        }
    }
}
