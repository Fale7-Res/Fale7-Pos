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
        // Configured before any view model is constructed: a retained request restores itself in its
        // own field initializer, so the store has to exist by then. Left unset in tests, where the
        // class behaves exactly as it did before persistence existed.
        Services.RetainedRequestStorage.Current =
            new Services.FileRetainedRequestStore(Services.FileRetainedRequestStore.DirectoryFor("cashier"));
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
#if FOOTER_LAYOUT_PROBE
        _window = new Window { Title = "Native footer layout verification — isolated fixtures" };
        Verification.NativeFooterLayoutProbe.Start(_window);
#elif SCREEN_LAYOUT_PROBE
        _window = new Window { Title = "Falah — native XAML verification" };
        global::Falah.Verification.NativeScreenLayoutProbe.Start(_window);
#else
        _window = new MainWindow();
        MainAppWindow = _window;
        _window.Activate();
#endif
    }
}
