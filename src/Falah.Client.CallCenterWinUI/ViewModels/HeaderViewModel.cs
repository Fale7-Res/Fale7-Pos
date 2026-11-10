using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Falah.Client.CallCenterWinUI.Services;
using Microsoft.UI.Xaml;

namespace Falah.Client.CallCenterWinUI.ViewModels;

/// <summary>
/// The persistent dark app header (logo, restaurant name, shift status pill, clock, user badge) -
/// present on every screen once logged in, matching the reference app's AppShell header exactly in
/// content and structure. Kept as its own view model/view so future screens (Call Center, etc.)
/// reuse this same header instead of re-declaring it.
/// </summary>
public partial class HeaderViewModel : ObservableObject
{
    public void Stop() => _clockTimer.Stop();
    private readonly AppSession _session;
    private readonly DispatcherTimer _clockTimer;

    public HeaderViewModel(AppSession session)
    {
        _session = session;
        UpdateClock();
        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _clockTimer.Tick += (_, _) => UpdateClock();
        _clockTimer.Start();
    }

    // Shown until Settings supplies the configured name (and if Settings cannot be read).
    private string _restaurantName = "فالح أبو العنبه";
    public string RestaurantName => _restaurantName;
    public void SetIdentity(string name) { _restaurantName = name; OnPropertyChanged(nameof(RestaurantName)); }
    public string LogoLetter => "ف";

    [ObservableProperty] private string _timeText = "";
    [ObservableProperty] private string _dateText = "";

    private void UpdateClock()
    {
        var now = DateTime.Now;
        var arEg = new CultureInfo("ar-EG");
        TimeText = now.ToString("hh:mm tt", arEg);
        DateText = now.ToString("dddd، d MMMM", arEg);
    }

    public string UserName => _session.CurrentUser?.Name ?? "";
    public string UserInitial => string.IsNullOrEmpty(UserName) ? "" : UserName[..1];
    public string RoleLabel => _session.CurrentUser?.Role switch
    {
        "owner" => "المالك",
        "manager" => "المدير",
        "cashier" => "كاشير",
        "supervisor" => "مشرف",
        _ => _session.CurrentUser?.Role ?? "",
    };

    public bool CanLock => !_session.HasUnresolvedOperation;
    public bool IsShiftOpen => _session.ShiftId is not null;
    public string ShiftLabel => _session.ShiftId is not null ? (_session.ShiftName ?? "وردية مفتوحة") : "مغلق";

    public void Refresh()
    {
        OnPropertyChanged(nameof(CanLock));
        OnPropertyChanged(nameof(IsShiftOpen));
        OnPropertyChanged(nameof(ShiftLabel));
        OnPropertyChanged(nameof(UserName));
        OnPropertyChanged(nameof(UserInitial));
        OnPropertyChanged(nameof(RoleLabel));
    }
}
