using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Falah.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftCloseIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CloseRequestId",
                table: "Shifts",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HandoverTreasuryMovementId",
                table: "Shifts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReconciliationTreasuryMovementId",
                table: "Shifts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_CloseRequestId",
                table: "Shifts",
                column: "CloseRequestId",
                unique: true,
                filter: "[CloseRequestId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Shifts_CloseRequestId",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "CloseRequestId",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "HandoverTreasuryMovementId",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "ReconciliationTreasuryMovementId",
                table: "Shifts");
        }
    }
}
