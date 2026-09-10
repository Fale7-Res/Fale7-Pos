namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>One entry in the management shell's side navigation. Permission is a raw
/// PermissionCatalog key (see Falah.Domain.Authorization.PermissionCatalog) - null means always
/// visible to anyone who reached the shell at all (e.g. "الكاشير" itself). ShellViewModel filters
/// the full candidate list down to only what AppSession.HasPermission grants, so a user never sees
/// a nav entry for a screen the backend would reject them from anyway.</summary>
public sealed record ShellNavItem(string Key, string Label, string? Permission);

/// <summary>
/// A top-bar entry. Twenty-two screens do not fit in one row, so related ones travel together and
/// the rest sit behind "المزيد".
///
/// Grouping is presentation only: every child is still the same permission-filtered ShellNavItem,
/// so a group whose children a user cannot open never appears at all - it is not shown and disabled,
/// it is absent, which is the same guarantee the flat list already gave.
/// </summary>
public sealed record ShellNavGroup(string Key, string Label, IReadOnlyList<ShellNavItem> Items)
{
    /// <summary>A group holding one screen behaves as that screen - no menu, one tap.</summary>
    public bool IsDirect => Items.Count == 1;
    public ShellNavItem Single => Items[0];
}

