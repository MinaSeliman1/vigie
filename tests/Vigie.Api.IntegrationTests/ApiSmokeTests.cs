using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

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

    private sealed record LoginPayload(string Token, UserPayload User);
    private sealed record UserPayload(Guid Id, string Name, string Email, string Role);
    private sealed record ShiftPayload(Guid Id);
}
