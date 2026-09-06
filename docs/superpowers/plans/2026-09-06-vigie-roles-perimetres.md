# Fondations des rôles et des périmètres — plan d’implémentation

> Pour les agents : sous-compétence obligatoire recommandée : `superpowers:executing-plans` (ou `superpowers:subagent-driven-development`). Exécuter les tâches dans l’ordre et conserver une validation reproductible pour chaque étape.

**Objectif :** faire évoluer Vigie d’un modèle « sauveteur / coordinateur » vers le modèle opérationnel de la Ville de Laval : Régie aquatique, chargés de secteur, chefs de piscine et sauveteurs, avec des droits limités au bon périmètre.

**Architecture :** conserver l’organisation comme frontière d’isolation, ajouter des secteurs et une table de memberships pour porter le rôle et le périmètre d’un employé. Le champ `Employee.Role` reste temporairement pour compatibilité avec les comptes existants ; les nouveaux contrôles utilisent le membership actif. Le rôle historique `Coordinator` est conservé pendant la migration et est traité comme un chef de piscine lors de la résolution de compatibilité.

**Stack :** .NET 8, ASP.NET Core, EF Core/SQLite, xUnit, React + TypeScript, Vitest, GitHub Actions.

**Spécification de référence :** `docs/superpowers/specs/2026-09-06-vigie-laval-operations-design.md`.

## Contraintes globales

- Toute donnée lue ou modifiée doit rester isolée par `OrganizationId`.
- Un utilisateur peut avoir plusieurs memberships, mais une seule combinaison active par organisation, site et secteur.
- Les rôles exposés par l’API restent en anglais pour stabiliser les contrats ; l’interface affiche les libellés français.
- Ne jamais supprimer ni réutiliser les audits existants.
- Les comptes de démonstration actuels doivent continuer à se connecter pendant la migration.
- Les réponses d’autorisation doivent être déterministes : `401` sans identité, `403` avec identité insuffisante, `404` pour une ressource d’un autre périmètre quand cette ressource ne doit pas être révélée.
- Chaque nouvelle règle métier doit avoir un test unitaire sans base de données ; les règles d’isolation et de persistance ont des tests d’intégration.

---

## Tâche 1 — Modèle de domaine des rôles et des memberships

**Fichiers :**

- `src/Vigie.Domain/EmployeeRole.cs`
- `src/Vigie.Domain/Sector.cs` (nouveau)
- `src/Vigie.Domain/OrganizationMembership.cs` (nouveau)
- `src/Vigie.Domain/Employee.cs`
- `tests/Vigie.Domain.Tests/OrganizationMembershipTests.cs` (nouveau)

**Travail :**

1. Ajouter les rôles `PoolChief`, `SectorManager` et `AquaticDirector` à `EmployeeRole`, en conservant `Coordinator` marqué comme rôle de compatibilité.
2. Créer `Sector` avec `Id`, `OrganizationId`, `Name`, `Code`, `IsActive`, dates UTC et une fabrique qui refuse un nom ou un code vide.
3. Créer `OrganizationMembership` avec `EmployeeId`, `OrganizationId`, `Role`, `SiteId` nullable, `SectorId` nullable, `IsActive`, dates UTC et un numéro de version optimiste.
4. Ajouter des méthodes métier explicites : activation/désactivation et changement de périmètre. Refuser les combinaisons invalides : un `Lifeguard` doit avoir un site, un `SectorManager` doit avoir un secteur, un `AquaticDirector` ne doit pas être limité à un secteur, et `PoolChief` doit être limité à un site.
5. Garder `Employee.Role` et `Employee.OrganizationId` jusqu’à la fin de la migration ; documenter leur statut de compatibilité dans les commentaires XML ou la documentation technique.

**Tests avant implémentation :**

- création valide pour chacun des quatre rôles ;
- refus des périmètres incompatibles ;
- activation et désactivation idempotentes ;
- changement de périmètre conserve l’identité et met à jour la version ;
- un membership d’une autre organisation est rejeté.

**Validation :** `dotnet test tests/Vigie.Domain.Tests/Vigie.Domain.Tests.csproj --no-restore`.

## Tâche 2 — Persistance, index et migration de compatibilité

**Fichiers :**

