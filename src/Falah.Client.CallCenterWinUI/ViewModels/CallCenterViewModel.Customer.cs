using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Customers;
using Falah.Application.Delivery;

namespace Falah.Client.CallCenterWinUI.ViewModels;

/// <summary>
/// Customer/address intake: phone-first lookup (a returning caller is recognized the instant enough
/// digits are typed, matching CustomerQueryService.FindByPhoneAsync's own "before checkout, before
/// any name is entered" design - see its doc comment), then the structured delivery-address parts
/// CheckoutOrderHandler actually requires for a Delivery order. DeliveryArea is kept as its own real
/// field here (unlike CashierWinUI's own delivery intake, which sends the delivery SERVICE's name as
/// the area - a known, separately tracked defect this station does not repeat).
/// </summary>
public partial class CallCenterViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResolvedCustomer))]
    private string? _customerId;

    [ObservableProperty] private string _customerPhone = "";
    [ObservableProperty] private string _customerName = "";
    [ObservableProperty] private bool _isLookingUpCustomer;
    [ObservableProperty] private string? _customerLookupNote;

    public bool HasResolvedCustomer => CustomerId is not null;
    public ObservableCollection<CustomerAddressDto> SavedAddresses { get; } = new();

    private string? _lastLookedUpPhone;
    private long _customerLookupVersion;

    /// <summary>Egyptian mobile numbers are 11 digits - firing the lookup any earlier would just be
    /// wasted round trips against a phone prefix that matches nothing yet.</summary>
    partial void OnCustomerPhoneChanged(string value)
    {
        ++_customerLookupVersion;
        IsLookingUpCustomer = false;
        ResetResolvedCustomer();
        NotifyDeliverySummary();
        if (value.Length >= 11 && value != _lastLookedUpPhone)
            _ = LookupCustomerAsync();
        else if (value.Length < 11)
            _lastLookedUpPhone = null;
    }

    partial void OnCustomerNameChanged(string value) => NotifyDeliverySummary();
    partial void OnDeliveryAreaChanged(string value) => NotifyDeliverySummary();
    partial void OnDeliveryStreetChanged(string value) => NotifyDeliverySummary();

    [RelayCommand]
    private Task LookupCustomerAsync() => LookupCustomerCoreAsync();

    private async Task LookupCustomerCoreAsync()
    {
        if (string.IsNullOrWhiteSpace(CustomerPhone)) return;
        var requestedPhone = CustomerPhone;
        var version = ++_customerLookupVersion;
        _lastLookedUpPhone = requestedPhone;
        IsLookingUpCustomer = true;
        CustomerLookupNote = null;
        try
        {
            var result = await _session.Api.LookupCustomerAsync(requestedPhone);
            if (version != _customerLookupVersion || CustomerPhone != requestedPhone) return;
            if (!result.Success || result.Value is null)
            {
                ResetResolvedCustomer();
                CustomerLookupNote = "عميل جديد - أدخل الاسم والعنوان.";
                return;
            }

            CustomerId = result.Value.Id;
            CustomerName = result.Value.Name;
            SavedAddresses.Clear();
            foreach (var address in result.Value.Addresses.OrderByDescending(a => a.LastUsedAtUtc))
                SavedAddresses.Add(address);
            CustomerLookupNote = SavedAddresses.Count > 0
                ? $"عميل معروف - {SavedAddresses.Count} عنوان محفوظ."
                : "عميل معروف - لا يوجد عنوان محفوظ بعد.";
        }
        finally
        {
            if (version == _customerLookupVersion) IsLookingUpCustomer = false;
        }
    }

    private void ResetResolvedCustomer()
    {
        CustomerId = null;
        SavedAddresses.Clear();
        SelectedSavedAddress = null;
        CustomerLookupNote = null;
    }

    // ---------------- Caller/address editor ----------------

    /// <summary>The address form lives in its own full-screen editor rather than stacked above the
    /// item list: on a 15" station the order lines are what the operator watches while the customer
    /// talks, and an always-expanded seven-field address form starved that list down to a couple of
    /// visible rows. Same split the Cashier station uses for its own delivery details.</summary>
    [ObservableProperty] private bool _isDeliveryEditorOpen;
    [ObservableProperty] private string? _deliveryError;

    [ObservableProperty]
    private CustomerAddressDto? _selectedSavedAddress;

    partial void OnSelectedSavedAddressChanged(CustomerAddressDto? value)
    {
        if (value is not null) ApplySavedAddress(value);
    }

    [RelayCommand]
    private void OpenDeliveryEditor()
    {
        if (!CanEditOperation) return;
        DeliveryError = null;
        IsDeliveryEditorOpen = true;
    }

    [RelayCommand]
    private void CloseDeliveryEditor() => IsDeliveryEditorOpen = false;

    /// <summary>Validates against the SAME field order the server enforces
    /// (DeliveryRules.FirstMissingDeliveryField: phone → name → service → area → street), so the
    /// operator is told what is missing here instead of at submit time.</summary>
    [RelayCommand]
    private void SaveDeliveryDetails()
    {
        DeliveryError = FirstMissingDeliveryField();
        if (DeliveryError is not null) return;
        IsDeliveryEditorOpen = false;
        NotifyDeliverySummary();
    }

    private string? FirstMissingDeliveryField()
    {
        if (string.IsNullOrWhiteSpace(CustomerPhone)) return "أدخل رقم هاتف العميل قبل المتابعة.";
        if (string.IsNullOrWhiteSpace(CustomerName)) return "أدخل اسم العميل قبل المتابعة.";
        // A pickup is matched at the counter by name and phone (DeliveryRules.FirstMissingPickupField).
        if (IsPickup) return null;
        if (SelectedDeliveryService is null) return "اختر خدمة التوصيل قبل المتابعة.";
        if (string.IsNullOrWhiteSpace(DeliveryArea)) return "أدخل المنطقة قبل المتابعة.";
        if (string.IsNullOrWhiteSpace(DeliveryStreet)) return "أدخل الشارع قبل المتابعة.";
        return null;
    }

    public bool HasDeliveryDetails => FirstMissingDeliveryField() is null;

    /// <summary>What the compact button in the order panel shows: either the caller and where the
    /// order is going, or a plain prompt when nothing has been captured yet.</summary>
    public string DeliverySummary
    {
        get
        {
            if (string.IsNullOrWhiteSpace(CustomerPhone) && string.IsNullOrWhiteSpace(CustomerName))
                return IsPickup ? "اضغط لإدخال اسم العميل وموعد الاستلام" : "اضغط لإدخال بيانات العميل والعنوان";
            var who = string.IsNullOrWhiteSpace(CustomerName) ? CustomerPhone : $"{CustomerName} • {CustomerPhone}";
            if (IsPickup)
                return string.Join("\n", new[] { who, PickupTimeLine ?? "يستلم أول ما يوصل (بدون موعد)" });
            var where = string.Join("، ", new[] { DeliveryArea, DeliveryStreet }.Where(p => !string.IsNullOrWhiteSpace(p)));
            var service = SelectedDeliveryService?.Name;
            var parts = new[] { who, where, service }.Where(p => !string.IsNullOrWhiteSpace(p));
            return string.Join("\n", parts);
        }
    }

    private void NotifyDeliverySummary()
    {
        OnPropertyChanged(nameof(DeliverySummary));
        OnPropertyChanged(nameof(HasDeliveryDetails));
    }

    [RelayCommand]
    private void SelectSavedAddress(CustomerAddressDto address) => ApplySavedAddress(address);

    private void ApplySavedAddress(CustomerAddressDto address)
    {
        DeliveryArea = address.Area;
        DeliveryStreet = address.Street;
        DeliveryBuilding = address.Building ?? "";
        DeliveryFloor = address.Floor ?? "";
        DeliveryApartment = address.Apartment ?? "";
        DeliveryLandmark = address.Landmark ?? "";
        if (address.ServiceId is not null)
            SelectedDeliveryService = DeliveryServices.FirstOrDefault(s => s.Id == address.ServiceId);
        NotifyDeliverySummary();
    }

    // ---------------- Delivery address ----------------

    [ObservableProperty] private string _deliveryArea = "";
    [ObservableProperty] private string _deliveryStreet = "";
    [ObservableProperty] private string _deliveryBuilding = "";
    [ObservableProperty] private string _deliveryFloor = "";
    [ObservableProperty] private string _deliveryApartment = "";
    [ObservableProperty] private string _deliveryLandmark = "";
    [ObservableProperty] private string _deliveryNotes = "";

    public ObservableCollection<DeliveryServiceDto> DeliveryServices { get; } = new();
    [ObservableProperty] private DeliveryServiceDto? _selectedDeliveryService;
    [ObservableProperty] private string? _deliveryServicesError;

    partial void OnSelectedDeliveryServiceChanged(DeliveryServiceDto? value)
    {
        OnPropertyChanged(nameof(GrandTotalPiastres));
        OnPropertyChanged(nameof(GrandTotalDisplay));
        OnPropertyChanged(nameof(DeliveryFeeDisplay));
        NotifyDeliverySummary();
    }

    [RelayCommand]
    private void SelectDeliveryService(DeliveryServiceDto service) => SelectedDeliveryService = service;

    [RelayCommand]
    private async Task LoadDeliveryServicesAsync()
    {
        var result = await _session.Api.GetActiveDeliveryServicesAsync();
        if (!result.Success || result.Value is null)
        {
            DeliveryServicesError = result.Error ?? "تعذر تحميل خدمات التوصيل.";
            return;
        }
        DeliveryServicesError = null;
        // Keep the operator's choice, but bound to the freshly loaded copy: the old object carries the old fee.
        var selectedId = SelectedDeliveryService?.Id;
        DeliveryServices.Clear();
        foreach (var s in result.Value) DeliveryServices.Add(s);
        SelectedDeliveryService = selectedId is null ? null : DeliveryServices.FirstOrDefault(s => s.Id == selectedId);
    }

    private void ResetCustomerAndDelivery()
    {
        CustomerPhone = "";
        CustomerName = "";
        _lastLookedUpPhone = null;
        ResetResolvedCustomer();
        DeliveryArea = "";
        DeliveryStreet = "";
        DeliveryBuilding = "";
        DeliveryFloor = "";
        DeliveryApartment = "";
        DeliveryLandmark = "";
        DeliveryNotes = "";
        SelectedDeliveryService = null;
        PickupDate = null;
        PickupTime = null;
        // The next call starts as the common case again.
        OrderType = "delivery";
    }
}
