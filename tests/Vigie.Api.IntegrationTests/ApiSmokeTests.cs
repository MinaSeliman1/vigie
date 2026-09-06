using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Testing;
using Vigie.Domain;
using Vigie.Infrastructure;
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
        var audit = await client.GetFromJsonAsync<AuditPayload[]>("/api/v1/audit");

        Assert.NotEmpty(shifts!);
        Assert.Contains(audit!, entry => entry.Action == "organization.created");
    }

    [Fact]
    public async Task Authenticated_user_can_restore_their_session()
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "amelie@vigie.demo", password = "vigie-demo" });
        var payload = await login.Content.ReadFromJsonAsync<LoginPayload>();

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", payload!.Token);
        var response = await client.GetAsync("/api/v1/auth/me");
        var user = await response.Content.ReadFromJsonAsync<UserPayload>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("amelie@vigie.demo", user?.Email);
        Assert.Equal("Lifeguard", user?.Role);
        Assert.True(user?.IsDemoAccount);
    }

    [Fact]
    public async Task Changing_password_revokes_the_previous_session()
    {
        var email = $"password-{Guid.NewGuid():N}@exemple.test";
        var registration = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            organizationName = $"Centre sécurité {Guid.NewGuid():N}",
            name = "Compte sécurité",
            email,
            password = "Mot-de-passe1"
        });
        var created = await registration.Content.ReadFromJsonAsync<RegistrationPayload>();
        var oldToken = created!.Login.Token;
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", oldToken);

        var change = await client.PostAsJsonAsync("/api/v1/auth/change-password", new
        {
            currentPassword = "Mot-de-passe1",
            newPassword = "Nouveau-motdepasse1"
        });
        var changed = await change.Content.ReadFromJsonAsync<LoginPayload>();

        Assert.Equal(HttpStatusCode.OK, change.StatusCode);
        Assert.NotEqual(oldToken, changed?.Token);

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", oldToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", changed!.Token);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
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
    public async Task Coordinator_can_read_audit_entries_for_their_organization()
    {
        var organizationName = $"Centre audit {Guid.NewGuid():N}";
        var registration = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            organizationName,
            name = "Coordonnateur audit",
            email = $"audit-{Guid.NewGuid():N}@exemple.test",
            password = "Mot-de-passe1"
        });
        var created = await registration.Content.ReadFromJsonAsync<RegistrationPayload>();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", created!.Login.Token);

        var response = await client.GetFromJsonAsync<AuditPayload[]>("/api/v1/audit");
        var export = await client.GetAsync("/api/v1/audit/export");
        var exportBody = await export.Content.ReadAsStringAsync();

        Assert.Contains(response!, entry => entry.Action == "organization.created" && entry.EntityType == "Organization");
        Assert.Equal(HttpStatusCode.OK, export.StatusCode);
        Assert.Contains("organization.created", exportBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Coordinator_can_invite_and_activate_a_lifeguard_once()
    {
        var coordinatorLogin = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "coordonnateur@vigie.demo", password = "vigie-demo" });
        var coordinator = await coordinatorLogin.Content.ReadFromJsonAsync<LoginPayload>();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", coordinator!.Token);
        var email = $"invite-{Guid.NewGuid():N}@exemple.test";
        var sites = await client.GetFromJsonAsync<SitePayload[]>("/api/v1/sites");
        var invitationResponse = await client.PostAsJsonAsync("/api/v1/invitations", new { email, name = "Sauveteur invité", role = "Lifeguard", siteId = sites!.Single(site => site.Name == "Piscine du Nord").Id });
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
    public async Task Pool_chief_can_list_only_members_of_their_pool()
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "coordonnateur@vigie.demo", password = "vigie-demo" });
        var payload = await login.Content.ReadFromJsonAsync<LoginPayload>();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", payload!.Token);

        var sectors = await client.GetFromJsonAsync<SectorPayload[]>("/api/v1/sectors");
        var sites = await client.GetFromJsonAsync<SitePayload[]>("/api/v1/sites");
        var members = await client.GetFromJsonAsync<MemberPayload[]>("/api/v1/members");

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Single(sectors!);
        Assert.Equal("NORD", sectors![0].Code);
        Assert.Single(sites!);
        Assert.Equal("Piscine du Nord", sites![0].Name);
        Assert.NotEmpty(members!);
        Assert.All(members!, member => Assert.Equal(Guid.Parse("20000000-0000-0000-0000-000000000001"), member.SiteId));
        Assert.DoesNotContain(members!, member => member.Role == "AquaticDirector");
    }

    [Fact]
    public async Task Lifeguard_cannot_create_a_sector()
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "amelie@vigie.demo", password = "vigie-demo" });
        var payload = await login.Content.ReadFromJsonAsync<LoginPayload>();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", payload!.Token);

        var response = await client.PostAsJsonAsync("/api/v1/sectors", new { name = "Secteur interdit", code = "NOPE" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Newly_registered_account_is_an_aquatic_director()
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            organizationName = $"Régie {Guid.NewGuid():N}",
            name = "Directrice aquatique",
            email = $"regie-{Guid.NewGuid():N}@exemple.test",
            password = "Mot-de-passe1"
        });
        var payload = await response.Content.ReadFromJsonAsync<RegistrationPayload>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("AquaticDirector", payload?.Login.User.Role);

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", payload!.Login.Token);
        var createSector = await client.PostAsJsonAsync("/api/v1/sectors", new { name = "Secteur central", code = "CENTRAL" });
        Assert.Equal(HttpStatusCode.Created, createSector.StatusCode);
    }

    [Fact]
    public async Task Aquatic_director_can_invite_a_pool_chief_with_a_site_scope()
    {
        var email = $"directeur-{Guid.NewGuid():N}@exemple.test";
        var registration = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            organizationName = $"Centre hiérarchie {Guid.NewGuid():N}",
            name = "Directeur de la régie",
            email,
            password = "Mot-de-passe1"
        });
        var account = await registration.Content.ReadFromJsonAsync<RegistrationPayload>();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", account!.Login.Token);
        var siteResponse = await client.PostAsJsonAsync("/api/v1/sites", new
        {
            name = "Piscine centrale",
            type = "Indoor",
            timeZoneId = "Eastern Standard Time",
            startMonth = 1,
            startDay = 1,
            endMonth = 12,
            endDay = 31
        });
        var site = await siteResponse.Content.ReadFromJsonAsync<SitePayload>();
        var inviteEmail = $"chef-{Guid.NewGuid():N}@exemple.test";
        var invitationResponse = await client.PostAsJsonAsync("/api/v1/invitations", new { email = inviteEmail, name = "Chef central", role = "PoolChief", siteId = site!.Id });
        var invitation = await invitationResponse.Content.ReadFromJsonAsync<InvitationPayload>();

        client.DefaultRequestHeaders.Authorization = null;
        var accept = await client.PostAsJsonAsync("/api/v1/invitations/accept", new { token = invitation!.InviteToken, password = "Mot-de-passe1" });
        var accepted = await accept.Content.ReadFromJsonAsync<LoginPayload>();

        Assert.Equal(HttpStatusCode.Created, invitationResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, accept.StatusCode);
        Assert.Equal("PoolChief", accepted?.User.Role);
        Assert.Equal(site.Id, accepted?.User.SiteId);
    }

    [Fact]
    public async Task Sector_manager_is_scoped_to_their_sector()
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "charge.nord@vigie.demo", password = "vigie-demo" });
        var payload = await login.Content.ReadFromJsonAsync<LoginPayload>();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", payload!.Token);

        var sectors = await client.GetFromJsonAsync<SectorPayload[]>("/api/v1/sectors");
        var members = await client.GetFromJsonAsync<MemberPayload[]>("/api/v1/members");
        var nordShift = await client.PostAsJsonAsync("/api/v1/shifts", new
        {
            siteId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
            startUtc = "2026-09-20T14:00:00Z",
            endUtc = "2026-09-20T22:00:00Z",
            requiredLifeguards = 2
        });
        var parcShift = await client.PostAsJsonAsync("/api/v1/shifts", new
        {
            siteId = Guid.Parse("20000000-0000-0000-0000-000000000002"),
            startUtc = "2026-09-10T14:00:00Z",
            endUtc = "2026-09-10T22:00:00Z",
            requiredLifeguards = 2
        });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal("SectorManager", payload.User.Role);
        Assert.Single(sectors!);
        Assert.Equal("NORD", sectors![0].Code);
        Assert.NotEmpty(members!);
        Assert.All(members!.Where(member => member.Role != "SectorManager"), member => Assert.Equal(Guid.Parse("20000000-0000-0000-0000-000000000001"), member.SiteId));
        Assert.Contains(members!, member => member.Role == "SectorManager" && member.SectorId == sectors[0].Id);
        Assert.Equal(HttpStatusCode.Created, nordShift.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, parcShift.StatusCode);
    }

    [Fact]
    public async Task Sector_manager_cannot_elevate_a_membership_to_director()
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "charge.nord@vigie.demo", password = "vigie-demo" });
        var payload = await login.Content.ReadFromJsonAsync<LoginPayload>();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", payload!.Token);

        var members = await client.GetFromJsonAsync<MemberPayload[]>("/api/v1/members");
        var ownMembership = members!.Single(member => member.EmployeeId == payload.User.Id);
        var response = await client.PatchAsJsonAsync($"/api/v1/memberships/{ownMembership.Id}", new { role = "AquaticDirector", expectedVersion = ownMembership.Version });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Sector_manager_audit_is_limited_to_their_sector()
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "charge.nord@vigie.demo", password = "vigie-demo" });
        var payload = await login.Content.ReadFromJsonAsync<LoginPayload>();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", payload!.Token);

        var audit = await client.GetFromJsonAsync<AuditPayload[]>("/api/v1/audit");

        Assert.DoesNotContain(audit!, entry => entry.EntityType == "Organization");
        Assert.DoesNotContain(audit!, entry => entry.EntityType == "Site" && entry.Details?.Contains("parc", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public async Task Sector_manager_can_approve_a_swap_inside_their_sector()
    {
        var coordinatorLogin = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "coordonnateur@vigie.demo", password = "vigie-demo" });
        var coordinator = await coordinatorLogin.Content.ReadFromJsonAsync<LoginPayload>();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", coordinator!.Token);
        var shiftResponse = await client.PostAsJsonAsync("/api/v1/shifts", new
        {
            siteId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
            startUtc = "2026-09-25T14:00:00Z",
            endUtc = "2026-09-25T22:00:00Z",
            requiredLifeguards = 2
        });
        var shift = await shiftResponse.Content.ReadFromJsonAsync<ShiftPayload>();
        var assignmentResponse = await client.PostAsJsonAsync($"/api/v1/shifts/{shift!.Id}/assignments", new { employeeId = Guid.Parse("10000000-0000-0000-0000-000000000002") });
        var assignment = await assignmentResponse.Content.ReadFromJsonAsync<AssignmentPayload>();

        var lifeguardLogin = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "amelie@vigie.demo", password = "vigie-demo" });
        var lifeguard = await lifeguardLogin.Content.ReadFromJsonAsync<LoginPayload>();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", lifeguard!.Token);
        var create = await client.PostAsJsonAsync("/api/v1/swap-requests", new
        {
            assignmentId = assignment!.Id,
            receiverId = Guid.Parse("10000000-0000-0000-0000-000000000004")
        });
        var swap = await create.Content.ReadFromJsonAsync<SwapPayload>();

        var managerLogin = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "charge.nord@vigie.demo", password = "vigie-demo" });
        var manager = await managerLogin.Content.ReadFromJsonAsync<LoginPayload>();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", manager!.Token);
        var decision = await client.PostAsync($"/api/v1/swap-requests/{swap!.Id}/approve", content: null);
        var decided = await decision.Content.ReadFromJsonAsync<SwapPayload>();

        Assert.Equal(HttpStatusCode.Created, shiftResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, assignmentResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        Assert.Equal(HttpStatusCode.OK, decision.StatusCode);
        Assert.Equal("Approved", decided?.Status);
    }

    [Fact]
    public async Task Aquatic_director_can_see_the_laval_municipal_catalog()
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "regie@vigie.demo", password = "vigie-demo" });
        var payload = await login.Content.ReadFromJsonAsync<LoginPayload>();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", payload!.Token);

        var sites = await client.GetFromJsonAsync<SitePayload[]>("/api/v1/sites");
        var sectors = await client.GetFromJsonAsync<SectorPayload[]>("/api/v1/sectors");

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal("AquaticDirector", payload.User.Role);
        Assert.True(sites!.Count(site => site.IsMunicipal) >= 27);
        Assert.Contains(sites!, site => site.Name == "Piscine Val-des-Arbres" && site.Neighborhood == "Vimont");
        Assert.Equal(5, sectors!.Length);
        Assert.Contains(sectors, sector => sector.Code == "NORD");
        Assert.Contains(sectors, sector => sector.Code == "CENTRE");
        Assert.Contains(sectors, sector => sector.Code == "EST");
        Assert.Contains(sectors, sector => sector.Code == "OUEST");
        Assert.Contains(sectors, sector => sector.Code == "PARC");
    }

    [Fact]
    public async Task Aquatic_director_can_reschedule_and_cancel_a_shift()
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "regie@vigie.demo", password = "vigie-demo" });
        var payload = await login.Content.ReadFromJsonAsync<LoginPayload>();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", payload!.Token);
        var sites = await client.GetFromJsonAsync<SitePayload[]>("/api/v1/sites");
        var site = sites!.Single(item => item.Name == "Piscine Val-des-Arbres");
        var create = await client.PostAsJsonAsync("/api/v1/shifts", new
        {
            siteId = site.Id,
            startUtc = "2026-09-21T13:00:00Z",
            endUtc = "2026-09-21T21:00:00Z",
            requiredLifeguards = 2
        });
        var created = await create.Content.ReadFromJsonAsync<ShiftPayload>();
        var update = await client.PatchAsJsonAsync($"/api/v1/shifts/{created!.Id}", new
        {
            startUtc = "2026-09-22T14:00:00Z",
            endUtc = "2026-09-22T22:00:00Z",
            requiredLifeguards = 3
        });
        var updated = await update.Content.ReadFromJsonAsync<ShiftPayload>();
        var cancel = await client.PostAsync($"/api/v1/shifts/{created.Id}/cancel", content: null);
        var cancelled = await cancel.Content.ReadFromJsonAsync<ShiftPayload>();

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal("Open", updated?.Status);
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        Assert.Equal("Cancelled", cancelled?.Status);
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
        Assert.NotNull(site.FindProperty(nameof(Site.SectorId)));
        Assert.NotNull(site.FindProperty(nameof(Site.Address)));
        Assert.NotNull(site.FindProperty(nameof(Site.Neighborhood)));
        Assert.NotNull(site.FindProperty(nameof(Site.IsMunicipal)));
        Assert.NotNull(context.Model.FindEntityType(typeof(Employee))!.FindProperty(nameof(Employee.PasswordHash)));
        Assert.NotNull(context.Model.FindEntityType(typeof(SiteCertificationRequirement)));
        var sector = context.Model.FindEntityType(typeof(Sector));
        Assert.NotNull(sector);
        Assert.NotNull(context.Model.FindEntityType(typeof(OrganizationMembership)));
        Assert.Contains(sector!.GetIndexes(), index => index.IsUnique && index.Properties.Select(property => property.Name).SequenceEqual(new[] { nameof(Sector.OrganizationId), nameof(Sector.Code) }));
    }

    [Fact]
    public void Laval_catalog_keys_are_isolated_per_organization()
    {
        var first = LavalPoolCatalog.ForOrganization(Guid.NewGuid());
        var second = LavalPoolCatalog.ForOrganization(Guid.NewGuid());

        Assert.Equal(27, first.Count);
        Assert.Equal(27, second.Count);
        Assert.Equal(first.Count, first.Select(pool => pool.SiteId).Distinct().Count());
        Assert.Equal(4, first.Select(pool => pool.SectorId).Distinct().Count());
        Assert.Equal(4, first.Select(pool => pool.SectorCode).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Contains(first, pool => pool.SectorCode == "NORD" && pool.SectorName == "Secteur Nord");
        Assert.Empty(first.Select(pool => pool.SiteId).Intersect(second.Select(pool => pool.SiteId)));
        Assert.Empty(first.Select(pool => pool.SectorId).Intersect(second.Select(pool => pool.SectorId)));
    }

    private sealed record LoginPayload(string Token, UserPayload User);
    private sealed record UserPayload(Guid Id, string Name, string Email, string Role, Guid OrganizationId, bool IsDemoAccount, Guid? SiteId = null, Guid? SectorId = null);
    private sealed record RegistrationPayload(LoginPayload Login, OrganizationPayload Organization);
    private sealed record OrganizationPayload(Guid Id, string Name, string Slug, DateTimeOffset CreatedAtUtc);
    private sealed record InvitationPayload(Guid Id, string Email, string Name, string Role, string Status, DateTimeOffset ExpiresAtUtc, string? InviteToken, string? InviteLink);
    private sealed record AuditPayload(Guid Id, string Action, string EntityType, Guid? EntityId, string? Details, string? ActorName, DateTimeOffset CreatedAtUtc);
    private sealed record SitePayload(Guid Id, string Name = "", string Type = "", string TimeZoneId = "", OpeningSeasonPayload? OpeningSeason = null, string Address = "", string Neighborhood = "", bool IsMunicipal = false);
    private sealed record OpeningSeasonPayload(int StartMonth, int StartDay, int EndMonth, int EndDay);
    private sealed record ShiftPayload(Guid Id, string? Status = null);
    private sealed record AssignmentPayload(Guid Id, Guid ShiftId, Guid EmployeeId, string EmployeeName);
    private sealed record SwapPayload(Guid Id, string? Status = null);
    private sealed record SectorPayload(Guid Id, Guid OrganizationId, string Name, string Code, bool IsActive, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc);
    private sealed record MemberPayload(Guid Id, Guid EmployeeId, string EmployeeName, string Email, string Role, Guid OrganizationId, Guid? SiteId, string? SiteName, Guid? SectorId, string? SectorName, bool IsActive, int Version);
}
