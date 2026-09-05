using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Vigie.Api.Auth;
using Vigie.Api.Contracts;
using Vigie.Application;
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
builder.Services.AddSingleton(new JwtTokenService(jwtKey));
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
});
builder.Services.AddAuthorization();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
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
app.UseAuthentication();
app.UseAuthorization();
app.MapOpenApi();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "vigie-api" }));

app.MapPost("/api/v1/auth/login", (LoginRequest request, IVigieStore store, JwtTokenService tokens) =>
{
    var employee = store.Employees.SingleOrDefault(e => string.Equals(e.Email, request.Email.Trim(), StringComparison.OrdinalIgnoreCase));
    if (employee is null || request.Password != "vigie-demo") return Problem("INVALID_CREDENTIALS", "Le courriel ou le mot de passe est invalide.", StatusCodes.Status401Unauthorized);
    var (token, expires) = tokens.Create(employee);
    return Results.Ok(new LoginResponse(token, expires, User(employee)));
}).AllowAnonymous().WithTags("Authentification");

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

app.MapGet("/api/v1/sites", (IVigieStore store) => Results.Ok(store.Sites.Select(ToSite).ToArray())).RequireAuthorization().WithTags("Sites");
app.MapPost("/api/v1/sites", async (CreateSiteRequest request, IVigieStore store, IUnitOfWork unitOfWork, CancellationToken ct) =>
{
    if (!Enum.TryParse<SiteType>(request.Type, true, out var type)) return Problem("INVALID_SITE_TYPE", "Le type de site est invalide.");
    try
    {
        var site = Site.Create(Guid.NewGuid(), request.Name, request.TimeZoneId, new OpeningSeason(request.StartMonth, request.StartDay, request.EndMonth, request.EndDay), type);
        store.AddSite(site);
        await unitOfWork.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/sites/{site.Id}", ToSite(site));
    }
    catch (DomainException ex) { return Problem("INVALID_SITE", ex.Message); }
}).RequireAuthorization(new AuthorizeAttribute { Roles = nameof(EmployeeRole.Coordinator) }).WithTags("Sites");

app.MapGet("/api/v1/shifts", (DateTimeOffset? from, DateTimeOffset? to, IVigieStore store) =>
{
    var start = from ?? DateTimeOffset.UtcNow.AddDays(-1);
    var end = to ?? DateTimeOffset.UtcNow.AddDays(14);
    var result = store.Shifts.Where(s => s.StartUtc < end && s.EndUtc > start).OrderBy(s => s.StartUtc).Select(s => ToShift(s, store)).ToArray();
    return Results.Ok(result);
}).RequireAuthorization().WithTags("Quarts");

app.MapPost("/api/v1/shifts", async (CreateShiftRequest request, IVigieStore store, IUnitOfWork unitOfWork, CancellationToken ct) =>
{
    try
    {
        if (!store.Sites.Any(s => s.Id == request.SiteId)) return Problem("NOT_FOUND", "Le site du quart est introuvable.", StatusCodes.Status404NotFound);
        var shift = Shift.Create(Guid.NewGuid(), request.SiteId, request.StartUtc, request.EndUtc, request.RequiredLifeguards);
        store.AddShift(shift);
        await unitOfWork.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/shifts/{shift.Id}", ToShift(shift, store));
    }
    catch (DomainException ex) { return Problem("INVALID_SHIFT", ex.Message); }
}).RequireAuthorization(new AuthorizeAttribute { Roles = nameof(EmployeeRole.Coordinator) }).WithTags("Quarts");

app.MapPost("/api/v1/shifts/{shiftId:guid}/assignments", async (Guid shiftId, AssignShiftRequest request, AssignShiftService service, CancellationToken ct) =>
    (await service.ExecuteAsync(request.EmployeeId, shiftId, ct)).ToHttpResult(assignment => Results.Ok(assignment))).RequireAuthorization(new AuthorizeAttribute { Roles = nameof(EmployeeRole.Coordinator) }).WithTags("Assignations");

app.MapDelete("/api/v1/assignments/{assignmentId:guid}", async (Guid assignmentId, IAssignmentRepository assignments, IUnitOfWork unitOfWork, CancellationToken ct) =>
{
    await assignments.RemoveAsync(assignmentId, ct); await unitOfWork.SaveChangesAsync(ct); return Results.NoContent();
}).RequireAuthorization(new AuthorizeAttribute { Roles = nameof(EmployeeRole.Coordinator) }).WithTags("Assignations");

app.MapGet("/api/v1/certifications", (ClaimsPrincipal user, IVigieStore store) =>
{
    var isCoordinator = user.IsInRole(nameof(EmployeeRole.Coordinator));
    var employeeId = UserId(user);
    var result = store.Certifications.Where(c => isCoordinator || c.EmployeeId == employeeId).Select(c =>
    {
        var employee = store.Employees.Single(e => e.Id == c.EmployeeId); var type = store.CertificationTypes.Single(t => t.Id == c.CertificationTypeId);
        return new CertificationResponse(c.Id, c.EmployeeId, employee.Name, type.Name, c.ExpiresOn, c.ExpiresOn.DayNumber - DateOnly.FromDateTime(DateTime.UtcNow).DayNumber);
    }).OrderBy(c => c.ExpiresOn).ToArray();
    return Results.Ok(result);
}).RequireAuthorization().WithTags("Certifications");

app.MapGet("/api/v1/employees", (IVigieStore store) => Results.Ok(store.Employees.Select(User).ToArray())).RequireAuthorization().WithTags("Employés");

