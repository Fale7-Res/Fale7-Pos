using System.ComponentModel;
using Falah.Client.CashierWinUI.Services;
using Falah.Client.CashierWinUI.ViewModels;
using Falah.Client.CashierWinUI.Views;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace Falah.Client.CashierWinUI;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        CloseNotice.DataContext = _viewModel;
        RecoveredNotice.DataContext = _viewModel;
        AppWindow.Closing += (_, args) => args.Cancel = !_viewModel.TryClose();
        Closed += (_, _) =>
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            ThemeService.Instance.Changed -= OnThemeChanged;
            _viewModel.Stop();
        };

        HeaderHost.DataContext = _viewModel.Header;
        HeaderHost.LockRequested += (_, _) => _viewModel.LockCommand.Execute(null);
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        // Applied before the rest of the tree is populated: RootGrid.RequestedTheme is inherited
        // by every descendant, so whatever is realised next (login screen or the full shell) is
        // already correct rather than flashing Light and then repainting.
        RootGrid.RequestedTheme = ThemeService.ToElementTheme(ThemeService.Instance.Current);
        ThemeService.Instance.Changed += OnThemeChanged;

        ApplyIsLoggedIn();
        ApplyIsOnline();
        ApplyCurrentView();
        // Maximize can show the native window and initiate layout. Attach its content and
        // bindings first; do not expose a partially initialized root to activation/layout.
        MaximizeOnTargetDisplay();
    }

    /// <summary>Live theme switching, no restart: RequestedTheme on the root propagates to the
    /// whole subtree, and every `{ThemeResource}` binding in it re-resolves on its own. Raised off
    /// the UI thread is not expected (ThemeService.Set is only ever called from a command), but the
    /// dispatch keeps this safe even if that ever changes.</summary>
    private void OnThemeChanged(AppTheme theme) =>
        DispatcherQueue.TryEnqueue(() => RootGrid.RequestedTheme = ThemeService.ToElementTheme(theme));

    /// <summary>This is real dedicated POS hardware (a ~15" touch terminal), not a resizable desktop
    /// tool window - it should own the whole screen from launch. Maximizing rather than requesting a
    /// specific pixel size also sidesteps AppWindow.Resize's one DPI gotcha in WinUI 3: unlike XAML
    /// content lengths (already DIP-based and auto-scaled per monitor), AppWindow's own Win32-level
    /// APIs take physical pixels, so a hardcoded Resize() call clips at higher display scaling.</summary>
    private void MaximizeOnTargetDisplay()
    {
        if (AppWindow.Presenter is OverlappedPresenter presenter)
            presenter.Maximize();
    }

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

    /// <summary>WinUI/UWP's ContentControl has no WPF-style implicit "DataTemplate keyed by
    /// content's CLR type" resolution, so the matching this app needs (LoginViewModel -> LoginView,
    /// CashierViewModel -> CashierView) is done explicitly here instead of in XAML.</summary>
    private void ApplyCurrentView()
    {
        RootContent.Content = _viewModel.CurrentView switch
        {
            LoginViewModel vm => new LoginView { DataContext = vm },
            CashierViewModel vm => new CashierView { DataContext = vm },
            ShellViewModel vm => new ShellView { DataContext = vm },
            _ => null,
        };
    }
}
