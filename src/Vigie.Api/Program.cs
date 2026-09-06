using System.IdentityModel.Tokens.Jwt;
using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Vigie.Api.Auth;
using Vigie.Api.Contracts;
using Vigie.Application;
using Vigie.Application.Auth;
using Vigie.Domain;
using Vigie.Infrastructure;
using Vigie.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) && builder.Environment.IsDevelopment())
    jwtKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException("La configuration Jwt:Key est obligatoire hors de l'environnement Development.");

builder.Services.AddVigiePersistence(builder.Configuration);
builder.Services.AddSingleton<IClock, SystemClock>();
var accessTokenMinutes = builder.Configuration.GetValue("Jwt:AccessTokenMinutes", 60);
if (accessTokenMinutes is < 15 or > 240) throw new InvalidOperationException("Jwt:AccessTokenMinutes doit être compris entre 15 et 240.");
builder.Services.AddSingleton(new JwtTokenService(jwtKey, TimeSpan.FromMinutes(accessTokenMinutes)));
builder.Services.AddScoped<AssignShiftService>();
builder.Services.AddScoped<RequestSwapService>();
builder.Services.AddScoped<ApproveSwapService>();
builder.Services.AddScoped<RejectSwapService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        NameClaimType = ClaimTypes.Name,
        RoleClaimType = ClaimTypes.Role,
        ClockSkew = TimeSpan.FromSeconds(30)
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var principal = context.Principal;
            var versionClaim = principal?.FindFirstValue("session_version");
            if (!int.TryParse(versionClaim, NumberStyles.None, CultureInfo.InvariantCulture, out var tokenVersion)) return;

            var subject = principal?.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(subject, out var employeeId))
            {
                context.Fail("La session est invalide.");
                return;
            }

            var employeeRepository = context.HttpContext.RequestServices.GetRequiredService<IEmployeeRepository>();
            var employee = await employeeRepository.GetAsync(employeeId, context.HttpContext.RequestAborted);
            if (employee is null || employee.SessionVersion != tokenVersion)
                context.Fail("La session a été révoquée.");
        }
    };
});
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    var permitLimit = builder.Environment.IsProduction() ? 10 : 100;
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").GetChildren()
    .Select(section => section.Value)
    .Where(value => !string.IsNullOrWhiteSpace(value))
    .Select(value => value!.Trim())
    .ToArray();
var allowedOriginsCsv = builder.Configuration["AllowedOrigins"];
if (!string.IsNullOrWhiteSpace(allowedOriginsCsv))
{
    allowedOrigins = allowedOriginsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    if (allowedOrigins.Length > 0)
    {
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
        return;
    }

    if (builder.Environment.IsDevelopment())
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        return;
    }

    throw new InvalidOperationException("La configuration AllowedOrigins est obligatoire hors de l'environnement Development.");
}));
builder.Services.AddOpenApi();

var app = builder.Build();
if (!string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("Vigie")))
{
    using (var scope = app.Services.CreateScope())
    {
        await VigieDatabaseInitializer.InitializeAsync(scope.ServiceProvider);
    }
}

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapOpenApi();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "vigie-api" }));

app.MapGet("/api/v1/notifications", (ClaimsPrincipal user, IVigieStore store) =>
{
    var scope = OrganizationScopeResolver.Resolve(user, store);
    if (scope is null) return Problem("SESSION_INVALID", "La session n'est plus valide.", StatusCodes.Status401Unauthorized);
    var notifications = store.Notifications
        .Where(notification => notification.OrganizationId == scope.OrganizationId && notification.RecipientEmployeeId == scope.EmployeeId)
        .OrderByDescending(notification => notification.CreatedAtUtc)
        .Take(50)
        .Select(ToNotification)
        .ToArray();
    return Results.Ok(notifications);
}).RequireAuthorization().WithTags("Notifications");

app.MapPost("/api/v1/notifications/{notificationId:guid}/read", async (ClaimsPrincipal user, Guid notificationId, IVigieStore store, IUnitOfWork unitOfWork, CancellationToken ct) =>
{
    var scope = OrganizationScopeResolver.Resolve(user, store);
    if (scope is null) return Problem("SESSION_INVALID", "La session n'est plus valide.", StatusCodes.Status401Unauthorized);
    var notification = store.Notifications.SingleOrDefault(item => item.Id == notificationId && item.OrganizationId == scope.OrganizationId);
    if (notification is null) return Problem("NOT_FOUND", "La notification est introuvable.", StatusCodes.Status404NotFound);
    if (notification.RecipientEmployeeId != scope.EmployeeId) return Results.StatusCode(StatusCodes.Status403Forbidden);
    notification.MarkRead(DateTimeOffset.UtcNow);
    store.UpdateNotification(notification);
    await unitOfWork.SaveChangesAsync(ct);
    return Results.Ok(ToNotification(notification));
}).RequireAuthorization().WithTags("Notifications");

app.MapPost("/api/v1/auth/login", (LoginRequest request, IVigieStore store, JwtTokenService tokens) =>
{
    if (request is null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        return Problem("INVALID_CREDENTIALS", "Le courriel ou le mot de passe est invalide.", StatusCodes.Status401Unauthorized);
    var employee = store.Employees.SingleOrDefault(e => string.Equals(e.Email, request.Email.Trim(), StringComparison.OrdinalIgnoreCase));
    if (employee is null || !PasswordHasher.Verify(request.Password, employee.PasswordHash)) return Problem("INVALID_CREDENTIALS", "Le courriel ou le mot de passe est invalide.", StatusCodes.Status401Unauthorized);
    var membership = PrimaryMembership(employee, store);
    var (token, expires) = tokens.Create(employee, membership);
    return Results.Ok(new LoginResponse(token, expires, User(employee, store)));
}).AllowAnonymous().RequireRateLimiting("auth").WithTags("Authentification");

app.MapGet("/api/v1/auth/me", (ClaimsPrincipal user, IVigieStore store) =>
{
    var employee = store.Employees.SingleOrDefault(item => item.Id == UserId(user) && item.OrganizationId == OrganizationId(user));
    return employee is null
        ? Problem("SESSION_INVALID", "La session n'est plus valide.", StatusCodes.Status401Unauthorized)
        : Results.Ok(User(employee, store));
}).RequireAuthorization().WithTags("Authentification");

app.MapPost("/api/v1/auth/change-password", async (ClaimsPrincipal user, ChangePasswordRequest request, IVigieStore store, JwtTokenService tokens, IUnitOfWork unitOfWork, CancellationToken ct) =>
{
    if (request is null || string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
        return Problem("INVALID_PASSWORD_CHANGE", "Les deux mots de passe sont obligatoires.");
    if (!PasswordPolicy.IsStrong(request.NewPassword))
        return Problem("WEAK_PASSWORD", "Le nouveau mot de passe doit contenir au moins 12 caractères, une majuscule, une minuscule et un chiffre.");

    var employee = await ((IEmployeeRepository)store).GetAsync(UserId(user), ct);
    if (employee is null || employee.OrganizationId != OrganizationId(user))
        return Problem("SESSION_INVALID", "La session n'est plus valide.", StatusCodes.Status401Unauthorized);
    if (!PasswordHasher.Verify(request.CurrentPassword, employee.PasswordHash))
        return Problem("CURRENT_PASSWORD_INVALID", "Le mot de passe actuel est invalide.", StatusCodes.Status400BadRequest);

    employee.SetPasswordHash(PasswordHasher.Hash(request.NewPassword));
    employee.RevokeSessions();
    store.AddAuditEntry(Audit(OrganizationId(user), employee.Id, "account.password_changed", "Employee", employee.Id));
    await unitOfWork.SaveChangesAsync(ct);
    var membership = PrimaryMembership(employee, store);
    var (token, expires) = tokens.Create(employee, membership);
    return Results.Ok(new LoginResponse(token, expires, User(employee, store)));
}).RequireAuthorization().RequireRateLimiting("auth").WithTags("Authentification");

app.MapPost("/api/v1/auth/register", async (RegisterOrganizationRequest request, IVigieStore store, IUnitOfWork unitOfWork, JwtTokenService tokens, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.OrganizationName) || string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email))
        return Problem("INVALID_REGISTRATION", "Le nom de l'organisation, votre nom et le courriel sont obligatoires.");
    if (!PasswordPolicy.IsStrong(request.Password))
        return Problem("WEAK_PASSWORD", "Le mot de passe doit contenir au moins 12 caractères, une majuscule, une minuscule et un chiffre.");

    var email = request.Email.Trim().ToLowerInvariant();
    if (store.Employees.Any(employee => string.Equals(employee.Email, email, StringComparison.OrdinalIgnoreCase)))
        return Problem("ACCOUNT_EXISTS", "Impossible de créer ce compte avec ces informations.", StatusCodes.Status409Conflict);

    var slug = Slugify(request.OrganizationName);
    if (store.Organizations.Any(organization => string.Equals(organization.Slug, slug, StringComparison.OrdinalIgnoreCase)))
        return Problem("ORGANIZATION_EXISTS", "Une organisation utilise déjà ce nom. Choisissez un autre nom.", StatusCodes.Status409Conflict);

    try
    {
        var organization = Organization.Create(Guid.NewGuid(), request.OrganizationName, slug);
        var employee = Employee.Create(Guid.NewGuid(), request.Name, email, EmployeeRole.Coordinator, 40, organization.Id);
        employee.SetPasswordHash(PasswordHasher.Hash(request.Password));
        var membership = OrganizationMembership.Create(Guid.NewGuid(), employee.Id, organization.Id, EmployeeRole.AquaticDirector, null, null);
        store.AddOrganization(organization);
        store.AddEmployee(employee);
        store.AddMembership(membership);
        store.AddAuditEntry(Audit(organization.Id, employee.Id, "organization.created", "Organization", organization.Id));
        await unitOfWork.SaveChangesAsync(ct);
        var (token, expires) = tokens.Create(employee, membership);
        return Results.Created($"/api/v1/organizations/{organization.Id}", new RegistrationResponse(new LoginResponse(token, expires, User(employee, store)), ToOrganization(organization)));
    }
    catch (DomainException ex) { return Problem("INVALID_REGISTRATION", ex.Message); }
}).AllowAnonymous().RequireRateLimiting("auth").WithTags("Authentification");

