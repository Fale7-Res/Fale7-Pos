using System.Net;
using System.Text;
using System.Text.Json;
using Falah.Application.Employees;
using Falah.Application.Delivery;
using Falah.Application.Inventory;
using Falah.Application.Purchasing;
using Falah.Client.CashierWinUI.Services;
using Falah.Client.CashierWinUI.ViewModels;

namespace Falah.Client.Tests;

public class ManagementWriteSafetyTests
{
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => send(request);
    }

    private static T Dto<T>(string json) => JsonSerializer.Deserialize<T>(json,
        new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
    private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private sealed record Flow(Func<Task> Submit, Action CorruptInputs, Action Close, Func<bool> IsOpen, Func<bool> CanEdit);

    private static async Task<Flow> CreateFlowAsync(string kind, AppSession session)
    {
        if (kind is "assign" or "outcome" or "settlement")
        {
            var vm = new DeliveryViewModel(session);
            var delivery = Dto<DeliveryDto>("""{"deliveryId":"delivery","orderNumber":"123","amountToCollectPiastres":1000}""");
            var driver = new DriverDto("driver", "سائق", true);
            if (kind == "assign")
            {
                vm.OpenAssignCommand.Execute(delivery); vm.AssignDriver = driver;
                return new(() => vm.ConfirmAssignCommand.ExecuteAsync(null), () => vm.AssignDriver = null,
                    () => vm.CloseAssignCommand.Execute(null), () => vm.IsAssignOpen, () => vm.CanEditAction);
            }
            if (kind == "outcome")
            {
                vm.OpenOutcomeCommand.Execute(delivery);
                return new(() => vm.ConfirmOutcomeCommand.ExecuteAsync(null), () => vm.OutcomeCollectedText = "invalid",
                    () => vm.CloseOutcomeCommand.Execute(null), () => vm.IsOutcomeOpen, () => vm.CanEditAction);
            }
            vm.AccountDriver = driver;
            vm.DriverAccount = Dto<DriverAccountDto>("""{"driverId":"driver","cashCustodyPiastres":1000}""");
            // The real account read mints the settlement key before the first submission.
            await vm.OpenDriverAccountCommand.ExecuteAsync(driver);
            vm.SettleActualCashText = "10";
            return new(() => vm.SettleDriverCommand.ExecuteAsync(null), () => vm.SettleActualCashText = "invalid",
                () => vm.CloseDriverAccountCommand.Execute(null), () => vm.IsDriverAccountOpen, () => vm.CanEditAction);
        }
        if (kind is "payroll-create" or "payroll-pay")
        {
            var vm = new PayrollViewModel(session);
            if (kind == "payroll-create")
            {
                vm.NewPeriod = "2026-09";
                return new(() => vm.CreateRunCommand.ExecuteAsync(null), () => vm.NewPeriod = "invalid",
                    () => vm.SelectRunCommand.Execute(null), () => !vm.CanEditAction, () => vm.CanEditAction);
            }
            vm.SelectedRun = Dto<PayrollRunDto>("""{"id":"run","period":"2026-09","status":"approved"}""");
            vm.OpenPayCommand.Execute(null); vm.PaymentAccount = vm.PaymentAccounts[0];
            return new(() => vm.PayRunCommand.ExecuteAsync(null), () => vm.PaymentAccount = vm.PaymentAccounts[1],
                () => vm.ClosePayCommand.Execute(null), () => vm.IsPayOpen, () => vm.CanEditAction);
        }
        const string itemJson = """{"id":"item","name":"صنف","unit":"kg","balanceMilliUnits":1000}""";
        // The stock screen now works from the overview row, which carries the item's value and last
        // movement alongside what the plain item DTO used to hold; purchasing still picks the item.
        var item = Dto<InventoryItemDto>(itemJson);
        var stockRow = Dto<InventoryOverviewRowDto>(itemJson);
        if (kind == "employee")
        {
            var vm = new EmployeesViewModel(session);
            vm.OpenCashOutCommand.Execute(new EmployeesViewModel.EmployeeRow {
                Employee = Dto<EmployeeDto>("""{"id":"employee","name":"موظف","active":true}""") });
            vm.CashOutAmountText = "10"; vm.CashOutReason = "اختبار";
            return new(() => vm.SubmitCashOutCommand.ExecuteAsync(null),
                () => { vm.CashOutAmountText = "invalid"; session.ShiftId = null; },
                () => vm.CloseCashOutCommand.Execute(null), () => vm.IsCashOutOpen, () => vm.CanEditAction);
        }
        if (kind is "count" or "waste")
        {
            var vm = new InventoryViewModel(session);
            if (kind == "count")
            {
                vm.OpenCountCommand.Execute(stockRow); vm.CountActualText = "2"; vm.CountReason = "اختبار";
                return new(() => vm.SubmitCountCommand.ExecuteAsync(null), () => vm.CountActualText = "invalid",
                    () => vm.CloseCountCommand.Execute(null), () => vm.IsCountOpen, () => vm.CanEditAction);
            }
            vm.OpenWasteCommand.Execute(stockRow); vm.WasteQuantityText = "1"; vm.WasteReason = "اختبار";
            return new(() => vm.SubmitWasteCommand.ExecuteAsync(null), () => vm.WasteQuantityText = "invalid",
                () => vm.CloseWasteCommand.Execute(null), () => vm.IsWasteOpen, () => vm.CanEditAction);
        }
        var purchasing = new PurchasingViewModel(session);
        if (kind == "supplier")
        {
            purchasing.OpenAddSupplierCommand.Execute(null); purchasing.SupplierName = "مورد";
            return new(() => purchasing.SubmitSupplierCommand.ExecuteAsync(null), () => purchasing.SupplierName = "",
                () => purchasing.CloseAddSupplierCommand.Execute(null), () => purchasing.IsSupplierOpen, () => purchasing.CanEditAction);
        }
        if (kind == "receive")
        {
            purchasing.OpenReceiveCommand.Execute(Dto<SupplierDto>("""{"id":"supplier","name":"مورد","active":true}"""));
            var line = purchasing.ReceiveLines[0];
            line.Item = item; line.QuantityText = "1"; line.UnitCostText = "10";
            purchasing.ReceivePaidText = "10";
            return new(() => purchasing.SubmitReceiveCommand.ExecuteAsync(null), () => line.QuantityText = "invalid",
                () => purchasing.CloseReceiveCommand.Execute(null), () => purchasing.IsReceiveOpen, () => purchasing.CanEditAction);
        }
        purchasing.OpenPayCommand.Execute(Dto<PurchaseDto>("""{"id":"purchase","remainingAmountPiastres":1000}"""));
        return new(() => purchasing.SubmitPayCommand.ExecuteAsync(null), () => purchasing.PayAmountText = "invalid",
            () => purchasing.ClosePayCommand.Execute(null), () => purchasing.IsPayOpen, () => purchasing.CanEditAction);
    }

    [Theory]
    [InlineData("employee")]
    [InlineData("count")]
    [InlineData("waste")]
    [InlineData("supplier")]
    [InlineData("receive")]
    [InlineData("pay")]
    [InlineData("assign")]
    [InlineData("outcome")]
    [InlineData("settlement")]
    [InlineData("payroll-create")]
    [InlineData("payroll-pay")]
    public async Task Lost_response_then_rejection_keeps_the_exact_write_and_blocks_abandonment(string kind)
    {
        var requests = new List<(string Path, string Body)>();
        var http = new HttpClient(new Handler(async r => {
            if (r.Method == HttpMethod.Get)
                return Json(r.RequestUri!.AbsolutePath.EndsWith("/account")
                    ? "{\"driverId\":\"driver\",\"cashCustodyPiastres\":1000}" : "[]");
            requests.Add((r.RequestUri!.AbsolutePath, await r.Content!.ReadAsStringAsync()));
            if (requests.Count == 1) throw new HttpRequestException("response lost after commit");
            if (requests.Count == 2) return Json("{\"error\":\"rejected\"}", HttpStatusCode.UnprocessableEntity);
            return Json("{\"wasReplayed\":true,\"balanceAfterPiastres\":1000,\"totalPiastres\":1000}");
        })) { BaseAddress = new Uri("http://fixture.invalid") };
        var session = new AppSession(new ApiClient(http)) {
            CurrentUser = new("actor", "اختبار", "manager", new[] { "all" }), ShiftId = "shift"
        };
        var flow = await CreateFlowAsync(kind, session);
        await flow.Submit();
        Assert.Single(requests);
        Assert.True(session.HasUnresolvedOperation);
        Assert.False(flow.CanEdit());
        flow.Close();
        Assert.True(flow.IsOpen());
        flow.CorruptInputs();
        await flow.Submit();
        Assert.True(session.HasUnresolvedOperation);
        Assert.False(flow.CanEdit());
        await flow.Submit();
        Assert.Equal(3, requests.Count);
        Assert.All(requests, request => Assert.Equal(requests[0], request));
        Assert.False(session.HasUnresolvedOperation);
        Assert.True(flow.CanEdit());
        if (kind == "settlement")
        {
            // Successful settlement stays on the refreshed account; it can now be closed.
            flow.Close();
        }
        Assert.False(flow.IsOpen());
    }

    [Fact]
    public async Task Lost_payroll_transition_reconciles_committed_status_without_advancing_a_second_step()
    {
        var writes = new List<string>();
        var reads = 0;
        var http = new HttpClient(new Handler(async r => {
            if (r.Method == HttpMethod.Post)
            {
                writes.Add(await r.Content!.ReadAsStringAsync());
                if (writes.Count == 1) throw new HttpRequestException("lost");
                return Json("{\"error\":\"already transitioned\"}", HttpStatusCode.UnprocessableEntity);
            }
            if (r.RequestUri!.AbsolutePath == "/api/payroll-runs/run")
            {
                reads++;
                return reads == 1 ? Json("{\"error\":\"offline\"}", HttpStatusCode.ServiceUnavailable)
                    : Json("{\"id\":\"run\",\"status\":\"calculated\"}");
            }
            return Json("[]");
        })) { BaseAddress = new Uri("http://fixture.invalid") };
        var session = new AppSession(new ApiClient(http)) { CurrentUser = new("actor", "اختبار", "manager", new[] { "all" }) };
        var vm = new PayrollViewModel(session) { SelectedRun = Dto<PayrollRunDto>("""{"id":"run","status":"draft"}""") };
        await vm.AdvanceRunCommand.ExecuteAsync(null);
        Assert.True(session.HasUnresolvedOperation);
        vm.SelectRunCommand.Execute(Dto<PayrollRunDto>("""{"id":"different","status":"reviewed"}"""));
        Assert.Equal("run", vm.SelectedRun!.Id);
        await vm.AdvanceRunCommand.ExecuteAsync(null);
        Assert.Equal(2, writes.Count);
        Assert.Equal(writes[0], writes[1]);
        Assert.False(session.HasUnresolvedOperation);
        Assert.Null(vm.ActionError);
    }
}
