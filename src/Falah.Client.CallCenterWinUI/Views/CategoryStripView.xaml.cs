using Falah.Client.CallCenterWinUI.Models;
using Falah.Client.CallCenterWinUI.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace Falah.Client.CallCenterWinUI.Views;

public sealed partial class CategoryStripView : UserControl
{
    private CallCenterViewModel? ViewModel => DataContext as CallCenterViewModel;

    public CategoryStripView() { InitializeComponent(); }

    private void OnCategoryClicked(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is CategoryTile tile && ViewModel is { } vm)
            vm.SelectCategoryCommand.Execute(tile);
    }
}
