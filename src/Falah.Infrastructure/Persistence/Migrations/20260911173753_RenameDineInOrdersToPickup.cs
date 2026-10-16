using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Falah.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameDineInOrdersToPickup : Migration
    {
        /// <summary>
        /// Data only - the schema does not change. DineIn became Pickup, so the orders written under
        /// the old wire value are moved onto the new one and the column holds one spelling again.
        /// OrderEnumMapping still parses "dine_in" regardless, so this is tidiness rather than a
        /// repair: a row missed here would still read back correctly.
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Orders SET OrderType = 'pickup' WHERE OrderType IN ('dine_in', 'dinein');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Orders SET OrderType = 'dine_in' WHERE OrderType = 'pickup';");
        }
    }
}
