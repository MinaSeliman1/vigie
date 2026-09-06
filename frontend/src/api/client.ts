import type { ApiProblem, AssignmentResponse, AuditEntryResponse, AvailabilityResponse, CertificationResponse, CreateShiftInput, InvitationResponse, LoginResponse, MembershipResponse, RegistrationResponse, SectorResponse, ShiftResponse, SiteResponse, SwapRequestResponse, UpdateShiftInput, UserSummary } from './types'

export const apiConfigured = Boolean(import.meta.env.VITE_API_URL)
const baseUrl = (import.meta.env.VITE_API_URL as string | undefined)?.replace(/\/$/, '') ?? 'http://localhost:5187'

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const token = localStorage.getItem('vigie.token')
  const response = await fetch(`${baseUrl}${path}`, {
    ...init,
    headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}), ...init.headers },
  })
  const payload = await response.text()
  if (!response.ok) {
    let problem: ApiProblem = {}
    try { problem = JSON.parse(payload) as ApiProblem } catch { /* Réponse non JSON : le statut reste exploitable. */ }
    const error = new Error(problem.message ?? problem.detail ?? 'La demande ne peut pas être traitée.')
    Object.assign(error, { code: problem.code })
    throw error
  }
  return (payload ? JSON.parse(payload) : undefined) as T
}

export const vigieApi = {
  login: (email: string, password: string) => request<LoginResponse>('/api/v1/auth/login', { method: 'POST', body: JSON.stringify({ email, password }) }),
  me: () => request<UserSummary>('/api/v1/auth/me'),
  changePassword: (currentPassword: string, newPassword: string) => request<LoginResponse>('/api/v1/auth/change-password', { method: 'POST', body: JSON.stringify({ currentPassword, newPassword }) }),
  audit: (limit = 50) => request<AuditEntryResponse[]>(`/api/v1/audit?limit=${limit}`),
  exportAudit: async () => {
    const token = localStorage.getItem('vigie.token')
    const response = await fetch(`${baseUrl}/api/v1/audit/export`, { headers: token ? { Authorization: `Bearer ${token}` } : {} })
    if (!response.ok) throw new Error('L’export du journal ne peut pas être généré.')
    return response.blob()
  },
  register: (organizationName: string, name: string, email: string, password: string) => request<RegistrationResponse>('/api/v1/auth/register', { method: 'POST', body: JSON.stringify({ organizationName, name, email, password }) }),
  acceptInvitation: (token: string, name: string, password: string) => request<LoginResponse>('/api/v1/invitations/accept', { method: 'POST', body: JSON.stringify({ token, name, password }) }),
  inviteMember: (email: string, name: string, role: string, siteId?: string, sectorId?: string) => request<InvitationResponse>('/api/v1/invitations', { method: 'POST', body: JSON.stringify({ email, name, role, siteId, sectorId }) }),
  updateMembership: (membershipId: string, input: { role?: string; siteId?: string | null; sectorId?: string | null; isActive?: boolean; expectedVersion?: number }) => request<MembershipResponse>(`/api/v1/memberships/${membershipId}`, { method: 'PATCH', body: JSON.stringify(input) }),
  deactivateMembership: (membershipId: string) => request<void>(`/api/v1/memberships/${membershipId}`, { method: 'DELETE' }),
  shifts: (from?: string, to?: string) => {
    const query = new URLSearchParams()
    if (from) query.set('from', from)
    if (to) query.set('to', to)
    return request<ShiftResponse[]>(`/api/v1/shifts${query.size ? `?${query.toString()}` : ''}`)
  },
  sites: () => request<SiteResponse[]>('/api/v1/sites'),
  createShift: (input: CreateShiftInput) => request<ShiftResponse>('/api/v1/shifts', { method: 'POST', body: JSON.stringify(input) }),
  updateShift: (shiftId: string, input: UpdateShiftInput) => request<ShiftResponse>(`/api/v1/shifts/${shiftId}`, { method: 'PATCH', body: JSON.stringify(input) }),
  cancelShift: (shiftId: string) => request<ShiftResponse>(`/api/v1/shifts/${shiftId}/cancel`, { method: 'POST' }),
  assignShift: (shiftId: string, employeeId: string) => request<AssignmentResponse>(`/api/v1/shifts/${shiftId}/assignments`, { method: 'POST', body: JSON.stringify({ employeeId }) }),
  removeAssignment: (assignmentId: string) => request<void>(`/api/v1/assignments/${assignmentId}`, { method: 'DELETE' }),
  employees: () => request<UserSummary[]>('/api/v1/employees'),
  availability: () => request<AvailabilityResponse[]>('/api/v1/availability'),
  setAvailability: (date: string, isAvailable: boolean, note?: string) => request<AvailabilityResponse>('/api/v1/availability', { method: 'PUT', body: JSON.stringify({ date, isAvailable, note }) }),
  certifications: () => request<CertificationResponse[]>('/api/v1/certifications'),
  sectors: () => request<SectorResponse[]>('/api/v1/sectors'),
  members: () => request<MembershipResponse[]>('/api/v1/members'),
  swaps: () => request<SwapRequestResponse[]>('/api/v1/swap-requests'),
  createSwap: (assignmentId: string, receiverId: string) => request<SwapRequestResponse>('/api/v1/swap-requests', { method: 'POST', body: JSON.stringify({ assignmentId, receiverId }) }),
  approveSwap: (requestId: string) => request<SwapRequestResponse>(`/api/v1/swap-requests/${requestId}/approve`, { method: 'POST' }),
  rejectSwap: (requestId: string) => request<SwapRequestResponse>(`/api/v1/swap-requests/${requestId}/reject`, { method: 'POST' }),
}
