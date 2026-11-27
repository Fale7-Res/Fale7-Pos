using Microsoft.UI.Windowing;

namespace Falah.Client.Windowing;

/// <summary>How a station window looks on the restaurant's terminals (owner request 2026-11-26): the restaurant's
/// logo as its icon, and the whole screen - no Windows taskbar, no title bar. F11 leaves full screen for a maximized
/// window and comes back; the header's «خروج» button (and Alt+F4) closes the program through the same guard as the
/// title bar's X.</summary>
public static class StationWindow
{
    public static void Prepare(AppWindow window)
    {
        var icon = Path.Combine(AppContext.BaseDirectory, "Assets", "falah.ico");
        if (File.Exists(icon)) window.SetIcon(icon);
        // Maximized underneath, so leaving full screen (F11) lands on a window that still owns the screen.
        if (window.Presenter is OverlappedPresenter presenter) presenter.Maximize();
        window.SetPresenter(AppWindowPresenterKind.FullScreen);
    }

    /// <summary>Owner request 2026-11-27: «تصغير» puts the station in the taskbar without closing it - the shift,
    /// the order being typed and the session all stay - and clicking it there brings it back full screen.
    /// A full-screen presenter cannot be minimized, so the window goes overlapped first; <see cref="Restored"/>
    /// (wired to the window's Activated event) puts full screen back on its return.</summary>
    public static void MinimizeToTaskbar(AppWindow window)
    {
        _returnToFullScreen.Add(window.Id.Value);
        if (window.Presenter.Kind != AppWindowPresenterKind.Overlapped) window.SetPresenter(AppWindowPresenterKind.Overlapped);
        if (window.Presenter is OverlappedPresenter presenter) presenter.Minimize();
    }

    /// <summary>Call when the window is activated: a window minimized by «تصغير» goes back to full screen.</summary>
    public static void Restored(AppWindow window)
    {
        if (window.Presenter is OverlappedPresenter { State: OverlappedPresenterState.Minimized }) return;
        if (!_returnToFullScreen.Remove(window.Id.Value)) return;
        if (window.Presenter is OverlappedPresenter presenter) presenter.Maximize();
        window.SetPresenter(AppWindowPresenterKind.FullScreen);
    }

    private static readonly HashSet<ulong> _returnToFullScreen = new();

    public static void ToggleFullScreen(AppWindow window)
    {
        if (window.Presenter.Kind == AppWindowPresenterKind.FullScreen)
        {
            window.SetPresenter(AppWindowPresenterKind.Overlapped);
            if (window.Presenter is OverlappedPresenter presenter) presenter.Maximize();
        }
        else
        {
            window.SetPresenter(AppWindowPresenterKind.FullScreen);
        }
    }
}
