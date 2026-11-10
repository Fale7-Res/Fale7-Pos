// Compiled only by the explicit ScreenLayoutProbe build. No authentication, disk queue or server I/O.
using Falah.Application.Shifts;
using Falah.Client.CashierWinUI.Services;
using Falah.Client.CashierWinUI.ViewModels;
using Falah.Client.CashierWinUI.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Falah.Verification;

internal static class NativeShiftLayoutProbe
{
    private sealed class NoNetwork : HttpMessageHandler
    {
        public int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls++;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable));
        }
    }

    public static async Task<List<object>> RunAsync(Window window, Grid host, string output)
    {
        var results = new List<object>();
        const string errorText = "لم تُحسم نتيجة العملية بعد انقطاع الاتصال. تحقّق من اتصال المحطة ثم أعد إرسال نفس العملية للتحقق من نتيجتها دون تسجيل حركة جديدة. تواصل مع المسؤول إذا استمر تعذر الاتصال بالخادم، واحتفظ بالمبلغ المعدود حتى تأكيد التسليم.";
        foreach (var role in new[] { "owner", "cashier" })
        foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
        foreach (var (width, height) in new[] { (1280, 640), (1093, 534), (960, 480) })
        foreach (var state in new[] { "open", "open-error", "count", "count-error", "approval", "approval-error" })
        {
            var key = $"Shift/{role}/{state}/{width}x{height}/{theme}";
            string? error = null;
            object? primaryBounds = null;
            double? actualRasterizationScale = null;
            var interactionsVerified = false;
            using var transport = new NoNetwork();
            using var http = new HttpClient(transport) { BaseAddress = new Uri("http://fixture.invalid") };
            var session = new AppSession(new ApiClient(http))
            { CurrentUser = new("fixture-owner", "كاشير اختبار", role, role == "owner" ? new[] { "all" } : new[] { "pos", "shifts_open_own", "shifts_blind_close" }) };
            var header = new HeaderViewModel(session);
            try
            {
                var vm = new ShiftsViewModel(session, header);
                if (state.StartsWith("open"))
                {
                    vm.SelectedShiftName = vm.ShiftNameOptions[1];
                    vm.MainTreasuryBalancePiastres = 0;
                    vm.OpeningCashText = "500";
                    vm.OpenShiftError = state.EndsWith("error") ? errorText : null;
                    vm.IsOpenShiftModalOpen = true;
                }
                else
                {
                    vm.ActiveShift = new CurrentShiftDto("fixture-shift", "وردية مسائية", "fixture-owner", 50000,
                        new DateTimeOffset(2026, 11, 7, 15, 0, 0, TimeSpan.Zero));
                    vm.CloseShiftActualCashText = "475";
                    vm.CloseShiftError = state.EndsWith("error") ? errorText : null;
                    if (state.StartsWith("approval"))
                    {
                        vm.CloseShiftPreview = new PrepareShiftCloseResult("fixture-shift", 50000, 47500, -2500, true);
                        vm.CloseShiftReason = "سبب توضيحي لاختبار عرض سبب الفارق النقدي ومراجعة الإغلاق قبل التسليم.";
                    }
                    vm.IsCloseShiftModalOpen = true;
                }
                host.RequestedTheme = theme;
                var view = new ShiftsView { DataContext = vm, Width = width, Height = height,
                    HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
                host.Children.Clear(); host.Children.Add(view);
                var scale = host.XamlRoot.RasterizationScale;
                actualRasterizationScale = scale;
                window.AppWindow.Resize(new Windows.Graphics.SizeInt32((int)Math.Ceiling(width * scale), (int)Math.Ceiling(height * scale) + 40));
                await Task.Delay(80); view.UpdateLayout();
                var id = state.StartsWith("open") ? "Shifts_ConfirmOpen" : state.StartsWith("count") ? "Shifts_PrepareClose" : "Shifts_ConfirmClose";
                var button = Descendants(view).OfType<Button>().Single(b => AutomationProperties.GetAutomationId(b) == id);
                var bounds = Bounds(button, view);
                primaryBounds = new { bounds.X, bounds.Y, bounds.Width, bounds.Height };
                var failures = new List<string>();
                var visibleText = Descendants(view).OfType<TextBlock>().Where(IsVisible).Select(t => t.Text).ToList();
                if (role == "cashier" && visibleText.Any(t => t.StartsWith("الرصيد المتاح بالخزنة الرئيسية")))
                    failures.Add("The cashier can see the main treasury balance.");
                if (state.StartsWith("count") && visibleText.Any(t => t.StartsWith("الكاش المتوقع بالدرج")))
                    failures.Add("Expected cash is visible before the blind count.");
                if (bounds.Height < 44 || bounds.Width < 44 || bounds.Left < -1 || bounds.Top < -1 || bounds.Right > width + 1 || bounds.Bottom > height + 1)
                    failures.Add("The primary action is outside the viewport or too small.");
                for (DependencyObject? ancestor = button; ancestor is not null && !ReferenceEquals(ancestor, view); ancestor = VisualTreeHelper.GetParent(ancestor))
                    if (ancestor is ScrollViewer scroll)
                    {
                        var clip = Bounds(scroll, view);
                        if (bounds.Top < clip.Top - 1 || bounds.Bottom > clip.Bottom + 1)
                            failures.Add("The primary action is clipped inside scrolling content.");
                    }
                if (width == 960 || (width == 1280 && state == "open-error"))
                    await NativeScreenLayoutProbe.CaptureAsync(view, output, $"shift-{role}-{state}-{width}-{theme}.png");
                // Exercise the actual bound buttons through their native automation peers.
                // Invalid input/PIN must be rejected locally, then cancel/recount must navigate correctly.
                if (button.Command is null) failures.Add("The primary button has no bound command.");
                if (state.StartsWith("open"))
                {
                    ((TextBox)Descendants(view).Single(e => AutomationProperties.GetAutomationId(e) == "Shifts_OpeningCash")).Text = "-1";
                    Invoke(button); await Task.Delay(40);
                    if (vm.OpenShiftError is null || vm.OpenShiftError == errorText || !vm.IsOpenShiftModalOpen)
                        failures.Add("Invalid opening cash was not rejected with the dialog still open.");
                    Invoke(Action(view, "Shifts_OpenCancel")); await Task.Delay(30);
                    if (vm.IsOpenShiftModalOpen) failures.Add("Cancel did not close the opening dialog.");
                }
                else
                {
                    if (state.StartsWith("count"))
                        ((TextBox)Descendants(view).Single(e => AutomationProperties.GetAutomationId(e) == "Shifts_CountedCash")).Text = "invalid";
                    Invoke(button); await Task.Delay(40);
                    if (vm.CloseShiftError is null || vm.CloseShiftError == errorText || !vm.IsCloseShiftModalOpen)
                        failures.Add("Invalid count or missing manager PIN was not rejected locally.");
                    if (state.StartsWith("approval"))
                    {
                        Invoke(Action(view, "Shifts_Recount")); await Task.Delay(30);
                        if (!vm.IsCloseShiftCounting) failures.Add("Recount did not return to the blind-count step.");
                    }
                    Invoke(Action(view, "Shifts_CloseCountCancel")); await Task.Delay(30);
                    if (vm.IsCloseShiftModalOpen) failures.Add("Cancel did not close the closing dialog.");
                }
                if (transport.Calls != 0) failures.Add("Invalid-input checks unexpectedly attempted an HTTP request.");
                interactionsVerified = failures.Count == 0;
                if (failures.Count > 0) error = string.Join(" ", failures);
            }
            catch (Exception ex) { error = ex.GetBaseException().ToString(); }
            finally { header.Stop(); host.Children.Clear(); }
            results.Add(new { key, role, width, height, theme = theme.ToString(), actualRasterizationScale, primaryBounds, interactionsVerified, error });
        }
        return results;
    }

    private static bool IsVisible(FrameworkElement element)
    {
        for (DependencyObject? current = element; current is not null; current = VisualTreeHelper.GetParent(current))
            if (current is UIElement ui && ui.Visibility != Visibility.Visible) return false;
        return element.ActualWidth > 0 && element.ActualHeight > 0;
    }

    private static Button Action(DependencyObject view, string id) =>
        Descendants(view).OfType<Button>().Single(b => AutomationProperties.GetAutomationId(b) == id);

    private static void Invoke(Button button)
    {
        if (new ButtonAutomationPeer(button).GetPattern(PatternInterface.Invoke) is not IInvokeProvider provider)
            throw new InvalidOperationException("The native button has no invoke provider.");
        provider.Invoke();
    }

    private static Rect Bounds(FrameworkElement element, UIElement relativeTo) =>
        element.TransformToVisual(relativeTo).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));

    private static IEnumerable<DependencyObject> Descendants(DependencyObject node)
    {
        yield return node;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
            foreach (var child in Descendants(VisualTreeHelper.GetChild(node, i))) yield return child;
    }
}
