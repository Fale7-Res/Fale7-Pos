using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Printing;
using Falah.Application.Reports;
using Falah.Application.Shifts;
using Falah.Client.CashierWinUI.Services;
using Falah.Domain.Common;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// The management-facing Shifts screen - ported from ShiftsScreen.tsx. The reference's own
/// permission shape carries over exactly: <see cref="CanOpenShift"/> matches its canOpenShift,
/// <see cref="CanSeeShiftTotals"/> its canSeeShiftTotals (sales/cash figures are management
/// information; a cashier operates and blind-counts a shift but must never see the running total
/// beforehand - that is what keeps the blind close honest), <see cref="CanSeeMainTreasuryBalance"/>
/// its canSeeMainTreasuryBalance.
///
/// One deliberate, disclosed simplification from the reference: the reference's shift-history array
/// lives entirely in client state, so every role sees every row and only certain COLUMNS are hidden
/// by permission. The C# history endpoint is one query gated on reports_view as a whole (it already
/// carries the same sales figures GetShiftSummaryAsync reports for a single shift), so a cashier
/// without reports_view sees no history rows at all rather than a row set with some columns
/// missing - see <see cref="IsHistoryRestricted"/>, which the view uses to show why, rather than
/// presenting a table that silently came back empty.
///
/// The actual open/close OPERATIONS reuse the exact same ApiClient calls and server handlers
/// CashierViewModel's own POS-embedded shift flow uses (OpenShiftAsync/PrepareShiftCloseAsync/
/// CloseShiftAsync) - nothing about the business rule (the blind count, the discrepancy threshold,
/// the manager-approval requirement) is reimplemented here, only the screen-specific presentation.
/// </summary>
public partial class ShiftsViewModel : ObservableObject
{
    private readonly AppSession _session;
    private readonly HeaderViewModel _header;

    public ShiftsViewModel(AppSession session, HeaderViewModel header)
    {
        _session = session;
        _header = header;
    }

    public bool CanOpenShift => _session.HasPermission("shifts_manage") || _session.HasPermission("shifts_open_own");
    public bool CanCloseShift => _session.HasPermission("shifts_manage") || _session.HasPermission("shifts_blind_close");
    public bool CanSeeShiftTotals => _session.HasPermission("reports_view");
    public bool CanSeeMainTreasuryBalance => _session.HasPermission("treasury_manage");
    public bool IsHistoryRestricted => !CanSeeShiftTotals;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _loadError;
    [ObservableProperty] private CurrentShiftDto? _activeShift;
    [ObservableProperty] private ShiftSummaryDto? _activeShiftSummary;

    public bool HasActiveShift => ActiveShift is not null;
    public bool CanOpenNewShift => !HasActiveShift && CanOpenShift;
    /// <summary>The reference's own third header state: no active shift AND this actor cannot open
    /// one either - never shown alongside the close button (a cashier without shifts_open_own can
    /// still close the shift someone else opened, and seeing "requires a manager" next to a working
    /// close button would read as a contradiction).</summary>
    public bool ShowOpenNotAllowedNotice => !HasActiveShift && !CanOpenShift;
    public string ActiveShiftOpenedLine => ActiveShift is null ? "" :
        $"تم الفتح بواسطة: {ActiveShift.OpenedByUserName ?? ActiveShift.OpenedByUserId} • تاريخ الفتح: {FormatCairo(ActiveShift.OpenedAtUtc)}";

    partial void OnActiveShiftChanged(CurrentShiftDto? value)
    {
        OnPropertyChanged(nameof(HasActiveShift));
        OnPropertyChanged(nameof(CanOpenNewShift));
        OnPropertyChanged(nameof(ShowOpenNotAllowedNotice));
        OnPropertyChanged(nameof(ActiveShiftOpenedLine));
    }

    public ObservableCollection<ShiftRow> History { get; } = new();

    // ---- Reprinting a shift's own paperwork ----

    [ObservableProperty] private string? _reprintMessage;
    [ObservableProperty] private bool _isReprinting;

    [RelayCommand]
    private Task ReprintShiftOpenAsync(ShiftRow? row) =>
        ReprintAsync(row, "ShiftOpen", "إعادة طباعة إيصال افتتاح الوردية");

    [RelayCommand]
    private Task ReprintShiftCloseAsync(ShiftRow? row) =>
        ReprintAsync(row, "ShiftClose", "إعادة طباعة إيصال تسليم الوردية");

