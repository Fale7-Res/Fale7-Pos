namespace Falah.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
[DbContext(typeof(FalahDbContext))]
[Migration("20261107030000_AttendanceFullInstants")]
public class AttendanceFullInstants : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(name: "CheckInAtUtc", table: "AttendanceRecords", type: "datetimeoffset", nullable: true);
        migrationBuilder.AddColumn<DateTimeOffset>(name: "CheckOutAtUtc", table: "AttendanceRecords", type: "datetimeoffset", nullable: true);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "CheckInAtUtc", table: "AttendanceRecords");
        migrationBuilder.DropColumn(name: "CheckOutAtUtc", table: "AttendanceRecords");
    }
}
