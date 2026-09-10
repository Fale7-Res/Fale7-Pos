using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Delivery;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// The delivery directory: the drivers who can be assigned, and the delivery services whose name
/// and fee every delivery order freezes a copy of at checkout. Both records existed on the server
/// with full create/update routes, but no station could reach them - a new driver or a new area
/// could not be added from the product at all.
///
/// Neither record is ever hard-deleted. Retiring is `Active = false`, matching the same soft-archive
/// idiom categories, products and drivers already use, and it deliberately does not touch history:
/// an order that was sold with a service keeps the name and fee it was actually charged.
///
/// `delivery_manage` authorises both. A user without it can still act with a manager's approval,
/// which is the server's own fallback (AuthorizeCommand checks the permission first and only then
/// looks for an approval) - so the manager PIN is asked for exactly when it is actually needed.
/// </summary>
public partial class DeliveryDirectoryViewModel : ObservableObject
{
    private readonly AppSession _session;
    private readonly RetainedRequest<object> _write = new("delivery.directory");

    public DeliveryDirectoryViewModel(AppSession session) => _session = session;

    public event Action? Changed;

    public ObservableCollection<DriverDto> Drivers { get; } = new();
    public ObservableCollection<DeliveryServiceDto> Services { get; } = new();

    [ObservableProperty] private bool _isOpen;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isReady;
    [ObservableProperty] private string _tab = "drivers";
    [ObservableProperty] private string? _loadError;
    [ObservableProperty] private string? _actionError;
    [ObservableProperty] private string? _actionNote;

    [ObservableProperty] private string _driverName = "";
    [ObservableProperty] private string _serviceName = "";
    [ObservableProperty] private string _serviceFeeText = "";
    [ObservableProperty] private bool _serviceActive = true;
    [ObservableProperty] private string _managerPin = "";
    private DeliveryServiceDto? _editingService;

    public bool IsDriversTab => Tab == "drivers";
    public bool IsServicesTab => Tab == "services";
    public bool CanOpen => _session.HasPermission("delivery_manage");
    public bool CanAct => !IsBusy && !IsLoading && IsReady;
    /// <summary>The server authorises on the permission first, so the PIN is only genuinely needed
    /// when this operator does not hold `delivery_manage` themselves.</summary>
    public bool NeedsApproval => !_session.HasPermission("delivery_manage");
    public bool IsEditingService => _editingService is not null;
    public string ServiceFormTitle => _editingService is null ? "خدمة توصيل جديدة" : "تعديل خدمة التوصيل";
    public string SaveLabel => IsBusy ? "جاري الحفظ والتحقق..."
        : _write.HasPending ? "التحقق من الحفظ السابق" : "حفظ";

    partial void OnIsBusyChanged(bool value) => NotifyState();
    partial void OnIsLoadingChanged(bool value) => NotifyState();

    private void NotifyState()
    {
        foreach (var name in new[] { nameof(CanAct), nameof(SaveLabel), nameof(IsEditingService), nameof(ServiceFormTitle) })
            OnPropertyChanged(name);
        _session.SetManagementOperationPending(this, IsBusy || _write.HasPending);
    }

    public async Task OpenAsync()
    {
        if (!CanOpen || _session.HasUnresolvedOperation) return;
        IsOpen = true;
        ActionError = ActionNote = null;
        ResetServiceForm();
        DriverName = "";
        ManagerPin = "";
        OnPropertyChanged(nameof(NeedsApproval));
        await LoadAsync();
    }

    [RelayCommand]
    private void Close()
    {
        if (IsBusy || _write.HasPending) return;
        IsOpen = false;
    }

    [RelayCommand]
    private void SetTab(string tab)
    {
        if (IsBusy || _write.HasPending || tab is not ("drivers" or "services")) return;
        Tab = tab;
        ActionError = ActionNote = null;
        OnPropertyChanged(nameof(IsDriversTab));
        OnPropertyChanged(nameof(IsServicesTab));
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        IsReady = false;
        LoadError = null;
        try
        {
            var drivers = await _session.Api.GetAllDriversAsync();
            if (!drivers.Success || drivers.Value is null) { LoadError = drivers.Error ?? "تعذر تحميل السائقين."; return; }
            var services = await _session.Api.GetAllDeliveryServicesAsync();
            if (!services.Success || services.Value is null) { LoadError = services.Error ?? "تعذر تحميل خدمات التوصيل."; return; }

            Drivers.Clear();
            foreach (var d in drivers.Value) Drivers.Add(d);
            Services.Clear();
            foreach (var s in services.Value) Services.Add(s);
            IsReady = true;
        }
        finally { IsLoading = false; }
    }

    private void ResetServiceForm()
    {
        _editingService = null;
        ServiceName = "";
        ServiceFeeText = "";
        ServiceActive = true;
        NotifyState();
    }

    [RelayCommand]
    private void NewService()
    {
        if (!CanAct || _write.HasPending) return;
        ActionError = ActionNote = null;
        ResetServiceForm();
    }