- `src/Vigie.Domain/Site.cs`
- `src/Vigie.Infrastructure/Persistence/VigieDbContext.cs`
- `src/Vigie.Infrastructure/Persistence/Migrations/*` (migration nouvelle)
- `src/Vigie.Infrastructure/Persistence/DbInitializer.cs` (ou l’initialiseur actuellement utilisé)
- `tests/Vigie.Infrastructure.Tests/*` (nouveaux tests si le projet n’existe pas)

**Travail :**

1. Ajouter `SectorId` nullable à `Site` et la navigation `Sector`.
2. Ajouter `DbSet<Sector>` et `DbSet<OrganizationMembership>`.
3. Configurer les longueurs, dates UTC, conversion texte de `EmployeeRole`, clés étrangères et suppressions restrictives.
4. Ajouter les index suivants : `(OrganizationId, Code)` unique pour les secteurs, `(OrganizationId, Name)` unique pour les secteurs, `(EmployeeId, OrganizationId, SiteId, SectorId)` unique pour les memberships actifs, et les index de filtrage par organisation.
5. Générer une migration additive. La migration ne doit supprimer aucune colonne existante.
6. Dans l’initialiseur, créer un membership de compatibilité pour chaque employé existant : `Coordinator` devient `PoolChief` sur son site principal ; `Lifeguard` reste `Lifeguard` sur son site ; les données sans site restent réparables sans bloquer le démarrage et sont journalisées.
7. Rendre l’initialisation idempotente et exécutable sur une base vide comme sur la base de démonstration.

**Tests :**

- le modèle EF se construit avec SQLite ;
- la migration s’applique sur une base vide et une base issue du schéma actuel ;
- la seconde exécution de l’initialiseur ne crée aucun doublon ;
- une requête d’un membership d’une autre organisation ne retourne aucune ligne.

**Validation :** `dotnet test Vigie.sln --no-restore --configuration Release`.

## Tâche 3 — Résolution d’autorisation par périmètre

**Fichiers :**

- `src/Vigie.Application/Authorization/*` (nouveaux services, politiques et modèles)
- `src/Vigie.Api/Program.cs`
- `src/Vigie.Api/Authentication/*` (si la logique JWT y est séparée)
- `tests/Vigie.Api.Tests/AuthorizationTests.cs` (nouveau)

**Travail :**

1. Créer un `IMembershipResolver` qui résout les memberships actifs de l’utilisateur courant et refuse un membership d’une autre organisation.
2. Ajouter aux claims JWT le rôle primaire et les identifiants de périmètre nécessaires (`organization_id`, `membership_id`, `site_id` et `sector_id` lorsqu’ils existent). Ne jamais faire confiance à un `site_id` envoyé par le client sans revalidation côté serveur.
3. Définir les politiques `CanManagePool`, `CanManageSector`, `CanManageOrganization` et `CanDecideSwap` selon la matrice de la spécification. `Coordinator` reste accepté pour les tokens historiques et est normalisé en `PoolChief` dans le contexte d’autorisation.
4. Remplacer progressivement les `RequireAuthorization` codés en dur sur `Coordinator` par les politiques métier, sans changer les réponses des endpoints existants.
5. Ajouter un service de portée réutilisable par les endpoints afin que les filtres organisation/site/secteur soient appliqués avant toute lecture métier.

**Tests :**

- matrice complète des quatre rôles sur les opérations d’organisation, site, secteur et échange ;
- token historique `Coordinator` toujours accepté sur les endpoints actuels ;
- absence de claim de périmètre ou périmètre invalide donne `403` ;
- tentative inter-organisation donne `404` ou `403` selon la politique de l’endpoint, sans fuite de nom ou d’identifiant.

**Validation :** `dotnet test tests/Vigie.Api.Tests/Vigie.Api.Tests.csproj --no-restore --configuration Release`.

## Tâche 4 — API de gestion des secteurs et des membres

**Fichiers :**

- `src/Vigie.Api/Endpoints/SectorsEndpoints.cs` (nouveau)
- `src/Vigie.Api/Endpoints/MembersEndpoints.cs` (nouveau)
- `src/Vigie.Application/Sectors/*` (nouveaux cas d’usage)
- `src/Vigie.Application/Members/*` (nouveaux cas d’usage)
- `tests/Vigie.Api.Tests/SectorsEndpointsTests.cs` (nouveau)
- `tests/Vigie.Api.Tests/MembersEndpointsTests.cs` (nouveau)

