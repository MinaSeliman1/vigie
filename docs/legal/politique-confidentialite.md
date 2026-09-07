# Politique de confidentialité de Vigie

**Version 1.0 — 6 septembre 2026**

Cette politique décrit les données utilisées par Vigie lorsque vous utilisez un espace d’organisation.

## Données traitées

Vigie peut traiter le nom, le courriel, le rôle, le rattachement à une piscine ou un secteur, les certifications, les disponibilités, les quarts, les assignations, les échanges, les notifications et les entrées d’audit nécessaires au fonctionnement du service. Les mots de passe sont conservés uniquement sous forme de hachages PBKDF2; les jetons d’invitation et de récupération sont conservés sous forme de hachages à usage unique.

Les journaux techniques contiennent la méthode, la route, le statut, la durée et un identifiant `X-Request-Id`. Ils ne doivent pas contenir de mot de passe, de jeton ou de détail personnel inutile.

## Finalités et fournisseurs

Les données servent à planifier les équipes, vérifier les règles métier, sécuriser les sessions, notifier les utilisateurs et répondre aux demandes de support. La base PostgreSQL peut être hébergée par Supabase et l’API par Render. Lorsque les variables sont configurées, Resend remet les invitations et les courriels de récupération. Ces fournisseurs ne reçoivent que les informations nécessaires à leur fonction.

## Conservation et droits

L’organisation conserve ses données pendant la durée de son utilisation et doit définir ses propres délais opérationnels. Un utilisateur connecté peut télécharger une copie de ses données depuis **Compte → Télécharger mes données** ou via `GET /api/v1/auth/export`; l’export exclut les secrets et ne peut pas être mis en cache. Une demande de correction ou de suppression peut être adressée au support par le responsable de l’organisation, avec vérification de l’identité et des obligations de conservation de l’audit.

Vigie n’utilise pas de publicité comportementale. Le jeton de session est conservé dans le stockage local du navigateur afin de maintenir la connexion; il est supprimé lors de la déconnexion.

Cette politique est un modèle produit et doit être adaptée aux lois applicables, aux contrats des fournisseurs et aux conseils juridiques avant une utilisation commerciale avec des données réelles.
