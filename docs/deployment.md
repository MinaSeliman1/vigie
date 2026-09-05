# Déployer Vigie gratuitement

> **État vérifié le 5 septembre 2026 :** l’API Render, PostgreSQL Supabase et la démo GitHub Pages sont en ligne. Cette page décrit la procédure complète pour reproduire ou réparer le déploiement.

Cette procédure publie l’API ASP.NET sur Render Free et utilise PostgreSQL Free de Supabase. Le frontend reste publié par GitHub Pages.

## 1. Créer la base Supabase

1. Créer un projet sur [Supabase](https://supabase.com/dashboard).
2. Choisir le plan Free et conserver le mot de passe PostgreSQL dans un gestionnaire de mots de passe.
3. Ouvrir **Connect**, puis copier la chaîne **Session pooler** (port `5432`). Elle est adaptée à un conteneur persistant sur un réseau IPv4; la chaîne directe est réservée aux environnements qui disposent d’IPv6. Vigie accepte directement l’URI `postgresql://...` fournie par Supabase. Voir la [documentation Supabase sur les connexions PostgreSQL](https://supabase.com/docs/guides/database/connecting-to-postgres).
4. Ne jamais mettre cette chaîne dans Git. Elle sera ajoutée comme secret Render.

Vigie applique automatiquement les migrations EF Core et charge les données fictives au premier démarrage lorsque `ConnectionStrings__Vigie` est définie. La chaîne doit utiliser la base `postgres` fournie par Supabase. L’API convertit l’URI Supabase en paramètres Npgsql et impose TLS.

## 2. Créer le service Render

1. Ouvrir [Render](https://dashboard.render.com/) et connecter GitHub.
2. Choisir **New → Blueprint**, puis sélectionner `MinaSeliman1/vigie`.
3. Render lit [`render.yaml`](../render.yaml) et propose le service `vigie-api` avec le plan **Free**.
4. Renseigner les deux valeurs secrètes demandées :
   - `Jwt__Key` : une valeur aléatoire longue, par exemple générée avec `openssl rand -base64 48`;
   - `ConnectionStrings__Vigie` : la chaîne Session pooler Supabase copiée à l’étape précédente.
5. Laisser `AllowedOrigins` à `https://minaseliman1.github.io` et lancer le déploiement.

Le service doit répondre à `https://<nom-du-service>.onrender.com/health` avec un JSON contenant `"status":"ok"`. Render fournit une URL publique et exécute le health check `/health` à chaque déploiement. Les services Free se mettent en veille après 15 minutes sans trafic et peuvent prendre environ une minute à redémarrer; c’est attendu pour une offre à 0 $. Voir les [limites Render Free](https://render.com/docs/free).

## 3. Relier la démo GitHub Pages

Une fois l’URL Render connue :

1. Ouvrir `github.com/MinaSeliman1/vigie/settings/variables/actions`.
2. Créer une variable de dépôt nommée `VITE_API_URL` avec l’URL Render, sans `/` final.
3. Relancer le workflow **Déployer la démo UI** depuis l’onglet Actions.

Le workflow injecte cette variable dans Vite. Une fois la variable configurée, la barre latérale de la démo doit afficher **API connectée**. Si elle est absente ou si l’API ne répond pas, la démo conserve automatiquement son mode local afin de rester présentable.

## Vérification avant de partager le lien

```powershell
Invoke-RestMethod https://<nom-du-service>.onrender.com/health
```

Puis ouvrir la démo GitHub Pages, vérifier que la barre latérale affiche **API connectée**, se connecter avec `amelie@vigie.demo` / `vigie-demo`, créer une demande d’échange et l’approuver avec le profil coordonnateur.

Les offres gratuites sont adaptées à la démonstration et à un portfolio, pas à des données réelles ni à une utilisation critique. Supabase Free peut mettre un projet en pause après une semaine d’inactivité; les données restent récupérables depuis le tableau de bord.
