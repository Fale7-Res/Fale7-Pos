namespace Falah.Client.Tests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Settings;
using Falah.Client.CashierWinUI.Services;
using Falah.Client.CashierWinUI.ViewModels;
using Xunit;

/// <summary>Owner, 2026-11-18: how large a discount may be without a manager is a setting (10% to start),
/// shown and saved as a percentage.</summary>
public class SettingsDiscountLimitTests
{
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => send(request);
    }

    private static SettingsDto Loaded(int limitBasisPoints) => SettingsQueryServiceDefaults() with { MaxDiscountWithoutApprovalBasisPoints = limitBasisPoints };

    private static SettingsDto SettingsQueryServiceDefaults() => new(
        "فالح", "فرع", "عنوان", "1", "2", "3", "4", "شكرا", 2500, 2000, "ج.م", 5000, 10, "صباحية", "80mm",
        true, true, 1, true, 10, 15, 2, 2.0, 1.5);

    private static (SettingsViewModel vm, List<string> saved) Create(SettingsDto loaded)
    {
        var saved = new List<string>();
        var session = new AppSession(new ApiClient(new HttpClient(new Handler(async request =>
        {
            if (request.Method == HttpMethod.Put) saved.Add(await request.Content!.ReadAsStringAsync());
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(loaded) };
        })) { BaseAddress = new Uri("http://fixture.invalid") }))
        { CurrentUser = new("owner", "المالك", "owner", new[] { "all" }) };
        return (new SettingsViewModel(session, new HeaderViewModel()), saved);
    }

    [Fact]
    public async Task The_limit_is_shown_as_a_percentage()
    {
        var (vm, _) = Create(Loaded(1000));

        await vm.LoadAsync();

        Assert.Equal("10", vm.MaxDiscountPercentText);
    }

    [Fact]
    public async Task A_typed_percentage_is_saved_as_basis_points()
    {
        var (vm, saved) = Create(Loaded(1000));
        await vm.LoadAsync();

        vm.MaxDiscountPercentText = "12.5";
        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Contains("\"maxDiscountWithoutApprovalBasisPoints\":1250", Assert.Single(saved));
    }

    [Theory]
    [InlineData("100.01")]
    [InlineData("-1")]
    [InlineData("عشرة")]
    public async Task A_percentage_outside_0_to_100_is_refused_before_saving(string typed)
    {
        var (vm, saved) = Create(Loaded(1000));
        await vm.LoadAsync();

        vm.MaxDiscountPercentText = typed;
        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Empty(saved);
        Assert.NotNull(vm.SaveError);
    }
}
