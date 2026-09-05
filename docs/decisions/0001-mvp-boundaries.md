# ADR 0001 — Limites du MVP

## Décision

Le MVP livre les quarts, les assignations, les échanges, les certifications, les disponibilités, deux rôles et une démo accessible. La génération automatique d’horaires, les notifications externes, les exports et l’application mobile sont reportés.

## Raisons

Les cinq invariants métier constituent la valeur démontrable de Vigie. Les livrer d’abord permet de tester le domaine sans infrastructure et de mettre en ligne un parcours complet avant d’ajouter des fonctions d’optimisation ou des intégrations coûteuses.

## Conséquences

La démo inclut un store mémoire pour réduire le temps de démarrage. `IVigieStore` sélectionne le store EF Core/PostgreSQL lorsque `ConnectionStrings__Vigie` est configurée; la migration `InitialCreate` et le seed idempotent sont appliqués au démarrage. Tout nouveau périmètre doit être proposé dans une Issue séparée et conserver le parcours principal utilisable.
