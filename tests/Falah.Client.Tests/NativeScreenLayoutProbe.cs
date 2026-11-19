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
#if CASHIER_LAYOUT_PROBE
                if (Environment.GetEnvironmentVariable("FALAH_SHIFT_PROBE_ONLY") == "1")
                {
                    global::Falah.Client.CashierWinUI.Services.ThemeResources.ObserveThemeRoot(host);
                    results.AddRange(await NativeShiftLayoutProbe.RunAsync(window, host, output));
                    return;
                }
#endif
#if CASHIER_LAYOUT_PROBE
                try
                {
                    // Reflection keeps the probe compatible with the pre-fix implementation too.
                    typeof(global::Falah.Client.CashierWinUI.Services.ThemeResources).GetMethod("ObserveThemeRoot")?.Invoke(null, new object[] { host });
                    host.RequestedTheme = ElementTheme.Light; await Task.Delay(60);
                    var proxy = (SolidColorBrush)global::Falah.Client.CashierWinUI.Services.ThemeResources.Get("CardBackgroundBrush")!;
                    var light = proxy.Color.ToString();
                    host.RequestedTheme = ElementTheme.Dark; await Task.Delay(60);
                    var dark = proxy.Color.ToString();
                    var freshDark = ((SolidColorBrush)global::Falah.Client.CashierWinUI.Services.ThemeResources.Get("CardBackgroundBrush")!).Color.ToString();
                    var requested = host.RequestedTheme.ToString(); var actual = host.ActualTheme.ToString();
                    var palette = DescribeResources(global::Microsoft.UI.Xaml.Application.Current.Resources).ToArray();
                    results.Add(new { key = "LiveTheme/retained-converter-brush", light, dark, freshDark, requested, actual, palette,
                        error = light == dark ? "The retained converter brush did not follow the live theme change." : null });
                    host.RequestedTheme = ElementTheme.Light; await Task.Delay(30);
                }
                catch (Exception ex) { results.Add(new { key = "LiveTheme/retained-converter-brush", error = ex.GetBaseException().ToString() }); }
#endif
#if CASHIER_LAYOUT_PROBE
                results.AddRange(await CheckExpenseThemesAsync(window, host, output));
#else
                results.Add(await CheckCartSubscriptionAsync(host));
#endif
                if (Environment.GetEnvironmentVariable("FALAH_SCREEN_PROBE_QUICK") == "1") return;
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

    private static IEnumerable<string> DescribeResources(ResourceDictionary r)
    {
        foreach (var key in r.ThemeDictionaries.Keys)
        {
            var d = (ResourceDictionary)r.ThemeDictionaries[key];
            yield return $"theme={key},keys={d.Keys.Count},card={d.TryGetValue("CardBackgroundBrush",out var b)},brush={(b as SolidColorBrush)?.Color},color={d.TryGetValue("CardBackgroundColor",out var c)}:{c}";
        }
        foreach(var m in r.MergedDictionaries) foreach(var s in DescribeResources(m)) yield return s;
    }

    private static int CountVisuals(DependencyObject element)
    {
        var count = VisualTreeHelper.GetChildrenCount(element);
        var total = count;
        for (var i = 0; i < count; i++) total += CountVisuals(VisualTreeHelper.GetChild(element, i));
        return total;
    }

    private sealed class NoNetworkFixture : System.Net.Http.HttpMessageHandler
    {
        protected override Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, CancellationToken ct) =>
            throw new InvalidOperationException("Native fixture must not access any server.");
    }
