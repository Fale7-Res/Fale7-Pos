using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Falah.Client.CashierWinUI.Services;

public interface IPendingRequest
{
    string? OwnerKey { get; }
    bool HasPending { get; }
    bool IsSending { get; }
    string? LastError { get; }
    void DiscardAfterReview(bool serverRecordReviewed);
}
public interface IManagementRequestOwner
{
    string RecoveryLabel { get; }
    bool IsRecoveryBusy { get; }
    IEnumerable<IPendingRequest> RecoveryRequests { get; }
    void RefreshRecoveryState();
}

/// <summary>Explicit operator recovery. Removing a local question never cancels server work and
/// never resends it. The operator must inspect the server's record first.</summary>
public partial class ManagementRecovery : ObservableObject
{
    public ObservableCollection<RecoveryItem> Items { get; } = new();
    public bool HasItems => Items.Count > 0;
    public void Refresh(IEnumerable<IManagementRequestOwner> owners)
    {
        var pending = owners.SelectMany(o => o.RecoveryRequests.Where(r => r.HasPending).Select(r => (Owner: o, Request: r))).ToList();
        foreach (var old in Items.Where(i => !pending.Any(p => ReferenceEquals(p.Request, i.Request))).ToList()) Items.Remove(old);
        foreach (var p in pending)
        {
            var item = Items.FirstOrDefault(i => ReferenceEquals(i.Request, p.Request));
            if (item is null) Items.Add(item = new RecoveryItem(p.Owner, p.Request));
            item.Refresh();
        }
        OnPropertyChanged(nameof(HasItems));
    }
}

public partial class RecoveryItem : ObservableObject
{
    private readonly IManagementRequestOwner _owner;
    public IPendingRequest Request { get; }
    public string Label => _owner.RecoveryLabel;
    public string Status => Request.LastError ?? "نتيجة هذه المحاولة غير محسومة. تحقّق من سجل السيرفر أولًا.";
    [ObservableProperty] private bool _serverRecordReviewed;
    [ObservableProperty] private string? _error;
    public bool CanDiscard => ServerRecordReviewed && !_owner.IsRecoveryBusy && !Request.IsSending && Request.HasPending;
    public RecoveryItem(IManagementRequestOwner owner, IPendingRequest request) { _owner = owner; Request = request; }
    partial void OnServerRecordReviewedChanged(bool value) => Refresh();
    public void Refresh() { OnPropertyChanged(nameof(CanDiscard)); OnPropertyChanged(nameof(Status)); }
    [RelayCommand]
    private void Discard()
    {
        if (!CanDiscard) return;
        try { Request.DiscardAfterReview(true); _owner.RefreshRecoveryState(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException) { Error = ex.Message; }
    }
}

public sealed class DelegatePendingRequest(Func<bool> hasPending, Func<bool> isBusy, Action discard) : IPendingRequest
{
    public string? OwnerKey => null;
    public bool HasPending => hasPending();
    public bool IsSending => isBusy();
    public string? LastError => null;
    public void DiscardAfterReview(bool serverRecordReviewed)
    {
        if (!serverRecordReviewed || IsSending) throw new InvalidOperationException("راجع سجل السيرفر وانتظر انتهاء الاتصال أولًا.");
        discard();
    }
}
