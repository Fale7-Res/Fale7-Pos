using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Delivery;
using Falah.Application.Reports;
using Falah.Application.Shifts;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// The owner's financial report (P5) and the two follow-ups it raises (P2, P4), on the financial control screen:
///
/// - every KPI as the server defined it: label, amount (or "غير متاح" - unknown is never shown as 0), whether it is an
///   actual figure or an estimate, its definition, basis and source records; the sales bridge and the money flow with
///   their residuals; the timing notes;
/// - collection shortfalls nobody has classified, classified here with evidence (and a manager's PIN for a user
///   without delivery_settlement_approve);
/// - handovers the receiver has not confirmed, confirmed here by the receiver's own session or the receiver's PIN.
///
/// Both commands are kept on disk until their outcome is known, like every command that changes money's meaning.
/// </summary>
public partial class FinancialControlViewModel
{
    // ---- Owner financial report ----

    public sealed record KpiRow(string Label, string AmountDisplay, string StatusLabel, bool IsEstimateOrPartial, bool IsUnavailable,
        string Definition, string SourceDisplay, string? Note);

    public sealed record KpiSection(string Title, IReadOnlyList<KpiRow> Rows);

    public sealed record BridgeRow(string Label, long AmountPiastres);

    public sealed record MoneyFlowRow(string Label, long InflowPiastres, long OutflowPiastres, long InternalPiastres, int Count, bool IsCustomerMoney);

    public ObservableCollection<KpiSection> OwnerSections { get; } = new();
    public ObservableCollection<BridgeRow> SalesBridge { get; } = new();
    public ObservableCollection<MoneyFlowRow> MoneyFlow { get; } = new();
    public ObservableCollection<string> TimingNotes { get; } = new();
    [ObservableProperty] private string? _ownerReportError;
    [ObservableProperty] private long _bridgeResidualPiastres;
    [ObservableProperty] private long _flowResidualPiastres;
    [ObservableProperty] private int _unclassifiedMovementCount;
    public bool HasOwnerReport => OwnerSections.Count > 0;
    public bool IsReconciled => BridgeResidualPiastres == 0 && FlowResidualPiastres == 0 && UnclassifiedMovementCount == 0;
    public string ReconciliationLabel => IsReconciled
        ? "المطابقة سليمة: كل مبيعات الفترة وكل حركات الخزينة مفسَّرة دون فرق."
        : $"فرق غير مفسَّر: الجسر {CashInput.Format(BridgeResidualPiastres)} ج - الحركات {CashInput.Format(FlowResidualPiastres)} ج - حركات غير مصنفة {UnclassifiedMovementCount}. راجع المسؤول التقني.";

    private async Task LoadOwnerControlsAsync(DateOnly from, DateOnly to)
    {
        var actor = _session.CurrentUser!.Id;
        var report = await _session.Api.GetOwnerFinancialReportAsync(actor, from, to);
        var shortfalls = await _session.Api.GetCollectionShortfallsAsync(actor);
        var handovers = await _session.Api.GetUnconfirmedHandoversAsync(actor);

        OwnerReportError = !report.Success ? report.Error ?? "تعذر تحميل التقرير المالي." : null;
        // A reply without its sections is not a report: say so rather than show an empty one as "nothing to report".
        if (report.Success && report.Value is { Sections: not null, SalesBridge: not null, MoneyFlow.Lines: not null } r) FillOwnerReport(r);
        else if (report.Success) OwnerReportError = "تعذر قراءة التقرير المالي.";
        if (shortfalls.Success && shortfalls.Value is { Items: not null } s) FillShortfalls(s);
        if (handovers.Success && handovers.Value is { } h) FillHandovers(h);
    }

