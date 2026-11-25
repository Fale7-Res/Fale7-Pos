using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Falah.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPickupHandover : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "HandedOverAtUtc",
                table: "Orders",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HandedOverBy",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HandoverRequestId",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_HandoverRequestId",
                table: "Orders",
                column: "HandoverRequestId",
                unique: true,
                filter: "[HandoverRequestId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_HandoverRequestId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "HandedOverAtUtc",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "HandedOverBy",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "HandoverRequestId",
                table: "Orders");
        }
    }
}
