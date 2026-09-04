export type Role = 'Lifeguard' | 'Coordinator'

export type UserSummary = { id: string; name: string; email: string; role: Role }
export type LoginResponse = { token: string; expiresAtUtc: string; user: UserSummary }
export type ApiProblem = { code?: string; message?: string; detail?: string; errors?: Array<{ code: string; message: string }> }

export type ShiftResponse = {
  id: string
  siteId: string
  siteName: string
  startUtc: string
  endUtc: string
  requiredLifeguards: number
  assignments: Array<{ id: string; employeeId: string; employeeName: string }>
}
