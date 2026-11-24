namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Falah.Application.BusinessDay;
using Falah.Application.Customers;
using Falah.Application.Orders;
using Falah.Application.Shifts;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// Defensive QA audit 2026-11-23 (docs/FINAL_DEFENSIVE_QA_REPORT.md), fixes approved by the owner. Each
/// test is one finding: a request that did not come from a screen is refused with an Arabic 422 instead
/// of being saved wrongly, silently replaying another operation, or failing with a 500.
/// </summary>
public class InputHardeningTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    private FalahApiFactory _f = default!;
    private HttpClient _c = default!;
    private string _manager = default!, _cashier = default!, _product = default!, _shift = default!, _day = default!;

    public async Task InitializeAsync()
    {
        _f = new FalahApiFactory();
        _c = _f.CreateAuthenticatedClient();
        _manager = await _f.SeedUserAsync("مدير", "manager", "all");
        _cashier = await _f.SeedUserAsync("كاشير", "cashier", "pos", "shifts_open_own", "shifts_blind_close", "orders_refund", "cash_count", "expenses_add_drawer");
        _f.StationUserId = _manager;
        await _f.FundMainAccountAsync();
        _product = await _f.SeedProductAsync(5000, "ساندوتش");
        _day = (await (await _c.PostAsJsonAsync("/api/business-day/open", new OpenBusinessDayRequest($"bd-{Guid.NewGuid():N}", _manager)))
            .Content.ReadFromJsonAsync<OpenBusinessDayResult>())!.BusinessDayId;
        _shift = (await (await _c.PostAsJsonAsync("/api/shifts/open", new OpenShiftRequest($"op-{Guid.NewGuid():N}", "صباحية", _cashier, 50000)))
            .Content.ReadFromJsonAsync<OpenShiftResult>())!.ShiftId;
    }

    public Task DisposeAsync() { _f.Dispose(); return Task.CompletedTask; }

    private Task<HttpResponseMessage> Post(string url, string json) =>
        _c.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));

    private JsonObject Sale(string requestId, Action<JsonObject>? change = null, int qty = 1)
    {
        var node = JsonSerializer.SerializeToNode(new CheckoutOrderRequest(requestId, _cashier, _shift, "takeaway", "cash",
            new[] { new CheckoutItemRequest(_product, null, qty, null, null) }, 0, 0, 0, null, null, null, 5000 * qty, null, null,
            null, null, null, null, null, null), Web)!.AsObject();
        change?.Invoke(node);
        return node;
    }

    private static async Task<string> ErrorOf(HttpResponseMessage r) => (await r.Content.ReadAsStringAsync());

    private async Task<string> PaidOrderAsync() =>
        (await (await Post("/api/orders/checkout", Sale($"s-{Guid.NewGuid():N}").ToJsonString())).Content.ReadFromJsonAsync<CheckoutOrderResult>())!.OrderId;

    // ---- QA-03: empty request ids

    [Fact]
    public async Task A_second_sale_with_an_empty_request_id_is_refused_not_replayed_as_the_first()
    {
        var first = await Post("/api/orders/checkout", Sale("").ToJsonString());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, first.StatusCode);
        Assert.Contains("رقم العملية", await ErrorOf(first));
        var second = await Post("/api/orders/checkout", Sale("   ", qty: 3).ToJsonString());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, second.StatusCode);
        var missing = await Post("/api/orders/checkout", Sale("x", n => n.Remove("checkoutRequestId")).ToJsonString());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, missing.StatusCode);
        Assert.Equal(0, await _f.QueryAsync(db => db.Orders.CountAsync()));
    }

    [Fact]
    public async Task A_refund_or_a_shift_close_with_an_empty_request_id_is_refused()
    {
        var order = await PaidOrderAsync();
        var refund = await _c.PostAsJsonAsync("/api/orders/refund", new RefundOrderRequest("", order, 1000, "خطأ", _cashier, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, refund.StatusCode);
        var close = await _c.PostAsJsonAsync("/api/shifts/close", new CloseShiftRequest(" ", _shift, _cashier, 55000, null, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, close.StatusCode);
        Assert.Equal("Open", await _f.QueryAsync(db => db.Shifts.Where(s => s.Id == _shift).Select(s => s.Status).FirstAsync()));
    }

    // ---- QA-08: unexpected values are a 422 with a message, not a 500

    [Fact]
    public async Task An_unknown_payment_method_or_a_missing_item_list_is_a_validation_message()
    {
        var bitcoin = await Post("/api/orders/checkout", Sale($"s-{Guid.NewGuid():N}", n => n["paymentMethod"] = "bitcoin").ToJsonString());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, bitcoin.StatusCode);
        Assert.Contains("طريقة دفع", await ErrorOf(bitcoin));
        var noItems = await Post("/api/orders/checkout", Sale($"s-{Guid.NewGuid():N}", n => n["items"] = null).ToJsonString());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, noItems.StatusCode);
    }

    // ---- QA-07 / QA-09: refunds

    [Fact]
    public async Task A_refund_needs_a_reason_and_a_positive_amount_with_the_right_message()
    {
        var order = await PaidOrderAsync();
        var blank = await _c.PostAsJsonAsync("/api/orders/refund", new RefundOrderRequest($"r-{Guid.NewGuid():N}", order, 1000, "  ", _cashier, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, blank.StatusCode);
        Assert.Contains("سبب", await ErrorOf(blank));
        var zero = await _c.PostAsJsonAsync("/api/orders/refund", new RefundOrderRequest($"r-{Guid.NewGuid():N}", order, 0, "خطأ", _cashier, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, zero.StatusCode);
        Assert.Contains("أكبر من صفر", await ErrorOf(zero));
        Assert.Equal(0, await _f.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.Type == "refund")));
    }

    // ---- QA-02: Arabic-Indic digits in a phone

    [Fact]
    public async Task A_phone_in_arabic_indic_digits_is_the_same_customer_and_finds_its_addresses()
    {
        (await _c.PostAsJsonAsync("/api/customers/upsert", new UpsertCustomerRequest("01012345678", "أحمد", "المعادي", "شارع 9", null, null, null, null, null, null))).EnsureSuccessStatusCode();
        (await _c.PostAsJsonAsync("/api/customers/upsert", new UpsertCustomerRequest("٠١٠١٢٣٤٥٦٧٨", "أحمد", null, null, null, null, null, null, null, null))).EnsureSuccessStatusCode();

        Assert.Equal(1, await _f.QueryAsync(db => db.Customers.CountAsync()));
        var found = await _c.GetFromJsonAsync<CustomerDto>("/api/customers/lookup?phone=" + Uri.EscapeDataString("٠١٠١٢٣٤٥٦٧٨"));
        Assert.Equal("01012345678", found!.Phone);
        Assert.Single(found.Addresses);
    }

    // ---- QA-08 / QA-11: lengths

    [Fact]
    public async Task Text_that_does_not_fit_is_refused_with_a_message_not_a_500()
    {
        var phone = await _c.PostAsJsonAsync("/api/customers/upsert", new UpsertCustomerRequest(new string('1', 600), "س", null, null, null, null, null, null, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, phone.StatusCode);
        var name = await _c.PostAsJsonAsync("/api/customers/upsert", new UpsertCustomerRequest("01011111111", new string('س', 201), null, null, null, null, null, null, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, name.StatusCode);
        var notes = await Post("/api/orders/checkout", Sale($"s-{Guid.NewGuid():N}", n => n["items"]![0]!["notes"] = new string('ن', 501)).ToJsonString());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, notes.StatusCode);
        var expense = await _c.PostAsJsonAsync("/api/expenses", new Falah.Application.Expenses.AddExpenseRequest($"e-{Guid.NewGuid():N}", _shift, "نظافة ومطهرات", 1000,
            new string('و', 1001), "tre-drawer", _cashier, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, expense.StatusCode);
        var category = await _f.QueryAsync(db => db.Products.Select(p => p.CategoryId).FirstAsync());
        var product = await _c.PostAsJsonAsync("/api/products", new Falah.Application.Menu.CreateProductRequest($"p-{Guid.NewGuid():N}", category, new string('ب', 201), 500, false, null, 0, null,
            Array.Empty<Falah.Application.Menu.ProductVariantInput>(), _manager));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, product.StatusCode);
        Assert.Equal(0, await _f.QueryAsync(db => db.Customers.CountAsync()));
    }

    // ---- QA-06: values the screens refuse

    private string Employee(Action<JsonObject> change)
    {
        var node = new JsonObject
        {
            ["requestId"] = $"em-{Guid.NewGuid():N}", ["name"] = "موظف", ["employeeNumber"] = $"E-{Guid.NewGuid():N}"[..10], ["jobTitle"] = "طباخ",
            ["phone"] = "01000000000", ["hireDate"] = "2026-01-01", ["compensationType"] = "Daily", ["dailyWagePiastres"] = 30000,
            ["shiftValuePiastres"] = 0, ["monthlySalaryPiastres"] = null, ["canDeliver"] = false, ["role"] = "kitchen", ["actorUserId"] = _manager,
        };
        change(node);
        return node.ToJsonString();
    }

    [Fact]
    public async Task Negative_pay_a_numeric_pay_type_or_an_overlong_employee_number_is_refused()
    {
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Post("/api/employees", Employee(n => n["dailyWagePiastres"] = -30000))).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Post("/api/employees", Employee(n => { n["compensationType"] = "Monthly"; n["monthlySalaryPiastres"] = -1; }))).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Post("/api/employees", Employee(n => n["compensationType"] = "1"))).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Post("/api/employees", Employee(n => n["employeeNumber"] = new string('9', 600)))).StatusCode);
        Assert.Equal(0, await _f.QueryAsync(db => db.Employees.CountAsync()));
        (await Post("/api/employees", Employee(_ => { }))).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Receipt_copies_above_what_printing_honours_are_refused()
    {
        var response = await _c.PutAsync("/api/settings", new StringContent($"{{\"actorUserId\":\"{_manager}\",\"numberOfReceiptCopies\":6}}", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    // ---- QA-10: negative banknote count

    [Fact]
    public async Task A_negative_banknote_count_is_refused_not_turned_into_zero()
    {
        var response = await _c.PostAsJsonAsync($"/api/business-day/{_day}/cash-counts", new SaveCashCountRequest($"cc-{Guid.NewGuid():N}", _day, _shift,
            new[] { new DenominationRowInput(20000, 2), new DenominationRowInput(10000, -3) }, 0, _cashier));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(0, await _f.QueryAsync(db => db.CashDenominationCounts.CountAsync()));
    }
}
