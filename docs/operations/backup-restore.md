# Sauvegarde et restauration

Cette procédure concerne l’instance PostgreSQL Supabase utilisée par l’API Render. Elle complète les sauvegardes gérées par le fournisseur et doit être exécutée par le responsable de l’organisation avant une utilisation avec des données réelles.

## Sauvegarde manuelle gratuite

1. Dans Supabase, ouvrir **Connect** et copier la chaîne **Session pooler** dans un gestionnaire de secrets temporaire; ne jamais l’ajouter à Git.
2. Avec PostgreSQL `pg_dump` installé, exécuter :

   ```powershell
   $env:PGPASSWORD = "<mot-de-passe-stocké>"
   pg_dump --format=custom --no-owner --file="vigie-$(Get-Date -Format yyyyMMdd-HHmm).dump" "<session-pooler-avec-utilisateur-et-base-postgres>"
   Remove-Item Env:PGPASSWORD
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

Le plan Free convient à une démonstration et à un portfolio; il ne fournit pas une stratégie de reprise garantie ni une conservation réglementaire. Pour une commercialisation réelle, définir une fréquence, une durée de rétention, un chiffrement, un test de restauration périodique et un fournisseur avec SLA.
