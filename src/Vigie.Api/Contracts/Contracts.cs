using Vigie.Domain;

namespace Vigie.Api.Contracts;

public sealed record LoginRequest(string Email, string Password);
public sealed record LoginResponse(string Token, DateTimeOffset ExpiresAtUtc, UserSummary User);
public sealed record UserSummary(Guid Id, string Name, string Email, string Role);
public sealed record SiteResponse(Guid Id, string Name, string Type, string TimeZoneId, OpeningSeason OpeningSeason);
public sealed record ShiftResponse(Guid Id, Guid SiteId, string SiteName, string SiteType, DateTimeOffset StartUtc, DateTimeOffset EndUtc, int RequiredLifeguards, IReadOnlyCollection<AssignmentResponse> Assignments);
public sealed record AssignmentResponse(Guid Id, Guid ShiftId, Guid EmployeeId, string EmployeeName);
public sealed record CreateShiftRequest(Guid SiteId, DateTimeOffset StartUtc, DateTimeOffset EndUtc, int RequiredLifeguards);
public sealed record CreateSiteRequest(string Name, string Type, string TimeZoneId, int StartMonth, int StartDay, int EndMonth, int EndDay);
public sealed record AssignShiftRequest(Guid EmployeeId);
public sealed record CertificationResponse(Guid Id, Guid EmployeeId, string EmployeeName, string Type, DateOnly ExpiresOn, int DaysRemaining);
public sealed record CreateCertificationRequest(Guid EmployeeId, Guid CertificationTypeId, DateOnly ExpiresOn);
public sealed record SwapRequestResponse(Guid Id, Guid AssignmentId, Guid RequesterId, string RequesterName, Guid ReceiverId, string ReceiverName, string ShiftLabel, string Status, DateTimeOffset RequestedAtUtc);
public sealed record CreateSwapRequest(Guid AssignmentId, Guid ReceiverId);
public sealed record DashboardResponse(int UpcomingShifts, int PendingSwapRequests, int CertificationAlerts, IReadOnlyCollection<CertificationResponse> CertificationWarnings);
public sealed record AvailabilityRequest(DateOnly Date, bool IsAvailable, string? Note);
public sealed record AvailabilityResponse(Guid Id, Guid EmployeeId, DateOnly Date, bool IsAvailable, string? Note);
