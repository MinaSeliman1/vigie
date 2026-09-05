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

Le mode mémoire est activé par défaut pour rendre la démo locale immédiate. Quand `ConnectionStrings__Vigie` est configurée, `AddVigiePersistence` enregistre `VigieDbContext` et `EfVigieStore`; l’API utilise alors le même contrat `IVigieStore` et les mêmes ports d’application avec PostgreSQL. Au démarrage, `VigieDatabaseInitializer` applique `InitialCreate` et charge le seed fictif si la base est vide.

Les règles d’assignation sont exécutées dans `AssignmentPolicy`. Une demande d’échange est toujours créée en `Pending` et l’approbation rejoue les règles avec les données courantes avant de réassigner le quart.

Les réponses d’erreur utilisent `ProblemDetails` avec un code stable. Les logs ne contiennent pas de jeton, mot de passe ou donnée personnelle inutile. Le fuseau horaire du site reste explicite et les instants persistés sont en UTC.
