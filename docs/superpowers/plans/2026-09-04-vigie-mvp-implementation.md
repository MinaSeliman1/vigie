# Vigie MVP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans (recommended) or superpowers:subagent-driven-development to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Construire et publier un MVP Vigie exécutable en français, avec domaine métier testé, API sécurisée, persistance PostgreSQL, interface React et pipeline CI.

**Architecture:** Le domaine C# reste pur et indépendant de l’API et de la base. La couche Application orchestre les cas d’usage, Infrastructure fournit EF Core/PostgreSQL, l’API expose REST/JWT et le frontend React consomme le contrat public. Les contraintes multi-entités sont validées dans des transactions et revalidées à l’approbation d’un échange.

**Tech Stack:** .NET 9 / ASP.NET Core, C#, xUnit, Entity Framework Core PostgreSQL, React 19, TypeScript, Vite, Docker Compose, GitHub Actions.

**Spec:** `docs/superpowers/specs/2026-09-04-vigie-mvp-design.md`

## Global Constraints

- Tous les textes visibles, messages d’erreur, documentation et données de démonstration sont en français.
- Les identifiants de code et termes techniques officiels restent en anglais.
- Les dates métier utilisent `DateTimeOffset`, sont persistées en UTC et converties avec le fuseau explicite du site.
- Les règles métier sont testables sans base de données ni serveur.
- Aucun secret, aucune donnée d’employeur ou donnée personnelle réelle ne doit entrer dans Git.
- Chaque étape doit laisser un build ou un test vérifiable et être commitée avec un message d’intention.

---

## État de cette itération

Le domaine, les services d’application, l’API JWT, le store mémoire de démonstration, le modèle EF Core, l’interface React, Docker, la CI et les tests d’intégration sont livrés. Le branchement des repositories EF et la migration PostgreSQL initiale restent volontairement séparés pour une prochaine Issue afin de conserver une démo locale immédiate et vérifiable.

### Task 1: Scaffolding solution and quality baseline

