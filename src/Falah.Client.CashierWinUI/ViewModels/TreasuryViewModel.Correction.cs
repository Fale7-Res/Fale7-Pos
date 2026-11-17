using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Treasury;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// P3: a hand-typed treasury entry recorded by mistake is corrected by its linked opposite (never edited or deleted).
/// The operator must confirm the money did NOT physically move - money that moved and came back is a new ordinary
/// entry - and may correct only part of it (the wrong-amount case). treasury_manage, or a manager's PIN. Kept on disk
/// until its outcome is known.
/// </summary>
public partial class TreasuryViewModel
{
    private readonly RetainedRequest<CorrectTreasuryMovementRequest> _correction = new("treasury.correction");

    [ObservableProperty] private MovementRow? _correctionTarget;
    [ObservableProperty] private string _correctionReason = "";
    [ObservableProperty] private string _correctionAmountText = "";
    [ObservableProperty] private bool _correctionConfirmedNotMoved;
    [ObservableProperty] private string _correctionManagerPin = "";
    [ObservableProperty] private string? _correctionMessage;
    [ObservableProperty] private bool _isCorrecting;

    public bool IsCorrectionOpen => CorrectionTarget is not null;
    public bool CorrectionNeedsManagerPin => !CanManage;

    partial void OnCorrectionTargetChanged(MovementRow? value) => OnPropertyChanged(nameof(IsCorrectionOpen));

    [RelayCommand]
    private void OpenCorrection(MovementRow? row)
    {
        if (row is not { IsCorrectable: true } || IsCorrecting || _correction.HasPending) return;
        CorrectionReason = "";
        CorrectionAmountText = "";
        CorrectionConfirmedNotMoved = false;
        CorrectionManagerPin = "";
        CorrectionMessage = null;
        CorrectionTarget = row;
    }

    [RelayCommand]
    private void CancelCorrection()
    {
        if (IsCorrecting || _correction.HasPending) return;
        CorrectionTarget = null;
    }

    [RelayCommand]
    private async Task ConfirmCorrectionAsync()
    {
        if (IsCorrecting) return;
        var pending = _correction.Value;
        long? part = null;
        if (pending is null)
        {
            if (CorrectionTarget is not { } target) return;
            if (string.IsNullOrWhiteSpace(CorrectionReason)) { CorrectionMessage = "اكتب سبب التصحيح."; return; }
            if (!CorrectionConfirmedNotMoved)
            { CorrectionMessage = "التصحيح يعني أن المال لم يتحرك فعلًا. إذا تحرك ثم عاد، سجّل حركة جديدة بدل التصحيح."; return; }
            if (!string.IsNullOrWhiteSpace(CorrectionAmountText))
            {
                if (!CashInput.TryParse(CorrectionAmountText, out var p) || p <= 0 || p > target.Movement.AmountPiastres)
                { CorrectionMessage = "مبلغ التصحيح يجب أن يكون أكبر من صفر ولا يتجاوز مبلغ الحركة."; return; }
                part = p;
            }
            if (CorrectionNeedsManagerPin && CorrectionManagerPin.Length < 4) { CorrectionMessage = "أدخل رمز المدير لاعتماد التصحيح."; return; }
        }

        IsCorrecting = true;
        CorrectionMessage = null;
        try
        {
            var request = pending;
            if (request is null)
            {
                string? approvalId = null, approverId = null;
                if (CorrectionNeedsManagerPin)
                {
                    var approval = await _session.Api.IssueManagerApprovalAsync(new Falah.Application.Approvals.IssueManagerApprovalRequest(
                        CorrectionManagerPin, "treasury_manage", CorrectionReason));
                    if (!approval.Success || approval.Value is null) { CorrectionMessage = approval.Error ?? "رمز المدير غير صحيح أو غير مخوّل."; return; }
                    (approvalId, approverId) = (approval.Value.ApprovalId, approval.Value.ApproverUserId);
                }
                request = new CorrectTreasuryMovementRequest($"corr-{Guid.NewGuid():N}", CorrectionTarget!.Movement.Id, CorrectionReason.Trim(),
                    _session.CurrentUser!.Id, ConfirmNotPhysicallyMoved: true, part, approvalId, approverId);
            }
            var result = await _correction.SendAsync(request, r => _session.Api.CorrectTreasuryMovementAsync(r));
            if (!result.Success) { CorrectionMessage = _correction.FailureMessage(result.Error); return; }
            CorrectionTarget = null;
        }
        finally
        {
            IsCorrecting = false;
            CorrectionManagerPin = "";
        }
        await LoadAsync();
    }
}