app.MapGet("/api/v1/organization", (ClaimsPrincipal user, IVigieStore store) =>
{
    var organization = store.Organizations.SingleOrDefault(item => item.Id == OrganizationId(user));
    return organization is null ? Problem("NOT_FOUND", "L'organisation est introuvable.", StatusCodes.Status404NotFound) : Results.Ok(ToOrganization(organization));
}).RequireAuthorization().WithTags("Organisation");

app.MapGet("/api/v1/sectors", (ClaimsPrincipal user, IVigieStore store) =>
{
    var scope = OrganizationScopeResolver.Resolve(user, store);
    if (scope is null) return Problem("SESSION_INVALID", "La session n'est plus valide.", StatusCodes.Status401Unauthorized);
    var visible = store.Sectors
        .Where(sector => sector.OrganizationId == scope.OrganizationId)
        .Where(sector => sector.IsActive)
        .Where(sector => scope.IsDirector || scope.SectorId == sector.Id || scope.SiteId.HasValue && store.Sites.Any(site => site.Id == scope.SiteId && site.SectorId == sector.Id))
        .OrderBy(sector => sector.Name)
        .Select(ToSector)
        .ToArray();
    return Results.Ok(visible);
}).RequireAuthorization().WithTags("Secteurs");

app.MapPost("/api/v1/sectors", async (ClaimsPrincipal user, CreateSectorRequest request, IVigieStore store, IUnitOfWork unitOfWork, CancellationToken ct) =>
{
    var scope = OrganizationScopeResolver.Resolve(user, store);
    if (!OrganizationScopeResolver.CanManageOrganization(scope)) return Results.StatusCode(StatusCodes.Status403Forbidden);
    if (request is null || string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Code))
        return Problem("INVALID_SECTOR", "Le nom et le code du secteur sont obligatoires.");
    var organizationId = scope!.OrganizationId;
    if (store.Sectors.Any(sector => sector.OrganizationId == organizationId && (string.Equals(sector.Code, request.Code.Trim(), StringComparison.OrdinalIgnoreCase) || string.Equals(sector.Name, request.Name.Trim(), StringComparison.OrdinalIgnoreCase))))
        return Problem("SECTOR_EXISTS", "Un secteur utilise déjà ce nom ou ce code.", StatusCodes.Status409Conflict);
    try
    {
        var sector = Sector.Create(Guid.NewGuid(), organizationId, request.Name, request.Code);
        store.AddSector(sector);
        store.AddAuditEntry(Audit(organizationId, scope.EmployeeId, "sector.created", "Sector", sector.Id, $"code={sector.Code}"));
        await unitOfWork.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/sectors/{sector.Id}", ToSector(sector));
    }
    catch (DomainException ex) { return Problem("INVALID_SECTOR", ex.Message); }
}).RequireAuthorization().WithTags("Secteurs");

app.MapPatch("/api/v1/sectors/{sectorId:guid}", async (ClaimsPrincipal user, Guid sectorId, UpdateSectorRequest request, IVigieStore store, IUnitOfWork unitOfWork, CancellationToken ct) =>
{
    var scope = OrganizationScopeResolver.Resolve(user, store);
    if (!OrganizationScopeResolver.CanManageOrganization(scope)) return Results.StatusCode(StatusCodes.Status403Forbidden);
    var access = scope!;
    var sector = store.Sectors.SingleOrDefault(item => item.Id == sectorId && item.OrganizationId == access.OrganizationId);
    if (sector is null) return Problem("NOT_FOUND", "Le secteur est introuvable.", StatusCodes.Status404NotFound);
    if (store.Sectors.Any(item => item.Id != sectorId && item.OrganizationId == access.OrganizationId && (string.Equals(item.Code, request.Code.Trim(), StringComparison.OrdinalIgnoreCase) || string.Equals(item.Name, request.Name.Trim(), StringComparison.OrdinalIgnoreCase))))
        return Problem("SECTOR_EXISTS", "Un secteur utilise déjà ce nom ou ce code.", StatusCodes.Status409Conflict);
    try
    {
        sector.Rename(request.Name, request.Code);
        if (request.IsActive) sector.Activate(); else sector.Deactivate();
        store.UpdateSector(sector);
        store.AddAuditEntry(Audit(access.OrganizationId, access.EmployeeId, "sector.updated", "Sector", sector.Id));
        await unitOfWork.SaveChangesAsync(ct);
        return Results.Ok(ToSector(sector));
    }
    catch (DomainException ex) { return Problem("INVALID_SECTOR", ex.Message); }
}).RequireAuthorization().WithTags("Secteurs");

app.MapGet("/api/v1/members", (ClaimsPrincipal user, IVigieStore store, Guid? siteId, Guid? sectorId, string? role) =>
{
    var scope = OrganizationScopeResolver.Resolve(user, store);
    if (scope is null) return Problem("SESSION_INVALID", "La session n'est plus valide.", StatusCodes.Status401Unauthorized);
    var query = store.Memberships.Where(membership => membership.OrganizationId == scope.OrganizationId && membership.IsActive);
    query = scope.Role switch
    {
        EmployeeRole.AquaticDirector => query,
        EmployeeRole.SectorManager => query.Where(membership => membership.SectorId == scope.SectorId),
        EmployeeRole.PoolChief or EmployeeRole.Coordinator => query.Where(membership => membership.SiteId == scope.SiteId),
        _ => query.Where(membership => membership.EmployeeId == scope.EmployeeId)
    };
    if (siteId.HasValue) query = query.Where(membership => membership.SiteId == siteId);
    if (sectorId.HasValue) query = query.Where(membership => membership.SectorId == sectorId);
    if (!string.IsNullOrWhiteSpace(role) && Enum.TryParse<EmployeeRole>(role, true, out var parsedRole)) query = query.Where(membership => membership.Role == parsedRole);
    var result = query.OrderBy(membership => membership.Role).ThenBy(membership => membership.EmployeeId).Select(membership => ToMembership(membership, store)).ToArray();
    return Results.Ok(result);
}).RequireAuthorization().WithTags("Membres");

