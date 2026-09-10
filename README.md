# Vigie

**Des quarts couverts, des échanges approuvés, des certifications à jour.**

[![CI](https://github.com/MinaSeliman1/vigie/actions/workflows/ci.yml/badge.svg)](https://github.com/MinaSeliman1/vigie/actions/workflows/ci.yml)

Application de gestion de quarts pour équipes de sauveteurs : horaires, remplacements et suivi des certifications dans un même outil.

> **État : fondation opérationnelle publique.** La démo UI est publiée sur [GitHub Pages](https://minaseliman1.github.io/vigie/) et l’API est déployée sur Render avec PostgreSQL Free de Supabase. Le parcours public affiche `API connectée` et couvre le calendrier, la création et l’assignation de quarts, les échanges avec approbation, l’équipe, les certifications, les périmètres et l’historique exportable en CSV. Les comptes réels disposent d’un espace isolé, d’invitations activables, d’un audit organisationnel et de sessions révoquées après changement de mot de passe. Projet personnel indépendant, sans affiliation officielle avec un employeur. Toutes les données de démonstration sont fictives.

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

## Périmètre opérationnel livré

- Connexion avec les rôles `Sauveteur`, `Chef de piscine`, `Chargé de secteur` et `Régie aquatique`.
- Portée d’accès contrôlée par organisation, secteur et piscine, avec memberships actifs, désactivation logique et version optimiste.
- Catalogue de référence Laval : 7 piscines intérieures et 20 piscines extérieures, avec adresse, quartier, type et saison d’ouverture.
- Gestion des sites et secteurs depuis la Régie (création, modification, rattachement et saison d’ouverture), puis des quarts (création, modification, publication et annulation), assignations, disponibilités personnelles et disponibilité de l’équipe, avec suivi de couverture.
- Calendrier hebdomadaire personnel et d’équipe.
- Demandes de remplacement, approbation et refus.
- Suivi des certifications et alertes à 90 et 30 jours de l’échéance.
- Mise à jour de la date d’expiration d’une certification par la personne concernée ou un responsable autorisé, avec audit de la modification.
- Création d’un espace d’organisation avec coordonnateur et isolation des sites et des équipes.
- Parcours d’inscription guidé en français : type de centre, nombre de piscines, taille d’équipe et priorité opérationnelle sont enregistrés dans le profil de l’organisation pour préparer son espace.
- Pour un centre municipal, les 27 piscines de référence de Laval et leurs quatre secteurs sont provisionnés immédiatement, avec les certifications requises pour chaque site.
- Invitations d’équipe à usage unique, expiration après sept jours et activation avec un mot de passe personnel.
- Administration de l’équipe pour le coordonnateur : changement de rôle et de périmètre (piscine ou secteur), désactivation logique et protection contre les modifications concurrentes.
- Journal d’audit organisationnel pour les créations, assignations, invitations et décisions d’échange, avec export CSV coordonnateur.

La génération automatique d’horaires, les SMS et l’application mobile restent hors du périmètre initial. Les courriels transactionnels (invitations, récupération de compte, nouveaux quarts et décisions d’échange) et l’export CSV sont disponibles lorsque leur configuration est activée.

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

Le backend cible .NET 9 et le frontend utilise Node.js 22 dans la CI. L’authentification de l’API repose sur des jetons JWT; les claims de périmètre sont revalidés contre les memberships actifs avant les opérations sensibles.

### Matrice des rôles

| Rôle | Périmètre | Capacités principales |
| --- | --- | --- |
| Sauveteur | Ses données et ses piscines affectées | Disponibilités, certifications, consultation et demandes d’échange |
| Chef de piscine | Une ou plusieurs piscines | Équipe du site, quarts, assignations et décisions d’échange |
| Chargé de secteur | Un secteur et ses piscines | Supervision de la couverture et des chefs de piscine |
| Régie aquatique | Toute l’organisation | Piscines, secteurs, membres, politiques, rapports et audit global |

## Feuille de route

| Jalon | État |
| --- | --- |
| 1 — Domaine | ✅ Entités, règles documentées et tests unitaires des cas limites. |
| 2 — API et authentification | ✅ Endpoints OpenAPI, JWT, rôles, memberships et isolation par périmètre. |
| 3 — Interface | ✅ Parcours responsive en français, profils et catalogue des piscines. |
| 4 — Démonstration | ✅ Démo UI publique, guide de parcours et workflow GitHub Pages. |
| 5 — Déploiement API | ✅ API Render, PostgreSQL Supabase et démo GitHub Pages publiés et vérifiés. |

Le périmètre restant est isolé derrière les mêmes ports d’application afin de ne pas fragiliser la démo.

Les prochaines étapes sont détaillées dans la [feuille de route commerciale](docs/roadmaps/2026-09-05-commercial-product.md) et les [Issues du dépôt](https://github.com/MinaSeliman1/vigie/issues) : facturation et exploitation de production. Les [conditions d’utilisation](docs/legal/conditions-utilisation.md), la [politique de confidentialité](docs/legal/politique-confidentialite.md) et la [procédure de support](docs/support.md) cadrent déjà le parcours commercial. L’envoi transactionnel est déjà branché sur Resend et s’active avec ses variables sécurisées. La procédure reproductible de déploiement reste disponible dans [`docs/deployment.md`](docs/deployment.md).

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

L’interface est en français et permet de basculer entre six profils de démonstration couvrant les quatre rôles pour parcourir les droits par périmètre. Le profil Régie conserve l’accès au catalogue des 27 piscines municipales même lorsque l’API gratuite est temporairement en veille.

## Ce qui est déjà vérifiable

- Les cinq règles métier sont testées dans `tests/Vigie.Domain.Tests` sans serveur ni base de données.
- L’API JWT expose les routes de calendrier, d’assignation, d’échange, de certification, de secteurs et de memberships; les tests d’intégration couvrent l’authentification, l’inscription d’organisation, l’isolation entre organisations, les quatre rôles, les règles de saison, les décisions répétées, le catalogue Laval, le contrat OpenAPI et le modèle EF.
- La démo UI publique est construite automatiquement par GitHub Actions et publiée sur GitHub Pages à chaque mise à jour de `main`.
- Le conteneur de l’API est construit dans la CI pour détecter les erreurs de packaging avant un déploiement.
- Les Pull Requests passent par l’action GitHub Dependency Review afin de repérer les dépendances introduites avec une vulnérabilité connue.
- Chaque réponse API expose un `X-Request-Id` corrélable avec les logs structurés, sans journaliser de secret ni de contenu sensible.
- `/health/ready` vérifie la disponibilité de PostgreSQL quand la persistance est activée, et `/metrics` expose des compteurs Prometheus sans donnée personnelle.
- `render.yaml` décrit le déploiement gratuit de l’API, son health check et les secrets attendus sans jamais les stocker dans Git.
- Le frontend React affiche un calendrier responsive et exécute les parcours création → assignation de quart et demande d’échange → approbation avec les profils de démonstration.
- La Régie aquatique peut créer ou modifier une piscine depuis l’interface en renseignant son type, son adresse, son quartier, son fuseau, sa saison d’ouverture et son secteur; le site est immédiatement disponible pour les quarts et les rattachements.
- La vue `Disponibilités` permet à un sauveteur de déclarer ses jours ouverts ou indisponibles et persiste ce choix via l’API.
- Les responsables disposent de `GET /api/v1/availability/team` et d’une vue d’équipe qui regroupe les déclarations de leur périmètre, avec contrôle d’accès côté serveur.
- EF Core et PostgreSQL sont branchés derrière `IVigieStore`; le mode mémoire reste le défaut local pour garder le démarrage reproductible.
- Les routes publiques `/api/v1/auth/register` et `/api/v1/auth/login` créent ou ouvrent un espace d’organisation ; les invitations `/api/v1/invitations` ne stockent que le hachage d’un jeton et les mots de passe sont stockés sous forme de hachages PBKDF2.
- Les courriels de nouveau quart et de décision d’échange sont envoyés aux utilisateurs réels via Resend lorsque les variables sécurisées sont présentes; les comptes de démonstration ne déclenchent aucun envoi externe.
- Les responsables peuvent corriger une date de certification depuis la vue Certifications; l’API vérifie le périmètre, conserve l’identité du certificat et journalise la modification.
- Chaque utilisateur connecté peut récupérer une copie JSON de ses données via `GET /api/v1/auth/export` ou supprimer son compte via `DELETE /api/v1/auth/account`; les secrets sont exclus, l’export est marqué `no-store`, la suppression révoque les sessions et l’opération est inscrite dans l’audit.
- Les quarts suivent un workflow brouillon → publié : un responsable prépare et vérifie un quart, puis sa publication le rend visible aux sauveteurs. Les quarts existants sont migrés comme publiés pour préserver la continuité de service.
- `GET /api/v1/coverage` expose aux responsables l’effectif requis, l’effectif assigné et les quarts à compléter dans leur périmètre; les sauveteurs ne peuvent pas consulter cette vue de pilotage.
- `GET /api/v1/auth/me` restaure une session, et `POST /api/v1/auth/change-password` renouvelle le jeton tout en invalidant les sessions précédentes.
- Les jetons d’accès expirent après 60 minutes et les routes d’authentification sont limitées à 10 tentatives par minute et par adresse en production.
- `GET /api/v1/audit`, `GET /api/v1/audit/query` et `GET /api/v1/audit/export` sont réservés aux responsables autorisés et restent bornés à leur organisation et à leur périmètre opérationnel; la recherche accepte texte, action, objet, dates et pagination.
- Les migrations `AddLavalOperationsFoundation`, `AddSiteCatalogMetadata` et `FixMembershipScopeIndexes` ainsi qu’un seed idempotent s’exécutent automatiquement lorsqu’une chaîne `ConnectionStrings__Vigie` est configurée.
- Le [runbook de sauvegarde et restauration](docs/operations/backup-restore.md) décrit le dump PostgreSQL gratuit, le test sur une base temporaire et les vérifications de reprise.
- La [checklist de lancement en production](docs/operations/production-launch-checklist.md) couvre le passage à un centre réel, les courriels, la surveillance, l’acceptation fonctionnelle et la facturation future.
- Les procédures d’exploitation incluent la [conservation des données](docs/operations/data-retention.md), la [réponse aux incidents](docs/operations/incident-response.md) et le [signalement de sécurité](SECURITY.md).
- Le [plan de facturation](docs/operations/billing-launch-plan.md) décrit l’activation future d’un abonnement sans simuler de paiement dans la démo.

## Examiner le projet

Commencer par les règles métier ci-dessus, puis ouvrir la [démo publique](https://minaseliman1.github.io/vigie/), lire [`docs/architecture.md`](docs/architecture.md) et les [conventions de contribution](CONTRIBUTING.md). Les commandes listées ici ont été vérifiées localement.

## Données et configuration

Aucune donnée réelle de collègues, aucun secret et aucun identifiant personnel de démonstration dans le dépôt. Les instants sont stockés en UTC ; les règles calendaires utilisent explicitement le fuseau du site. Les fichiers `.env.example` documentent la configuration sans secrets.
