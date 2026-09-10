namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Orders;
using Falah.Application.Printing;
using Falah.Application.Shifts;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// New real-world hardware topology (no legacy precedent - the reference app only ever did a
/// browser window.print() and combined charcoal+grill into one ticket): four physical printers
/// (Cashier receipt, Call Center receipt, Charcoal station, Grill station). Covers the critical
/// invariant this whole feature exists to preserve - successful order/payment != successful print;
/// creating, reporting on, or reprinting a print job must never create another order/payment, deduct
/// inventory again, or change any financial truth - plus station-based routing (separate ticket per
/// station, same order number, one printer's failure never affecting another's job) and the full
/// audit trail (attempt time, success/failure, retry/reprint count, last error).
/// </summary>
public class PrintingVerticalSliceTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _cashierId = default!;
    private string _menuManagerId = default!;
    private string _shiftId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        _cashierId = await _factory.SeedUserAsync("كاشير الاختبار", "cashier", "pos", "shifts_open_own");
        _menuManagerId = await _factory.SeedUserAsync("مدير المنيو", "supervisor", "menu_manage");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();

        var opened = await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, 50000))).Content.ReadFromJsonAsync<OpenShiftResult>();
        _shiftId = opened!.ShiftId;

        // Automatic printing off for this class. Checkout now creates an order's tickets by itself
        // (OrderAutoPrintService), which is the behaviour a restaurant needs and the reason these
        // tests started counting more jobs than they expected. But every test HERE is about the
        // explicit create-print-jobs endpoint - its idempotency, its reprints, its routing - and
        // mixing automatic jobs into those counts would make each one assert two things at once.
        // Automatic printing has its own test at the bottom of this file, with it switched on.
        var settingsAdmin = await _factory.SeedUserAsync("مسؤول الإعدادات", "manager", "settings_manage");
        (await _client.PutAsJsonAsync("/api/settings", new Falah.Application.Settings.UpdateSettingsRequest(
            settingsAdmin, AutoPrintReceipt: false, AutoPrintPreparationTicket: false))).EnsureSuccessStatusCode();
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private async Task<string> CreateProductAsync(string name, long pricePiastres, string? station)
    {
        var productId = await _factory.SeedProductAsync(pricePiastres, name);
        if (station is not null)
        {
            var response = await _client.PutAsJsonAsync($"/api/products/{productId}/preparation-station",
                new SetProductStationRequest(station, _menuManagerId));
            response.EnsureSuccessStatusCode();
        }
        return productId;
    }

    private Task<CheckoutOrderResult> CheckoutAsync(params (string ProductId, int Quantity)[] items) =>
        CheckoutAsync($"req-{Guid.NewGuid():N}", items);

    /// <summary>Checkout under a caller-chosen idempotency key, so a test can replay the exact same
    /// sale and check that its tickets are not printed twice.</summary>
    private async Task<CheckoutOrderResult> CheckoutAsync(string checkoutRequestId, params (string ProductId, int Quantity)[] items)
    {
        var request = new CheckoutOrderRequest(
            CheckoutRequestId: checkoutRequestId, CashierId: _cashierId, ShiftId: _shiftId,
            OrderType: "takeaway", PaymentMethod: "cash",
            Items: items.Select(i => new CheckoutItemRequest(i.ProductId, null, i.Quantity, null, null)).ToArray(),
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: 100_000, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: null, CustomerPhone: null,
            DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null);
        var response = await _client.PostAsJsonAsync("/api/orders/checkout", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CheckoutOrderResult>())!;
    }

    /// <summary>The ticket contract has carried a table number since it was written, PrintRules
    /// prints it, a domain test covers it - and nothing ever supplied one. The receipt composer
    /// passed a hard-coded null, so every dine-in ticket said "صالة" and left the runner carrying
    /// the tray to ask whose food it was.
    ///
    /// This follows one table number the whole way: typed at the till, frozen on the order,
    /// readable again after the order is looked up, and printed on both the customer's receipt and
    /// the kitchen's preparation ticket.</summary>
    [Fact]
    public async Task A_dine_in_table_number_reaches_the_receipt_and_the_kitchen_ticket()
    {
        var charcoalProduct = await _factory.SeedProductAsync(20_000, preparationStation: "Charcoal");

        var request = new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: _shiftId,
            OrderType: "dine_in", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(charcoalProduct, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: 20_000, SplitPayments: null, WalletProvider: null,
            TableNumber: "  12  ", CustomerName: null, CustomerPhone: null,
            DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null);

        var checkout = await (await _client.PostAsJsonAsync("/api/orders/checkout", request))
            .Content.ReadFromJsonAsync<CheckoutOrderResult>();

        // Survives the round trip, trimmed - a stray space must not print an empty table.
        var looked = await (await _client.GetAsync($"/api/orders/lookup?orderNumber={checkout!.OrderNumber}"))
            .Content.ReadFromJsonAsync<Falah.Application.Orders.OrderDetailsDto>();
        Assert.Equal("12", looked!.TableNumber);

        var jobs = await CreatePrintJobsAsync(checkout.OrderId, "Receipt");
        foreach (var job in jobs!.Jobs)
        {
            var ticket = await (await _client.GetAsync($"/api/print-jobs/{job.Id}/ticket"))
                .Content.ReadFromJsonAsync<ComposedTicketDto>();
            var text = string.Join("\n", ticket!.Lines.Select(l => l.Text));
            Assert.Contains("12", text);
        }

        // Both of them: the customer's copy and the charcoal station's.
        Assert.Contains(jobs.Jobs, j => j.Route == "Receipt");
        Assert.Contains(jobs.Jobs, j => j.Route == "Charcoal");
    }

    /// <summary>A table number left on a takeaway order would print a table that does not exist.</summary>
    [Fact]
    public async Task A_takeaway_order_carries_no_table_even_if_one_is_sent()
    {
        var productId = await _factory.SeedProductAsync(15_000);

        var request = new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: _shiftId,
            OrderType: "takeaway", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: 15_000, SplitPayments: null, WalletProvider: null,
            TableNumber: "7", CustomerName: null, CustomerPhone: null,
            DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null);

        var checkout = await (await _client.PostAsJsonAsync("/api/orders/checkout", request))
            .Content.ReadFromJsonAsync<CheckoutOrderResult>();

        var looked = await (await _client.GetAsync($"/api/orders/lookup?orderNumber={checkout!.OrderNumber}"))
            .Content.ReadFromJsonAsync<Falah.Application.Orders.OrderDetailsDto>();
        Assert.Null(looked!.TableNumber);
    }

    private Task<CreatePrintJobsResult?> CreatePrintJobsAsync(string orderId, string? customerRoute, bool isReprint = false, string? requestId = null) =>
        _client.PostAsJsonAsync($"/api/orders/{orderId}/print-jobs",
                new CreatePrintJobsRequest(requestId ?? $"preq-{Guid.NewGuid():N}", orderId, customerRoute, isReprint, _cashierId))
            .ContinueWith(t => t.Result.Content.ReadFromJsonAsync<CreatePrintJobsResult>()).Unwrap();

    // ---- Preparation station configuration ----

    [Fact]
    public async Task Setting_a_products_station_persists_it_and_is_visible_in_the_menu()
    {
        var productId = await CreateProductAsync("كباب حلة", 8000, "Charcoal");
        var menu = await (await _client.GetAsync("/api/menu")).Content.ReadFromJsonAsync<Falah.Application.Menu.MenuResponse>();
        var product = menu!.Categories.SelectMany(c => c.Products).Single(p => p.Id == productId);
        Assert.Equal("Charcoal", product.PreparationStation);
    }

    [Fact]
    public async Task Setting_an_invalid_station_value_is_rejected()
    {
        var productId = await _factory.SeedProductAsync(5000, "صنف اختبار");
        var response = await _client.PutAsJsonAsync($"/api/products/{productId}/preparation-station",
            new SetProductStationRequest("Kitchen", _menuManagerId));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Setting_a_products_station_requires_menu_manage()
    {
        var productId = await _factory.SeedProductAsync(5000, "صنف اختبار");
        var response = await _client.PutAsJsonAsync($"/api/products/{productId}/preparation-station",
            new SetProductStationRequest("Grill", _cashierId));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Clearing_a_products_station_with_null_removes_its_kitchen_ticket()
    {
        var productId = await CreateProductAsync("مشروب غازي", 1000, "Grill"); // deliberately wrong, then cleared
        await _client.PutAsJsonAsync($"/api/products/{productId}/preparation-station", new SetProductStationRequest(null, _menuManagerId));

        var order = await CheckoutAsync((productId, 1));
        var response = await CreatePrintJobsAsync(order.OrderId, "Receipt");
        Assert.Single(response!.Jobs); // only the Receipt job - no kitchen job at all
    }

    // ---- Station-based routing ----

    [Fact]
    public async Task An_order_touching_two_stations_creates_one_job_per_station_each_containing_only_its_own_items()
    {
        var charcoalProduct = await CreateProductAsync("كباب", 8000, "Charcoal");
        var grillProduct = await CreateProductAsync("فراخ مشوية", 9000, "Grill");
        var order = await CheckoutAsync((charcoalProduct, 1), (grillProduct, 1));

        var result = await CreatePrintJobsAsync(order.OrderId, "Receipt");
        var routes = result!.Jobs.Select(j => j.Route).ToHashSet();
        Assert.Equal(new HashSet<string> { "Receipt", "Charcoal", "Grill" }, routes);

        var charcoalJobId = result.Jobs.Single(j => j.Route == "Charcoal").Id;
        var charcoalTicket = await (await _client.GetAsync($"/api/print-jobs/{charcoalJobId}/ticket")).Content.ReadFromJsonAsync<ComposedTicketDto>();
        var charcoalText = string.Join('\n', charcoalTicket!.Lines.Select(l => l.Text));
        Assert.Contains("كباب", charcoalText);
        Assert.DoesNotContain("فراخ مشوية", charcoalText);
    }

    [Fact]
    public async Task An_order_with_no_kitchen_bound_items_creates_only_the_requested_customer_job()
    {
        var drinkProduct = await CreateProductAsync("مياه معدنية", 500, station: null);
        var order = await CheckoutAsync((drinkProduct, 1));

        var result = await CreatePrintJobsAsync(order.OrderId, "Receipt");
        Assert.Single(result!.Jobs);
        Assert.Equal("Receipt", result.Jobs.Single().Route);
    }

    [Fact]
    public async Task Requesting_the_CallCenter_route_creates_a_callcenter_job_not_a_receipt_job()
    {
        var product = await CreateProductAsync("مياه معدنية", 500, station: null);
        var order = await CheckoutAsync((product, 1));

        var result = await CreatePrintJobsAsync(order.OrderId, "CallCenter");
        Assert.Single(result!.Jobs);
        Assert.Equal("CallCenter", result.Jobs.Single().Route);
    }

    [Fact]
    public async Task Requesting_no_customer_route_creates_only_kitchen_jobs()
    {
        var product = await CreateProductAsync("كباب", 8000, "Charcoal");
        var order = await CheckoutAsync((product, 1));

        var result = await CreatePrintJobsAsync(order.OrderId, customerRoute: null);
        Assert.Single(result!.Jobs);
        Assert.Equal("Charcoal", result.Jobs.Single().Route);
    }

    [Fact]
    public async Task An_invalid_customer_route_is_rejected()
    {
        var product = await CreateProductAsync("كباب", 8000, "Charcoal");
        var order = await CheckoutAsync((product, 1));
        var response = await _client.PostAsJsonAsync($"/api/orders/{order.OrderId}/print-jobs",
            new CreatePrintJobsRequest($"preq-{Guid.NewGuid():N}", order.OrderId, "Charcoal", false, _cashierId)); // a kitchen route is not a valid CUSTOMER route
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task An_order_with_no_kitchen_items_and_no_customer_route_requested_is_rejected()
    {
        var product = await CreateProductAsync("مياه معدنية", 500, station: null);
        var order = await CheckoutAsync((product, 1));
        var response = await _client.PostAsJsonAsync($"/api/orders/{order.OrderId}/print-jobs",
            new CreatePrintJobsRequest($"preq-{Guid.NewGuid():N}", order.OrderId, null, false, _cashierId));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    // ---- The critical invariant: printing never touches order/payment/inventory ----

    [Fact]
    public async Task Creating_print_jobs_never_changes_the_orders_own_financial_fields()
    {
        var product = await CreateProductAsync("كباب", 8000, "Charcoal");
        var order = await CheckoutAsync((product, 1));
        await CreatePrintJobsAsync(order.OrderId, "Receipt");

        var orderEntity = await _factory.QueryAsync(db => db.Orders.AsNoTracking().FirstAsync(o => o.Id == order.OrderId));
        Assert.Equal(order.TotalPiastres, orderEntity.TotalPiastres);
        Assert.Equal(order.Status, orderEntity.Status);
    }

    [Fact]
    public async Task Creating_and_reporting_on_print_jobs_never_posts_a_stock_or_treasury_movement()
    {
        var (productId, _, _) = await _factory.SeedProductWithRecipeAsync(8000, quantityMilliUnitsPerUnit: 100);
        await _client.PutAsJsonAsync($"/api/products/{productId}/preparation-station", new SetProductStationRequest("Grill", _menuManagerId));
        var order = await CheckoutAsync((productId, 1));

        var stockBefore = await _factory.QueryAsync(db => db.StockMovements.CountAsync());
        var treasuryBefore = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync());

        var result = await CreatePrintJobsAsync(order.OrderId, "Receipt");
        foreach (var job in result!.Jobs)
        {
            await _client.PostAsJsonAsync($"/api/print-jobs/{job.Id}/report-result",
                new ReportPrintJobResultRequest($"rreq-{Guid.NewGuid():N}", job.Id, true, null, _cashierId));
        }
        // A reprint, and a failed report too - still no financial/stock effect either way.
        await CreatePrintJobsAsync(order.OrderId, "Receipt", isReprint: true);
        await _client.PostAsJsonAsync($"/api/print-jobs/{result.Jobs[0].Id}/report-result",
            new ReportPrintJobResultRequest($"rreq-{Guid.NewGuid():N}", result.Jobs[0].Id, false, "paper jam", _cashierId));

        var stockAfter = await _factory.QueryAsync(db => db.StockMovements.CountAsync());
        var treasuryAfter = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync());
        Assert.Equal(stockBefore, stockAfter);
        Assert.Equal(treasuryBefore, treasuryAfter);
    }

    // ---- Idempotency / reprint ----

    [Fact]
    public async Task Retrying_the_same_create_request_id_never_duplicates_jobs()
    {
        var product = await CreateProductAsync("كباب", 8000, "Charcoal");
        var order = await CheckoutAsync((product, 1));
        var requestId = $"preq-{Guid.NewGuid():N}";

        var first = await CreatePrintJobsAsync(order.OrderId, "Receipt", requestId: requestId);
        var second = await CreatePrintJobsAsync(order.OrderId, "Receipt", requestId: requestId);

        Assert.False(first!.WasReplayed);
        Assert.True(second!.WasReplayed);
        var count = await _factory.QueryAsync(db => db.PrintJobs.CountAsync(j => j.OrderId == order.OrderId));
        Assert.Equal(2, count); // Receipt + Charcoal, not duplicated
    }

    [Fact]
    public async Task Concurrent_create_print_job_requests_with_the_same_request_id_never_duplicate_tickets()
    {
        var product = await CreateProductAsync("كباب", 8000, "Charcoal");
        var order = await CheckoutAsync((product, 1));
        var requestId = $"preq-{Guid.NewGuid():N}";

        var tasks = Enumerable.Range(0, 6).Select(_ => CreatePrintJobsAsync(order.OrderId, "Receipt", requestId: requestId));
        var results = await Task.WhenAll(tasks);

        Assert.Equal(1, results.Count(r => !r!.WasReplayed));
        var count = await _factory.QueryAsync(db => db.PrintJobs.CountAsync(j => j.OrderId == order.OrderId));
        Assert.Equal(2, count); // Receipt + Charcoal - never multiplied by the 6 concurrent posts
    }

    [Fact]
    public async Task A_reprint_creates_a_brand_new_set_of_jobs_marked_as_a_reprint()
    {
        var product = await CreateProductAsync("كباب", 8000, "Charcoal");
        var order = await CheckoutAsync((product, 1));

        var original = await CreatePrintJobsAsync(order.OrderId, "Receipt");
        var reprint = await CreatePrintJobsAsync(order.OrderId, "Receipt", isReprint: true);

        Assert.False(reprint!.WasReplayed);
        Assert.All(reprint.Jobs, j => Assert.True(j.IsReprint));
        Assert.All(original!.Jobs, j => Assert.False(j.IsReprint));

        var totalJobs = await _factory.QueryAsync(db => db.PrintJobs.CountAsync(j => j.OrderId == order.OrderId));
        Assert.Equal(4, totalJobs); // 2 original + 2 reprint
    }

    // ---- Reporting results / retry ----

    [Fact]
    public async Task Reporting_a_successful_print_marks_the_job_succeeded_with_one_attempt()
    {
        var product = await CreateProductAsync("كباب", 8000, "Charcoal");
        var order = await CheckoutAsync((product, 1));
        var jobId = (await CreatePrintJobsAsync(order.OrderId, "Receipt"))!.Jobs.Single(j => j.Route == "Charcoal").Id;

        var response = await _client.PostAsJsonAsync($"/api/print-jobs/{jobId}/report-result",
            new ReportPrintJobResultRequest($"rreq-{Guid.NewGuid():N}", jobId, true, null, _cashierId));
        var job = await response.Content.ReadFromJsonAsync<PrintJobDto>();

        Assert.Equal("Succeeded", job!.Status);
        Assert.Equal(1, job.AttemptCount);
        Assert.Null(job.LastError);
    }

    [Fact]
    public async Task A_failed_print_records_the_error_and_can_be_retried_by_reporting_again()
    {
        var product = await CreateProductAsync("كباب", 8000, "Charcoal");
        var order = await CheckoutAsync((product, 1));
        var jobId = (await CreatePrintJobsAsync(order.OrderId, "Receipt"))!.Jobs.Single(j => j.Route == "Charcoal").Id;

        await _client.PostAsJsonAsync($"/api/print-jobs/{jobId}/report-result",
            new ReportPrintJobResultRequest($"rreq-{Guid.NewGuid():N}", jobId, false, "printer offline", _cashierId));
        var afterFailure = await (await _client.GetAsync($"/api/print-jobs/{jobId}")).Content.ReadFromJsonAsync<PrintJobDetailsDto>();
        Assert.Equal("Failed", afterFailure!.Job.Status);
        Assert.Equal("printer offline", afterFailure.Job.LastError);

        // Retry = report another attempt against the SAME job id.
        var retryResponse = await _client.PostAsJsonAsync($"/api/print-jobs/{jobId}/report-result",
            new ReportPrintJobResultRequest($"rreq-{Guid.NewGuid():N}", jobId, true, null, _cashierId));
        var afterRetry = await retryResponse.Content.ReadFromJsonAsync<PrintJobDto>();

        Assert.Equal("Succeeded", afterRetry!.Status);
        Assert.Equal(2, afterRetry.AttemptCount);
        Assert.Null(afterRetry.LastError);

        var details = await (await _client.GetAsync($"/api/print-jobs/{jobId}")).Content.ReadFromJsonAsync<PrintJobDetailsDto>();
        Assert.Equal(2, details!.Attempts.Count);
        Assert.False(details.Attempts[0].Succeeded);
        Assert.True(details.Attempts[1].Succeeded);
    }

    [Fact]
    public async Task Retrying_the_same_report_result_request_id_never_inflates_attempt_count()
    {
        var product = await CreateProductAsync("كباب", 8000, "Charcoal");
        var order = await CheckoutAsync((product, 1));
        var jobId = (await CreatePrintJobsAsync(order.OrderId, "Receipt"))!.Jobs.Single(j => j.Route == "Charcoal").Id;
        var requestId = $"rreq-{Guid.NewGuid():N}";

        var request = new ReportPrintJobResultRequest(requestId, jobId, true, null, _cashierId);
        await _client.PostAsJsonAsync($"/api/print-jobs/{jobId}/report-result", request);
        await _client.PostAsJsonAsync($"/api/print-jobs/{jobId}/report-result", request);

        var job = await (await _client.GetAsync($"/api/print-jobs/{jobId}")).Content.ReadFromJsonAsync<PrintJobDetailsDto>();
        Assert.Equal(1, job!.Job.AttemptCount);
        Assert.Single(job.Attempts);
    }

    [Fact]
    public async Task A_failure_at_one_station_never_affects_another_stations_job()
    {
        var charcoalProduct = await CreateProductAsync("كباب", 8000, "Charcoal");
        var grillProduct = await CreateProductAsync("فراخ مشوية", 9000, "Grill");
        var order = await CheckoutAsync((charcoalProduct, 1), (grillProduct, 1));
        var jobs = (await CreatePrintJobsAsync(order.OrderId, "Receipt"))!.Jobs;
        var charcoalJobId = jobs.Single(j => j.Route == "Charcoal").Id;
        var grillJobId = jobs.Single(j => j.Route == "Grill").Id;

        await _client.PostAsJsonAsync($"/api/print-jobs/{charcoalJobId}/report-result",
            new ReportPrintJobResultRequest($"rreq-{Guid.NewGuid():N}", charcoalJobId, false, "out of paper", _cashierId));

        var grillJob = await (await _client.GetAsync($"/api/print-jobs/{grillJobId}")).Content.ReadFromJsonAsync<PrintJobDetailsDto>();
        Assert.Equal("Pending", grillJob!.Job.Status);
        Assert.Equal(0, grillJob.Job.AttemptCount);
    }

    // ---- Historical routing consistency: printing must use the FROZEN order-time station, never
    // the product's current live configuration (a later menu edit must not retroactively change how
    // an already-committed order routes or reprints). ----

    [Fact]
    public async Task Reprinting_an_order_after_its_products_station_changes_still_routes_to_the_original_station()
    {
        var productId = await CreateProductAsync("كباب حلة", 8000, "Charcoal");
        var order = await CheckoutAsync((productId, 1));

        // Initial print, while the product still routes to Charcoal.
        var initial = await CreatePrintJobsAsync(order.OrderId, "Receipt");
        Assert.Contains(initial!.Jobs, j => j.Route == "Charcoal");

        // An administrator later moves the product to Grill - this must affect FUTURE orders only.
        var changeResponse = await _client.PutAsJsonAsync($"/api/products/{productId}/preparation-station",
            new SetProductStationRequest("Grill", _menuManagerId));
        changeResponse.EnsureSuccessStatusCode();

        // Reprinting the OLD order must still route to Charcoal - never Grill.
        var reprint = await CreatePrintJobsAsync(order.OrderId, "Receipt", isReprint: true);
        var reprintRoutes = reprint!.Jobs.Select(j => j.Route).ToHashSet();
        Assert.Contains("Charcoal", reprintRoutes);
        Assert.DoesNotContain("Grill", reprintRoutes);

        var reprintCharcoalJobId = reprint.Jobs.Single(j => j.Route == "Charcoal").Id;
        var reprintTicket = await (await _client.GetAsync($"/api/print-jobs/{reprintCharcoalJobId}/ticket")).Content.ReadFromJsonAsync<ComposedTicketDto>();
        Assert.Equal("Charcoal", reprintTicket!.Route);
        Assert.Contains("كباب حلة", string.Join('\n', reprintTicket.Lines.Select(l => l.Text)));

        // A brand-new order for the SAME (now-Grill) product must route to Grill.
        var newOrder = await CheckoutAsync((productId, 1));
        var newOrderJobs = await CreatePrintJobsAsync(newOrder.OrderId, "Receipt");
        var newOrderRoutes = newOrderJobs!.Jobs.Select(j => j.Route).ToHashSet();
        Assert.Contains("Grill", newOrderRoutes);
        Assert.DoesNotContain("Charcoal", newOrderRoutes);
    }

    [Fact]
    public async Task Mixed_station_orders_preserve_each_items_original_routing_independently_after_both_stations_are_reassigned()
    {
        var charcoalProduct = await CreateProductAsync("كباب", 8000, "Charcoal");
        var grillProduct = await CreateProductAsync("فراخ مشوية", 9000, "Grill");
        var order = await CheckoutAsync((charcoalProduct, 1), (grillProduct, 1));

        // Swap BOTH stations after the sale.
        await _client.PutAsJsonAsync($"/api/products/{charcoalProduct}/preparation-station", new SetProductStationRequest("Grill", _menuManagerId));
        await _client.PutAsJsonAsync($"/api/products/{grillProduct}/preparation-station", new SetProductStationRequest("Charcoal", _menuManagerId));

        // The OLD order's per-item routing must be entirely unaffected by the swap.
        var reprint = await CreatePrintJobsAsync(order.OrderId, "Receipt", isReprint: true);
        var routes = reprint!.Jobs.Select(j => j.Route).ToHashSet();
        Assert.Equal(new HashSet<string> { "Receipt", "Charcoal", "Grill" }, routes);

        var oldCharcoalJobId = reprint.Jobs.Single(j => j.Route == "Charcoal").Id;
        var oldCharcoalText = string.Join('\n', (await (await _client.GetAsync($"/api/print-jobs/{oldCharcoalJobId}/ticket"))
            .Content.ReadFromJsonAsync<ComposedTicketDto>())!.Lines.Select(l => l.Text));
        Assert.Contains("كباب", oldCharcoalText); // originally-Charcoal product still on the Charcoal ticket
        Assert.DoesNotContain("فراخ مشوية", oldCharcoalText);

        var oldGrillJobId = reprint.Jobs.Single(j => j.Route == "Grill").Id;
        var oldGrillText = string.Join('\n', (await (await _client.GetAsync($"/api/print-jobs/{oldGrillJobId}/ticket"))
            .Content.ReadFromJsonAsync<ComposedTicketDto>())!.Lines.Select(l => l.Text));
        Assert.Contains("فراخ مشوية", oldGrillText); // originally-Grill product still on the Grill ticket
        Assert.DoesNotContain("كباب", oldGrillText);

        // A NEW order for the same two products must reflect the NEW (swapped) configuration.
        var newOrder = await CheckoutAsync((charcoalProduct, 1), (grillProduct, 1));
        var newJobs = await CreatePrintJobsAsync(newOrder.OrderId, "Receipt");

        var newGrillJobId = newJobs!.Jobs.Single(j => j.Route == "Grill").Id;
        var newGrillText = string.Join('\n', (await (await _client.GetAsync($"/api/print-jobs/{newGrillJobId}/ticket"))
            .Content.ReadFromJsonAsync<ComposedTicketDto>())!.Lines.Select(l => l.Text));
        Assert.Contains("كباب", newGrillText); // now-Grill product (originally Charcoal) is on the new Grill ticket

        var newCharcoalJobId = newJobs.Jobs.Single(j => j.Route == "Charcoal").Id;
        var newCharcoalText = string.Join('\n', (await (await _client.GetAsync($"/api/print-jobs/{newCharcoalJobId}/ticket"))
            .Content.ReadFromJsonAsync<ComposedTicketDto>())!.Lines.Select(l => l.Text));
        Assert.Contains("فراخ مشوية", newCharcoalText); // now-Charcoal product (originally Grill) is on the new Charcoal ticket
    }

    // ---- Queries ----

    [Fact]
    public async Task Pending_jobs_for_a_route_lists_only_that_routes_pending_jobs()
    {
        var charcoalProduct = await CreateProductAsync("كباب", 8000, "Charcoal");
        var grillProduct = await CreateProductAsync("فراخ مشوية", 9000, "Grill");
        var order = await CheckoutAsync((charcoalProduct, 1), (grillProduct, 1));
        await CreatePrintJobsAsync(order.OrderId, "Receipt");

        var pendingCharcoal = await (await _client.GetAsync("/api/print-jobs/pending?route=Charcoal")).Content.ReadFromJsonAsync<List<PrintJobDto>>();
        Assert.Contains(pendingCharcoal!, j => j.OrderId == order.OrderId);
        Assert.DoesNotContain(pendingCharcoal!, j => j.Route != "Charcoal");
    }

    [Fact]
    public async Task Listing_jobs_for_an_order_returns_every_job_created_for_it()
    {
        var charcoalProduct = await CreateProductAsync("كباب", 8000, "Charcoal");
        var order = await CheckoutAsync((charcoalProduct, 1));
        await CreatePrintJobsAsync(order.OrderId, "Receipt");

        var jobs = await (await _client.GetAsync($"/api/orders/{order.OrderId}/print-jobs")).Content.ReadFromJsonAsync<List<PrintJobDto>>();
        Assert.Equal(2, jobs!.Count);
    }

    [Fact]
    public async Task A_receipt_ticket_includes_the_total_and_never_omits_it_even_with_no_kitchen_items()
    {
        var product = await CreateProductAsync("مياه معدنية", 500, station: null);
        var order = await CheckoutAsync((product, 1));
        var jobId = (await CreatePrintJobsAsync(order.OrderId, "Receipt"))!.Jobs.Single().Id;

        var ticket = await (await _client.GetAsync($"/api/print-jobs/{jobId}/ticket")).Content.ReadFromJsonAsync<ComposedTicketDto>();
        var text = string.Join('\n', ticket!.Lines.Select(l => l.Text));
        Assert.Contains("الإجمالي", text);
        Assert.Contains(order.OrderNumber, text);
    }

    // ---- Automatic printing at checkout ----

    [Fact]
    public async Task A_sale_creates_its_own_tickets_without_anyone_asking()
    {
        // The defect this guards: no print job was created at checkout by anything - not the server,
        // and in the cashier client not at all - so an order was taken on screen and the kitchen
        // never heard about it, while the Settings screen showed automatic printing switched on.
        var settingsAdmin = await _factory.SeedUserAsync("مسؤول الإعدادات ٢", "manager", "settings_manage");
        (await _client.PutAsJsonAsync("/api/settings", new Falah.Application.Settings.UpdateSettingsRequest(
            settingsAdmin, AutoPrintReceipt: true, AutoPrintPreparationTicket: true))).EnsureSuccessStatusCode();

        var charcoal = await CreateProductAsync("كباب فحم", 8000, "Charcoal");
        var order = await CheckoutAsync((charcoal, 1));

        var jobs = await (await _client.GetAsync($"/api/orders/{order.OrderId}/print-jobs")).Content.ReadFromJsonAsync<List<PrintJobDto>>();

        Assert.Equal(2, jobs!.Count);
        Assert.Contains(jobs, j => j.Route == "Receipt");
        Assert.Contains(jobs, j => j.Route == "Charcoal");
        Assert.All(jobs, j => Assert.False(j.IsReprint));
    }

    [Fact]
    public async Task Turning_automatic_preparation_tickets_off_leaves_only_the_customer_copy()
    {
        var settingsAdmin = await _factory.SeedUserAsync("مسؤول الإعدادات ٣", "manager", "settings_manage");
        (await _client.PutAsJsonAsync("/api/settings", new Falah.Application.Settings.UpdateSettingsRequest(
            settingsAdmin, AutoPrintReceipt: true, AutoPrintPreparationTicket: false))).EnsureSuccessStatusCode();

        var charcoal = await CreateProductAsync("كباب فحم ٢", 8000, "Charcoal");
        var order = await CheckoutAsync((charcoal, 1));

        var jobs = await (await _client.GetAsync($"/api/orders/{order.OrderId}/print-jobs")).Content.ReadFromJsonAsync<List<PrintJobDto>>();

        // The switches in Settings used to be read by nothing at all. They govern this now.
        Assert.Single(jobs!);
        Assert.Equal("Receipt", jobs![0].Route);
    }

    [Fact]
    public async Task Replaying_a_checkout_does_not_print_the_kitchen_a_second_copy()
    {
        var settingsAdmin = await _factory.SeedUserAsync("مسؤول الإعدادات ٤", "manager", "settings_manage");
        (await _client.PutAsJsonAsync("/api/settings", new Falah.Application.Settings.UpdateSettingsRequest(
            settingsAdmin, AutoPrintReceipt: true, AutoPrintPreparationTicket: true))).EnsureSuccessStatusCode();

        var charcoal = await CreateProductAsync("كباب فحم ٣", 8000, "Charcoal");
        var checkoutRequestId = $"co-{Guid.NewGuid():N}";

        var first = await CheckoutAsync(checkoutRequestId, (charcoal, 1));
        var again = await CheckoutAsync(checkoutRequestId, (charcoal, 1));

        Assert.Equal(first.OrderNumber, again.OrderNumber);
        var jobs = await (await _client.GetAsync($"/api/orders/{first.OrderId}/print-jobs")).Content.ReadFromJsonAsync<List<PrintJobDto>>();
        Assert.Equal(2, jobs!.Count);
    }
}
