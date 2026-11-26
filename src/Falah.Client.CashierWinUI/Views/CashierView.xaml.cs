using Falah.Client.CashierWinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Falah.Client.CashierWinUI.Views;

public sealed partial class CashierView : UserControl
{
    // The cart column stays a fixed reading width regardless of order size (that's the point - it
    // must not grow/shrink with the cart's own content), but that width should still be
    // proportioned to the actual screen: too narrow on a 1366-wide POS crowds the order-type tabs
    // and price text, too wide on a 1920+ one steals rows from the product grid for no benefit.
    private const double CartWidthRatio = 0.30, CartMinWidth = 350, CartMaxWidth = 420;

    // Owner, 2026-11-25: the menu and its offers are re-read every minute while the till is shown.
    private readonly DispatcherTimer _menuRefresh = new() { Interval = CashierViewModel.MenuRefreshInterval };

    public CashierView()
    {
        InitializeComponent();
        _menuRefresh.Tick += (_, _) =>
        {
            if (ViewModel is { } vm && vm.RefreshMenuQuietlyCommand.CanExecute(null)) vm.RefreshMenuQuietlyCommand.Execute(null);
        };
        Loaded += (_, _) => _menuRefresh.Start();
        Unloaded += (_, _) => _menuRefresh.Stop();
    }

    private CashierViewModel? ViewModel => DataContext as CashierViewModel;

    /// <summary>Whether a function key should do anything right now. The accelerators live on the
    /// workspace grid, so a closed shift already rules them out; what remains is a modal or an
    /// in-flight operation sitting on top of the workspace, where a key that reached past it and
    /// changed the order underneath would be worse than a key that does nothing.</summary>
    private bool IsWorkspaceInteractive =>
        ViewModel?.CanUseWorkspaceShortcuts == true;

    private void OnSearchAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        if (!IsWorkspaceInteractive) return;
        CategoryStrip.FocusSearch();
    }

    private void OnOperationsAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        if (!IsWorkspaceInteractive) return;
        CashierOperationsButton.Flyout?.ShowAt(CashierOperationsButton);
    }

    private void OnCheckoutAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        // Deliberately routed through the same guard the button uses, so the key can never do what
        // the disabled button refuses to; IsWorkspaceInteractive additionally keeps it from
        // re-opening a checkout sheet that is already on screen.
        if (!IsWorkspaceInteractive) return;
        if (ViewModel is { CanOpenCheckout: true } vm) vm.OpenCheckoutCommand.Execute(null);
    }

    private void OnWorkspaceSizeChanged(object sender, SizeChangedEventArgs e)
    {
        var width = e.NewSize.Width;
        // WorkspaceGrid starts Collapsed (no shift open yet) and this first fires the moment it
        // becomes Visible, from inside WinUI's own in-flight layout pass for that visibility change.
        // Setting ColumnDefinition.Width synchronously here - even followed by an explicit
        // InvalidateMeasure/UpdateLayout - does not make the CURRENT pass pick it up: WinUI ignores
        // reentrant layout requests raised from inside an active layout pass. The column ends up
        // rendered at its old XAML-declared width (390) even though ColumnDefinition.Width correctly
        // reports the new value - confirmed by direct inspection, not a guess. Deferring the actual
        // assignment to the next dispatcher tick (i.e. after this pass has fully finished) is what
        // makes it actually take effect on screen.
        DispatcherQueue.TryEnqueue(() =>
        {
            var target = Math.Clamp(width * CartWidthRatio, CartMinWidth, CartMaxWidth);
            CartColumn.Width = new GridLength(target);
        });
    }
}
