using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Inventory;
using Falah.Application.Purchasing;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// Suppliers and their invoices. Receiving a purchase posts three facts atomically server-side (a
/// stock receipt per line, a supplier-ledger invoice, and - only if something was actually paid at
/// receipt - a treasury movement), so this screen never tries to stage those separately.
///
/// "آجل" (credit) is a real payment method here, not an absent one: it means nothing moves through
/// treasury and the whole invoice starts outstanding on the supplier ledger. Outstanding is always
/// read back from the server, never tracked locally, because a payment must never be allowed to
/// overshoot what is genuinely still owed.
/// </summary>
public partial class PurchasingViewModel : ObservableObject
{
    private readonly AppSession _session;

    public PurchasingViewModel(AppSession session) => _session = session;
    private readonly RetainedRequest<AddSupplierRequest> _supplierRequest = new("purchasing.supplier");
    private readonly RetainedRequest<ReceivePurchaseRequest> _receiveRequest = new("purchasing.receive");
    private readonly RetainedRequest<PayPurchaseRequest> _payRequest = new("purchasing.pay");
    public bool CanEditAction => !IsBusy && !_supplierRequest.HasPending && !_receiveRequest.HasPending && !_payRequest.HasPending;
    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanEditAction));
        _session.SetManagementOperationPending(this, !CanEditAction);
    }

    public ObservableCollection<SupplierDto> Suppliers { get; } = new();
    public ObservableCollection<PurchaseDto> Purchases { get; } = new();
    public ObservableCollection<InventoryItemDto> InventoryItems { get; } = new();

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _loadError;
    [ObservableProperty] private string? _actionNote;
    [ObservableProperty] private bool _isBusy;

    public bool HasNoSuppliers => !IsLoading && LoadError is null && Suppliers.Count == 0;
    public bool CanManage => _session.HasPermission("purchasing_manage");
    public bool CanAddSupplier => CanManage || _session.HasPermission("suppliers_manage");

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        LoadError = null;
        try
        {
            var suppliers = await _session.Api.GetSuppliersAsync();
            if (!suppliers.Success || suppliers.Value is null)
            {
                LoadError = suppliers.Error ?? "تعذر تحميل الموردين.";
                return;
            }
            Suppliers.Clear();
            foreach (var s in suppliers.Value) Suppliers.Add(s);

            var purchases = await _session.Api.GetPurchasesAsync();
            if (!purchases.Success || purchases.Value is null) { LoadError = purchases.Error ?? "تعذر تحميل المشتريات."; return; }
            Purchases.Clear();
            if (purchases.Success && purchases.Value is not null)
                foreach (var p in purchases.Value.OrderByDescending(p => p.CreatedAtUtc)) Purchases.Add(p);

            var items = await _session.Api.GetInventoryItemsAsync();
            if (!items.Success || items.Value is null) { LoadError = items.Error ?? "تعذر تحميل المخزون."; return; }
            InventoryItems.Clear();
            if (items.Success && items.Value is not null)
                foreach (var i in items.Value.Where(i => i.Active)) InventoryItems.Add(i);

            await LoadPayablesAsync();
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasNoSuppliers));
        }
    }

    // ---------------- Payables aging ----------------

    public ObservableCollection<PayableDto> Payables { get; } = new();

    /// <summary>The few invoices somebody has to deal with first. Capped rather than complete: the
    /// full list is the invoice table below, and a card that reprinted it would bury the one line
    /// that matters under thirty that do not.</summary>
    public ObservableCollection<PayableDto> WorstOverdue { get; } = new();
    private const int WorstOverdueCount = 5;
    public ObservableCollection<PayablesBucketDto> PayableBuckets { get; } = new();
    [ObservableProperty] private PayablesAgingDto? _aging;

    public bool HasPayables => Payables.Count > 0;
    public bool HasOverdue => WorstOverdue.Count > 0;
    public string OverdueSummary => Aging is null ? ""
        : Aging.OverdueInvoiceCount == 0
            ? "لا توجد فواتير متأخرة عن موعد استحقاقها"
            : $"{Aging.OverdueInvoiceCount} فاتورة متأخرة بقيمة {CashInput.Format(Aging.OverduePiastres)} ج";

    /// <summary>Said out loud rather than left as a silent gap: an invoice with no agreed due date
    /// can never appear as late, so a payables report that looked clean while these existed would
    /// be misleading.</summary>
    public string UnscheduledSummary => Aging is null || Aging.UnscheduledInvoiceCount == 0 ? ""
        : $"{Aging.UnscheduledInvoiceCount} فاتورة بدون تاريخ استحقاق ({CashInput.Format(Aging.UnscheduledPiastres)} ج) - لا يمكن اعتبارها متأخرة أو منتظمة";

    private async Task LoadPayablesAsync()
    {
        var aging = await _session.Api.GetPayablesAgingAsync();
        // A failure here does not fail the screen: the supplier and invoice lists above are still
        // useful, and an empty aging panel that says nothing is better than a blocked page.
        if (!aging.Success || aging.Value is null) return;
        Aging = aging.Value;
        Payables.Clear();
        foreach (var invoice in aging.Value.Invoices) Payables.Add(invoice);
        PayableBuckets.Clear();
        foreach (var bucket in aging.Value.Buckets.Where(b => b.InvoiceCount > 0)) PayableBuckets.Add(bucket);
        WorstOverdue.Clear();
        // The server already ranks worst-first, so this takes the head of its ordering rather than
        // inventing a second one that could disagree with the buckets beside it.
        foreach (var invoice in aging.Value.Invoices.Where(i => i.DaysOverdue is > 0).Take(WorstOverdueCount))
            WorstOverdue.Add(invoice);
        foreach (var name in new[] { nameof(HasPayables), nameof(HasOverdue), nameof(OverdueSummary), nameof(UnscheduledSummary) })
            OnPropertyChanged(name);
    }

    // ---------------- Setting a due date after the fact ----------------

    [ObservableProperty] private PurchaseDto? _dueDateTarget;
    [ObservableProperty] private string _dueDateText = "";
    [ObservableProperty] private string? _dueDateError;
    public bool IsDueDateOpen => DueDateTarget is not null;
    partial void OnDueDateTargetChanged(PurchaseDto? value) => OnPropertyChanged(nameof(IsDueDateOpen));

    /// <summary>The invoice number reads better than the id on the dialog, but an invoice received
    /// without one still has to be identifiable.</summary>
    public string DueDateTargetLabel => DueDateTarget is null ? ""
        : string.IsNullOrWhiteSpace(DueDateTarget.InvoiceNumber) ? DueDateTarget.Id : DueDateTarget.InvoiceNumber!;

    [RelayCommand]
    private void OpenDueDate(PurchaseDto purchase)
    {
        if (!CanEditAction || !CanManage) return;
        DueDateError = null;
        DueDateText = purchase.DueDate ?? "";
        DueDateTarget = purchase;
        OnPropertyChanged(nameof(DueDateTargetLabel));
    }

    [RelayCommand]
    private void CloseDueDate() { if (CanEditAction) DueDateTarget = null; }

    [RelayCommand]
    private async Task SubmitDueDateAsync()
    {
        if (IsBusy || DueDateTarget is null || _session.CurrentUser is null) return;
        if (!TryParseDueDate(DueDateText, out var dueDate))
        {
            DueDateError = "أدخل التاريخ بصيغة سنة-شهر-يوم مثل 2026-10-01، أو اتركه فارغًا لإلغائه.";
            return;
        }
        IsBusy = true;
        DueDateError = null;
        try
        {
            var result = await _session.Api.SetPurchaseDueDateAsync(new SetPurchaseDueDateRequest(
                $"due-{Guid.NewGuid():N}", DueDateTarget.Id, dueDate, _session.CurrentUser.Id));
            if (!result.Success) { DueDateError = result.Error ?? "تعذّر حفظ تاريخ الاستحقاق."; return; }
            ActionNote = dueDate is null ? "تم إلغاء تاريخ الاستحقاق." : "تم حفظ تاريخ الاستحقاق.";
            DueDateTarget = null;
            await LoadAsync();
        }
        finally { IsBusy = false; }
    }

    /// <summary>Blank is a valid answer and means "no term agreed" - the one case that must not be
    /// treated as an input error, because it is the honest state for most invoices.</summary>
    private static bool TryParseDueDate(string text, out DateOnly? date)
    {
        date = null;
        if (string.IsNullOrWhiteSpace(text)) return true;
        if (!DateOnly.TryParseExact(text.Trim(), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var parsed)) return false;
        date = parsed;
        return true;
    }

    // ---------------- Add supplier ----------------

    [ObservableProperty] private bool _isSupplierOpen;
    [ObservableProperty] private string _supplierName = "";
    [ObservableProperty] private string _supplierPhone = "";
    [ObservableProperty] private string _supplierCategory = "";
    [ObservableProperty] private string _supplierNotes = "";
    [ObservableProperty] private string _supplierManagerPin = "";
    [ObservableProperty] private string? _supplierError;

    private string? _supplierRequestId;
    public bool SupplierNeedsApproval => !CanAddSupplier;

    [RelayCommand]
    private void OpenAddSupplier()
    {
        if (!CanEditAction) return;
        SupplierError = null;
        SupplierName = SupplierPhone = SupplierCategory = SupplierNotes = SupplierManagerPin = "";
        _supplierRequestId = $"sup-{Guid.NewGuid():N}";
        IsSupplierOpen = true;
        OnPropertyChanged(nameof(SupplierNeedsApproval));
    }

    [RelayCommand]
    private void CloseAddSupplier() { if (CanEditAction) IsSupplierOpen = false; }

    [RelayCommand]
    private async Task SubmitSupplierAsync()
    {
        if (IsBusy || _receiveRequest.HasPending || _payRequest.HasPending) return;
        if (_supplierRequest.Value is { } pending)
        {
            IsBusy = true;
            try { await CompleteSupplierAsync(pending); }
            finally { IsBusy = false; }
            return;
        }
        if (string.IsNullOrWhiteSpace(SupplierName))
        {
            SupplierError = "اسم المورد إلزامي.";
            return;
        }
        IsBusy = true;
        SupplierError = null;
        try
        {
            // Adding a supplier accepts EITHER purchasing_manage OR suppliers_manage, with a manager
            // approval bound to suppliers_manage for anyone holding neither - see
            // AuthorizationPolicy.AuthorizeAddSupplier.
            var (approvalId, approverId, ok) = await ResolveApprovalAsync(
                SupplierNeedsApproval, SupplierManagerPin, "suppliers_manage", $"إضافة مورد {SupplierName}",
                e => SupplierError = e);
            if (!ok) return;

            await CompleteSupplierAsync(new AddSupplierRequest(
                _supplierRequestId!, SupplierName.Trim(), NullIfEmpty(SupplierPhone), null,
                NullIfEmpty(SupplierCategory), NullIfEmpty(SupplierNotes),
                _session.CurrentUser!.Id, approvalId, approverId));
        }
        finally { IsBusy = false; }
    }

    private async Task CompleteSupplierAsync(AddSupplierRequest request)
    {
            SupplierError = null;
            var result = await _supplierRequest.SendAsync(request, r => _session.Api.AddSupplierAsync(r));
            if (!result.Success || result.Value is null)
            {
                SupplierError = _supplierRequest.FailureMessage(result.Error);
                return;
            }
            ActionNote = $"تمت إضافة المورد {request.Name}.";
            IsSupplierOpen = false;
            _supplierRequestId = null;
            await LoadAsync();
    }

    // ---------------- Receive a purchase ----------------

    public sealed partial class PurchaseLineEntry : ObservableObject
    {
        [ObservableProperty] private InventoryItemDto? _item;
        [ObservableProperty] private string _quantityText = "";
        [ObservableProperty] private string _unitCostText = "";
    }

    [ObservableProperty] private bool _isReceiveOpen;
    [ObservableProperty] private SupplierDto? _receiveSupplier;
    [ObservableProperty] private string _invoiceNumber = "";
    [ObservableProperty] private string _receivePaymentMethod = "cash";
    [ObservableProperty] private string _receivePaidText = "0";
    [ObservableProperty] private string _receiveNotes = "";
    [ObservableProperty] private string _receiveManagerPin = "";
    [ObservableProperty] private string? _receiveError;

    public ObservableCollection<PurchaseLineEntry> ReceiveLines { get; } = new();

    private string? _receiveRequestId;
    public bool ReceiveNeedsApproval => !CanManage;

    /// <summary>"credit" (آجل) means nothing moves through treasury - the invoice starts entirely
    /// outstanding, so a paid amount is not just unnecessary but rejected server-side.</summary>
    public bool IsCreditPurchase => ReceivePaymentMethod == "credit";

    /// <summary>The agreed due date, as yyyy-MM-dd. Left blank when no term was agreed - the form
    /// never prefills one, because a prefilled due date becomes an agreed one nobody agreed to.</summary>
    [ObservableProperty] private string _receiveDueDateText = "";
    partial void OnReceivePaymentMethodChanged(string value)
    {
        OnPropertyChanged(nameof(IsCreditPurchase));
        if (value == "credit") ReceivePaidText = "0";
    }

    [RelayCommand]
    private void OpenReceive(SupplierDto supplier)
    {
        if (!CanEditAction) return;
        ReceiveError = null;
        ReceiveSupplier = supplier;
        InvoiceNumber = "";
        ReceivePaymentMethod = "cash";
        ReceivePaidText = "0";
        ReceiveNotes = "";
        ReceiveDueDateText = "";
        ReceiveManagerPin = "";
        ReceiveLines.Clear();
        ReceiveLines.Add(new PurchaseLineEntry());
        _receiveRequestId = $"pur-{Guid.NewGuid():N}";
        IsReceiveOpen = true;
        OnPropertyChanged(nameof(ReceiveNeedsApproval));
    }

    [RelayCommand]
    private void CloseReceive() { if (CanEditAction) IsReceiveOpen = false; }

    [RelayCommand]
    private void AddReceiveLine() { if (CanEditAction) ReceiveLines.Add(new PurchaseLineEntry()); }

    [RelayCommand]
    private void RemoveReceiveLine(PurchaseLineEntry line)
    {
        if (!CanEditAction) return;
        ReceiveLines.Remove(line);
        if (ReceiveLines.Count == 0) ReceiveLines.Add(new PurchaseLineEntry());
    }

    [RelayCommand]
    private void SetReceivePaymentMethod(string method) { if (CanEditAction) ReceivePaymentMethod = method; }

    [RelayCommand]
    private async Task SubmitReceiveAsync()
    {
        if (IsBusy || _supplierRequest.HasPending || _payRequest.HasPending) return;
        if (_receiveRequest.Value is { } pending)
        {
            IsBusy = true;
            try { await CompleteReceiveAsync(pending); }
            finally { IsBusy = false; }
            return;
        }
        if (ReceiveSupplier is null) return;

        var lines = new List<PurchaseLineRequest>();
        foreach (var entry in ReceiveLines)
        {
            if (entry.Item is null) continue;
            if (!MilliUnits.TryParse(entry.QuantityText, out var qty) || qty <= 0)
            {
                ReceiveError = "أدخل كمية صحيحة لكل صنف.";
                return;
            }
            if (!CashInput.TryParse(entry.UnitCostText, out var unitCost) || unitCost <= 0)
            {
                ReceiveError = "أدخل سعر وحدة صحيح لكل صنف.";
                return;
            }
            lines.Add(new PurchaseLineRequest(entry.Item.Id, qty, unitCost));
        }
        if (lines.Count == 0)
        {
            ReceiveError = "أضف صنفًا واحدًا على الأقل.";
            return;
        }
        if (!CashInput.TryParse(ReceivePaidText, out var paid))
        {
            ReceiveError = "أدخل المبلغ المدفوع بشكل صحيح.";
            return;
        }
        if (!TryParseDueDate(ReceiveDueDateText, out var dueDate))
        {
            ReceiveError = "أدخل تاريخ الاستحقاق بصيغة سنة-شهر-يوم مثل 2026-10-01، أو اتركه فارغًا.";
            return;
        }

        IsBusy = true;
        ReceiveError = null;
        try
        {
            var (approvalId, approverId, ok) = await ResolveApprovalAsync(
                ReceiveNeedsApproval, ReceiveManagerPin, "purchasing_manage", $"استلام مشتريات من {ReceiveSupplier.Name}",
                e => ReceiveError = e);
            if (!ok) return;

            await CompleteReceiveAsync(new ReceivePurchaseRequest(
                _receiveRequestId!, ReceiveSupplier.Id, NullIfEmpty(InvoiceNumber), lines,
                IsCreditPurchase ? 0 : paid, ReceivePaymentMethod, _session.CurrentUser!.Id,
                NullIfEmpty(ReceiveNotes), approvalId, approverId, dueDate));
        }
        finally { IsBusy = false; }
    }

    private async Task CompleteReceiveAsync(ReceivePurchaseRequest request)
    {
            ReceiveError = null;
            var result = await _receiveRequest.SendAsync(request, r => _session.Api.ReceivePurchaseAsync(r));
            if (!result.Success || result.Value is null)
            {
                ReceiveError = _receiveRequest.FailureMessage(result.Error);
                return;
            }
            ActionNote = $"تم استلام فاتورة بإجمالي {CashInput.Format(result.Value.TotalPiastres)} ج.";
            IsReceiveOpen = false;
            _receiveRequestId = null;
            await LoadAsync();
    }

    // ---------------- Settle an outstanding invoice ----------------

    [ObservableProperty] private PurchaseDto? _payTarget;
    [ObservableProperty] private string _payAmountText = "";
    [ObservableProperty] private string _payMethod = "cash";
    [ObservableProperty] private string _payManagerPin = "";
    [ObservableProperty] private string? _payError;
    public bool IsPayOpen => PayTarget is not null;
    partial void OnPayTargetChanged(PurchaseDto? value) => OnPropertyChanged(nameof(IsPayOpen));

    private string? _payRequestId;
    public bool PayNeedsApproval => !CanManage;

    [RelayCommand]
    private void OpenPay(PurchaseDto purchase)
    {
        if (!CanEditAction) return;
        PayError = null;
        PayAmountText = CashInput.Format(purchase.RemainingAmountPiastres);
        PayMethod = "cash";
        PayManagerPin = "";
        _payRequestId = $"pay-{Guid.NewGuid():N}";
        PayTarget = purchase;
        OnPropertyChanged(nameof(PayNeedsApproval));
    }

    [RelayCommand]
    private void ClosePay() { if (CanEditAction) PayTarget = null; }

    [RelayCommand]
    private void SetPayMethod(string method) { if (CanEditAction) PayMethod = method; }

    [RelayCommand]
    private async Task SubmitPayAsync()
    {
        if (IsBusy || _supplierRequest.HasPending || _receiveRequest.HasPending) return;
        if (_payRequest.Value is { } pending)
        {
            IsBusy = true;
            try { await CompletePayAsync(pending); }
            finally { IsBusy = false; }
            return;
        }
        if (PayTarget is null) return;
        if (!CashInput.TryParse(PayAmountText, out var amount) || amount <= 0)
        {
            PayError = "أدخل مبلغ السداد بشكل صحيح.";
            return;
        }

        IsBusy = true;
        PayError = null;
        try
        {
            var (approvalId, approverId, ok) = await ResolveApprovalAsync(
                PayNeedsApproval, PayManagerPin, "purchasing_manage", "سداد فاتورة مشتريات", e => PayError = e);
            if (!ok) return;

            await CompletePayAsync(new PayPurchaseRequest(
                _payRequestId!, PayTarget.Id, amount, PayMethod, _session.CurrentUser!.Id, approvalId, approverId));
        }
        finally { IsBusy = false; }
    }

    private async Task CompletePayAsync(PayPurchaseRequest request)
    {
            PayError = null;
            var result = await _payRequest.SendAsync(request, r => _session.Api.PayPurchaseAsync(r));
            if (!result.Success || result.Value is null)
            {
                PayError = _payRequest.FailureMessage(result.Error);
                return;
            }
            ActionNote = $"تم سداد {CashInput.Format(request.AmountPiastres)} ج.";
            PayTarget = null;
            _payRequestId = null;
            await LoadAsync();
    }

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

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
