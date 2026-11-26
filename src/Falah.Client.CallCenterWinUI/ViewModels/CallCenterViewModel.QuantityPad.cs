using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Client.CallCenterWinUI.Models;

namespace Falah.Client.CallCenterWinUI.ViewModels;

/// <summary>Owner, 2026-11-25: tapping a cart line's quantity opens a number pad, so a large quantity is typed rather
/// than pressed «+» many times. The pad starts empty (the first digit is the new quantity); never 0 or empty, at most
/// <see cref="MaxQuantity"/>. «رجوع» leaves the line as it was.</summary>
public partial class CallCenterViewModel
{
    public const int MaxQuantity = 999;

    [ObservableProperty] private CartLine? _quantityPadLine;
    [ObservableProperty] private string _quantityPadText = "";
    [ObservableProperty] private string? _quantityPadError;

    public bool IsQuantityPadOpen => QuantityPadLine is not null;
    public string QuantityPadTitle => QuantityPadLine is { } l ? $"عدد: {l.ProductName}" + (l.VariantName is { } v ? $" ({v})" : "") : "";

    partial void OnQuantityPadLineChanged(CartLine? value) { OnPropertyChanged(nameof(IsQuantityPadOpen)); OnPropertyChanged(nameof(QuantityPadTitle)); }
    partial void OnQuantityPadTextChanged(string value) => QuantityPadError = null;

    [RelayCommand]
    private void OpenQuantityPad(CartLine line)
    {
        if (!CanEditOperation || !Cart.Contains(line)) return;
        QuantityPadText = "";
        QuantityPadError = null;
        QuantityPadLine = line;
    }

    [RelayCommand]
    private void QuantityPadDigit(string digit)
    {
        if (digit is not { Length: 1 } || !char.IsAsciiDigit(digit[0]) || QuantityPadText.Length >= 3) return;
        QuantityPadText += digit;
    }

    [RelayCommand]
    private void QuantityPadBackspace() { if (QuantityPadText.Length > 0) QuantityPadText = QuantityPadText[..^1]; }

    [RelayCommand]
    private void QuantityPadClear() => QuantityPadText = "";

    [RelayCommand]
    private void CloseQuantityPad() => QuantityPadLine = null;

    [RelayCommand]
    private void ConfirmQuantityPad()
    {
        if (QuantityPadLine is not { } line) return;
        if (!CanEditOperation) { QuantityPadLine = null; return; }
        if (!int.TryParse(QuantityPadText.Trim(), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var quantity)
            || quantity < 1 || quantity > MaxQuantity)
        {
            QuantityPadError = $"اكتب عدد من 1 لـ {MaxQuantity}.";
            return;
        }
        line.Quantity = quantity;
        RecomputeCartTotals();
        QuantityPadLine = null;
    }
}
