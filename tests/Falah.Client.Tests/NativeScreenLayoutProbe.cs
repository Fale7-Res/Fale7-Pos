// Test-only native XAML construction/measurement. No session, authentication or API writes.
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Falah.Verification;

internal static class NativeScreenLayoutProbe
{
    public static void Start(Window window)
    {
        var host = new Grid { Background = new SolidColorBrush(Microsoft.UI.Colors.White) };
        window.Content = host;
        var started = false;
        host.Loaded += async (_, _) =>
        {
            if (started) return;
            started = true;
            var assembly = global::Microsoft.UI.Xaml.Application.Current.GetType().Assembly;
            var output = Path.Combine(Environment.GetEnvironmentVariable("FALAH_SCREEN_PROBE_OUTPUT")
                ?? Path.Combine(AppContext.BaseDirectory, "Verification"), assembly.GetName().Name!);
            Directory.CreateDirectory(output);
            var results = new List<object>();
            try
            {
                var views = assembly.GetTypes().Where(t => t.Namespace?.EndsWith(".Views") == true &&
                    !t.Name.Contains("Placeholder") && !t.IsAbstract && typeof(UserControl).IsAssignableFrom(t));
                foreach (var (pixelsW, pixelsH, scale) in new[] {
                    (1920,1080,1d), (1920,1080,1.25), (1920,1080,1.5),
                    (1366,768,1d), (1366,768,1.25), (1366,768,1.5) })
                foreach (var type in views.OrderBy(t => t.Name))
                {
                    var width = pixelsW / scale;
                    var height = (pixelsH - 80) / scale - 64;
                    var key = $"{type.Name}/{pixelsW}x{pixelsH}/{scale:0.00}";
                    await File.AppendAllTextAsync(Path.Combine(output, "progress.log"), key + Environment.NewLine);
                    string? error = null;
                    int visualCount = 0;
                    try
                    {
                        var view = (UserControl)Activator.CreateInstance(type)!;
                        view.Width = width; view.Height = height;
                        view.HorizontalAlignment = HorizontalAlignment.Left;
                        view.VerticalAlignment = VerticalAlignment.Top;
                        host.Children.Clear(); host.Children.Add(view);
                        window.AppWindow.Resize(new Windows.Graphics.SizeInt32((int)(width + 40), (int)(height + 80)));
                        await Task.Delay(25);
                        view.UpdateLayout();
                        visualCount = CountVisuals(view);
                    }
                    catch (Exception ex) { error = ex.GetBaseException().ToString(); }
                    results.Add(new { key, width, height, visualCount, error });
                }
#if CASHIER_LAYOUT_PROBE
                results.AddRange(await NativeManagementLayoutProbe.RunAsync(window, host));
#endif
            }
            finally
            {
                await File.WriteAllTextAsync(Path.Combine(output, "results.json"),
                    JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
                window.Close();
            }
        };
        window.Activate();
    }

    private static int CountVisuals(DependencyObject element)
    {
        var count = VisualTreeHelper.GetChildrenCount(element);
        var total = count;
        for (var i = 0; i < count; i++) total += CountVisuals(VisualTreeHelper.GetChild(element, i));
        return total;
    }
}