app.MapPost("/api/v1/memberships", async (ClaimsPrincipal user, CreateMembershipRequest request, IVigieStore store, IUnitOfWork unitOfWork, CancellationToken ct) =>
{
    var scope = OrganizationScopeResolver.Resolve(user, store);
    if (scope is null) return Problem("SESSION_INVALID", "La session n'est plus valide.", StatusCodes.Status401Unauthorized);
    if (!Enum.TryParse<EmployeeRole>(request.Role, true, out var role) || role == EmployeeRole.Coordinator)
        return Problem("INVALID_ROLE", "Le rôle du membership est invalide.");
    var employee = store.Employees.SingleOrDefault(item => item.Id == request.EmployeeId && item.OrganizationId == scope.OrganizationId);
    var site = request.SiteId.HasValue ? store.Sites.SingleOrDefault(item => item.Id == request.SiteId && item.OrganizationId == scope.OrganizationId) : null;
    var sector = request.SectorId.HasValue ? store.Sectors.SingleOrDefault(item => item.Id == request.SectorId && item.OrganizationId == scope.OrganizationId) : null;
    if (employee is null || request.SiteId.HasValue && site is null || request.SectorId.HasValue && sector is null)
        return Problem("NOT_FOUND", "L'employé, le site ou le secteur est introuvable.", StatusCodes.Status404NotFound);
    if (!HasValidRoleScope(role, site, sector))
        return Problem("INVALID_SCOPE", "Le rôle et la portée du membership sont incompatibles.");
    if (site is not null && sector is not null && site.SectorId != sector.Id)
        return Problem("INVALID_SCOPE", "La piscine et le secteur du membership ne correspondent pas.");
    var targetSiteAllowed = site is not null && OrganizationScopeResolver.CanManageSite(scope, site, store);
    var targetSectorAllowed = sector is not null && (scope.IsDirector || scope.Role == EmployeeRole.SectorManager && sector.Id == scope.SectorId);
    if (!scope.IsDirector && scope.Role == EmployeeRole.SectorManager && role is not (EmployeeRole.Lifeguard or EmployeeRole.PoolChief))
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    if (!scope.IsDirector && scope.IsPoolChief && role != EmployeeRole.Lifeguard)
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    if (!(scope.IsDirector || targetSiteAllowed || targetSectorAllowed))
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    if (store.Memberships.Any(item => item.EmployeeId == employee.Id && item.OrganizationId == scope.OrganizationId && item.IsActive && item.SiteId == request.SiteId && item.SectorId == request.SectorId))
        return Problem("MEMBERSHIP_EXISTS", "Ce rattachement est déjà actif.", StatusCodes.Status409Conflict);
    try
    {
        var membership = OrganizationMembership.Create(Guid.NewGuid(), employee.Id, scope.OrganizationId, role, request.SiteId, request.SectorId);
        store.AddMembership(membership);
        store.AddAuditEntry(Audit(scope.OrganizationId, scope.EmployeeId, "membership.created", "OrganizationMembership", membership.Id, $"employé={employee.Name};role={role}"));
        await unitOfWork.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/memberships/{membership.Id}", ToMembership(membership, store));
    }
    catch (DomainException ex) { return Problem("INVALID_MEMBERSHIP", ex.Message); }
}).RequireAuthorization().WithTags("Membres");

app.MapPatch("/api/v1/memberships/{membershipId:guid}", async (ClaimsPrincipal user, Guid membershipId, UpdateMembershipRequest request, IVigieStore store, IUnitOfWork unitOfWork, CancellationToken ct) =>
{
    var scope = OrganizationScopeResolver.Resolve(user, store);
    if (scope is null) return Problem("SESSION_INVALID", "La session n'est plus valide.", StatusCodes.Status401Unauthorized);
    var membership = store.Memberships.SingleOrDefault(item => item.Id == membershipId && item.OrganizationId == scope.OrganizationId);
    if (membership is null) return Problem("NOT_FOUND", "Le membership est introuvable.", StatusCodes.Status404NotFound);
    var site = membership.SiteId.HasValue ? store.Sites.SingleOrDefault(item => item.Id == membership.SiteId && item.OrganizationId == scope.OrganizationId) : null;
    if (!(scope.IsDirector || site is not null && OrganizationScopeResolver.CanManageSite(scope, site, store) || scope.IsSectorManager && membership.SectorId == scope.SectorId))
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    if (request.ExpectedVersion.HasValue && request.ExpectedVersion != membership.Version)
        return Problem("CONFLICT", "Le membership a été modifié par une autre personne. Rechargez la page.", StatusCodes.Status409Conflict);
    try
    {
        var role = string.IsNullOrWhiteSpace(request.Role) ? membership.Role : Enum.TryParse<EmployeeRole>(request.Role, true, out var parsed) ? parsed : throw new DomainException("Le rôle du membership est invalide.");
        if (!scope.IsDirector && (role is EmployeeRole.AquaticDirector or EmployeeRole.SectorManager || scope.IsPoolChief && role != EmployeeRole.Lifeguard))
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        var nextSite = role is EmployeeRole.AquaticDirector or EmployeeRole.SectorManager ? null : request.SiteId ?? membership.SiteId;
        var nextSector = role is EmployeeRole.AquaticDirector or EmployeeRole.PoolChief or EmployeeRole.Lifeguard ? null : request.SectorId ?? membership.SectorId;
        var nextSiteEntity = nextSite.HasValue ? store.Sites.SingleOrDefault(item => item.Id == nextSite && item.OrganizationId == scope.OrganizationId) : null;
        var nextSectorEntity = nextSector.HasValue ? store.Sectors.SingleOrDefault(item => item.Id == nextSector && item.OrganizationId == scope.OrganizationId) : null;
        if (nextSite.HasValue && nextSiteEntity is null || nextSector.HasValue && nextSectorEntity is null)
            return Problem("NOT_FOUND", "La piscine ou le secteur du membership est introuvable.", StatusCodes.Status404NotFound);
        if (!HasValidRoleScope(role, nextSiteEntity, nextSectorEntity) || nextSiteEntity is not null && nextSectorEntity is not null && nextSiteEntity.SectorId != nextSectorEntity.Id)
            return Problem("INVALID_SCOPE", "Le rôle et la portée du membership sont incompatibles.");
        if (!scope.IsDirector && scope.Role == EmployeeRole.SectorManager && role is (EmployeeRole.Lifeguard or EmployeeRole.PoolChief) && (nextSiteEntity is null || nextSiteEntity.SectorId != scope.SectorId))
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        if (!scope.IsDirector && scope.IsPoolChief && (role != EmployeeRole.Lifeguard || nextSite != scope.SiteId))
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        membership.ChangeRoleAndScope(role, nextSite, nextSector);
        if (request.IsActive.HasValue)
        {
            if (request.IsActive.Value) membership.Activate(); else membership.Deactivate();
        }
        store.UpdateMembership(membership);
        store.AddAuditEntry(Audit(scope.OrganizationId, scope.EmployeeId, "membership.updated", "OrganizationMembership", membership.Id));
        await unitOfWork.SaveChangesAsync(ct);
        return Results.Ok(ToMembership(membership, store));
    }
    catch (DomainException ex) { return Problem("INVALID_MEMBERSHIP", ex.Message); }
}).RequireAuthorization().WithTags("Membres");

app.MapDelete("/api/v1/memberships/{membershipId:guid}", async (ClaimsPrincipal user, Guid membershipId, IVigieStore store, IUnitOfWork unitOfWork, CancellationToken ct) =>
{
    var scope = OrganizationScopeResolver.Resolve(user, store);
    if (scope is null) return Problem("SESSION_INVALID", "La session n'est plus valide.", StatusCodes.Status401Unauthorized);
    var membership = store.Memberships.SingleOrDefault(item => item.Id == membershipId && item.OrganizationId == scope.OrganizationId);
    if (membership is null) return Problem("NOT_FOUND", "Le membership est introuvable.", StatusCodes.Status404NotFound);
    var site = membership.SiteId.HasValue ? store.Sites.SingleOrDefault(item => item.Id == membership.SiteId) : null;
    if (!(scope.IsDirector || site is not null && OrganizationScopeResolver.CanManageSite(scope, site, store) || scope.IsSectorManager && membership.SectorId == scope.SectorId))
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    membership.Deactivate();
    store.UpdateMembership(membership);
    store.AddAuditEntry(Audit(scope.OrganizationId, scope.EmployeeId, "membership.deactivated", "OrganizationMembership", membership.Id));
    await unitOfWork.SaveChangesAsync(ct);
    return Results.NoContent();
}).RequireAuthorization().WithTags("Membres");

app.MapGet("/api/v1/audit", (ClaimsPrincipal user, int? limit, IVigieStore store) =>
{
    var take = Math.Clamp(limit ?? 50, 1, 100);
    var scope = OrganizationScopeResolver.Resolve(user, store);
    if (scope is null) return Problem("SESSION_INVALID", "La session n'est plus valide.", StatusCodes.Status401Unauthorized);
    var result = store.AuditEntries
        .Where(entry => entry.OrganizationId == scope.OrganizationId && IsAuditVisible(entry, scope, store))
        .OrderByDescending(entry => entry.CreatedAtUtc)
        .Take(take)
        .Select(entry => new AuditEntryResponse(entry.Id, entry.Action, entry.EntityType, entry.EntityId, entry.Details, entry.ActorId.HasValue ? store.Employees.FirstOrDefault(employee => employee.Id == entry.ActorId.Value)?.Name : null, entry.CreatedAtUtc))
        .ToArray();
    return Results.Ok(result);
}).RequireAuthorization(new AuthorizeAttribute { Roles = nameof(EmployeeRole.Coordinator) }).WithTags("Audit");

