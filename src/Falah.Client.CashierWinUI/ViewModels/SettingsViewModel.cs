using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Settings;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// System settings - the single system-wide row. The update command is a PARTIAL update (null means
/// "leave unchanged"), so this screen sends only the fields it actually edits and never risks
/// blanking a field it does not show.
///
/// Money fields are piastres in the API and edited in pounds here, so they round-trip through
/// CashInput rather than being typed as raw piastres. Values that feed real financial or payroll
/// behaviour (the cash-discrepancy threshold that decides when a shift close needs manager
/// approval, the late-penalty and overtime multipliers) are edited here but never interpreted
/// client-side - the server remains the only thing that applies them.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly AppSession _session;
    private readonly HeaderViewModel _header;
    private SettingsDto? _loaded;

    public SettingsViewModel(AppSession session, HeaderViewModel header)
    {
        _session = session;
        _header = header;
        InitializeAppearance();
    }

    /// <summary>Implemented in SettingsViewModel.Appearance.cs - the local, non-server theme
    /// preference this screen also exposes.</summary>
    partial void InitializeAppearance();

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isSaving;
    [ObservableProperty] private string? _loadError;
    [ObservableProperty] private string? _saveError;
    [ObservableProperty] private string? _savedNote;

    [ObservableProperty] private string _restaurantName = "";
    [ObservableProperty] private string _subName = "";
    [ObservableProperty] private string _address = "";
    [ObservableProperty] private string _phone1 = "";
    [ObservableProperty] private string _phone2 = "";
    [ObservableProperty] private string _phoneLandline = "";
    [ObservableProperty] private string _complaintsPhone = "";
    [ObservableProperty] private string _receiptFooter = "";
    [ObservableProperty] private string _menuUrl = "";
    [ObservableProperty] private string _deliveryFeeText = "";
    /// <summary>The float the open-shift dialog starts from. Only a suggestion: each shift stores
    /// what the cashier actually entered, and changing this never touches a shift already opened.</summary>
    [ObservableProperty] private string _defaultOpeningCashText = "";
    [ObservableProperty] private string _cashDiscrepancyThresholdText = "";
    [ObservableProperty] private string _overtimeHourlyRateText = "";
    [ObservableProperty] private string _autoLockMinutesText = "";
    [ObservableProperty] private string _numberOfReceiptCopiesText = "";
    [ObservableProperty] private bool _autoPrintReceipt;
    [ObservableProperty] private bool _autoPrintPreparationTicket;

    /// <summary>Whether the kitchen waits for an unconfirmed electronic transfer. Off by default,
    /// which is what this system has always done - the owner decides, not the software.</summary>
    [ObservableProperty] private bool _holdPreparationUntilPaymentConfirmed;

    public bool CanEdit => _session.HasPermission("settings_manage");
    public bool HasData => _loaded is not null;

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        LoadError = null;
        try
        {
            var result = await _session.Api.GetSettingsAsync();
            if (!result.Success || result.Value is null)
            {
                LoadError = result.Error ?? "تعذر تحميل الإعدادات.";
                return;
            }
            var s = result.Value;
            _loaded = s;

            RestaurantName = s.RestaurantName;
            SubName = s.SubName;
            Address = s.Address;
            Phone1 = s.Phone1;
            Phone2 = s.Phone2;
            PhoneLandline = s.PhoneLandline;
            ComplaintsPhone = s.ComplaintsPhone;
            ReceiptFooter = s.ReceiptFooter;
            MenuUrl = s.MenuUrl ?? "";
            DeliveryFeeText = CashInput.Format(s.DeliveryFeePiastres);
            CashDiscrepancyThresholdText = CashInput.Format(s.CashDiscrepancyThresholdPiastres);
            OvertimeHourlyRateText = CashInput.Format(s.OvertimeHourlyRatePiastres);
            DefaultOpeningCashText = CashInput.Format(s.DefaultOpeningCashPiastres);
            AutoLockMinutesText = s.AutoLockMinutes.ToString();
            NumberOfReceiptCopiesText = s.NumberOfReceiptCopies.ToString();
            AutoPrintReceipt = s.AutoPrintReceipt;
            AutoPrintPreparationTicket = s.AutoPrintPreparationTicket;
            HoldPreparationUntilPaymentConfirmed = s.HoldPreparationUntilPaymentConfirmed;

            OnPropertyChanged(nameof(HasData));
            OnPropertyChanged(nameof(CanEdit));
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsSaving || _loaded is null) return;

        if (!CashInput.TryParse(DefaultOpeningCashText, out var defaultOpeningCash) || defaultOpeningCash < 0)
        {
            SaveError = "أدخل رصيدًا افتتاحيًا افتراضيًا صحيحًا (صفر أو أكثر).";
            return;
        }
        if (!CashInput.TryParse(DeliveryFeeText, out var deliveryFee)
            || !CashInput.TryParse(CashDiscrepancyThresholdText, out var threshold)
            || !CashInput.TryParse(OvertimeHourlyRateText, out var overtimeRate))
        {
            SaveError = "تحقق من صحة المبالغ المدخلة.";
            return;
        }
        if (!int.TryParse(AutoLockMinutesText, out var autoLock) || autoLock < 0
            || !int.TryParse(NumberOfReceiptCopiesText, out var copies) || copies < 1)
        {
            SaveError = "تحقق من صحة الأرقام المدخلة.";
            return;
        }

        IsSaving = true;
        SaveError = null;
        SavedNote = null;
        try
        {
            var result = await _session.Api.UpdateSettingsAsync(new UpdateSettingsRequest(
                ActorUserId: _session.CurrentUser!.Id,
                RestaurantName: RestaurantName,
                SubName: SubName,
                Address: Address,
                Phone1: Phone1,
                Phone2: Phone2,
                PhoneLandline: PhoneLandline,
                ComplaintsPhone: ComplaintsPhone,
                ReceiptFooter: ReceiptFooter,
                // Always sent: an empty box is a deliberate "no menu link", which clears it.
                MenuUrl: MenuUrl,
                OvertimeHourlyRatePiastres: overtimeRate,
                DeliveryFeePiastres: deliveryFee,
                DefaultOpeningCashPiastres: defaultOpeningCash,
                CashDiscrepancyThresholdPiastres: threshold,
                AutoLockMinutes: autoLock,
                NumberOfReceiptCopies: copies,
                AutoPrintReceipt: AutoPrintReceipt,
                AutoPrintPreparationTicket: AutoPrintPreparationTicket,
                HoldPreparationUntilPaymentConfirmed: HoldPreparationUntilPaymentConfirmed));

            if (!result.Success || result.Value is null)
            {
                SaveError = result.Error ?? "تعذر حفظ الإعدادات.";
                return;
            }
            _loaded = result.Value;
            SavedNote = "تم حفظ الإعدادات.";

            // The header shows the restaurant identity from these same settings - refresh it so the
            // rename is visible immediately rather than only after the next login.
            _header.SetIdentity(result.Value.RestaurantName);
        }
        finally { IsSaving = false; }
    }
}
