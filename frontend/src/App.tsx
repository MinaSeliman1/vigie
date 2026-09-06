import { useEffect, useMemo, useRef, useState, type FormEvent, type ReactNode } from 'react'
import { apiConfigured, vigieApi } from './api/client'
import type { AuditEntryResponse, AvailabilityResponse, MembershipResponse, Role as ApiRole, SectorResponse, CertificationResponse, SiteResponse, ShiftResponse, SwapRequestResponse, UserSummary } from './api/types'
import { createShiftRequest, type CreateShiftDraft, validateCreateShiftDraft } from './features/shifts/createShift'
import { filterSwaps, type SwapFilter } from './features/swaps/swapFilters'
import { filterSites, type SiteTypeFilter } from './features/sites/siteFilters'
import { roleLabel, roleScopeLabel } from './features/access/roleLabels'
import { demoSitesForRole } from './data/lavalCatalog'
import './App.css'

type Role = ApiRole
type View = 'calendar' | 'swaps' | 'certifications' | 'team' | 'sites' | 'availability' | 'audit'
type AuthMode = 'login' | 'register' | 'accept-invitation'
type DemoUser = { id: string; apiId?: string; name: string; email: string; role: Role; initials: string; organizationId?: string; siteId?: string | null; sectorId?: string | null; isDemoAccount: boolean }
type Shift = { id: string; assignmentId?: string; day: string; date: number; start: string; end: string; site: string; siteKind: string; status: 'assigné' | 'disponible'; lifecycleStatus?: string; colleagues: string[]; requiredLifeguards: number; assignments?: Array<{ id: string; employeeId: string; employeeName: string }> }
type Swap = { id: string; shiftId: string; requester: string; receiver: string; shiftLabel: string; status: 'En attente' | 'Approuvé' | 'Refusé'; requestedAt?: string }
type CertificationRow = { id: string; initials: string; name: string; email: string; type: string; expiry: string; detail: string; warning: boolean }
type InviteDraft = { name: string; email: string; role: Role; siteId: string; sectorId: string }

const demoUsers: DemoUser[] = [
  { id: 'amelie', name: 'Amélie Roy', email: 'amelie@vigie.demo', role: 'Lifeguard', initials: 'AR', isDemoAccount: true },
  { id: 'noah', name: 'Noah Tremblay', email: 'noah@vigie.demo', role: 'Lifeguard', initials: 'NT', isDemoAccount: true },
  { id: 'sofia', name: 'Sofia Nguyen', email: 'sofia@vigie.demo', role: 'Lifeguard', initials: 'SN', isDemoAccount: true },
  { id: 'camille', name: 'Camille Gagnon', email: 'coordonnateur@vigie.demo', role: 'Coordinator', initials: 'CG', isDemoAccount: true },
  { id: 'marc', name: 'Marc-André Bouchard', email: 'charge.nord@vigie.demo', role: 'SectorManager', initials: 'MB', isDemoAccount: true },
  { id: 'elodie', name: 'Élodie Martel', email: 'regie@vigie.demo', role: 'AquaticDirector', initials: 'EM', isDemoAccount: true },
]

function toUiUser(user: UserSummary): DemoUser {
  const demoUser = demoUsers.find((candidate) => candidate.email.toLowerCase() === user.email.toLowerCase())
  return {
    id: demoUser?.id ?? user.id,
    apiId: user.id,
    name: user.name,
    email: user.email,
    role: user.role,
    initials: initials(user.name),
    organizationId: user.organizationId,
    siteId: user.siteId,
    sectorId: user.sectorId,
    isDemoAccount: user.isDemoAccount,
  }
}

const demoSites: SiteResponse[] = [
  { id: '20000000-0000-0000-0000-000000000001', name: 'Piscine du Nord', type: 'Indoor', timeZoneId: 'Eastern Standard Time', openingSeason: { startMonth: 1, startDay: 1, endMonth: 12, endDay: 31 }, address: 'Site de démonstration', neighborhood: 'Laval', isMunicipal: false },
  { id: '20000000-0000-0000-0000-000000000002', name: 'Bassin du parc', type: 'Outdoor', timeZoneId: 'Eastern Standard Time', openingSeason: { startMonth: 5, startDay: 15, endMonth: 9, endDay: 15 }, address: 'Site de démonstration', neighborhood: 'Laval', isMunicipal: false },
]
const demoSectors: SectorResponse[] = [
  { id: '80000000-0000-0000-0000-000000000001', organizationId: '00000000-0000-0000-0000-000000000001', name: 'Secteur Nord', code: 'NORD', isActive: true, createdAtUtc: '2026-01-01T00:00:00Z', updatedAtUtc: '2026-01-01T00:00:00Z' },
  { id: '80000000-0000-0000-0000-000000000002', organizationId: '00000000-0000-0000-0000-000000000001', name: 'Secteur du parc', code: 'PARC', isActive: true, createdAtUtc: '2026-01-01T00:00:00Z', updatedAtUtc: '2026-01-01T00:00:00Z' },
]
const availabilityDays = [
  { date: '2026-09-07', day: 'LUN', number: '7' },
  { date: '2026-09-08', day: 'MAR', number: '8' },
  { date: '2026-09-09', day: 'MER', number: '9' },
  { date: '2026-09-10', day: 'JEU', number: '10' },
  { date: '2026-09-11', day: 'VEN', number: '11' },
  { date: '2026-09-12', day: 'SAM', number: '12' },
  { date: '2026-09-13', day: 'DIM', number: '13' },
]
const initialShifts: Shift[] = [
  { id: 's1', day: 'MAR', date: 8, start: '09:00', end: '17:00', site: 'Piscine du Nord', siteKind: 'Intérieur', status: 'assigné', colleagues: ['NT', 'CG'], requiredLifeguards: 2 },
  { id: 's2', day: 'MER', date: 9, start: '13:00', end: '21:00', site: 'Piscine du Nord', siteKind: 'Intérieur', status: 'disponible', colleagues: ['NT'], requiredLifeguards: 2 },
  { id: 's3', day: 'VEN', date: 11, start: '14:00', end: '22:00', site: 'Bassin du parc', siteKind: 'Extérieur', status: 'assigné', colleagues: ['SN', 'CG'], requiredLifeguards: 2 },
  { id: 's4', day: 'SAM', date: 12, start: '12:00', end: '20:00', site: 'Piscine du Nord', siteKind: 'Intérieur', status: 'disponible', colleagues: [], requiredLifeguards: 2 },
]
const initialSwaps: Swap[] = [{ id: 'swap-1', shiftId: 's1', shiftLabel: 'Mardi 8 sept. · 09:00–17:00', requester: 'Amélie Roy', receiver: 'Noah Tremblay', status: 'En attente', requestedAt: '2026-09-04T15:30:00Z' }]

const frenchDays = ['DIM', 'LUN', 'MAR', 'MER', 'JEU', 'VEN', 'SAM']
const swapStatuses: Record<string, Swap['status']> = { Pending: 'En attente', Approved: 'Approuvé', Rejected: 'Refusé', Cancelled: 'Refusé' }

function initials(name: string) {
  return name.split(' ').map((part) => part[0]).join('').slice(0, 2).toUpperCase()
}

function formatTime(value: string) {
  return new Intl.DateTimeFormat('fr-CA', { hour: '2-digit', minute: '2-digit', hour12: false }).format(new Date(value))
}

function formatCertificationDate(value: string) {
  return new Intl.DateTimeFormat('fr-CA', { day: 'numeric', month: 'short', year: 'numeric' }).format(new Date(`${value}T12:00:00`))
}

function formatSwapDate(value?: string) {
  if (!value) return 'Demande reçue récemment'
  return new Intl.DateTimeFormat('fr-CA', { day: 'numeric', month: 'long', hour: '2-digit', minute: '2-digit' }).format(new Date(value))
}

const auditActionLabels: Record<string, string> = {
  'organization.created': 'Organisation créée',
  'account.password_changed': 'Mot de passe modifié',
  'invitation.created': 'Invitation créée',
  'member.joined': 'Membre activé',
  'site.created': 'Site créé',
  'shift.created': 'Quart créé',
  'assignment.created': 'Assignation créée',
  'swap.created': 'Échange demandé',
  'swap.approved': 'Échange approuvé',
  'swap.rejected': 'Échange refusé',
  'sector.created': 'Secteur créé',
  'sector.updated': 'Secteur modifié',
  'membership.created': 'Affectation créée',
  'membership.updated': 'Affectation modifiée',
  'membership.deactivated': 'Affectation désactivée',
}

function formatAuditAction(action: string) {
  return auditActionLabels[action] ?? action.replaceAll('.', ' · ')
}

function formatAuditDate(value: string) {
  return new Intl.DateTimeFormat('fr-CA', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value))
}

function tomorrowInputValue() {
  const date = new Date()
  date.setDate(date.getDate() + 1)
  return date.toISOString().slice(0, 10)
}

