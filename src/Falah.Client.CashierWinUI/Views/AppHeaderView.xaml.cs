using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Falah.Client.CashierWinUI.Views;

public sealed partial class AppHeaderView : UserControl
{
    /// <summary>The header's DataContext is HeaderViewModel, which has no Lock action of its own
    /// (Lock switches the whole app back to the login screen, which is MainViewModel's job) - so
    /// the button raises this event instead, and MainWindow wires it to MainViewModel.LockCommand.</summary>
    public event EventHandler? LockRequested;

    /// <summary>The station runs full screen with no title bar, so the header carries the way out; MainWindow closes
    /// the program through the same guard as the title bar's X.</summary>
    public event EventHandler? ExitRequested;

    public AppHeaderView()
    {
        InitializeComponent();
    }

    private void OnLockClicked(object sender, RoutedEventArgs e) => LockRequested?.Invoke(this, EventArgs.Empty);

    private void OnExitClicked(object sender, RoutedEventArgs e) => ExitRequested?.Invoke(this, EventArgs.Empty);
}
