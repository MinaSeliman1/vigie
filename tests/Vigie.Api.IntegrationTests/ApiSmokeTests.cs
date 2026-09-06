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
    public async Task Coordinator_cannot_create_a_shift_outside_site_opening_season()
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "coordonnateur@vigie.demo", password = "vigie-demo" });
        var payload = await login.Content.ReadFromJsonAsync<LoginPayload>();

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", payload!.Token);
        var response = await client.PostAsJsonAsync("/api/v1/shifts", new
        {
            siteId = Guid.Parse("20000000-0000-0000-0000-000000000002"),
            startUtc = "2026-10-01T14:00:00Z",
            endUtc = "2026-10-01T22:00:00Z",
            requiredLifeguards = 2
        });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("SITE_CLOSED", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Coordinator_can_assign_a_qualified_lifeguard()
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "coordonnateur@vigie.demo", password = "vigie-demo" });
        var payload = await login.Content.ReadFromJsonAsync<LoginPayload>();

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", payload!.Token);
        var response = await client.PostAsJsonAsync("/api/v1/shifts/40000000-0000-0000-0000-000000000002/assignments", new
        {
            employeeId = Guid.Parse("10000000-0000-0000-0000-000000000004")
        });
        var assignment = await response.Content.ReadFromJsonAsync<AssignmentPayload>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(Guid.Parse("40000000-0000-0000-0000-000000000002"), assignment?.ShiftId);
        Assert.Equal(Guid.Parse("10000000-0000-0000-0000-000000000004"), assignment?.EmployeeId);
    }

    [Fact]
    public async Task Lifeguard_cannot_assign_a_shift()
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "amelie@vigie.demo", password = "vigie-demo" });
        var payload = await login.Content.ReadFromJsonAsync<LoginPayload>();

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", payload!.Token);
        var response = await client.PostAsJsonAsync("/api/v1/shifts/40000000-0000-0000-0000-000000000002/assignments", new
        {
            employeeId = Guid.Parse("10000000-0000-0000-0000-000000000004")
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Coordinator_cannot_decide_the_same_swap_twice()
    {
        var lifeguardLogin = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "amelie@vigie.demo", password = "vigie-demo" });
        var lifeguard = await lifeguardLogin.Content.ReadFromJsonAsync<LoginPayload>();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", lifeguard!.Token);
        var create = await client.PostAsJsonAsync("/api/v1/swap-requests", new
        {
            assignmentId = Guid.Parse("50000000-0000-0000-0000-000000000001"),
            receiverId = Guid.Parse("10000000-0000-0000-0000-000000000004")
        });
        var swap = await create.Content.ReadFromJsonAsync<SwapPayload>();

        var coordinatorLogin = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "coordonnateur@vigie.demo", password = "vigie-demo" });
        var coordinator = await coordinatorLogin.Content.ReadFromJsonAsync<LoginPayload>();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", coordinator!.Token);
        var firstDecision = await client.PostAsync($"/api/v1/swap-requests/{swap!.Id}/approve", content: null);
        var secondDecision = await client.PostAsync($"/api/v1/swap-requests/{swap.Id}/approve", content: null);
        var body = await secondDecision.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, firstDecision.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondDecision.StatusCode);
        Assert.Contains("CONFLICT", body, StringComparison.Ordinal);
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
    public async Task Registration_creates_an_isolated_organization()
    {
        var organizationName = $"Centre {Guid.NewGuid():N}";
        var email = $"coordonnateur-{Guid.NewGuid():N}@exemple.test";
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            organizationName,
            name = "Coordonnateur du centre",
            email,
            password = "Mot-de-passe1"
        });
        var payload = await response.Content.ReadFromJsonAsync<RegistrationPayload>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(payload?.Login.Token);
        Assert.Equal(organizationName, payload?.Organization.Name);
        Assert.Equal(payload?.Organization.Id, payload?.Login.User.OrganizationId);

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", payload!.Login.Token);
        var organization = await client.GetFromJsonAsync<OrganizationPayload>("/api/v1/organization");
        var sites = await client.GetFromJsonAsync<SitePayload[]>("/api/v1/sites");
        var crossOrganizationShift = await client.PostAsJsonAsync("/api/v1/shifts", new
        {
            siteId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
            startUtc = "2026-09-20T14:00:00Z",
            endUtc = "2026-09-20T22:00:00Z",
            requiredLifeguards = 2
        });

        Assert.Equal(payload.Organization.Id, organization?.Id);
        Assert.Empty(sites!);
        Assert.Equal(HttpStatusCode.NotFound, crossOrganizationShift.StatusCode);
    }

    [Fact]
    public async Task Coordinator_can_invite_and_activate_a_lifeguard_once()
    {
        var coordinatorLogin = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "coordonnateur@vigie.demo", password = "vigie-demo" });
        var coordinator = await coordinatorLogin.Content.ReadFromJsonAsync<LoginPayload>();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", coordinator!.Token);
        var email = $"invite-{Guid.NewGuid():N}@exemple.test";
        var invitationResponse = await client.PostAsJsonAsync("/api/v1/invitations", new { email, name = "Sauveteur invité", role = "Lifeguard" });
        var invitation = await invitationResponse.Content.ReadFromJsonAsync<InvitationPayload>();

        Assert.Equal(HttpStatusCode.Created, invitationResponse.StatusCode);
        Assert.NotNull(invitation?.InviteToken);

        client.DefaultRequestHeaders.Authorization = null;
        var accept = await client.PostAsJsonAsync("/api/v1/invitations/accept", new { token = invitation!.InviteToken, password = "Mot-de-passe1" });
        var accepted = await accept.Content.ReadFromJsonAsync<LoginPayload>();
        var secondAccept = await client.PostAsJsonAsync("/api/v1/invitations/accept", new { token = invitation.InviteToken, password = "Mot-de-passe1" });

        Assert.Equal(HttpStatusCode.OK, accept.StatusCode);
        Assert.Equal(email, accepted?.User.Email);
        Assert.Equal("Lifeguard", accepted?.User.Role);
        Assert.Equal(HttpStatusCode.BadRequest, secondAccept.StatusCode);
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
        Assert.NotNull(context.Model.FindEntityType(typeof(Employee))!.FindProperty(nameof(Employee.PasswordHash)));
        Assert.NotNull(context.Model.FindEntityType(typeof(SiteCertificationRequirement)));
    }

    private sealed record LoginPayload(string Token, UserPayload User);
    private sealed record UserPayload(Guid Id, string Name, string Email, string Role, Guid OrganizationId, bool IsDemoAccount);
    private sealed record RegistrationPayload(LoginPayload Login, OrganizationPayload Organization);
    private sealed record OrganizationPayload(Guid Id, string Name, string Slug, DateTimeOffset CreatedAtUtc);
    private sealed record InvitationPayload(Guid Id, string Email, string Name, string Role, string Status, DateTimeOffset ExpiresAtUtc, string? InviteToken, string? InviteLink);
    private sealed record SitePayload(Guid Id);
    private sealed record ShiftPayload(Guid Id);
    private sealed record AssignmentPayload(Guid Id, Guid ShiftId, Guid EmployeeId, string EmployeeName);
    private sealed record SwapPayload(Guid Id);
}
