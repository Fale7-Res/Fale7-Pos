using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Inventory;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// Loss for one business date: the total value, split by the three distinct loss types the domain
/// keeps separate (prep waste / spoilage / breakage), over the movements that produced it.
///
/// Recording new loss lives on the Inventory screen, next to the stock balances it changes - this
/// screen is the management read of what was already recorded. A movement whose value is unknown
/// (no purchase history to value it against) shows its quantity and an explicit "غير مقيّم" rather
/// than a fabricated zero.
/// </summary>
public partial class WasteViewModel : ObservableObject
{
    private readonly AppSession _session;

    public WasteViewModel(AppSession session)
    {
        _session = session;
        var cairo = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
        _businessDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, cairo).DateTime);
    }

    public sealed record LossTypeRow(string Type, string Label, long ValuePiastres, int UnknownValueCount = 0)
    {
        public string ValueDisplay => UnknownValueCount > 0 && ValuePiastres == 0 ? "غير معلوم"
            : (UnknownValueCount > 0 ? "من " : "") + CashInput.Display(ValuePiastres) + " ج";
    }
    public string TotalValueDisplay => Summary is null ? "—"
        : Summary.UnknownValueMovementCount > 0 && Summary.UnknownValueMovementCount == Summary.MovementCount ? "غير معلوم"
        : (Summary.UnknownValueMovementCount > 0 ? "من " : "") + CashInput.Display(Summary.TotalLossValuePiastres) + " ج";
    public string UnknownValueNote => Summary is { UnknownValueMovementCount: > 0 } s
        ? $"{s.UnknownValueMovementCount} حركة فاقد بلا تكلفة؛ القيمة المعروضة للجزء المعروف فقط." : "";
    public bool HasUnknownValue => Summary?.UnknownValueMovementCount > 0;
    partial void OnSummaryChanged(LossSummaryDto? value)
    {
        OnPropertyChanged(nameof(TotalValueDisplay)); OnPropertyChanged(nameof(UnknownValueNote)); OnPropertyChanged(nameof(HasUnknownValue));
    }
    private bool _reloadRequested;

    [ObservableProperty] private DateOnly _businessDate;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _loadError;
    [ObservableProperty] private LossSummaryDto? _summary;

    public ObservableCollection<LossTypeRow> ByType { get; } = new();
    public ObservableCollection<StockMovementDto> Movements { get; } = new();

    public string BusinessDateLabel => BusinessDate.ToString("yyyy-MM-dd");
    partial void OnBusinessDateChanged(DateOnly value) => OnPropertyChanged(nameof(BusinessDateLabel));

    public bool HasNoLoss => !IsLoading && LoadError is null && (Summary?.MovementCount ?? 0) == 0;

    private static string LabelFor(string type) => type switch
    {
        "Waste" => "هدر تحضير",
        "Spoilage" => "تلف وانتهاء صلاحية",
        "Breakage" => "كسر",
        var other => other,
    };

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsLoading) { _reloadRequested = true; return; }
        var date = BusinessDate;
        IsLoading = true;
        LoadError = null;
        try
        {
            var actor = _session.CurrentUser!.Id;
            var summary = await _session.Api.GetLossSummaryAsync(actor, date);
            if (date != BusinessDate) return;
            if (!summary.Success || summary.Value is null)
            {
                LoadError = summary.Error ?? "تعذر تحميل ملخص الفاقد.";
                return;
            }
            Summary = summary.Value;

            ByType.Clear();
            foreach (var kv in summary.Value.ValueByType.OrderByDescending(k => k.Value))
                ByType.Add(new LossTypeRow(kv.Key, LabelFor(kv.Key), kv.Value, summary.Value.UnknownValueByType.GetValueOrDefault(kv.Key)));

            // The movement log filtered to the three loss types - the same rows the summary totals.
            var (start, end) = Falah.Domain.Common.BusinessClock.UtcRangeForBusinessDate(date);
            var movements = await _session.Api.GetStockMovementsAsync(limit: 200, from: start, to: end.AddTicks(-1), lossesOnly: true);
            if (date != BusinessDate) return;
            if (!movements.Success || movements.Value is null) { LoadError = movements.Error ?? "تعذر تحميل حركات الفاقد."; return; }
            Movements.Clear();
            if (movements.Success && movements.Value is not null)
                foreach (var m in movements.Value.Where(m => m.Type is "Waste" or "Spoilage" or "Breakage"))
                    Movements.Add(m);
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasNoLoss));
            if (_reloadRequested || date != BusinessDate) { _reloadRequested = false; await LoadAsync(); }
        }
    }

    [RelayCommand]
    private void PreviousDay() { BusinessDate = BusinessDate.AddDays(-1); _ = LoadAsync(); }

    [RelayCommand]
    private void NextDay() { BusinessDate = BusinessDate.AddDays(1); _ = LoadAsync(); }
}
