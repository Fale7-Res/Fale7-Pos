using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Falah.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCashHandover : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CashHandovers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FromAccountId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ToAccountId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AmountPiastres = table.Column<long>(type: "bigint", nullable: false),
                    HandedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReceivedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShiftId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TreasuryMovementId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashHandovers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashHandovers_TreasuryMovementId",
                table: "CashHandovers",
                column: "TreasuryMovementId",
                unique: true,
                filter: "[TreasuryMovementId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CashHandovers");
        }
    }
}
