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

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
