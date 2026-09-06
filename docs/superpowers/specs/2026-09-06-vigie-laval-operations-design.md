# Vigie Laval — conception des opérations municipales

## Objectif

Transformer Vigie en un produit opérationnel pour les piscines municipales de la Ville de Laval. Le système doit refléter la chaîne de responsabilité réelle : sauveteur, chef de piscine, chargé de secteur et régie aquatique.

## Périmètre cible

Une organisation Vigie représente la Ville de Laval. Elle contient plusieurs piscines, des secteurs de supervision et les équipes qui y sont affectées. Le modèle reste multi-organisation dans la base de données pour conserver l’isolation déjà livrée et permettre une future offre à d’autres villes, mais la démonstration et les données de référence ciblent Laval.

Le produit couvre le cycle de vie complet d’un quart : brouillon, publié, assigné, complété ou annulé. Il couvre aussi les disponibilités, les certifications, les échanges, les invitations, les alertes, l’historique et les exports.

## Rôles et périmètres

Les rôles persistés utilisent des noms anglais stables côté API et sont traduits dans l’interface :

| Rôle API | Libellé | Périmètre | Capacités principales |
| --- | --- | --- | --- |
| `Lifeguard` | Sauveteur | Ses données et ses quarts | Disponibilités, certifications, consultation, demandes d’échange |
| `PoolChief` | Chef de piscine | Une ou plusieurs piscines affectées | Équipe du site, quarts, assignations, échanges et alertes du site |
| `SectorManager` | Chargé de secteur | Un secteur et ses piscines | Supervision des chefs, couverture, rapports et arbitrage de secteur |
| `AquaticDirector` | Régie aquatique | Toute l’organisation | Piscines, secteurs, utilisateurs, politiques, rapports et audit global |

Une personne peut avoir plusieurs affectations. Une affectation contient l’organisation, le rôle, et au plus un site ou un secteur selon le rôle. Les vérifications d’autorisation partent toujours de l’affectation active et du périmètre de la ressource; un identifiant de site envoyé par le client ne suffit jamais à obtenir un accès.

## Modèle de données

`Employee` conserve l’identité, le courriel, le mot de passe haché et la révocation de session. Le rôle unique actuel est remplacé par `OrganizationMembership`, qui contient `EmployeeId`, `OrganizationId`, `Role`, `SiteId` nullable, `SectorId` nullable, `IsActive`, `CreatedAtUtc` et `UpdatedAtUtc`.

`Sector` contient `OrganizationId`, un nom, un code court, un ordre d’affichage et un état actif. `Site` reçoit un `SectorId` nullable et reste rattaché à une organisation. Les contraintes garantissent qu’un site et son secteur appartiennent à la même organisation.

Les quarts reçoivent un état et un numéro de version. Chaque mutation de quart vérifie la version attendue et retourne un conflit exploitable si une autre personne l’a modifié. Les assignations et les décisions d’échange sont traitées dans une transaction et réévaluent certifications, chevauchements, quota et état du quart.

Les certifications conservent leur type, leur date d’expiration et leur état calculé. Les données d’audit contiennent l’acteur, l’organisation, le site ou secteur concerné, l’objet, l’action, le résultat, les détails utiles et l’horodatage UTC. Les données personnelles inutiles ne sont jamais ajoutées aux détails d’audit.

## API et autorisation

Les jetons contiennent l’identité et l’organisation, puis l’API charge les affectations actives avant chaque opération sensible. Les politiques suivantes sont exposées : `CanManagePool`, `CanManageSector`, `CanManageOrganization` et `CanDecideSwap`.

Les routes ajoutées ou adaptées sont :

- `GET/POST/PATCH /api/v1/sectors` pour la régie et les chargés de secteur autorisés.
- `GET/POST/PATCH /api/v1/sites` avec contrôle du secteur et du périmètre.
- `GET/PATCH /api/v1/members` pour consulter, activer, suspendre et modifier une affectation.
- `GET/POST/PATCH/DELETE /api/v1/shifts` avec cycle de vie, publication, annulation et contrôle de concurrence.
- `GET/POST/DELETE /api/v1/assignments` avec validation atomique et journalisation du résultat.
- `GET/POST/{id}/approve|reject /api/v1/swap-requests` avec décision idempotente et périmètre du décideur.
- `GET /api/v1/dashboard` enrichi avec couverture par site, certifications à risque et alertes non traitées.
- `GET /api/v1/notifications` et `PATCH /api/v1/notifications/{id}` pour les alertes dans l’application.
- `GET /api/v1/audit` et `GET /api/v1/audit/export` avec filtres, pagination et périmètre obligatoire.

Les erreurs de validation utilisent les codes déjà présents et ajoutent `CONFLICT` pour une version périmée. Les réponses d’authentification restent génériques pour ne pas révéler l’existence d’un courriel.

## Interface

La démo publique reste disponible avec des profils fictifs. Le parcours commercial utilise la session réelle et affiche uniquement les espaces autorisés.

La navigation coordonnateur devient une navigation par périmètre :

- `Mon calendrier` avec filtres secteur, piscine et état du quart.
- `Équipe` avec affectations, rôle, site, secteur, certifications et suspension.
- `Piscines et secteurs` pour les responsables habilités.
- `Échanges` avec décision et explication du refus.
- `Alertes` pour certifications, couvertures insuffisantes, invitations et conflits.
- `Historique` avec recherche, filtres de période et export CSV.

Chaque écran possède un état de chargement, un état vide explicite, une erreur récupérable et un retour de succès. Les actions irréversibles demandent une confirmation locale et l’interface indique le périmètre de la personne connectée.

## Notifications et exploitation

Les notifications dans l’application sont créées pour une invitation, une certification à 90 ou 30 jours, une demande d’échange, une décision d’échange, une couverture insuffisante et un conflit de modification. L’envoi courriel est isolé derrière `INotificationSender`; un fournisseur transactionnel sera branché par configuration sans exposer de secret dans le dépôt.

Les logs structurés ajoutent un identifiant de corrélation à chaque requête. Le health check reste public et minimal. Les métriques portent sur les erreurs HTTP, les refus métier, le temps de réponse et l’état de la base. Les migrations sont appliquées avant le démarrage de l’API et le seed démo reste idempotent.

## Validation et critères d’acceptation

- Un sauveteur ne voit et ne modifie que ses données.
- Un chef de piscine ne voit et ne modifie que les piscines de ses affectations.
- Un chargé de secteur voit les piscines de son secteur et aucun autre secteur.
- La régie voit toute l’organisation et peut gérer les périmètres.
- Une modification concurrente d’un quart retourne `CONFLICT` sans écraser la version gagnante.
- Une décision d’échange répétée retourne l’état existant sans appliquer deux fois l’opération.
- Les cinq règles métier sont testées au domaine et aux routes autorisées.
- Chaque opération sensible crée une entrée d’audit bornée à l’organisation et exportable.
- Les parcours de connexion, invitation, création de quart, assignation, échange, décision, suspension et export sont vérifiés par tests d’intégration et navigateur.
- La CI exécute le lint, les tests, le build frontend, les tests .NET et la construction Docker avant publication.

## Hors périmètre immédiat

La récupération de mot de passe par courriel, la facturation, les horaires générés automatiquement et l’application mobile sont des tranches séparées. Elles ne doivent pas affaiblir les règles d’autorisation ou le parcours de planification municipal.
