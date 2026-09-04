using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Falah.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessDay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BusinessDays",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    OpenedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OpenedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CloseRevision = table.Column<int>(type: "int", nullable: false),
                    SnapshotId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReopenReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReopenedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OpenRequestId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessDays", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessDays_OpenRequestId",
                table: "BusinessDays",
                column: "OpenRequestId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusinessDays");
        }
    }
}
