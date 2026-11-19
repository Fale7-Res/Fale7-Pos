using System.Runtime.Versioning;
using System.ServiceProcess;
namespace Falah.PrintAgent;

[SupportedOSPlatform("windows")]
public sealed class AgentWindowsService : ServiceBase
{
    private readonly Func<CancellationToken, Task<int>> _run;
    private readonly Action<string> _log;
    private readonly CancellationTokenSource _stop = new();
    private Task<int>? _worker;

    public AgentWindowsService(string serviceName, Func<CancellationToken, Task<int>> run, Action<string> log)
    {
        ServiceName = serviceName; _run = run; _log = log;
        CanStop = true; CanShutdown = true; AutoLog = true;
    }
    protected override void OnStart(string[] args)
    {
        _worker = Task.Run(() => _run(_stop.Token));
        _ = _worker.ContinueWith(completed =>
        {
            ExitCode = completed.IsCompletedSuccessfully ? completed.Result : 1;
            if (!completed.IsCompletedSuccessfully) _log("Print service worker failed; inspect the journal before restarting.");
            if (!_stop.IsCancellationRequested) Stop();
        }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
    }
    protected override void OnStop()
    {
        _stop.Cancel();
        try { RequestAdditionalTime(20000); } catch (InvalidOperationException) { }
        try
        {
            if (_worker is not null && !_worker.Wait(TimeSpan.FromSeconds(20)))
                _log("Print service stop timed out; inspect the attempt journal and paper before retrying.");
        }
        catch (AggregateException) { ExitCode = 1; _log("Print service stopped after a worker failure."); }
    }
    protected override void OnShutdown() => OnStop();
    protected override void Dispose(bool disposing)
    {
        if (disposing) _stop.Dispose();
        base.Dispose(disposing);
    }
}