    [RelayCommand]
    private void SelectService(DeliveryServiceDto service)
    {
        if (!CanAct || _write.HasPending || service is null) return;
        ActionError = ActionNote = null;
        _editingService = service;
        ServiceName = service.Name;
        ServiceFeeText = service.FeePiastres is { } fee ? CashInput.Format(fee) : "";
        ServiceActive = service.Active;
        NotifyState();
    }

    [RelayCommand]
    private async Task AddDriverAsync()
    {
        if (IsBusy) return;
        ActionError = null;
        var request = _write.Value ?? BuildDriverRequest();
        if (request is null) return;
        await SubmitAsync(request, "تمت إضافة السائق.");
    }

    private object? BuildDriverRequest()
    {
        if (!CanAct || _session.CurrentUser is null) return null;
        if (string.IsNullOrWhiteSpace(DriverName)) { ActionError = "اسم السائق مطلوب."; return null; }
        return new CreateDriverRequest($"drv-{Guid.NewGuid():N}", DriverName.Trim(), _session.CurrentUser.Id);
    }

    [RelayCommand]
    private async Task ToggleDriverAsync(DriverDto driver)
    {
        if (IsBusy || driver is null) return;
        ActionError = null;
        var request = _write.Value ?? (object)new SetDriverActiveRequest(driver.Id, !driver.Active, _session.CurrentUser!.Id);
        await SubmitAsync(request, "تم تحديث حالة السائق.");
    }

    [RelayCommand]
    private async Task SaveServiceAsync()
    {
        if (IsBusy) return;
        ActionError = null;
        if (_write.Value is { } pending) { await SubmitAsync(pending, "تم حفظ خدمة التوصيل."); return; }
        if (!CanAct || _session.CurrentUser is null) return;
        if (string.IsNullOrWhiteSpace(ServiceName)) { ActionError = "اسم خدمة التوصيل مطلوب."; return; }

        long? fee = null;
        if (!string.IsNullOrWhiteSpace(ServiceFeeText))
        {
            if (!CashInput.TryParse(ServiceFeeText, out var parsed) || parsed < 0)
            { ActionError = "أدخل رسوم التوصيل بشكل صحيح، أو اتركها فارغة."; return; }
            fee = parsed;
        }

        IsBusy = true;
        try
        {
            var (approvalId, approverId, ok) = await ResolveApprovalAsync("تعديل خدمات التوصيل");
            if (!ok) return;
            var actor = _session.CurrentUser.Id;
            object request = _editingService is { } existing
                ? new UpdateDeliveryServiceRequest(existing.Id, ServiceName.Trim(), fee, ServiceActive, actor, approvalId, approverId)
                : new CreateDeliveryServiceRequest($"dsv-{Guid.NewGuid():N}", ServiceName.Trim(), fee, actor, approvalId, approverId);
            await CompleteAsync(request, "تم حفظ خدمة التوصيل.");
        }
        finally { IsBusy = false; }
    }

    private async Task SubmitAsync(object request, string note)
    {
        IsBusy = true;
        try { await CompleteAsync(request, note); }
        finally { IsBusy = false; }
    }

    private async Task CompleteAsync(object request, string note)
    {
        ActionError = ActionNote = null;
        var result = await _write.SendAsync(request, SendWriteAsync);
        if (!result.Success) { ActionError = _write.FailureMessage(result.Error); return; }
        ActionNote = note;
        DriverName = "";
        ManagerPin = "";
        ResetServiceForm();
        await LoadAsync();
        Changed?.Invoke();
    }

    private async Task<ApiClient.ApiResult<bool>> SendWriteAsync(object request)
    {
        static ApiClient.ApiResult<bool> Confirm<T>(ApiClient.ApiResult<T> r) =>
            new(r.Success, r.Success, r.Error, r.OutcomeUnknown, r.StatusCode);

        return request switch
        {
            CreateDriverRequest r => Confirm(await _session.Api.CreateDriverAsync(r)),
            SetDriverActiveRequest r => Confirm(await _session.Api.SetDriverActiveAsync(r)),
            CreateDeliveryServiceRequest r => Confirm(await _session.Api.CreateDeliveryServiceAsync(r)),
            UpdateDeliveryServiceRequest r => Confirm(await _session.Api.UpdateDeliveryServiceAsync(r)),
            _ => throw new InvalidOperationException("Unsupported delivery directory command."),
        };
    }

    private async Task<(string? ApprovalId, string? ApproverUserId, bool Ok)> ResolveApprovalAsync(string reason)
    {
        if (!NeedsApproval) return (null, null, true);
        if (ManagerPin.Length < 4)
        {
            ActionError = "أدخل رمز المدير المخوّل باعتماد هذا الإجراء.";
            return (null, null, false);
        }
        var approval = await _session.Api.IssueManagerApprovalAsync(
            new Falah.Application.Approvals.IssueManagerApprovalRequest(ManagerPin, "delivery_manage", reason));
        if (!approval.Success || approval.Value is null)
        {
            ActionError = approval.Error ?? "رمز المدير غير صحيح أو غير مخوّل باعتماد هذا الإجراء.";
            return (null, null, false);
        }
        return (approval.Value.ApprovalId, approval.Value.ApproverUserId, true);
    }
}
