namespace Falah.Infrastructure.BusinessDay;

using System.Data;
using Falah.Domain.Common;
using Falah.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

/// <summary>Transaction-owned, cross-process admission barrier. Money writers share it; shift/day
/// close takes it exclusively before reading balances or blockers. Independent money writers retain
/// their existing account/employee/invoice locks and can still run concurrently.</summary>
internal static class FinancialAdmission
{
    internal static async Task AcquireAsync(FalahDbContext db, CancellationToken ct, bool closing = false)
    {
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Financial admission requires a transaction.");
        var result = new SqlParameter("@admissionResult", SqlDbType.Int) { Direction = ParameterDirection.Output };
        var mode = closing ? "Exclusive" : "Shared";
        await db.Database.ExecuteSqlRawAsync(
            "EXEC @admissionResult = sys.sp_getapplock @Resource=N'FalahFinancialAdmission', @LockMode=N'" + mode +
            "', @LockOwner=N'Transaction', @LockTimeout=" + Falah.Infrastructure.Persistence.LockTimeouts.Milliseconds, new object[] { result }, ct);
        if (result.Value is not int code || code < 0)
            throw new DomainException("تعذر بدء العملية أثناء إغلاق أو تحديث مالي متزامن. أعد المحاولة.");
    }
}
