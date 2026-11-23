using Microsoft.UI.Xaml;

namespace Falah.Client.CashierWinUI;

// "Falah.Application" (the referenced project) and "Microsoft.UI.Xaml.Application" collide on the
// bare name "Application": this file's own namespace is nested under "Falah" too, and C# resolves
// enclosing-namespace members before ANY `using` directive - even a `using Application = ...`
// alias loses to the sibling Falah.Application NAMESPACE (CS0118). Only a global::-qualified name
// bypasses that enclosing-scope lookup entirely, so the base class below must stay fully qualified.
public partial class App : global::Microsoft.UI.Xaml.Application
{
    private Window? _window;

    /// <summary>The app's own window. Needed by the Win32 file dialogs a file picker sits on: an
    /// unpackaged WinUI app has no implicit window for them to parent to, so the caller has to hand
    /// one over or the dialog appears behind the app - or not at all.</summary>
    public static Window? MainAppWindow { get; private set; }

    public App()
    {
        InitializeComponent();
        // Audit 2026-11-18: written down before the process ends (see CrashLog). Not marked handled:
        // the station recovers its in-flight work on the next start, which a half-finished screen would not.
        var crashDirectory = System.IO.Path.Combine(Services.FileRetainedRequestStore.DataRoot(), "logs");
        // Runtime logging audit 2026-11-23: the station log beside the crash log (FALAH_LOG_DIR overrides),
        // 30 days kept, and a start line naming the build and the machine.
        Falah.Client.Diagnostics.ClientLog.Configure(Falah.Client.Diagnostics.ClientLog.ResolveDirectory(crashDirectory), "cashier");
        Falah.Client.Diagnostics.ClientLog.DeleteOldLogs(retentionDays: 30);
        Falah.Client.Diagnostics.ClientLog.Write("INFO", "cashier station started");
        crashDirectory = Falah.Client.Diagnostics.ClientLog.ResolveDirectory(crashDirectory)!;
        UnhandledException += (_, e) => Falah.Client.Diagnostics.CrashLog.Write(crashDirectory, "cashier", e.Exception, "UI");
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Falah.Client.Diagnostics.CrashLog.Write(crashDirectory, "cashier", e.ExceptionObject as Exception, "process");
        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Falah.Client.Diagnostics.CrashLog.Write(crashDirectory, "cashier", e.Exception, "background task");
            e.SetObserved();
        };
        // Configured before any view model is constructed: a retained request restores itself in its
        // own field initializer, so the store has to exist by then. Left unset in tests, where the
        // class behaves exactly as it did before persistence existed.
#if SCREEN_LAYOUT_PROBE || FOOTER_LAYOUT_PROBE
        Services.RetainedRequestStorage.Current = null; // Native fixtures must never read the live station queue.
#else
        Services.RetainedRequestStorage.Current =
            new Services.FileRetainedRequestStore(Services.FileRetainedRequestStore.DirectoryFor("cashier"));
        Services.ReportFiles.Current = new Platform.ReportFileService();   // the report export's save/print (Windows dialogs)
#endif
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
#if FOOTER_LAYOUT_PROBE
        _window = new Window { Title = "Native footer layout verification — isolated fixtures" };
        Verification.NativeFooterLayoutProbe.Start(_window);
#elif SCREEN_LAYOUT_PROBE
        _window = new Window { Title = "Falah — native XAML verification" };
        MainAppWindow = _window;
        global::Falah.Verification.NativeScreenLayoutProbe.Start(_window);
#else
        _window = new MainWindow();
        MainAppWindow = _window;
        if (_window.Content is FrameworkElement themeRoot) Services.ThemeResources.ObserveThemeRoot(themeRoot);
        _window.Activate();
#endif
    }
}
