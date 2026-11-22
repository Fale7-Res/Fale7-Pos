namespace Falah.Client.CashierWinUI.Services;

using Falah.Application.Approvals;

/// <summary>SEC-0030 (owner decision 2026-11-22): a reprint needs print_reprint or a manager's approval.
/// A screen without the permission shows a manager-PIN box; the PIN is exchanged for an approval by
/// the server (never checked here) and the approval travels with the reprint. The server decides.</summary>
public static class ReprintApproval
{
    public const string Permission = "print_reprint";

    public static bool IsNeeded(AppSession session) => !session.HasPermission(Permission);

    public static async Task<(string? ApprovalId, string? ApproverUserId, string? Error)> ResolveAsync(
        AppSession session, string managerPin, string reason)
    {
        if (!IsNeeded(session)) return (null, null, null);
        var pin = (managerPin ?? "").Trim();
        if (pin.Length < 4) return (null, null, "إعادة الطباعة تحتاج رمز المدير.");
        var approval = await session.Api.IssueManagerApprovalAsync(new IssueManagerApprovalRequest(pin, Permission, reason));
        if (!approval.Success || approval.Value is null)
            return (null, null, approval.Error ?? "رمز المدير غير صحيح أو غير مخوّل باعتماد إعادة الطباعة.");
        return (approval.Value.ApprovalId, approval.Value.ApproverUserId, null);
    }
}
