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

    // Audit 2026-11-25 U16: hour and minute lists instead of the framework TimePicker (English "hour"/"minute").
    private readonly System.Collections.ObjectModel.ObservableCollection<string> _hours =
        new(Enumerable.Range(0, 24).Select(h => h.ToString("00")));
    private readonly System.Collections.ObjectModel.ObservableCollection<string> _minutes =
        new(Enumerable.Range(0, 12).Select(m => (m * 5).ToString("00")));

    public PickupDetailsView()
    {
        InitializeComponent();
        PickupHourBox.ItemsSource = _hours;
        PickupMinuteBox.ItemsSource = _minutes;
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
        // Audit 2026-11-25 U17: the cursor sat in the name field while the phone - the field the counter types
        // first and the one that finds a repeat customer - is first in the form.
        if (e.PropertyName == nameof(CashierViewModel.IsPickupEditorOpen) && _vm?.IsPickupEditorOpen == true)
            DispatcherQueue.TryEnqueue(() => PickupPhoneBox.Focus(Microsoft.UI.Xaml.FocusState.Programmatic));
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

    /// <summary>A time once both its hour and its minute are chosen; until then none.</summary>
    private void OnPickupTimePartChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_pushing || _vm is null) return;
        _vm.PickupTime = PickupHourBox.SelectedItem is string h && PickupMinuteBox.SelectedItem is string m
            ? new TimeSpan(int.Parse(h), int.Parse(m), 0)
            : null;
    }
}