app.MapGet("/api/v1/audit/export", (ClaimsPrincipal user, IVigieStore store) =>
{
    var scope = OrganizationScopeResolver.Resolve(user, store);
    if (scope is null) return Problem("SESSION_INVALID", "La session n'est plus valide.", StatusCodes.Status401Unauthorized);
    var rows = store.AuditEntries
        .Where(entry => entry.OrganizationId == scope.OrganizationId && IsAuditVisible(entry, scope, store))
        .OrderByDescending(entry => entry.CreatedAtUtc)
        .Select(entry => new
        {
            entry.CreatedAtUtc,
            entry.Action,
            entry.EntityType,
            Actor = entry.ActorId.HasValue ? store.Employees.FirstOrDefault(employee => employee.Id == entry.ActorId.Value)?.Name : null,
            entry.Details
        })
        .ToArray();
    var csv = new StringBuilder("Date (UTC);Action;Objet;Acteur;Détails\r\n");
    foreach (var row in rows)
        csv.AppendLine(string.Join(';', Csv(row.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture)), Csv(row.Action), Csv(row.EntityType), Csv(row.Actor), Csv(row.Details)));
    return Results.File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(), "text/csv; charset=utf-8", "vigie-historique.csv");
}).RequireAuthorization(new AuthorizeAttribute { Roles = nameof(EmployeeRole.Coordinator) }).WithTags("Audit");

app.MapGet("/api/v1/invitations", (ClaimsPrincipal user, IVigieStore store) =>
{
    var scope = OrganizationScopeResolver.Resolve(user, store);
    if (scope is null) return Problem("SESSION_INVALID", "La session n'est plus valide.", StatusCodes.Status401Unauthorized);
    var visible = store.Invitations
        .Where(invitation => invitation.OrganizationId == scope.OrganizationId && IsInvitationVisible(invitation, scope, store))
        .OrderByDescending(invitation => invitation.CreatedAtUtc)
        .Select(invitation => ToInvitation(invitation))
        .ToArray();
    return Results.Ok(visible);
}).RequireAuthorization(new AuthorizeAttribute { Roles = nameof(EmployeeRole.Coordinator) }).WithTags("Invitations");

app.MapPost("/api/v1/invitations", async (ClaimsPrincipal user, InviteMemberRequest request, IVigieStore store, IUnitOfWork unitOfWork, IConfiguration configuration, CancellationToken ct) =>
{
    if (request is null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Name))
        return Problem("INVALID_INVITATION", "Le nom et le courriel du membre sont obligatoires.");
    if (!Enum.TryParse<EmployeeRole>(request.Role, true, out var role)) return Problem("INVALID_ROLE", "Le rôle de l'invitation est invalide.");
    var scope = OrganizationScopeResolver.Resolve(user, store);
    if (scope is null) return Problem("SESSION_INVALID", "La session n'est plus valide.", StatusCodes.Status401Unauthorized);
    var organizationId = OrganizationId(user);
    var requestedSite = request.SiteId.HasValue ? store.Sites.SingleOrDefault(site => site.Id == request.SiteId && site.OrganizationId == organizationId) : null;
    var requestedSector = request.SectorId.HasValue ? store.Sectors.SingleOrDefault(sector => sector.Id == request.SectorId && sector.OrganizationId == organizationId) : null;
    if (request.SiteId.HasValue && requestedSite is null || request.SectorId.HasValue && requestedSector is null)
        return Problem("NOT_FOUND", "Le site ou le secteur de l'invitation est introuvable.", StatusCodes.Status404NotFound);
    if (!HasValidRoleScope(role, requestedSite, requestedSector))
        return Problem("INVALID_SCOPE", "Le rôle doit être associé à une portée valide : une piscine pour un sauveteur ou un chef, un secteur pour un chargé de secteur, et aucune portée pour la Régie aquatique.");
    if (requestedSite is not null && requestedSector is not null && requestedSite.SectorId != requestedSector.Id)
        return Problem("INVALID_SCOPE", "La piscine et le secteur de l'invitation ne correspondent pas.");
    var invitationScopeAllowed = role == EmployeeRole.AquaticDirector
        ? scope.IsDirector
        : role == EmployeeRole.SectorManager
            ? scope.IsDirector || scope.Role == EmployeeRole.SectorManager && requestedSector?.Id == scope.SectorId
            : role is EmployeeRole.Lifeguard or EmployeeRole.PoolChief
                ? requestedSite is not null && OrganizationScopeResolver.CanManageSite(scope, requestedSite, store)
                : scope.IsDirector || scope.Role == EmployeeRole.Coordinator;
    if (!scope.IsDirector && scope.Role == EmployeeRole.SectorManager && role is not (EmployeeRole.Lifeguard or EmployeeRole.PoolChief))
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    if (!scope.IsDirector && scope.IsPoolChief && role != EmployeeRole.Lifeguard)
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    if (!invitationScopeAllowed) return Results.StatusCode(StatusCodes.Status403Forbidden);
    var email = request.Email.Trim().ToLowerInvariant();
    if (store.Employees.Any(employee => string.Equals(employee.Email, email, StringComparison.OrdinalIgnoreCase)) ||
        store.Invitations.Any(invitation => invitation.OrganizationId == organizationId && string.Equals(invitation.Email, email, StringComparison.OrdinalIgnoreCase) && invitation.IsPending(DateTimeOffset.UtcNow)))
        return Problem("INVITATION_EXISTS", "Un compte ou une invitation existe déjà pour ce courriel.", StatusCodes.Status409Conflict);
    try
    {
        var (token, tokenHash) = InvitationToken.Create();
        var invitation = Invitation.Create(Guid.NewGuid(), organizationId, email, request.Name, role, tokenHash, DateTimeOffset.UtcNow, TimeSpan.FromDays(7), request.SiteId, request.SectorId);
        store.AddInvitation(invitation);
        store.AddAuditEntry(Audit(organizationId, UserId(user), "invitation.created", "Invitation", invitation.Id, $"role={role}"));
        await unitOfWork.SaveChangesAsync(ct);
        var publicAppUrl = configuration["PublicAppUrl"]?.TrimEnd('/');
        var link = string.IsNullOrWhiteSpace(publicAppUrl) ? null : $"{publicAppUrl}/?invitation={Uri.EscapeDataString(token)}";
        return Results.Created($"/api/v1/invitations/{invitation.Id}", ToInvitation(invitation, token, link));
    }
    catch (DomainException ex) { return Problem("INVALID_INVITATION", ex.Message); }
}).RequireAuthorization(new AuthorizeAttribute { Roles = nameof(EmployeeRole.Coordinator) }).WithTags("Invitations");

app.MapPost("/api/v1/invitations/accept", async (AcceptInvitationRequest request, IVigieStore store, IUnitOfWork unitOfWork, JwtTokenService tokens, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Token)) return Problem("INVALID_INVITATION", "Le jeton d'invitation est obligatoire.");
    if (!PasswordPolicy.IsStrong(request.Password)) return Problem("WEAK_PASSWORD", "Le mot de passe doit contenir au moins 12 caractères, une majuscule, une minuscule et un chiffre.");
    var invitation = store.Invitations.SingleOrDefault(item => item.TokenHash == InvitationToken.Hash(request.Token));
    if (invitation is null || !invitation.IsPending(DateTimeOffset.UtcNow)) return Problem("INVALID_INVITATION", "Cette invitation est expirée ou déjà utilisée.", StatusCodes.Status400BadRequest);
    if (store.Employees.Any(employee => string.Equals(employee.Email, invitation.Email, StringComparison.OrdinalIgnoreCase))) return Problem("ACCOUNT_EXISTS", "Impossible d'activer cette invitation.", StatusCodes.Status409Conflict);
    var invitationSite = invitation.SiteId.HasValue ? store.Sites.SingleOrDefault(site => site.Id == invitation.SiteId && site.OrganizationId == invitation.OrganizationId) : null;
    var invitationSector = invitation.SectorId.HasValue ? store.Sectors.SingleOrDefault(sector => sector.Id == invitation.SectorId && sector.OrganizationId == invitation.OrganizationId) : null;
    if (!HasValidRoleScope(invitation.Role, invitationSite, invitationSector) || invitationSite is not null && invitationSector is not null && invitationSite.SectorId != invitationSector.Id)
        return Problem("INVALID_INVITATION", "La portée de cette invitation est invalide. Demandez à un responsable de créer une nouvelle invitation.", StatusCodes.Status400BadRequest);
    try
    {
        var name = string.IsNullOrWhiteSpace(request.Name) ? invitation.Name : request.Name;
        var employeeRole = invitation.Role;
        var employee = Employee.Create(Guid.NewGuid(), name, invitation.Email, employeeRole, invitation.Role is EmployeeRole.Coordinator or EmployeeRole.PoolChief or EmployeeRole.SectorManager or EmployeeRole.AquaticDirector ? 40 : 24, invitation.OrganizationId);
        employee.SetPasswordHash(PasswordHasher.Hash(request.Password));
        var membershipRole = invitation.Role == EmployeeRole.Coordinator ? EmployeeRole.Coordinator : invitation.Role;
        OrganizationMembership? newMembership = null;
        newMembership = OrganizationMembership.Create(Guid.NewGuid(), employee.Id, invitation.OrganizationId, membershipRole, invitation.SiteId, invitation.SectorId);
        invitation.Accept(DateTimeOffset.UtcNow);
        store.AddEmployee(employee);
        if (newMembership is not null) store.AddMembership(newMembership);
        store.UpdateInvitation(invitation);
        store.AddAuditEntry(Audit(invitation.OrganizationId, employee.Id, "member.joined", "Employee", employee.Id, $"role={employee.Role}"));
        await unitOfWork.SaveChangesAsync(ct);
        var membership = PrimaryMembership(employee, store);
        var (token, expires) = tokens.Create(employee, membership);
        return Results.Ok(new LoginResponse(token, expires, User(employee, store)));
    }
    catch (DomainException ex) { return Problem("INVALID_INVITATION", ex.Message); }
}).AllowAnonymous().RequireRateLimiting("auth").WithTags("Invitations");

