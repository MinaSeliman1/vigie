export type Role = 'Lifeguard' | 'Coordinator'

export type UserSummary = { id: string; name: string; email: string; role: Role }
export type LoginResponse = { token: string; expiresAtUtc: string; user: UserSummary }
export type ApiProblem = { code?: string; message?: string; detail?: string; errors?: Array<{ code: string; message: string }> }
export type SiteResponse = { id: string; name: string; type: string; timeZoneId: string; openingSeason: { startMonth: number; startDay: number; endMonth: number; endDay: number } }
export type CreateShiftInput = { siteId: string; startUtc: string; endUtc: string; requiredLifeguards: number }
export type AssignmentResponse = { id: string; shiftId: string; employeeId: string; employeeName: string }

export type ShiftResponse = {
  id: string
  siteId: string
  siteName: string
  siteType: 'Indoor' | 'Outdoor' | string
  startUtc: string
  endUtc: string
  requiredLifeguards: number
  assignments: AssignmentResponse[]
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
