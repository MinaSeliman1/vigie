# Vigie MVP — Spécification technique

## Objectif

Livrer une application web démontrable qui permet à une équipe de sauveteurs de consulter ses quarts, de demander un remplacement et de faire appliquer cinq règles métier avant toute assignation. Le résultat doit être compréhensible en moins d’une minute par un recruteur et suffisamment structuré pour soutenir une discussion technique approfondie.

Le MVP couvre les rôles `Lifeguard` et `Coordinator`, les sites et leurs saisons, les quarts, les assignations, les certifications, les disponibilités et les demandes d’échange. La génération automatique d’horaires, les notifications externes, les exports et l’application mobile restent hors périmètre.

## Langue et présentation

Tous les textes visibles par l’utilisateur, les messages d’erreur, le README, les guides et les exemples sont en français. Les identifiants de code, noms de routes, types de données et termes normalisés de l’écosystème (`Employee`, `Shift`, `JWT`, `PostgreSQL`, etc.) restent en anglais afin de conserver des interfaces techniques idiomatiques.

## Utilisateurs et autorisations

- Un `Lifeguard` se connecte, consulte ses quarts et les quarts de l’équipe, déclare ses disponibilités, crée une demande d’échange et accepte une demande qui lui est adressée.
- Un `Coordinator` possède les droits du sauveteur, crée et modifie les sites et les quarts, assigne des sauveteurs, consulte les certifications et approuve ou refuse les échanges.
- Un sauveteur ne peut pas approuver une demande, modifier les données d’un autre employé ou contourner une validation métier par un appel direct à l’API.
- Les données de démonstration sont inventées. Aucun compte ne représente une personne réelle.

## Règles métier

Les règles sont des composants purs du domaine. Elles retournent un résultat de validation structuré, avec un code stable et un message lisible. Un cas d’usage arrête l’opération si une règle échoue et l’API transforme l’erreur en réponse HTTP documentée.

1. **Certification requise** — pour chaque `CertificationType` requis par le site, l’employé doit posséder une certification dont la date d’expiration est postérieure ou égale à la fin du quart, dans le fuseau du site. Une certification manquante ou échue est signalée par son type.
2. **Chevauchement** — une personne ne peut pas avoir deux assignations dont les intervalles se recouvrent. Une fin égale au début d’un autre quart est permise ; les quarts de nuit peuvent traverser minuit.
3. **Échange approuvé** — une demande démarre à `Pending`. Seul un coordonnateur peut la faire passer à `Approved` ou `Rejected`. L’approbation recrée la validation de certification, de chevauchement, de quota et de saison avec l’état courant.
4. **Quota hebdomadaire** — la somme des durées des quarts assignés dans la semaine civile du site, en heures, ne peut pas dépasser le quota configurable de l’employé. Le quart candidat est inclus dans le calcul.
5. **Saison du site** — le quart doit être entièrement compris dans la période d’ouverture annuelle du site. Une période qui traverse le changement d’année est supportée ; les bornes sont inclusives.

Les dates sont manipulées comme `DateTimeOffset` dans l’application et persistées en UTC. Les décisions qui dépendent du calendrier utilisent explicitement le fuseau IANA du site, enregistré avec celui-ci. Les cas limites (bornes adjacentes, changement d’heure, saison traversant le 31 décembre) sont couverts par les tests du domaine.

## Architecture

```text
React + TypeScript + Vite
          |
      REST/JSON + JWT
          |
Vigie.Api  ->  Vigie.Application  ->  Vigie.Domain
      |                 |
      +--------> Vigie.Infrastructure -> PostgreSQL
```

- `Vigie.Domain` ne référence aucun framework web, ORM ou fournisseur de données. Il contient les entités, value objects, erreurs et politiques de validation.
- `Vigie.Application` expose les cas d’usage et les ports (`IClock`, dépôts, hachage et émission de jetons). Les services orchestrent le domaine et retournent des DTOs.
- `Vigie.Infrastructure` fournit EF Core PostgreSQL, les configurations, les migrations, le seeding fictif et le job de rappel des certifications.
- `Vigie.Api` configure l’authentification, l’autorisation, la validation des requêtes, le format `ProblemDetails`, OpenAPI et les endpoints REST.
- `frontend` consomme uniquement l’API publique et garde les types de contrat dans un module séparé. Aucun secret n’est embarqué dans le bundle.

## Modèle de données

