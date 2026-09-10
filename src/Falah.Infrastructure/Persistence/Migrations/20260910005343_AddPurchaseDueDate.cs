using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Falah.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseDueDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "DueDate",
                table: "Purchases",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "Purchases");
        }
    }
}
