using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Falah.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCollectionShortfallClassificationAndHandoverReceipt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HandoverAcknowledgeRequestId",
                table: "Shifts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "HandoverAcknowledgedAtUtc",
                table: "Shifts",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HandoverAcknowledgedByName",
                table: "Shifts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HandoverAcknowledgedByUserId",
                table: "Shifts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HandoverReceiptNote",
                table: "Shifts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HandoverReceiptStatus",
                table: "Shifts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "HandoverReceivedPiastres",
                table: "Shifts",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CollectionShortfallResolutions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DeliveryId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OrderId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DriverId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AmountPiastres = table.Column<long>(type: "bigint", nullable: false),
                    Classification = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Evidence = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ClassifiedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClassifiedByName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionShortfallResolutions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollectionShortfallResolutions_DeliveryId",
                table: "CollectionShortfallResolutions",
                column: "DeliveryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CollectionShortfallResolutions_RequestId",
                table: "CollectionShortfallResolutions",
                column: "RequestId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CollectionShortfallResolutions");

            migrationBuilder.DropColumn(
                name: "HandoverAcknowledgeRequestId",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "HandoverAcknowledgedAtUtc",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "HandoverAcknowledgedByName",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "HandoverAcknowledgedByUserId",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "HandoverReceiptNote",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "HandoverReceiptStatus",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "HandoverReceivedPiastres",
                table: "Shifts");
        }
    }
}
