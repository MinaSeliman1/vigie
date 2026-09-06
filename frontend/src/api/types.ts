export type Role = 'Lifeguard' | 'PoolChief' | 'SectorManager' | 'AquaticDirector' | 'Coordinator'

export type UserSummary = { id: string; name: string; email: string; role: Role; organizationId: string; isDemoAccount: boolean; siteId?: string | null; sectorId?: string | null }
export type AuditEntryResponse = { id: string; action: string; entityType: string; entityId?: string | null; details?: string | null; actorName?: string | null; createdAtUtc: string }
export type LoginResponse = { token: string; expiresAtUtc: string; user: UserSummary }
export type NotificationResponse = { id: string; type: string; title: string; body: string; actionUrl?: string | null; createdAtUtc: string; isRead: boolean; readAtUtc?: string | null }
export type OrganizationResponse = { id: string; name: string; slug: string; createdAtUtc: string }
export type RegistrationResponse = { login: LoginResponse; organization: OrganizationResponse }
export type InvitationResponse = { id: string; email: string; name: string; role: Role; status: string; expiresAtUtc: string; inviteToken?: string | null; inviteLink?: string | null; siteId?: string | null; sectorId?: string | null }
export type ApiProblem = { code?: string; message?: string; detail?: string; errors?: Array<{ code: string; message: string }> }
export type SiteResponse = { id: string; name: string; type: string; timeZoneId: string; openingSeason: { startMonth: number; startDay: number; endMonth: number; endDay: number }; address?: string; neighborhood?: string; isMunicipal?: boolean; sectorId?: string | null; sectorName?: string | null }
export type CreateShiftInput = { siteId: string; startUtc: string; endUtc: string; requiredLifeguards: number }
export type UpdateShiftInput = { startUtc: string; endUtc: string; requiredLifeguards: number }
export type AssignmentResponse = { id: string; shiftId: string; employeeId: string; employeeName: string }
export type AvailabilityResponse = { id: string; employeeId: string; date: string; isAvailable: boolean; note?: string | null }
export type SectorResponse = { id: string; organizationId: string; name: string; code: string; isActive: boolean; createdAtUtc: string; updatedAtUtc: string }
export type MembershipResponse = { id: string; employeeId: string; employeeName: string; email: string; role: Role; organizationId: string; siteId?: string | null; siteName?: string | null; sectorId?: string | null; sectorName?: string | null; isActive: boolean; version: number; createdAtUtc: string; updatedAtUtc: string }

export type ShiftResponse = {
  id: string
  siteId: string
  siteName: string
  siteType: 'Indoor' | 'Outdoor' | string
  startUtc: string
  endUtc: string
  requiredLifeguards: number
  assignments: AssignmentResponse[]
  status?: 'Open' | 'Filled' | 'Cancelled' | string
}

export type SwapRequestResponse = {
  id: string
  assignmentId: string
  requesterId: string
  requesterName: string
  receiverId: string
  receiverName: string
  shiftLabel: string
  status: 'Pending' | 'Approved' | 'Rejected' | 'Cancelled' | string
  requestedAtUtc: string
}

export type CertificationResponse = {
  id: string
  employeeId: string
  employeeName: string
  type: string
  expiresOn: string
  daysRemaining: number
}
