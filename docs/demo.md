# Démonstration locale

## Démo publique

La démo UI est accessible directement sur [minaseliman1.github.io/vigie](https://minaseliman1.github.io/vigie/). Elle démarre avec le profil d’Amélie et permet de basculer entre six profils couvrant les quatre rôles pour parcourir la planification, les droits par périmètre, le catalogue Laval, une demande d’échange et son approbation.

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
| Chef de piscine (compatibilité) | `coordonnateur@vigie.demo` |
| Chargé de secteur — Nord | `charge.nord@vigie.demo` |
| Régie aquatique | `regie@vigie.demo` |

OpenAPI est disponible à `http://localhost:5187/openapi/v1.json` et l’état du service à `http://localhost:5187/health`.

En dehors de `Development`, l’API refuse de démarrer si `Jwt__Key` n’est pas défini. Utiliser une valeur longue et aléatoire dans le secret manager de l’hébergeur; ne jamais la commit.

## Interface

```powershell
cd frontend
npm install
npm run dev
```

L’interface inclut un sélecteur de profils de démonstration pour parcourir les quatre rôles sans configuration supplémentaire. Elle utilise les données locales par défaut; le profil Régie affiche les 27 installations municipales même lorsque l’API gratuite est en veille, et le chargé de secteur conserve son périmètre Nord. Pour activer l’API réelle, copier `frontend/.env.example` vers `frontend/.env.local`, conserver `VITE_API_URL=http://localhost:5187`, puis relancer Vite : la connexion, les quarts, les échanges, les certifications, les membres et les piscines seront chargés depuis l’API. Si l’API devient indisponible, l’interface revient au mode de démonstration et affiche son état dans la barre latérale.

## Parcours à montrer

1. Basculer vers `Élodie Martel · Régie aquatique` et ouvrir `Piscines` pour montrer les installations municipales et leurs quartiers.
2. Basculer vers `Marc-André Bouchard · Chargé de secteur` et ouvrir `Piscines` puis `Équipe` : seuls les sites et membres du secteur Nord sont visibles.
3. Basculer vers `Camille Gagnon · chef de piscine` et ouvrir un quart dans `Mon calendrier`.
4. Cliquer sur `Gérer les assignations`, choisir un sauveteur puis montrer que l’API valide les règles métier avant l’écriture. Un responsable peut aussi annuler un quart depuis son panneau de détail.
5. Revenir au profil `Amélie Roy · sauv.` et sélectionner un quart assigné.
6. Cliquer sur `Demander un échange` et choisir Noah Tremblay.
7. Basculer sur `Camille Gagnon · chef de piscine` et ouvrir `Échanges`.
8. Ouvrir `Détails`, puis approuver la demande et montrer la confirmation ainsi que le statut `Approuvé`.
9. Ouvrir `Disponibilités` avec le profil sauveteur et basculer un jour pour montrer une écriture persistée par l’API.
10. Ouvrir `Équipe` et `Certifications` pour expliquer le roster et le signal `À surveiller`.
11. Revenir sur `Élodie Martel · Régie aquatique`, ouvrir `Historique` et télécharger `Exporter CSV` pour montrer la traçabilité exploitable hors de l’application.

## Catalogue Laval

Le seed de démonstration inclut les 7 piscines intérieures et les 20 piscines extérieures listées par la [Ville de Laval](https://www.laval.ca/sports-loisirs/sports/piscines/piscines-interieures/) et la [page des piscines extérieures](https://www.laval.ca/sports-loisirs/sports/piscines/piscines-exterieures-jeux-eau/). Les dates et statuts d’ouverture restent des données opérationnelles à confirmer chaque saison; Vigie conserve la saison configurée par installation et bloque les quarts hors période.