function readInvitationToken() {
  return typeof window === 'undefined' ? '' : new URLSearchParams(window.location.search).get('invitation') ?? ''
}

function toUiShift(response: ShiftResponse, employeeId: string): Shift {
  const start = new Date(response.startUtc)
  const assignment = response.assignments.find((item) => item.employeeId === employeeId)
  return {
    id: response.id,
    assignmentId: assignment?.id,
    day: frenchDays[start.getDay()],
    date: start.getDate(),
    start: formatTime(response.startUtc),
    end: formatTime(response.endUtc),
    site: response.siteName,
    siteKind: response.siteType === 'Outdoor' ? 'Extérieur' : 'Intérieur',
    status: assignment ? 'assigné' : 'disponible',
    lifecycleStatus: response.status ?? 'Open',
    colleagues: response.assignments.map((item) => initials(item.employeeName)),
    requiredLifeguards: response.requiredLifeguards,
    assignments: response.assignments,
  }
}

function toUiSwap(response: SwapRequestResponse): Swap {
  return {
    id: response.id,
    shiftId: response.assignmentId,
    requester: response.requesterName,
    receiver: response.receiverName,
    shiftLabel: response.shiftLabel,
    status: swapStatuses[response.status] ?? 'En attente',
    requestedAt: response.requestedAtUtc,
  }
}

function Icon({ name }: { name: 'calendar' | 'users' | 'swap' | 'shield' | 'history' | 'plus' | 'arrow' | 'check' | 'close' | 'menu' | 'bell' }) {
  const paths: Record<string, ReactNode> = {
    calendar: <><rect x="3" y="4" width="18" height="17" rx="2" /><path d="M16 2v4M8 2v4M3 10h18" /></>,
    users: <><circle cx="9" cy="8" r="3" /><path d="M3 20c.5-3 2.5-5 6-5s5.5 2 6 5M16 5.5a3 3 0 0 1 0 5.8M17 15c2.4.4 3.6 2 4 4" /></>,
    swap: <><path d="M7 7h11l-3-3M17 17H6l3 3" /><path d="M18 4v3M6 17v-3" /></>,
    shield: <><path d="M12 3 20 6v5c0 5-3.4 8.2-8 10-4.6-1.8-8-5-8-10V6l8-3Z" /><path d="m8.5 12 2.3 2.3 4.7-4.7" /></>,
    history: <><path d="M3 12a9 9 0 1 0 3-6.7" /><path d="M3 4v5h5M12 7v5l3 2" /></>,
    plus: <><path d="M12 5v14M5 12h14" /></>,
    arrow: <><path d="M5 12h14M13 6l6 6-6 6" /></>,
    check: <path d="m5 12 4 4L19 6" />,
    close: <><path d="m6 6 12 12M18 6 6 18" /></>,
    menu: <><path d="M4 7h16M4 12h16M4 17h16" /></>,
    bell: <><path d="M18 9a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9M10 21h4" /></>,
  }
  return <svg className="icon" viewBox="0 0 24 24" aria-hidden="true">{paths[name]}</svg>
}

