using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Falah.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscountLimitAndReportingIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxDiscountWithoutApprovalBasisPoints",
                table: "Settings",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TreasuryMovements_CreatedAtUtc",
                table: "TreasuryMovements",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PrintJobAttempts_AttemptedAtUtc",
                table: "PrintJobAttempts",
                column: "AttemptedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CreatedAtUtc",
                table: "Orders",
                column: "CreatedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TreasuryMovements_CreatedAtUtc",
                table: "TreasuryMovements");

            migrationBuilder.DropIndex(
                name: "IX_PrintJobAttempts_AttemptedAtUtc",
                table: "PrintJobAttempts");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CreatedAtUtc",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "MaxDiscountWithoutApprovalBasisPoints",
                table: "Settings");
        }
    }
}
