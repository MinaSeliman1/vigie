# Vigie V1 — Workflow coordonnateur Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Transformer le MVP déployé en tranche V1 crédible en livrant un vrai parcours coordonnateur et en supprimant les contrôles qui paraissent actifs sans comportement produit.

**Architecture:** Réutiliser les endpoints REST et les règles métier existants. Ajouter les appels API manquants dans le client TypeScript, conserver la validation métier dans le domaine/API, puis brancher l’interface sur des modales et des états explicites. Les données de démonstration restent disponibles lorsque l’API est absente, mais aucun contrôle connecté ne doit simuler silencieusement une écriture réussie.

**Tech Stack:** ASP.NET Core 9, C#/.NET 9, EF Core/PostgreSQL, React 19, TypeScript, Vite, xUnit, GitHub Actions.

**Spec:** `docs/superpowers/specs/2026-09-04-vigie-mvp-design.md`

## Global Constraints

- Tous les textes visibles, messages d’erreur, README et guides restent en français.
- Les règles métier demeurent indépendantes de l’API, de React et de la base de données.
- Les secrets restent dans Render/Supabase/GitHub Actions et ne sont jamais commités.
- Chaque changement de comportement doit avoir une vérification automatisée appropriée avant publication.
- Le mode démonstration local doit rester utilisable lorsque `VITE_API_URL` est absent.

---

### Task 1: Mettre la documentation de livraison à jour

**Files:**
- Modify: `README.md:9,70-75,99-105`
- Modify: `docs/deployment.md:1-50`

**Interfaces:**
- Produces: une documentation qui décrit l’API Render et la démo GitHub Pages comme déployées et vérifiées.

- [x] **Step 1: Remplacer l’état MVP ambigu**

  Indiquer que le MVP est public, que l’API répond sur Render et que la démo affiche `API connectée` lorsque les variables sont configurées.

- [x] **Step 2: Marquer le jalon de déploiement comme livré**

  Conserver explicitement les limites gratuites Render/Supabase et les fonctionnalités hors périmètre.

- [x] **Step 3: Vérifier les liens et le texte**

  Run: `rg -n "reste à|prête à être publiée|🧭|déployée|API connectée" README.md docs/deployment.md`

- [x] **Step 4: Commit**

  `git add README.md docs/deployment.md && git commit -m "docs: refléter le déploiement public du MVP"`

### Task 2: Livrer la création de quart coordonnateur

**Files:**
- Modify: `src/Vigie.Api/Contracts/Contracts.cs`
- Modify: `src/Vigie.Api/Program.cs`
- Modify: `tests/Vigie.Api.IntegrationTests/ApiSmokeTests.cs`
- Modify: `frontend/src/api/types.ts`
- Modify: `frontend/src/api/client.ts`
- Modify: `frontend/src/App.tsx`
- Modify: `frontend/src/App.css`

**Interfaces:**
- Consumes: `GET /api/v1/sites`, `POST /api/v1/shifts`, `ShiftResponse`, `SiteResponse`.
- Produces: `vigieApi.sites()`, `vigieApi.createShift(input)`, une modale de création réservée au coordonnateur et le nouveau quart affiché après succès.

- [x] **Step 1: Écrire les tests d’autorisation et de validation**

  Ajouter à `ApiSmokeTests`:

  ```csharp
  [Fact]
  public async Task Coordinator_can_create_a_shift()
  {
      var token = await LoginAsync("coordonnateur@vigie.demo");
      client.DefaultRequestHeaders.Authorization = new("Bearer", token);
      var sites = await client.GetFromJsonAsync<SitePayload[]>("/api/v1/sites");

      var response = await client.PostAsJsonAsync("/api/v1/shifts", new
      {
          siteId = sites![0].Id,
          startUtc = "2026-09-15T09:00:00Z",
          endUtc = "2026-09-15T17:00:00Z",
          requiredLifeguards = 2
      });

      Assert.Equal(HttpStatusCode.Created, response.StatusCode);
  }

  [Fact]
  public async Task Lifeguard_cannot_create_a_shift()
  {
      var token = await LoginAsync("amelie@vigie.demo");
      client.DefaultRequestHeaders.Authorization = new("Bearer", token);
      var response = await client.PostAsJsonAsync("/api/v1/shifts", new
      {
          siteId = Guid.NewGuid(),
          startUtc = "2026-09-15T09:00:00Z",
          endUtc = "2026-09-15T17:00:00Z",
          requiredLifeguards = 2
      });

      Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }
  ```

