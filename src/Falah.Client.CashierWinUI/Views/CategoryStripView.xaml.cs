using Falah.Client.CashierWinUI.Models;
using Falah.Client.CashierWinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Falah.Client.CashierWinUI.Views;

public sealed partial class CategoryStripView : UserControl
{
    private CashierViewModel? ViewModel => DataContext as CashierViewModel;

    public CategoryStripView() { InitializeComponent(); }

    /// <summary>Puts the caret in the product search box and selects whatever is already there, so a
    /// second press of the shortcut starts a fresh search instead of appending to the last one.</summary>
    public void FocusSearch()
    {
        ProductSearchBox.Focus(FocusState.Programmatic);
        ProductSearchBox.SelectAll();
    }

    private void OnCategoryClicked(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is CategoryTile tile && ViewModel is { } vm)
            vm.SelectCategoryCommand.Execute(tile);
    }
}
