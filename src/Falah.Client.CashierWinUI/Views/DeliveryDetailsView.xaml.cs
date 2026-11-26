using Falah.Application.Delivery;
using Falah.Client.CashierWinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Falah.Client.CashierWinUI.Views;

public sealed partial class DeliveryDetailsView : UserControl
{
    public DeliveryDetailsView() { InitializeComponent(); }

    private CashierViewModel? Vm => DataContext as CashierViewModel;

    // Owner, 2026-11-26: the delivery service is searched by typing (about 50 of them). Only the person's typing
    // searches; the box's own text updates (a chosen suggestion, the view model's value) do not.
    private void OnServiceTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput) Vm?.SearchDeliveryServices(sender.Text);
    }

    private void OnServiceChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (Vm is { } vm && args.SelectedItem is DeliveryServiceDto service) vm.SelectedDeliveryService = service;
    }

    /// <summary>Enter picks the highlighted suggestion, or the first one.</summary>
    private void OnServiceSubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (Vm is not { } vm) return;
        var service = args.ChosenSuggestion as DeliveryServiceDto ?? vm.DeliveryServiceSuggestions.FirstOrDefault();
        if (service is not null) vm.SelectedDeliveryService = service;
        sender.IsSuggestionListOpen = false;
    }

    /// <summary>On focus the list opens with what matches the text so far (every service when it is empty).</summary>
    private void OnServiceFocused(object sender, RoutedEventArgs e)
    {
        if (Vm is not { } vm || sender is not AutoSuggestBox box || vm.SelectedDeliveryService is not null) return;
        vm.SearchDeliveryServices(box.Text);
        box.IsSuggestionListOpen = vm.DeliveryServiceSuggestions.Count > 0;
    }
}