app.MapGet("/api/v1/dashboard", (ClaimsPrincipal user, IVigieStore store) =>
{
    var employeeId = UserId(user);
    var now = DateTimeOffset.UtcNow;
    var warnings = store.Certifications.Where(c => c.EmployeeId == employeeId)
        .Select(c => (Certification: c, Type: store.CertificationTypes.Single(t => t.Id == c.CertificationTypeId)))
        .Select(x => new CertificationResponse(x.Certification.Id, employeeId, store.Employees.Single(e => e.Id == employeeId).Name, x.Type.Name, x.Certification.ExpiresOn, x.Certification.ExpiresOn.DayNumber - DateOnly.FromDateTime(now.UtcDateTime).DayNumber))
        .Where(x => x.DaysRemaining is >= 0 and <= 90).OrderBy(x => x.ExpiresOn).ToArray();
    var upcoming = store.Assignments.Count(a => a.EmployeeId == employeeId && store.Shifts.Single(s => s.Id == a.ShiftId).StartUtc >= now);
    var pending = store.SwapRequests.Count(r => r.Status == SwapStatus.Pending && (store.Assignments.Single(a => a.Id == r.AssignmentId).EmployeeId == employeeId || r.ReceiverId == employeeId));
    return Results.Ok(new DashboardResponse(upcoming, pending, warnings.Length, warnings));
}).RequireAuthorization().WithTags("Tableau de bord");

app.MapGet("/api/v1/sites", (ClaimsPrincipal user, IVigieStore store) =>
{
    var scope = OrganizationScopeResolver.Resolve(user, store);
    if (scope is null) return Problem("SESSION_INVALID", "La session n'est plus valide.", StatusCodes.Status401Unauthorized);
    var visible = store.Sites
        .Where(site => site.OrganizationId == scope.OrganizationId)
        .Where(site => IsSiteVisible(site, scope, store))
        .OrderBy(site => site.Name)
        .Select(site => ToSite(site, store))
        .ToArray();
    return Results.Ok(visible);
}).RequireAuthorization().WithTags("Sites");
app.MapPost("/api/v1/sites", async (ClaimsPrincipal user, CreateSiteRequest request, IVigieStore store, IUnitOfWork unitOfWork, CancellationToken ct) =>
{
    var scope = OrganizationScopeResolver.Resolve(user, store);
    if (!OrganizationScopeResolver.CanManageOrganization(scope)) return Results.StatusCode(StatusCodes.Status403Forbidden);
    if (!Enum.TryParse<SiteType>(request.Type, true, out var type)) return Problem("INVALID_SITE_TYPE", "Le type de site est invalide.");
    try
    {
        var site = Site.Create(Guid.NewGuid(), request.Name, request.TimeZoneId, new OpeningSeason(request.StartMonth, request.StartDay, request.EndMonth, request.EndDay), type, OrganizationId(user), request.Address, request.Neighborhood, request.IsMunicipal);
        store.AddSite(site);
        store.AddAuditEntry(Audit(OrganizationId(user), UserId(user), "site.created", "Site", site.Id));
        await unitOfWork.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/sites/{site.Id}", ToSite(site, store));
    }
    catch (DomainException ex) { return Problem("INVALID_SITE", ex.Message); }
}).RequireAuthorization().WithTags("Sites");

app.MapGet("/api/v1/shifts", (ClaimsPrincipal user, DateTimeOffset? from, DateTimeOffset? to, IVigieStore store) =>
{
    var start = from ?? DateTimeOffset.UtcNow.AddDays(-1);
    var end = to ?? DateTimeOffset.UtcNow.AddDays(14);
    var organizationId = OrganizationId(user);
    var scope = OrganizationScopeResolver.Resolve(user, store);
    if (scope is null) return Results.Unauthorized();
    var result = store.Shifts
        .Where(s => s.StartUtc < end && s.EndUtc > start)
        .Where(s => store.Sites.Any(site => site.Id == s.SiteId && IsSiteVisible(site, scope, store)))
        .OrderBy(s => s.StartUtc).Select(s => ToShift(s, store)).ToArray();
    return Results.Ok(result);
}).RequireAuthorization().WithTags("Quarts");

app.MapPost("/api/v1/shifts", async (ClaimsPrincipal user, CreateShiftRequest request, IVigieStore store, IUnitOfWork unitOfWork, CancellationToken ct) =>
{
    try
    {
        var site = store.Sites.SingleOrDefault(s => s.Id == request.SiteId);
        if (site is null || site.OrganizationId != OrganizationId(user)) return Problem("NOT_FOUND", "Le site du quart est introuvable.", StatusCodes.Status404NotFound);
        var scope = OrganizationScopeResolver.Resolve(user, store);
        if (!OrganizationScopeResolver.CanManageSite(scope, site, store)) return Results.StatusCode(StatusCodes.Status403Forbidden);
        if (!site.IsOpen(request.StartUtc, request.EndUtc)) return Problem("SITE_CLOSED", "Le quart se trouve en dehors de la saison d’ouverture du site.");
        var shift = Shift.Create(Guid.NewGuid(), request.SiteId, request.StartUtc, request.EndUtc, request.RequiredLifeguards);
        store.AddShift(shift);
        store.AddAuditEntry(Audit(OrganizationId(user), UserId(user), "shift.created", "Shift", shift.Id));
        await unitOfWork.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/shifts/{shift.Id}", ToShift(shift, store));
    }
    catch (DomainException ex) { return Problem("INVALID_SHIFT", ex.Message); }
}).RequireAuthorization().WithTags("Quarts");

app.MapPatch("/api/v1/shifts/{shiftId:guid}", async (ClaimsPrincipal user, Guid shiftId, UpdateShiftRequest request, IVigieStore store, IUnitOfWork unitOfWork, CancellationToken ct) =>
{
    var scope = OrganizationScopeResolver.Resolve(user, store);
    var shift = store.Shifts.SingleOrDefault(item => item.Id == shiftId);
    var site = shift is null ? null : store.Sites.SingleOrDefault(item => item.Id == shift.SiteId);
    if (scope is null) return Problem("SESSION_INVALID", "La session n'est plus valide.", StatusCodes.Status401Unauthorized);
    if (shift is null || site is null || site.OrganizationId != scope.OrganizationId)
        return Problem("NOT_FOUND", "Le quart demandé est introuvable.", StatusCodes.Status404NotFound);
    if (!OrganizationScopeResolver.CanManageSite(scope, site, store)) return Results.StatusCode(StatusCodes.Status403Forbidden);
    try
    {
        if (!site.IsOpen(request.StartUtc, request.EndUtc)) return Problem("SITE_CLOSED", "Le quart se trouve en dehors de la saison d’ouverture du site.");
        shift.Reschedule(request.StartUtc, request.EndUtc, request.RequiredLifeguards);
        store.AddAuditEntry(Audit(scope.OrganizationId, scope.EmployeeId, "shift.updated", "Shift", shift.Id));
        await unitOfWork.SaveChangesAsync(ct);
        return Results.Ok(ToShift(shift, store));
    }
    catch (DomainException ex) { return Problem("INVALID_SHIFT", ex.Message); }
}).RequireAuthorization().WithTags("Quarts");