function App() {
  const [currentUser, setCurrentUser] = useState<DemoUser>(demoUsers[0])
  const [authBootstrapped, setAuthBootstrapped] = useState(() => !apiConfigured || !localStorage.getItem('vigie.token'))
  const [authModal, setAuthModal] = useState<AuthMode | null>(() => readInvitationToken() ? 'accept-invitation' : null)
  const [invitationToken] = useState(readInvitationToken)
  const [passwordModalOpen, setPasswordModalOpen] = useState(false)
  const [passwordSubmitting, setPasswordSubmitting] = useState(false)
  const [passwordError, setPasswordError] = useState('')
  const [passwordForm, setPasswordForm] = useState({ current: '', next: '' })
  const [authSubmitting, setAuthSubmitting] = useState(false)
  const [authError, setAuthError] = useState('')
  const [authForm, setAuthForm] = useState({ organizationName: '', name: '', email: '', password: '' })
  const [inviteModalOpen, setInviteModalOpen] = useState(false)
  const [inviteSubmitting, setInviteSubmitting] = useState(false)
  const [inviteError, setInviteError] = useState('')
  const [inviteLink, setInviteLink] = useState('')
  const [inviteForm, setInviteForm] = useState<InviteDraft>({ name: '', email: '', role: 'Lifeguard', siteId: '', sectorId: '' })
  const [view, setView] = useState<View>('calendar')
  const [mobileNavOpen, setMobileNavOpen] = useState(false)
  const [selectedShift, setSelectedShift] = useState<Shift | null>(null)
  const [swapModalOpen, setSwapModalOpen] = useState(false)
  const [shifts, setShifts] = useState(initialShifts)
  const [swaps, setSwaps] = useState(initialSwaps)
  const [certifications, setCertifications] = useState<CertificationResponse[] | null>(null)
  const [employees, setEmployees] = useState<UserSummary[] | null>(null)
  const [memberships, setMemberships] = useState<MembershipResponse[] | null>(null)
  const [sectors, setSectors] = useState<SectorResponse[]>([])
  const [availabilities, setAvailabilities] = useState<AvailabilityResponse[] | null>(null)
  const [auditEntries, setAuditEntries] = useState<AuditEntryResponse[]>([])
  const [auditExporting, setAuditExporting] = useState(false)
  const [sites, setSites] = useState<SiteResponse[]>([])
  const [toast, setToast] = useState('')
  const [apiState, setApiState] = useState<'inactive' | 'loading' | 'ready' | 'error'>(apiConfigured ? 'loading' : 'inactive')
  const [apiEmployeeIds, setApiEmployeeIds] = useState<Record<string, string>>({})
  const [createShiftOpen, setCreateShiftOpen] = useState(false)
  const [createShiftSubmitting, setCreateShiftSubmitting] = useState(false)
  const [createShiftErrors, setCreateShiftErrors] = useState<Partial<Record<keyof CreateShiftDraft | 'form', string>>>({})
  const [createShiftDraft, setCreateShiftDraft] = useState<CreateShiftDraft>({ siteId: '', date: tomorrowInputValue(), startTime: '09:00', endTime: '17:00', requiredLifeguards: 2 })
  const [swapFilter, setSwapFilter] = useState<SwapFilter>('all')
  const [selectedSwap, setSelectedSwap] = useState<Swap | null>(null)
  const [decidingSwapId, setDecidingSwapId] = useState<string | null>(null)
  const [assignmentModalOpen, setAssignmentModalOpen] = useState(false)
  const [assignmentEmployeeId, setAssignmentEmployeeId] = useState('')
  const [assignmentSubmitting, setAssignmentSubmitting] = useState(false)
  const [assignmentError, setAssignmentError] = useState('')
  const [shiftActionSubmitting, setShiftActionSubmitting] = useState(false)
  const [availabilitySavingDate, setAvailabilitySavingDate] = useState<string | null>(null)
  const [siteQuery, setSiteQuery] = useState('')
  const [siteTypeFilter, setSiteTypeFilter] = useState<SiteTypeFilter>('all')
  const syncGeneration = useRef(0)
  const isManagement = currentUser.role !== 'Lifeguard'
  const pendingSwaps = swaps.filter((swap) => swap.status === 'En attente')
  const assigned = shifts.filter((shift) => shift.status === 'assigné')
  const availableSites = useMemo(() => sites.length > 0 ? sites : currentUser.isDemoAccount ? demoSitesForRole(currentUser.role, demoSites) : [], [currentUser.isDemoAccount, currentUser.role, sites])
  const filteredSites = useMemo(() => filterSites(availableSites, { query: siteQuery, type: siteTypeFilter }), [availableSites, siteQuery, siteTypeFilter])
  const availableSectors = sectors.length > 0 ? sectors : currentUser.isDemoAccount ? demoSectors : []
  const inviteRoles: Role[] = currentUser.role === 'AquaticDirector'
    ? ['Lifeguard', 'PoolChief', 'SectorManager', 'AquaticDirector']
    : currentUser.role === 'SectorManager'
      ? ['Lifeguard', 'PoolChief']
      : ['Lifeguard']
  const allVisibleSwaps = useMemo(() => isManagement ? swaps : swaps.filter((swap) => swap.requester === currentUser.name || swap.receiver === currentUser.name), [currentUser.name, isManagement, swaps])
  const visibleSwaps = useMemo(() => filterSwaps(allVisibleSwaps, swapFilter), [allVisibleSwaps, swapFilter])
  const teamMembers = employees ?? (currentUser.isDemoAccount ? demoUsers.map(({ id, name, email, role }) => ({ id, name, email, role })) : [])
  const currentUserId = currentUser.id
  const currentUserEmail = currentUser.email
  const currentUserName = currentUser.name
  const currentUserRole = currentUser.role
  const currentUserOrganizationId = currentUser.organizationId
  const currentUserIsDemo = currentUser.isDemoAccount

  useEffect(() => {
    if (!apiConfigured) return
    const storedToken = localStorage.getItem('vigie.token')
    if (!storedToken) return
    let active = true
    async function restoreSession() {
      try {
        const apiUser = await vigieApi.me()
        if (active) setCurrentUser(toUiUser(apiUser))
      } catch {
        localStorage.removeItem('vigie.token')
        if (active) setCurrentUser(demoUsers[0])
      } finally {
        if (active) setAuthBootstrapped(true)
      }
    }
    void restoreSession()
    return () => { active = false }
  }, [])

  useEffect(() => {
    if (!apiConfigured || !authBootstrapped) return
    let active = true
    const generation = ++syncGeneration.current
    const isCurrent = () => active && generation === syncGeneration.current
    async function syncApi() {
      try {
        const login = currentUserIsDemo
          ? await vigieApi.login(currentUserEmail, 'vigie-demo')
          : null
        if (login) localStorage.setItem('vigie.token', login.token)
        const [apiShifts, apiSwaps, employees, apiCertifications, apiSites, apiAvailabilities, apiSectors, apiMembers, apiAudit] = await Promise.all([vigieApi.shifts(), vigieApi.swaps(), vigieApi.employees(), vigieApi.certifications(), vigieApi.sites(), vigieApi.availability(), vigieApi.sectors(), vigieApi.members(), isManagement ? vigieApi.audit() : Promise.resolve([])])
        if (!isCurrent()) return
        setApiEmployeeIds(Object.fromEntries(employees.map((employee) => [employee.email, employee.id])))
        setEmployees(employees)
        setMemberships(apiMembers)
        setSectors(apiSectors)
        const apiUser = login?.user ?? { id: currentUserId, name: currentUserName, email: currentUserEmail, role: currentUserRole, organizationId: currentUserOrganizationId ?? '', isDemoAccount: currentUserIsDemo, siteId: currentUser.siteId, sectorId: currentUser.sectorId }
        setCurrentUser((user) => ({ ...user, apiId: apiUser.id, name: apiUser.name, role: apiUser.role, organizationId: apiUser.organizationId, siteId: apiUser.siteId, sectorId: apiUser.sectorId, isDemoAccount: apiUser.isDemoAccount }))
        setShifts(apiShifts.map((shift) => toUiShift(shift, apiUser.id)))
        setSwaps(apiSwaps.map(toUiSwap))
        setCertifications(apiCertifications)
        setSites(apiSites)
        setAvailabilities(apiAvailabilities)
        setAuditEntries(apiAudit)
        setApiState('ready')
      } catch (error) {
        if (!isCurrent()) return
        setApiState('error')
        setShifts(initialShifts)
        setSwaps(initialSwaps)
        setSites([])
        setSectors([])
        setEmployees(null)
        setMemberships(null)
        setCertifications(null)
        setAvailabilities(null)
        setAuditEntries([])
        setToast(error instanceof Error ? `API indisponible : ${error.message}` : 'API indisponible : mode démo local')
        window.setTimeout(() => setToast(''), 3600)
      }
    }
    void syncApi()
    return () => { active = false }
  }, [authBootstrapped, currentUserEmail, currentUserId, currentUserIsDemo, currentUserName, currentUserOrganizationId, currentUserRole, currentUser.siteId, currentUser.sectorId, isManagement])

  function flash(message: string) { setToast(message); window.setTimeout(() => setToast(''), 2800) }
  async function exportAudit() {
    if (auditExporting) return
    setAuditExporting(true)
    try {
      const blob = await vigieApi.exportAudit()
      const url = URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = 'vigie-historique.csv'
      link.click()
      URL.revokeObjectURL(url)
      flash('Historique exporté en CSV')
    } catch (error) {
      flash(error instanceof Error ? error.message : 'L’export ne peut pas être généré.')
    } finally {
      setAuditExporting(false)
    }
  }
  function selectUser(id: string) { const user = demoUsers.find((candidate) => candidate.id === id); if (user) { syncGeneration.current += 1; localStorage.removeItem('vigie.token'); if (apiConfigured) setApiState('loading'); setSites([]); setSectors([]); setEmployees(null); setMemberships(null); setCertifications(null); setAvailabilities(null); setAuditEntries([]); setCurrentUser(user); setView('calendar'); flash(`Profil de démonstration : ${user.name}`) } }
  function openAuth(mode: 'login' | 'register') { setAuthError(''); setAuthForm({ organizationName: '', name: '', email: '', password: '' }); setAuthModal(mode) }
  function closeAuth() { if (!authSubmitting) { setAuthModal(null); setAuthError('') } }
  async function submitAuth(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!apiConfigured || authSubmitting) return
    setAuthSubmitting(true); setAuthError('')
    try {
      const mode = authModal
      const result = mode === 'register'
        ? (await vigieApi.register(authForm.organizationName, authForm.name, authForm.email, authForm.password)).login
        : mode === 'accept-invitation'
          ? await vigieApi.acceptInvitation(invitationToken, authForm.name, authForm.password)
          : await vigieApi.login(authForm.email, authForm.password)
      localStorage.setItem('vigie.token', result.token)
      setCurrentUser(toUiUser(result.user))
      setAuthBootstrapped(true)
      if (mode === 'accept-invitation') window.history.replaceState({}, document.title, window.location.pathname)
      setAuthModal(null); setView('calendar'); setApiState('loading'); flash(mode === 'register' ? 'Votre espace Vigie est prêt.' : mode === 'accept-invitation' ? 'Votre compte est activé.' : 'Connexion réussie.')
    } catch (error) { setAuthError(error instanceof Error ? error.message : 'La connexion ne peut pas être établie.') }
    finally { setAuthSubmitting(false) }
  }
  function logout() { localStorage.removeItem('vigie.token'); setCurrentUser(demoUsers[0]); setView('calendar'); setApiState(apiConfigured ? 'loading' : 'inactive'); flash('Session fermée') }
  function openPasswordModal() { setPasswordError(''); setPasswordForm({ current: '', next: '' }); setPasswordModalOpen(true) }
  function closePasswordModal() { if (!passwordSubmitting) { setPasswordModalOpen(false); setPasswordError('') } }
  async function submitPassword(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (passwordSubmitting) return
    setPasswordSubmitting(true); setPasswordError('')
    try {
      const result = await vigieApi.changePassword(passwordForm.current, passwordForm.next)
      localStorage.setItem('vigie.token', result.token)
      setCurrentUser(toUiUser(result.user))
      setPasswordModalOpen(false)
      flash('Mot de passe mis à jour. Les autres sessions sont fermées.')
    } catch (error) { setPasswordError(error instanceof Error ? error.message : 'Le mot de passe ne peut pas être mis à jour.') }
    finally { setPasswordSubmitting(false) }
  }
  function openInvite() { setInviteError(''); setInviteLink(''); setInviteForm({ name: '', email: '', role: 'Lifeguard', siteId: availableSites[0]?.id ?? '', sectorId: availableSectors[0]?.id ?? '' }); setInviteModalOpen(true) }
  async function submitInvite(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (inviteSubmitting) return
    setInviteSubmitting(true); setInviteError('')
    try {
      const needsSite = inviteForm.role === 'Lifeguard' || inviteForm.role === 'PoolChief'
      const needsSector = inviteForm.role === 'SectorManager'
      if (needsSite && !inviteForm.siteId) throw new Error('Choisissez la piscine de rattachement.')
      if (needsSector && !inviteForm.sectorId) throw new Error('Choisissez le secteur de rattachement.')
      const invitation = await vigieApi.inviteMember(inviteForm.email, inviteForm.name, inviteForm.role, needsSite ? inviteForm.siteId : undefined, needsSector ? inviteForm.sectorId : undefined)
      setInviteLink(invitation.inviteLink ?? invitation.inviteToken ?? '')
      setInviteForm({ name: '', email: '', role: 'Lifeguard', siteId: availableSites[0]?.id ?? '', sectorId: availableSectors[0]?.id ?? '' })
      flash('Invitation créée pour votre équipe')
    } catch (error) { setInviteError(error instanceof Error ? error.message : 'L’invitation ne peut pas être créée.') }
    finally { setInviteSubmitting(false) }
  }
  function openCreateShift() {
    if (!isManagement) { flash('La création de quart est réservée aux responsables autorisés.'); return }
    setCreateShiftErrors({})
    setCreateShiftDraft((draft) => ({ ...draft, siteId: draft.siteId || availableSites[0]?.id || '' }))
    setCreateShiftOpen(true)
  }
  function closeCreateShift() {
    if (createShiftSubmitting) return
    setCreateShiftOpen(false)
    setCreateShiftErrors({})
  }
  function openAssignmentModal(shift: Shift) {
    if (!isManagement) { flash('La gestion des assignations est réservée aux responsables autorisés.'); return }
    const assignedIds = new Set(shift.assignments?.map((assignment) => assignment.employeeId) ?? [])
    const firstAvailable = teamMembers.find((member) => member.role === 'Lifeguard' && !assignedIds.has(member.id))
    setAssignmentEmployeeId(firstAvailable?.id ?? '')
    setAssignmentError('')
    setAssignmentModalOpen(true)
  }
  function closeAssignmentModal() {
    if (assignmentSubmitting) return
    setAssignmentModalOpen(false)
    setAssignmentError('')
  }
  async function cancelSelectedShift() {
    if (!selectedShift || shiftActionSubmitting || selectedShift.lifecycleStatus === 'Cancelled' || !apiConfigured) return
    setShiftActionSubmitting(true)
    try {
      const cancelled = await vigieApi.cancelShift(selectedShift.id)
      const next = toUiShift(cancelled, currentUser.apiId ?? currentUser.id)
      setShifts((items) => items.map((item) => item.id === next.id ? next : item))
      setSelectedShift(next)
      flash('Quart annulé. Les assignations restent visibles dans l’historique.')
    } catch (error) {
      flash(error instanceof Error ? error.message : 'Le quart ne peut pas être annulé.')
    } finally {
      setShiftActionSubmitting(false)
    }
  }
  async function submitAssignment() {
    if (!selectedShift || !assignmentEmployeeId || assignmentSubmitting) return
    const employee = teamMembers.find((member) => member.id === assignmentEmployeeId)
    if (!employee) return
    setAssignmentSubmitting(true)
    setAssignmentError('')
    try {
      if (apiConfigured && apiState === 'ready') {
        await vigieApi.assignShift(selectedShift.id, assignmentEmployeeId)
        const refreshed = await vigieApi.shifts()
        const updatedShifts = refreshed.map((shift) => toUiShift(shift, currentUser.apiId ?? ''))
        setShifts(updatedShifts)
        setSelectedShift(updatedShifts.find((shift) => shift.id === selectedShift.id) ?? selectedShift)
      } else {
        const assignment = { id: `local-assignment-${Date.now()}`, shiftId: selectedShift.id, employeeId: employee.id, employeeName: employee.name }
        const updated = { ...selectedShift, colleagues: [...selectedShift.colleagues, initials(employee.name)], assignments: [...(selectedShift.assignments ?? []), assignment] }
        setShifts((current) => current.map((shift) => shift.id === selectedShift.id ? updated : shift))
        setSelectedShift(updated)
      }
      setAssignmentModalOpen(false)
      flash(`${employee.name} a été assigné au quart`)
    } catch (error) {
      setAssignmentError(error instanceof Error ? error.message : 'L’assignation ne peut pas être enregistrée. Vérifiez les certifications, le chevauchement et le quota.')
    } finally {
      setAssignmentSubmitting(false)
    }
  }
  async function toggleAvailability(date: string) {
    if (availabilitySavingDate) return
    const current = availabilities?.find((availability) => availability.date === date)
    const isAvailable = !(current?.isAvailable ?? true)
    setAvailabilitySavingDate(date)
    try {
      const updated = apiConfigured && apiState === 'ready'
        ? await vigieApi.setAvailability(date, isAvailable)
        : { id: `local-availability-${date}`, employeeId: currentUser.apiId ?? currentUser.id, date, isAvailable, note: null }
      setAvailabilities((currentRows) => [...(currentRows ?? []).filter((row) => row.date !== date), updated].sort((a, b) => a.date.localeCompare(b.date)))
      flash(isAvailable ? `Disponible le ${date}` : `Indisponible le ${date}`)
    } catch (error) {
      flash(error instanceof Error ? error.message : 'La disponibilité ne peut pas être enregistrée.')
    } finally {
      setAvailabilitySavingDate(null)
    }
  }
  async function submitCreateShift(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const errors = validateCreateShiftDraft(createShiftDraft)
    setCreateShiftErrors(errors)
    if (Object.keys(errors).length > 0) return

    setCreateShiftSubmitting(true)
    try {
      const input = createShiftRequest(createShiftDraft)
      if (apiConfigured && apiState === 'ready') {
        const created = await vigieApi.createShift(input)
        setShifts((current) => [...current, toUiShift(created, currentUser.apiId ?? '')])
      } else {
        const start = new Date(`${createShiftDraft.date}T${createShiftDraft.startTime}:00`)
        const site = availableSites.find((candidate) => candidate.id === createShiftDraft.siteId)
        setShifts((current) => [...current, {
          id: `local-${Date.now()}`,
          day: frenchDays[start.getDay()],
          date: start.getDate(),
          start: createShiftDraft.startTime,
          end: createShiftDraft.endTime,
          site: site?.name ?? 'Nouveau site',
          siteKind: site?.type === 'Outdoor' ? 'Extérieur' : 'Intérieur',
          status: 'disponible',
          colleagues: [],
          requiredLifeguards: createShiftDraft.requiredLifeguards,
        }])
      }
      setCreateShiftOpen(false)
      flash('Quart créé et ajouté au calendrier')
    } catch (error) {
      setCreateShiftErrors({ form: error instanceof Error ? error.message : 'Le quart ne peut pas être créé.' })
    } finally {
      setCreateShiftSubmitting(false)
    }
  }
  async function createSwap(receiver: string) {
    if (!selectedShift) return
    const receiverUser = teamMembers.find((user) => user.name === receiver)
    if (apiConfigured && apiState === 'ready' && selectedShift.assignmentId && receiverUser) {
      const receiverId = apiEmployeeIds[receiverUser.email]
      if (!receiverId) { flash('Le receveur est introuvable dans l’équipe'); return }
      try {
        const created = await vigieApi.createSwap(selectedShift.assignmentId, receiverId)
        setSwaps((current) => [...current, toUiSwap(created)])
        setSwapModalOpen(false); setSelectedShift(null); flash('Demande envoyée au coordonnateur')
      } catch (error) { flash(error instanceof Error ? error.message : 'La demande ne peut pas être envoyée.') }
      return
    }
    setSwaps((current) => [...current, { id: `swap-${Date.now()}`, shiftId: selectedShift.id, shiftLabel: `${selectedShift.day} ${selectedShift.date} sept. · ${selectedShift.start}–${selectedShift.end}`, requester: currentUser.name, receiver, status: 'En attente' }])
    setSwapModalOpen(false); setSelectedShift(null); flash('Demande envoyée au coordonnateur')
  }
  async function decideSwap(id: string, status: 'Approuvé' | 'Refusé') {
    if (decidingSwapId) return
    setDecidingSwapId(id)
    try {
      if (apiConfigured && apiState === 'ready') {
        const updated = status === 'Approuvé' ? await vigieApi.approveSwap(id) : await vigieApi.rejectSwap(id)
        setSwaps((current) => current.map((swap) => swap.id === id ? toUiSwap(updated) : swap))
      } else {
        setSwaps((current) => current.map((swap) => swap.id === id ? { ...swap, status } : swap))
      }
      flash(status === 'Approuvé' ? 'Échange approuvé et calendrier mis à jour' : 'Échange refusé')
    } catch (error) {
      flash(error instanceof Error ? error.message : 'La décision ne peut pas être enregistrée.')
    } finally {
      setDecidingSwapId(null)
    }
  }

  const certificationRows: CertificationRow[] = certifications === null
    ? demoUsers.filter((user) => user.role === 'Lifeguard').map((user, index) => ({
      id: user.id, initials: user.initials, name: user.name, email: user.email, type: 'Premiers soins',
      expiry: index === 2 ? '30 sept. 2026' : index === 0 ? '19 nov. 2026' : '12 août 2027',
      detail: index === 2 ? 'Expire dans 20 jours' : index === 0 ? 'Expire dans 75 jours' : 'Valide', warning: index === 2,
    }))
    : certifications.map((certification) => {
      const user = demoUsers.find((candidate) => candidate.name === certification.employeeName)
      return {
        id: certification.id, initials: user?.initials ?? initials(certification.employeeName), name: certification.employeeName,
        email: user?.email ?? 'Équipe Vigie', type: certification.type, expiry: formatCertificationDate(certification.expiresOn),
        detail: certification.daysRemaining < 0 ? 'Expirée' : certification.daysRemaining <= 90 ? `Expire dans ${certification.daysRemaining} jours` : 'Valide',
        warning: certification.daysRemaining <= 90,
      }
    })
  const certificationTotal = certifications === null ? 6 : certifications.length
  const certificationValid = certifications === null ? 5 : certifications.filter((certification) => certification.daysRemaining >= 0).length
  const certificationProgress = certificationTotal === 0 ? 0 : Math.round((certificationValid / certificationTotal) * 100)

  return <div className="app-shell">
    <aside className={`sidebar ${mobileNavOpen ? 'is-open' : ''}`}>
      <div className="brand"><span className="brand-mark"><span>V</span></span><span>Vigie</span></div><div className="workspace-label">CENTRE AQUATIQUE</div>
      <nav aria-label="Navigation principale">
        <button className={view === 'calendar' ? 'nav-item active' : 'nav-item'} onClick={() => { setView('calendar'); setMobileNavOpen(false) }}><Icon name="calendar" /><span>Mon calendrier</span></button>
        <button className={view === 'swaps' ? 'nav-item active' : 'nav-item'} onClick={() => { setView('swaps'); setMobileNavOpen(false) }}><Icon name="swap" /><span>Échanges</span>{pendingSwaps.length > 0 && <b className="nav-count">{pendingSwaps.length}</b>}</button>
        <button className={view === 'certifications' ? 'nav-item active' : 'nav-item'} onClick={() => { setView('certifications'); setMobileNavOpen(false) }}><Icon name="shield" /><span>Certifications</span><span className="nav-dot" /></button>
        <button className={view === 'team' ? 'nav-item active' : 'nav-item'} onClick={() => { setView('team'); setMobileNavOpen(false) }}><Icon name="users" /><span>Équipe</span></button>
        {isManagement && <button className={view === 'sites' ? 'nav-item active' : 'nav-item'} onClick={() => { setView('sites'); setMobileNavOpen(false) }}><Icon name="shield" /><span>Piscines</span></button>}
        <button className={view === 'availability' ? 'nav-item active' : 'nav-item'} onClick={() => { setView('availability'); setMobileNavOpen(false) }}><Icon name="calendar" /><span>Disponibilités</span></button>
        {isManagement && <button className={view === 'audit' ? 'nav-item active' : 'nav-item'} onClick={() => { setView('audit'); setMobileNavOpen(false) }}><Icon name="history" /><span>Historique</span></button>}
      </nav>
      <div className="sidebar-bottom"><div className="status-line"><span className="status-pulse" />{apiState === 'ready' ? 'API connectée' : apiState === 'loading' ? 'Connexion à l’API…' : apiState === 'error' ? 'Mode démo local' : 'Système opérationnel'}</div><div className="version">Vigie MVP · v0.1.0</div></div>
    </aside>
    <main className="main-content">
      <header className="topbar"><button className="mobile-menu" onClick={() => setMobileNavOpen((open) => !open)} aria-label="Ouvrir le menu"><Icon name="menu" /></button><div className="breadcrumbs"><span>Vigie</span><span className="crumb-separator">/</span><strong>{view === 'calendar' ? 'Mon calendrier' : view === 'swaps' ? 'Échanges' : view === 'certifications' ? 'Certifications' : view === 'team' ? 'Équipe' : view === 'sites' ? 'Piscines' : view === 'audit' ? 'Historique' : 'Disponibilités'}</strong></div><div className="topbar-actions"><button className="icon-button" aria-label="Notifications"><Icon name="bell" /><span className="notification-dot" /></button>{currentUser.isDemoAccount ? <div className="profile-switcher"><span className="avatar">{currentUser.initials}</span><select aria-label="Profil de démonstration" value={currentUser.id} onChange={(event) => selectUser(event.target.value)}>{demoUsers.map((user) => <option key={user.id} value={user.id}>{user.name} · {roleLabel(user.role)}</option>)}</select><button className="account-link" onClick={() => openAuth('login')}>Se connecter</button></div> : <div className="profile-switcher"><span className="avatar">{currentUser.initials}</span><span className="account-name">{currentUser.name} · {roleLabel(currentUser.role)}</span><button className="account-link" onClick={openPasswordModal}>Compte</button><button className="account-link" onClick={logout}>Déconnexion</button></div>}</div></header>
      <div className="content-wrap">
        <div className="page-heading"><div><p className="eyebrow">SEMAINE DU 7 AU 13 SEPTEMBRE 2026</p><h1>{view === 'calendar' ? (isManagement ? 'Vue équipe' : `Bonjour, ${currentUser.name.split(' ')[0]}`) : view === 'swaps' ? 'Demandes d’échange' : view === 'certifications' ? 'Certifications' : view === 'team' ? 'Équipe' : view === 'sites' ? 'Piscines municipales' : view === 'audit' ? 'Historique' : 'Disponibilités'}</h1><p className="page-subtitle">{view === 'calendar' ? (isManagement ? 'Gardez une vue claire sur les quarts et les remplacements.' : 'Voici vos quarts et les disponibilités de votre équipe.') : view === 'swaps' ? 'Chaque remplacement reste sous contrôle jusqu’à votre approbation.' : view === 'certifications' ? 'La bonne certification, au bon moment, pour chaque quart.' : view === 'team' ? 'Les personnes autorisées et leur état de préparation pour les quarts.' : view === 'sites' ? 'Les installations, secteurs et saisons d’ouverture de votre organisation.' : view === 'audit' ? 'Les opérations importantes de votre organisation, avec leur acteur et leur horodatage.' : 'Indiquez les jours où vous pouvez prendre un quart.'}</p></div>{view === 'calendar' && isManagement && <button className="primary-button" onClick={openCreateShift}><Icon name="plus" />Créer un quart</button>}{view === 'team' && isManagement && <button className="primary-button" onClick={openInvite}><Icon name="plus" />Inviter un membre</button>}</div>
        {view === 'calendar' && <><section className="alert-banner"><div className="alert-icon"><Icon name="shield" /></div><div><strong>Certification à surveiller</strong><span>La certification Premiers soins de {currentUser.id === 'sofia' ? 'Sofia Nguyen' : 'votre dossier'} expire dans {currentUser.id === 'sofia' ? 20 : 75} jours.</span></div><button onClick={() => setView('certifications')}>Voir les détails <Icon name="arrow" /></button></section><div className="metric-row"><div className="metric"><span className="metric-label">QUARTS CETTE SEMAINE</span><strong>{assigned.length + 1}</strong><span className="metric-meta positive">↑ 1 vs. semaine dernière</span></div><div className="metric"><span className="metric-label">HEURES PLANIFIÉES</span><strong>24<span className="metric-unit">h</span></strong><span className="metric-meta">Quota : {isManagement ? '40' : '24'} h</span></div><div className="metric"><span className="metric-label">ÉCHANGES EN ATTENTE</span><strong>{pendingSwaps.length}</strong><span className="metric-meta">Dernière mise à jour il y a 8 min</span></div></div><section className="calendar-section"><div className="section-header"><div><h2>Cette semaine</h2><p>Votre planning du lundi 7 au dimanche 13 septembre</p></div><div className="week-controls"><button aria-label="Semaine précédente">←</button><button className="today-button">Aujourd’hui</button><button aria-label="Semaine suivante">→</button></div></div><div className="calendar-grid"><div className="time-column"><span /><span>08:00</span><span>12:00</span><span>16:00</span><span>20:00</span></div>{['LUN','MAR','MER','JEU','VEN','SAM','DIM'].map((day) => <div className={`day-column ${day === 'MAR' ? 'today' : ''}`} key={day}><div className="day-header"><span>{day}</span><strong>{day === 'MAR' ? '8' : day === 'MER' ? '9' : day === 'VEN' ? '11' : day === 'SAM' ? '12' : '•'}</strong></div><div className="day-body">{shifts.filter((shift) => shift.day === day).map((shift) => <button className={`shift-block ${shift.status}`} key={shift.id} onClick={() => setSelectedShift(shift)}><span className="shift-time">{shift.start} – {shift.end}</span><strong>{shift.site}</strong><small>{shift.siteKind}</small><span className="shift-people">{shift.colleagues.map((colleague) => <i key={colleague}>{colleague}</i>)}</span></button>)}</div></div>)}</div></section><div className="lower-grid"><section className="list-section"><div className="section-header compact"><div><h2>Échanges en attente</h2><p>Les demandes à traiter</p></div><button className="text-button" onClick={() => setView('swaps')}>Tout voir <Icon name="arrow" /></button></div>{pendingSwaps.length === 0 ? <div className="empty-state">Aucune demande en attente.</div> : pendingSwaps.map((swap) => <div className="swap-row" key={swap.id}><div className="mini-avatar">{swap.requester.split(' ').map((part) => part[0]).join('')}</div><div className="swap-copy"><strong>{swap.requester} <span>→</span> {swap.receiver}</strong><span>{swap.shiftLabel}</span></div><span className="pending-label">En attente</span></div>)}</section><section className="list-section"><div className="section-header compact"><div><h2>À venir</h2><p>Prochains quarts assignés</p></div></div>{shifts.slice(0, 2).map((shift) => <div className="upcoming-row" key={shift.id}><div className="date-block"><strong>{shift.date}</strong><span>{shift.day}</span></div><div><strong>{shift.site}</strong><span>{shift.start} – {shift.end}</span></div><span className="assigned-mark"><Icon name="check" /></span></div>)}</section></div></>}
        {view === 'swaps' && <section className="full-section"><div className="filter-bar"><button className={`filter ${swapFilter === 'all' ? 'active' : ''}`} onClick={() => setSwapFilter('all')}>Toutes <span>{allVisibleSwaps.length}</span></button><button className={`filter ${swapFilter === 'pending' ? 'active' : ''}`} onClick={() => setSwapFilter('pending')}>En attente <span>{allVisibleSwaps.filter((swap) => swap.status === 'En attente').length}</span></button><button className={`filter ${swapFilter === 'processed' ? 'active' : ''}`} onClick={() => setSwapFilter('processed')}>Traitées <span>{allVisibleSwaps.filter((swap) => swap.status !== 'En attente').length}</span></button></div><div className="swaps-table"><div className="table-head"><span>DEMANDE</span><span>QUART</span><span>STATUT</span><span>ACTION</span></div>{visibleSwaps.length === 0 ? <div className="empty-state">Aucune demande dans ce filtre.</div> : visibleSwaps.map((swap) => <div className="table-row" key={swap.id}><div className="person-cell"><span className="mini-avatar">{swap.requester.split(' ').map((part) => part[0]).join('')}</span><div><strong>{swap.requester} <span>→</span> {swap.receiver}</strong><small>{formatSwapDate(swap.requestedAt)}</small></div></div><span>{swap.shiftLabel}</span><span className={`status-tag ${swap.status.toLowerCase().replace('é', 'e')}`}>{swap.status}</span><div className="row-actions">{isManagement && swap.status === 'En attente' && <><button className="approve-button" disabled={decidingSwapId === swap.id} onClick={() => decideSwap(swap.id, 'Approuvé')}>{decidingSwapId === swap.id ? '…' : 'Approuver'}</button><button className="reject-button" disabled={decidingSwapId === swap.id} onClick={() => decideSwap(swap.id, 'Refusé')}>Refuser</button></>}<button className="detail-button" onClick={() => setSelectedSwap(swap)}>Détails <Icon name="arrow" /></button></div></div>)}</div></section>}
        {view === 'certifications' && <section className="full-section certification-page">
          <div className="cert-summary"><div><span className="metric-label">CERTIFICATIONS À JOUR</span><strong>{certificationValid}<span>/{certificationTotal}</span></strong></div><div className="progress-track"><span style={{ width: `${certificationProgress}%` }} /></div><p>{certificationRows.some((certification) => certification.warning) ? 'Une certification nécessite votre attention dans les 90 prochains jours.' : 'Toutes les certifications affichées sont à jour.'}</p></div>
          <div className="cert-list">{certificationRows.map((certification) => <div className="cert-row" key={certification.id}><span className="mini-avatar">{certification.initials}</span><div className="cert-person"><strong>{certification.name}</strong><span>{certification.email}</span></div><div className="cert-name"><strong>{certification.type}</strong><span>Certification requise</span></div><div className={`cert-expiry ${certification.warning ? 'warning' : ''}`}><strong>{certification.expiry}</strong><span>{certification.detail}</span></div><span className={`cert-status ${certification.warning ? 'warning' : 'valid'}`}>{certification.warning ? 'À surveiller' : 'À jour'}</span></div>)}</div>
        </section>}
        {view === 'team' && <section className="full-section team-page"><div className="team-summary"><div><span className="metric-label">MEMBRES ACTIFS</span><strong>{teamMembers.length}</strong><p>Profils chargés depuis l’équipe Vigie.</p></div><div><span className="metric-label">RESPONSABLES</span><strong>{teamMembers.filter((member) => member.role !== 'Lifeguard').length}</strong><p>Accès à la planification selon leur périmètre.</p></div><div><span className="metric-label">SECTEURS ACTIFS</span><strong>{sectors.filter((sector) => sector.isActive).length || 2}</strong><p>Rattachements opérationnels de la régie.</p></div></div><div className="sector-strip"><div><strong>Périmètres de la régie</strong><span>Chaque piscine peut être découpée en secteurs opérationnels.</span></div><div className="sector-list">{(sectors.length > 0 ? sectors : [{ id: 'demo-nord', name: 'Secteur Nord', code: 'NORD', isActive: true } as SectorResponse]).map((sector) => <span className="sector-chip" key={sector.id}><i />{sector.name}<small>{sector.code}</small></span>)}</div></div><div className="team-list"><div className="team-list-head"><span>MEMBRE</span><span>RÔLE</span><span>PÉRIMÈTRE</span><span>CONTACT</span></div>{teamMembers.map((member) => { const memberCertifications = certificationRows.filter((certification) => certification.name === member.name); const hasWarning = memberCertifications.some((certification) => certification.warning); const certificationClass = memberCertifications.length === 0 || hasWarning ? 'warning' : 'valid'; const membership = memberships?.find((item) => item.employeeId === member.id); return <div className="team-row" key={member.id}><div className="person-cell"><span className="mini-avatar">{initials(member.name)}</span><div><strong>{member.name}</strong><small>{member.email}</small></div></div><span className="role-pill">{roleLabel(member.role)}</span><span className={`cert-status ${certificationClass}`}>{membership ? roleScopeLabel(member.role, membership.siteName, membership.sectorName) : memberCertifications.length === 0 ? 'À vérifier' : hasWarning ? 'À surveiller' : 'À jour'}</span><a className="team-email" href={`mailto:${member.email}`}>{member.email}</a></div> })}</div></section>}
        {view === 'sites' && <section className="full-section sites-page"><div className="sites-overview"><div><span className="metric-label">INSTALLATIONS VISIBLES</span><strong>{availableSites.length}</strong><p>Selon le périmètre de {roleLabel(currentUser.role).toLowerCase()}.</p></div><div><span className="metric-label">INTÉRIEURES</span><strong>{availableSites.filter((site) => site.type === 'Indoor').length}</strong><p>Ouvertes toute l’année selon le calendrier du site.</p></div><div><span className="metric-label">EXTÉRIEURES</span><strong>{availableSites.filter((site) => site.type === 'Outdoor').length}</strong><p>Saison estivale, ouverture contrôlée par la régie.</p></div></div><div className="sites-toolbar"><label className="site-search"><span>Rechercher une piscine</span><input aria-label="Rechercher une piscine" value={siteQuery} onChange={(event) => setSiteQuery(event.target.value)} placeholder="Nom, adresse ou quartier" /></label><label className="site-type-filter"><span>Type</span><select aria-label="Filtrer les piscines par type" value={siteTypeFilter} onChange={(event) => setSiteTypeFilter(event.target.value as SiteTypeFilter)}><option value="all">Tous les types</option><option value="Indoor">Intérieures</option><option value="Outdoor">Extérieures</option></select></label><span className="site-result-count">{filteredSites.length} résultat{filteredSites.length === 1 ? '' : 's'}</span></div><div className="sites-grid">{filteredSites.length === 0 ? <div className="empty-state">Aucune piscine ne correspond à vos filtres.</div> : filteredSites.map((site) => <article className="site-card" key={site.id}><div className={`site-icon ${site.type === 'Outdoor' ? 'outdoor' : ''}`}><Icon name="shield" /></div><div className="site-card-copy"><div className="site-card-title"><h2>{site.name}</h2><span>{site.type === 'Outdoor' ? 'Extérieure' : 'Intérieure'}</span></div><p>{site.address || 'Adresse à compléter'}{site.neighborhood ? ` · ${site.neighborhood}` : ''}</p><small>{site.type === 'Outdoor' ? 'Saison estivale' : 'Ouverte toute l’année'} · {site.isMunicipal ? 'Catalogue municipal Laval' : 'Site de démonstration'}</small></div></article>)}</div></section>}
        {view === 'audit' && <section className="full-section audit-page"><div className="section-header"><div><h2>Journal des opérations</h2><p>Les actions sensibles de votre organisation, conservées pour la traçabilité.</p></div><div className="audit-header-actions"><span className="audit-count">{auditEntries.length} opération{auditEntries.length === 1 ? '' : 's'}</span><button className="secondary-button audit-export" onClick={() => { void exportAudit() }} disabled={auditExporting}>{auditExporting ? 'Export…' : 'Exporter CSV'}</button></div></div>{auditEntries.length === 0 ? <div className="empty-state">Aucune opération enregistrée pour le moment.</div> : <div className="audit-list">{auditEntries.map((entry) => <div className="audit-row" key={entry.id}><div><strong>{formatAuditAction(entry.action)}</strong><span>{entry.entityType}{entry.details ? ` · ${entry.details}` : ''}</span></div><div><strong>{entry.actorName ?? 'Système'}</strong><span>{formatAuditDate(entry.createdAtUtc)}</span></div></div>)}</div>}</section>}
        {view === 'availability' && <section className="full-section availability-page"><div className="availability-intro"><div><span className="drawer-kicker">MES DISPONIBILITÉS</span><h2>Une vue claire pour mieux planifier</h2><p>Les changements sont enregistrés dans l’API et servent de signal au coordonnateur lors des prochains quarts.</p></div><span className="availability-legend"><i className="available-dot" />Disponible</span></div><div className="availability-grid">{availabilityDays.map((day) => { const saved = availabilities?.find((availability) => availability.date === day.date); const isAvailable = saved?.isAvailable ?? true; return <article className={`availability-card ${isAvailable ? 'is-available' : 'is-unavailable'}`} key={day.date}><div className="availability-date"><span>{day.day}</span><strong>{day.number}</strong></div><div><strong>{isAvailable ? 'Disponible' : 'Indisponible'}</strong><p>{saved?.note ?? (isAvailable ? 'Vous pouvez prendre un quart.' : 'Aucun quart à proposer ce jour.')}</p></div><button className="availability-toggle" disabled={availabilitySavingDate === day.date} onClick={() => { void toggleAvailability(day.date) }}>{availabilitySavingDate === day.date ? 'Enregistrement…' : isAvailable ? 'Déclarer indisponible' : 'Déclarer disponible'}</button></article> })}</div></section>}
      </div>
    </main>
    {createShiftOpen && <div className="modal-backdrop" onClick={closeCreateShift}><form className="create-shift-modal" onClick={(event) => event.stopPropagation()} onSubmit={submitCreateShift} noValidate>
      <button className="drawer-close" type="button" onClick={closeCreateShift} aria-label="Fermer"><Icon name="close" /></button>
      <span className="drawer-kicker">NOUVEAU QUART</span>
      <h2>Créer un quart</h2>
      <p>Le site et l’horaire seront validés avant l’ajout au calendrier.</p>
      <div className="form-grid">
        <label>Site<select value={createShiftDraft.siteId} onChange={(event) => setCreateShiftDraft((draft) => ({ ...draft, siteId: event.target.value }))} aria-invalid={Boolean(createShiftErrors.siteId)}>
          <option value="">Choisir un site</option>{availableSites.map((site) => <option key={site.id} value={site.id}>{site.name}</option>)}
        </select>{createShiftErrors.siteId && <small className="field-error">{createShiftErrors.siteId}</small>}</label>
        <label>Date<input type="date" value={createShiftDraft.date} onChange={(event) => setCreateShiftDraft((draft) => ({ ...draft, date: event.target.value }))} aria-invalid={Boolean(createShiftErrors.date)} />{createShiftErrors.date && <small className="field-error">{createShiftErrors.date}</small>}</label>
        <label>Début<input type="time" value={createShiftDraft.startTime} onChange={(event) => setCreateShiftDraft((draft) => ({ ...draft, startTime: event.target.value }))} /></label>
        <label>Fin<input type="time" value={createShiftDraft.endTime} onChange={(event) => setCreateShiftDraft((draft) => ({ ...draft, endTime: event.target.value }))} aria-invalid={Boolean(createShiftErrors.endTime)} />{createShiftErrors.endTime && <small className="field-error">{createShiftErrors.endTime}</small>}</label>
        <label>Sauveteurs requis<input type="number" min="1" max="50" value={createShiftDraft.requiredLifeguards} onChange={(event) => setCreateShiftDraft((draft) => ({ ...draft, requiredLifeguards: Number(event.target.value) }))} aria-invalid={Boolean(createShiftErrors.requiredLifeguards)} />{createShiftErrors.requiredLifeguards && <small className="field-error">{createShiftErrors.requiredLifeguards}</small>}</label>
      </div>
      {createShiftErrors.form && <p className="form-error" role="alert">{createShiftErrors.form}</p>}
      <div className="modal-actions"><button className="secondary-button" type="button" onClick={closeCreateShift}>Annuler</button><button className="primary-button" type="submit" disabled={createShiftSubmitting}>{createShiftSubmitting ? 'Création…' : 'Créer le quart'}</button></div>
    </form></div>}
    {selectedShift && <div className="drawer-backdrop" onClick={() => setSelectedShift(null)}><aside className="shift-drawer" onClick={(event) => event.stopPropagation()}><button className="drawer-close" onClick={() => setSelectedShift(null)} aria-label="Fermer"><Icon name="close" /></button><span className="drawer-kicker">DÉTAIL DU QUART</span><h2>{selectedShift.site}</h2><p className="drawer-site">{selectedShift.siteKind} · {selectedShift.lifecycleStatus === 'Cancelled' ? 'Quart annulé' : 'Période d’ouverture valide'}</p><div className="drawer-time"><strong>{selectedShift.day} {selectedShift.date} septembre</strong><span>{selectedShift.start} – {selectedShift.end}</span></div><div className="drawer-rule" /><div className="drawer-detail"><span>Équipe assignée</span><div className="avatar-stack">{selectedShift.colleagues.length === 0 ? <em className="empty-assignment">Aucune</em> : selectedShift.colleagues.map((person) => <i key={person}>{person}</i>)}</div></div><div className="drawer-detail"><span>Statut</span><b className={isManagement ? ((selectedShift.assignments?.length ?? selectedShift.colleagues.length) >= selectedShift.requiredLifeguards ? 'status-tag approved' : 'status-tag pending') : selectedShift.status === 'assigné' ? 'status-tag approved' : 'status-tag pending'}>{isManagement ? `${selectedShift.assignments?.length ?? selectedShift.colleagues.length}/${selectedShift.requiredLifeguards} assignés` : selectedShift.lifecycleStatus === 'Cancelled' ? 'Annulé' : selectedShift.status === 'assigné' ? 'Assigné' : 'Disponible'}</b></div>{isManagement && selectedShift.lifecycleStatus !== 'Cancelled' && <button className="primary-button full" onClick={() => openAssignmentModal(selectedShift)}><Icon name="users" />Gérer les assignations</button>}{isManagement && selectedShift.lifecycleStatus !== 'Cancelled' && apiConfigured && <button className="secondary-button full" disabled={shiftActionSubmitting} onClick={() => { void cancelSelectedShift() }}>{shiftActionSubmitting ? 'Annulation…' : 'Annuler ce quart'}</button>}{selectedShift.status === 'assigné' && !isManagement && <button className="primary-button full" onClick={() => setSwapModalOpen(true)}><Icon name="swap" />Demander un échange</button>}{selectedShift.status === 'disponible' && selectedShift.lifecycleStatus !== 'Cancelled' && !isManagement && <button className="primary-button full" onClick={() => flash('Votre intérêt a été communiqué au coordonnateur')}><Icon name="check" />Me proposer sur ce quart</button>}</aside></div>}
    {assignmentModalOpen && selectedShift && <div className="modal-backdrop" onClick={closeAssignmentModal}><div className="swap-modal assignment-modal" onClick={(event) => event.stopPropagation()}><button className="drawer-close" onClick={closeAssignmentModal} aria-label="Fermer"><Icon name="close" /></button><span className="drawer-kicker">ASSIGNATION</span><h2>Ajouter un sauveteur</h2><p>Vigie rejouera les règles de certification, de chevauchement et de quota avant l’enregistrement.</p><div className="modal-shift"><strong>{selectedShift.site}</strong><span>{selectedShift.day} {selectedShift.date} septembre · {selectedShift.start} – {selectedShift.end}</span></div><label className="assignment-select-label">Sauveteur<select value={assignmentEmployeeId} onChange={(event) => setAssignmentEmployeeId(event.target.value)}><option value="">Choisir un sauveteur</option>{teamMembers.filter((member) => member.role === 'Lifeguard' && !(selectedShift.assignments ?? []).some((assignment) => assignment.employeeId === member.id)).map((member) => <option key={member.id} value={member.id}>{member.name}</option>)}</select></label>{assignmentError && <p className="form-error" role="alert">{assignmentError}</p>}<div className="modal-actions"><button className="secondary-button" type="button" onClick={closeAssignmentModal}>Annuler</button><button className="primary-button" type="button" disabled={!assignmentEmployeeId || assignmentSubmitting} onClick={() => { void submitAssignment() }}>{assignmentSubmitting ? 'Validation…' : 'Assigner'}</button></div></div></div>}
    {swapModalOpen && <div className="modal-backdrop"><div className="swap-modal"><button className="drawer-close" onClick={() => setSwapModalOpen(false)} aria-label="Fermer"><Icon name="close" /></button><span className="drawer-kicker">NOUVEL ÉCHANGE</span><h2>Choisir un receveur</h2><p>Votre demande restera en attente jusqu’à l’approbation du coordonnateur.</p><div className="modal-shift"><strong>{selectedShift?.site}</strong><span>{selectedShift?.day} {selectedShift?.date} septembre · {selectedShift?.start} – {selectedShift?.end}</span></div><div className="receiver-list">{teamMembers.filter((user) => user.id !== currentUser.apiId && user.role === 'Lifeguard').map((user) => <button key={user.id} onClick={() => createSwap(user.name)}><span className="mini-avatar">{initials(user.name)}</span><span><strong>{user.name}</strong><small>Disponible · certifications à jour</small></span><Icon name="arrow" /></button>)}</div></div></div>}
    {currentUser.isDemoAccount && <div className="commercial-cta"><span><strong>Vous gérez un vrai centre aquatique ?</strong><small>Créez un espace isolé pour votre équipe et vos données.</small></span><button className="primary-button" onClick={() => openAuth('register')}>Créer mon espace</button></div>}
    {inviteModalOpen && <div className="modal-backdrop" onClick={() => { if (!inviteSubmitting) setInviteModalOpen(false) }}><form className="auth-modal" onClick={(event) => event.stopPropagation()} onSubmit={submitInvite} noValidate><button className="drawer-close" type="button" onClick={() => { if (!inviteSubmitting) setInviteModalOpen(false) }} aria-label="Fermer"><Icon name="close" /></button><span className="drawer-kicker">ÉQUIPE</span><h2>Inviter un membre</h2>{inviteLink ? <><p>L’invitation est prête. Transmettez ce lien à la personne concernée; il expire dans 7 jours et ne peut servir qu’une seule fois.</p><label className="auth-field">Lien d’invitation<input readOnly value={inviteLink} onFocus={(event) => event.currentTarget.select()} /></label><div className="modal-actions"><button className="secondary-button" type="button" onClick={() => setInviteLink('')}>Inviter une autre personne</button><button className="primary-button" type="button" onClick={() => setInviteModalOpen(false)}>Terminé</button></div></> : <><p>La personne recevra un lien d’activation pour créer son mot de passe et rejoindre votre organisation.</p><label className="auth-field">Nom complet<input required value={inviteForm.name} onChange={(event) => setInviteForm((form) => ({ ...form, name: event.target.value }))} placeholder="Noah Tremblay" /></label><label className="auth-field">Courriel<input required type="email" value={inviteForm.email} onChange={(event) => setInviteForm((form) => ({ ...form, email: event.target.value }))} placeholder="noah@centre.ca" /></label><label className="auth-field">Rôle<select value={inviteForm.role} onChange={(event) => setInviteForm((form) => ({ ...form, role: event.target.value as Role, siteId: '', sectorId: '' }))}>{inviteRoles.map((role) => <option key={role} value={role}>{roleLabel(role)}</option>)}</select></label>{(inviteForm.role === 'Lifeguard' || inviteForm.role === 'PoolChief') && <label className="auth-field">Piscine de rattachement<select required value={inviteForm.siteId} onChange={(event) => setInviteForm((form) => ({ ...form, siteId: event.target.value }))}><option value="">Choisir une piscine</option>{availableSites.map((site) => <option key={site.id} value={site.id}>{site.name}{site.neighborhood ? ` · ${site.neighborhood}` : ''}</option>)}</select></label>}{inviteForm.role === 'SectorManager' && <label className="auth-field">Secteur de rattachement<select required value={inviteForm.sectorId} onChange={(event) => setInviteForm((form) => ({ ...form, sectorId: event.target.value }))}><option value="">Choisir un secteur</option>{availableSectors.filter((sector) => sector.isActive).map((sector) => <option key={sector.id} value={sector.id}>{sector.name} · {sector.code}</option>)}</select></label>}{inviteError && <p className="form-error" role="alert">{inviteError}</p>}<button className="primary-button full" type="submit" disabled={inviteSubmitting}>{inviteSubmitting ? 'Création…' : 'Créer l’invitation'}</button></>}</form></div>}
    {authModal && <div className="modal-backdrop" onClick={closeAuth}><form className="auth-modal" onClick={(event) => event.stopPropagation()} onSubmit={submitAuth} noValidate><button className="drawer-close" type="button" onClick={closeAuth} aria-label="Fermer"><Icon name="close" /></button><span className="drawer-kicker">{authModal === 'register' ? 'NOUVEL ESPACE' : authModal === 'accept-invitation' ? 'INVITATION D’ÉQUIPE' : 'ACCÈS SÉCURISÉ'}</span><h2>{authModal === 'register' ? 'Créer votre espace Vigie' : authModal === 'accept-invitation' ? 'Rejoindre votre équipe' : 'Se connecter à Vigie'}</h2><p>{authModal === 'register' ? 'Votre centre aura son espace isolé, son coordonnateur et ses données privées.' : authModal === 'accept-invitation' ? 'Choisissez votre mot de passe pour activer votre accès au calendrier de votre organisation.' : 'Retrouvez le calendrier et les opérations de votre organisation.'}</p>{authModal === 'register' && <label className="auth-field">Nom de l’organisation<input required value={authForm.organizationName} onChange={(event) => setAuthForm((form) => ({ ...form, organizationName: event.target.value }))} placeholder="Centre aquatique Laval" /></label>}{authModal !== 'login' && <label className="auth-field">Votre nom<input required value={authForm.name} onChange={(event) => setAuthForm((form) => ({ ...form, name: event.target.value }))} placeholder="Marie Tremblay" /></label>}{authModal !== 'accept-invitation' && <label className="auth-field">Courriel<input required type="email" value={authForm.email} onChange={(event) => setAuthForm((form) => ({ ...form, email: event.target.value }))} placeholder="vous@centre.ca" /></label>}<label className="auth-field">Mot de passe<input required type="password" minLength={12} value={authForm.password} onChange={(event) => setAuthForm((form) => ({ ...form, password: event.target.value }))} placeholder="12 caractères, une majuscule et un chiffre" /></label>{authError && <p className="form-error" role="alert">{authError}</p>}<button className="primary-button full" type="submit" disabled={authSubmitting}>{authSubmitting ? 'Vérification…' : authModal === 'register' ? 'Créer mon espace' : authModal === 'accept-invitation' ? 'Activer mon compte' : 'Se connecter'}</button>{authModal !== 'accept-invitation' && <button className="auth-switch" type="button" onClick={() => openAuth(authModal === 'register' ? 'login' : 'register')}>{authModal === 'register' ? 'J’ai déjà un compte' : 'Créer un nouvel espace'}</button>}</form></div>}
    {passwordModalOpen && <div className="modal-backdrop" onClick={closePasswordModal}><form className="auth-modal" onClick={(event) => event.stopPropagation()} onSubmit={submitPassword} noValidate><button className="drawer-close" type="button" onClick={closePasswordModal} aria-label="Fermer"><Icon name="close" /></button><span className="drawer-kicker">SÉCURITÉ DU COMPTE</span><h2>Changer le mot de passe</h2><p>Votre mot de passe sera remplacé et toutes les autres sessions seront fermées.</p><label className="auth-field">Mot de passe actuel<input required type="password" value={passwordForm.current} onChange={(event) => setPasswordForm((form) => ({ ...form, current: event.target.value }))} /></label><label className="auth-field">Nouveau mot de passe<input required type="password" minLength={12} value={passwordForm.next} onChange={(event) => setPasswordForm((form) => ({ ...form, next: event.target.value }))} placeholder="12 caractères, une majuscule et un chiffre" /></label>{passwordError && <p className="form-error" role="alert">{passwordError}</p>}<div className="modal-actions"><button className="secondary-button" type="button" onClick={closePasswordModal}>Annuler</button><button className="primary-button" type="submit" disabled={passwordSubmitting}>{passwordSubmitting ? 'Mise à jour…' : 'Enregistrer'}</button></div></form></div>}
    {selectedSwap && <div className="modal-backdrop" onClick={() => setSelectedSwap(null)}><aside className="swap-detail-modal" onClick={(event) => event.stopPropagation()}><button className="drawer-close" onClick={() => setSelectedSwap(null)} aria-label="Fermer"><Icon name="close" /></button><span className="drawer-kicker">DÉTAIL DE L’ÉCHANGE</span><h2>{selectedSwap.requester} <span className="swap-arrow">→</span> {selectedSwap.receiver}</h2><p className="swap-detail-status"><span className={`status-tag ${selectedSwap.status.toLowerCase().replace('é', 'e')}`}>{selectedSwap.status}</span></p><div className="detail-list"><div><span>Quart concerné</span><strong>{selectedSwap.shiftLabel}</strong></div><div><span>Demande reçue</span><strong>{formatSwapDate(selectedSwap.requestedAt)}</strong></div><div><span>Identifiant</span><strong>{selectedSwap.id}</strong></div></div>{isManagement && selectedSwap.status === 'En attente' && <div className="modal-actions"><button className="reject-button" disabled={decidingSwapId === selectedSwap.id} onClick={() => { void decideSwap(selectedSwap.id, 'Refusé'); setSelectedSwap(null) }}>Refuser</button><button className="primary-button" disabled={decidingSwapId === selectedSwap.id} onClick={() => { void decideSwap(selectedSwap.id, 'Approuvé'); setSelectedSwap(null) }}>Approuver</button></div>}</aside></div>}
    {toast && <div className="toast"><span className="toast-check"><Icon name="check" /></span>{toast}</div>}
  </div>
}
export default App
