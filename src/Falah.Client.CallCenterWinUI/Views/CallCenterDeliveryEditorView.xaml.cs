using System.ComponentModel;
using Falah.Client.CallCenterWinUI.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace Falah.Client.CallCenterWinUI.Views;

/// <summary>
/// The customer editor (delivery address, or pickup name and optional time). The pickup pickers are kept in step
/// with <see cref="CallCenterViewModel.PickupDate"/> and <see cref="CallCenterViewModel.PickupTime"/> here rather
/// than by binding, for the reason the cashier's PickupDetailsView found: bound, a null reached the pickers as the
/// calendar's MinDate "1/1/1926" and "0:00", as if a time had been chosen. Assigned in code, null stays null and
/// the pickers show their placeholders. The view model stays the only owner of the values.
/// </summary>
public sealed partial class CallCenterDeliveryEditorView : UserControl
{
    private CallCenterViewModel? _vm;
    private bool _pushing;

    // Audit 2026-11-25 U16: hour and minute lists instead of the framework TimePicker (English "hour"/"minute").
    private readonly System.Collections.ObjectModel.ObservableCollection<string> _hours =
        new(Enumerable.Range(0, 24).Select(h => h.ToString("00")));
    private readonly System.Collections.ObjectModel.ObservableCollection<string> _minutes =
        new(Enumerable.Range(0, 12).Select(m => (m * 5).ToString("00")));

    public CallCenterDeliveryEditorView()
    {
        InitializeComponent();
        PickupHourBox.ItemsSource = _hours;
        PickupMinuteBox.ItemsSource = _minutes;
        DataContextChanged += (_, _) => Attach(DataContext as CallCenterViewModel);
    }

    private void Attach(CallCenterViewModel? vm)
    {
        if (ReferenceEquals(vm, _vm)) return;
        if (_vm is not null) _vm.PropertyChanged -= OnViewModelChanged;
        _vm = vm;
        if (_vm is not null) _vm.PropertyChanged += OnViewModelChanged;
        PushToPickers();
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(CallCenterViewModel.PickupDate) or nameof(CallCenterViewModel.PickupTime)
            or nameof(CallCenterViewModel.IsDeliveryEditorOpen))
            PushToPickers();
    }

    private void PushToPickers()
    {
        _pushing = true;
        try
        {
            PickupDatePicker.Date = _vm?.PickupDate;
            if (_vm?.PickupTime is { } time)
            {
                var minute = time.Minutes.ToString("00");
                // A time held from before (not on the 5-minute grid) keeps its exact minute.
                if (!_minutes.Contains(minute)) _minutes.Insert(_minutes.TakeWhile(m => string.CompareOrdinal(m, minute) < 0).Count(), minute);
                PickupHourBox.SelectedItem = time.Hours.ToString("00");
                PickupMinuteBox.SelectedItem = minute;
            }
            else
            {
                PickupHourBox.SelectedIndex = -1;
                PickupMinuteBox.SelectedIndex = -1;
            }
        }
        finally { _pushing = false; }
    }

    private void OnPickupDateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
    {
        if (!_pushing && _vm is not null) _vm.PickupDate = sender.Date;
    }

    // Owner, 2026-11-26: the delivery service is searched by typing (about 50 of them). Only the person's typing
    // searches; the box's own text updates (a chosen suggestion, the view model's value) do not.
    private void OnServiceTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput) _vm?.SearchDeliveryServices(sender.Text);
    }

    private void OnServiceChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (_vm is not null && args.SelectedItem is Falah.Application.Delivery.DeliveryServiceDto service) _vm.SelectedDeliveryService = service;
    }

    /// <summary>Enter picks the highlighted suggestion, or the first one.</summary>
    private void OnServiceSubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (_vm is null) return;
        var service = args.ChosenSuggestion as Falah.Application.Delivery.DeliveryServiceDto ?? _vm.DeliveryServiceSuggestions.FirstOrDefault();
        if (service is not null) _vm.SelectedDeliveryService = service;
        sender.IsSuggestionListOpen = false;
    }

    /// <summary>On focus the list opens with what matches the text so far (every service when it is empty).</summary>
    private void OnServiceFocused(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_vm is null || sender is not AutoSuggestBox box || _vm.SelectedDeliveryService is not null) return;
        _vm.SearchDeliveryServices(box.Text);
        box.IsSuggestionListOpen = _vm.DeliveryServiceSuggestions.Count > 0;
    }

    /// <summary>A time once both its hour and its minute are chosen; until then none.</summary>
    private void OnPickupTimePartChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_pushing || _vm is null) return;
        _vm.PickupTime = PickupHourBox.SelectedItem is string h && PickupMinuteBox.SelectedItem is string m
            ? new TimeSpan(int.Parse(h), int.Parse(m), 0)
            : null;
    }
}
