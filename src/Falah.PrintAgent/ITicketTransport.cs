namespace Falah.PrintAgent;

/// <summary>
/// How rendered ESC/POS bytes physically reach a printer. Kept behind an interface because the four
/// print routes in this restaurant do not necessarily share one connection type - a network kitchen
/// printer and a serial receipt printer are both normal - and because the job-processing logic must
/// be testable without any hardware attached.
/// </summary>
public interface ITicketTransport
{
    /// <summary>A short description of where this transport actually sends bytes, used in logs and
    /// in the failure text reported back to the server.</summary>
    string Description { get; }

    /// <summary>Sends one complete ticket. Throws on failure - the caller reports every attempt,
    /// successful or not, so a swallowed error would silently mark a job printed that never was.</summary>
    Task SendAsync(byte[] payload, CancellationToken ct);
}
