# Vigie — feuille de route produit commercial

## Objectif

Transformer la démonstration Vigie en un logiciel que plusieurs centres aquatiques peuvent utiliser avec leurs propres équipes, leurs données et leurs règles, sans compte partagé ni intervention technique du développeur.

## Critères de sortie commerciale

- Chaque organisation possède son espace isolé, ses sites, ses équipes et ses rôles.
- Les utilisateurs s’inscrivent, se connectent, récupèrent leur accès et peuvent être invités par un coordonnateur.
- Les données métier sont persistées, auditées et protégées par des autorisations testées côté serveur.
- Les opérations critiques sont idempotentes, concurrentes de façon sûre et expliquent leurs refus.
- Le coordonnateur peut gérer le cycle complet : sites, certifications, disponibilités, quarts, assignations, échanges et historique.
- L’application est observable, sauvegardée, documentée et déployée avec une procédure de retour arrière.
- Les comptes de démonstration restent isolés du parcours commercial et n’utilisent aucun secret partagé en production.

## Étapes livrées dans cet ordre

### 1. Fondation sécurité et comptes

- Remplacer le mot de passe codé en dur par des mots de passe hachés PBKDF2 et des comptes explicitement marqués démonstration.
- Ajouter rotation de clé JWT, expiration courte, révocation et limitation des tentatives de connexion.
- Ajouter inscription d’organisation, invitation d’équipe, récupération de mot de passe et session persistante côté interface.
- Couvrir les erreurs d’authentification sans révéler si une adresse existe.

### 2. Multi-tenant et autorisations

- Ajouter `Organization`, membership et rôle par organisation.
- Scoper chaque requête métier à l’organisation du jeton.
- Ajouter les règles de propriété pour sites, équipes, certifications et disponibilités.
- Tester l’impossibilité de lire ou modifier les données d’une autre organisation.

### 3. Opérations métier complètes

- Gérer création, modification, publication et annulation de quarts.
- Permettre une assignation et un retrait avec validation atomique des cinq règles.
- Exposer certifications, disponibilités et capacité de couverture dans la vue coordonnateur.
- Prévenir les doublons et verrouiller les décisions concurrentes.

### 4. Historique, notifications et expérience

- Journaliser les actions importantes avec acteur, organisation, objet, résultat et horodatage.
- Ajouter notifications dans l’application et courriels transactionnels pour invitations, échanges et certifications.
- Ajouter recherche, filtres, états vides, pagination et export CSV pour les coordonnateurs.
- Remplacer le sélecteur de profils de démo par une vraie session et une page d’accueil adaptée au rôle.

### 5. Mise en production

- Environnements séparés, migrations contrôlées, sauvegardes et vérification de restauration.
- Logs structurés, corrélation, métriques d’erreur et alertes de disponibilité.
- Tests de contrat API, tests navigateur des parcours critiques et analyse de dépendances.
- Conditions d’utilisation, politique de confidentialité, suppression de compte et procédure de support.

## Décision actuelle

Le MVP/V1 public couvre le domaine et les opérations principales avec des comptes de démonstration. La prochaine tranche implémente la fondation des comptes réels sans retirer le parcours public existant.
