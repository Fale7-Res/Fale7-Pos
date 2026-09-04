using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Shifts;

namespace Falah.Client.CashierWinUI.ViewModels;

public partial class CashierViewModel
{
    [ObservableProperty] private string _openingCashText = "0";
    [ObservableProperty] private string? _openShiftError;
    [ObservableProperty] private bool _isOpeningShift;

    [RelayCommand]
    private async Task OpenShiftAsync()
    {
        if (!decimal.TryParse(OpeningCashText, out var openingEgp) || openingEgp < 0)
        {
            OpenShiftError = "أدخل رصيدًا افتتاحيًا صحيحًا.";
            return;
        }

        IsOpeningShift = true;
        OpenShiftError = null;
        try
        {
            var request = new OpenShiftRequest(
                RequestId: $"open-{Guid.NewGuid():N}",
                ShiftName: DateTime.Now.Hour < 16 ? "صباحية" : "مسائية",
                OpenedByUserId: _session.CurrentUser!.Id,
                OpeningCashPiastres: (long)Math.Round(openingEgp * 100m));

            var result = await _session.Api.OpenShiftAsync(request);
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
        finally
        {
            IsOpeningShift = false;
        }
    }
}
