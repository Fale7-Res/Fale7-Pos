using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Falah.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmployeeLedgerEntries",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DebitPiastres = table.Column<long>(type: "bigint", nullable: false),
                    CreditPiastres = table.Column<long>(type: "bigint", nullable: false),
                    BalanceAfterPiastres = table.Column<long>(type: "bigint", nullable: false),
                    RecordedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AuthorizedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShiftId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReferenceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Hours = table.Column<double>(type: "float", nullable: true),
                    RateSnapshotPiastres = table.Column<long>(type: "bigint", nullable: true),
                    ValueSnapshotPiastres = table.Column<long>(type: "bigint", nullable: true),
                    PayrollRunId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TreasuryMovementId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeLedgerEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmployeeNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    JobTitle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HireDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CompensationType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DailyWagePiastres = table.Column<long>(type: "bigint", nullable: false),
                    ShiftValuePiastres = table.Column<long>(type: "bigint", nullable: false),
                    MonthlySalaryPiastres = table.Column<long>(type: "bigint", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CanDeliver = table.Column<bool>(type: "bit", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NationalId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Department = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmploymentArrangement = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FingerprintId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LifecycleStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OvertimeRatePiastres = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLedgerEntries_EmployeeId",
                table: "EmployeeLedgerEntries",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLedgerEntries_IdempotencyKey",
                table: "EmployeeLedgerEntries",
                column: "IdempotencyKey",
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_EmployeeNumber",
                table: "Employees",
                column: "EmployeeNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeLedgerEntries");

            migrationBuilder.DropTable(
                name: "Employees");
        }
    }
}
