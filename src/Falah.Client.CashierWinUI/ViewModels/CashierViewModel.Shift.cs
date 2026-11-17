using Falah.Client.CashierWinUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Shifts;

namespace Falah.Client.CashierWinUI.ViewModels;

public partial class CashierViewModel
{
    [ObservableProperty] private string _openingCashText = "0";
    [ObservableProperty] private string? _openShiftError;
    [ObservableProperty] private bool _isOpeningShift;
    /// <summary>Set when the previous close left a float in the drawer: this shift opens on that cash (no second
    /// float from the treasury), and the cashier types what they actually found.</summary>
    [ObservableProperty] private string? _openingInheritedNote;

    /// <summary>Offers the inherited float as the opening figure, or the configured float on an empty drawer.</summary>
    internal async Task OfferOpeningCashAsync(long configuredFloatPiastres)
    {
        var left = await _session.Api.GetDrawerFloatAsync();
        if (left.Success && left.Value?.InheritedFloatPiastres is { } inherited)
        {
            OpeningCashText = CashInput.Format(inherited);
            OpeningInheritedNote = $"الوردية السابقة ({left.Value.FromShiftName}) تركت في الدرج {CashInput.Format(inherited)} ج. عُدّ الدرج وأدخل ما وجدته فعلًا؛ أي فرق يُسجَّل منفصلًا ولا يُحمَّل عليك. لإكمال العهدة استخدم «إضافة نقدية» من الخزنة بعد الفتح.";
            return;
        }
        OpeningInheritedNote = null;
        OpeningCashText = CashInput.Format(configuredFloatPiastres);
    }

    [RelayCommand]
    private async Task OpenShiftAsync()
    {
        if (IsOpeningShift || IsInitializing || !CanOpenShift) return;
        if (_pendingOpenShift is not null) { IsOpeningShift = true; try { await CompleteOpenShiftAsync(_pendingOpenShift); } finally { IsOpeningShift = false; } return; }
        if (!CashInput.TryParse(OpeningCashText, out var openingEgp) || openingEgp < 0)
        {
            OpenShiftError = "أدخل رصيدًا افتتاحيًا صحيحًا.";
            return;
        }

        IsOpeningShift = true;
        OpenShiftError = null;
        try
        {
            if (_pendingOpenShift is not null) { await CompleteOpenShiftAsync(_pendingOpenShift); return; }

            var request = new OpenShiftRequest(
                RequestId: _OpenShiftRequestId,
                ShiftName: DateTime.Now.Hour < 16 ? "صباحية" : "مسائية",
                OpenedByUserId: _session.CurrentUser!.Id,
                OpeningCashPiastres: openingEgp);

            await CompleteOpenShiftAsync(request);
        }
        finally
        {
            IsOpeningShift = false;
        }
    }
    private OpenShiftRequest? _pendingOpenShift;
    private string _OpenShiftRequestId = Guid.NewGuid().ToString("N");
    private async Task CompleteOpenShiftAsync(OpenShiftRequest request)
    {
        var wasUnresolved = _pendingOpenShift is not null;
        _pendingOpenShift = request;
        RefreshOperationState();
        var result = await _session.Api.OpenShiftAsync(request);
        if (result.Success || (!wasUnresolved && !result.OutcomeUnknown)) _pendingOpenShift = null;
        if (result.Success) _OpenShiftRequestId = Guid.NewGuid().ToString("N");
        RefreshOperationState();
        if (!result.Success || result.Value is null)
        {
            OpenShiftError = result.Error ?? "تعذر فتح الوردية.";
            return;
        }

        _session.ShiftId = result.Value.ShiftId;
        _session.ShiftName = result.Value.ShiftName;
        OnPropertyChanged(nameof(IsShiftOpen));
        _header.Refresh();
        await LoadMenuAsync();
    }

}
