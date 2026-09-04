# Vigie

**Des quarts couverts, des échanges approuvés, des certifications à jour.**

Application de gestion de quarts pour équipes de sauveteurs : horaires, remplacements et suivi des certifications dans un même outil.

> **État : cadrage initial.** Ce dépôt contient la documentation de départ. L’application, les tests et la démo en ligne sont à construire. Projet personnel indépendant, sans affiliation officielle avec un employeur. Toutes les données de démonstration seront fictives.

## Le problème

Un sauveteur ne peut plus assurer son quart. Un collègue accepte de le remplacer dans une conversation de groupe, mais le coordonnateur apprend le changement après coup. L’horaire officiel ne reflète plus la réalité et les qualifications du remplaçant n’ont pas forcément été vérifiées.

Vigie vise à centraliser l’horaire, valider les assignations et soumettre les remplacements à une approbation explicite. Les règles métier seront testables sans interface ni base de données.

## Les cinq règles métier

| Règle | Comportement attendu |
| --- | --- |
| Certifications | Refuser une assignation si une certification requise n’est pas valide pour le quart et préciser laquelle. |
| Chevauchement | Refuser deux assignations qui se chevauchent, même sur des sites différents. |
| Échange | Conserver la demande en attente jusqu’à son approbation par un coordonnateur et revérifier l’admissibilité du remplaçant lors de l’approbation. |
| Quota | Respecter le maximum d’heures hebdomadaires configuré pour chaque employé. |
| Saison | Autoriser les quarts uniquement pendant la période d’ouverture du site. |

Les limites exactes (jour d’expiration, début de semaine, quarts de nuit et changement d’heure) seront documentées avec des exemples avant leur implémentation. Ces règles décrivent le produit envisagé et ne constituent pas une interprétation réglementaire.

## Première version prévue

- Connexion avec les rôles sauveteur et coordonnateur.
- Gestion des sites, quarts, assignations et disponibilités.
- Calendrier hebdomadaire personnel et d’équipe.
- Demandes de remplacement, approbation et refus.
- Suivi des certifications et alertes à 90 et 30 jours de l’échéance.

La génération automatique d’horaires, les courriels/SMS, les exports et l’application mobile sont hors du périmètre initial.

## Architecture prévue

```text
frontend/                 React + TypeScript + Vite
src/
  Vigie.Domain/           Entités et règles métier sans dépendance externe
  Vigie.Application/      Cas d’usage et interfaces des dépendances
  Vigie.Infrastructure/   Entity Framework Core, PostgreSQL et rappels
  Vigie.Api/              ASP.NET Core, endpoints et autorisation
tests/
  Vigie.Domain.Tests/     Tests unitaires xUnit des invariants
  Vigie.IntegrationTests/ Tests API, autorisation et persistance
```

Cette arborescence est une cible : les projets ne sont pas encore générés. Le domaine reste indépendant de l’infrastructure. L’API compose les dépendances ; la couche Application orchestre les règles et la persistance.

| Technologie prévue | Raison |
| --- | --- |
| ASP.NET Core / C# | Approfondir la stack backend maîtrisée par l’auteur. |
| Entity Framework Core / PostgreSQL | Modèle relationnel et migrations versionnées. |
| React / TypeScript / Vite | Interface typée et calendrier interactif. |
| xUnit | Exprimer les règles et leurs cas limites dans des tests rapides. |
| Docker Compose | Rendre l’environnement local reproductible. |
| GitHub Actions | Vérifier le build et les tests sur les Pull Requests. |

Les versions supportées et le mode d’authentification seront confirmés au démarrage technique et consignés dans des décisions d’architecture.

## Feuille de route

| Jalon | Résultat attendu |
| --- | --- |
| 1 — Domaine | Entités, règles documentées et tests unitaires des cas limites. |
| 2 — Persistance et API | Migrations, endpoints OpenAPI et données fictives reproductibles. |
| 3 — Authentification | Autorisations par rôle et tests d’intégration des refus. |
| 4 — Interface | Parcours complets sauveteur et coordonnateur. |
| 5 — Démonstration | URL publique, comptes de démo, captures et guide de lancement vérifié. |

Objectif indicatif : six semaines. Le périmètre sera ajusté pour livrer une démonstration utilisable.

## Examiner le projet

À ce stade, commencer par les règles métier ci-dessus et les [conventions de contribution](CONTRIBUTING.md). Au fil des livraisons, ce README présentera les commandes réellement vérifiées, les résultats des tests, les décisions techniques et le lien de démonstration.

## Données et configuration

Aucune donnée réelle de collègues, aucun secret et aucun identifiant personnel de démonstration dans le dépôt. Les instants seront stockés en UTC ; les règles calendaires utiliseront explicitement le fuseau du site. Un exemple de configuration sans secrets sera ajouté avec les services concernés.
