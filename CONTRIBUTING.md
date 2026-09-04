# Conventions de développement

Vigie est un projet personnel. L’historique doit expliquer les changements réels et les décisions de l’auteur. Chaque contribution est relue et comprise avant intégration, y compris lorsqu’elle est préparée avec une assistance IA.

## Issues et branches

Les titres sont en anglais, en minuscules et en kebab-case. Le titre de l’Issue est également le nom recommandé de la branche :

| Préfixe | Usage |
| --- | --- |
| `feature/` | Nouvelle fonctionnalité ou nouveau comportement |
| `fix/` | Correction d’un bug existant |
| `chore/` | Configuration, infrastructure, dépendances et CI |
| `refactor/` | Restructuration sans changement de comportement |
| `test/` | Ajout ou amélioration des tests |
| `docs/` | Documentation uniquement |

Une Issue représente une unité logique intégrable par Pull Request. Sa description est en français, avec exactement cette structure :

```text
Mise en place de [objectif précis].

Scope : [périmètre précis].

- [élément concret à réaliser]
- [élément concret à réaliser]
- [élément concret à réaliser]

[Contrainte importante ou limite du périmètre.]
```

Adapter la première phrase à la tâche : Implémentation de…, Correction de…, Refactorisation de…, Ajout des tests de… Utiliser entre trois et sept points, sans section de critères d’acceptation séparée. Conserver les noms techniques officiels en anglais.

## Commits et Pull Requests

- Décrire l’intention du changement dans le commit, par exemple `docs: define scheduling rules and project scope`.
- Relier la Pull Request à son Issue et expliquer le comportement final.
- Décrire les vérifications réellement exécutées et leurs résultats ; signaler celles qui ne l’ont pas été.
- Tester les règles métier et leurs cas limites avant de les connecter à l’API.
- Ne jamais fabriquer de commits, d’antériorité ou de résultats de tests pour améliorer la présentation du projet.

## Décisions à préciser avant le domaine

Documenter les bornes des quarts (deux quarts adjacents sont-ils permis ?), la validité d’une certification sur toute la durée du quart, la semaine de référence, le fuseau horaire des sites et le traitement des demandes concurrentes. Une approbation doit revérifier les contraintes au moment où elle est appliquée.