#if CASHIER_LAYOUT_PROBE
    private static async Task<List<object>> CheckExpenseThemesAsync(Window window, Grid host, string output)
    {
        var results = new List<object>();
        using var http = new System.Net.Http.HttpClient(new NoNetworkFixture()) { BaseAddress = new Uri("http://fixture.invalid") };
        var session = new global::Falah.Client.CashierWinUI.Services.AppSession(new global::Falah.Client.CashierWinUI.Services.ApiClient(http))
        { CurrentUser = new("fixture-owner", "اختبار مستقل", "owner", new[] { "all" }), ShiftId = "fixture-shift" };
        var header = new global::Falah.Client.CashierWinUI.ViewModels.HeaderViewModel(session);
        try
        {
            var vm = session.GetCashier(header); vm.IsInitializing = false; vm.IsAddExpenseOpen = true;
            var view = new global::Falah.Client.CashierWinUI.Views.CashierView { DataContext = vm };
            host.Children.Clear(); host.Children.Add(view);
            window.AppWindow.Resize(new Windows.Graphics.SizeInt32(1600, 1000)); await Task.Delay(80);
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            {
                string? error = null; string? background = null, foreground = null; double? ratio = null;
                try
                {
                    host.RequestedTheme = theme; await Task.Delay(250); view.UpdateLayout();
                    await CaptureAsync(view, output, $"cashier-expense-{theme}.png");
                    var button = FindButton(view, "نثريات أخرى") ?? throw new InvalidOperationException($"Expense category button missing; shift={vm.IsShiftOpen}, modal={vm.IsAddExpenseOpen}, visuals={CountVisuals(view)}, size={view.ActualWidth}x{view.ActualHeight}, buttons={string.Join(" | ", DescribeButtons(view).Take(30))}.");
                    var bg = button.Background as SolidColorBrush ?? throw new InvalidOperationException("Expense background is not a solid brush.");
                    var fg = button.Foreground as SolidColorBrush ?? throw new InvalidOperationException("Expense text is not a solid brush.");
                    background = bg.Color.ToString(); foreground = fg.Color.ToString();
                    static double L(Windows.UI.Color c)
                    { double C(byte b) { var x = b / 255d; return x <= .04045 ? x / 12.92 : Math.Pow((x + .055) / 1.055, 2.4); } return .2126*C(c.R)+.7152*C(c.G)+.0722*C(c.B); }
                    var one = L(bg.Color); var two = L(fg.Color); ratio = (Math.Max(one, two) + .05)/(Math.Min(one, two) + .05);
                    if (ratio < 4.5) error = "Expense category text contrast is below4.5:1.";
                }
                catch (Exception ex) { error = ex.GetBaseException().ToString(); }
                results.Add(new { key = $"Populated/expense-theme/{theme}", background, foreground, ratio, error });
            }
            host.Children.Clear(); host.RequestedTheme = ElementTheme.Light;
        }
        finally { header.Stop(); }
        return results;
    }
    private static IEnumerable<string> DescribeButtons(DependencyObject node)
    {
        if (node is Button b) yield return $"{b.Content?.GetType().Name}:{b.Content}";
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
            foreach (var text in DescribeButtons(VisualTreeHelper.GetChild(node, i))) yield return text;
    }
    private static Button? FindButton(DependencyObject node, string text)
    {
        if (node is Button b && b.Content as string == text) return b;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
            if (FindButton(VisualTreeHelper.GetChild(node, i), text) is { } found) return found;
        return null;
    }
    internal static async Task CaptureAsync(FrameworkElement element, string output, string name)
    {
        var bitmap = new Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap(); await bitmap.RenderAsync(element);
        var buffer = await bitmap.GetPixelsAsync(); using var reader = Windows.Storage.Streams.DataReader.FromBuffer(buffer);
        var bytes = new byte[buffer.Length]; reader.ReadBytes(bytes);
        var folder = await Windows.Storage.StorageFolder.GetFolderFromPathAsync(Path.GetFullPath(output));
        var file = await folder.CreateFileAsync(name, Windows.Storage.CreationCollisionOption.ReplaceExisting);
        using var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);
        var encoder = await Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId, stream);
        encoder.SetPixelData(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied,
            (uint)bitmap.PixelWidth, (uint)bitmap.PixelHeight, 96, 96, bytes); await encoder.FlushAsync();
    }
#else
    private static async Task<object> CheckCartSubscriptionAsync(Grid host)
    {
        string? error = null;
        using var http = new System.Net.Http.HttpClient(new NoNetworkFixture()) { BaseAddress = new Uri("http://fixture.invalid") };
        var session = new global::Falah.Client.CallCenterWinUI.Services.AppSession(new global::Falah.Client.CallCenterWinUI.Services.ApiClient(http));
        var header = new global::Falah.Client.CallCenterWinUI.ViewModels.HeaderViewModel(session);
        try
        {
            var a = new global::Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel(session, header);
            var b = new global::Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel(session, header);
            var panel = new global::Falah.Client.CallCenterWinUI.Views.CallCenterOrderPanelView { DataContext = a };
            static int Count(object collection, object target)
            {
                var field = collection.GetType().GetField("CollectionChanged", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                return (field?.GetValue(collection) as Delegate)?.GetInvocationList().Count(d => ReferenceEquals(d.Target, target)) ?? 0;
            }
            host.Children.Clear(); host.Children.Add(panel); await Task.Delay(60);
            if (Count(a.Cart, panel) != 1) throw new InvalidOperationException("Loaded panel must subscribe exactly once.");
            panel.DataContext = b; await Task.Delay(60);
            if (Count(a.Cart, panel) != 0 || Count(b.Cart, panel) != 1) throw new InvalidOperationException("Old cart still retains the panel after context change.");
            host.Children.Clear(); await Task.Delay(60);
            if (Count(b.Cart, panel) != 0) throw new InvalidOperationException("Unloaded cart still retains the panel.");
        }
        catch (Exception ex) { error = ex.GetBaseException().ToString(); }
        finally { header.Stop(); }
        return new { key = "Lifecycle/cart-subscription", error };
    }
#endif
}
