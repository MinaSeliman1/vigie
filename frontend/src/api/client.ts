import type { ApiProblem, AssignmentResponse, AvailabilityResponse, CertificationResponse, CreateShiftInput, InvitationResponse, LoginResponse, RegistrationResponse, ShiftResponse, SiteResponse, SwapRequestResponse, UserSummary } from './types'

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
  register: (organizationName: string, name: string, email: string, password: string) => request<RegistrationResponse>('/api/v1/auth/register', { method: 'POST', body: JSON.stringify({ organizationName, name, email, password }) }),
  acceptInvitation: (token: string, name: string, password: string) => request<LoginResponse>('/api/v1/invitations/accept', { method: 'POST', body: JSON.stringify({ token, name, password }) }),
  inviteMember: (email: string, name: string, role: string) => request<InvitationResponse>('/api/v1/invitations', { method: 'POST', body: JSON.stringify({ email, name, role }) }),
  shifts: (from?: string, to?: string) => {
    const query = new URLSearchParams()
    if (from) query.set('from', from)
    if (to) query.set('to', to)
    return request<ShiftResponse[]>(`/api/v1/shifts${query.size ? `?${query.toString()}` : ''}`)
  },
  sites: () => request<SiteResponse[]>('/api/v1/sites'),
  createShift: (input: CreateShiftInput) => request<ShiftResponse>('/api/v1/shifts', { method: 'POST', body: JSON.stringify(input) }),
  assignShift: (shiftId: string, employeeId: string) => request<AssignmentResponse>(`/api/v1/shifts/${shiftId}/assignments`, { method: 'POST', body: JSON.stringify({ employeeId }) }),
  removeAssignment: (assignmentId: string) => request<void>(`/api/v1/assignments/${assignmentId}`, { method: 'DELETE' }),
  employees: () => request<UserSummary[]>('/api/v1/employees'),
  availability: () => request<AvailabilityResponse[]>('/api/v1/availability'),
  setAvailability: (date: string, isAvailable: boolean, note?: string) => request<AvailabilityResponse>('/api/v1/availability', { method: 'PUT', body: JSON.stringify({ date, isAvailable, note }) }),
  certifications: () => request<CertificationResponse[]>('/api/v1/certifications'),
  swaps: () => request<SwapRequestResponse[]>('/api/v1/swap-requests'),
  createSwap: (assignmentId: string, receiverId: string) => request<SwapRequestResponse>('/api/v1/swap-requests', { method: 'POST', body: JSON.stringify({ assignmentId, receiverId }) }),
  approveSwap: (requestId: string) => request<SwapRequestResponse>(`/api/v1/swap-requests/${requestId}/approve`, { method: 'POST' }),
  rejectSwap: (requestId: string) => request<SwapRequestResponse>(`/api/v1/swap-requests/${requestId}/reject`, { method: 'POST' }),
}
