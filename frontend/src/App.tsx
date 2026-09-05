import { useEffect, useMemo, useState, type FormEvent, type ReactNode } from 'react'
import { apiConfigured, vigieApi } from './api/client'
import type { CertificationResponse, SiteResponse, ShiftResponse, SwapRequestResponse, UserSummary } from './api/types'
import { createShiftRequest, type CreateShiftDraft, validateCreateShiftDraft } from './features/shifts/createShift'
import { filterSwaps, type SwapFilter } from './features/swaps/swapFilters'
import './App.css'

type Role = 'Lifeguard' | 'Coordinator'
type View = 'calendar' | 'swaps' | 'certifications' | 'team'
type DemoUser = { id: string; apiId?: string; name: string; email: string; role: Role; initials: string }
type Shift = { id: string; assignmentId?: string; day: string; date: number; start: string; end: string; site: string; siteKind: string; status: 'assigné' | 'disponible'; colleagues: string[]; assignments?: Array<{ id: string; employeeId: string; employeeName: string }> }
type Swap = { id: string; shiftId: string; requester: string; receiver: string; shiftLabel: string; status: 'En attente' | 'Approuvé' | 'Refusé'; requestedAt?: string }
type CertificationRow = { id: string; initials: string; name: string; email: string; type: string; expiry: string; detail: string; warning: boolean }