Les entités persistées sont `Employee`, `Site`, `CertificationType`, `Certification`, `Shift`, `Assignment`, `Availability` et `SwapRequest`. Les identifiants sont des UUID. Les relations et contraintes d’unicité empêchent les doublons évidents ; les invariants qui dépendent de plusieurs lignes sont vérifiés dans une transaction applicative.

Un `Shift` contient le site, le début et la fin UTC, le nombre de sauveteurs requis et un statut. Une `Assignment` lie un employé à un quart. Une `SwapRequest` référence l’assignation d’origine et l’employé receveur, avec le statut, les dates et l’approbateur. Les opérations d’assignation et d’approbation utilisent une transaction et une concurrence optimiste pour éviter deux décisions contradictoires.

## API MVP

Les routes suivent `/api/v1` et renvoient des enveloppes JSON cohérentes. Les erreurs métier utilisent `ProblemDetails` avec `code`, `message` et, si nécessaire, `details`.

- `POST /auth/login` — connexion et jeton de démonstration.
- `GET /sites` et `POST /sites` — lecture pour les deux rôles, création pour le coordonnateur.
- `GET /shifts?from=&to=` et `POST /shifts` — calendrier et création de quarts.
- `POST /shifts/{id}/assignments` — assignation avec toutes les règles.
- `DELETE /assignments/{id}` — retrait d’une assignation par un coordonnateur.
- `GET /certifications` et `POST /certifications` — suivi et mise à jour par coordonnateur.
- `GET /availability` et `PUT /availability` — disponibilités personnelles.
- `POST /swap-requests`, `GET /swap-requests`, `POST /swap-requests/{id}/approve`, `POST /swap-requests/{id}/reject` — cycle de remplacement.
- `GET /dashboard` — agrégat minimal pour le bandeau de certification et les compteurs du front.

Le contrat OpenAPI est généré à chaque build et sert de référence au frontend. Les changements de contrat sont versionnés et doivent conserver une réponse d’erreur exploitable.

## Interface

Le parcours principal du sauveteur est : connexion → calendrier de la semaine → sélection d’un quart → demande de remplacement → confirmation de l’état `Pending`. Le parcours coordonnateur est : connexion → tableau des demandes → inspection des certifications et règles → approbation ou refus → calendrier mis à jour.

L’interface privilégie une grille calendrier lisible, un état vide explicite, des messages d’erreur actionnables et un bandeau d’alerte pour les certifications à 90 ou 30 jours. Elle doit rester utilisable sur une largeur mobile et respecter les préférences de réduction des mouvements. Les contrôles critiques indiquent leur état de chargement et désactivent les doubles soumissions.

## Tests et qualité

- Tests unitaires du domaine : chaque règle possède des cas passant, échouant et limite, sans base de données ni serveur.
- Tests d’application : assignation, demande d’échange et approbation avec une horloge contrôlée et des ports en mémoire.
- Tests d’intégration API : authentification, autorisations par rôle, réponses `ProblemDetails` et persistance sur PostgreSQL de test.
- Frontend : vérification TypeScript, lint et tests des interactions critiques ; le rendu est vérifié dans un navigateur après chaque tranche UI.
- CI : restauration, build, tests et vérification du format sur chaque Pull Request et chaque poussée vers `main`.

La CI doit échouer si une migration n’est pas reproductible, si un test métier échoue ou si une dépendance est vulnérable selon l’outil standard disponible. Les logs ne doivent jamais imprimer de secret.

## Exécution locale et déploiement

Docker Compose démarre PostgreSQL et fournit les variables nécessaires via un fichier `.env` local ignoré. Un script de démarrage applique les migrations et charge un jeu de données fictif idempotent. Le README contiendra les commandes réellement vérifiées et les comptes de démonstration.

La cible de déploiement est un hébergement gratuit ou à faible coût compatible avec un conteneur pour l’API, un hébergement statique pour le frontend et PostgreSQL managé. Les URL et secrets de production seront configurés dans les secrets du fournisseur et jamais commités. Le MVP est considéré livré seulement lorsque l’URL publique, les comptes de démo et un parcours complet ont été vérifiés.

## Observabilité et limites

L’API journalise les identifiants de corrélation, les échecs de validation et les transitions d’échange sans données personnelles inutiles. Un endpoint de santé ne révèle aucune configuration sensible. Les notifications externes, la génération automatique et l’application mobile feront l’objet de décisions séparées après validation de l’usage du MVP.
