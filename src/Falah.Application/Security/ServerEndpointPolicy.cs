namespace Falah.Application.Security;

/// <summary>PINs and bearer tokens may cross a network only over HTTPS. No DNS resolution or
/// certificate bypass is involved; HTTP is permitted only for explicit loopback development.</summary>
public static class ServerEndpointPolicy
{
    public static Uri RequireSecureAddress(string address, bool allowInsecureRemoteHttp = false)
    {
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Fragment))
            throw new ArgumentException("عنوان خادم فالح غير صالح؛ استخدم عنوان HTTPS بدون بيانات دخول.", nameof(address));
        if (uri.Scheme == "http" && !uri.IsLoopback && !allowInsecureRemoteHttp)
            throw new ArgumentException("الاتصال بخادم فالح عبر الشبكة يتطلب HTTPS لحماية رمز الدخول والجلسة.", nameof(address));
        return uri;
    }
}