    public void FillOwnerReport(OwnerFinancialReportDto report)
    {
        OwnerSections.Clear();
        foreach (var section in report.Sections)
            OwnerSections.Add(new KpiSection(section.Title, section.Kpis.Select(ToRow).ToList()));

        var b = report.SalesBridge;
        SalesBridge.Clear();
        foreach (var row in new[]
        {
            new BridgeRow("إجمالي مبيعات الفترة (قبل المرتجعات)", b.GrossSalesPiastres),
            new BridgeRow("دخل الخزينة عند البيع أو التأكيد", b.ReceivedThroughTreasuryPiastres),
            new BridgeRow("سوّاه السائقون", b.CodSettledPiastres),
            new BridgeRow("مع السائقين لم يُسوَّ", b.CodHeldByDriversPiastres),
            new BridgeRow("لم يُحصَّل بعد (مع العميل)", b.CodNotYetCollectedPiastres),
            new BridgeRow("تحويلات لم تُؤكد", b.PendingElectronicPiastres),
            new BridgeRow("فروق تحصيل غير محسومة", b.ShortfallUnresolvedPiastres),
            new BridgeRow("فروق تحصيل مستحقة (عميل/سائق)", b.ShortfallReceivablePiastres),
            new BridgeRow("فروق تحصيل معتمدة كخصم أو إعدام", b.ShortfallRealisedPiastres),
            new BridgeRow("فرق غير مفسَّر", b.ResidualPiastres),
            new BridgeRow("مرتجعات دُفعت في الفترة", b.RefundsMadePiastres),
            new BridgeRow("صافي المبيعات", b.NetSalesPiastres),
        })
            SalesBridge.Add(row);

        MoneyFlow.Clear();
        foreach (var line in report.MoneyFlow.Lines)
            MoneyFlow.Add(new MoneyFlowRow(line.Label, line.InflowPiastres, line.OutflowPiastres, line.MovedInternallyPiastres,
                line.MovementCount, line.CountsAsCustomerReceipt));

        TimingNotes.Clear();
        foreach (var note in report.TimingNotes ?? Array.Empty<string>()) TimingNotes.Add(note);

        BridgeResidualPiastres = b.ResidualPiastres;
        FlowResidualPiastres = report.MoneyFlow.ResidualPiastres;
        UnclassifiedMovementCount = report.MoneyFlow.UnclassifiedMovementCount;
        OnPropertyChanged(nameof(HasOwnerReport));
        OnPropertyChanged(nameof(IsReconciled));
        OnPropertyChanged(nameof(ReconciliationLabel));
    }

    public static KpiRow ToRow(FinancialKpiDto k)
    {
        // Unknown is never zero: an unavailable figure says so in words.
        var amount = k.AmountPiastres is { } a ? $"{CashInput.Format(a)} ج"
            : k.Count is { } c && k.Status != "unavailable" ? c.ToString()
            : "غير متاح";
        if (k.AmountPiastres is not null && k.Count is { } n && n > 0) amount += $"  ({n})";
        var status = k.Status switch
        {
            "estimate" => "تقديري",
            "partial" => "جزئي",
            "unavailable" => "غير متاح",
            _ => "فعلي",
        };
        var basis = k.Basis switch
        {
            "sale" => "بوقت البيع",
            "movement" => "بوقت حركة المال",
            "now" => "الوضع الآن",
            "shift" => "ورديات الفترة",
            "business_date" => "بتاريخ يوم العمل",
            _ => k.Basis,
        };
        return new KpiRow(k.Label, amount, status, k.Status is "estimate" or "partial", k.Status == "unavailable",
            k.Definition, $"{basis} - المصدر: {k.Sources}", k.Note);
    }

    // ---- Collection shortfalls (AUD-03) ----

    public sealed record ShortfallRow(string DeliveryId, string OrderLabel, string DriverName, long ShortfallPiastres, string StatusLabel, bool IsUnresolved);

    public sealed record ClassificationOption(string Key, string Label);

    public IReadOnlyList<ClassificationOption> ClassificationOptions { get; } = new[]
    {
        new ClassificationOption("customer_receivable", "مديونية على العميل (سيُحصَّل لاحقًا)"),
        new ClassificationOption("approved_discount", "خصم معتمد للعميل"),
        new ClassificationOption("write_off", "إعدام دين معتمد (لن يُحصَّل)"),
        new ClassificationOption("driver_liability", "مسؤولية السائق"),
    };

    public ObservableCollection<ShortfallRow> Shortfalls { get; } = new();
    [ObservableProperty] private long _shortfallUnresolvedPiastres;
    [ObservableProperty] private int _shortfallUnresolvedCount;
    [ObservableProperty] private ShortfallRow? _classifyTarget;
    [ObservableProperty] private ClassificationOption? _selectedClassification;
    [ObservableProperty] private string _classificationEvidence = "";
    [ObservableProperty] private string _classificationManagerPin = "";
    [ObservableProperty] private string? _classificationMessage;
    [ObservableProperty] private bool _isClassifying;
    public bool HasShortfalls => Shortfalls.Count > 0;
    public bool IsClassifyOpen => ClassifyTarget is not null;
    public bool ClassificationNeedsManagerPin => !_session.HasPermission("delivery_settlement_approve");

