using Falah.Domain.Printing;
using Falah.PrintAgent;

// One station, one route, one printer. Started on the machine physically wired to that printer.
var testPrint = args.Contains("--test-print", StringComparer.OrdinalIgnoreCase);
var configArg = args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal));
var configPath = configArg ?? Path.Combine(AppContext.BaseDirectory, "appsettings.json");
var options = AgentOptions.Load(configPath);

var problems = options.Validate();
if (problems.Count > 0)
{
    Console.Error.WriteLine($"Configuration problems in {configPath}:");
    foreach (var problem in problems) Console.Error.WriteLine($"  - {problem}");
    return 1;
}

void Log(string message) => Console.WriteLine($"[{DateTimeOffset.Now:HH:mm:ss}] {message}");

// A one-shot ticket through the configured transport, so a station can be proved end to end at
// install time without waiting for a real order - and so the Arabic encoding question can be settled
// against the actual printer, which is the one thing no test can answer.
if (testPrint)
{
    var testTransport = options.CreateTransport();
    // Deliberately exercises every element type, because the point of a test print is to settle on
    // paper the things no unit test can: Arabic shaping, stroke weight, column alignment, the rule,
    // the Huge order number, and whether a mid-stream cut really yields two separate tickets.
    var sample = new ComposedTicket(PrintRoute.Receipt, "test-print", "TEST", new TicketElement[]
    {
        new TicketText("فالح أبو العنبه", Bold: true, Align: TicketAlign.Center, Size: TicketTextSize.Large),
        new TicketText("تذكرة اختبار - التيكت الأول", Align: TicketAlign.Center),
        new TicketRule(),
        new TicketText("رقم الطلب", Align: TicketAlign.Center),
        new TicketText("#0000", Bold: true, Align: TicketAlign.Center, Size: TicketTextSize.Huge),
        new TicketRule(),
        new TicketText($"الوقت: {DateTimeOffset.Now:yyyy/MM/dd HH:mm}", Align: TicketAlign.Center),
        new TicketText("احتفظ بهذا التيكت حتى استلام الطلب", Align: TicketAlign.Center),

        new TicketCut(),

        new TicketText("فالح أبو العنبه", Bold: true, Align: TicketAlign.Center, Size: TicketTextSize.Large),
        new TicketText("تذكرة اختبار - التيكت الثاني", Align: TicketAlign.Center),
        new TicketRule(TicketRuleStyle.Heavy),
        new TicketRow(new[]
        {
            new TicketCell("الصنف", 42, Bold: true),
            new TicketCell("الحجم", 20, Bold: true),
            new TicketCell("الكمية", 17, TicketAlign.Center, Bold: true),
            new TicketCell("السعر", 21, TicketAlign.End, Bold: true),
        }),
        new TicketRule(),
        new TicketRow(new[]
        {
            new TicketCell("شاورما فراخ", 42),
            new TicketCell("جامبو", 20),
            new TicketCell("2", 17, TicketAlign.Center),
            new TicketCell("85 ج", 21, TicketAlign.End),
        }),
        new TicketText("  ملاحظات: بدون بصل - زيادة طحينة"),
        new TicketRule(),
        new TicketRow(new[]
        {
            new TicketCell("بيبسي", 42),
            new TicketCell("—", 20),
            new TicketCell("1", 17, TicketAlign.Center),
            new TicketCell("20 ج", 21, TicketAlign.End),
        }),
        new TicketRule(TicketRuleStyle.Heavy),
        new TicketText("الإجمالي: 105 ج", Bold: true, Size: TicketTextSize.Large),
        new TicketRule(TicketRuleStyle.Blank),
        new TicketText($"المسار: {options.Route}"),
        new TicketText($"الترميز: {options.TextEncoding}"),
        new TicketText($"وضع الصورة: {(options.RenderAsImage ? "مفعل" : "غير مفعل")}"),
        new TicketText($"تغليظ الحروف: {(options.ThickenStrokes ? "مفعل" : "غير مفعل")}"),
        new TicketText("١٢٣٤٥٦٧٨٩٠ - 1234567890"),
        new TicketText("<<< آخر سطر - يجب أن يظهر كاملاً >>>", Bold: true),
    });
    var bytes = options.RenderAsImage && OperatingSystem.IsWindows()
        ? BitmapTicketRenderer.Render(sample, options.ImageWidthDots, options.ThickenStrokes, options.ImageMarginDots)
        : EscPosTicketRenderer.Render(sample, options.ResolveEncoding(), options.CodePageTableId);
    Log($"test print: {bytes.Length} bytes via {testTransport.Description}");
    try
    {
        await testTransport.SendAsync(bytes, CancellationToken.None);
        Log("test print: the transport accepted the ticket.");
        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"test print FAILED: {ex.GetType().Name}: {ex.Message}");
        return 2;
    }
}

// One polling agent per route on this machine. The server hands every pending job to whoever asks,
// so a second agent started for the same route by mistake (a double-click, a service plus a manual
// run) would print every ticket twice. A named mutex makes the second one refuse to start instead.
// Scoped to this Windows session's machine-wide namespace, keyed on the route.
using var singleAgent = new Mutex(initiallyOwned: false, $@"Global\FalahPrintAgent-{options.Route}");
bool ownsRoute;
try { ownsRoute = singleAgent.WaitOne(TimeSpan.Zero); }
catch (AbandonedMutexException) { ownsRoute = true; } // a previous agent crashed; the route is free
if (!ownsRoute)
{
    Console.Error.WriteLine($"another print agent is already running for route {options.Route} on this machine - " +
        "not starting a second one, because both would print every ticket.");
    return 4;
}

using var server = new PrintServerClient(options.ServerBaseAddress, TimeSpan.FromSeconds(options.TransportTimeoutSeconds));
try
{
    await server.AuthenticateAsync(options.Pin, CancellationToken.None);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"login failed: {ex.GetType().Name}: {ex.Message}");
    return 3;
}
var transport = options.CreateTransport();
var processor = new PrintJobProcessor(server, transport, options.ResolveEncoding(), options.ReportedByUserId, Log,
    unreported: UnreportedAttemptStore.ForRoute(AppContext.BaseDirectory, options.Route),
    codePageTableId: options.CodePageTableId, renderAsImage: options.RenderAsImage, imageWidthDots: options.ImageWidthDots,
    thickenStrokes: options.ThickenStrokes, imageMarginDots: options.ImageMarginDots);

using var shutdown = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; shutdown.Cancel(); };

Log($"logged in as {options.ReportedByUserId}");
Log($"print agent started - route {options.Route}, server {options.ServerBaseAddress}, transport {transport.Description}");
if (options.Transport.Equals("file", StringComparison.OrdinalIgnoreCase))
    Log("WARNING: the file transport writes tickets to disk and prints nothing. Use it for verification only.");

var pollDelay = TimeSpan.FromSeconds(options.PollSeconds);
while (!shutdown.IsCancellationRequested)
{
    try
    {
        await processor.DrainAsync(options.Route, shutdown.Token);
    }
    catch (OperationCanceledException) when (shutdown.IsCancellationRequested)
    {
        break;
    }
    catch (Exception ex)
    {
        // A server that is down must not kill the agent: the queue is durable server-side, so the
        // agent keeps polling and drains the backlog once the server is reachable again.
        Log($"poll failed: {ex.GetType().Name}: {ex.Message}");
    }

    try { await Task.Delay(pollDelay, shutdown.Token); }
    catch (OperationCanceledException) { break; }
}

Log("print agent stopped.");
return 0;
