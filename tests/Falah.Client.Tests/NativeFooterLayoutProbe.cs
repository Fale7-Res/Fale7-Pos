// Compiled only by -p:FooterLayoutProbe=true into an isolated test build.
// Uses the real CartPanelView, XAML resources and ViewModel. No live API is contacted.
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Falah.Client.CashierWinUI.Models;
using Falah.Client.CashierWinUI.Services;
using Falah.Client.CashierWinUI.ViewModels;
using Falah.Client.CashierWinUI.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Falah.Client.CashierWinUI.Verification;

internal static class NativeFooterLayoutProbe
{
    public static void Start(Window window)
    {
        var host = new Grid { Background = new SolidColorBrush(Microsoft.UI.Colors.White) };
        window.Content = host;
        var started = false;
        host.Loaded += async (_, _) => { if (started) return; started = true; await RunAsync(window, host); };
        window.Activate();
    }

    private static async Task RunAsync(Window window, Grid host)
    {
        var output = Environment.GetEnvironmentVariable("FALAH_LAYOUT_OUTPUT")
            ?? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "footer-layout"));
        Directory.CreateDirectory(output);
        var results = new List<object>();
        try
        {
            foreach (var (pixelsW, pixelsH, scale) in new[] {
                (1920,1080,1d), (1920,1080,1.25), (1920,1080,1.5),
                (1366,768,1d), (1366,768,1.25), (1366,768,1.5) })
            foreach (var state in new[] { "empty", "small", "large", "discount", "tax", "inclusive-tax", "split", "combined" })
            {
                var tax = state is "tax" or "inclusive-tax" or "combined";
                var session = new AppSession(new ApiClient(new HttpClient(new Fixtures(tax, state == "inclusive-tax")) {
                    BaseAddress = new Uri("http://layout-fixture.invalid") })) {
                    CurrentUser = new("layout-fixture", "اختبار التخطيط", "cashier", new[] { "all" }), ShiftId = "fixture" };
                var vm = session.GetCashier(new HeaderViewModel(session));
                await vm.LoadMenuAsync();
                var count = state == "empty" ? 0 : state is "large" or "combined" ? 60 : 2;
                for (var i = 0; i < count; i++) vm.Cart.Add(new CartLine {
                    ProductId = "fixture-" + i, ProductName = "وجبة مشويات مشكلة مع أرز وسلطات وإضافات خاصة", Quantity = state == "large" ? 99 : 1,
                    UnitPricePiastres = state == "large" ? 987654 : 12550 });
                if (state is "discount" or "combined") { vm.IsDiscountEnabled = true; vm.DiscountAmountText = "10.50"; }
                if (state == "combined") { vm.IsCompEnabled = true; vm.CompAmountText = "12.50"; }
                if (state == "split") { vm.IsSplitPaymentEnabled = true; vm.SplitAmount1Text = "100"; vm.SplitAmount2Text = "151"; }
                var width = Math.Clamp(pixelsW / scale * .30, 350, 420);
                // Reserve the taskbar/title bar (80 physical px) and the actual 64 DIP app header.
                var height = (pixelsH - 80) / scale - 64;
                var view = new CartPanelView { DataContext = vm, Width = width, Height = height,
                    HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
                host.Children.Clear(); host.Children.Add(view);
                window.AppWindow.Resize(new Windows.Graphics.SizeInt32((int)(width * host.XamlRoot.RasterizationScale + 60),
                    (int)(height * host.XamlRoot.RasterizationScale + 80)));
                await Task.Delay(100);
                view.UpdateLayout();
                var errors = new List<string>();
                Rect Bounds(string name)
                {
                    var element = (FrameworkElement)view.FindName(name);
                    return element.TransformToVisual(host).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
                }
                double previousBottom = 0;
                foreach (var key in new[] { "Subtotal", "Delivery", "Discount", "Comp", "Tax", "Total" })
                {
                    var label = (FrameworkElement)view.FindName("Footer" + key + "Label");
                    if (label.Visibility != Visibility.Visible) continue;
                    var l = Bounds("Footer" + key + "Label"); var a = Bounds("Footer" + key + "Amount");
                    if (a.Right > l.Left - 10) errors.Add(key + ": RTL columns overlap or lose gutter");
                    if (Math.Min(a.Top, l.Top) < previousBottom - .5) errors.Add(key + ": rows overlap");
                    previousBottom = Math.Max(a.Bottom, l.Bottom);
                }
                var total = Bounds("FooterTotalAmount"); var checkout = Bounds("FooterCheckoutButton");
                var footer = Bounds("OrderFooter"); var scroll = Bounds("CartScrollViewer");
                if (checkout.Top < total.Bottom + 11) errors.Add("CTA encroaches on total");
                if (footer.Bottom > view.ActualHeight + .5) errors.Add("Footer outside viewport");
                if (count > 0 && scroll.Height < 40) errors.Add("Cart viewport too short: " + scroll.Height);
                if (count > 0 && scroll.Bottom > footer.Top + .5) errors.Add("Cart overlaps pinned footer");
                var sv = (ScrollViewer)view.FindName("CartScrollViewer");
                sv.ChangeView(null, 10000, null, true); await Task.Delay(30);
                if (Math.Abs(Bounds("OrderFooter").Top - footer.Top) > .5) errors.Add("Footer moved with cart scroll");
                var file = $"{pixelsW}x{pixelsH}-{scale:0.00}-{state}";
                string? captureError = null;
                var bitmapInfo = "";
                if (state is "combined" or "large" or "empty")
                {
                    try
                    {
                    var bitmap = new RenderTargetBitmap();
                    await bitmap.RenderAsync(view, (int)Math.Ceiling(width * scale), (int)Math.Ceiling(height * scale));
                    var buffer = await bitmap.GetPixelsAsync(); var bytes = new byte[buffer.Length];
                    bitmapInfo = $"{bitmap.PixelWidth}x{bitmap.PixelHeight}, {buffer.Length} bytes";
                    if (bitmap.PixelWidth == 0 || bitmap.PixelHeight == 0 || buffer.Length == 0)
                        throw new InvalidOperationException("Native bitmap capture returned no pixels; visual inspection is unavailable.");
                    using (var reader = DataReader.FromBuffer(buffer)) reader.ReadBytes(bytes);
                    var path = Path.GetFullPath(Path.Combine(output, file + ".png"));
                    await File.WriteAllBytesAsync(path, Array.Empty<byte>());
                    var imageFile = await StorageFile.GetFileFromPathAsync(path);
                    using var stream = await imageFile.OpenAsync(FileAccessMode.ReadWrite);
                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                    encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
                        (uint)bitmap.PixelWidth, (uint)bitmap.PixelHeight, 96 * scale, 96 * scale, bytes);
                    await encoder.FlushAsync();
                    }
                    catch (Exception ex) { captureError = ex.Message; }
                }
                results.Add(new { file, width, height, rasterizationScale = host.XamlRoot.RasterizationScale, footer, checkout, cartHeight = scroll.Height, errors, captureError, bitmapInfo,
                    label = Bounds("FooterSubtotalLabel"), amount = Bounds("FooterSubtotalAmount") });
            }
            await File.WriteAllTextAsync(Path.Combine(output, "results.json"), JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex) { await File.WriteAllTextAsync(Path.Combine(output, "error.txt"), ex.ToString()); }
        finally { window.Close(); }
    }

    private sealed class Fixtures(bool tax, bool inclusive) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (request.Method != HttpMethod.Get) throw new InvalidOperationException("No writes permitted in layout verification");
            object data = request.RequestUri!.AbsolutePath switch {
                "/api/menu" => new { categories = Array.Empty<object>() },
                "/api/tax-rules" => tax ? new[] { new { id = "fixture", name = "ضريبة", rateBasisPoints = 1400, mode = inclusive ? "Inclusive" : "Exclusive", enabled = true } } : Array.Empty<object>(),
                _ => throw new InvalidOperationException("Unexpected fixture request") };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(data) });
        }
    }
}