- [x] **Step 2: Run the focused tests and confirm the expected failure**

  Run: `dotnet test tests/Vigie.Api.IntegrationTests/Vigie.Api.IntegrationTests.csproj --configuration Release --filter FullyQualifiedName~Coordinator_can_create_a_shift`

  Expected: the new test fails to compile until the small `SitePayload`/`LoginAsync` helpers are added, then the endpoint test exposes any missing contract behavior.

- [x] **Step 3: Add typed frontend API contracts and calls**

  Add `SiteResponse`, `CreateShiftInput`, `vigieApi.sites()` and `vigieApi.createShift()` without leaking `fetch` details into `App.tsx`.

- [x] **Step 4: Add the coordinator modal**

  Add controlled fields for site, date, start time, end time and required lifeguards. Reject an end time that is not after the start time before sending the request. Show the API problem message in the existing toast and close the modal only after a `201 Created` response.

- [x] **Step 5: Refresh the calendar after creation**

  Keep the new `ShiftResponse` in the same state shape used by `toUiShift`; the coordinator must see it immediately without a full page reload.

- [x] **Step 6: Run verification**

  Run: `dotnet test Vigie.sln --configuration Release`, `npm run lint`, and `npm run build` from `frontend`.

- [x] **Step 7: Commit**

  `git add src tests frontend && git commit -m "feat: permettre au coordonnateur de créer un quart"`

### Task 3: Remplacer les contrôles d’échange fictifs par des états réels

**Files:**
- Modify: `frontend/src/App.tsx`
- Modify: `frontend/src/App.css`

**Interfaces:**
- Consumes: `SwapRequestResponse` and existing approve/reject calls.
- Produces: filters `Toutes`, `En attente`, `Traitées`, détail lisible d’une demande et états chargement/erreur pour les décisions.

- [x] **Step 1: Add a filter state and test the pure filter behavior**
- [x] **Step 2: Wire filter buttons to visible rows**
- [x] **Step 3: Add a detail drawer with requester, receiver, shift and current status**
- [x] **Step 4: Disable duplicate approve/reject submissions while awaiting the API**
- [x] **Step 5: Run `npm run lint` and `npm run build`**
- [x] **Step 6: Commit `feat: rendre le suivi des échanges interactif`**

### Task 4: Ajouter une vue équipe en lecture seule

**Files:**
- Modify: `frontend/src/App.tsx`
- Modify: `frontend/src/api/client.ts`
- Modify: `frontend/src/api/types.ts`
- Modify: `frontend/src/App.css`

**Interfaces:**
- Consumes: `GET /api/v1/employees` and certifications already loaded during API sync.
- Produces: roster des sauveteurs, rôle, certifications à surveiller et état vide explicite.

- [x] **Step 1: Store the employee list returned by the API**
- [x] **Step 2: Add the `team` view and navigation route**
- [x] **Step 3: Render responsive roster cards with certification status**
- [x] **Step 4: Preserve the local demo fallback**
- [x] **Step 5: Run frontend checks and commit `feat: ajouter la vue équipe`**

### Task 5: Vérifier la livraison publique et actualiser le parcours recruteur

**Files:**
- Modify: `docs/demo.md`
- Modify: `README.md`

- [x] **Step 1: Run the complete backend, frontend and container checks**
- [x] **Step 2: Verify `/health`, demo login, protected dashboard and CORS from GitHub Pages origin**
- [x] **Step 3: Verify the public workflow: create shift → request swap → coordinator approval**
- [x] **Step 4: Update the recruiter demo script with exact clicks and demo accounts**
- [x] **Step 5: Commit and push the V1 slice only after all checks pass**
