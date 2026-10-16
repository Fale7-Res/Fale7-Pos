using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Shifts;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// Open-shift modal - ported from ShiftsScreen.tsx's own OPEN NEW SHIFT MODAL, including its shift-
/// name dropdown (the two fixed real shift patterns this restaurant runs, morning crossing into
/// evening) and its opening-cash prefill from the Main Treasury balance. CashierViewModel's own
/// POS-embedded open-shift flow deliberately skips both (it infers the shift name from the time of
/// day and starts the field at 0) - that is a screen-specific simplification made for the fast
/// touch-terminal path, not a reference behaviour to repeat here; this screen is the one explicitly
/// modelled on the reference's own dialog.
/// </summary>
public partial class ShiftsViewModel
{
    public sealed record ShiftNameOption(string Value, string Label);

    public IReadOnlyList<ShiftNameOption> ShiftNameOptions { get; } = new[]
    {
        new ShiftNameOption("وردية صباحية", "وردية صباحية (07:00 ص - 05:00 م)"),
        new ShiftNameOption("وردية مسائية", "وردية مسائية (05:00 م - 03:00 ص)"),
    };

    [ObservableProperty] private bool _isOpenShiftModalOpen;
    [ObservableProperty] private ShiftNameOption _selectedShiftName = null!;
    [ObservableProperty] private string _openingCashText = "0";
    [ObservableProperty] private long? _mainTreasuryBalancePiastres;
    [ObservableProperty] private string? _openShiftError;
    [ObservableProperty] private bool _isOpeningShift;

    public bool MainTreasuryIsEmpty => CanSeeMainTreasuryBalance && MainTreasuryBalancePiastres is <= 0;

    partial void OnMainTreasuryBalancePiastresChanged(long? value) => OnPropertyChanged(nameof(MainTreasuryIsEmpty));

    [RelayCommand]
    private async Task OpenOpenShiftModalAsync()
    {
        if (!CanOpenShift || HasActiveShift) return;
        SelectedShiftName = ShiftNameOptions[0];
        OpenShiftError = null;
        MainTreasuryBalancePiastres = null;

        // The float the owner configured, offered as a starting point - the cashier types over it
        // freely, and whatever they confirm is what the shift stores and reconciles against.
        var settings = await _session.Api.GetSettingsAsync();
        var suggested = settings.Success && settings.Value is not null
            ? Math.Max(0, settings.Value.DefaultOpeningCashPiastres)
            : 0;

        if (CanSeeMainTreasuryBalance)
        {
            var balance = await _session.Api.GetTreasuryBalanceAsync("tre-main");
            if (balance.Success && balance.Value is not null)
            {
                MainTreasuryBalancePiastres = balance.Value.BalancePiastres;
                // Never suggest handing out more cash than the main treasury actually holds - the
                // server refuses that anyway, and offering it would only produce a failed open.
                suggested = Math.Max(0, Math.Min(suggested, balance.Value.BalancePiastres));
            }
        }

        OpeningCashText = Services.CashInput.Format(suggested);

        IsOpenShiftModalOpen = true;
    }

    [RelayCommand]
    private void CloseOpenShiftModal() => IsOpenShiftModalOpen = false;

    [RelayCommand]
    private async Task ConfirmOpenShiftAsync()
    {
        if (IsOpeningShift) return;
        if (!CashInput.TryParse(OpeningCashText, out var openingEgp) || openingEgp < 0)
        {
            OpenShiftError = "أدخل رصيدًا افتتاحيًا صحيحًا.";
            return;
        }

        IsOpeningShift = true;
        OpenShiftError = null;
        try
        {
            var request = new OpenShiftRequest(
                RequestId: $"shift-open-{Guid.NewGuid():N}",
                ShiftName: SelectedShiftName.Value,
                OpenedByUserId: _session.CurrentUser!.Id,
                OpeningCashPiastres: openingEgp);

            var result = await _session.Api.OpenShiftAsync(request);
            if (!result.Success || result.Value is null)
            {
                // Never surface the raw "insufficient source balance" domain error to someone who
                // cannot see the Main Treasury balance in the first place - it would leak the same
                // information the permission gate above was protecting. Mirrors the reference's own
                // identical guard on this exact error string.
                var raw = result.Error ?? "تعذر فتح الوردية.";
                OpenShiftError = !CanSeeMainTreasuryBalance && raw.Contains("رصيد حساب المصدر غير كافٍ")
                    ? "العهدة المطلوبة غير متاحة. راجع المسؤول."
                    : raw;
                return;
            }

            IsOpenShiftModalOpen = false;
            _header.Refresh();
            await LoadAsync();
        }
        finally
        {
            IsOpeningShift = false;
        }
    }
}
