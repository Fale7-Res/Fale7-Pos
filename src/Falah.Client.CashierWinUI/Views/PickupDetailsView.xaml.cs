using System.ComponentModel;
using Falah.Client.CashierWinUI.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace Falah.Client.CashierWinUI.Views;

/// <summary>
/// The pickup editor. The two pickers are kept in step with <see cref="CashierViewModel.PickupDate"/> and
/// <see cref="CashierViewModel.PickupTime"/> here rather than by binding. Bound, a null reached the pickers as
/// a default value, so a pickup with no time showed the date "1/1/1926" (the calendar's MinDate) and the
/// time "0:00", as if a time had been chosen. Assigned in code, null stays null and the pickers show their
/// placeholders. The view model stays the only owner of the values; this class only copies them.
/// </summary>
public sealed partial class PickupDetailsView : UserControl
{
    private CashierViewModel? _vm;
    private bool _pushing;

    public PickupDetailsView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => Attach(DataContext as CashierViewModel);
    }

    private void Attach(CashierViewModel? vm)
    {
        if (ReferenceEquals(vm, _vm)) return;
        if (_vm is not null) _vm.PropertyChanged -= OnViewModelChanged;
        _vm = vm;
        if (_vm is not null) _vm.PropertyChanged += OnViewModelChanged;
        PushToPickers();
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(CashierViewModel.PickupDate) or nameof(CashierViewModel.PickupTime)
            or nameof(CashierViewModel.IsPickupEditorOpen))
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
