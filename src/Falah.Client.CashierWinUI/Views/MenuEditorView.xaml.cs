using Falah.Client.CashierWinUI.Platform;
using Falah.Client.CashierWinUI.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace Falah.Client.CashierWinUI.Views;

public sealed partial class MenuEditorView : UserControl
{
    public MenuEditorView()
    {
        InitializeComponent();
        // The picker needs a window handle, which is the view's business, not the view model's -
        // so the view hands the view model a way to ask for a file rather than a way to find a
        // window. See MenuEditorViewModel.PickImageAsync.
        DataContextChanged += (_, _) =>
        {
            if (DataContext is MenuEditorViewModel vm) vm.PickImageAsync = ImageFilePicker.PickAsync;
        };
    }
}
