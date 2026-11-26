using Falah.Client.CallCenterWinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Falah.Client.CallCenterWinUI.Views;

public sealed partial class CallCenterView : UserControl
{
    // Owner, 2026-11-25: the menu and its offers are re-read every minute while the desk is shown.
    private readonly DispatcherTimer _menuRefresh = new() { Interval = CallCenterViewModel.MenuRefreshInterval };

    public CallCenterView()
    {
        InitializeComponent();
        _menuRefresh.Tick += (_, _) =>
        {
            if (DataContext is CallCenterViewModel vm && vm.RefreshMenuQuietlyCommand.CanExecute(null)) vm.RefreshMenuQuietlyCommand.Execute(null);
        };
        Loaded += (_, _) => _menuRefresh.Start();
        Unloaded += (_, _) => _menuRefresh.Stop();
    }
}
