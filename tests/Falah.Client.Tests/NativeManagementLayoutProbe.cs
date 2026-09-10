// Compiled only into the explicit Cashier ScreenLayoutProbe build, never a production binary.
// Uses the real views/view models with an isolated HTTP fixture. No live authentication or writes.
using System.Net;
using System.Net.Http.Json;
using Falah.Application.Employees;
using Falah.Application.Users;
using Falah.Client.CashierWinUI.Services;
using Falah.Client.CashierWinUI.ViewModels;
using Falah.Client.CashierWinUI.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Falah.Verification;

internal static class NativeManagementLayoutProbe
{
    private sealed class FixtureHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (request.Method != HttpMethod.Get) throw new InvalidOperationException("Probe must not submit writes.");
            object value = request.RequestUri!.AbsolutePath == "/api/users/all"
                ? new[] { new UserDetailsDto("probe-owner", "اختبار تخطيط مستقل", "owner", true, new[] { "all" }) }
                : new[] { new { key = "pos", category = "orders" }, new { key = "attendance_manage", category = "employees" } };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(value) });
        }
    }
    public static async Task<List<object>> RunAsync(Window window, Grid host)
    {
        var results = new List<object>();
        foreach (var (pixelsW, pixelsH, scale) in new[] {
            (1920,1080,1d), (1920,1080,1.25), (1920,1080,1.5),
            (1366,768,1d), (1366,768,1.25), (1366,768,1.5) })
        foreach (var state in new[] { "attendance", "attendance-error", "user-create", "user-create-error" })
        {
            var width = pixelsW / scale;
            var height = (pixelsH - 80) / scale - 64;
            var key = $"Populated/{state}/{pixelsW}x{pixelsH}/{scale:0.00}";
            string? error = null;
            Rect? cardBounds = null, scrollBounds = null, footerBounds = null;
            try
            {
                using var http = new HttpClient(new FixtureHandler()) { BaseAddress = new Uri("http://fixture.invalid") };
                var session = new AppSession(new ApiClient(http))
                { CurrentUser = new("probe-owner", "اختبار تخطيط مستقل", "owner", new[] { "all" }) };
                UserControl view;
                string prefix;
                const string longError = "لم تُحسم نتيجة العملية. أعد المحاولة بنفس الطلب للتحقق قبل المغادرة. يرجى التأكد من اتصال المحطة بالخادم ثم إعادة التحقق من النتيجة السابقة.";
                if (state.StartsWith("attendance"))
                {
                    var vm = new AttendanceViewModel(session);
                    vm.OpenCorrectionCommand.Execute(new AttendanceRecordDto("probe", "employee", "موظف للاختبار باسم عربي طويل للتحقق من التفاف النص",
                        new(2026, 9, 8), "08:00", "16:00", "صباحية", "Present", 0, 8, 0, 0, false, null, null, "biometric"));
                    if (state.EndsWith("error")) vm.CorrectionError = longError;
                    view = new AttendanceView { DataContext = vm }; prefix = "AttendanceCorrection";
                }
                else
                {
                    var vm = new UsersViewModel(session); await vm.LoadAsync(); vm.OpenCreateUserCommand.Execute(null);
                    vm.NewUserName = "اسم عربي طويل لاختبار تخطيط مستخدم جديد";
                    if (state.EndsWith("error")) vm.ActionError = longError;
                    view = new UsersView { DataContext = vm }; prefix = "CreateUser";
                }
                view.Width = width; view.Height = height;
                view.HorizontalAlignment = HorizontalAlignment.Left; view.VerticalAlignment = VerticalAlignment.Top;
                host.Children.Clear(); host.Children.Add(view);
                window.AppWindow.Resize(new Windows.Graphics.SizeInt32((int)(width + 40), (int)(height + 80)));
                await Task.Delay(35); view.UpdateLayout();
                var card = (FrameworkElement)view.FindName(prefix + "Card");
                var scroll = (FrameworkElement)view.FindName(prefix + "Scroll");
                var footer = (FrameworkElement)view.FindName(prefix + "Footer");
                cardBounds = Bounds(card, view); scrollBounds = Bounds(scroll, view); footerBounds = Bounds(footer, view);
                if (card.ActualWidth <= 0 || footer.ActualHeight < 44 || scroll.ActualHeight < 44)
                    throw new InvalidOperationException("Dialog or scrolling viewport collapsed.");
                if (cardBounds.Value.Left < -1 || cardBounds.Value.Right > width + 1 || cardBounds.Value.Bottom > height + 1)
                    throw new InvalidOperationException("Dialog extends outside the available viewport.");
                if (scrollBounds.Value.Bottom > footerBounds.Value.Top + 1 || footerBounds.Value.Bottom > cardBounds.Value.Bottom - 20)
                    throw new InvalidOperationException("Dialog footer overlaps the scroll area or extends outside its card.");
            }
            catch (Exception ex) { error = ex.GetBaseException().ToString(); }
            results.Add(new { key, width, height, cardBounds, scrollBounds, footerBounds, error });
        }
        return results;
    }
    private static Rect Bounds(FrameworkElement element, UIElement relativeTo) =>
        element.TransformToVisual(relativeTo).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
}
