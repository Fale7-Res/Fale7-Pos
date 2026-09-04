using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Falah.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttendanceRecords",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    CheckIn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CheckOut = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShiftName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LateMinutes = table.Column<int>(type: "int", nullable: false),
                    WorkHours = table.Column<double>(type: "float", nullable: false),
                    OvertimeHours = table.Column<double>(type: "float", nullable: false),
                    TatbiqCount = table.Column<int>(type: "int", nullable: false),
                    IsManualCorrection = table.Column<bool>(type: "bit", nullable: false),
                    CorrectedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorrectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastCheckInEventId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastCheckOutEventId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_EmployeeId_Date",
                table: "AttendanceRecords",
                columns: new[] { "EmployeeId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceRecords");
        }
    }
}