    private readonly RetainedRequest<ClassifyCollectionShortfallRequest> _classifyRequest = new("financialcontrol.shortfall.classify");

    partial void OnClassifyTargetChanged(ShortfallRow? value) => OnPropertyChanged(nameof(IsClassifyOpen));

    public void FillShortfalls(CollectionShortfallListDto list)
    {
        Shortfalls.Clear();
        foreach (var s in list.Items)
            Shortfalls.Add(new ShortfallRow(s.DeliveryId, s.OrderNumber ?? s.OrderId, s.DriverName ?? "", s.ShortfallPiastres,
                s.Classification is null ? "غير محسوم" : ClassificationOptions.FirstOrDefault(o => o.Key == s.Classification)?.Label ?? s.Classification,
                s.Classification is null));
        ShortfallUnresolvedPiastres = list.UnresolvedPiastres;
        ShortfallUnresolvedCount = list.UnresolvedCount;
        OnPropertyChanged(nameof(HasShortfalls));
    }

    [RelayCommand]
    private void OpenClassify(ShortfallRow? row)
    {
        if (row is not { IsUnresolved: true } || IsClassifying || _classifyRequest.HasPending) return;
        SelectedClassification = null;
        ClassificationEvidence = "";
        ClassificationManagerPin = "";
        ClassificationMessage = null;
        ClassifyTarget = row;
    }

    [RelayCommand]
    private void CancelClassify()
    {
        if (IsClassifying || _classifyRequest.HasPending) return;
        ClassifyTarget = null;
    }

    [RelayCommand]
    private async Task ConfirmClassifyAsync()
    {
        if (IsClassifying) return;
        var pending = _classifyRequest.Value;
        if (pending is null)
        {
            if (ClassifyTarget is null) return;
            if (SelectedClassification is null) { ClassificationMessage = "اختر تصنيف الفرق."; return; }
            if (ClassificationEvidence.Trim().Length < 5) { ClassificationMessage = "اكتب الدليل: من وافق، ولماذا، وأي مستند."; return; }
            if (ClassificationNeedsManagerPin && ClassificationManagerPin.Length < 4) { ClassificationMessage = "أدخل رمز المدير لاعتماد التصنيف."; return; }
        }

        IsClassifying = true;
        ClassificationMessage = null;
        try
        {
            var request = pending;
            if (request is null)
            {
                string? approvalId = null, approverId = null;
                if (ClassificationNeedsManagerPin)
                {
                    var approval = await _session.Api.IssueManagerApprovalAsync(new Falah.Application.Approvals.IssueManagerApprovalRequest(
                        ClassificationManagerPin, "delivery_settlement_approve", ClassificationEvidence));
                    if (!approval.Success || approval.Value is null) { ClassificationMessage = approval.Error ?? "رمز المدير غير صحيح أو غير مخوّل."; return; }
                    (approvalId, approverId) = (approval.Value.ApprovalId, approval.Value.ApproverUserId);
                }
                request = new ClassifyCollectionShortfallRequest($"cls-{Guid.NewGuid():N}", ClassifyTarget!.DeliveryId,
                    SelectedClassification!.Key, ClassificationEvidence.Trim(), _session.CurrentUser!.Id, approvalId, approverId);
            }
            var result = await _classifyRequest.SendAsync(request, r => _session.Api.ClassifyCollectionShortfallAsync(r));
            if (!result.Success) { ClassificationMessage = _classifyRequest.FailureMessage(result.Error); return; }
            ClassifyTarget = null;
        }
        finally
        {
            IsClassifying = false;
            ClassificationManagerPin = "";
        }
        await LoadAsync();
    }

    // ---- Handover receipts (P4) ----

    public sealed record HandoverRow(string ShiftId, string ShiftName, string ClosedBy, string ReceiverUserId, string ReceiverName,
        long HandedOverPiastres, string StatusLabel, bool IsPending, string? Note);

    public ObservableCollection<HandoverRow> Handovers { get; } = new();
    [ObservableProperty] private HandoverRow? _acknowledgeTarget;
    [ObservableProperty] private string _receivedAmountText = "";
    [ObservableProperty] private string _receiptNote = "";
    [ObservableProperty] private string _receiverPin = "";
    [ObservableProperty] private string? _receiptMessage;
    [ObservableProperty] private bool _isAcknowledging;
    public bool HasHandovers => Handovers.Count > 0;
    public bool IsAcknowledgeOpen => AcknowledgeTarget is not null;

