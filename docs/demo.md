# Démonstration locale

## Démo publique

La démo UI est accessible directement sur [minaseliman1.github.io/vigie](https://minaseliman1.github.io/vigie/). Elle démarre avec le profil d’Amélie et permet de basculer vers le profil coordonnateur pour parcourir la planification, l’assignation d’un quart, une demande d’échange et son approbation.

## API

```powershell
dotnet run --project src/Vigie.Api --urls http://localhost:5187
```

La démo démarre en mémoire et ne nécessite aucun compte PostgreSQL. Les mots de passe des comptes fictifs sont `vigie-demo`. En environnement `Development`, une clé JWT éphémère est générée au démarrage; aucune clé locale n’est stockée dans Git.

Pour tester le chemin PostgreSQL, démarrer `docker compose up -d postgres`, renseigner `ConnectionStrings__Vigie` (voir `.env.example`), puis lancer l’API : elle sélectionne automatiquement `EfVigieStore`, applique `InitialCreate` et charge les données fictives si la base est vide. Pour gérer les migrations manuellement, exécuter d’abord `dotnet tool restore`. Le mode mémoire reste le parcours de démonstration le plus rapide.

| Rôle | Courriel |
| --- | --- |
| Sauveteur | `amelie@vigie.demo` |
| Sauveteur | `noah@vigie.demo` |
| Sauveteur | `sofia@vigie.demo` |
| Coordonnateur | `coordonnateur@vigie.demo` |

OpenAPI est disponible à `http://localhost:5187/openapi/v1.json` et l’état du service à `http://localhost:5187/health`.

En dehors de `Development`, l’API refuse de démarrer si `Jwt__Key` n’est pas défini. Utiliser une valeur longue et aléatoire dans le secret manager de l’hébergeur; ne jamais la commit.

## Interface

```powershell
cd frontend
npm install
npm run dev
```

L’interface inclut un sélecteur de profils de démonstration pour parcourir les deux rôles sans configuration supplémentaire. Elle utilise les données locales par défaut. Pour activer l’API réelle, copier `frontend/.env.example` vers `frontend/.env.local`, conserver `VITE_API_URL=http://localhost:5187`, puis relancer Vite : la connexion, les quarts, les échanges et les certifications seront chargés depuis l’API. Si l’API devient indisponible, l’interface revient au mode de démonstration et affiche son état dans la barre latérale.

## Parcours à montrer

1. Basculer vers `Camille Gagnon · coord.` et ouvrir un quart dans `Mon calendrier`.
2. Cliquer sur `Gérer les assignations`, choisir un sauveteur puis montrer que l’API valide les règles métier avant l’écriture.
3. Revenir au profil `Amélie Roy · sauv.` et sélectionner un quart assigné.
4. Cliquer sur `Demander un échange` et choisir Noah Tremblay.
5. Basculer sur le profil coordonnateur et ouvrir `Échanges`.
6. Ouvrir `Détails`, puis approuver la demande et montrer la confirmation ainsi que le statut `Approuvé`.
7. Ouvrir `Disponibilités` avec le profil sauveteur et basculer un jour pour montrer une écriture persistée par l’API.
8. Ouvrir `Équipe` et `Certifications` pour expliquer le roster et le signal `À surveiller`.
