# Architecture de Vigie

Vigie sépare le métier des détails d’exécution. Le domaine ne connaît ni HTTP ni Entity Framework ; l’API et la persistance sont des adaptateurs remplaçables.

```text
React + TypeScript + Vite
          │ REST / JSON + JWT
      Vigie.Api
          │ services d’application
  Vigie.Application
          │ politiques pures
    Vigie.Domain
          │ ports
Vigie.Infrastructure ─── PostgreSQL / mode mémoire de démonstration
```

Le mode mémoire est activé par défaut pour rendre la démo locale immédiate. Quand `ConnectionStrings__Vigie` est configurée, `AddVigiePersistence` enregistre `VigieDbContext` et `EfVigieStore`; l’API utilise alors le même contrat `IVigieStore` et les mêmes ports d’application avec PostgreSQL. Au démarrage, `VigieDatabaseInitializer` applique les migrations, ajoute idempotemment le catalogue Laval et crée les memberships de compatibilité des bases existantes.

## Portée et hiérarchie

`OrganizationMembership` porte le rôle et le périmètre actifs d’une personne. Un `Lifeguard` et un `PoolChief` sont rattachés à une piscine, un `SectorManager` à un secteur et un `AquaticDirector` à l’organisation entière. `Employee.Role` reste présent pour lire les anciennes données, mais les routes sensibles résolvent toujours le membership actif et revalident l’organisation, le secteur et la piscine de la ressource.

Le catalogue de référence est défini dans `src/Vigie.Infrastructure/LavalPoolCatalog.cs`. Chaque installation dispose d’un identifiant stable, d’un secteur, d’un type intérieur/extérieur, d’une adresse, d’un quartier et d’une saison d’ouverture. Les informations de programmation saisonnière doivent être confirmées à partir des pages officielles de la Ville avant une utilisation réelle.

Les règles d’assignation sont exécutées dans `AssignmentPolicy`. Une demande d’échange est toujours créée en `Pending` et l’approbation rejoue les règles avec les données courantes avant de réassigner le quart.

Les quarts suivent un cycle explicite : `Open`, `Filled` ou `Cancelled`. Une modification de l’horaire rejoue la validation de saison avant l’écriture, et un quart annulé reste consultable pour l’audit tout en bloquant les nouvelles assignations et demandes d’échange.

Les réponses d’erreur utilisent `ProblemDetails` avec un code stable. Les logs ne contiennent pas de jeton, mot de passe ou donnée personnelle inutile. Le fuseau horaire du site reste explicite et les instants persistés sont en UTC.
