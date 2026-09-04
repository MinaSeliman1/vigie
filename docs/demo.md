# Démonstration locale

## Démo publique

La démo UI est accessible directement sur [minaseliman1.github.io/vigie](https://minaseliman1.github.io/vigie/). Elle démarre avec le profil d’Amélie et permet de basculer vers le profil coordonnateur pour parcourir une demande d’échange et son approbation.

## API

```powershell
dotnet run --project src/Vigie.Api --urls http://localhost:5187
```

La démo démarre en mémoire et ne nécessite aucun compte PostgreSQL. Les mots de passe des comptes fictifs sont `vigie-demo`.

| Rôle | Courriel |
| --- | --- |
| Sauveteur | `amelie@vigie.demo` |
| Sauveteur | `noah@vigie.demo` |
| Sauveteur | `sofia@vigie.demo` |
| Coordonnateur | `coordonnateur@vigie.demo` |

OpenAPI est disponible à `http://localhost:5187/openapi/v1.json` et l’état du service à `http://localhost:5187/health`.

## Interface

```powershell
cd frontend
npm install
npm run dev
```

L’interface inclut un sélecteur de profils de démonstration pour parcourir les deux rôles sans configuration supplémentaire. Elle conserve les états d’échange dans la session du navigateur ; l’API reste disponible pour tester les mêmes routes avec un client HTTP.

## Parcours à montrer

1. Ouvrir le calendrier et sélectionner un quart assigné.
2. Cliquer sur `Demander un échange` et choisir Noah Tremblay.
3. Basculer sur le profil coordonnateur et ouvrir `Échanges`.
4. Approuver la demande et montrer la confirmation ainsi que le statut `Approuvé`.
5. Ouvrir `Certifications` et expliquer le signal `À surveiller`.
