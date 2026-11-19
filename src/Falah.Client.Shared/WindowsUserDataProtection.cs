using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
namespace Falah.Client.Security;

public sealed class PendingDataProtectionException : IOException
{
    public PendingDataProtectionException(string message, Exception? inner = null) : base(message, inner) { }
}
public interface ILocalDataProtector
{
    byte[] Protect(byte[] value);
    byte[] Unprotect(byte[] value);
}

/// <summary>Windows DPAPI, CurrentUser scope, fixed application entropy. No plaintext fallback.</summary>
public sealed class WindowsUserDataProtection : ILocalDataProtector
{
    public static WindowsUserDataProtection Instance { get; } = new();
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("Falah.PendingOperations.Payload.v1");
    [StructLayout(LayoutKind.Sequential)]
    private struct Blob { public int Length; public IntPtr Data; }
    [DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptProtectData(ref Blob input, string? description, ref Blob entropy, IntPtr reserved, IntPtr prompt, int flags, out Blob output);
    [DllImport("crypt32.dll", SetLastError = true, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptUnprotectData(ref Blob input, IntPtr description, ref Blob entropy, IntPtr reserved, IntPtr prompt, int flags, out Blob output);
    [DllImport("kernel32.dll")] private static extern IntPtr LocalFree(IntPtr value);
    public byte[] Protect(byte[] value) => Transform(value, true);
    public byte[] Unprotect(byte[] value) => Transform(value, false);
    private static byte[] Transform(byte[] value, bool protect)
    {
        if (!OperatingSystem.IsWindows()) throw new PendingDataProtectionException("حماية الطلبات المحلية تتطلب حساب ويندوز.");
        var input = new Blob { Length = value.Length, Data = Marshal.AllocHGlobal(value.Length) };
        var entropy = new Blob { Length = Entropy.Length, Data = Marshal.AllocHGlobal(Entropy.Length) };
        Blob output = default;
        try
        {
            Marshal.Copy(value, 0, input.Data, value.Length);
            Marshal.Copy(Entropy, 0, entropy.Data, Entropy.Length);
            var ok = protect
                ? CryptProtectData(ref input, "Falah pending request", ref entropy, IntPtr.Zero, IntPtr.Zero, 1, out output)
                : CryptUnprotectData(ref input, IntPtr.Zero, ref entropy, IntPtr.Zero, IntPtr.Zero, 1, out output);
            if (!ok) throw new PendingDataProtectionException("تعذر حماية أو فتح الطلب المحفوظ بحساب ويندوز الحالي.", new Win32Exception(Marshal.GetLastWin32Error()));
            var result = new byte[output.Length];
            Marshal.Copy(output.Data, result, 0, result.Length);
            return result;
        }
        finally
        {
            Marshal.Copy(new byte[input.Length], 0, input.Data, input.Length);
            Marshal.FreeHGlobal(input.Data); Marshal.FreeHGlobal(entropy.Data);
            if (output.Data != IntPtr.Zero) { Marshal.Copy(new byte[output.Length], 0, output.Data, output.Length); LocalFree(output.Data); }
        }
    }
}
