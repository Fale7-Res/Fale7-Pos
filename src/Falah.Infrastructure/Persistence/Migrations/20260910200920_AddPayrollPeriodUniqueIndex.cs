using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Falah.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// One live payroll run per month, enforced by the database.
    ///
    /// The handler read "is there already a run for this period" and then inserted. Eight requests
    /// arriving together all read "no" before any of them had inserted, and the month ended up with
    /// several runs, each carrying its own set of employee lines waiting to be paid.
    ///
    /// If this migration fails on an existing database, the message will name a duplicate Period:
    /// that database ALREADY has more than one live run for a month, and which of them is the real
    /// payroll is an accounting decision, not one a migration may make quietly. Cancel the wrong
    /// run first, then migrate. Period is narrowed to nvarchar(450) so it can be indexed; it holds
    /// "YYYY-MM" and is validated as such before any run is created.
    /// </summary>
    public partial class AddPayrollPeriodUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Period",
                table: "PayrollRuns",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_Period_Live",
                table: "PayrollRuns",
                column: "Period",
                unique: true,
                filter: "[Status] <> 'Cancelled'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PayrollRuns_Period_Live",
                table: "PayrollRuns");

            migrationBuilder.AlterColumn<string>(
                name: "Period",
                table: "PayrollRuns",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