app.MapPost("/api/v1/shifts/{shiftId:guid}/cancel", async (ClaimsPrincipal user, Guid shiftId, IVigieStore store, IUnitOfWork unitOfWork, CancellationToken ct) =>
{
    var scope = OrganizationScopeResolver.Resolve(user, store);
    var shift = store.Shifts.SingleOrDefault(item => item.Id == shiftId);
    var site = shift is null ? null : store.Sites.SingleOrDefault(item => item.Id == shift.SiteId);
    if (scope is null) return Problem("SESSION_INVALID", "La session n'est plus valide.", StatusCodes.Status401Unauthorized);
    if (shift is null || site is null || site.OrganizationId != scope.OrganizationId)
        return Problem("NOT_FOUND", "Le quart demandé est introuvable.", StatusCodes.Status404NotFound);
    if (!OrganizationScopeResolver.CanManageSite(scope, site, store)) return Results.StatusCode(StatusCodes.Status403Forbidden);
    try
    {
        shift.Cancel();
        store.AddAuditEntry(Audit(scope.OrganizationId, scope.EmployeeId, "shift.cancelled", "Shift", shift.Id));
        await unitOfWork.SaveChangesAsync(ct);
        return Results.Ok(ToShift(shift, store));
    }
    catch (DomainException ex) { return Problem("INVALID_SHIFT", ex.Message); }
}).RequireAuthorization().WithTags("Quarts");

app.MapPost("/api/v1/shifts/{shiftId:guid}/assignments", async (ClaimsPrincipal user, Guid shiftId, AssignShiftRequest request, IVigieStore store, AssignShiftService service, CancellationToken ct) =>
{
    var organizationId = OrganizationId(user);
    var shift = store.Shifts.SingleOrDefault(item => item.Id == shiftId);
    var employee = store.Employees.SingleOrDefault(item => item.Id == request.EmployeeId);
    if (shift is null || employee is null || store.Sites.SingleOrDefault(site => site.Id == shift.SiteId)?.OrganizationId != organizationId || employee.OrganizationId != organizationId)
        return Problem("NOT_FOUND", "Le quart ou l'employé est introuvable.", StatusCodes.Status404NotFound);
    var site = store.Sites.Single(item => item.Id == shift.SiteId);
    if (!OrganizationScopeResolver.CanManageSite(OrganizationScopeResolver.Resolve(user, store), site, store)) return Results.StatusCode(StatusCodes.Status403Forbidden);
    var result = await service.ExecuteAsync(request.EmployeeId, shiftId, ct);
    if (!result.IsSuccess) return result.ToHttpResult(assignment => Results.Ok(assignment));
    store.AddAuditEntry(Audit(organizationId, UserId(user), "assignment.created", "Assignment", result.Value!.Id, $"employé={employee.Name}"));
    AddNotification(store, organizationId, employee.Id, "assignment", "Nouveau quart assigné", $"Un quart vous a été assigné à {site.Name} le {shift.StartUtc:ddd d MMM à HH:mm}.", "calendar");
    await ((IUnitOfWork)store).SaveChangesAsync(ct);
    return Results.Ok(result.Value);
}).RequireAuthorization().WithTags("Assignations");

app.MapDelete("/api/v1/assignments/{assignmentId:guid}", async (ClaimsPrincipal user, Guid assignmentId, IVigieStore store, IAssignmentRepository assignments, IUnitOfWork unitOfWork, CancellationToken ct) =>
{
    var assignment = store.Assignments.SingleOrDefault(item => item.Id == assignmentId);
    var shift = assignment is null ? null : store.Shifts.SingleOrDefault(item => item.Id == assignment.ShiftId);
    if (shift is null || store.Sites.SingleOrDefault(site => site.Id == shift.SiteId)?.OrganizationId != OrganizationId(user)) return Problem("NOT_FOUND", "L'assignation est introuvable.", StatusCodes.Status404NotFound);
    var site = store.Sites.Single(item => item.Id == shift.SiteId);
    if (!OrganizationScopeResolver.CanManageSite(OrganizationScopeResolver.Resolve(user, store), site, store)) return Results.StatusCode(StatusCodes.Status403Forbidden);
    await assignments.RemoveAsync(assignmentId, ct); await unitOfWork.SaveChangesAsync(ct); return Results.NoContent();
}).RequireAuthorization().WithTags("Assignations");

app.MapGet("/api/v1/certifications", (ClaimsPrincipal user, IVigieStore store) =>
{
    var scope = OrganizationScopeResolver.Resolve(user, store);
    if (scope is null) return Problem("SESSION_INVALID", "La session n'est plus valide.", StatusCodes.Status401Unauthorized);
    var result = store.Certifications.Where(c => store.Employees.Any(employee => employee.Id == c.EmployeeId && employee.OrganizationId == scope.OrganizationId && IsEmployeeVisible(employee, scope, store))).Select(c =>
    {
        var employee = store.Employees.Single(e => e.Id == c.EmployeeId); var type = store.CertificationTypes.Single(t => t.Id == c.CertificationTypeId);
        return new CertificationResponse(c.Id, c.EmployeeId, employee.Name, type.Name, c.ExpiresOn, c.ExpiresOn.DayNumber - DateOnly.FromDateTime(DateTime.UtcNow).DayNumber);
    }).OrderBy(c => c.ExpiresOn).ToArray();
    return Results.Ok(result);
}).RequireAuthorization().WithTags("Certifications");

app.MapGet("/api/v1/employees", (ClaimsPrincipal user, IVigieStore store) =>
{
    var scope = OrganizationScopeResolver.Resolve(user, store);
    if (scope is null) return Problem("SESSION_INVALID", "La session n'est plus valide.", StatusCodes.Status401Unauthorized);
    var visible = store.Employees
        .Where(employee => employee.OrganizationId == scope.OrganizationId && IsEmployeeVisible(employee, scope, store))
        .OrderBy(employee => employee.Name)
        .Select(employee => User(employee, store))
        .ToArray();
    return Results.Ok(visible);
}).RequireAuthorization().WithTags("Employés");

app.MapPost("/api/v1/swap-requests", async (ClaimsPrincipal user, CreateSwapRequest request, RequestSwapService service, IVigieStore store, CancellationToken ct) =>
{
    var organizationId = OrganizationId(user);
    var assignment = store.Assignments.SingleOrDefault(item => item.Id == request.AssignmentId);
    var shift = assignment is null ? null : store.Shifts.SingleOrDefault(item => item.Id == assignment.ShiftId);
    var receiver = store.Employees.SingleOrDefault(item => item.Id == request.ReceiverId);
    if (shift is null || receiver is null || store.Sites.SingleOrDefault(site => site.Id == shift.SiteId)?.OrganizationId != organizationId || receiver.OrganizationId != organizationId)
        return Problem("NOT_FOUND", "L'assignation ou le receveur est introuvable.", StatusCodes.Status404NotFound);
    var result = await service.ExecuteAsync(UserId(user), request.AssignmentId, request.ReceiverId, ct);
    if (!result.IsSuccess) return result.ToHttpResult(swap => Results.Ok(ToSwap(swap, store)));
    store.AddAuditEntry(Audit(organizationId, UserId(user), "swap.created", "SwapRequest", result.Value!.Id, $"receveur={receiver.Name}"));
    NotifyManagement(store, organizationId, shift, "Échange à traiter", $"Une demande de remplacement pour {shift.StartUtc:ddd d MMM à HH:mm} attend votre approbation.", "swaps");
    await ((IUnitOfWork)store).SaveChangesAsync(ct);
    return Results.Ok(ToSwap(result.Value!, store));
}).RequireAuthorization().WithTags("Échanges");

app.MapGet("/api/v1/swap-requests", (ClaimsPrincipal user, IVigieStore store) =>
{
    var scope = OrganizationScopeResolver.Resolve(user, store);
    if (scope is null) return Problem("SESSION_INVALID", "La session n'est plus valide.", StatusCodes.Status401Unauthorized);
    var id = UserId(user); var organizationId = scope.OrganizationId;
    var requests = store.SwapRequests.Where(r =>
        store.Assignments.Any(assignment => assignment.Id == r.AssignmentId && store.Shifts.Any(shift => shift.Id == assignment.ShiftId && store.Sites.Any(site => site.Id == shift.SiteId && site.OrganizationId == organizationId && IsSiteVisible(site, scope, store)))) &&
        (scope.IsDirector || scope.IsSectorManager || scope.IsPoolChief || r.ReceiverId == id || store.Assignments.Single(a => a.Id == r.AssignmentId).EmployeeId == id)).Select(r => ToSwap(r, store)).OrderBy(r => r.Status).ThenBy(r => r.RequestedAtUtc).ToArray();
    return Results.Ok(requests);
}).RequireAuthorization().WithTags("Échanges");

