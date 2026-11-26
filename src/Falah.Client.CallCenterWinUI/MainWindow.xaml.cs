using System.ComponentModel;
using Falah.Client.CallCenterWinUI.ViewModels;
using Falah.Client.CallCenterWinUI.Views;
using Falah.Client.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace Falah.Client.CallCenterWinUI;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        // Before the tree is populated: RequestedTheme on the root is inherited by everything below
        // it, and every {ThemeResource} re-resolves on its own when it changes.
        RootGrid.RequestedTheme = Services.ThemeService.ToElementTheme(Services.ThemeService.Instance.Current);
        Services.ThemeService.Instance.Changed += OnThemeChanged;
        _viewModel = new MainViewModel();
        CloseNotice.DataContext = _viewModel;
        AppWindow.Closing += (_, args) => args.Cancel = !_viewModel.TryClose();
        Closed += (_, _) =>
        {
            Services.ThemeService.Instance.Changed -= OnThemeChanged;
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel.Stop();
        };

        HeaderHost.DataContext = _viewModel.Header;
        HeaderHost.LockRequested += (_, _) => _viewModel.LockCommand.Execute(null);
        HeaderHost.ExitRequested += (_, _) => RequestExit();
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        var fullScreen = new KeyboardAccelerator { Key = Windows.System.VirtualKey.F11 };
        fullScreen.Invoked += (_, args) => { args.Handled = true; StationWindow.ToggleFullScreen(AppWindow); };
        RootGrid.KeyboardAccelerators.Add(fullScreen);

        ApplyIsLoggedIn();
        ApplyIsOnline();
        ApplyCurrentView();
        // Install the initial screen before maximizing / full screen can activate native layout. Same reasoning as
        // CashierWinUI's MainWindow - real dedicated LAN station hardware, not a resizable desktop tool window.
        StationWindow.Prepare(AppWindow);
    }

    /// <summary>The header's «خروج» and the PIN screen's: the same guard as the title bar's X.</summary>
    private void RequestExit()
    {
        if (_viewModel.TryClose()) Close();
    }

    private void OnExitClicked(object sender, RoutedEventArgs e) => RequestExit();

    private void OnThemeChanged(Services.AppTheme theme) =>
        DispatcherQueue.TryEnqueue(() => RootGrid.RequestedTheme = Services.ThemeService.ToElementTheme(theme));

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MainViewModel.IsLoggedIn):
                ApplyIsLoggedIn();
                break;
            case nameof(MainViewModel.IsOnline):
                ApplyIsOnline();
                break;
            case nameof(MainViewModel.CurrentView):
                ApplyCurrentView();
                break;
            case nameof(MainViewModel.Section):
                ApplySection();
                break;
            case nameof(MainViewModel.CanSeeDelivery):
                ApplyDeliveryTab();
                break;
        }
    }

    private void ApplyIsLoggedIn()
    {
        HeaderHost.Visibility = _viewModel.IsLoggedIn ? Visibility.Visible : Visibility.Collapsed;
        SectionBar.Visibility = HeaderHost.Visibility;
        LoginExitButton.Visibility = _viewModel.IsLoggedIn ? Visibility.Collapsed : Visibility.Visible;
        ApplyDeliveryTab();
        ApplySection();
    }

    /// <summary>The delivery tab is taken out of the bar for a user without delivery_manage rather than collapsed,
    /// so the bar's selection and keyboard order never land on an item that is not there.</summary>
    private void ApplyDeliveryTab()
    {
        var shown = Sections.Items.Contains(DeliverySection);
        if (_viewModel.CanSeeDelivery && !shown) Sections.Items.Add(DeliverySection);
        else if (!_viewModel.CanSeeDelivery && shown) Sections.Items.Remove(DeliverySection);
    }

    /// <summary>The bar follows the view model (a lock returns to the order desk); a click on the bar asks the
    /// view model to navigate, which may refuse (the delivery tab without delivery_manage).</summary>
    private void ApplySection()
    {
        var item = Sections.Items.FirstOrDefault(i => (string)i.Tag == _viewModel.Section);
        if (item is not null && !ReferenceEquals(Sections.SelectedItem, item)) Sections.SelectedItem = item;
    }

    private void OnSectionChanged(Microsoft.UI.Xaml.Controls.SelectorBar sender, Microsoft.UI.Xaml.Controls.SelectorBarSelectionChangedEventArgs args)
    {
        if (sender.SelectedItem?.Tag is string section && section != _viewModel.Section)
            _viewModel.NavigateCommand.Execute(section);
        ApplySection();
    }

    private void ApplyIsOnline() =>
        OfflineBanner.Visibility = _viewModel.IsOnline ? Visibility.Collapsed : Visibility.Visible;

    private void ApplyCurrentView()
    {
        RootContent.Content = _viewModel.CurrentView switch
        {
            LoginViewModel vm => new LoginView { DataContext = vm },
            CallCenterViewModel vm => new CallCenterView { DataContext = vm },
            DeliveryViewModel vm => new DeliveryView { DataContext = vm },
            OrdersViewModel vm => new OrdersView { DataContext = vm },
            _ => null,
        };
    }
}
