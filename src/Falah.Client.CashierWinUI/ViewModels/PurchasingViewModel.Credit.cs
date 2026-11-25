using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Purchasing;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// P3: correcting what is owed on one supplier invoice without touching the invoice as received - a credit on the
/// supplier ledger (SupplierCreditRules): the invoice was entered wrong, the supplier granted a reduction, or goods
/// were handed back (those leave stock at the invoice's own cost; the server computes their value). Never more than is
/// still owed; nothing is paid or received. purchasing_manage, or a manager's PIN. Kept on disk until its outcome is known.
/// </summary>
public partial class PurchasingViewModel
{
    private readonly RetainedRequest<RecordSupplierCreditRequest> _creditRequest = new("purchasing.credit");

    public sealed record CreditKindOption(string Key, string Label);

    public IReadOnlyList<CreditKindOption> CreditKinds { get; } = new[]
    {
        new CreditKindOption("invoice_error", "خطأ في تسجيل الفاتورة"),
        new CreditKindOption("supplier_credit", "خصم أو تعويض من المورد"),
        new CreditKindOption("goods_returned", "مرتجع بضاعة للمورد"),
    };

    [ObservableProperty] private PurchaseDto? _creditTarget;
    [ObservableProperty] private CreditKindOption? _selectedCreditKind;
    [ObservableProperty] private string _creditAmountText = "";
    /// <summary>An invoice line as the operator reads it: the item's name, never its internal id.</summary>
    public sealed record ReturnLineOption(PurchaseLineDto Line, string Label);

    public System.Collections.ObjectModel.ObservableCollection<ReturnLineOption> CreditReturnOptions { get; } = new();
    [ObservableProperty] private ReturnLineOption? _creditReturnLine;
    [ObservableProperty] private string _creditReturnQuantityText = "";
    [ObservableProperty] private string _creditReason = "";
    [ObservableProperty] private string _creditManagerPin = "";
    [ObservableProperty] private string? _creditError;

    public bool IsCreditOpen => CreditTarget is not null;
    public bool IsGoodsReturn => SelectedCreditKind?.Key == "goods_returned";
    public bool IsAmountCredit => SelectedCreditKind is not null && !IsGoodsReturn;

    partial void OnCreditTargetChanged(PurchaseDto? value) => OnPropertyChanged(nameof(IsCreditOpen));
    partial void OnSelectedCreditKindChanged(CreditKindOption? value)
    {
        OnPropertyChanged(nameof(IsGoodsReturn));
        OnPropertyChanged(nameof(IsAmountCredit));
    }

    [RelayCommand]
    private void OpenCredit(PurchaseDto purchase)
    {
        if (!CanEditAction || _creditRequest.HasPending) return;
        SelectedCreditKind = null;
        CreditAmountText = "";
        CreditReturnOptions.Clear();
        foreach (var line in purchase.Lines)
            CreditReturnOptions.Add(new ReturnLineOption(line,
                InventoryItems.FirstOrDefault(i => i.Id == line.InventoryItemId)?.Name ?? "صنف غير موجود في القائمة"));
        CreditReturnLine = CreditReturnOptions.FirstOrDefault();
        CreditReturnQuantityText = "";
        CreditReason = "";
        CreditManagerPin = "";
        CreditError = null;
        CreditTarget = purchase;
    }

    [RelayCommand]
    private void CloseCredit() { if (CanEditAction && !_creditRequest.HasPending) CreditTarget = null; }

    [RelayCommand]
    private async Task SubmitCreditAsync()
    {
        if (IsBusy) return;
        var pending = _creditRequest.Value;
        RecordSupplierCreditRequest? request = pending;
        if (request is null)
        {
            if (CreditTarget is not { } target) return;
            if (SelectedCreditKind is null) { CreditError = "اختر نوع التصحيح."; return; }
            if (string.IsNullOrWhiteSpace(CreditReason)) { CreditError = "اكتب سبب التصحيح."; return; }
            long? amount = null;
            IReadOnlyList<SupplierReturnLineRequest>? lines = null;
            if (IsGoodsReturn)
            {
                if (CreditReturnLine is null || !MilliUnits.TryParse(CreditReturnQuantityText, out var qty) || qty <= 0)
                { CreditError = "اختر الصنف المرتجع واكتب الكمية."; return; }
                lines = new[] { new SupplierReturnLineRequest(CreditReturnLine.Line.InventoryItemId, qty) };
            }
            else
            {
                if (!CashInput.TryParse(CreditAmountText, out var a) || a <= 0 || a > target.RemainingAmountPiastres)
                { CreditError = "المبلغ يجب أن يكون أكبر من صفر ولا يتجاوز المتبقي على الفاتورة."; return; }
                amount = a;
            }

            IsBusy = true;
            try
            {
                var (approvalId, approverId, ok) = await ResolveApprovalAsync(
                    PayNeedsApproval, CreditManagerPin, "purchasing_manage", "تصحيح فاتورة مورد", e => CreditError = e);
                if (!ok) return;
                request = new RecordSupplierCreditRequest($"cr-{Guid.NewGuid():N}", target.Id, SelectedCreditKind.Key, amount,
                    CreditReason.Trim(), _session.CurrentUser!.Id, lines, approvalId, approverId);
            }
            finally { IsBusy = false; }
        }

        IsBusy = true;
        CreditError = null;
        try
        {
            var result = await _creditRequest.SendAsync(request, r => _session.Api.RecordSupplierCreditAsync(r));
            if (!result.Success || result.Value is null) { CreditError = _creditRequest.FailureMessage(result.Error); return; }
            ActionNote = $"سُجّل تصحيح {CashInput.Display(result.Value.AmountPiastres)} ج على الفاتورة؛ المتبقي {CashInput.Display(result.Value.RemainingAmountPiastres)} ج.";
            CreditTarget = null;
        }
        finally
        {
            IsBusy = false;
            CreditManagerPin = "";
        }
        await LoadAsync();
    }
}
