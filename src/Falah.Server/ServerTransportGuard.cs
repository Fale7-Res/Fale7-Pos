namespace Falah.Server;
using Falah.Application.Security;
using System.Net;

internal static class ServerTransportGuard
{
    internal static bool AllowsRemoteHttp(IConfiguration configuration) => configuration.GetValue("Security:Transport:AllowInsecureRemoteHttp", false);
    internal static void ValidateBindings(IConfiguration configuration, IEnumerable<string> runtimeAddresses)
    {
        if (AllowsRemoteHttp(configuration)) return;
        var urls = (configuration["Urls"] ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var address in urls.Concat(runtimeAddresses).Distinct(StringComparer.OrdinalIgnoreCase)) Validate(address);
        foreach (var endpoint in configuration.GetSection("Kestrel:Endpoints").GetChildren())
            if (endpoint["Url"] is { Length: > 0 } address) Validate(address);
        if (urls.Length == 0 && !string.IsNullOrWhiteSpace(configuration["HTTP_PORTS"]))
            throw new ArgumentException("HTTP_PORTS يفتح اتصال شبكة غير مشفر؛ اضبط ربط HTTPS وشهادة الخادم أولًا.");
    }
    private static void Validate(string address)
    {
        // Kestrel accepts * and + as any-interface bindings; neither denotes loopback.
        if (address.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            (address.Contains("://*") || address.Contains("://+")))
            throw new ArgumentException("تشغيل خادم فالح على الشبكة يتطلب HTTPS؛ لا يمكن استخدام ربط HTTP عام.");
        ServerEndpointPolicy.RequireSecureAddress(address);
    }
    internal static bool IsInsecureNetworkRequest(HttpContext context) =>
        !context.Request.IsHttps && context.Connection.RemoteIpAddress is { } remote && !IPAddress.IsLoopback(remote);
}
