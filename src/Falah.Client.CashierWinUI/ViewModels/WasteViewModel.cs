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

    public sealed record LossTypeRow(string Type, string Label, long ValuePiastres);

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
        if (IsLoading) return;
        IsLoading = true;
        LoadError = null;
        try
        {
            var actor = _session.CurrentUser!.Id;
            var summary = await _session.Api.GetLossSummaryAsync(actor, BusinessDate);
            if (!summary.Success || summary.Value is null)
            {
                LoadError = summary.Error ?? "تعذر تحميل ملخص الفاقد.";
                return;
            }
            Summary = summary.Value;

            ByType.Clear();
            foreach (var kv in summary.Value.ValueByType.OrderByDescending(k => k.Value))
                ByType.Add(new LossTypeRow(kv.Key, LabelFor(kv.Key), kv.Value));

            // The movement log filtered to the three loss types - the same rows the summary totals.
            var movements = await _session.Api.GetStockMovementsAsync(limit: 200);
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
        }
    }

    [RelayCommand]
    private void PreviousDay() { BusinessDate = BusinessDate.AddDays(-1); _ = LoadAsync(); }

    [RelayCommand]
    private void NextDay() { BusinessDate = BusinessDate.AddDays(1); _ = LoadAsync(); }
}
