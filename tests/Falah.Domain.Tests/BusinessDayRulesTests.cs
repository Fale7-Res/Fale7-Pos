using Falah.Domain.BusinessDay;
using Falah.Domain.Common;
using Xunit;

namespace Falah.Domain.Tests;

public class BusinessDayRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Cannot_open_a_second_business_day_while_one_is_already_active()
    {
        var open = new BusinessDayRecord("day-1", new DateOnly(2026, 3, 5), "owner", Now, BusinessDayStatus.Open, 0);
        Assert.Throws<DomainException>(() =>
            BusinessDayRules.Open("day-2", new DateOnly(2026, 3, 5), "owner", Now, new[] { open }));
    }

    [Fact]
    public void Reopening_a_day_requires_a_reason()
    {
        var day = new BusinessDayRecord("day-1", new DateOnly(2026, 3, 5), "owner", Now, BusinessDayStatus.Closed, 1, SnapshotId: "snap-1");
        Assert.Throws<DomainException>(() =>
            BusinessDayRules.Reopen(day, new[] { day }, reason: "", "owner", Now));
    }

    [Fact]
    public void Reopening_a_day_without_a_close_snapshot_is_rejected()
    {
        var day = new BusinessDayRecord("day-1", new DateOnly(2026, 3, 5), "owner", Now, BusinessDayStatus.Closed, 1);
        Assert.Throws<DomainException>(() =>
            BusinessDayRules.Reopen(day, new[] { day }, "مراجعة فاتورة", "owner", Now));
    }

    [Fact]
    public void An_open_shift_blocks_business_day_close()
    {
        var blockers = BusinessDayRules.EvaluateCloseBlockers(new BusinessDayRules.CloseBlockerInputs(
            Shifts: new[] { ("shift-1", false) },
            DeliveryTrips: Array.Empty<(string, bool)>(),
            DriverCustodies: Array.Empty<(string, bool)>(),
            JournalEntries: Array.Empty<(string, bool)>(),
            Orders: Array.Empty<(string, bool)>(),
            PersistenceHealthy: true,
            WriterOwned: true,
            RequiredChecklist: Array.Empty<(string, string, bool)>(),
            ReconciliationChecks: Array.Empty<(string, bool)>()));

        Assert.Contains(blockers, b => b.Code == "OPEN_SHIFT");
        Assert.Throws<DomainException>(() => BusinessDayRules.AssertCanClose(blockers));
    }

    [Fact]
    public void An_unconfirmed_electronic_payment_blocks_close_even_when_everything_else_is_clean()
    {
        var blockers = BusinessDayRules.EvaluateCloseBlockers(new BusinessDayRules.CloseBlockerInputs(
            Shifts: new[] { ("shift-1", true) },
            DeliveryTrips: Array.Empty<(string, bool)>(),
            DriverCustodies: Array.Empty<(string, bool)>(),
            JournalEntries: Array.Empty<(string, bool)>(),
            Orders: new[] { ("ord-1", true) },
            PersistenceHealthy: true,
            WriterOwned: true,
            RequiredChecklist: Array.Empty<(string, string, bool)>(),
            ReconciliationChecks: Array.Empty<(string, bool)>()));

        Assert.Contains(blockers, b => b.Code == "PENDING_ELECTRONIC_PAYMENT");
    }

    [Fact]
    public void A_fully_clean_day_has_no_blockers_and_can_close()
    {
        var blockers = BusinessDayRules.EvaluateCloseBlockers(new BusinessDayRules.CloseBlockerInputs(
            Shifts: new[] { ("shift-1", true) },
            DeliveryTrips: new[] { ("trip-1", true) },
            DriverCustodies: new[] { ("c1", true) },
            JournalEntries: new[] { ("op-1", true) },
            Orders: new[] { ("ord-1", false) },
            PersistenceHealthy: true,
            WriterOwned: true,
            RequiredChecklist: new[] { ("chk-1", "عد الدرج", true) },
            ReconciliationChecks: new[] { ("DRAWER_RECONCILIATION", false) }));

        Assert.Empty(blockers);
        BusinessDayRules.AssertCanClose(blockers);
    }
}
