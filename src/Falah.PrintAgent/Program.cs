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
    var sample = new ComposedTicket(PrintRoute.Receipt, "test-print", "TEST", new[]
    {
        new TicketLine("فلاح - تذكرة اختبار", Bold: true, Centered: true, LargeFont: true),
        new TicketLine("Falah print agent test ticket", Centered: true),
        new TicketLine($"المسار: {options.Route}"),
        new TicketLine($"الترميز: {options.TextEncoding}"),
        new TicketLine($"الوقت: {DateTimeOffset.Now:yyyy/MM/dd HH:mm}"),
        new TicketLine("١٢٣٤٥٦٧٨٩٠ - 1234567890"),
    });
    var bytes = EscPosTicketRenderer.Render(sample, options.ResolveEncoding());
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

using var server = new PrintServerClient(options.ServerBaseAddress, TimeSpan.FromSeconds(options.TransportTimeoutSeconds));
var transport = options.CreateTransport();
var processor = new PrintJobProcessor(server, transport, options.ResolveEncoding(), options.ReportedByUserId, Log);

using var shutdown = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; shutdown.Cancel(); };

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
