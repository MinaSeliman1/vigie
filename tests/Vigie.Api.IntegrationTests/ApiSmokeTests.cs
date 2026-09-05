using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Testing;
using Vigie.Domain;
using Vigie.Infrastructure.Persistence;

namespace Vigie.Api.IntegrationTests;

public sealed class ApiSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public ApiSmokeTests(WebApplicationFactory<Program> factory) => client = factory.CreateClient();

    [Fact]
    public async Task Dashboard_requires_authentication()
    {
        var response = await client.GetAsync("/api/v1/dashboard");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Demo_coordinator_can_login_and_read_shifts()
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "coordonnateur@vigie.demo", password = "vigie-demo" });
        var payload = await login.Content.ReadFromJsonAsync<LoginPayload>();

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.NotNull(payload?.Token);
        Assert.Equal("Coordinator", payload?.User.Role);

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", payload!.Token);
        var shifts = await client.GetFromJsonAsync<ShiftPayload[]>("/api/v1/shifts");

        Assert.NotEmpty(shifts!);
    }

    [Fact]
    public async Task Lifeguard_cannot_approve_a_swap()
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "amelie@vigie.demo", password = "vigie-demo" });
        var payload = await login.Content.ReadFromJsonAsync<LoginPayload>();

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", payload!.Token);
        var response = await client.PostAsync("/api/v1/swap-requests/60000000-0000-0000-0000-000000000001/approve", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Invalid_credentials_return_a_problem_details_code()
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "intrus@example.test", password = "incorrect" });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("INVALID_CREDENTIALS", body, StringComparison.Ordinal);
    }

    [Fact]
    public void Ef_model_maps_the_opening_season_without_a_database()
    {
        var options = new DbContextOptionsBuilder<VigieDbContext>()
            .UseNpgsql("Host=localhost;Database=vigie;Username=vigie")
            .Options;

        using var context = new VigieDbContext(options);
        var site = context.Model.FindEntityType(typeof(Site));

        Assert.NotNull(site);
        Assert.NotNull(site!.FindProperty(nameof(Site.OpeningSeason)));
        Assert.NotNull(context.Model.FindEntityType(typeof(SiteCertificationRequirement)));
    }

    private sealed record LoginPayload(string Token, UserPayload User);
    private sealed record UserPayload(Guid Id, string Name, string Email, string Role);
    private sealed record ShiftPayload(Guid Id);
}
