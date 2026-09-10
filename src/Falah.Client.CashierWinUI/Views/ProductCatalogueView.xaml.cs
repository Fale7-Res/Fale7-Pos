using Falah.Client.CashierWinUI.Models;
using Falah.Client.CashierWinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Falah.Client.CashierWinUI.Views;

public sealed partial class ProductCatalogueView : UserControl
{
    private CashierViewModel? ViewModel => DataContext as CashierViewModel;

    // Product cards need to stay comfortably tap-able (touch target guidance is roughly 9mm, well
    // under these) while making full use of whatever the window's actual width is - a 1366-wide POS
    // screen and a 1920-wide one should both show a dense, evenly-filled grid with no ragged gap on
    // the trailing edge, not the same fixed card width stretched into empty space or crammed tight.
    private const double ProductTargetWidth = 212, ProductMinWidth = 180, ProductMaxWidth = 250;

    public ProductCatalogueView()
    {
        InitializeComponent();
        ProductsGridView.SizeChanged += (_, e) => ApplyItemWidth(e.NewSize.Width);
        // ItemsPanelRoot may not be realized yet on the very first SizeChanged (template
        // application timing), so re-apply once more once the control is definitely fully loaded.
        Loaded += (_, _) => ApplyItemWidth(ProductsGridView.ActualWidth);
    }

    private void ApplyItemWidth(double availableWidth)
    {
        if (ProductsGridView.ItemsPanelRoot is not ItemsWrapGrid panel) return;
        var usable = availableWidth - ProductsGridView.Padding.Left - ProductsGridView.Padding.Right;
        if (usable <= 0) return;

        var columns = Math.Max(2, (int)(usable / ProductTargetWidth));
        panel.ItemWidth = usable / columns;
        // ItemHeight is intentionally left unset: a fixed cell height clips any card that grows
        // taller than the common case (an offer badge stacked below the card adds ~30px). Leaving
        // it auto lets each ROW size itself to its own tallest item - offer-less rows stay compact,
        // only a row that actually contains an offer grows, and nothing is ever cut off.
    }

    /// <summary>Product cards live in a GridView rather than plain Buttons so the grid gets touch-
    /// friendly item click/press feedback without the click handling ambiguity a Button nested
    /// inside an item-clickable GridView row would create.</summary>
    private void OnProductClicked(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ProductTile tile && ViewModel is { } vm)
            vm.SelectProductCommand.Execute(tile.Product);
    }

    /// <summary>Marking the event handled here is load-bearing, not decorative: without it the tap
    /// still bubbles to the GridViewItem and fires OnProductClicked for the base product right after
    /// this runs, so the offer picker/pending-offer state gets immediately clobbered by a plain
    /// product selection.</summary>
    private void OnOfferTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        e.Handled = true;
        if (sender is FrameworkElement { DataContext: OfferChoice choice } && ViewModel is { } vm)
            vm.SelectOfferCommand.Execute(choice);
    }
}
