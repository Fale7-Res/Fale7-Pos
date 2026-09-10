namespace Falah.Client.CashierWinUI.ViewModels;
// The only native dependency of the cashier model is the header's clock. Tests exercise the
// actual cashier source; this substitute omits its DispatcherTimer and visual notifications.
public sealed class HeaderViewModel
{
    public HeaderViewModel(Services.AppSession? session = null) { }
    public void Stop() { }
    public void Refresh() { }
    public void SetIdentity(string name) { }
}