app.MapPost("/api/v1/swap-requests/{requestId:guid}/approve", async (ClaimsPrincipal user, Guid requestId, ApproveSwapService service, IVigieStore store, CancellationToken ct) =>
{
    if (!SwapBelongsToOrganization(requestId, OrganizationId(user), store)) return Problem("NOT_FOUND", "La demande d'échange est introuvable.", StatusCodes.Status404NotFound);
    var swapSite = SwapSite(requestId, store);
    if (swapSite is null || !OrganizationScopeResolver.CanDecideSwap(OrganizationScopeResolver.Resolve(user, store), swapSite)) return Results.StatusCode(StatusCodes.Status403Forbidden);
    var result = await service.ExecuteAsync(UserId(user), requestId, ct);
    if (!result.IsSuccess) return result.ToHttpResult(swap => Results.Ok(ToSwap(swap, store)));
    store.AddAuditEntry(Audit(OrganizationId(user), UserId(user), "swap.approved", "SwapRequest", requestId));
    NotifySwapParticipants(store, result.Value!, "Échange approuvé", "Votre demande d'échange a été approuvée.");
    await ((IUnitOfWork)store).SaveChangesAsync(ct);
    return Results.Ok(ToSwap(result.Value!, store));
}).RequireAuthorization().WithTags("Échanges");
app.MapPost("/api/v1/swap-requests/{requestId:guid}/reject", async (ClaimsPrincipal user, Guid requestId, RejectSwapService service, IVigieStore store, CancellationToken ct) =>
{
    if (!SwapBelongsToOrganization(requestId, OrganizationId(user), store)) return Problem("NOT_FOUND", "La demande d'échange est introuvable.", StatusCodes.Status404NotFound);
    var swapSite = SwapSite(requestId, store);
    if (swapSite is null || !OrganizationScopeResolver.CanDecideSwap(OrganizationScopeResolver.Resolve(user, store), swapSite)) return Results.StatusCode(StatusCodes.Status403Forbidden);
    var result = await service.ExecuteAsync(UserId(user), requestId, ct);
    if (!result.IsSuccess) return result.ToHttpResult(swap => Results.Ok(ToSwap(swap, store)));
    store.AddAuditEntry(Audit(OrganizationId(user), UserId(user), "swap.rejected", "SwapRequest", requestId));
    NotifySwapParticipants(store, result.Value!, "Échange refusé", "Votre demande d'échange a été refusée.");
    await ((IUnitOfWork)store).SaveChangesAsync(ct);
    return Results.Ok(ToSwap(result.Value!, store));
}).RequireAuthorization().WithTags("Échanges");

app.MapGet("/api/v1/availability", (ClaimsPrincipal user, IVigieStore store) => Results.Ok(store.Availabilities.Where(a => a.EmployeeId == UserId(user)).OrderBy(a => a.Date).Select(a => new AvailabilityResponse(a.Id, a.EmployeeId, a.Date, a.IsAvailable, a.Note)).ToArray())).RequireAuthorization().WithTags("Disponibilités");
app.MapPut("/api/v1/availability", async (ClaimsPrincipal user, AvailabilityRequest request, IVigieStore store, IUnitOfWork unitOfWork, CancellationToken ct) =>
{
    var availability = store.UpsertAvailability(UserId(user), request.Date, request.IsAvailable, request.Note);
    await unitOfWork.SaveChangesAsync(ct);
    return Results.Ok(new AvailabilityResponse(availability.Id, availability.EmployeeId, availability.Date, availability.IsAvailable, availability.Note));
}).RequireAuthorization().WithTags("Disponibilités");

app.Run();

static Guid UserId(ClaimsPrincipal principal) => Guid.Parse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Identité absente."));
static Guid OrganizationId(ClaimsPrincipal principal) => Guid.Parse(principal.FindFirstValue("organization_id") ?? throw new InvalidOperationException("Organisation absente."));
static OrganizationMembership? PrimaryMembership(Employee employee, IVigieStore store)
    => store.Memberships.Where(membership => membership.EmployeeId == employee.Id && membership.OrganizationId == employee.OrganizationId && membership.IsActive)
        .OrderBy(membership => membership.Role == EmployeeRole.AquaticDirector ? 0 : membership.Role == EmployeeRole.SectorManager ? 1 : membership.Role is EmployeeRole.PoolChief or EmployeeRole.Coordinator ? 2 : 3)
        .FirstOrDefault();
static bool IsSiteVisible(Site site, OrganizationScope scope, IVigieStore store)
    => site.OrganizationId == scope.OrganizationId &&
       (scope.IsDirector ||
        scope.IsSectorManager && scope.SectorId.HasValue && site.SectorId == scope.SectorId ||
        scope.IsPoolChief && scope.SiteId == site.Id);
static bool IsInvitationVisible(Invitation invitation, OrganizationScope scope, IVigieStore store)
{
    if (scope.IsDirector || scope.Role == EmployeeRole.Coordinator) return true;
    if (scope.Role == EmployeeRole.SectorManager)
        return invitation.SectorId == scope.SectorId || invitation.SiteId.HasValue && store.Sites.Any(site => site.Id == invitation.SiteId && site.SectorId == scope.SectorId);
    return scope.IsPoolChief && invitation.SiteId == scope.SiteId;
}
static bool IsAuditVisible(AuditEntry entry, OrganizationScope scope, IVigieStore store)
{
    if (scope.IsDirector || scope.Role == EmployeeRole.Coordinator) return true;
    return entry.EntityType switch
    {
        "Sector" => scope.Role == EmployeeRole.SectorManager && entry.EntityId == scope.SectorId,
        "Site" => entry.EntityId.HasValue && store.Sites.Any(site => site.Id == entry.EntityId && IsSiteVisible(site, scope, store)),
        "Shift" => entry.EntityId.HasValue && store.Shifts.Any(shift => shift.Id == entry.EntityId && store.Sites.Any(site => site.Id == shift.SiteId && IsSiteVisible(site, scope, store))),
        "Assignment" => entry.EntityId.HasValue && store.Assignments.Any(assignment => assignment.Id == entry.EntityId && store.Shifts.Any(shift => shift.Id == assignment.ShiftId && store.Sites.Any(site => site.Id == shift.SiteId && IsSiteVisible(site, scope, store)))),
        "SwapRequest" => entry.EntityId.HasValue && store.SwapRequests.Any(request => request.Id == entry.EntityId && store.Assignments.Any(assignment => assignment.Id == request.AssignmentId && store.Shifts.Any(shift => shift.Id == assignment.ShiftId && store.Sites.Any(site => site.Id == shift.SiteId && IsSiteVisible(site, scope, store))))),
        "Invitation" => entry.EntityId.HasValue && store.Invitations.Any(invitation => invitation.Id == entry.EntityId && IsInvitationVisible(invitation, scope, store)),
        "OrganizationMembership" => entry.EntityId.HasValue && store.Memberships.Any(membership => membership.Id == entry.EntityId && (membership.SiteId == scope.SiteId || membership.SectorId == scope.SectorId)),
        "Employee" => entry.ActorId == scope.EmployeeId,
        _ => false
    };
}
static bool HasValidRoleScope(EmployeeRole role, Site? site, Sector? sector)
    => role switch
    {
        EmployeeRole.Lifeguard or EmployeeRole.PoolChief => site is not null && sector is null,
        EmployeeRole.SectorManager => site is null && sector is not null,
        EmployeeRole.AquaticDirector => site is null && sector is null,
        EmployeeRole.Coordinator => true,
        _ => false
    };
static bool IsEmployeeVisible(Employee employee, OrganizationScope scope, IVigieStore store)
    => scope.IsDirector || employee.Id == scope.EmployeeId ||
       store.Memberships.Any(membership => membership.EmployeeId == employee.Id && membership.OrganizationId == scope.OrganizationId && membership.IsActive &&
           (scope.IsSectorManager && membership.SectorId == scope.SectorId || scope.IsPoolChief && membership.SiteId == scope.SiteId));
