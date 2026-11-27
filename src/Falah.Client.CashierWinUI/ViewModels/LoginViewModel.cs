using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>Mirrors the reference app's lock screen exactly: PIN only, no username field. The
/// server determines the matching user (or rejects the PIN) - this screen has no idea who anyone
/// is until that response comes back.</summary>
public partial class LoginViewModel : ObservableObject
{
    private readonly AppSession _session;
    public event Action? LoggedIn;

    public LoginViewModel(AppSession session) => _session = session;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    private string _pin = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    private bool _isBusy;

    public string PinDisplay => new('●', Pin.Length);
    public string SubmitLabel => IsBusy ? "جارٍ الدخول..." : "فتح الشاشة";

    partial void OnPinChanged(string value) => OnPropertyChanged(nameof(PinDisplay));
    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(SubmitLabel));

    [RelayCommand]
    private void PressDigit(string digit)
    {
        if (Pin.Length >= 6) return;
        Pin += digit;
        ErrorMessage = null;
    }

    [RelayCommand]
    private void Backspace()
    {
        if (Pin.Length > 0) Pin = Pin[..^1];
    }

    [RelayCommand]
    private void Clear() => Pin = string.Empty;

    private bool CanSubmit() => Pin.Length >= 4 && !IsBusy;

    // ---- Owner, 2026-11-24: a PIN older than 90 days is changed right here, before the till opens ----

    /// <summary>The user who logged in with an expired PIN, held until the new PIN is set.</summary>
    private Falah.Application.Users.UserSummaryDto? _expiredUser;
    private string? _currentPin, _newPin;
    private string? _pinChangeRequestId;

    /// <summary>Null while logging in; the instruction while the expired PIN is being replaced.</summary>
    [ObservableProperty] private string? _pinChangePrompt;

    private void AskForNewPin(string? error = null)
    {
        _newPin = null;
        Pin = string.Empty;
        ErrorMessage = error;
        PinChangePrompt = "رمزك عدّى 90 يوم. اكتب رمز جديد (من 4 لـ 6 أرقام) واضغط الزرار.";
    }

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task SubmitAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            if (_expiredUser is { } user)
            {
                if (_newPin is null)
                {
                    _newPin = Pin;
                    Pin = string.Empty;
                    PinChangePrompt = "اكتب الرمز الجديد تاني للتأكيد.";
                    return;
                }
                if (Pin != _newPin) { AskForNewPin("الرمزين مش زي بعض. اكتب الرمز الجديد من الأول."); return; }
                _pinChangeRequestId ??= $"pin-{Guid.NewGuid():N}";
                var changed = await _session.Api.ChangePinAsync(new Falah.Application.Users.ChangePinRequest(
                    user.Id, _currentPin, _newPin, user.Id, _pinChangeRequestId));
                if (!changed.Success) { AskForNewPin(changed.Error ?? "تعذر تغيير الرمز. جرّب تاني."); return; }
                _expiredUser = null; _currentPin = null; _newPin = null; _pinChangeRequestId = null;
                PinChangePrompt = null;
                _session.CurrentUser = user;
                Pin = string.Empty;
                LoggedIn?.Invoke();
                return;
            }

            var result = await _session.Api.LoginByPinAsync(Pin);
            if (!result.Success || result.Value is null)
            {
                ErrorMessage = FailureMessage(result.OutcomeUnknown, result.StatusCode, result.Error);
                Pin = string.Empty;
                return;
            }

            if (_session.Api.LastLoginRequiresPinChange)
            {
                _expiredUser = result.Value;
                _currentPin = Pin;
                AskForNewPin();
                return;
            }

            _session.CurrentUser = result.Value;
            Pin = string.Empty;
            LoggedIn?.Invoke();
        }
        catch (Exception ex)
        {
            Falah.Client.Diagnostics.ClientLog.WriteException("sign-in", ex);
            ErrorMessage = $"تعذر الاتصال بالسيرفر: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
    /// <summary>The login failure in words that name its cause. Only a refusal from the server means the PIN was wrong;
    /// an unreachable server used to be reported as "رمز PIN غير صحيح." too.</summary>
    internal static string FailureMessage(bool outcomeUnknown, int statusCode, string? serverError) =>
        outcomeUnknown || statusCode == 0 ? "تعذر الاتصال بالسيرفر - تحقق من تشغيل Falah.Server ثم أعد المحاولة. لم يُرفض رمزك."
        : statusCode == 429 ? serverError ?? "محاولات كثيرة. انتظر قليلًا ثم أعد المحاولة."
        : statusCode >= 500 ? "تعذر تسجيل الدخول بسبب خطأ في السيرفر. أعد المحاولة أو راجع سجل السيرفر."
        : "رمز PIN غير صحيح.";
}
