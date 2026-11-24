using Microsoft.UI.Xaml.Controls;

namespace Falah.Client.CallCenterWinUI.Views;

public sealed partial class DeliveryView : UserControl
{
    // Audit 2026-11-23, F-16: the board refreshes itself while this screen is shown (DeliveryViewModel.AutoRefresh).
    private readonly Microsoft.UI.Xaml.DispatcherTimer _refresh = new() { Interval = ViewModels.DeliveryViewModel.AutoRefreshInterval };

    public DeliveryView()
    {
        InitializeComponent();
        _refresh.Tick += (_, _) =>
        {
            if (DataContext is ViewModels.DeliveryViewModel vm && vm.AutoRefreshCommand.CanExecute(null)) vm.AutoRefreshCommand.Execute(null);
        };
        Loaded += (_, _) => _refresh.Start();
        Unloaded += (_, _) => _refresh.Stop();
    }
}
