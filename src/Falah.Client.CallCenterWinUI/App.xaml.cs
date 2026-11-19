using Microsoft.UI.Xaml;

namespace Falah.Client.CallCenterWinUI;

// See Falah.Client.CashierWinUI's own App.xaml.cs for why the base class must stay fully
// global::-qualified - "Falah.Application" and "Microsoft.UI.Xaml.Application" collide on the bare
// name "Application" here too, for the same reason.
public partial class App : global::Microsoft.UI.Xaml.Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
        // Audit 2026-11-18: written down before the process ends (see CrashLog). Not marked handled:
        // the station recovers its in-flight work on the next start, which a half-finished screen would not.
        var crashDirectory = System.IO.Path.Combine(Services.FileRetainedRequestStore.DataRoot(), "logs");
        UnhandledException += (_, e) => Falah.Client.Diagnostics.CrashLog.Write(crashDirectory, "callcenter", e.Exception, "UI");
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Falah.Client.Diagnostics.CrashLog.Write(crashDirectory, "callcenter", e.ExceptionObject as Exception, "process");
        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Falah.Client.Diagnostics.CrashLog.Write(crashDirectory, "callcenter", e.Exception, "background task");
            e.SetObserved();
        };
        // Configured before the order desk is constructed: it recovers its pending work in its own
        // constructor, so the store has to exist by then.
#if SCREEN_LAYOUT_PROBE || FOOTER_LAYOUT_PROBE
        Services.RetainedRequestStorage.Current = null; // Native fixtures must never read the live station queue.
#else
        Services.RetainedRequestStorage.Current =
            new Services.FileRetainedRequestStore(Services.FileRetainedRequestStore.DirectoryFor("callcenter"));
#endif
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
#if SCREEN_LAYOUT_PROBE
        _window = new Window { Title = "Falah Call Center — native XAML verification" };
        global::Falah.Verification.NativeScreenLayoutProbe.Start(_window);
#else
        _window = new MainWindow();
        // Converter-chosen brushes follow the window root's theme, not the application's.
        if (_window.Content is FrameworkElement themeRoot) Services.ThemeResources.ObserveThemeRoot(themeRoot);
        _window.Activate();
#endif
    }
}
