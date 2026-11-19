using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Falah.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDriverFeeSettlementAndShiftHandover : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "HandedOverPiastres",
                table: "Shifts",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HandoverReceiverName",
                table: "Shifts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HandoverReceiverUserId",
                table: "Shifts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "InheritedFloatPiastres",
                table: "Shifts",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "RetainedFloatPiastres",
                table: "Shifts",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CarriedInPiastres",
                table: "DriverSettlements",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "CarriedOutPiastres",
                table: "DriverSettlements",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "CashCollectedPiastres",
                table: "DriverSettlements",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "FeeEntitlementPiastres",
                table: "DriverSettlements",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<bool>(
                name: "KeepsBalanceOpen",
                table: "DriverSettlements",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "PaidToDriverPiastres",
                table: "DriverSettlements",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "PayoutTreasuryMovementId",
                table: "DriverSettlements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceivedByUserId",
                table: "DriverSettlements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShiftId",
                table: "DriverSettlements",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HandedOverPiastres",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "HandoverReceiverName",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "HandoverReceiverUserId",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "InheritedFloatPiastres",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "RetainedFloatPiastres",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "CarriedInPiastres",
                table: "DriverSettlements");

            migrationBuilder.DropColumn(
                name: "CarriedOutPiastres",
                table: "DriverSettlements");

            migrationBuilder.DropColumn(
                name: "CashCollectedPiastres",
                table: "DriverSettlements");

            migrationBuilder.DropColumn(
                name: "FeeEntitlementPiastres",
                table: "DriverSettlements");

            migrationBuilder.DropColumn(
                name: "KeepsBalanceOpen",
                table: "DriverSettlements");

            migrationBuilder.DropColumn(
                name: "PaidToDriverPiastres",
                table: "DriverSettlements");

            migrationBuilder.DropColumn(
                name: "PayoutTreasuryMovementId",
                table: "DriverSettlements");

            migrationBuilder.DropColumn(
                name: "ReceivedByUserId",
                table: "DriverSettlements");

            migrationBuilder.DropColumn(
                name: "ShiftId",
                table: "DriverSettlements");
        }
    }
}