    /// <summary>
    /// Queues the slip again. This never reopens, recloses or edits the shift - the server creates a
    /// print job marked "إعادة طباعة" and writes the audit entry, and that is all.
    ///
    /// A fresh request id per press on purpose: each press is a separate, legitimate reprint, and a
    /// reviewer should see each one.
    /// </summary>
    private async Task ReprintAsync(ShiftRow? row, string documentKind, string reason)
    {
        if (row is null || IsReprinting) return;
        IsReprinting = true;
        ReprintMessage = null;
        try
        {
            var result = await _session.Api.ReprintDocumentAsync(new ReprintDocumentRequest(
                $"rp-{Guid.NewGuid():N}", documentKind, row.ShiftId, _session.CurrentUser!.Id, reason));
            ReprintMessage = result.Success
                ? "تم إرسال الإيصال للطباعة؛ لم يتم تأكيد خروج الورقة."
                : result.Error ?? "تعذر إرسال أمر إعادة الطباعة.";
        }
        finally
        {
            IsReprinting = false;
        }
    }

    /// <summary>Cairo wall-clock, matching every other timestamp this build shows a person - not
    /// machine-local time, which depends on whatever timezone the till happens to be set to.</summary>
    internal static string FormatCairo(DateTimeOffset at) =>
        TimeZoneInfo.ConvertTime(at, BusinessClock.CairoTimeZone).ToString("yyyy-MM-dd HH:mm");

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        LoadError = null;
        try
        {
            var actor = _session.CurrentUser!.Id;

            var current = await _session.Api.GetCurrentShiftAsync();
            ActiveShift = current.Success ? current.Value : null;
            // Kept in sync with the session-wide field CashierViewModel also writes, so a manager who
            // opens/closes a shift from THIS screen and then switches to the POS screen does not see
            // a stale "no shift open" state there.
            _session.ShiftId = ActiveShift?.ShiftId;
            _session.ShiftName = ActiveShift?.ShiftName;

            if (!CanSeeShiftTotals)
            {
                ActiveShiftSummary = null;
                History.Clear();
                return;
            }

            if (ActiveShift is { } active)
            {
                var summary = await _session.Api.GetShiftSummaryAsync(active.ShiftId, actor);
                ActiveShiftSummary = summary.Success ? summary.Value : null;
            }
            else
            {
                ActiveShiftSummary = null;
            }

            var history = await _session.Api.GetShiftsHistoryAsync(actor);
            if (!history.Success || history.Value is null)
            {
                LoadError = history.Error ?? "تعذر تحميل سجل الورديات.";
                History.Clear();
                return;
            }

            History.Clear();
            foreach (var dto in history.Value) History.Add(new ShiftRow(dto));
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>One history row, with the display logic ShiftsScreen.tsx computes inline (the
    /// three-way "مطابق / عجز / زيادة" state) pulled out as plain properties the view binds to,
    /// rather than a converter guessing at a string it was never given the real number behind.</summary>
    public sealed class ShiftRow
    {
        private readonly ShiftSummaryDto _dto;
        public ShiftRow(ShiftSummaryDto dto) => _dto = dto;

        public string ShiftName => _dto.ShiftName;
        public string ShiftId => _dto.ShiftId;
        /// <summary>Shown under the shift name in the history list, where the raw shift id used to be
        /// printed - an internal key means nothing to the manager reading this list.</summary>
        public string OpenedByLine => $"فتحها: {_dto.OpenedByUserName ?? "—"}";
        public bool IsClosed => _dto.Status == "Closed";
        public string OpenedLine => $"فتح: {FormatCairo(_dto.OpenedAtUtc)}";
        public string ClosedLine => _dto.ClosedAtUtc is { } at ? $"إغلاق: {FormatCairo(at)}" : "إغلاق: مستمرة";
        public string ClosedByOrOpenedByName => _dto.ClosedByUserName ?? _dto.OpenedByUserName ?? "—";
        public long OpeningCashPiastres => _dto.OpeningCashPiastres;
        public long NetSalesPiastres => _dto.Sales.NetSalesPiastres;
        public long ExpensesTotalPiastres => _dto.ExpensesTotalPiastres;
        public long? ExpectedCashPiastres => _dto.ExpectedCashPiastres;
        public long? ActualCashPiastres => _dto.ActualCashPiastres;

        private long Difference => _dto.CashDifferencePiastres ?? 0;
        public bool IsBalanced => IsClosed && Difference == 0;
        public bool IsDeficit => IsClosed && Difference < 0;
        public bool IsSurplus => IsClosed && Difference > 0;
        public long AbsoluteDifferencePiastres => Math.Abs(Difference);
    }
}
