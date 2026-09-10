using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.BusinessDay;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// The physical cash count: how many of each Egyptian note and coin a person actually counted.
///
/// This is deliberately not the same thing as the cashier's drawer-balance inquiry, which reports
/// what the system believes should be there. The whole point of counting by denomination is to
/// produce an independent number and let the server compare it against the expected figure - so the
/// expected amount is entered/carried separately and the difference is never computed here as a
/// convenience that could quietly paper over a discrepancy.
///
/// The running total shown while typing is a live aid for the person counting; the authoritative
/// total, expected figure and difference all come back from the server's own calculation.
/// </summary>
public partial class CashCountViewModel : ObservableObject
{
    private readonly AppSession _session;
    private readonly RetainedRequest<SaveCashCountRequest> _write = new("business-day.cash-count");
    private string? _businessDayId;
    private string? _pendingId;

    public CashCountViewModel(AppSession session) => _session = session;

    public sealed partial class DenominationRow : ObservableObject
    {
        public required long DenominationPiastres { get; init; }
        [ObservableProperty] private string _quantityText = "";

        /// <summary>Notes are shown in pounds, coins in piastres - the way the person holding them
        /// would name them, rather than a uniform decimal that reads oddly for a 25pt coin.</summary>
        public string Label => DenominationPiastres >= 100
            ? $"{DenominationPiastres / 100} ج"
            : $"{DenominationPiastres} قرش";

        public long Quantity => long.TryParse(QuantityText, out var q) && q > 0 ? q : 0;
        public string LineTotalDisplay => CashInput.Format(Quantity * DenominationPiastres) + " ج";

        partial void OnQuantityTextChanged(string value)
        {
            OnPropertyChanged(nameof(LineTotalDisplay));
            Changed?.Invoke();
        }

        public event Action? Changed;
    }

    public ObservableCollection<DenominationRow> Rows { get; } = new();
    public ObservableCollection<CashDenominationCountDto> History { get; } = new();

    [ObservableProperty] private bool _isOpen;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isReady;
    [ObservableProperty] private string? _loadError;
    [ObservableProperty] private string? _actionError;
    [ObservableProperty] private string? _actionNote;
    [ObservableProperty] private string _expectedText = "";

    public bool CanOpen => _session.HasPermission("cash_count");
    public bool CanAct => CanOpen && !IsBusy && !IsLoading && IsReady;
    public bool HasHistory => History.Count > 0;
    public string SaveLabel => IsBusy ? "جاري الحفظ والتحقق..."
        : _write.HasPending ? "التحقق من الحفظ السابق" : "حفظ العدّ";

    /// <summary>A live aid for the person counting - never the figure that decides anything.</summary>
    public string CountedTotalDisplay => CashInput.Format(Rows.Sum(r => r.Quantity * r.DenominationPiastres)) + " ج";

    partial void OnIsBusyChanged(bool value) => NotifyState();
    partial void OnIsLoadingChanged(bool value) => NotifyState();

    private void NotifyState()
    {
        OnPropertyChanged(nameof(CanAct));
        OnPropertyChanged(nameof(SaveLabel));
        _session.SetManagementOperationPending(this, IsBusy || _write.HasPending);
    }

    public async Task OpenAsync(string businessDayId)
    {
        if (!CanOpen || _session.HasUnresolvedOperation || string.IsNullOrWhiteSpace(businessDayId)) return;
        _businessDayId = businessDayId;
        IsOpen = true;
        ActionError = ActionNote = null;
        ExpectedText = "";
        await LoadAsync();
    }

    [RelayCommand]
    private void Close()
    {
        if (IsBusy || _write.HasPending) return;
        IsOpen = false;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsLoading || _businessDayId is null) return;
        IsLoading = true;
        IsReady = false;
        LoadError = null;
        try
        {
            var denominations = await _session.Api.GetCashCountDenominationsAsync();
            if (!denominations.Success || denominations.Value is null)
            { LoadError = denominations.Error ?? "تعذر تحميل فئات النقد."; return; }

            var history = await _session.Api.GetCashCountsAsync(_businessDayId);
            if (!history.Success || history.Value is null)
            { LoadError = history.Error ?? "تعذر تحميل عمليات العدّ السابقة."; return; }

            foreach (var row in Rows) row.Changed -= OnRowChanged;
            Rows.Clear();
            // The server owns the denomination list; it is never hardcoded in the client.
            foreach (var value in denominations.Value)
            {
                var row = new DenominationRow { DenominationPiastres = value };
                row.Changed += OnRowChanged;
                Rows.Add(row);
            }

            History.Clear();
            foreach (var count in history.Value) History.Add(count);

            IsReady = true;
            OnPropertyChanged(nameof(HasHistory));
            OnPropertyChanged(nameof(CountedTotalDisplay));
        }
        finally { IsLoading = false; }
    }

    private void OnRowChanged() => OnPropertyChanged(nameof(CountedTotalDisplay));

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsBusy) return;
        ActionError = null;

        var request = _write.Value;
        if (request is null)
        {
            if (!CanAct || _businessDayId is null || _session.CurrentUser is null) return;
            if (!Rows.Any(r => r.Quantity > 0)) { ActionError = "أدخل عدد الأوراق أو العملات لفئة واحدة على الأقل."; return; }
            if (!CashInput.TryParse(ExpectedText, out var expected) || expected < 0)
            { ActionError = "أدخل المبلغ المتوقع بشكل صحيح."; return; }

            _pendingId ??= $"ccx-{Guid.NewGuid():N}";
            request = new SaveCashCountRequest(
                _pendingId, _businessDayId, _session.ShiftId,
                Rows.Select(r => new DenominationRowInput(r.DenominationPiastres, r.Quantity)).ToList(),
                expected, _session.CurrentUser.Id);
        }

        IsBusy = true;
        ActionNote = null;
        try
        {
            var result = await _write.SendAsync(request, r => _session.Api.SaveCashCountAsync(r));
            if (!result.Success || result.Value is null)
            { ActionError = _write.FailureMessage(result.Error); return; }

            var saved = result.Value;
            var difference = saved.DifferencePiastres;
            ActionNote = difference == 0
                ? $"تم تسجيل العدّ. المبلغ المعدود {CashInput.Format(saved.TotalPiastres)} ج مطابق للمتوقع."
                : difference > 0
                    ? $"تم تسجيل العدّ. زيادة {CashInput.Format(difference)} ج عن المتوقع."
                    : $"تم تسجيل العدّ. عجز {CashInput.Format(-difference)} ج عن المتوقع.";
            _pendingId = null;
            foreach (var row in Rows) row.QuantityText = "";
            ExpectedText = "";
            await LoadAsync();
        }
        finally { IsBusy = false; }
    }
}
