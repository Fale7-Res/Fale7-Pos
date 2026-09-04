using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Falah.Client.CashierWinUI.Views;

public sealed partial class AppHeaderView : UserControl
{
    /// <summary>The header's DataContext is HeaderViewModel, which has no Lock action of its own
    /// (Lock switches the whole app back to the login screen, which is MainViewModel's job) - so
    /// the button raises this event instead, and MainWindow wires it to MainViewModel.LockCommand.</summary>
    public event EventHandler? LockRequested;

    public AppHeaderView()
    {
        InitializeComponent();
    }

    private void OnLockClicked(object sender, RoutedEventArgs e) => LockRequested?.Invoke(this, EventArgs.Empty);
}
