using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Falah.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovedBy",
                table: "StockMovements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Direction",
                table: "StockMovements",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "LossValuePiastres",
                table: "StockMovements",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperationId",
                table: "StockMovements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "UnitCostPiastres",
                table: "StockMovements",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Active",
                table: "InventoryItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "InventoryItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsPackaging",
                table: "InventoryItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "MinimumMilliUnits",
                table: "InventoryItems",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "StandardDailyUsageMilliUnits",
                table: "InventoryItems",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "InventoryItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "OrderPackagingRules",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OrderType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InventoryItemId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    QuantityMilliUnits = table.Column<long>(type: "bigint", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderPackagingRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderPackagingRules_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrepRecipes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OutputInventoryItemId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OutputQuantityMilliUnits = table.Column<long>(type: "bigint", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrepRecipes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrepRecipes_InventoryItems_OutputInventoryItemId",
                        column: x => x.OutputInventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrepRecipeLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PrepRecipeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    InventoryItemId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    QuantityMilliUnits = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrepRecipeLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrepRecipeLines_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrepRecipeLines_PrepRecipes_PrepRecipeId",
                        column: x => x.PrepRecipeId,
                        principalTable: "PrepRecipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderPackagingRules_InventoryItemId",
                table: "OrderPackagingRules",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PrepRecipeLines_InventoryItemId",
                table: "PrepRecipeLines",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PrepRecipeLines_PrepRecipeId",
                table: "PrepRecipeLines",
                column: "PrepRecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_PrepRecipes_OutputInventoryItemId",
                table: "PrepRecipes",
                column: "OutputInventoryItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderPackagingRules");

            migrationBuilder.DropTable(
                name: "PrepRecipeLines");

            migrationBuilder.DropTable(
                name: "PrepRecipes");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "Direction",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "LossValuePiastres",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "OperationId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "UnitCostPiastres",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "Active",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "IsPackaging",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "MinimumMilliUnits",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "StandardDailyUsageMilliUnits",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "InventoryItems");
        }
    }
}