**Contrat minimal :**

- `GET /api/v1/sectors`
- `POST /api/v1/sectors`
- `PATCH /api/v1/sectors/{id}`
- `GET /api/v1/members?siteId=&sectorId=&role=`
- `POST /api/v1/memberships`
- `PATCH /api/v1/memberships/{id}`
- `DELETE /api/v1/memberships/{id}` (désactivation logique)

**Travail :**

1. Valider les DTO avec des messages français et des codes d’erreur stables.
2. Vérifier la portée avant chaque commande et utiliser des transactions pour la création d’un secteur et d’un membership.
3. Empêcher les doublons actifs et les affectations de site/secteur incohérentes.
4. Écrire une entrée d’audit pour création, modification et désactivation, avec l’ancien et le nouvel état utiles à la relecture.
5. Ajouter des tests d’API pour les statuts, l’isolation, la concurrence optimiste et l’idempotence de désactivation.

**Validation :** `dotnet test Vigie.sln --no-restore --configuration Release`.

## Tâche 5 — Migration des flux existants et du seed de démonstration

**Fichiers :**

- `src/Vigie.Infrastructure/InMemoryVigieStore.cs`
- `src/Vigie.Api/Program.cs`
- `src/Vigie.Application/Swaps/SwapServices.cs`
- `src/Vigie.Domain/Invitation.cs`
- tests existants qui utilisent `EmployeeRole.Coordinator`

**Travail :**

1. Ajouter une piscine municipale et plusieurs secteurs Laval au seed de démonstration, avec un chef, un chargé et des sauveteurs représentatifs.
2. Faire évoluer l’invitation et la création d’utilisateur pour accepter les nouveaux rôles et traduire les rôles historiques.
3. Faire utiliser `CanDecideSwap` au service des échanges ; conserver l’expérience actuelle du compte de démonstration chef.
4. Vérifier que les données en mémoire et EF exposent le même comportement et les mêmes identifiants de démonstration.

**Tests :** régression de connexion, invitation, création de quart, échange, audit et isolation organisationnelle.

**Validation :** `dotnet test Vigie.sln --no-restore --configuration Release`.

## Tâche 6 — Interface française et navigation par périmètre

**Fichiers :**

- `frontend/src/types/*`
- `frontend/src/lib/api.ts`
- `frontend/src/pages/*`
- `frontend/src/components/*`
- `frontend/src/App.tsx`
- tests Vitest associés

**Travail :**

1. Ajouter les types `PoolChief`, `SectorManager`, `AquaticDirector`, les secteurs et les memberships.
2. Afficher le libellé français et le périmètre courant dans l’en-tête : « Chef de piscine », « Chargé de secteur », « Régie aquatique ».
3. Ajouter les écrans `Secteurs` et `Membres`, visibles selon la politique renvoyée par l’API.
4. Ajouter un sélecteur de piscine/secteur pour les utilisateurs multi-memberships et recharger les données sans perdre les filtres.
5. Afficher des états de chargement, erreurs API, absence de droits et succès d’audit de façon cohérente avec le design existant.
6. Garder le parcours de démonstration actuel fonctionnel sur mobile et bureau.

**Validation :**

- `npm run lint`
- `npm run test -- --run`
- `npm run build`

## Tâche 7 — Documentation, vérification et livraison

**Fichiers :**

- `README.md`
- `docs/architecture.md` (ou documentation équivalente)
- `.github/workflows/*`
- `docs/superpowers/specs/2026-09-06-vigie-laval-operations-design.md`

**Travail :**

1. Documenter la matrice des rôles, les comptes de démo, les migrations et les limites encore prévues.
2. Ajouter des exemples `curl` pour les secteurs, memberships, droits et erreurs `CONFLICT`.
3. Vérifier que les workflows CI exécutent backend, frontend, conteneur et déploiement sans secrets en clair.
4. Exécuter les tests locaux, vérifier l’URL publique et tester les quatre rôles avec les comptes de démonstration.
5. Committer par étape cohérente, pousser `main`, puis vérifier les runs GitHub Actions et le déploiement Render/Pages.

**Critère de sortie :** aucune tâche cochée sans commande de validation réussie, aucun endpoint de gestion sans test d’isolation, et aucune fonctionnalité présentée comme disponible si elle n’est pas déployée sur l’URL publique.
