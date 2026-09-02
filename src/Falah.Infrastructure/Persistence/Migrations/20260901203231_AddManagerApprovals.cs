using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Falah.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddManagerApprovals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ManagerApprovals",
                columns: table => new
                {
                    ApprovalId = table.Column<string>(type: "TEXT", nullable: false),
                    ApproverUserId = table.Column<string>(type: "TEXT", nullable: false),
                    ApproverName = table.Column<string>(type: "TEXT", nullable: false),
                    ApproverRole = table.Column<string>(type: "TEXT", nullable: false),
                    Action = table.Column<string>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: true),
                    ApprovedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Consumed = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManagerApprovals", x => x.ApprovalId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ManagerApprovals");
        }
    }
}
