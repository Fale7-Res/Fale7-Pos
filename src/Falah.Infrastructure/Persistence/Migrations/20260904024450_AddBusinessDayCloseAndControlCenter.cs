using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Falah.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessDayCloseAndControlCenter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ClosedAtUtc",
                table: "BusinessDays",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClosedBy",
                table: "BusinessDays",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PreviousClosedAtUtc",
                table: "BusinessDays",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousClosedBy",
                table: "BusinessDays",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousSnapshotId",
                table: "BusinessDays",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReopenApprovalId",
                table: "BusinessDays",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReopenApprovedBy",
                table: "BusinessDays",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReopenRequestId",
                table: "BusinessDays",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReopenedAtUtc",
                table: "BusinessDays",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BusinessDayCloseSnapshots",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BusinessDayId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CloseRevision = table.Column<int>(type: "int", nullable: false),
                    Authoritative = table.Column<bool>(type: "bit", nullable: false),
                    PreviousSnapshotId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    GeneratedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ClosedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OperationId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReconciliationJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExceptionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChecklistJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessDayCloseSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessDayCloseSnapshots_BusinessDays_BusinessDayId",
                        column: x => x.BusinessDayId,
                        principalTable: "BusinessDays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChecklistCompletions",
                columns: table => new
                {
                    BusinessDayId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ItemId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Completed = table.Column<bool>(type: "bit", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChecklistCompletions", x => new { x.BusinessDayId, x.ItemId });
                });

            migrationBuilder.CreateTable(
                name: "ChecklistItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phase = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Required = table.Column<bool>(type: "bit", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChecklistItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ControlAuditEvents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Actor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AmountPiastres = table.Column<long>(type: "bigint", nullable: true),
                    OperationId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BlockerCodesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ControlAuditEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ControlExceptionStates",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Actor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TimestampUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ControlExceptionStates", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ChecklistItems",
                columns: new[] { "Id", "Active", "Label", "Phase", "Required", "SortOrder" },
                values: new object[,]
                {
                    { "close-delivery", true, "مراجعة عهد السائقين", "Closing", true, 3 },
                    { "close-drawer", true, "عد درج النقدية", "Closing", true, 2 },
                    { "close-review", true, "مراجعة المرتجعات والمصروفات", "Closing", false, 4 },
                    { "close-shifts", true, "مراجعة إغلاق كل الورديات", "Closing", true, 1 },
                    { "open-drawer", true, "تجهيز درج النقدية", "Opening", true, 1 },
                    { "open-printer", true, "فحص الطابعة", "Opening", false, 2 },
                    { "open-stations", true, "تأكيد جاهزية محطات التشغيل والتنظيف", "Opening", false, 3 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessDayCloseSnapshots_BusinessDayId",
                table: "BusinessDayCloseSnapshots",
                column: "BusinessDayId");

            migrationBuilder.CreateIndex(
                name: "IX_ControlAuditEvents_CreatedAtUtc",
                table: "ControlAuditEvents",
                column: "CreatedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusinessDayCloseSnapshots");

            migrationBuilder.DropTable(
                name: "ChecklistCompletions");

            migrationBuilder.DropTable(
                name: "ChecklistItems");

            migrationBuilder.DropTable(
                name: "ControlAuditEvents");

            migrationBuilder.DropTable(
                name: "ControlExceptionStates");

            migrationBuilder.DropColumn(
                name: "ClosedAtUtc",
                table: "BusinessDays");

            migrationBuilder.DropColumn(
                name: "ClosedBy",
                table: "BusinessDays");

            migrationBuilder.DropColumn(
                name: "PreviousClosedAtUtc",
                table: "BusinessDays");

            migrationBuilder.DropColumn(
                name: "PreviousClosedBy",
                table: "BusinessDays");

            migrationBuilder.DropColumn(
                name: "PreviousSnapshotId",
                table: "BusinessDays");

            migrationBuilder.DropColumn(
                name: "ReopenApprovalId",
                table: "BusinessDays");

            migrationBuilder.DropColumn(
                name: "ReopenApprovedBy",
                table: "BusinessDays");

            migrationBuilder.DropColumn(
                name: "ReopenRequestId",
                table: "BusinessDays");

            migrationBuilder.DropColumn(
                name: "ReopenedAtUtc",
                table: "BusinessDays");
        }
    }
}
