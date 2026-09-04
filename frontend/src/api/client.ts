import type { ApiProblem, LoginResponse, ShiftResponse } from './types'

const baseUrl = (import.meta.env.VITE_API_URL as string | undefined)?.replace(/\/$/, '') ?? 'http://localhost:5187'

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const token = localStorage.getItem('vigie.token')
  const response = await fetch(`${baseUrl}${path}`, {
    ...init,
    headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}), ...init.headers },
  })
  if (!response.ok) {
    const problem = await response.json() as ApiProblem
    throw new Error(problem.message ?? problem.detail ?? 'La demande ne peut pas être traitée.')
  }
  return response.json() as Promise<T>
}

export const vigieApi = {
  login: (email: string, password: string) => request<LoginResponse>('/api/v1/auth/login', { method: 'POST', body: JSON.stringify({ email, password }) }),
  shifts: (from?: string, to?: string) => request<ShiftResponse[]>(`/api/v1/shifts?from=${encodeURIComponent(from ?? '')}&to=${encodeURIComponent(to ?? '')}`),
}
