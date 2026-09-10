using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Inventory;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// Stock on hand, physical count corrections and waste. Balance is never stored or computed here -
/// it is derived server-side from the movement log, so this screen reads it rather than tracking it.
/// The two write actions are deliberately distinct: a COUNT ADJUSTMENT records what a human actually
/// found on the shelf (the server trusts the observed direction and quantity, because it cannot
/// re-count a physical shelf the way it can recompute cash), while WASTE records a known loss valued
/// at the last purchase cost. Merging them would destroy the difference between "we miscounted" and
/// "we threw food away", which are different corrective actions.
/// </summary>
public partial class InventoryViewModel : ObservableObject
{
    private readonly AppSession _session;

    public InventoryViewModel(AppSession session) => _session = session;
    private readonly RetainedRequest<RecordCountAdjustmentRequest> _countRequest = new("inventory.count-adjustment");
    private readonly RetainedRequest<RecordWasteRequest> _wasteRequest = new("inventory.waste");
    public bool CanEditAction => !IsBusy && !_countRequest.HasPending && !_wasteRequest.HasPending;
    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanEditAction));
        _session.SetManagementOperationPending(this, !CanEditAction);
    }

    public ObservableCollection<InventoryItemDto> Items { get; } = new();
    public ObservableCollection<StockMovementDto> Movements { get; } = new();

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _loadError;
    [ObservableProperty] private string? _actionNote;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _searchQuery = "";
    [ObservableProperty] private bool _lowStockOnly;

    private readonly List<InventoryItemDto> _allItems = new();

    public bool HasNoItems => !IsLoading && LoadError is null && Items.Count == 0;
    public bool CanManage => _session.HasPermission("inventory_manage");

    partial void OnSearchQueryChanged(string value) => ApplyFilter();
    partial void OnLowStockOnlyChanged(bool value) => ApplyFilter();

    private void ApplyFilter()
    {
        Items.Clear();
        IEnumerable<InventoryItemDto> source = _allItems;
        if (LowStockOnly) source = source.Where(i => i.IsLowStock);
        if (!string.IsNullOrWhiteSpace(SearchQuery))
            source = source.Where(i => i.Name.Contains(SearchQuery, StringComparison.CurrentCultureIgnoreCase)
                                    || i.Category.Contains(SearchQuery, StringComparison.CurrentCultureIgnoreCase));
        foreach (var i in source) Items.Add(i);
        OnPropertyChanged(nameof(HasNoItems));
        OnPropertyChanged(nameof(LowStockCount));
    }

    public int LowStockCount => _allItems.Count(i => i.IsLowStock);

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        LoadError = null;
        try
        {
            var items = await _session.Api.GetInventoryItemsAsync();
            if (!items.Success || items.Value is null)
            {
                LoadError = items.Error ?? "تعذر تحميل المخزون.";
                return;
            }
            _allItems.Clear();
            _allItems.AddRange(items.Value);
            ApplyFilter();

            var movements = await _session.Api.GetStockMovementsAsync(limit: 50);
            if (!movements.Success || movements.Value is null) { LoadError = movements.Error ?? "تعذر تحميل حركات المخزون."; return; }
            Movements.Clear();
            if (movements.Success && movements.Value is not null)
                foreach (var m in movements.Value) Movements.Add(m);
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasNoItems));
        }
    }

    // ---------------- Physical count adjustment ----------------

    [ObservableProperty] private InventoryItemDto? _countTarget;
    [ObservableProperty] private string _countActualText = "";
    [ObservableProperty] private string _countReason = "";
    [ObservableProperty] private string _countManagerPin = "";
    [ObservableProperty] private string? _countError;
    public bool IsCountOpen => CountTarget is not null;
    partial void OnCountTargetChanged(InventoryItemDto? value) => OnPropertyChanged(nameof(IsCountOpen));

    private string? _countRequestId;

    public bool CountNeedsApproval => !CanManage;

    /// <summary>What the correction will post, shown before it is submitted: the server records the
    /// human's observed difference, so the operator should see the difference they are about to
    /// assert rather than discovering it afterwards in the movement log.</summary>
    public string CountDifferenceLabel
    {
        get
        {
            if (CountTarget is null || !MilliUnits.TryParse(CountActualText, out var actual)) return "";
            var diff = actual - CountTarget.BalanceMilliUnits;
            if (diff == 0) return "لا يوجد فرق عن رصيد النظام.";
            var direction = diff > 0 ? "زيادة" : "عجز";
            return $"{direction} {MilliUnits.Format(Math.Abs(diff))} {CountTarget.Unit}";
        }
    }

    partial void OnCountActualTextChanged(string value) => OnPropertyChanged(nameof(CountDifferenceLabel));

    [RelayCommand]
    private void OpenCount(InventoryItemDto item)
    {
        if (!CanEditAction) return;
        CountError = null;
        CountActualText = MilliUnits.Format(item.BalanceMilliUnits);
        CountReason = "";
        CountManagerPin = "";
        _countRequestId = $"cnt-{Guid.NewGuid():N}";
        CountTarget = item;
        OnPropertyChanged(nameof(CountNeedsApproval));
        OnPropertyChanged(nameof(CountDifferenceLabel));
    }

    [RelayCommand]
    private void CloseCount() { if (CanEditAction) CountTarget = null; }

    [RelayCommand]
    private async Task SubmitCountAsync()
    {
        if (IsBusy || _wasteRequest.HasPending) return;
        if (_countRequest.Value is { } pending)
        {
            IsBusy = true;
            try { await CompleteCountAsync(pending); }
            finally { IsBusy = false; }
            return;
        }
        if (CountTarget is null) return;
        if (!MilliUnits.TryParse(CountActualText, out var actual))
        {
            CountError = "أدخل الكمية المعدودة بشكل صحيح.";
            return;
        }
        if (string.IsNullOrWhiteSpace(CountReason))
        {
            CountError = "سبب التسوية إلزامي.";
            return;
        }

        var difference = actual - CountTarget.BalanceMilliUnits;
        if (difference == 0)
        {
            CountError = "لا يوجد فرق لتسجيله.";
            return;
        }

        IsBusy = true;
        CountError = null;
        try
        {
            var (approvalId, approverId, ok) = await ResolveApprovalAsync(
                CountNeedsApproval, CountManagerPin, "inventory_manage", CountReason, e => CountError = e);
            if (!ok) return;

            await CompleteCountAsync(new RecordCountAdjustmentRequest(
                _countRequestId!, CountTarget.Id, Math.Abs(difference), difference > 0 ? "In" : "Out",
                CountReason, _session.CurrentUser!.Id, approvalId, approverId));
        }
        finally { IsBusy = false; }
    }

    private async Task CompleteCountAsync(RecordCountAdjustmentRequest request)
    {
            CountError = null;
            var result = await _countRequest.SendAsync(request, r => _session.Api.RecordCountAdjustmentAsync(r));
            if (!result.Success || result.Value is null)
            {
                CountError = _countRequest.FailureMessage(result.Error);
                return;
            }
            ActionNote = "تم تسجيل تسوية الجرد.";
            CountTarget = null;
            _countRequestId = null;
            await LoadAsync();
    }

    // ---------------- Waste / spoilage / breakage ----------------

    [ObservableProperty] private InventoryItemDto? _wasteTarget;
    [ObservableProperty] private string _wasteQuantityText = "";
    [ObservableProperty] private string _wasteType = "Waste";
    [ObservableProperty] private string _wasteReason = "";
    [ObservableProperty] private string _wasteManagerPin = "";
    [ObservableProperty] private string? _wasteError;
    public bool IsWasteOpen => WasteTarget is not null;
    partial void OnWasteTargetChanged(InventoryItemDto? value) => OnPropertyChanged(nameof(IsWasteOpen));

    private string? _wasteRequestId;

    public bool WasteNeedsApproval => !CanManage;

    [RelayCommand]
    private void OpenWaste(InventoryItemDto item)
    {
        if (!CanEditAction) return;
        WasteError = null;
        WasteQuantityText = "";
        WasteType = "Waste";
        WasteReason = "";
        WasteManagerPin = "";
        _wasteRequestId = $"wst-{Guid.NewGuid():N}";
        WasteTarget = item;
        OnPropertyChanged(nameof(WasteNeedsApproval));
    }

    [RelayCommand]
    private void CloseWaste() { if (CanEditAction) WasteTarget = null; }

    [RelayCommand]
    private void SetWasteType(string type) { if (CanEditAction) WasteType = type; }

    [RelayCommand]
    private async Task SubmitWasteAsync()
    {
        if (IsBusy || _countRequest.HasPending) return;
        if (_wasteRequest.Value is { } pending)
        {
            IsBusy = true;
            try { await CompleteWasteAsync(pending); }
            finally { IsBusy = false; }
            return;
        }
        if (WasteTarget is null) return;
        if (!MilliUnits.TryParse(WasteQuantityText, out var quantity) || quantity <= 0)
        {
            WasteError = "أدخل الكمية التالفة بشكل صحيح.";
            return;
        }
        if (string.IsNullOrWhiteSpace(WasteReason))
        {
            WasteError = "سبب الفاقد إلزامي.";
            return;
        }

        IsBusy = true;
        WasteError = null;
        try
        {
            var (approvalId, approverId, ok) = await ResolveApprovalAsync(
                WasteNeedsApproval, WasteManagerPin, "inventory_manage", WasteReason, e => WasteError = e);
            if (!ok) return;

            await CompleteWasteAsync(new RecordWasteRequest(
                _wasteRequestId!, WasteTarget.Id, quantity, WasteType, WasteReason,
                _session.CurrentUser!.Id, approvalId, approverId));
        }
        finally { IsBusy = false; }
    }

    private async Task CompleteWasteAsync(RecordWasteRequest request)
    {
            WasteError = null;
            var result = await _wasteRequest.SendAsync(request, r => _session.Api.RecordWasteAsync(r));
            if (!result.Success || result.Value is null)
            {
                WasteError = _wasteRequest.FailureMessage(result.Error);
                return;
            }

            // "Unknown cost is not zero": an item with no purchase history posts the loss with a
            // quantity but no value, and the message must not imply a value was calculated.
            ActionNote = result.Value.LossValuePiastres is { } loss
                ? $"تم تسجيل الفاقد بقيمة {CashInput.Format(loss)} ج."
                : "تم تسجيل الفاقد (لا توجد تكلفة شراء سابقة لتقييمه).";
            WasteTarget = null;
            _wasteRequestId = null;
            await LoadAsync();
    }

    private async Task<(string? ApprovalId, string? ApproverUserId, bool Ok)> ResolveApprovalAsync(
        bool needsApproval, string managerPin, string permission, string reason, Action<string> onError)
    {
        if (!needsApproval) return (null, null, true);
        if (managerPin.Length < 4)
        {
            onError("أدخل رمز المدير المخوّل باعتماد هذا الإجراء.");
            return (null, null, false);
        }
        // One call, not two: the server checks the PIN and decides who approved. Verifying it
        // here and then naming the manager made the check a formality anything could skip.
        var approval = await _session.Api.IssueManagerApprovalAsync(
            new Falah.Application.Approvals.IssueManagerApprovalRequest(managerPin, permission, reason));
        if (!approval.Success || approval.Value is null)
        {
            onError(approval.Error ?? "رمز المدير غير صحيح أو غير مخوّل باعتماد هذا الإجراء.");
            return (null, null, false);
        }

        return (approval.Value.ApprovalId, approval.Value.ApproverUserId, true);
    }
}