app.MapPost("/api/v1/swap-requests", async (ClaimsPrincipal user, CreateSwapRequest request, RequestSwapService service, IVigieStore store, CancellationToken ct) =>
    (await service.ExecuteAsync(UserId(user), request.AssignmentId, request.ReceiverId, ct)).ToHttpResult(result => Results.Ok(ToSwap(result, store)))).RequireAuthorization().WithTags("Échanges");

app.MapGet("/api/v1/swap-requests", (ClaimsPrincipal user, IVigieStore store) =>
{
    var coordinator = user.IsInRole(nameof(EmployeeRole.Coordinator)); var id = UserId(user);
    var requests = store.SwapRequests.Where(r => coordinator || r.ReceiverId == id || store.Assignments.Single(a => a.Id == r.AssignmentId).EmployeeId == id).Select(r => ToSwap(r, store)).OrderBy(r => r.Status).ThenBy(r => r.RequestedAtUtc).ToArray();
    return Results.Ok(requests);
}).RequireAuthorization().WithTags("Échanges");

app.MapPost("/api/v1/swap-requests/{requestId:guid}/approve", async (ClaimsPrincipal user, Guid requestId, ApproveSwapService service, IVigieStore store, CancellationToken ct) =>
    (await service.ExecuteAsync(UserId(user), requestId, ct)).ToHttpResult(result => Results.Ok(ToSwap(result, store)))).RequireAuthorization(new AuthorizeAttribute { Roles = nameof(EmployeeRole.Coordinator) }).WithTags("Échanges");
app.MapPost("/api/v1/swap-requests/{requestId:guid}/reject", async (ClaimsPrincipal user, Guid requestId, RejectSwapService service, IVigieStore store, CancellationToken ct) =>
    (await service.ExecuteAsync(UserId(user), requestId, ct)).ToHttpResult(result => Results.Ok(ToSwap(result, store)))).RequireAuthorization(new AuthorizeAttribute { Roles = nameof(EmployeeRole.Coordinator) }).WithTags("Échanges");

app.MapGet("/api/v1/availability", (ClaimsPrincipal user, IVigieStore store) => Results.Ok(store.Availabilities.Where(a => a.EmployeeId == UserId(user)).OrderBy(a => a.Date).Select(a => new AvailabilityResponse(a.Id, a.EmployeeId, a.Date, a.IsAvailable, a.Note)).ToArray())).RequireAuthorization().WithTags("Disponibilités");
app.MapPut("/api/v1/availability", async (ClaimsPrincipal user, AvailabilityRequest request, IVigieStore store, IUnitOfWork unitOfWork, CancellationToken ct) =>
{
    var availability = store.UpsertAvailability(UserId(user), request.Date, request.IsAvailable, request.Note);
    await unitOfWork.SaveChangesAsync(ct);
    return Results.Ok(new AvailabilityResponse(availability.Id, availability.EmployeeId, availability.Date, availability.IsAvailable, availability.Note));
}).RequireAuthorization().WithTags("Disponibilités");

app.Run();

static Guid UserId(ClaimsPrincipal principal) => Guid.Parse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Identité absente."));
static UserSummary User(Employee employee) => new(employee.Id, employee.Name, employee.Email, employee.Role.ToString());
static SiteResponse ToSite(Site site) => new(site.Id, site.Name, site.Type.ToString(), site.TimeZoneId, site.OpeningSeason);
static ShiftResponse ToShift(Shift shift, IVigieStore store)
{
    var site = store.Sites.Single(s => s.Id == shift.SiteId);
    return new ShiftResponse(shift.Id, shift.SiteId, site.Name, site.Type.ToString(), shift.StartUtc, shift.EndUtc, shift.RequiredLifeguards, store.Assignments.Where(a => a.ShiftId == shift.Id).Select(a => new AssignmentResponse(a.Id, a.ShiftId, a.EmployeeId, store.Employees.Single(e => e.Id == a.EmployeeId).Name)).ToArray());
}
static SwapRequestResponse ToSwap(SwapRequest request, IVigieStore store)
{
    var assignment = store.Assignments.Single(a => a.Id == request.AssignmentId); var shift = store.Shifts.Single(s => s.Id == assignment.ShiftId); var requester = store.Employees.Single(e => e.Id == assignment.EmployeeId); var receiver = store.Employees.Single(e => e.Id == request.ReceiverId);
    return new SwapRequestResponse(request.Id, request.AssignmentId, requester.Id, requester.Name, request.ReceiverId, receiver.Name, $"{shift.StartUtc:ddd d MMM HH:mm} · {store.Sites.Single(s => s.Id == shift.SiteId).Name}", request.Status.ToString(), request.RequestedAtUtc);
}
static IResult Problem(string code, string message, int status = StatusCodes.Status400BadRequest) => Results.Problem(statusCode: status, title: "La demande ne peut pas être traitée", detail: message, extensions: new Dictionary<string, object?> { ["code"] = code, ["message"] = message });

public static class OperationResultHttpExtensions
{
    public static IResult ToHttpResult<T>(this OperationResult<T> result, Func<T, IResult> onSuccess)
        => result.IsSuccess ? onSuccess(result.Value!) : Results.Problem(statusCode: result.Errors.Any(e => e.Code == "FORBIDDEN") ? 403 : result.Errors.Any(e => e.Code == "NOT_FOUND") ? 404 : 409, title: "La demande ne peut pas être traitée", detail: result.Errors[0].Message, extensions: new Dictionary<string, object?> { ["code"] = result.Errors[0].Code, ["errors"] = result.Errors });
}

public partial class Program { }
