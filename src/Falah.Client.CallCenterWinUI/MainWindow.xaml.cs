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
        _viewModel = new MainViewModel();
        CloseNotice.DataContext = _viewModel;
        AppWindow.Closing += (_, args) => args.Cancel = !_viewModel.TryClose();
        Closed += (_, _) => { _viewModel.PropertyChanged -= OnViewModelPropertyChanged; _viewModel.Stop(); };

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
