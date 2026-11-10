namespace Falah.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

[DbContext(typeof(FalahDbContext))]
[Migration("20261107010000_AddVariantRetirement")]
public class AddVariantRetirement : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<bool>(name: "Active", table: "ProductVariants", type: "bit", nullable: false, defaultValue: true);
    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn(name: "Active", table: "ProductVariants");
}
