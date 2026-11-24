using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Falah.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFrozenFoodCost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FoodCostFrozenAtUtc",
                table: "Orders",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FrozenPackagingCostPiastres",
                table: "Orders",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FrozenPackagingMissing",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FrozenUnitFoodCostPiastres",
                table: "OrderItems",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FoodCostFrozenAtUtc",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "FrozenPackagingCostPiastres",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "FrozenPackagingMissing",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "FrozenUnitFoodCostPiastres",
                table: "OrderItems");
        }
    }
}
