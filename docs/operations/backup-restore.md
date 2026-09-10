# Sauvegarde et restauration

Cette procédure concerne l’instance PostgreSQL Supabase utilisée par l’API Render. Elle complète les sauvegardes gérées par le fournisseur et doit être exécutée par le responsable de l’organisation avant une utilisation avec des données réelles.

## Sauvegarde manuelle gratuite

Le plan Free ne fournit pas la même stratégie de sauvegarde gérée que les plans supérieurs. Supabase recommande d’exporter régulièrement la base avec son outil CLI et de conserver la copie hors du projet. Une sauvegarde PostgreSQL couvre les tables et leurs métadonnées, mais pas les objets binaires déposés dans Storage.

1. Dans Supabase, ouvrir **Connect** et copier la chaîne **Session pooler** dans un gestionnaire de secrets temporaire; ne jamais l’ajouter à Git.
2. Avec le CLI Supabase installé et le projet lié (`supabase link`), exécuter depuis le dépôt :

   ```powershell
   $stamp = Get-Date -Format yyyyMMdd-HHmm
   supabase db dump --db-url "<session-pooler-avec-utilisateur-et-base-postgres>" -f "vigie-$stamp-schema.sql"
   supabase db dump --db-url "<session-pooler-avec-utilisateur-et-base-postgres>" --data-only --use-copy -f "vigie-$stamp-data.sql"
   ```

3. Chiffrer le fichier et le conserver dans un emplacement séparé du projet, avec une date d’expiration et un responsable identifiés.

## Vérification de restauration

Restaurer une copie dans une base temporaire avant de dépendre d’une sauvegarde :

```powershell
$env:PGPASSWORD = "<mot-de-passe-temporaire>"
createdb --host=<hôte> --username=<utilisateur> vigie_restore_check
pg_restore --clean --if-exists --no-owner --dbname="<connexion-vigie_restore_check>" .\vigie-<date>.dump
Remove-Item Env:PGPASSWORD
```

Démarrer ensuite une instance locale de l’API avec `ConnectionStrings__Vigie` vers cette base et vérifier `/health/ready`, la connexion du compte de démonstration, le calendrier, une couverture et l’export de données. Supprimer la base temporaire après la vérification.

Le dump de schéma et le dump de données sont séparés volontairement : le CLI exclut les schémas gérés par Supabase (`auth`, `storage` et extensions). Pour une copie au format custom compatible avec `pg_restore`, utiliser aussi `pg_dump --format=custom --no-owner`; traiter alors les objets Storage séparément. Ne pas conserver le mot de passe dans l’historique du terminal; utiliser un gestionnaire de secrets ou une variable de processus temporaire.

Le plan Free convient à une démonstration et à un portfolio; il ne fournit pas une stratégie de reprise garantie ni une conservation réglementaire. Pour une commercialisation réelle, définir une fréquence, une durée de rétention, un chiffrement, un test de restauration périodique et un fournisseur avec SLA. Voir la [documentation officielle Supabase sur les sauvegardes](https://supabase.com/docs/guides/platform/backups).