    /// <summary>The receiver confirms from their own session; anyone else's screen needs the receiver's PIN.</summary>
    public bool AcknowledgeNeedsReceiverPin => AcknowledgeTarget is { } t && t.ReceiverUserId != _session.CurrentUser?.Id;

    private readonly RetainedRequest<AcknowledgeShiftHandoverRequest> _acknowledgeRequest = new("financialcontrol.handover.acknowledge");

    partial void OnAcknowledgeTargetChanged(HandoverRow? value)
    {
        OnPropertyChanged(nameof(IsAcknowledgeOpen));
        OnPropertyChanged(nameof(AcknowledgeNeedsReceiverPin));
    }

    public void FillHandovers(IReadOnlyList<ShiftHandoverReceiptDto> list)
    {
        Handovers.Clear();
        foreach (var h in list)
            Handovers.Add(new HandoverRow(h.ShiftId, h.ShiftName, h.ClosedBy ?? "", h.ReceiverUserId ?? "", h.ReceiverName ?? "لم يُحدَّد",
                h.HandedOverPiastres,
                h.Status == "disputed" ? $"مختلف عليه: استُلم {CashInput.Format(h.ReceivedPiastres ?? 0)} ج" : "بانتظار تأكيد المستلم",
                h.Status == "pending", h.Note));
        OnPropertyChanged(nameof(HasHandovers));
    }

    [RelayCommand]
    private void OpenAcknowledge(HandoverRow? row)
    {
        if (row is not { IsPending: true } || IsAcknowledging || _acknowledgeRequest.HasPending) return;
        ReceivedAmountText = CashInput.Format(row.HandedOverPiastres);
        ReceiptNote = "";
        ReceiverPin = "";
        ReceiptMessage = null;
        AcknowledgeTarget = row;
    }

    [RelayCommand]
    private void CancelAcknowledge()
    {
        if (IsAcknowledging || _acknowledgeRequest.HasPending) return;
        AcknowledgeTarget = null;
    }

    [RelayCommand]
    private async Task ConfirmAcknowledgeAsync()
    {
        if (IsAcknowledging) return;
        var pending = _acknowledgeRequest.Value;
        long received = 0;
        if (pending is null)
        {
            if (AcknowledgeTarget is not { } target) return;
            if (!CashInput.TryParse(ReceivedAmountText, out received) || received < 0) { ReceiptMessage = "اكتب المبلغ الذي عددته فعلًا."; return; }
            if (received != target.HandedOverPiastres && string.IsNullOrWhiteSpace(ReceiptNote))
            { ReceiptMessage = "المبلغ يختلف عن المسلَّم؛ اكتب ملاحظة بما حدث."; return; }
            if (AcknowledgeNeedsReceiverPin && ReceiverPin.Length < 4) { ReceiptMessage = $"أدخل رمز المستلم ({target.ReceiverName})."; return; }
        }

        IsAcknowledging = true;
        ReceiptMessage = null;
        try
        {
            var request = pending;
            if (request is null)
            {
                string? approvalId = null, approverId = null;
                if (AcknowledgeNeedsReceiverPin)
                {
                    var approval = await _session.Api.IssueManagerApprovalAsync(new Falah.Application.Approvals.IssueManagerApprovalRequest(
                        ReceiverPin, "shifts_manage", "تأكيد استلام نقدية الإغلاق"));
                    if (!approval.Success || approval.Value is null) { ReceiptMessage = approval.Error ?? "رمز المستلم غير صحيح."; return; }
                    (approvalId, approverId) = (approval.Value.ApprovalId, approval.Value.ApproverUserId);
                }
                request = new AcknowledgeShiftHandoverRequest($"ack-{Guid.NewGuid():N}", AcknowledgeTarget!.ShiftId, _session.CurrentUser!.Id,
                    received, string.IsNullOrWhiteSpace(ReceiptNote) ? null : ReceiptNote.Trim(), approvalId, approverId);
            }
            var result = await _acknowledgeRequest.SendAsync(request, r => _session.Api.AcknowledgeShiftHandoverAsync(r));
            if (!result.Success) { ReceiptMessage = _acknowledgeRequest.FailureMessage(result.Error); return; }
            AcknowledgeTarget = null;
        }
        finally
        {
            IsAcknowledging = false;
            ReceiverPin = "";
        }
        await LoadAsync();
    }
}
