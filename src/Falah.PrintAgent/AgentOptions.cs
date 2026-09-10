using System.Text;
using System.Text.Json;

namespace Falah.PrintAgent;

/// <summary>
/// One station's print agent configuration. A station owns exactly one route - the receipt printer
/// at the cashier, the ticket printer at the grill - so the route is configuration, never something
/// the agent discovers or guesses.
/// </summary>
public sealed class AgentOptions
{
    public string ServerBaseAddress { get; set; } = "http://localhost:5151";

    /// <summary>Receipt, CallCenter, Charcoal or Grill - the PrintRoute this station serves.</summary>
    public string Route { get; set; } = "Receipt";

    /// <summary>The user the agent reports attempts as. Print attempts are recorded against a real
    /// user like every other action in this system.</summary>
    public string ReportedByUserId { get; set; } = "";

    /// <summary>network | serial | windows | file</summary>
    public string Transport { get; set; } = "file";

    public string PrinterHost { get; set; } = "";
    public int PrinterPort { get; set; } = 9100;
    public string SerialPort { get; set; } = "";
    public int SerialBaudRate { get; set; } = 9600;

    /// <summary>The Windows print queue name for a USB thermal printer, exactly as it appears in
    /// Printers &amp; scanners (for example "XP-80C").</summary>
    public string WindowsPrinterName { get; set; } = "";
    public string FileOutputDirectory { get; set; } = "tickets";

    public int PollSeconds { get; set; } = 3;
    public int TransportTimeoutSeconds { get; set; } = 15;

    /// <summary>The text encoding used for ticket characters. Arabic on ESC/POS hardware is
    /// firmware-dependent: some printers accept UTF-8 directly, others need a legacy Arabic codepage
    /// such as windows-1256. This stays configurable rather than hardcoded because it can only be
    /// settled against the physical printer, and getting it wrong prints mojibake, not an error.</summary>
    public string TextEncoding { get; set; } = "utf-8";

    public Encoding ResolveEncoding()
    {
        // windows-1256 and friends live in the CodePages provider, not in the default set.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(TextEncoding);
    }

    public ITicketTransport CreateTransport() => Transport.ToLowerInvariant() switch
    {
        "network" => new NetworkTicketTransport(PrinterHost, PrinterPort, TimeSpan.FromSeconds(TransportTimeoutSeconds)),
        "serial" => new SerialTicketTransport(SerialPort, SerialBaudRate, TimeSpan.FromSeconds(TransportTimeoutSeconds)),
        "windows" => OperatingSystem.IsWindows()
            ? new WindowsSpoolerTicketTransport(WindowsPrinterName)
            : throw new InvalidOperationException("The windows transport needs a Windows print spooler."),
        "file" => new FileTicketTransport(FileOutputDirectory),
        _ => throw new InvalidOperationException($"Unknown transport '{Transport}'. Use network, serial, windows or file."),
    };

    public IReadOnlyList<string> Validate()
    {
        var problems = new List<string>();
        if (string.IsNullOrWhiteSpace(ServerBaseAddress)) problems.Add("ServerBaseAddress is required.");
        if (!Enum.TryParse<Falah.Domain.Printing.PrintRoute>(Route, ignoreCase: true, out _))
            problems.Add("Route must be one of: Receipt, CallCenter, Charcoal, Grill.");
        if (string.IsNullOrWhiteSpace(ReportedByUserId)) problems.Add("ReportedByUserId is required.");
        switch (Transport.ToLowerInvariant())
        {
            case "network" when string.IsNullOrWhiteSpace(PrinterHost):
                problems.Add("PrinterHost is required for the network transport.");
                break;
            case "serial" when string.IsNullOrWhiteSpace(SerialPort):
                problems.Add("SerialPort is required for the serial transport.");
                break;
            case "windows" when string.IsNullOrWhiteSpace(WindowsPrinterName):
                problems.Add("WindowsPrinterName is required for the windows transport.");
                break;
            case "windows" when !OperatingSystem.IsWindows():
                problems.Add("The windows transport needs a Windows print spooler.");
                break;
            case "network" or "serial" or "windows" or "file":
                break;
            default:
                problems.Add($"Unknown transport '{Transport}'. Use network, serial, windows or file.");
                break;
        }
        if (PollSeconds < 1) problems.Add("PollSeconds must be at least 1.");
        return problems;
    }

    public static AgentOptions Load(string path)
    {
        if (!File.Exists(path)) return new AgentOptions();
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<AgentOptions>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        }) ?? new AgentOptions();
    }
}
