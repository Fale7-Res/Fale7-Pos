using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Falah.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSettingsDefaultOpeningCash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 100000 piastres = 1000 EGP: the float this restaurant actually starts a shift with.
            // Given as the column default so the row an existing database already has is backfilled
            // with it rather than with zero, which would offer every cashier an empty drawer.
            migrationBuilder.AddColumn<long>(
                name: "DefaultOpeningCashPiastres",
                table: "Settings",
                type: "bigint",
                nullable: false,
                defaultValue: 100000L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultOpeningCashPiastres",
                table: "Settings");
        }
    }
}