**Files:**
- Create: `Vigie.sln`, `src/`, `tests/`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig` updates if needed
- Create: `src/Vigie.Domain/Vigie.Domain.csproj`, `src/Vigie.Application/Vigie.Application.csproj`, `src/Vigie.Infrastructure/Vigie.Infrastructure.csproj`, `src/Vigie.Api/Vigie.Api.csproj`
- Create: `tests/Vigie.Domain.Tests/Vigie.Domain.Tests.csproj`
- Create: `.github/workflows/ci.yml`
- Modify: `README.md` with verified local commands as they become available

**Interfaces:**
- Produces a buildable solution with project references ordered Domain → Application → Infrastructure → Api.
- Produces a CI workflow that restores, builds and tests the solution on pushes and Pull Requests.

**Steps:**
- [ ] Generate the .NET solution and projects targeting `net9.0` without adding web or ORM dependencies to Domain.
- [ ] Configure central package versions and analyzers with nullable reference types enabled.
- [ ] Add a smoke test proving the test project runs.
- [ ] Add CI for `dotnet restore`, `dotnet build --no-restore` and `dotnet test --no-build`.
- [ ] Run the build and test commands locally and commit the baseline.

### Task 2: Domain entities, values and validation results

**Files:**
- Create: `src/Vigie.Domain/Employees/Employee.cs`, `EmployeeRole.cs`
- Create: `src/Vigie.Domain/Sites/Site.cs`, `SiteType.cs`, `OpeningSeason.cs`
- Create: `src/Vigie.Domain/Shifts/Shift.cs`, `ShiftStatus.cs`, `Assignment.cs`
- Create: `src/Vigie.Domain/Certifications/Certification.cs`, `CertificationType.cs`
- Create: `src/Vigie.Domain/Availability/Availability.cs`
- Create: `src/Vigie.Domain/Swaps/SwapRequest.cs`, `SwapStatus.cs`
- Create: `src/Vigie.Domain/Validation/RuleViolation.cs`, `ValidationResult.cs`

**Interfaces:**
- `Employee.Create(Guid id, string name, string email, EmployeeRole role, decimal weeklyQuotaHours)`.
- `Shift.Create(Guid id, Guid siteId, DateTimeOffset startUtc, DateTimeOffset endUtc, int requiredLifeguards)`.
- `Assignment.Create(Guid id, Guid shiftId, Guid employeeId)`.
- `SwapRequest.Create(Guid id, Guid assignmentId, Guid receiverId)` starts in `Pending`.
- Entities reject impossible local state (empty identity, invalid interval, negative quota, invalid capacity) with domain exceptions.

**Steps:**
- [ ] Write tests for valid construction and invalid local state.
- [ ] Implement immutable identifiers and controlled state transitions.
- [ ] Add timezone-aware site season helpers for seasons within a year and seasons crossing New Year.
- [ ] Run focused domain tests and commit the model.

### Task 3: Five business rule policies

**Files:**
- Create: `src/Vigie.Domain/Rules/ICheckAssignmentRule.cs`
- Create: `src/Vigie.Domain/Rules/CertificationRule.cs`
- Create: `src/Vigie.Domain/Rules/OverlapRule.cs`
- Create: `src/Vigie.Domain/Rules/QuotaRule.cs`
- Create: `src/Vigie.Domain/Rules/SeasonRule.cs`
- Create: `src/Vigie.Domain/Rules/AssignmentPolicy.cs`
- Test: `tests/Vigie.Domain.Tests/Rules/*Tests.cs`

**Interfaces:**
- `ICheckAssignmentRule.Check(AssignmentCandidate candidate, AssignmentContext context) -> RuleViolation?`.
- `AssignmentPolicy.Validate(AssignmentCandidate candidate, AssignmentContext context) -> IReadOnlyList<RuleViolation>`.
- Stable codes: `CERTIFICATION_EXPIRED`, `SHIFT_OVERLAP`, `WEEKLY_QUOTA_EXCEEDED`, `SITE_CLOSED`.

**Steps:**
- [ ] Write passing and failing tests for each rule, including adjacent shifts, overnight shifts, DST boundary, missing certification and season crossing New Year.
- [ ] Implement each policy with no infrastructure dependency.
- [ ] Ensure messages are French and identify the actionable cause.
- [ ] Run `dotnet test tests/Vigie.Domain.Tests` and commit the rules.

### Task 4: Application use cases and ports

**Files:**
- Create: `src/Vigie.Application/Abstractions/*`
- Create: `src/Vigie.Application/Assignments/AssignShiftService.cs`
- Create: `src/Vigie.Application/Swaps/RequestSwapService.cs`, `ApproveSwapService.cs`, `RejectSwapService.cs`
- Create: `src/Vigie.Application/Dashboard/GetDashboardService.cs`
- Create: `tests/Vigie.Domain.Tests/Application/*ServiceTests.cs`

**Interfaces:**
- Ports for clock, employee, site, shift, assignment and swap repositories expose async methods and cancellation tokens.
- Services return typed results or application errors, never HTTP types.
- Approval revalidates the receiver against the current schedule, certifications, quota and season before replacing the assignment.

**Steps:**
- [ ] Write service tests using in-memory fakes and a fixed clock.
- [ ] Implement assign, request, approve, reject and dashboard services.
- [ ] Make approval idempotent and reject stale or already-final requests.
- [ ] Run all unit tests and commit the application layer.

### Task 5: EF Core persistence and demo seed

**Files:**
- Create: `src/Vigie.Infrastructure/Persistence/VigieDbContext.cs`
- Create: `src/Vigie.Infrastructure/Persistence/Configurations/*.cs`
- Create: `src/Vigie.Infrastructure/Persistence/Migrations/*`
- Create: `src/Vigie.Infrastructure/Seeding/DemoDataSeeder.cs`
- Create: `docker-compose.yml`, `.env.example`
- Modify: `src/Vigie.Infrastructure/Vigie.Infrastructure.csproj`

**Interfaces:**
- EF repositories implement Application ports and preserve `CancellationToken`.
- `DemoDataSeeder.SeedAsync` is idempotent and creates fictitious French names, two sites and realistic upcoming shifts.

**Steps:**
- [ ] Add EF Core PostgreSQL packages only to Infrastructure and configure UTC conversion.
- [ ] Map relationships, indexes, uniqueness and optimistic concurrency token.
- [ ] Generate an initial migration and verify database recreation from an empty volume.
- [ ] Add Compose healthcheck and local configuration without committing secrets.
- [ ] Run migration and seeding against a local PostgreSQL container when available; otherwise validate model build and document the blocker.
- [ ] Commit persistence and local runtime files.

### Task 6: ASP.NET Core API, JWT and ProblemDetails

**Files:**
- Create: `src/Vigie.Api/Program.cs`, `appsettings.json`, `appsettings.Development.json`
- Create: `src/Vigie.Api/Auth/*`
- Create: `src/Vigie.Api/Endpoints/*.cs`
- Create: `src/Vigie.Api/Contracts/*.cs`
- Create: `tests/Vigie.Api.IntegrationTests/*`
- Modify: `.github/workflows/ci.yml`

**Interfaces:**
- Routes follow `/api/v1` and the contracts in the spec.
- `POST /api/v1/auth/login` returns a short-lived JWT for seeded demo accounts.
- Domain failures become `application/problem+json` with `code`, `message` and optional `details`.
- Coordinator-only actions enforce authorization independently of frontend visibility.

**Steps:**
- [ ] Add request validation and typed response contracts.
- [ ] Configure JWT validation from environment configuration and safe development defaults.
- [ ] Implement sites, shifts, assignments, certifications, availability, swaps and dashboard endpoints.
- [ ] Add integration tests for 401, 403, successful flows and business-rule ProblemDetails.
- [ ] Expose OpenAPI and health endpoint without secrets.
- [ ] Run API tests and commit the server slice.

### Task 7: French React frontend and core workflows

**Files:**
- Create: `frontend/package.json`, `frontend/tsconfig.json`, `frontend/vite.config.ts`
- Create: `frontend/src/main.tsx`, `frontend/src/App.tsx`
- Create: `frontend/src/api/client.ts`, `frontend/src/api/types.ts`
- Create: `frontend/src/features/auth/*`, `frontend/src/features/calendar/*`, `frontend/src/features/swaps/*`, `frontend/src/features/certifications/*`
- Create: `frontend/src/components/*`, `frontend/src/styles/tokens.css`, `frontend/src/styles/app.css`
- Modify: `README.md` with frontend commands

**Interfaces:**
- `apiClient` attaches the JWT and normalizes `ProblemDetails` errors.
- App shell exposes French labels for `Mon calendrier`, `Équipe`, `Échanges`, `Certifications` and `Déconnexion`.
- Core interaction: login → select shift → request swap → visible `En attente` state; coordinator view → approve/reject → updated state.

**Steps:**
- [ ] Build a restrained responsive design system with navy, cyan and semantic status tokens, accessible contrast and explicit control typography.
- [ ] Implement login, protected shell, weekly calendar and loading/error/empty states.
- [ ] Implement swap request modal and coordinator approval actions with disabled double-submit state.
- [ ] Implement certification alert banner for 90/30-day thresholds.
- [ ] Add frontend typecheck, lint and unit interaction tests.
- [ ] Run production build and commit the frontend slice.

### Task 8: Verification, documentation and release readiness

**Files:**
- Modify: `README.md`, `CONTRIBUTING.md`
- Create: `docs/architecture.md`, `docs/demo.md`, `docs/decisions/0001-mvp-boundaries.md`
- Create: `frontend/.env.example`
- Modify: `.github/workflows/ci.yml`

**Interfaces:**
- README includes verified commands, architecture diagram, demo credentials, screenshots/GIF when available and an honest status section.
- CI runs backend tests, frontend checks and a build artifact step.

**Steps:**
- [ ] Run full backend and frontend verification from a clean checkout.
- [ ] Start the app and verify the core workflow in Browser/IAB; use Playwright only if Browser is unavailable.
- [ ] Check desktop and mobile layout, console health, error states and coordinator authorization.
- [ ] Add a concise fidelity/QA ledger to the release notes with intentional deviations.
- [ ] Remove temporary QA files, inspect Git diff for secrets and commit release documentation.
- [ ] Push `main` and verify GitHub Actions and repository metadata.
