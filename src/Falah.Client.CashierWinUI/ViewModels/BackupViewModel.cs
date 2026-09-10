using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Backup;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// Backups: create and verify.
///
/// RESTORE is deliberately NOT exposed on this screen. A restore is the one command in this system
/// that can erase committed financial history - the backend guards it with a mandatory reason, a
/// mandatory literal confirmation phrase, a zero-active-operations check and a manager approval on
/// top of `backup_manage`. Putting it one click away from "create backup" in a management screen
/// would undermine all four guards by making it look like a peer of the routine actions beside it.
/// Verification (RESTORE VERIFYONLY) is safe against a live system and IS offered here, because a
/// backup nobody has verified is only a hope.
/// </summary>
public partial class BackupViewModel : ObservableObject
{
    private readonly AppSession _session;

    public BackupViewModel(AppSession session) => _session = session;

    public ObservableCollection<BackupRecordDto> Backups { get; } = new();

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _loadError;
    [ObservableProperty] private string? _actionNote;
    [ObservableProperty] private string? _actionError;
    [ObservableProperty] private bool _isBusy;

    public bool HasNoBackups => !IsLoading && LoadError is null && Backups.Count == 0;
    public bool CanManage => _session.HasPermission("backup_manage");

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        LoadError = null;
        try
        {
            var result = await _session.Api.GetBackupsAsync();
            if (!result.Success || result.Value is null)
            {
                LoadError = result.Error ?? "تعذر تحميل سجل النسخ الاحتياطي.";
                return;
            }
            Backups.Clear();
            foreach (var b in result.Value.OrderByDescending(b => b.RequestedAtUtc)) Backups.Add(b);
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasNoBackups));
        }
    }

    [RelayCommand]
    private async Task CreateBackupAsync()
    {
        if (IsBusy || !CanManage) return;
        IsBusy = true;
        ActionError = null;
        ActionNote = null;
        try
        {
            // Destination left null: the server falls back to its own configured backup directory,
            // which is the right default for a station that has no view of the server's filesystem.
            var result = await _session.Api.CreateBackupAsync(new CreateBackupRequest(_session.CurrentUser!.Id));
            if (!result.Success || result.Value is null)
            {
                ActionError = result.Error ?? "تعذر إنشاء نسخة احتياطية.";
                return;
            }
            ActionNote = "تم إنشاء نسخة احتياطية. يُنصح بالتحقق منها الآن.";
            await LoadAsync();
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task VerifyBackupAsync(BackupRecordDto backup)
    {
        if (IsBusy || !CanManage) return;
        IsBusy = true;
        ActionError = null;
        ActionNote = null;
        try
        {
            var result = await _session.Api.VerifyBackupAsync(new VerifyBackupRequest(backup.Id, _session.CurrentUser!.Id));
            if (!result.Success || result.Value is null)
            {
                ActionError = result.Error ?? "تعذر التحقق من النسخة.";
                return;
            }
            ActionNote = result.Value.VerificationStatus == "Verified"
                ? "النسخة سليمة وقابلة للاستعادة."
                : $"نتيجة التحقق: {result.Value.VerificationStatus}";
            await LoadAsync();
        }
        finally { IsBusy = false; }
    }

    // ---------------- Restore ----------------

    private readonly RetainedRequest<RestoreBackupRequest> _restore = new("backup.restore");

    [ObservableProperty] private BackupRecordDto? _restoreTarget;
    [ObservableProperty] private string _restoreReason = "";
    [ObservableProperty] private string _restoreConfirmation = "";
    [ObservableProperty] private string _restoreManagerPin = "";
    [ObservableProperty] private string? _restoreError;

    /// <summary>An unresolved restore must block navigation and locking like every other in-flight
    /// management write - more so, since its outcome decides whether the database was replaced.</summary>
    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanSubmitRestore));
        OnPropertyChanged(nameof(RestoreLabel));
        _session.SetManagementOperationPending(this, IsBusy || _restore.HasPending);
    }

    public bool IsRestoreOpen => RestoreTarget is not null;
    partial void OnRestoreTargetChanged(BackupRecordDto? value) => OnPropertyChanged(nameof(IsRestoreOpen));

    /// <summary>The exact phrase the server demands, read from the domain rule itself so the two can
    /// never drift apart.</summary>
    public string RequiredRestorePhrase => Falah.Domain.Backup.BackupRules.RequiredRestoreConfirmationPhrase;

    public string RestorePhrasePrompt => $"اكتب {RequiredRestorePhrase} للتأكيد";

    /// <summary>The server authorises on the permission first and only falls back to a manager
    /// approval, so the PIN is asked for exactly when this operator cannot authorise it alone.</summary>
    public bool RestoreNeedsApproval => !CanManage;

    public bool CanSubmitRestore => !IsBusy;

    public string RestoreLabel => IsBusy ? "جاري التنفيذ والتحقق..."
        : _restore.HasPending ? "التحقق من المحاولة السابقة" : "تنفيذ الاستعادة";

    [RelayCommand]
    private void OpenRestore(BackupRecordDto backup)
    {
        if (IsBusy || _restore.HasPending || backup is null) return;
        RestoreError = null;
        RestoreReason = "";
        RestoreConfirmation = "";
        RestoreManagerPin = "";
        RestoreTarget = backup;
        OnPropertyChanged(nameof(RestoreNeedsApproval));
    }

    [RelayCommand]
    private void CloseRestore()
    {
        if (IsBusy || _restore.HasPending) return;
        RestoreTarget = null;
    }

    [RelayCommand]
    private async Task SubmitRestoreAsync()
    {
        if (IsBusy) return;
        RestoreError = null;

        var request = _restore.Value;
        if (request is null)
        {
            if (RestoreTarget is null || _session.CurrentUser is null) return;
            if (string.IsNullOrWhiteSpace(RestoreReason)) { RestoreError = "سبب الاستعادة مطلوب."; return; }
            if (!string.Equals(RestoreConfirmation, RequiredRestorePhrase, StringComparison.Ordinal))
            { RestoreError = $"يجب كتابة عبارة التأكيد بالضبط: {RequiredRestorePhrase}"; return; }

            IsBusy = true;
            try
            {
                var (approvalId, approverId, ok) = await ResolveRestoreApprovalAsync();
                if (!ok) return;
                request = new RestoreBackupRequest(RestoreTarget.Id, RestoreReason.Trim(), RestoreConfirmation,
                    _session.CurrentUser.Id, approvalId, approverId);
                await CompleteRestoreAsync(request);
            }
            finally { IsBusy = false; }
            return;
        }

        IsBusy = true;
        try { await CompleteRestoreAsync(request); }
        finally { IsBusy = false; }
    }

    private async Task CompleteRestoreAsync(RestoreBackupRequest request)
    {
        var result = await _restore.SendAsync(request, r => _session.Api.RestoreBackupAsync(r));
        if (!result.Success || result.Value is null)
        {
            RestoreError = _restore.FailureMessage(result.Error);
            return;
        }
        // The server records a Blocked restore rather than throwing when a precondition fails, so a
        // successful HTTP response is not by itself proof that the database was restored.
        var record = result.Value;
        if (string.Equals(record.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            ActionNote = "تمت استعادة قاعدة البيانات من النسخة الاحتياطية.";
        else
        {
            RestoreError = record.ErrorMessage ?? $"لم تكتمل الاستعادة. الحالة: {record.Status}";
            await LoadAsync();
            return;
        }
        RestoreTarget = null;
        await LoadAsync();
    }

    private async Task<(string? ApprovalId, string? ApproverUserId, bool Ok)> ResolveRestoreApprovalAsync()
    {
        if (!RestoreNeedsApproval) return (null, null, true);
        if (RestoreManagerPin.Length < 4)
        {
            RestoreError = "أدخل رمز المدير المخوّل باعتماد الاستعادة.";
            return (null, null, false);
        }
        var login = await _session.Api.LoginByPinAsync(RestoreManagerPin);
        if (!login.Success || login.Value is null)
        {
            RestoreError = login.Error ?? "تعذر التحقق من رمز المدير.";
            return (null, null, false);
        }
        var approval = await _session.Api.IssueManagerApprovalAsync(
            new Falah.Application.Approvals.IssueManagerApprovalRequest(login.Value.Id, "backup_manage", RestoreReason.Trim()));
        if (!approval.Success || approval.Value is null)
        {
            RestoreError = approval.Error ?? "هذا المستخدم غير مخول باعتماد الاستعادة.";
            return (null, null, false);
        }
        return (approval.Value.ApprovalId, login.Value.Id, true);
    }
}
