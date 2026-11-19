using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Falah.Application.Employees;
using Falah.Application.Inventory;
using Falah.Application.Menu;
using Falah.Client.CashierWinUI.Services;
using Falah.Client.CashierWinUI.ViewModels;
namespace Falah.Client.Tests;

// Shares the process-wide RetainedRequestStorage.Current with the other durability classes; running them in
// parallel let one class's store leak into another's assertions.
[Collection(RetainedRequestStorageCollection.Name)]
public class UiRecoveryAndRaceTests
{
    private static T Dto<T>(string json) => JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
    private static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => send(request);
    }
    private static AppSession Session(HttpMessageHandler handler) => new(new ApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://fixture.invalid") }))
        { CurrentUser = new("owner", "المالك", "owner", new[] { "all" }) };
    private static FieldInfo Field<T>(string name) => typeof(T).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!;

    [Fact]
    public async Task A_late_recipe_detail_does_not_replace_the_new_selection()
    {
        var first = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var second = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var vm = new RecipesViewModel(Session(new Handler(_ => Interlocked.Increment(ref calls) == 1 ? first.Task : second.Task)));
        var a = new RecipesViewModel.RecipeRow { Cost = Dto<RecipeCostSummaryDto>("{\"recipeId\":\"a\",\"missing\":[]}") };
        var b = new RecipesViewModel.RecipeRow { Cost = Dto<RecipeCostSummaryDto>("{\"recipeId\":\"b\",\"missing\":[]}") };
        Field<RecipesViewModel>("_selectedRecipe").SetValue(vm, a);
        var old = vm.LoadDetailAsync();
        Field<RecipesViewModel>("_selectedRecipe").SetValue(vm, b);
        var current = vm.LoadDetailAsync();
        second.SetResult(Json("{\"recipeId\":\"b\",\"lines\":[],\"missing\":[]}")); await current;
        first.SetResult(Json("{\"recipeId\":\"a\",\"lines\":[],\"missing\":[]}")); await old;
        Assert.Equal("b", vm.Detail!.RecipeId);
    }

    [Fact]
    public async Task Fractional_yield_is_refused_before_sending_instead_of_truncated()
    {
        var calls = 0;
        var vm = new RecipesViewModel(Session(new Handler(_ => { calls++; return Task.FromResult(Json("{}")); })));
        vm.Detail = Dto<RecipeCostDetailDto>("{\"recipeId\":\"recipe\",\"productId\":\"product\",\"name\":\"وصفة\",\"lines\":[],\"missing\":[]}");
        vm.EditorLines.Add(new() { InventoryItemId = "item", ItemName = "خامة", Unit = "kg", QuantityText = "1", KindIndex = 1, YieldRatioText = "2.5" });
        await vm.SaveAsync();
        Assert.Equal(0, calls);
        Assert.Contains("دون كسور", vm.SaveError);
    }

    private static ProductDetailsDto Product(string id) => Dto<ProductDetailsDto>(JsonSerializer.Serialize(new { id, name = id, categoryId = "category", variants = Array.Empty<object>() }));
    [Fact]
    public async Task A_file_picker_completion_for_an_old_product_does_not_upload_or_retarget_the_editor()
    {
        var writes = 0;
        var session = Session(new Handler(r => { if (r.Method != HttpMethod.Get) writes++; return Task.FromResult(Json(r.Method == HttpMethod.Get ? "[]" : "{\"version\":\"v1\"}")); }));
        var vm = new MenuEditorViewModel(session) { IsReady = true };
        vm.SelectRowCommand.Execute(new MenuEditorViewModel.CatalogRow("a", "", Product("a")));
        var picked = new TaskCompletionSource<(string ContentType, byte[] Bytes)?>(TaskCreationOptions.RunContinuationsAsynchronously);
        vm.PickImageAsync = () => picked.Task;
        var upload = vm.ChooseImageCommand.ExecuteAsync(null);
        vm.SelectRowCommand.Execute(new MenuEditorViewModel.CatalogRow("b", "", Product("b")));
        picked.SetResult(("image/png", new byte[] { 1 })); await upload;
        Assert.Equal(0, writes);
        Assert.Equal("b", ((ProductDetailsDto)Field<MenuEditorViewModel>("_editing").GetValue(vm)!).Id);
    }

    [Fact]
    public async Task An_upload_completion_for_an_old_product_does_not_retarget_the_editor()
    {
        var response = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var vm = new MenuEditorViewModel(Session(new Handler(r => r.Method == HttpMethod.Get ? Task.FromResult(Json("[]")) : response.Task))) { IsReady = true };
        vm.SelectRowCommand.Execute(new MenuEditorViewModel.CatalogRow("a", "", Product("a")));
        vm.PickImageAsync = () => Task.FromResult<(string, byte[])? >(("image/png", new byte[] { 1 }));
        var upload = vm.ChooseImageCommand.ExecuteAsync(null);
        vm.SelectRowCommand.Execute(new MenuEditorViewModel.CatalogRow("b", "", Product("b")));
        response.SetResult(Json("{\"version\":\"v1\"}")); await upload;
        Assert.Equal("b", ((ProductDetailsDto)Field<MenuEditorViewModel>("_editing").GetValue(vm)!).Id);
    }

    [Fact]
    public void Resetting_a_completed_order_clears_its_pickup_time()
    {
        var vm = Session(new Handler(_ => Task.FromResult(Json("[]")))).GetCashier(new());
        vm.PickupDate = DateTimeOffset.UtcNow.AddDays(2); vm.PickupTime = TimeSpan.FromHours(18);
        typeof(CashierViewModel).GetMethod("ResetDelivery", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(vm, null);
        Assert.Null(vm.PickupDate); Assert.Null(vm.PickupTime);
        Assert.Null(vm.RequestedPickupAtUtc);
    }

    [Theory]
    [InlineData("Draft", "calculated")]
    [InlineData("Calculated", "reviewed")]
    [InlineData("Reviewed", "approved")]
    public void Real_server_payroll_statuses_offer_the_next_action(string status, string expected)
    {
        var vm = new PayrollViewModel(Session(new Handler(_ => Task.FromResult(Json("[]")))));
        vm.SelectedRun = Dto<PayrollRunDto>(JsonSerializer.Serialize(new { id = "run", status, period = "2026-11" }));
        Assert.Equal(expected, vm.NextStatus);
        Assert.True(vm.CanAdvance);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Recovered_payroll_writes_are_retryable_without_loading_a_selection_or_opening_a_dialog(bool payment)
    {
        var dir = Path.Combine(Path.GetTempPath(), "falah-payroll-recovery-" + Guid.NewGuid().ToString("N"));
        var previous = RetainedRequestStorage.Current;
        try
        {
            var store = new FileRetainedRequestStore(dir);
            object request = payment ? new PayPayrollRequest("original-run", "tre-main", "owner", null, null)
                : new TransitionPayrollRequest("original-run", "reviewed", "owner", null, null);
            store.Save(payment ? "payroll.pay" : "payroll.transition", request);
            RetainedRequestStorage.Current = store;
            string? sent = null;
            var vm = new PayrollViewModel(Session(new Handler(async r => {
                if (r.Method == HttpMethod.Get) return Json("[]");
                sent = await r.Content!.ReadAsStringAsync();
                return Json(payment ? "{\"treasuryMovementId\":\"paid\",\"totalPaidPiastres\":1000,\"settlementCount\":1,\"wasReplayed\":true}" : "{\"id\":\"original-run\",\"status\":\"Reviewed\"}");
            })));
            Assert.True(vm.HasPendingRequest);
            Assert.Null(vm.SelectedRun); Assert.False(vm.IsPayOpen);
            await vm.RetryPendingOperationCommand.ExecuteAsync(null);
            Assert.NotNull(sent);
            Assert.Contains("original-run", sent);
            Assert.False(vm.HasPendingRequest);
            Assert.Null(store.Load(payment ? "payroll.pay" : "payroll.transition"));
        }
        finally { RetainedRequestStorage.Current = previous; if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }


    [Fact]
    public async Task Activating_an_inactive_user_opens_the_pin_form_and_sends_one_atomic_activation()
    {
        Falah.Application.Users.ChangePinRequest? sent = null;
        var vm = new UsersViewModel(Session(new Handler(async r => {
            if (r.Method == HttpMethod.Get) return Json("[]");
            sent = await System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<Falah.Application.Users.ChangePinRequest>(r.Content!);
            return Json("{}");
        })));
        vm.SelectedUser = new("inactive", "عامل", "cashier", false, new[] { "pos" });
        await vm.ToggleActiveCommand.ExecuteAsync(null);
        Assert.True(vm.IsPinChangeOpen); Assert.Null(sent);
        Assert.Contains("تفعيل", vm.PinChangeTitle);
        vm.NewPin = vm.ConfirmPin = "7351";
        await vm.ChangePinCommand.ExecuteAsync(null);
        Assert.NotNull(sent); Assert.True(sent.Reactivate); Assert.Equal("inactive", sent.UserId);
        Assert.False(vm.IsPinChangeOpen);
    }


    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Settled_cash_on_delivery_has_a_reachable_refund_action(bool collected)
    {
        var vm = Session(new Handler(_ => Task.FromResult(Json("[]")))).GetCashier(new());
        vm.OrderLookupResult = Dto<Falah.Application.Orders.OrderDetailsDto>(JsonSerializer.Serialize(new { id = "delivery-order", orderType = "delivery", paymentMethod = "cash", items = Array.Empty<object>(), isPaid = false, totalPiastres = 1000, status = "completed", customerFundsCollected = collected }));
        Assert.Equal(collected, vm.CanRefundOrder);
    }

    [Fact]
    public async Task Printer_status_lists_every_domain_route_including_both_delivery_routes()
    {
        var vm = new PrintingViewModel(Session(new Handler(r => Task.FromResult(Json(r.RequestUri!.AbsolutePath.Contains("overview") ? "{\"routes\":[],\"needsAttention\":[]}" : "[]")))));
        await vm.LoadAsync();
        Assert.Null(vm.LoadError);
        Assert.Equal(Enum.GetNames<Falah.Domain.Printing.PrintRoute>().Order(), vm.Queues.Select(q => q.Route).Order());
        Assert.Equal(6, vm.Queues.Count);
    }


    [Theory]
    [InlineData("IsRefundOpen")]
    [InlineData("IsDeliveryEditorOpen")]
    [InlineData("IsAddExpenseOpen")]
    [InlineData("IsSafeDropOpen")]
    [InlineData("IsCashMovementOpen")]
    [InlineData("IsCashDrawerOpen")]
    [InlineData("IsCashCountOpen")]
    [InlineData("IsOrderLookupOpen")]
    [InlineData("IsPickupEditorOpen")]
    [InlineData("IsCloseShiftOpen")]
    [InlineData("IsClearCartOpen")]
    [InlineData("IsCheckoutOpen")]
    public void Any_workspace_dialog_blocks_function_key_actions(string modal)
    {
        var vm = Session(new Handler(_ => Task.FromResult(Json("[]")))).GetCashier(new());
        Assert.True(vm.CanUseWorkspaceShortcuts);
        typeof(CashierViewModel).GetProperty(modal)!.SetValue(vm, true);
        Assert.True(vm.IsAnyModalOpen); Assert.False(vm.CanUseWorkspaceShortcuts);
        typeof(CashierViewModel).GetProperty(modal)!.SetValue(vm, false);
        Assert.True(vm.CanUseWorkspaceShortcuts);
    }


    [Fact]
    public void Entirely_unpriced_loss_is_shown_as_unknown_not_zero()
    {
        var vm = new WasteViewModel(Session(new Handler(_ => Task.FromResult(Json("[]")))))
        { Summary = new LossSummaryDto(0, 1, new Dictionary<string, long> { ["Waste"] = 0 }) { UnknownValueMovementCount = 1, UnknownValueByType = new Dictionary<string, int> { ["Waste"] = 1 } } };
        Assert.Equal("غير معلوم", vm.TotalValueDisplay); Assert.True(vm.HasUnknownValue);
        Assert.NotEmpty(vm.UnknownValueNote);
    }

    [Fact]
    public async Task Waste_shows_a_known_floor_and_requests_only_selected_days_loss_movements()
    {
        string? movementQuery = null;
        var vm = new WasteViewModel(Session(new Handler(r => {
            if (r.RequestUri!.AbsolutePath.EndsWith("loss-summary")) return Task.FromResult(Json("{\"totalLossValuePiastres\":500,\"movementCount\":2,\"valueByType\":{\"Waste\":500},\"unknownValueMovementCount\":1,\"unknownValueByType\":{\"Waste\":1}}"));
            movementQuery = r.RequestUri.Query; return Task.FromResult(Json("[]"));
        }))) { BusinessDate = new DateOnly(2026, 11, 3) };
        await vm.LoadAsync();
        Assert.Null(vm.LoadError); Assert.StartsWith("من ", vm.TotalValueDisplay);
        Assert.StartsWith("من ", Assert.Single(vm.ByType).ValueDisplay);
        Assert.Contains("lossesOnly=true", movementQuery);
        var (start, end) = Falah.Domain.Common.BusinessClock.UtcRangeForBusinessDate(vm.BusinessDate);
        var decoded = Uri.UnescapeDataString(movementQuery!);
        Assert.Contains(start.ToString("O"), decoded); Assert.Contains(end.AddTicks(-1).ToString("O"), decoded);
    }


    [Fact]
    public async Task Count_submission_carries_the_physical_total_not_only_a_stale_client_delta()
    {
        RecordCountAdjustmentRequest? sent = null;
        var vm = new InventoryViewModel(Session(new Handler(async r => {
            if (r.Method == HttpMethod.Get) return Json(r.RequestUri!.AbsolutePath.Contains("overview") ? "{\"items\":[]}" : "[]");
            sent = await System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<RecordCountAdjustmentRequest>(r.Content!);
            return Json("{\"movementId\":\"count\",\"inventoryItemId\":\"item\"}");
        })));
        vm.OpenCountCommand.Execute(Dto<InventoryOverviewRowDto>("{\"id\":\"item\",\"name\":\"خامة\",\"unit\":\"kg\",\"balanceMilliUnits\":10000,\"active\":true}"));
        vm.CountActualText = "8"; vm.CountReason = "جرد";
        await vm.SubmitCountCommand.ExecuteAsync(null);
        Assert.NotNull(sent); Assert.Equal(8000, sent.CountedTotalMilliUnits);
        Assert.Equal(2000, sent.QuantityMilliUnits);
    }

}
