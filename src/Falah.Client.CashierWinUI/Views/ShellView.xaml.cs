using System.ComponentModel;
using Falah.Client.CashierWinUI.Converters;
using Falah.Client.CashierWinUI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace Falah.Client.CashierWinUI.Views;

public sealed partial class ShellView : UserControl
{
    private static readonly NavKeyToGlyphConverter GlyphConverter = new();

    private ShellViewModel? _viewModel;
    private readonly List<(string GroupKey, Button Button, IReadOnlyList<string> Screens)> _navButtons = new();

    public ShellView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => AttachViewModel();
    }

    private void AttachViewModel()
    {
        if (_viewModel is not null)
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

        _viewModel = DataContext as ShellViewModel;
        if (_viewModel is null) return;

        BuildNavBar();
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        ApplyCurrentPage();
        ApplySelectedItem();
    }

    /// <summary>Builds the bar directly in C#, one button per permitted group. Permissions are fixed
    /// for the life of a login, so a single build is all that is ever needed - and constructing the
    /// controls here rather than through an items template keeps the shell clear of the recycled
    /// container path that crashed this app's previous NavigationView (see ShellView.xaml).</summary>
    private void BuildNavBar()
    {
        NavBar.Children.Clear();
        _navButtons.Clear();
        if (_viewModel is null) return;

        foreach (var group in _viewModel.NavGroups)
        {
            var button = CreateNavButton(group);
            NavBar.Children.Add(button);
            _navButtons.Add((group.Key, button, group.Items.Select(i => i.Key).ToList()));
        }
    }

    private Button CreateNavButton(ShellNavGroup group)
    {
        var content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 7 };
        content.Children.Add(new FontIcon
        {
            Glyph = (string)GlyphConverter.Convert(group.Items[0].Key, typeof(string), null!, string.Empty),
            FontSize = 14,
        });
        content.Children.Add(new TextBlock { Text = group.Label, FontSize = 13, VerticalAlignment = VerticalAlignment.Center });

        // A group holding several screens opens a menu; one holding a single screen goes straight
        // there. A chevron appears only where there is actually something to expand.
        if (!group.IsDirect)
            content.Children.Add(new FontIcon { Glyph = "", FontSize = 9, VerticalAlignment = VerticalAlignment.Center });

        var button = new Button
        {
            Content = content,
            Style = (Style)global::Microsoft.UI.Xaml.Application.Current.Resources["ShellNavButtonStyle"],
            Tag = group.Key,
        };
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(button, group.Label);

        if (group.IsDirect)
        {
            var item = group.Single;
            button.Click += (_, _) => _viewModel?.NavigateCommand.Execute(item);
            return button;
        }

        var flyout = new MenuFlyout { Placement = FlyoutPlacementMode.Bottom };
        foreach (var item in group.Items)
        {
            var entry = new MenuFlyoutItem { Text = item.Label, Tag = item.Key };
            entry.Click += (_, _) => _viewModel?.NavigateCommand.Execute(item);
            flyout.Items.Add(entry);
        }
        button.Flyout = flyout;
        return button;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ShellViewModel.CurrentPage):
                ApplyCurrentPage();
                break;
            case nameof(ShellViewModel.SelectedItem):
                ApplySelectedItem();
                break;
        }
    }

    /// <summary>Same manual content-swap pattern as MainWindow.xaml.cs's ApplyCurrentView - WinUI's
    /// ContentControl has no implicit DataTemplate-by-CLR-type resolution.</summary>
    private void ApplyCurrentPage()
    {
        PageHost.Content = _viewModel?.CurrentPage switch
        {
            CashierViewModel vm => new CashierView { DataContext = vm },
            DeliveryViewModel vm => new DeliveryView { DataContext = vm },
            OwnerDashboardViewModel vm => new OwnerDashboardView { DataContext = vm },
            ReportsViewModel vm => new ReportsView { DataContext = vm },
            DailyStatementViewModel vm => new DailyStatementView { DataContext = vm },
            DailySummaryViewModel vm => new DailySummaryView { DataContext = vm },
            InventoryViewModel vm => new InventoryView { DataContext = vm },
            TreasuryViewModel vm => new TreasuryView { DataContext = vm },
            ShiftsViewModel vm => new ShiftsView { DataContext = vm },
            PurchasingViewModel vm => new PurchasingView { DataContext = vm },
            RecipesViewModel vm => new RecipesView { DataContext = vm },
            WasteViewModel vm => new WasteView { DataContext = vm },
            EmployeesViewModel vm => new EmployeesView { DataContext = vm },
            AttendanceViewModel vm => new AttendanceView { DataContext = vm },
            PayrollViewModel vm => new PayrollView { DataContext = vm },
            UsersViewModel vm => new UsersView { DataContext = vm },
            AuditViewModel vm => new AuditView { DataContext = vm },
            MenuViewModel vm => new MenuView { DataContext = vm },
            SettingsViewModel vm => new SettingsView { DataContext = vm },
            BackupViewModel vm => new BackupView { DataContext = vm },
            ControlCenterViewModel vm => new ControlCenterView { DataContext = vm },
            PrintingViewModel vm => new PrintingView { DataContext = vm },
            OrdersViewModel vm => new OrdersView { DataContext = vm },
            CustomersViewModel vm => new CustomersView { DataContext = vm },
            ShellPlaceholderViewModel vm => new ShellPlaceholderView { DataContext = vm },
            _ => null,
        };
    }

    /// <summary>Marks the group that owns the open screen, not the screen itself - the bar shows
    /// groups, so highlighting anything else would point at a button that is not there.</summary>
    private void ApplySelectedItem()
    {
        var key = _viewModel?.SelectedItem?.Key;
        foreach (var (_, button, screens) in _navButtons)
        {
            var isCurrent = key is not null && screens.Contains(key);
            button.Background = isCurrent
                ? (Brush)global::Microsoft.UI.Xaml.Application.Current.Resources["BrandPrimaryBrush"]
                : new SolidColorBrush(Colors.Transparent);
            button.Foreground = isCurrent
                ? (Brush)global::Microsoft.UI.Xaml.Application.Current.Resources["TextOnDarkBrush"]
                : (Brush)global::Microsoft.UI.Xaml.Application.Current.Resources["TextOnDarkMutedBrush"];
        }
    }
}
