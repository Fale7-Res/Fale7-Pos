using System.ComponentModel;
using Falah.Client.CallCenterWinUI.ViewModels;
using Falah.Client.CallCenterWinUI.Views;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

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
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        ApplyIsLoggedIn();
        ApplyIsOnline();
        ApplyCurrentView();
        // Install the initial screen before maximizing can activate native layout.
        MaximizeOnTargetDisplay();
    }

    /// <summary>Same reasoning as CashierWinUI's own MainWindow - real dedicated LAN station
    /// hardware, not a resizable desktop tool window.</summary>
    private void MaximizeOnTargetDisplay()
    {
        if (AppWindow.Presenter is OverlappedPresenter presenter)
            presenter.Maximize();
    }

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
        }
    }

    private void ApplyIsLoggedIn() =>
        HeaderHost.Visibility = _viewModel.IsLoggedIn ? Visibility.Visible : Visibility.Collapsed;

    private void ApplyIsOnline() =>
        OfflineBanner.Visibility = _viewModel.IsOnline ? Visibility.Collapsed : Visibility.Visible;

    private void ApplyCurrentView()
    {
        RootContent.Content = _viewModel.CurrentView switch
        {
            LoginViewModel vm => new LoginView { DataContext = vm },
            CallCenterViewModel vm => new CallCenterView { DataContext = vm },
            _ => null,
        };
    }
}
