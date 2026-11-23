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

    public CallCenterDeliveryEditorView()
    {
        InitializeComponent();
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
            PickupTimePicker.SelectedTime = _vm?.PickupTime;
        }
        finally { _pushing = false; }
    }

    private void OnPickupDateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
    {
        if (!_pushing && _vm is not null) _vm.PickupDate = sender.Date;
    }

    private void OnPickupTimeChanged(TimePicker sender, TimePickerSelectedValueChangedEventArgs args)
    {
        if (!_pushing && _vm is not null) _vm.PickupTime = sender.SelectedTime;
    }
}
