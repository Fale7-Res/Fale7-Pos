using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Falah.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeliveryEvents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DeliveryId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OrderId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DriverId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TripId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AmountPiastres = table.Column<long>(type: "bigint", nullable: true),
                    Actor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Approver = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OperationId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryReassignments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OrderId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeliveryId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OldDriverId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OldDriverName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewDriverId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NewDriverName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryReassignments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Drivers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    EmployeeId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drivers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DriverSettlements",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DriverId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DriverName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrderIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrderCount = table.Column<int>(type: "int", nullable: false),
                    GrossOrderPiastres = table.Column<long>(type: "bigint", nullable: false),
                    PrepaidPiastres = table.Column<long>(type: "bigint", nullable: false),
                    ExpectedPiastres = table.Column<long>(type: "bigint", nullable: false),
                    ActualPiastres = table.Column<long>(type: "bigint", nullable: false),
                    DifferencePiastres = table.Column<long>(type: "bigint", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OperationId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TreasuryMovementId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReceivedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverSettlements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Deliveries",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OrderId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShiftId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    DriverId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    DriverNameAtHandover = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TripId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssignedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssignedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    OutForDeliveryAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeliveredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CollectedAmountPiastres = table.Column<long>(type: "bigint", nullable: true),
                    FailedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FailureNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReturnedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReturnReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SettledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Deliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Deliveries_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Deliveries_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Deliveries_Shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "Shifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DriverCustodies",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TripId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DriverId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OrderId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeliveryId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ExpectedPiastres = table.Column<long>(type: "bigint", nullable: false),
                    CollectedPiastres = table.Column<long>(type: "bigint", nullable: false),
                    SettledPiastres = table.Column<long>(type: "bigint", nullable: false),
                    DifferencePiastres = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SettledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverCustodies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverCustodies_Deliveries_DeliveryId",
                        column: x => x.DeliveryId,
                        principalTable: "Deliveries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DriverCustodies_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_DriverId",
                table: "Deliveries",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_OrderId",
                table: "Deliveries",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_ShiftId",
                table: "Deliveries",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryEvents_DeliveryId",
                table: "DeliveryEvents",
                column: "DeliveryId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryReassignments_DeliveryId",
                table: "DeliveryReassignments",
                column: "DeliveryId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverCustodies_DeliveryId",
                table: "DriverCustodies",
                column: "DeliveryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DriverCustodies_DriverId",
                table: "DriverCustodies",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverSettlements_IdempotencyKey",
                table: "DriverSettlements",
                column: "IdempotencyKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeliveryEvents");

            migrationBuilder.DropTable(
                name: "DeliveryReassignments");

            migrationBuilder.DropTable(
                name: "DriverCustodies");

            migrationBuilder.DropTable(
                name: "DriverSettlements");

            migrationBuilder.DropTable(
                name: "Deliveries");

            migrationBuilder.DropTable(
                name: "Drivers");
        }
    }
}
