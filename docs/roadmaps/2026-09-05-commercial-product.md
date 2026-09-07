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

- ✅ Remplacer le mot de passe codé en dur par des mots de passe hachés PBKDF2 et des comptes explicitement marqués démonstration.
- ✅ Créer un espace d’organisation avec un coordonnateur propriétaire et vérifier l’isolation des sites et des équipes côté API.
- ✅ Expiration courte des jetons d’accès et limitation des tentatives sur les routes d’authentification.
- ✅ Révoquer explicitement les sessions après un changement de mot de passe, avec un compteur persistant vérifié par l’API.
- ✅ Ajouter inscription d’organisation et invitation d’équipe à usage unique avec expiration.
- ✅ Restaurer une session réelle côté interface après un rechargement et permettre le changement de mot de passe.
- ✅ Ajouter récupération de mot de passe à jeton unique, expiration courte et révocation des sessions; le fournisseur courriel reste configurable.
- ✅ Couvrir les erreurs d’authentification sans révéler si une adresse existe.

### 2. Multi-tenant et autorisations

- Ajouter `Organization`, membership et rôle par organisation.
- ✅ Scoper chaque requête métier à l’organisation du jeton.
- Ajouter les règles de propriété pour sites, équipes, certifications et disponibilités.
- Tester l’impossibilité de lire ou modifier les données d’une autre organisation.

### 3. Opérations métier complètes

- ✅ Gérer création, modification, publication et annulation de quarts; les brouillons restent réservés aux responsables jusqu’à leur publication.
- Permettre une assignation et un retrait avec validation atomique des cinq règles.
- ✅ Exposer certifications, capacité de couverture et disponibilités déclarées de l’équipe dans les vues responsables; les disponibilités personnelles restent visibles par leur propriétaire.
- Prévenir les doublons et verrouiller les décisions concurrentes.

### 4. Historique, notifications et expérience

- ✅ Journaliser les actions importantes avec acteur, organisation, objet et horodatage; le coordonnateur peut exporter l’historique en CSV.
- ✅ Ajouter les notifications dans l’application pour les assignations, échanges et alertes de certification; les courriels transactionnels restent derrière un fournisseur configurable.
- ✅ Brancher les invitations d’équipe, la récupération de compte et les notifications de quart ou d’échange sur Resend lorsque `Resend__ApiKey` et `Resend__From` sont configurés; les opérations restent disponibles si l’envoi est désactivé.
- ✅ Ajouter recherche, filtres et pagination à l’historique coordonnateur.
- ✅ Utiliser une vraie session pour les comptes commerciaux; le sélecteur reste disponible uniquement dans la démo publique.

### 5. Mise en production

- Environnements séparés, migrations contrôlées, sauvegardes et vérification de restauration.
- ✅ Logs structurés, corrélation par requête, endpoint de readiness PostgreSQL et métriques Prometheus sans donnée personnelle; les alertes de disponibilité restent à brancher au fournisseur d’observabilité choisi.
- ✅ Test d’intégrité du contrat OpenAPI sur les routes opérationnelles critiques.
- ✅ Analyse des dépendances ajoutée à la CI pour les Pull Requests.
- ✅ Export JSON des données personnelles depuis l’espace Compte, sans secret et avec traçabilité.
- Tests navigateur des parcours critiques.
- ✅ Conditions d’utilisation, politique de confidentialité, procédure de support et suppression automatisée de compte documentées et implémentées; la validation juridique du modèle reste requise avant une commercialisation.

## Décision actuelle

Le MVP/V1 public couvre le domaine et les opérations principales avec des comptes de démonstration. La fondation des comptes réels, l’isolation organisationnelle, les invitations activables, la gestion des rattachements depuis l’interface coordonnateur, la révocation de sessions, l’historique exportable, les notifications dans l’application et l’envoi transactionnel configurable sont maintenant en place sans retirer le parcours public existant ; les prochaines tranches se concentrent sur la facturation et l’exploitation commerciale.
