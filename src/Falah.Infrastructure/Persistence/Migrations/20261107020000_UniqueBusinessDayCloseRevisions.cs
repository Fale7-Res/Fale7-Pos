namespace Falah.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
[DbContext(typeof(FalahDbContext))]
[Migration("20261107020000_UniqueBusinessDayCloseRevisions")]
public class UniqueBusinessDayCloseRevisions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.CreateIndex(
        name: "IX_BusinessDayCloseSnapshots_BusinessDayId_CloseRevision", table: "BusinessDayCloseSnapshots",
        columns: new[] { "BusinessDayId", "CloseRevision" }, unique: true);
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropIndex(
        name: "IX_BusinessDayCloseSnapshots_BusinessDayId_CloseRevision", table: "BusinessDayCloseSnapshots");
}
