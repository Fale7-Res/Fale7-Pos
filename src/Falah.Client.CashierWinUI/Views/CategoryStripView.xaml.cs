using Falah.Client.CashierWinUI.Models;
using Falah.Client.CashierWinUI.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace Falah.Client.CashierWinUI.Views;

public sealed partial class CategoryStripView : UserControl
{
    private CashierViewModel? ViewModel => DataContext as CashierViewModel;

    private const double CategoryTargetWidth = 132, CategoryMinWidth = 108, CategoryMaxWidth = 168, CategoryHeight = 84;

    public CategoryStripView()
    {
        InitializeComponent();
        CategoriesGridView.SizeChanged += (_, e) => ApplyItemWidth(e.NewSize.Width);
        Loaded += (_, _) => ApplyItemWidth(CategoriesGridView.ActualWidth);
    }

    private void ApplyItemWidth(double availableWidth)
    {
        if (CategoriesGridView.ItemsPanelRoot is not ItemsWrapGrid panel) return;
        var usable = availableWidth - CategoriesGridView.Padding.Left - CategoriesGridView.Padding.Right;
        if (usable <= 0) return;

        var columns = Math.Max(3, (int)(usable / CategoryTargetWidth));
        panel.ItemWidth = Math.Clamp(usable / columns, CategoryMinWidth, CategoryMaxWidth);
        panel.ItemHeight = CategoryHeight;
    }

    private void OnCategoryClicked(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is CategoryTile tile && ViewModel is { } vm)
            vm.SelectCategoryCommand.Execute(tile);
    }
}
