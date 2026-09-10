using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Falah.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEstimatedUnitCost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "EstimatedUnitCostPiastres",
                table: "InventoryItems",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstimatedUnitCostPiastres",
                table: "InventoryItems");
        }
    }
}