static UserSummary User(Employee employee, IVigieStore store)
{
    var membership = PrimaryMembership(employee, store);
    var role = membership is null
        ? OrganizationScopeResolver.Normalize(employee.Role)
        : employee.Role == EmployeeRole.Coordinator && membership.Role == EmployeeRole.PoolChief
            ? EmployeeRole.Coordinator
            : OrganizationScopeResolver.Normalize(membership.Role);
    var siteId = membership?.SiteId;
    var sectorId = membership?.SectorId;
    return new UserSummary(employee.Id, employee.Name, employee.Email, role.ToString(), employee.OrganizationId, employee.IsDemoAccount, siteId, sectorId);
}
static string Csv(string? value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
static AuditEntry Audit(Guid organizationId, Guid? actorId, string action, string entityType, Guid? entityId = null, string? details = null)
    => AuditEntry.Create(Guid.NewGuid(), organizationId, actorId, action, entityType, entityId, details, DateTimeOffset.UtcNow);
static OrganizationResponse ToOrganization(Organization organization) => new(organization.Id, organization.Name, organization.Slug, organization.CreatedAtUtc);
static SectorResponse ToSector(Sector sector) => new(sector.Id, sector.OrganizationId, sector.Name, sector.Code, sector.IsActive, sector.CreatedAtUtc, sector.UpdatedAtUtc);
static MembershipResponse ToMembership(OrganizationMembership membership, IVigieStore store)
{
    var employee = store.Employees.Single(item => item.Id == membership.EmployeeId);
    var site = membership.SiteId.HasValue ? store.Sites.SingleOrDefault(item => item.Id == membership.SiteId) : null;
    var sector = membership.SectorId.HasValue ? store.Sectors.SingleOrDefault(item => item.Id == membership.SectorId) : null;
    return new MembershipResponse(membership.Id, employee.Id, employee.Name, employee.Email, OrganizationScopeResolver.Normalize(membership.Role).ToString(), membership.OrganizationId, membership.SiteId, site?.Name, membership.SectorId, sector?.Name, membership.IsActive, membership.Version, membership.CreatedAtUtc, membership.UpdatedAtUtc);
}
static InvitationResponse ToInvitation(Invitation invitation, string? token = null, string? link = null)
    => new(invitation.Id, invitation.Email, invitation.Name, invitation.Role.ToString(), invitation.Status.ToString(), invitation.ExpiresAtUtc, token, link, invitation.SiteId, invitation.SectorId);
static bool SwapBelongsToOrganization(Guid requestId, Guid organizationId, IVigieStore store)
{
    var request = store.SwapRequests.SingleOrDefault(item => item.Id == requestId);
    var assignment = request is null ? null : store.Assignments.SingleOrDefault(item => item.Id == request.AssignmentId);
    var shift = assignment is null ? null : store.Shifts.SingleOrDefault(item => item.Id == assignment.ShiftId);
    return shift is not null && store.Sites.Any(site => site.Id == shift.SiteId && site.OrganizationId == organizationId);
}
static Site? SwapSite(Guid requestId, IVigieStore store)
{
    var request = store.SwapRequests.SingleOrDefault(item => item.Id == requestId);
    var assignment = request is null ? null : store.Assignments.SingleOrDefault(item => item.Id == request.AssignmentId);
    var shift = assignment is null ? null : store.Shifts.SingleOrDefault(item => item.Id == assignment.ShiftId);
    return shift is null ? null : store.Sites.SingleOrDefault(site => site.Id == shift.SiteId);
}
static SiteResponse ToSite(Site site, IVigieStore store)
{
    var sector = site.SectorId.HasValue ? store.Sectors.SingleOrDefault(item => item.Id == site.SectorId) : null;
    return new SiteResponse(site.Id, site.Name, site.Type.ToString(), site.TimeZoneId, site.OpeningSeason, site.Address, site.Neighborhood, site.IsMunicipal, site.SectorId, sector?.Name);
}
static ShiftResponse ToShift(Shift shift, IVigieStore store)
{
    var site = store.Sites.Single(s => s.Id == shift.SiteId);
    return new ShiftResponse(shift.Id, shift.SiteId, site.Name, site.Type.ToString(), shift.StartUtc, shift.EndUtc, shift.RequiredLifeguards, store.Assignments.Where(a => a.ShiftId == shift.Id).Select(a => new AssignmentResponse(a.Id, a.ShiftId, a.EmployeeId, store.Employees.Single(e => e.Id == a.EmployeeId).Name)).ToArray(), shift.Status.ToString());
}
static SwapRequestResponse ToSwap(SwapRequest request, IVigieStore store)
{
    var assignment = store.Assignments.Single(a => a.Id == request.AssignmentId); var shift = store.Shifts.Single(s => s.Id == assignment.ShiftId); var requester = store.Employees.Single(e => e.Id == assignment.EmployeeId); var receiver = store.Employees.Single(e => e.Id == request.ReceiverId);
    return new SwapRequestResponse(request.Id, request.AssignmentId, requester.Id, requester.Name, request.ReceiverId, receiver.Name, $"{shift.StartUtc:ddd d MMM HH:mm} · {store.Sites.Single(s => s.Id == shift.SiteId).Name}", request.Status.ToString(), request.RequestedAtUtc);
}
static NotificationResponse ToNotification(Notification notification)
    => new(notification.Id, notification.Type, notification.Title, notification.Body, notification.ActionUrl, notification.CreatedAtUtc, notification.IsRead, notification.ReadAtUtc);
static void AddNotification(IVigieStore store, Guid organizationId, Guid recipientEmployeeId, string type, string title, string body, string? actionUrl)
{
    if (!store.Employees.Any(employee => employee.Id == recipientEmployeeId && employee.OrganizationId == organizationId)) return;
    store.AddNotification(Notification.Create(Guid.NewGuid(), organizationId, recipientEmployeeId, type, title, body, DateTimeOffset.UtcNow, actionUrl));
}
static void NotifyManagement(IVigieStore store, Guid organizationId, Shift shift, string title, string body, string actionUrl)
{
    var site = store.Sites.SingleOrDefault(item => item.Id == shift.SiteId && item.OrganizationId == organizationId);
    if (site is null) return;
    var recipients = store.Memberships
        .Where(membership => membership.OrganizationId == organizationId && membership.IsActive)
        .Where(membership => membership.Role == EmployeeRole.AquaticDirector ||
            membership.Role == EmployeeRole.SectorManager && membership.SectorId.HasValue && membership.SectorId == site.SectorId ||
            (membership.Role is EmployeeRole.PoolChief or EmployeeRole.Coordinator) && membership.SiteId == site.Id)
        .Select(membership => membership.EmployeeId)
        .Distinct()
        .ToArray();
    foreach (var recipient in recipients) AddNotification(store, organizationId, recipient, "swap", title, body, actionUrl);
}
static void NotifySwapParticipants(IVigieStore store, SwapRequest swap, string title, string body)
{
    var assignment = store.Assignments.SingleOrDefault(item => item.Id == swap.AssignmentId);
    var shift = assignment is null ? null : store.Shifts.SingleOrDefault(item => item.Id == assignment.ShiftId);
    var site = shift is null ? null : store.Sites.SingleOrDefault(item => item.Id == shift.SiteId);
    if (assignment is null || site is null) return;
    foreach (var recipient in new[] { assignment.EmployeeId, swap.ReceiverId }.Distinct())
        AddNotification(store, site.OrganizationId, recipient, "swap", title, body, "swaps");
}
static IResult Problem(string code, string message, int status = StatusCodes.Status400BadRequest) => Results.Problem(statusCode: status, title: "La demande ne peut pas être traitée", detail: message, extensions: new Dictionary<string, object?> { ["code"] = code, ["message"] = message });

static string Slugify(string value)
{
    var normalized = value.Trim().Normalize(NormalizationForm.FormD);
    var chars = normalized.Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark).ToArray();
    var slug = new string(chars).ToLowerInvariant();
    slug = new string(slug.Select(character => char.IsLetterOrDigit(character) ? character : '-').ToArray());
    while (slug.Contains("--", StringComparison.Ordinal)) slug = slug.Replace("--", "-", StringComparison.Ordinal);
    return slug.Trim('-') is { Length: > 0 } clean ? clean[..Math.Min(clean.Length, 70)] : $"organisation-{Guid.NewGuid():N}"[..24];
}

public static class OperationResultHttpExtensions
{
    public static IResult ToHttpResult<T>(this OperationResult<T> result, Func<T, IResult> onSuccess)
        => result.IsSuccess ? onSuccess(result.Value!) : Results.Problem(statusCode: result.Errors.Any(e => e.Code == "FORBIDDEN") ? 403 : result.Errors.Any(e => e.Code == "NOT_FOUND") ? 404 : 409, title: "La demande ne peut pas être traitée", detail: result.Errors[0].Message, extensions: new Dictionary<string, object?> { ["code"] = result.Errors[0].Code, ["errors"] = result.Errors });
}

public partial class Program { }