const demoUsers: DemoUser[] = [
  { id: 'amelie', name: 'Amélie Roy', email: 'amelie@vigie.demo', role: 'Lifeguard', initials: 'AR' },
  { id: 'noah', name: 'Noah Tremblay', email: 'noah@vigie.demo', role: 'Lifeguard', initials: 'NT' },
  { id: 'sofia', name: 'Sofia Nguyen', email: 'sofia@vigie.demo', role: 'Lifeguard', initials: 'SN' },
  { id: 'camille', name: 'Camille Gagnon', email: 'coordonnateur@vigie.demo', role: 'Coordinator', initials: 'CG' },
]
const demoSites: SiteResponse[] = [
  { id: '20000000-0000-0000-0000-000000000001', name: 'Piscine du Nord', type: 'Indoor', timeZoneId: 'Eastern Standard Time', openingSeason: { startMonth: 1, startDay: 1, endMonth: 12, endDay: 31 } },
  { id: '20000000-0000-0000-0000-000000000002', name: 'Bassin du parc', type: 'Outdoor', timeZoneId: 'Eastern Standard Time', openingSeason: { startMonth: 5, startDay: 15, endMonth: 9, endDay: 15 } },
]
const initialShifts: Shift[] = [
  { id: 's1', day: 'MAR', date: 8, start: '09:00', end: '17:00', site: 'Piscine du Nord', siteKind: 'Intérieur', status: 'assigné', colleagues: ['NT', 'CG'] },
  { id: 's2', day: 'MER', date: 9, start: '13:00', end: '21:00', site: 'Piscine du Nord', siteKind: 'Intérieur', status: 'disponible', colleagues: ['NT'] },
  { id: 's3', day: 'VEN', date: 11, start: '14:00', end: '22:00', site: 'Bassin du parc', siteKind: 'Extérieur', status: 'assigné', colleagues: ['SN', 'CG'] },
  { id: 's4', day: 'SAM', date: 12, start: '12:00', end: '20:00', site: 'Piscine du Nord', siteKind: 'Intérieur', status: 'disponible', colleagues: [] },
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

function tomorrowInputValue() {
  const date = new Date()
  date.setDate(date.getDate() + 1)
  return date.toISOString().slice(0, 10)
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
    colleagues: response.assignments.map((item) => initials(item.employeeName)),
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

function Icon({ name }: { name: 'calendar' | 'users' | 'swap' | 'shield' | 'plus' | 'arrow' | 'check' | 'close' | 'menu' | 'bell' }) {
  const paths: Record<string, ReactNode> = {
    calendar: <><rect x="3" y="4" width="18" height="17" rx="2" /><path d="M16 2v4M8 2v4M3 10h18" /></>,
    users: <><circle cx="9" cy="8" r="3" /><path d="M3 20c.5-3 2.5-5 6-5s5.5 2 6 5M16 5.5a3 3 0 0 1 0 5.8M17 15c2.4.4 3.6 2 4 4" /></>,
    swap: <><path d="M7 7h11l-3-3M17 17H6l3 3" /><path d="M18 4v3M6 17v-3" /></>,
    shield: <><path d="M12 3 20 6v5c0 5-3.4 8.2-8 10-4.6-1.8-8-5-8-10V6l8-3Z" /><path d="m8.5 12 2.3 2.3 4.7-4.7" /></>,
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
  const [view, setView] = useState<View>('calendar')
  const [mobileNavOpen, setMobileNavOpen] = useState(false)
  const [selectedShift, setSelectedShift] = useState<Shift | null>(null)
  const [swapModalOpen, setSwapModalOpen] = useState(false)
  const [shifts, setShifts] = useState(initialShifts)
  const [swaps, setSwaps] = useState(initialSwaps)
  const [certifications, setCertifications] = useState<CertificationResponse[] | null>(null)
  const [employees, setEmployees] = useState<UserSummary[] | null>(null)
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
  const isCoordinator = currentUser.role === 'Coordinator'
  const pendingSwaps = swaps.filter((swap) => swap.status === 'En attente')
  const assigned = shifts.filter((shift) => shift.status === 'assigné')
  const availableSites = sites.length > 0 ? sites : demoSites
  const allVisibleSwaps = useMemo(() => isCoordinator ? swaps : swaps.filter((swap) => swap.requester === currentUser.name || swap.receiver === currentUser.name), [currentUser.name, isCoordinator, swaps])
  const visibleSwaps = useMemo(() => filterSwaps(allVisibleSwaps, swapFilter), [allVisibleSwaps, swapFilter])
  const teamMembers = employees ?? demoUsers.map(({ id, name, email, role }) => ({ id, name, email, role }))

  useEffect(() => {
    if (!apiConfigured) return
    let active = true
    async function syncApi() {
      try {
        const login = await vigieApi.login(currentUser.email, 'vigie-demo')
        localStorage.setItem('vigie.token', login.token)
        const [apiShifts, apiSwaps, employees, apiCertifications, apiSites] = await Promise.all([vigieApi.shifts(), vigieApi.swaps(), vigieApi.employees(), vigieApi.certifications(), vigieApi.sites()])
        if (!active) return
        setApiEmployeeIds(Object.fromEntries(employees.map((employee) => [employee.email, employee.id])))
        setEmployees(employees)
        setCurrentUser((user) => ({ ...user, apiId: login.user.id, name: login.user.name, role: login.user.role }))
        setShifts(apiShifts.map((shift) => toUiShift(shift, login.user.id)))
        setSwaps(apiSwaps.map(toUiSwap))
        setCertifications(apiCertifications)
        setSites(apiSites)
        setApiState('ready')
      } catch (error) {
        if (!active) return
        setApiState('error')
        setToast(error instanceof Error ? `API indisponible : ${error.message}` : 'API indisponible : mode démo local')
        window.setTimeout(() => setToast(''), 3600)
      }
    }
    void syncApi()
    return () => { active = false }
  }, [currentUser.email, currentUser.id])

  function flash(message: string) { setToast(message); window.setTimeout(() => setToast(''), 2800) }
  function selectUser(id: string) { const user = demoUsers.find((candidate) => candidate.id === id); if (user) { if (apiConfigured) setApiState('loading'); setCurrentUser(user); setView('calendar'); flash(`Profil de démonstration : ${user.name}`) } }
  function openCreateShift() {
    if (!isCoordinator) { flash('La création de quart est réservée au coordonnateur.'); return }
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
    if (!isCoordinator) { flash('La gestion des assignations est réservée au coordonnateur.'); return }
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
    const receiverUser = demoUsers.find((user) => user.name === receiver)
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
      </nav>
      <div className="sidebar-bottom"><div className="status-line"><span className="status-pulse" />{apiState === 'ready' ? 'API connectée' : apiState === 'loading' ? 'Connexion à l’API…' : apiState === 'error' ? 'Mode démo local' : 'Système opérationnel'}</div><div className="version">Vigie MVP · v0.1.0</div></div>
    </aside>
    <main className="main-content">
      <header className="topbar"><button className="mobile-menu" onClick={() => setMobileNavOpen((open) => !open)} aria-label="Ouvrir le menu"><Icon name="menu" /></button><div className="breadcrumbs"><span>Vigie</span><span className="crumb-separator">/</span><strong>{view === 'calendar' ? 'Mon calendrier' : view === 'swaps' ? 'Échanges' : view === 'certifications' ? 'Certifications' : 'Équipe'}</strong></div><div className="topbar-actions"><button className="icon-button" aria-label="Notifications"><Icon name="bell" /><span className="notification-dot" /></button><div className="profile-switcher"><span className="avatar">{currentUser.initials}</span><select aria-label="Profil de démonstration" value={currentUser.id} onChange={(event) => selectUser(event.target.value)}>{demoUsers.map((user) => <option key={user.id} value={user.id}>{user.name} · {user.role === 'Coordinator' ? 'coord.' : 'sauv.'}</option>)}</select></div></div></header>
      <div className="content-wrap">
        <div className="page-heading"><div><p className="eyebrow">SEMAINE DU 7 AU 13 SEPTEMBRE 2026</p><h1>{view === 'calendar' ? (isCoordinator ? 'Vue équipe' : 'Bonjour, Amélie') : view === 'swaps' ? 'Demandes d’échange' : view === 'certifications' ? 'Certifications' : 'Équipe'}</h1><p className="page-subtitle">{view === 'calendar' ? (isCoordinator ? 'Gardez une vue claire sur les quarts et les remplacements.' : 'Voici vos quarts et les disponibilités de votre équipe.') : view === 'swaps' ? 'Chaque remplacement reste sous contrôle jusqu’à votre approbation.' : view === 'certifications' ? 'La bonne certification, au bon moment, pour chaque quart.' : 'Les personnes autorisées et leur état de préparation pour les quarts.'}</p></div>{view === 'calendar' && isCoordinator && <button className="primary-button" onClick={openCreateShift}><Icon name="plus" />Créer un quart</button>}</div>
        {view === 'calendar' && <><section className="alert-banner"><div className="alert-icon"><Icon name="shield" /></div><div><strong>Certification à surveiller</strong><span>La certification Premiers soins de {currentUser.id === 'sofia' ? 'Sofia Nguyen' : 'votre dossier'} expire dans {currentUser.id === 'sofia' ? 20 : 75} jours.</span></div><button onClick={() => setView('certifications')}>Voir les détails <Icon name="arrow" /></button></section><div className="metric-row"><div className="metric"><span className="metric-label">QUARTS CETTE SEMAINE</span><strong>{assigned.length + 1}</strong><span className="metric-meta positive">↑ 1 vs. semaine dernière</span></div><div className="metric"><span className="metric-label">HEURES PLANIFIÉES</span><strong>24<span className="metric-unit">h</span></strong><span className="metric-meta">Quota : {isCoordinator ? '40' : '24'} h</span></div><div className="metric"><span className="metric-label">ÉCHANGES EN ATTENTE</span><strong>{pendingSwaps.length}</strong><span className="metric-meta">Dernière mise à jour il y a 8 min</span></div></div><section className="calendar-section"><div className="section-header"><div><h2>Cette semaine</h2><p>Votre planning du lundi 7 au dimanche 13 septembre</p></div><div className="week-controls"><button aria-label="Semaine précédente">←</button><button className="today-button">Aujourd’hui</button><button aria-label="Semaine suivante">→</button></div></div><div className="calendar-grid"><div className="time-column"><span /><span>08:00</span><span>12:00</span><span>16:00</span><span>20:00</span></div>{['LUN','MAR','MER','JEU','VEN','SAM','DIM'].map((day) => <div className={`day-column ${day === 'MAR' ? 'today' : ''}`} key={day}><div className="day-header"><span>{day}</span><strong>{day === 'MAR' ? '8' : day === 'MER' ? '9' : day === 'VEN' ? '11' : day === 'SAM' ? '12' : '•'}</strong></div><div className="day-body">{shifts.filter((shift) => shift.day === day).map((shift) => <button className={`shift-block ${shift.status}`} key={shift.id} onClick={() => setSelectedShift(shift)}><span className="shift-time">{shift.start} – {shift.end}</span><strong>{shift.site}</strong><small>{shift.siteKind}</small><span className="shift-people">{shift.colleagues.map((colleague) => <i key={colleague}>{colleague}</i>)}</span></button>)}</div></div>)}</div></section><div className="lower-grid"><section className="list-section"><div className="section-header compact"><div><h2>Échanges en attente</h2><p>Les demandes à traiter</p></div><button className="text-button" onClick={() => setView('swaps')}>Tout voir <Icon name="arrow" /></button></div>{pendingSwaps.length === 0 ? <div className="empty-state">Aucune demande en attente.</div> : pendingSwaps.map((swap) => <div className="swap-row" key={swap.id}><div className="mini-avatar">{swap.requester.split(' ').map((part) => part[0]).join('')}</div><div className="swap-copy"><strong>{swap.requester} <span>→</span> {swap.receiver}</strong><span>{swap.shiftLabel}</span></div><span className="pending-label">En attente</span></div>)}</section><section className="list-section"><div className="section-header compact"><div><h2>À venir</h2><p>Prochains quarts assignés</p></div></div>{shifts.slice(0, 2).map((shift) => <div className="upcoming-row" key={shift.id}><div className="date-block"><strong>{shift.date}</strong><span>{shift.day}</span></div><div><strong>{shift.site}</strong><span>{shift.start} – {shift.end}</span></div><span className="assigned-mark"><Icon name="check" /></span></div>)}</section></div></>}
        {view === 'swaps' && <section className="full-section"><div className="filter-bar"><button className={`filter ${swapFilter === 'all' ? 'active' : ''}`} onClick={() => setSwapFilter('all')}>Toutes <span>{allVisibleSwaps.length}</span></button><button className={`filter ${swapFilter === 'pending' ? 'active' : ''}`} onClick={() => setSwapFilter('pending')}>En attente <span>{allVisibleSwaps.filter((swap) => swap.status === 'En attente').length}</span></button><button className={`filter ${swapFilter === 'processed' ? 'active' : ''}`} onClick={() => setSwapFilter('processed')}>Traitées <span>{allVisibleSwaps.filter((swap) => swap.status !== 'En attente').length}</span></button></div><div className="swaps-table"><div className="table-head"><span>DEMANDE</span><span>QUART</span><span>STATUT</span><span>ACTION</span></div>{visibleSwaps.length === 0 ? <div className="empty-state">Aucune demande dans ce filtre.</div> : visibleSwaps.map((swap) => <div className="table-row" key={swap.id}><div className="person-cell"><span className="mini-avatar">{swap.requester.split(' ').map((part) => part[0]).join('')}</span><div><strong>{swap.requester} <span>→</span> {swap.receiver}</strong><small>{formatSwapDate(swap.requestedAt)}</small></div></div><span>{swap.shiftLabel}</span><span className={`status-tag ${swap.status.toLowerCase().replace('é', 'e')}`}>{swap.status}</span><div className="row-actions">{isCoordinator && swap.status === 'En attente' && <><button className="approve-button" disabled={decidingSwapId === swap.id} onClick={() => decideSwap(swap.id, 'Approuvé')}>{decidingSwapId === swap.id ? '…' : 'Approuver'}</button><button className="reject-button" disabled={decidingSwapId === swap.id} onClick={() => decideSwap(swap.id, 'Refusé')}>Refuser</button></>}<button className="detail-button" onClick={() => setSelectedSwap(swap)}>Détails <Icon name="arrow" /></button></div></div>)}</div></section>}
        {view === 'certifications' && <section className="full-section certification-page">
          <div className="cert-summary"><div><span className="metric-label">CERTIFICATIONS À JOUR</span><strong>{certificationValid}<span>/{certificationTotal}</span></strong></div><div className="progress-track"><span style={{ width: `${certificationProgress}%` }} /></div><p>{certificationRows.some((certification) => certification.warning) ? 'Une certification nécessite votre attention dans les 90 prochains jours.' : 'Toutes les certifications affichées sont à jour.'}</p></div>
          <div className="cert-list">{certificationRows.map((certification) => <div className="cert-row" key={certification.id}><span className="mini-avatar">{certification.initials}</span><div className="cert-person"><strong>{certification.name}</strong><span>{certification.email}</span></div><div className="cert-name"><strong>{certification.type}</strong><span>Certification requise</span></div><div className={`cert-expiry ${certification.warning ? 'warning' : ''}`}><strong>{certification.expiry}</strong><span>{certification.detail}</span></div><span className={`cert-status ${certification.warning ? 'warning' : 'valid'}`}>{certification.warning ? 'À surveiller' : 'À jour'}</span></div>)}</div>
        </section>}
        {view === 'team' && <section className="full-section team-page"><div className="team-summary"><div><span className="metric-label">MEMBRES ACTIFS</span><strong>{teamMembers.length}</strong><p>Profils chargés depuis l’équipe Vigie.</p></div><div><span className="metric-label">COORDONNATEURS</span><strong>{teamMembers.filter((member) => member.role === 'Coordinator').length}</strong><p>Accès à la planification et aux décisions.</p></div><div><span className="metric-label">CERTIFICATIONS À SURVEILLER</span><strong>{certificationRows.filter((certification) => certification.warning).length}</strong><p>Échéances dans les 90 prochains jours.</p></div></div><div className="team-list"><div className="team-list-head"><span>MEMBRE</span><span>RÔLE</span><span>CERTIFICATION</span><span>CONTACT</span></div>{teamMembers.map((member) => { const memberCertifications = certificationRows.filter((certification) => certification.name === member.name); const hasWarning = memberCertifications.some((certification) => certification.warning); const certificationClass = memberCertifications.length === 0 || hasWarning ? 'warning' : 'valid'; return <div className="team-row" key={member.id}><div className="person-cell"><span className="mini-avatar">{initials(member.name)}</span><div><strong>{member.name}</strong><small>{member.email}</small></div></div><span className="role-pill">{member.role === 'Coordinator' ? 'Coordonnateur' : 'Sauveteur'}</span><span className={`cert-status ${certificationClass}`}>{memberCertifications.length === 0 ? 'À vérifier' : hasWarning ? 'À surveiller' : 'À jour'}</span><a className="team-email" href={`mailto:${member.email}`}>{member.email}</a></div> })}</div></section>}
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
    {selectedShift && <div className="drawer-backdrop" onClick={() => setSelectedShift(null)}><aside className="shift-drawer" onClick={(event) => event.stopPropagation()}><button className="drawer-close" onClick={() => setSelectedShift(null)} aria-label="Fermer"><Icon name="close" /></button><span className="drawer-kicker">DÉTAIL DU QUART</span><h2>{selectedShift.site}</h2><p className="drawer-site">{selectedShift.siteKind} · Période d’ouverture valide</p><div className="drawer-time"><strong>{selectedShift.day} {selectedShift.date} septembre</strong><span>{selectedShift.start} – {selectedShift.end}</span></div><div className="drawer-rule" /><div className="drawer-detail"><span>Équipe assignée</span><div className="avatar-stack">{selectedShift.colleagues.length === 0 ? <em className="empty-assignment">Aucune</em> : selectedShift.colleagues.map((person) => <i key={person}>{person}</i>)}</div></div><div className="drawer-detail"><span>Statut</span><b className={selectedShift.status === 'assigné' ? 'status-tag approved' : 'status-tag pending'}>{selectedShift.status === 'assigné' ? 'Assigné' : 'Disponible'}</b></div>{isCoordinator && <button className="primary-button full" onClick={() => openAssignmentModal(selectedShift)}><Icon name="users" />Gérer les assignations</button>}{selectedShift.status === 'assigné' && !isCoordinator && <button className="primary-button full" onClick={() => setSwapModalOpen(true)}><Icon name="swap" />Demander un échange</button>}{selectedShift.status === 'disponible' && !isCoordinator && <button className="primary-button full" onClick={() => flash('Votre intérêt a été communiqué au coordonnateur')}><Icon name="check" />Me proposer sur ce quart</button>}</aside></div>}
    {assignmentModalOpen && selectedShift && <div className="modal-backdrop" onClick={closeAssignmentModal}><div className="swap-modal assignment-modal" onClick={(event) => event.stopPropagation()}><button className="drawer-close" onClick={closeAssignmentModal} aria-label="Fermer"><Icon name="close" /></button><span className="drawer-kicker">ASSIGNATION</span><h2>Ajouter un sauveteur</h2><p>Vigie rejouera les règles de certification, de chevauchement et de quota avant l’enregistrement.</p><div className="modal-shift"><strong>{selectedShift.site}</strong><span>{selectedShift.day} {selectedShift.date} septembre · {selectedShift.start} – {selectedShift.end}</span></div><label className="assignment-select-label">Sauveteur<select value={assignmentEmployeeId} onChange={(event) => setAssignmentEmployeeId(event.target.value)}><option value="">Choisir un sauveteur</option>{teamMembers.filter((member) => member.role === 'Lifeguard' && !(selectedShift.assignments ?? []).some((assignment) => assignment.employeeId === member.id)).map((member) => <option key={member.id} value={member.id}>{member.name}</option>)}</select></label>{assignmentError && <p className="form-error" role="alert">{assignmentError}</p>}<div className="modal-actions"><button className="secondary-button" type="button" onClick={closeAssignmentModal}>Annuler</button><button className="primary-button" type="button" disabled={!assignmentEmployeeId || assignmentSubmitting} onClick={() => { void submitAssignment() }}>{assignmentSubmitting ? 'Validation…' : 'Assigner'}</button></div></div></div>}
    {swapModalOpen && <div className="modal-backdrop"><div className="swap-modal"><button className="drawer-close" onClick={() => setSwapModalOpen(false)} aria-label="Fermer"><Icon name="close" /></button><span className="drawer-kicker">NOUVEL ÉCHANGE</span><h2>Choisir un receveur</h2><p>Votre demande restera en attente jusqu’à l’approbation du coordonnateur.</p><div className="modal-shift"><strong>{selectedShift?.site}</strong><span>{selectedShift?.day} {selectedShift?.date} septembre · {selectedShift?.start} – {selectedShift?.end}</span></div><div className="receiver-list">{demoUsers.filter((user) => user.id !== currentUser.id && user.role === 'Lifeguard').map((user) => <button key={user.id} onClick={() => createSwap(user.name)}><span className="mini-avatar">{user.initials}</span><span><strong>{user.name}</strong><small>Disponible · certifications à jour</small></span><Icon name="arrow" /></button>)}</div></div></div>}
    {selectedSwap && <div className="modal-backdrop" onClick={() => setSelectedSwap(null)}><aside className="swap-detail-modal" onClick={(event) => event.stopPropagation()}><button className="drawer-close" onClick={() => setSelectedSwap(null)} aria-label="Fermer"><Icon name="close" /></button><span className="drawer-kicker">DÉTAIL DE L’ÉCHANGE</span><h2>{selectedSwap.requester} <span className="swap-arrow">→</span> {selectedSwap.receiver}</h2><p className="swap-detail-status"><span className={`status-tag ${selectedSwap.status.toLowerCase().replace('é', 'e')}`}>{selectedSwap.status}</span></p><div className="detail-list"><div><span>Quart concerné</span><strong>{selectedSwap.shiftLabel}</strong></div><div><span>Demande reçue</span><strong>{formatSwapDate(selectedSwap.requestedAt)}</strong></div><div><span>Identifiant</span><strong>{selectedSwap.id}</strong></div></div>{isCoordinator && selectedSwap.status === 'En attente' && <div className="modal-actions"><button className="reject-button" disabled={decidingSwapId === selectedSwap.id} onClick={() => { void decideSwap(selectedSwap.id, 'Refusé'); setSelectedSwap(null) }}>Refuser</button><button className="primary-button" disabled={decidingSwapId === selectedSwap.id} onClick={() => { void decideSwap(selectedSwap.id, 'Approuvé'); setSelectedSwap(null) }}>Approuver</button></div>}</aside></div>}
    {toast && <div className="toast"><span className="toast-check"><Icon name="check" /></span>{toast}</div>}
  </div>
}
export default App
