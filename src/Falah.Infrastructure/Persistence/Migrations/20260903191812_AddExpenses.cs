using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Falah.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Expenses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BusinessDayId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AmountPiastres = table.Column<long>(type: "bigint", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShiftId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PaidFromDrawer = table.Column<bool>(type: "bit", nullable: false),
                    PaymentAccountId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OperationId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TreasuryMovementId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expenses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_BusinessDate",
                table: "Expenses",
                column: "BusinessDate");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_IdempotencyKey",
                table: "Expenses",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_ShiftId_PaidFromDrawer",
                table: "Expenses",
                columns: new[] { "ShiftId", "PaidFromDrawer" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Expenses");
        }
    }
}
