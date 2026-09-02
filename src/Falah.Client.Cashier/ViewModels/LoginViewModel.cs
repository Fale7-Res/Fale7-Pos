using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Client.Cashier.Services;

namespace Falah.Client.Cashier.ViewModels;

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

    partial void OnPinChanged(string value) => OnPropertyChanged(nameof(PinDisplay));

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

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task SubmitAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var result = await _session.Api.LoginByPinAsync(Pin);
            if (!result.Success || result.Value is null)
            {
                ErrorMessage = "رمز PIN غير صحيح.";
                Pin = string.Empty;
                return;
            }

            _session.CurrentUser = result.Value;
            Pin = string.Empty;
            LoggedIn?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"تعذر الاتصال بالسيرفر: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
