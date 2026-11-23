using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Falah.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPickupPayOnCollection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AwaitingCollection",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CollectedBy",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CollectionRequestId",
                table: "Orders",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RegisteredAtUtc",
                table: "Orders",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegisteredShiftId",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CollectionRequestId",
                table: "Orders",
                column: "CollectionRequestId",
                unique: true,
                filter: "[CollectionRequestId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_CollectionRequestId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "AwaitingCollection",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CollectedBy",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CollectionRequestId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RegisteredAtUtc",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RegisteredShiftId",
                table: "Orders");
        }
    }
}
