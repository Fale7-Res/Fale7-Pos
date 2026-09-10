namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Customers;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// Ported from findCustomerByPhone/upsertCustomer in lib/store.tsx. Phone is the identity - never
/// looked up or merged by name. An address is matched by geographic content (area/street/building/
/// floor/apartment), never by landmark or delivery-service selection; a genuinely new address is
/// appended, a matching one is refreshed in place. No permission gate - this is normal order-taking
/// (Cashier or Call Center), not a privileged management action.
/// </summary>
public class CustomerVerticalSliceTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;

    public Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        return Task.CompletedTask;
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private static UpsertCustomerRequest MakeRequest(
        string phone = "01012345678", string name = "أحمد", string? area = "المعادي", string? street = "شارع 9",
        string? building = "5", string? floor = "3", string? apartment = "12", string? landmark = null,
        string? serviceId = "svc-1", string? serviceName = "توصيل سريع") =>
        new(phone, name, area, street, building, floor, apartment, landmark, serviceId, serviceName);

    [Fact]
    public async Task Upserting_a_new_customer_creates_it_with_its_first_address()
    {
        var response = await _client.PostAsJsonAsync("/api/customers/upsert", MakeRequest());
        response.EnsureSuccessStatusCode();
        var customer = await response.Content.ReadFromJsonAsync<CustomerDto>();

        Assert.Equal("01012345678", customer!.Phone);
        Assert.Equal("أحمد", customer.Name);
        Assert.Single(customer.Addresses);
        Assert.Equal("المعادي", customer.Addresses[0].Area);
    }

    [Fact]
    public async Task Upserting_the_same_phone_again_updates_the_name_and_never_creates_a_second_customer()
    {
        await _client.PostAsJsonAsync("/api/customers/upsert", MakeRequest(name: "أحمد"));
        var response = await _client.PostAsJsonAsync("/api/customers/upsert", MakeRequest(name: "أحمد محمد"));
        response.EnsureSuccessStatusCode();
        var customer = await response.Content.ReadFromJsonAsync<CustomerDto>();

        Assert.Equal("أحمد محمد", customer!.Name);
        var count = await _factory.QueryAsync(db => db.Customers.CountAsync(c => c.Phone == "01012345678"));
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Differently_formatted_phone_numbers_resolve_to_the_same_customer()
    {
        await _client.PostAsJsonAsync("/api/customers/upsert", MakeRequest(phone: "010-1234-5678"));
        var response = await _client.PostAsJsonAsync("/api/customers/upsert", MakeRequest(phone: "01012345678", name: "محدث"));
        response.EnsureSuccessStatusCode();

        var count = await _factory.QueryAsync(db => db.Customers.CountAsync(c => c.Phone == "01012345678"));
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task A_second_address_at_a_different_location_is_appended_not_replacing_the_first()
    {
        await _client.PostAsJsonAsync("/api/customers/upsert", MakeRequest(area: "المعادي", street: "شارع 9"));
        var response = await _client.PostAsJsonAsync("/api/customers/upsert", MakeRequest(area: "المهندسين", street: "شارع 26 يوليو"));
        response.EnsureSuccessStatusCode();
        var customer = await response.Content.ReadFromJsonAsync<CustomerDto>();

        Assert.Equal(2, customer!.Addresses.Count);
    }

    [Fact]
    public async Task Saving_the_same_geographic_address_again_refreshes_landmark_and_service_without_duplicating()
    {
        await _client.PostAsJsonAsync("/api/customers/upsert", MakeRequest(landmark: null, serviceId: "svc-old", serviceName: "خدمة قديمة"));
        var response = await _client.PostAsJsonAsync("/api/customers/upsert",
            MakeRequest(landmark: "بجوار الصيدلية", serviceId: "svc-new", serviceName: "خدمة جديدة"));
        response.EnsureSuccessStatusCode();
        var customer = await response.Content.ReadFromJsonAsync<CustomerDto>();

        Assert.Single(customer!.Addresses);
        Assert.Equal("بجوار الصيدلية", customer.Addresses[0].Landmark);
        Assert.Equal("svc-new", customer.Addresses[0].ServiceId);
    }

    [Fact]
    public async Task A_different_building_at_the_same_area_and_street_is_a_genuinely_different_address()
    {
        await _client.PostAsJsonAsync("/api/customers/upsert", MakeRequest(building: "5"));
        var response = await _client.PostAsJsonAsync("/api/customers/upsert", MakeRequest(building: "7"));
        response.EnsureSuccessStatusCode();
        var customer = await response.Content.ReadFromJsonAsync<CustomerDto>();

        Assert.Equal(2, customer!.Addresses.Count);
    }

    [Fact]
    public async Task Upserting_with_only_phone_and_name_creates_no_address()
    {
        var response = await _client.PostAsJsonAsync("/api/customers/upsert",
            new UpsertCustomerRequest("01099998888", "سارة", null, null, null, null, null, null, null, null));
        response.EnsureSuccessStatusCode();
        var customer = await response.Content.ReadFromJsonAsync<CustomerDto>();

        Assert.Empty(customer!.Addresses);
    }

    [Fact]
    public async Task Upserting_with_an_empty_phone_is_rejected()
    {
        var response = await _client.PostAsJsonAsync("/api/customers/upsert", MakeRequest(phone: ""));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Upserting_with_a_blank_name_is_rejected()
    {
        var response = await _client.PostAsJsonAsync("/api/customers/upsert", MakeRequest(name: "   "));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Looking_up_an_existing_phone_returns_the_customer_and_its_addresses()
    {
        await _client.PostAsJsonAsync("/api/customers/upsert", MakeRequest());
        var found = await (await _client.GetAsync("/api/customers/lookup?phone=01012345678")).Content.ReadFromJsonAsync<CustomerDto>();

        Assert.Equal("أحمد", found!.Name);
        Assert.Single(found.Addresses);
    }

    [Fact]
    public async Task Looking_up_an_unknown_phone_returns_not_found()
    {
        var response = await _client.GetAsync("/api/customers/lookup?phone=01000000099");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Looking_up_never_creates_or_modifies_anything()
    {
        await _client.GetAsync("/api/customers/lookup?phone=01000000099");
        var count = await _factory.QueryAsync(db => db.Customers.CountAsync());
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Getting_a_customer_by_id_returns_the_full_record()
    {
        var created = await (await _client.PostAsJsonAsync("/api/customers/upsert", MakeRequest())).Content.ReadFromJsonAsync<CustomerDto>();
        var fetched = await (await _client.GetAsync($"/api/customers/{created!.Id}")).Content.ReadFromJsonAsync<CustomerDto>();

        Assert.Equal(created.Id, fetched!.Id);
        Assert.Equal(created.Phone, fetched.Phone);
    }

    [Fact]
    public async Task Concurrent_upserts_for_a_brand_new_phone_with_the_same_address_create_exactly_one_customer_and_one_address()
    {
        var phone = "01055554444";
        var tasks = Enumerable.Range(0, 6).Select(_ => _client.PostAsJsonAsync("/api/customers/upsert", MakeRequest(phone: phone)));
        var responses = await Task.WhenAll(tasks);

        Assert.All(responses, r => Assert.True(r.IsSuccessStatusCode, $"unexpected status {r.StatusCode}"));
        var customerCount = await _factory.QueryAsync(db => db.Customers.CountAsync(c => c.Phone == phone));
        Assert.Equal(1, customerCount);
        var addressCount = await _factory.QueryAsync(db => db.CustomerAddresses.CountAsync(a => a.Area == "المعادي"));
        Assert.Equal(1, addressCount);
    }
}
