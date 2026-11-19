using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Falah.Client.CallCenterWinUI.Views;

public sealed partial class AppHeaderView : UserControl
{
    /// <summary>The header's DataContext is HeaderViewModel, which has no Lock action of its own
    /// (Lock switches the whole app back to the login screen, which is MainViewModel's job) - so
    /// the button raises this event instead, and MainWindow wires it to MainViewModel.LockCommand.</summary>
    public event EventHandler? LockRequested;

    public AppHeaderView()
    {
        InitializeComponent();
        ThemeButton.Content = Services.ThemeService.Label(Services.ThemeService.Instance.Current);
    }

    /// <summary>The station's appearance, from the header: the Call Center has no settings screen.</summary>
    private void OnThemeClicked(object sender, RoutedEventArgs e)
    {
        var next = Services.ThemeService.Next(Services.ThemeService.Instance.Current);
        Services.ThemeService.Instance.Set(next);
        ThemeButton.Content = Services.ThemeService.Label(next);
    }

    private void OnLockClicked(object sender, RoutedEventArgs e) => LockRequested?.Invoke(this, EventArgs.Empty);
}
