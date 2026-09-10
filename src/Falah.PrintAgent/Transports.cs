using System.IO.Ports;
using System.Net.Sockets;

namespace Falah.PrintAgent;

/// <summary>Ethernet thermal printers listen on the JetDirect raw port (9100 by default) and accept
/// an ESC/POS byte stream directly, with no protocol wrapper.</summary>
public sealed class NetworkTicketTransport : ITicketTransport
{
    private readonly string _host;
    private readonly int _port;
    private readonly TimeSpan _timeout;

    public NetworkTicketTransport(string host, int port, TimeSpan timeout)
    {
        _host = host;
        _port = port;
        _timeout = timeout;
    }

    public string Description => $"network {_host}:{_port}";

    public async Task SendAsync(byte[] payload, CancellationToken ct)
    {
        using var client = new TcpClient();
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(_timeout);
        await client.ConnectAsync(_host, _port, timeoutCts.Token);
        await using var stream = client.GetStream();
        await stream.WriteAsync(payload, timeoutCts.Token);
        await stream.FlushAsync(timeoutCts.Token);
    }
}

/// <summary>Serial, including the virtual COM port most USB thermal printers expose.</summary>
public sealed class SerialTicketTransport : ITicketTransport
{
    private readonly string _portName;
    private readonly int _baudRate;
    private readonly TimeSpan _timeout;

    public SerialTicketTransport(string portName, int baudRate, TimeSpan timeout)
    {
        _portName = portName;
        _baudRate = baudRate;
        _timeout = timeout;
    }

    public string Description => $"serial {_portName}@{_baudRate}";

    public Task SendAsync(byte[] payload, CancellationToken ct)
    {
        using var port = new SerialPort(_portName, _baudRate)
        {
            WriteTimeout = (int)_timeout.TotalMilliseconds,
        };
        port.Open();
        port.Write(payload, 0, payload.Length);
        // Give the printer's buffer time to drain before the handle closes; closing mid-write on a
        // serial line truncates the ticket rather than failing loudly.
        port.BaseStream.Flush();
        return Task.CompletedTask;
    }
}

/// <summary>Writes each ticket to a file instead of a printer. This exists so the whole pipeline -
/// polling, composing, rendering, reporting - can be exercised and inspected byte for byte on a
/// machine with no printer attached. It is a verification transport, not a fallback: a station
/// configured this way prints nothing, and the log says so on every ticket.</summary>
public sealed class FileTicketTransport : ITicketTransport
{
    private readonly string _directory;

    public FileTicketTransport(string directory)
    {
        _directory = directory;
        Directory.CreateDirectory(_directory);
    }

    public string Description => $"file {_directory} (no physical printing)";

    public async Task SendAsync(byte[] payload, CancellationToken ct)
    {
        var path = Path.Combine(_directory, $"ticket-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}.escpos");
        await File.WriteAllBytesAsync(path, payload, ct);
    }
}
