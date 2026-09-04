using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Falah.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCashCountAndSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CashDenominationCounts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BusinessDayId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ShiftId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalPiastres = table.Column<long>(type: "bigint", nullable: false),
                    ExpectedPiastres = table.Column<long>(type: "bigint", nullable: false),
                    DifferencePiastres = table.Column<long>(type: "bigint", nullable: false),
                    CountedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FinalizedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashDenominationCounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RestaurantName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone1 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone2 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneLandline = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ComplaintsPhone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReceiptFooter = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OvertimeHourlyRatePiastres = table.Column<long>(type: "bigint", nullable: false),
                    DeliveryFeePiastres = table.Column<long>(type: "bigint", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CashDiscrepancyThresholdPiastres = table.Column<long>(type: "bigint", nullable: false),
                    AutoLockMinutes = table.Column<int>(type: "int", nullable: false),
                    ActiveShiftName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrinterWidth = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AutoPrintReceipt = table.Column<bool>(type: "bit", nullable: false),
                    AutoPrintPreparationTicket = table.Column<bool>(type: "bit", nullable: false),
                    NumberOfReceiptCopies = table.Column<int>(type: "int", nullable: false),
                    GrillScreenSoundAlert = table.Column<bool>(type: "bit", nullable: false),
                    StandardWorkdayHours = table.Column<int>(type: "int", nullable: false),
                    LateGraceMinutes = table.Column<int>(type: "int", nullable: false),
                    MaxLatePenaltyHours = table.Column<int>(type: "int", nullable: false),
                    LatePenaltyMultiplier = table.Column<double>(type: "float", nullable: false),
                    OvertimeMultiplier = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashDenominationCounts_BusinessDayId",
                table: "CashDenominationCounts",
                column: "BusinessDayId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CashDenominationCounts");

            migrationBuilder.DropTable(
                name: "Settings");
        }
    }
}
