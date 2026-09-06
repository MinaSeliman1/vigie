# Vigie

**Des quarts couverts, des échanges approuvés, des certifications à jour.**

[![CI](https://github.com/MinaSeliman1/vigie/actions/workflows/ci.yml/badge.svg)](https://github.com/MinaSeliman1/vigie/actions/workflows/ci.yml)

Application de gestion de quarts pour équipes de sauveteurs : horaires, remplacements et suivi des certifications dans un même outil.

> **État : MVP public, fondation commerciale en cours.** La démo UI est publiée sur [GitHub Pages](https://minaseliman1.github.io/vigie/) et l’API est déployée sur Render avec PostgreSQL Free de Supabase. Le parcours public affiche `API connectée` et couvre le calendrier, la création et l’assignation de quarts, les échanges avec approbation, l’équipe, les certifications et l’historique exportable en CSV. Les comptes réels disposent d’un espace isolé, d’invitations activables, d’un audit organisationnel et de sessions révoquées après changement de mot de passe. La feuille de route pour passer à un produit commercial est dans [`docs/roadmaps/2026-09-05-commercial-product.md`](docs/roadmaps/2026-09-05-commercial-product.md). Projet personnel indépendant, sans affiliation officielle avec un employeur. Toutes les données de démonstration sont fictives.

## Le problème

Un sauveteur ne peut plus assurer son quart. Un collègue accepte de le remplacer dans une conversation de groupe, mais le coordonnateur apprend le changement après coup. L’horaire officiel ne reflète plus la réalité et les qualifications du remplaçant n’ont pas forcément été vérifiées.

Vigie centralise l’horaire, valide les assignations et soumet les remplacements à une approbation explicite. Les règles métier sont codifiées dans le domaine et testées sans interface ni base de données.

## Les cinq règles métier

| Règle | Comportement attendu |
| --- | --- |
| Certifications | Refuser une assignation si une certification requise n’est pas valide pour le quart et préciser laquelle. |
| Chevauchement | Refuser deux assignations qui se chevauchent, même sur des sites différents. |
| Échange | Conserver la demande en attente jusqu’à son approbation par un coordonnateur et revérifier l’admissibilité du remplaçant lors de l’approbation. |
| Quota | Respecter le maximum d’heures hebdomadaires configuré pour chaque employé. |
| Saison | Autoriser les quarts uniquement pendant la période d’ouverture du site. |

Les limites calendaires (jour d’expiration, début de semaine, quarts de nuit et changement d’heure) sont documentées dans la spécification et doivent rester couvertes par des tests dédiés. Ces règles décrivent le produit et ne constituent pas une interprétation réglementaire.

## Périmètre du MVP livré

- Connexion avec les rôles sauveteur et coordonnateur.
- Gestion des sites, quarts, assignations et disponibilités.
- Calendrier hebdomadaire personnel et d’équipe.
- Demandes de remplacement, approbation et refus.
- Suivi des certifications et alertes à 90 et 30 jours de l’échéance.
- Création d’un espace d’organisation avec coordonnateur et isolation des sites et des équipes.
- Invitations d’équipe à usage unique, expiration après sept jours et activation avec un mot de passe personnel.
- Journal d’audit organisationnel pour les créations, assignations, invitations et décisions d’échange, avec export CSV coordonnateur.

La génération automatique d’horaires, les courriels/SMS, les exports et l’application mobile sont hors du périmètre initial.

## Architecture livrée

```text
frontend/                 React + TypeScript + Vite
src/
  Vigie.Domain/           Entités et règles métier sans dépendance externe
  Vigie.Application/      Cas d’usage et interfaces des dépendances
  Vigie.Infrastructure/   Entity Framework Core, PostgreSQL et rappels
  Vigie.Api/              ASP.NET Core, endpoints et autorisation
tests/
  Vigie.Domain.Tests/     Tests unitaires xUnit des invariants
  Vigie.Api.IntegrationTests/ Tests API et autorisation
```

Le domaine reste indépendant de l’infrastructure. L’API compose les dépendances ; la couche Application orchestre les règles et la persistance. Le store mémoire rend la démo immédiate, tandis que `IVigieStore` permet de sélectionner le store EF Core/PostgreSQL avec `ConnectionStrings__Vigie`.

| Technologie livrée | Raison |
| --- | --- |
| ASP.NET Core / C# | Approfondir la stack backend maîtrisée par l’auteur. |
| Entity Framework Core / PostgreSQL | Modèle relationnel et migrations versionnées. |
| React / TypeScript / Vite | Interface typée et calendrier interactif. |
| xUnit | Exprimer les règles et leurs cas limites dans des tests rapides. |
| Docker Compose | Rendre l’environnement local reproductible. |
| GitHub Actions | Vérifier le build et les tests sur les Pull Requests. |

Le backend cible .NET 9 et le frontend utilise Node.js 22 dans la CI. L’authentification de l’API repose sur des jetons JWT et des rôles sauveteur/coordonnateur.

## Feuille de route

| Jalon | État |
| --- | --- |
| 1 — Domaine | ✅ Entités, règles documentées et tests unitaires des cas limites. |
| 2 — API et authentification | ✅ Endpoints OpenAPI, JWT, rôles et données fictives reproductibles. |
| 3 — Interface | ✅ Parcours sauveteur et coordonnateur, interface responsive en français. |
| 4 — Démonstration | ✅ Démo UI publique, guide de parcours et workflow GitHub Pages. |
| 5 — Déploiement API | ✅ API Render, PostgreSQL Supabase et démo GitHub Pages publiés et vérifiés. |

Le périmètre restant est isolé derrière les mêmes ports d’application afin de ne pas fragiliser la démo.

Les prochaines étapes sont détaillées dans la [feuille de route commerciale](docs/roadmaps/2026-09-05-commercial-product.md) et les [Issues du dépôt](https://github.com/MinaSeliman1/vigie/issues) : récupération de compte, gestion complète du cycle de vie des quarts, concurrence optimiste, notifications et exploitation de production. La procédure reproductible de déploiement reste disponible dans [`docs/deployment.md`](docs/deployment.md).

## Démarrer en local

### API

```powershell
dotnet test Vigie.sln
dotnet run --project src/Vigie.Api --urls http://localhost:5187
```

L’API utilise un jeu de données mémoire pour démarrer sans dépendance externe. Les détails des comptes et le parcours à montrer sont dans [`docs/demo.md`](docs/demo.md). OpenAPI est disponible sur `http://localhost:5187/openapi/v1.json`.

### Interface

```powershell
cd frontend
npm install
npm run lint
npm run build
npm run dev
```

L’interface est en français et permet de basculer entre les profils sauveteur et coordonnateur pour parcourir le flux d’échange.

## Ce qui est déjà vérifiable

- Les cinq règles métier sont testées dans `tests/Vigie.Domain.Tests` sans serveur ni base de données.
- L’API JWT expose les routes de calendrier, d’assignation, d’échange et de certification ; les tests d’intégration couvrent l’authentification, l’inscription d’organisation, l’isolation entre organisations, les autorisations, les règles de saison, les décisions répétées et le modèle EF.
- La démo UI publique est construite automatiquement par GitHub Actions et publiée sur GitHub Pages à chaque mise à jour de `main`.
- Le conteneur de l’API est construit dans la CI pour détecter les erreurs de packaging avant un déploiement.
- `render.yaml` décrit le déploiement gratuit de l’API, son health check et les secrets attendus sans jamais les stocker dans Git.
- Le frontend React affiche un calendrier responsive et exécute les parcours création → assignation de quart et demande d’échange → approbation avec les profils de démonstration.
- La vue `Disponibilités` permet à un sauveteur de déclarer ses jours ouverts ou indisponibles et persiste ce choix via l’API.
- EF Core et PostgreSQL sont branchés derrière `IVigieStore`; le mode mémoire reste le défaut local pour garder le démarrage reproductible.
- Les routes publiques `/api/v1/auth/register` et `/api/v1/auth/login` créent ou ouvrent un espace d’organisation ; les invitations `/api/v1/invitations` ne stockent que le hachage d’un jeton et les mots de passe sont stockés sous forme de hachages PBKDF2.
- `GET /api/v1/auth/me` restaure une session, et `POST /api/v1/auth/change-password` renouvelle le jeton tout en invalidant les sessions précédentes.
- Les jetons d’accès expirent après 60 minutes et les routes d’authentification sont limitées à 10 tentatives par minute et par adresse en production.
- `GET /api/v1/audit` et `GET /api/v1/audit/export` sont réservés au rôle coordonnateur et restent strictement bornés à son organisation.
- Une migration `InitialCreate` et un seed idempotent s’exécutent automatiquement lorsqu’une chaîne `ConnectionStrings__Vigie` est configurée.

## Examiner le projet

Commencer par les règles métier ci-dessus, puis ouvrir la [démo publique](https://minaseliman1.github.io/vigie/), lire [`docs/architecture.md`](docs/architecture.md) et les [conventions de contribution](CONTRIBUTING.md). Les commandes listées ici ont été vérifiées localement.

## Données et configuration

Aucune donnée réelle de collègues, aucun secret et aucun identifiant personnel de démonstration dans le dépôt. Les instants sont stockés en UTC ; les règles calendaires utilisent explicitement le fuseau du site. Les fichiers `.env.example` documentent la configuration sans secrets.
