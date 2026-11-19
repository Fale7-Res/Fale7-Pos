using Xunit;
namespace Falah.Client.Tests;
public class TransportSecurityTests
{
    [Theory]
    [InlineData("http://192.168.1.20:5151")]
    [InlineData("http://restaurant-pc:5151")]
    [InlineData("http://localhost.example.com:5151")]
    [InlineData("http://[fe80::1]:5151")]
    [InlineData("ftp://localhost:5151")]
    public void Both_stations_reject_insecure_remote_addresses_before_creating_the_transport(string address)
    {
        Assert.ThrowsAny<ArgumentException>(() => new Falah.Client.CashierWinUI.Services.ApiClient(address));
        Assert.ThrowsAny<ArgumentException>(() => new Falah.Client.CallCenterWinUI.Services.ApiClient(address));
    }
    [Theory]
    [InlineData("http://localhost:5151")]
    [InlineData("http://127.0.0.1:5151")]
    [InlineData("http://[::1]:5151")]
    [InlineData("https://restaurant-pc:5152")]
    public void Loopback_development_and_HTTPS_remain_available(string address)
    {
        _ = new Falah.Client.CashierWinUI.Services.ApiClient(address);
        _ = new Falah.Client.CallCenterWinUI.Services.ApiClient(address);
    }
}
