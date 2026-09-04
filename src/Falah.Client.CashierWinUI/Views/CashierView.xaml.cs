using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Falah.Client.CashierWinUI.Views;

public sealed partial class CashierView : UserControl
{
    // The cart column stays a fixed reading width regardless of order size (that's the point - it
    // must not grow/shrink with the cart's own content), but that width should still be
    // proportioned to the actual screen: too narrow on a 1366-wide POS crowds the order-type tabs
    // and price text, too wide on a 1920+ one steals rows from the product grid for no benefit.
    private const double CartWidthRatio = 0.22, CartMinWidth = 340, CartMaxWidth = 440;

    public CashierView()
    {
        InitializeComponent();
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
