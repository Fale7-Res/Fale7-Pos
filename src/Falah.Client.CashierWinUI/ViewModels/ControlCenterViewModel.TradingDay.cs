using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>Audit 2026-11-25 - the Control Center's day dialogs, as a manager met them on a full trading day.</summary>
public partial class ControlCenterViewModel
{
    /// <summary>U1/U33: what the confirm button will do, in words - "تأكيد العملية" said nothing.</summary>
    public string DayActionConfirmText => DayAction switch
    {
        "open" => "فتح يوم العمل",
        "close" => BusinessDay is { } day ? $"إغلاق يوم {CashierViewModel.Ltr(day.BusinessDate)}" : "إغلاق يوم العمل",
        _ => "إعادة فتح يوم العمل",
    };

    /// <summary>U1: the day that opens (the server's Cairo date unless another is typed). U33: closing ends the day's
    /// books, so it confirms with the day's figures the manager is signing off.</summary>
    public string DayActionSummary => DayAction switch
    {
        "open" => $"هيتفتح يوم {CashierViewModel.Ltr(Falah.Domain.Common.BusinessClock.BusinessDate(DateTimeOffset.UtcNow).ToString("yyyy-MM-dd"))} (تاريخ القاهرة النهارده). اكتب تاريخ تاني تحت بس لو محتاج.",
        "close" when Reconciliation is { } r =>
            $"صافي المبيعات {CashInput.Display(r.Sales.NetSalesPiastres)} ج · المرتجعات {CashInput.Display(r.Sales.RefundsPiastres)} ج · "
            + $"المصروفات {CashInput.Display(r.ExpensesTotalPiastres)} ج · فرق الورديات {CashInput.Display(ReconciliationDifferencePiastres)} ج."
            + " بعد الإغلاق الأرقام دي بتتثبت ومحتاجة إعادة فتح عشان تتغيّر.",
        "close" => "بعد الإغلاق أرقام اليوم بتتثبت ومحتاجة إعادة فتح عشان تتغيّر.",
        _ => "",
    };

    /// <summary>U32: the count is compared with the open shift's drawer (the server refuses it otherwise).</summary>
    public bool CanStartCashCount => HasBusinessDay && HasOpenShiftForDrop;
    public string? CashCountUnavailableHint => HasOpenShiftForDrop ? null
        : "عدّ الدرج بيتقارن بدرج الوردية المفتوحة - مفيش وردية مفتوحة دلوقتي. بند «عد درج النقدية» في قائمة الإغلاق بيتعلّم يدويًا.";

    private void NotifyDayDialogs()
    {
        OnPropertyChanged(nameof(DayActionConfirmText)); OnPropertyChanged(nameof(DayActionSummary));
        OnPropertyChanged(nameof(CanStartCashCount)); OnPropertyChanged(nameof(CashCountUnavailableHint));
    }

    partial void OnReconciliationChanged(Falah.Application.Reports.BusinessDayReconciliationDto? value) => NotifyDayDialogs();
}
