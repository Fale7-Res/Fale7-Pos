using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Delivery;
using Falah.Application.Customers;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// Delivery order-type details: customer association and the structured address parts the server
/// actually composes the final DeliveryAddress from (see CheckoutOrderRequest's own doc comment - a
/// raw free-text address is never trusted, only Street/Building/Floor/Apartment/Landmark/Notes are).
/// Delivery-area pricing/service selection reuses the real backend delivery-services list rather
/// than a client-side guess at fees, matching "the client never computes a price."
/// </summary>
public partial class CashierViewModel
{
    [ObservableProperty] private string _customerName = "";
    [ObservableProperty] private string _customerPhone = "";
    [ObservableProperty] private string _deliveryArea = "";
    [ObservableProperty] private bool _isDeliveryEditorOpen;
    [ObservableProperty] private bool _isLoadingDelivery;
    [ObservableProperty] private string? _deliveryError;
    [ObservableProperty] private string? _customerLookupMessage;
    [ObservableProperty] private string _deliveryStreet = "";
    [ObservableProperty] private string _deliveryBuilding = "";
    [ObservableProperty] private string _deliveryFloor = "";
    [ObservableProperty] private string _deliveryApartment = "";
    [ObservableProperty] private string _deliveryLandmark = "";
    [ObservableProperty] private string _deliveryNotes = "";

    public ObservableCollection<DeliveryServiceDto> DeliveryServices { get; } = new();
    [ObservableProperty] private DeliveryServiceDto? _selectedDeliveryService;
    public ObservableCollection<CustomerAddressDto> SavedAddresses { get; } = new();
    [ObservableProperty] private CustomerAddressDto? _selectedSavedAddress;
    public string DeliverySummary => string.IsNullOrWhiteSpace(CustomerPhone)
        ? "أضف العميل وعنوان التوصيل" : $"{CustomerName} · {CustomerPhone}\n{DeliveryArea}، {DeliveryStreet}";

    partial void OnOrderTypeChanged(string value)
    {
        NotifyTotals();
        OnPropertyChanged(nameof(IsDelivery));
        OnPropertyChanged(nameof(IsDineIn));
        OnPropertyChanged(nameof(OrderTypeLabel));
        // A table number left over from the last dine-in order must not ride along on a takeaway.
        if (value != "dine_in") TableNumber = string.Empty;
        if (value == "delivery") OpenDeliveryEditor();
    }

    partial void OnSelectedDeliveryServiceChanged(DeliveryServiceDto? value) => NotifyTotals();
    partial void OnCustomerPhoneChanged(string value)
    {
        SavedAddresses.Clear(); SelectedSavedAddress = null; CustomerLookupMessage = null;
    }
    partial void OnSelectedSavedAddressChanged(CustomerAddressDto? value)
    {
        if (value is null) return;
        DeliveryArea = value.Area; DeliveryStreet = value.Street;
        DeliveryBuilding = value.Building ?? ""; DeliveryFloor = value.Floor ?? "";
        DeliveryApartment = value.Apartment ?? ""; DeliveryLandmark = value.Landmark ?? "";
        SelectedDeliveryService = DeliveryServices.FirstOrDefault(s => s.Id == value.ServiceId);
    }
    [RelayCommand] private void OpenDeliveryEditor()
    {
        if (!CanEditOperation) return;
        IsDeliveryEditorOpen = true;
        _ = LoadDeliveryServicesAsync();
    }
    [RelayCommand] private void CloseDeliveryEditor() => IsDeliveryEditorOpen = false;
    [RelayCommand]
    private async Task LoadDeliveryServicesAsync()
    {
        if (IsLoadingDelivery) return;
        IsLoadingDelivery = true; DeliveryError = null;
        var selectedId = SelectedDeliveryService?.Id;
        try
        {
            var result = await _session.Api.GetActiveDeliveryServicesAsync();
            if (!result.Success || result.Value is null) { DeliveryError = result.Error; return; }
            DeliveryServices.Clear();
            foreach (var s in result.Value) DeliveryServices.Add(s);
            SelectedDeliveryService = DeliveryServices.FirstOrDefault(s => s.Id == selectedId);
        }
        finally { IsLoadingDelivery = false; }
    }

    [RelayCommand] private async Task LookupCustomerAsync()
    {
        var phone = CustomerPhone.Trim();
        if (phone.Length == 0) { DeliveryError = "أدخل رقم الهاتف أولاً."; return; }
        var result = await _session.Api.LookupCustomerAsync(phone);
        if (phone != CustomerPhone.Trim()) return;
        SavedAddresses.Clear();
        if (!result.Success || result.Value is null)
        {
            CustomerLookupMessage = result.StatusCode == 404 ? "عميل جديد — أدخل الاسم والعنوان" : result.Error;
            return;
        }
        CustomerName = result.Value.Name;
        foreach (var address in result.Value.Addresses) SavedAddresses.Add(address);
        CustomerLookupMessage = "تم العثور على العميل؛ اختر عنواناً محفوظاً أو أضف عنواناً.";
    }

    [RelayCommand] private async Task SaveDeliveryDetailsAsync()
    {
        if (IsLoadingDelivery || !CanEditOperation) return;
        if (string.IsNullOrWhiteSpace(CustomerPhone) || string.IsNullOrWhiteSpace(CustomerName) ||
            string.IsNullOrWhiteSpace(DeliveryArea) || string.IsNullOrWhiteSpace(DeliveryStreet) || SelectedDeliveryService is null)
        { DeliveryError = "أكمل الاسم والهاتف والمنطقة والشارع وخدمة التوصيل."; return; }
        IsLoadingDelivery = true;
        try
        {
            var result = await _session.Api.SaveCustomerAsync(new UpsertCustomerRequest(CustomerPhone.Trim(), CustomerName.Trim(),
                DeliveryArea, DeliveryStreet, NullIfEmpty(DeliveryBuilding), NullIfEmpty(DeliveryFloor),
                NullIfEmpty(DeliveryApartment), NullIfEmpty(DeliveryLandmark), SelectedDeliveryService.Id, SelectedDeliveryService.Name));
            if (!result.Success) { DeliveryError = result.Error; return; }
            OnPropertyChanged(nameof(DeliverySummary)); IsDeliveryEditorOpen = false;
        }
        finally { IsLoadingDelivery = false; }
    }

    private void ResetDelivery()
    {
        CustomerName = CustomerPhone = DeliveryArea = DeliveryStreet = DeliveryBuilding = DeliveryFloor =
            DeliveryApartment = DeliveryLandmark = DeliveryNotes = "";
        SelectedDeliveryService = null; DeliveryError = null;
        OnPropertyChanged(nameof(DeliverySummary));
    }

    [RelayCommand]
    private void SelectDeliveryService(DeliveryServiceDto service) => SelectedDeliveryService = service;
}
